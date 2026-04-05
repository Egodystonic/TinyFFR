// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Reflection.Metadata;
using System.Security;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering.Local.Sync;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

[SuppressUnmanagedCodeSecurity]
sealed unsafe class LocalMeshBuilder : IMeshBuilder, IMeshImplProvider, IResourceDirectory<Mesh>, IDisposable {
	const string DefaultMeshName = "Unnamed Mesh";
	readonly ArrayPoolBackedMap<ResourceHandle<Mesh>, MeshBufferData> _activeMeshes = new();
	readonly ArrayPoolBackedMap<ResourceHandle<VertexBuffer>, int> _vertexBufferRefCounts = new();
	readonly ArrayPoolBackedMap<ResourceHandle<IndexBuffer>, int> _indexBufferRefCounts = new();
	readonly ObjectPool<LocalMeshPolygonGroup, LocalMeshBuilder> _meshPolyGroupPool;
	readonly ObjectPool<LocalMeshAnimationTable, LocalMeshBuilder> _meshAnimationTablePool;
	readonly ArrayPoolBackedMap<ResourceHandle<Mesh>, LocalMeshAnimationTable> _activeMeshAnimationTables = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	bool _isDisposed = false;
	nuint _nextHandleId = 0;

	public LocalMeshBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
		_meshPolyGroupPool = new(&CreateNewPolyGroupInstance, this);
		_meshAnimationTablePool = new(&CreateNewMeshAnimationTable, this);
	}

	static LocalMeshPolygonGroup CreateNewPolyGroupInstance(LocalMeshBuilder arg) => new(arg, &PolyGroupHeapPoolAccessorFunc, &ReturnPolyGroup);
	static LocalMeshAnimationTable CreateNewMeshAnimationTable(LocalMeshBuilder @this) => new(@this._globals);

	static HeapPool PolyGroupHeapPoolAccessorFunc(LocalMeshBuilder builder) {
		if (builder._isDisposed) {
			throw new ObjectDisposedException(
				nameof(IMeshBuilder), 
				$"The mesh builder that allocated the given {nameof(IMeshPolygonGroup)} has been disposed, " +
				$"therefore this polygon group is no longer valid."
			);
		}
		return builder._globals.HeapPool;
	}

	static void ReturnPolyGroup(LocalMeshBuilder builder, LocalMeshPolygonGroup group) {
		if (builder._isDisposed) return;
		builder._meshPolyGroupPool.Return(group);
	}

	public IMeshPolygonGroup AllocateNewPolygonGroup() {
		ThrowIfThisIsDisposed();
		return _meshPolyGroupPool.Rent();
	}

	public Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, in MeshCreationConfig config) {
		return ProcessVerticesAndCreateMesh(vertices, triangles, in config, 0);
	}

	public Mesh CreateMesh(ReadOnlySpan<MeshVertexSkeletal> vertices, ReadOnlySpan<VertexTriangle> triangles, ReadOnlySpan<SkeletalAnimationNode> skeletalNodes, in MeshCreationConfig config) {
		var boneCount = 0;
		for (var i = 0; i < skeletalNodes.Length; ++i) {
			if (skeletalNodes[i].CorrespondingBoneIndex >= boneCount) boneCount = skeletalNodes[i].CorrespondingBoneIndex!.Value + 1;
		}
		
		if (boneCount > IMeshBuilder.MaxSkeletalBoneCount) {
			throw new ArgumentException($"TinyFFR only supports a maximum of {IMeshBuilder.MaxSkeletalBoneCount} skeletal bones (given nodes refer to a bone at index {boneCount - 1}).");
		}
		
		// If there are 0 bones we'll create a mesh with a default value for a single bone instead.
		// This just makes things easier elsewhere as we don't have to branch around 'null' or 'invalid' skeleton setups.
		// Supplying an animation skeleton with no bones is kind of an error anyway and there's no meaningful result
		// when trying to apply an animation.
		// We could also just throw an exception, but some users may expect a mesh with 0 bones to behave the same as the non-skeletal overload.
		if (boneCount == 0) {
			var defaultNode = new SkeletalAnimationNode(
				Matrix4x4.Identity,
				Matrix4x4.Identity,
				null,
				0
			);
			 
			return CreateMesh(
				vertices, 
				triangles, 
				new ReadOnlySpan<SkeletalAnimationNode>(in defaultNode),
				in config
			);
		}

		var result = ProcessVerticesAndCreateMesh(vertices, triangles, in config, boneCount);

		var modelImportTransformMatrix = Matrix4x4.CreateTranslation(-(config.OriginTranslation.ToVector3())) * Matrix4x4.CreateScale(config.LinearRescalingFactor);
		var animTable = _meshAnimationTablePool.Rent();
		animTable.SetSkeleton(result, boneCount, skeletalNodes, modelImportTransformMatrix);
		_activeMeshAnimationTables.Add(result.Handle, animTable);
		return result;
	}
	
	Mesh ProcessVerticesAndCreateMesh<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, in MeshCreationConfig config, int boneCount) where TVertex : unmanaged, IMeshVertex {
		ThrowIfThisIsDisposed();
		static void CheckTriangleIndex(char indexChar, int triangleIndex, int value, int numVertices) {
			if (value < 0 || value >= numVertices) {
				throw new ArgumentException($"Index '{indexChar}' in triangle #{triangleIndex} (0-indexed) is \"{value}\"; " +
											$"expected a non-negative value smaller than the number of vertices ({numVertices}).");
			}
		}

		if (vertices.Length == 0) throw new ArgumentException("Vertices span must not be empty!", nameof(vertices));
		if (triangles.Length == 0) throw new ArgumentException("Triangles span must not be empty!", nameof(triangles));

		for (var i = 0; i < triangles.Length; ++i) {
			CheckTriangleIndex('A', i, triangles[i].IndexA, vertices.Length);
			CheckTriangleIndex('B', i, triangles[i].IndexB, vertices.Length);
			CheckTriangleIndex('C', i, triangles[i].IndexC, vertices.Length);
		}

		var tempVertexBuffer = _globals.CreateGpuHoldingBufferAndCopyData(vertices);
		var tempIndexBuffer = _globals.CreateGpuHoldingBufferAndCopyData(triangles);
		
		if (config.FlipTriangles) {
			var intSpan = tempIndexBuffer.AsSpan<int>();
			for (var i = 0; i < triangles.Length; ++i) {
				var a = intSpan[i * 3];
				var b = intSpan[i * 3 + 1];
				intSpan[i * 3] = b;
				intSpan[i * 3 + 1] = a;
			}
		}

		// ReSharper disable once CompareOfFloatsByEqualityOperator Direct comparison with 1f is correct and exact
		if (config.InvertTextureU || config.InvertTextureV || config.OriginTranslation != Vect.Zero || config.LinearRescalingFactor != 1f) {
			var vBufferSpan = tempVertexBuffer.AsSpan<TVertex>();
			for (var v = 0; v < vBufferSpan.Length; ++v) {
				vBufferSpan[v] = vBufferSpan[v] with {
					Location = (vBufferSpan[v].Location - config.OriginTranslation).ScaledFromOriginBy(config.LinearRescalingFactor),
					TextureCoords = (
						config.InvertTextureU ? 1f - vBufferSpan[v].TextureCoords.X : vBufferSpan[v].TextureCoords.X,
						config.InvertTextureV ? 1f - vBufferSpan[v].TextureCoords.Y : vBufferSpan[v].TextureCoords.Y
					)
				};
			}
		}

		int indexBufferCount;
		checked {
			indexBufferCount = triangles.Length * 3;
		}

		UIntPtr vbHandle;
		if (typeof(TVertex) == typeof(MeshVertex)) {
			AllocateVertexBuffer(tempVertexBuffer.BufferIdentity, (MeshVertex*) tempVertexBuffer.DataPtr, vertices.Length, out vbHandle).ThrowIfFailure();
		}
		else if (typeof(TVertex) == typeof(MeshVertexSkeletal)) {
			AllocateSkeletalVertexBuffer(tempVertexBuffer.BufferIdentity, (MeshVertexSkeletal*) tempVertexBuffer.DataPtr, vertices.Length, out vbHandle).ThrowIfFailure();
		}
		else {
			throw new InvalidOperationException($"Unexpected mesh vertex type '{typeof(TVertex)}'.");
		}
		
		AllocateIndexBuffer(tempIndexBuffer.BufferIdentity, (VertexTriangle*) tempIndexBuffer.DataPtr, indexBufferCount, out var ibHandle).ThrowIfFailure();

		_vertexBufferRefCounts.Add(vbHandle, 1);
		_indexBufferRefCounts.Add(ibHandle, 1);
		_nextHandleId++;
		var handle = new ResourceHandle<Mesh>(_nextHandleId);
		_activeMeshes.Add(handle, new(vbHandle, ibHandle, 0, indexBufferCount, boneCount));
		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, config.Name, DefaultMeshName);
		return new Mesh(handle, this);
	}

	public MeshBufferData GetBufferData(ResourceHandle<Mesh> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeMeshes[handle];
	}
	
	public MeshAnimation AttachAnimation(
		Mesh mesh, 
		ReadOnlySpan<SkeletalAnimationScalingKeyframe> scalingKeyframes, 
		ReadOnlySpan<SkeletalAnimationRotationKeyframe> rotationKeyframes, 
		ReadOnlySpan<SkeletalAnimationTranslationKeyframe> translationKeyframes, 
		ReadOnlySpan<SkeletalAnimationNodeMutationDescriptor> boneMutations, 
		float defaultCompletionTimeSeconds, 
		ReadOnlySpan<char> name
	) {
		var handle = mesh.Handle;
		if (!_activeMeshAnimationTables.TryGetValue(handle, out var animTable)) {
			throw new InvalidOperationException($"Can not attach animation to {mesh} as it was not created with skeletal vertex/bone data.");
		}
		return animTable.Add(scalingKeyframes, rotationKeyframes, translationKeyframes, boneMutations, defaultCompletionTimeSeconds, name);
	}
	public MeshAnimation AttachAnimationAndTransferBufferOwnership(
		Mesh mesh, 
		PooledHeapMemory<SkeletalAnimationScalingKeyframe> scalingKeyframes, 
		PooledHeapMemory<SkeletalAnimationRotationKeyframe> rotationKeyframes, 
		PooledHeapMemory<SkeletalAnimationTranslationKeyframe> translationKeyframes, 
		PooledHeapMemory<SkeletalAnimationNodeMutationDescriptor> boneMutations, 
		float defaultCompletionTimeSeconds, 
		ReadOnlySpan<char> name
	) {
		var handle = mesh.Handle;
		if (!_activeMeshAnimationTables.TryGetValue(handle, out var animTable)) {
			throw new InvalidOperationException($"Can not attach animation to {mesh} as it was not created with skeletal vertex/bone data.");
		}
		return animTable.AddAndTransferBufferOwnership(scalingKeyframes, rotationKeyframes, translationKeyframes, boneMutations, defaultCompletionTimeSeconds, name);
	}

	public IndirectEnumerable<Mesh, MeshAnimation> GetAnimations(ResourceHandle<Mesh> handle, MeshAnimationType? type) {
		ThrowIfThisOrHandleIsDisposed(handle);
		
		if (!_activeMeshAnimationTables.TryGetValue(handle, out var animTable)) return IndirectEnumerable<Mesh, MeshAnimation>.Empty;
		
		static LocalMeshAnimationTable? GetAnimTableForMeshIterator(Mesh m) {
			if (m.Implementation is not LocalMeshBuilder lmb) return null;
			if (!lmb._activeMeshAnimationTables.TryGetValue(m.Handle, out var animTable)) return null;
			return animTable;
		}
		static int GetMeshAnimCount(Mesh m) => GetAnimTableForMeshIterator(m)?.Count ?? -1;
		static MeshAnimation GetMeshAnimAtIndex(Mesh m, int index) => GetAnimTableForMeshIterator(m)?.GetAnimationAtUnstableIndex(index) ?? throw new InvalidOperationException("Somehow attempted to access null animation table.");
		static int GetMeshSkeletalAnimCount(Mesh m) {
			var animTable = GetAnimTableForMeshIterator(m);
			if (animTable == null) return -1;
			
			var result = 0;
			for (var i = 0; i < animTable.Count; ++i) {
				if (animTable.GetAnimationAtUnstableIndex(i).Type == MeshAnimationType.Skeletal) ++result;
			}
			return result;
		}
		static MeshAnimation GetMeshSkeletalAnimAtIndex(Mesh m, int index) {
			var animTable = GetAnimTableForMeshIterator(m);
			if (animTable == null) throw new InvalidOperationException("Somehow attempted to access null animation table.");
			
			var count = 0;
			for (var i = 0; i < animTable.Count; ++i) {
				var anim = animTable.GetAnimationAtUnstableIndex(i);
				if (anim.Type == MeshAnimationType.Skeletal) {
					if (count == index) return anim;
					++count;
				}
			}
			throw new ArgumentOutOfRangeException(nameof(index));
		}
		static int GetMeshMorphingAnimCount(Mesh m) {
			var animTable = GetAnimTableForMeshIterator(m);
			if (animTable == null) return -1;
			
			var result = 0;
			for (var i = 0; i < animTable.Count; ++i) {
				if (animTable.GetAnimationAtUnstableIndex(i).Type == MeshAnimationType.Morphing) ++result;
			}
			return result;
		}
		static MeshAnimation GetMeshMorphingAnimAtIndex(Mesh m, int index) {
			var animTable = GetAnimTableForMeshIterator(m);
			if (animTable == null) throw new InvalidOperationException("Somehow attempted to access null animation table.");
			
			var count = 0;
			for (var i = 0; i < animTable.Count; ++i) {
				var anim = animTable.GetAnimationAtUnstableIndex(i);
				if (anim.Type == MeshAnimationType.Morphing) {
					if (count == index) return anim;
					++count;
				}
			}
			throw new ArgumentOutOfRangeException(nameof(index));
		}
		
		switch (type) {
			case MeshAnimationType.Skeletal:
				return new IndirectEnumerable<Mesh, MeshAnimation>(
					HandleToInstance(handle),
					animTable.Count,
					&GetMeshSkeletalAnimCount,
					&GetMeshSkeletalAnimCount,
					&GetMeshSkeletalAnimAtIndex
				);
			case MeshAnimationType.Morphing:
				return new IndirectEnumerable<Mesh, MeshAnimation>(
					HandleToInstance(handle),
					animTable.Count,
					&GetMeshMorphingAnimCount,
					&GetMeshMorphingAnimCount,
					&GetMeshMorphingAnimAtIndex
				);
			default:
				return new IndirectEnumerable<Mesh, MeshAnimation>(
					HandleToInstance(handle),
					animTable.Count,
					&GetMeshAnimCount,
					&GetMeshAnimCount,
					&GetMeshAnimAtIndex
				);
		}
	}

	public MeshAnimation? TryGetAnimationByName(ResourceHandle<Mesh> handle, ReadOnlySpan<char> name, MeshAnimationType? type) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_activeMeshAnimationTables.TryGetValue(handle, out var animTable)) return null;
		
		var match = animTable.FindByName(name);
		if (match == null || (type != null && type != match.Value.Type)) return null;
		
		return match;
	}

	public void SetSkeletonNodeName(Mesh mesh, int nodeIndex, ReadOnlySpan<char> name) {
		var handle = mesh.Handle;
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_activeMeshAnimationTables.TryGetValue(handle, out var animTable)) return;
		
		animTable.SetNodeName(nodeIndex, name);
	}
	public IndirectEnumerable<Mesh, MeshNode> GetNodes(ResourceHandle<Mesh> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_activeMeshAnimationTables.ContainsKey(handle)) return IndirectEnumerable<Mesh, MeshNode>.Empty;
		
		static LocalMeshAnimationTable? GetAnimTableForMeshIterator(Mesh m) {
			if (m.Implementation is not LocalMeshBuilder lmb) return null;
			if (!lmb._activeMeshAnimationTables.TryGetValue(m.Handle, out var animTable)) return null;
			return animTable;
		}
		static int GetNodeCount(Mesh m) => GetAnimTableForMeshIterator(m)?.GetNodeCount() ?? -1;
		static int GetVersion(Mesh _) => 0;
		static MeshNode GetNode(Mesh m, int index) => GetAnimTableForMeshIterator(m)?.GetNode(index) ?? throw new InvalidOperationException("Somehow attempted to access null animation table.");
		
		return new IndirectEnumerable<Mesh, MeshNode>(
			HandleToInstance(handle),
			0,
			&GetNodeCount,
			&GetVersion,
			&GetNode
		);
	}
	public MeshNode? TryGetNodeByName(ResourceHandle<Mesh> handle, ReadOnlySpan<char> name) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_activeMeshAnimationTables.TryGetValue(handle, out var animTable)) return null;
		return animTable.TryGetNode(name);
	}
	public void GetSkeletalBindPoseNodeModelTransforms(ResourceHandle<Mesh> handle, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_activeMeshAnimationTables.TryGetValue(handle, out var animTable)) {
			if (nodes.Length > modelSpaceTransforms.Length) {
				throw new ArgumentException($"Requested {nodes.Length} {nameof(nodes)}, but {nameof(modelSpaceTransforms)} destination span is too small (length {modelSpaceTransforms.Length}).", nameof(modelSpaceTransforms));
			}
			for (var i = 0; i < nodes.Length; ++i) {
				modelSpaceTransforms[i] = Matrix4x4.Identity;
			}
			return;
		}
		animTable.GetBindPoseNodeTransforms(nodes, modelSpaceTransforms);
	}
	public void GetSkeletalBindPoseNodeModelTransforms(ResourceHandle<Mesh> handle, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_activeMeshAnimationTables.TryGetValue(handle, out var animTable)) {
			if (nodeIndices.Length > modelSpaceTransforms.Length) {
				throw new ArgumentException($"Requested {nodeIndices.Length} {nameof(nodeIndices)}, but {nameof(modelSpaceTransforms)} destination span is too small (length {modelSpaceTransforms.Length}).", nameof(modelSpaceTransforms));
			}
			for (var i = 0; i < nodeIndices.Length; ++i) {
				modelSpaceTransforms[i] = Matrix4x4.Identity;
			}
			return;
		}
		animTable.GetBindPoseNodeTransforms(nodeIndices, modelSpaceTransforms);
	}

	public void ApplySkeletalBindPose(ResourceHandle<Mesh> handle, ModelInstance targetInstance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_activeMeshAnimationTables.TryGetValue(handle, out var animTable)) return;
		
		animTable.ApplyBindPose(targetInstance);
	}

	public string GetNameAsNewStringObject(ResourceHandle<Mesh> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultMeshName));
	}
	public int GetNameLength(ResourceHandle<Mesh> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultMeshName).Length;
	}
	public void CopyName(ResourceHandle<Mesh> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultMeshName, destinationBuffer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Mesh HandleToInstance(ResourceHandle<Mesh> h) => new(h, this);

	#region Resource Directory
	public IndirectEnumerable<object, Mesh> AllActiveInstances {
		get {
			static LocalMeshBuilder CastSelf(object self) => self as LocalMeshBuilder ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetCount(object self) => CastSelf(self)._activeMeshes.Count;
			static int GetVersion(object self) => CastSelf(self)._activeMeshes.Version;
			static Mesh GetItem(object self, int index) => CastSelf(self).HandleToInstance(CastSelf(self)._activeMeshes.GetPairAtIndex(index).Key);

			ThrowIfThisIsDisposed();
			return new(
				this,
				GetVersion(this),
				&GetCount,
				&GetVersion,
				&GetItem
			);
		}
	}
	public bool ResourceNameMatchIsMatching(Mesh resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var handle = resource.GetHandleWithoutDisposeCheck();
		ThrowIfThisOrHandleIsDisposed(handle);
		return allowPartialMatch
			? _globals.GetResourceName(handle.Ident, DefaultMeshName).Contains(name, comparisonType)
			: _globals.GetResourceName(handle.Ident, DefaultMeshName).Equals(name, comparisonType);
	}
	#endregion

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_vertex_buffer")]
	static extern InteropResult AllocateVertexBuffer(
		nuint bufferId,
		MeshVertex* verticesPtr,
		int numVertices,
		out UIntPtr outBufferHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_vertex_buffer_skeletal")]
	static extern InteropResult AllocateSkeletalVertexBuffer(
		nuint bufferId,
		MeshVertexSkeletal* verticesPtr,
		int numVertices,
		out UIntPtr outBufferHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_vertex_buffer")]
	static extern InteropResult DisposeVertexBuffer(
		UIntPtr bufferHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_index_buffer")]
	static extern InteropResult AllocateIndexBuffer(
		nuint bufferId,
		VertexTriangle* indicesPtr,
		int numIndices,
		out UIntPtr outBufferHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_index_buffer")]
	static extern InteropResult DisposeIndexBuffer(
		UIntPtr bufferHandle
	);
	#endregion

	public override string ToString() => _isDisposed ? "TinyFFR Local Mesh Builder [Disposed]" : "TinyFFR Local Mesh Builder";

	#region Disposal
	public bool IsDisposed(ResourceHandle<Mesh> handle) => _isDisposed || !_activeMeshes.ContainsKey(handle);

	public void Dispose(ResourceHandle<Mesh> handle) => Dispose(handle, removeFromMap: true);
	void Dispose(ResourceHandle<Mesh> handle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		
#pragma warning disable CA2000 // Compiler incorrectly assumes animTable is going out of scope here and warns me to invoke Dispose() on it
		if (_activeMeshAnimationTables.Remove(handle, out var animTable)) {
#pragma warning restore CA2000
			animTable.Recycle();
			_meshAnimationTablePool.Return(animTable);
		}
		
		var bufferData = _activeMeshes[handle];
		var curVbRefCount = _vertexBufferRefCounts[bufferData.VertexBufferHandle];
		var curIbRefCount = _indexBufferRefCounts[bufferData.IndexBufferHandle];
		if (curVbRefCount <= 1) {
			_vertexBufferRefCounts.Remove(bufferData.VertexBufferHandle);
			LocalFrameSynchronizationManager.QueueResourceDisposal(bufferData.VertexBufferHandle, &DisposeVertexBuffer);
		}
		if (curIbRefCount <= 1) {
			_indexBufferRefCounts.Remove(bufferData.IndexBufferHandle);
			LocalFrameSynchronizationManager.QueueResourceDisposal(bufferData.IndexBufferHandle, &DisposeIndexBuffer);
		}
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromMap) _activeMeshes.Remove(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _activeMeshes) Dispose(kvp.Key, removeFromMap: false);
			_activeMeshes.Dispose();
			_activeMeshAnimationTables.Dispose();

			// In theory, both ref-count maps should be empty at this point. But we'll do this anyway.
			foreach (var kvp in _vertexBufferRefCounts) {
				DisposeVertexBuffer(kvp.Key).ThrowIfFailure();
			}
			foreach (var kvp in _indexBufferRefCounts) {
				DisposeIndexBuffer(kvp.Key).ThrowIfFailure();
			}
			_vertexBufferRefCounts.Dispose();
			_indexBufferRefCounts.Dispose();
			_meshAnimationTablePool.Dispose(invokeDisposeOnEachItemBeforeRelease: true);
			_meshPolyGroupPool.Dispose(invokeDisposeOnEachItemBeforeRelease: false);
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Mesh> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Mesh));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}