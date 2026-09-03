// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Reflection.Metadata;
using System.Security;
using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Rendering.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;
using Egodystonic.TinyFFR.World;
using static Egodystonic.TinyFFR.Assets.Baking.BakedResourceSchemata;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

[SuppressUnmanagedCodeSecurity]
sealed unsafe class LocalMeshBuilder : IMeshBuilder, IMeshImplProvider, IResourceDirectory<Mesh>, IResourceDirectory<MeshAnimation>, IResourceDirectory<MeshNode>, IResourceDirectory<DynamicVertexBuffer>, IDisposable {
	readonly record struct MeshData(MeshBufferData BufferData, PositionedCuboid BoundingBox);
	
	readonly record struct DynamicVertexBufferData(
		UIntPtr VertexBufferHandle,
		UIntPtr IndexBufferHandle,
		int VertexCapacity,
		int IndexCapacity,
		bool UsesImGuiVertices,
		PooledHeapMemory<MeshVertex>? Vertices,
		PooledHeapMemory<ushort>? Indices,
		PositionedCuboid BoundingBox
	);
	readonly record struct DynamicBufferLeaseData(Range Range, bool RecalculateBoundingBox, bool OverwriteChildMeshBoundingBoxes);

	const string DefaultMeshName = "Unnamed Mesh";
	const string DefaultDynamicVertexBufferName = "Unnamed Dynamic Vertex Buffer";
	readonly ArrayPoolBackedMap<ResourceHandle<DynamicVertexBuffer>, DynamicVertexBufferData> _activeDynamicVertexBuffers = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Mesh>, ResourceHandle<DynamicVertexBuffer>> _meshViewParents = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Mesh>, MeshData> _activeMeshes = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Mesh>, PooledHeapMemory<MeshVertex>> _defaultMutableVerticesMap = new();
	readonly ResourceHandleBasedSpanLeaseTracker<MeshVertex> _mutableVertexLeaseTracker;
	readonly ResourceHandleBasedSpanLeaseTracker<MeshVertex, DynamicBufferLeaseData> _dynamicVertexLeaseTracker;
	readonly ResourceHandleBasedSpanLeaseTracker<ushort, DynamicBufferLeaseData> _dynamicIndexLeaseTracker;
	readonly ArrayPoolBackedMap<ResourceHandle<VertexBuffer>, int> _vertexBufferRefCounts = new();
	readonly ArrayPoolBackedMap<ResourceHandle<IndexBuffer>, int> _indexBufferRefCounts = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Mesh>, MeshBufferData> _activeMeshWireframeBufferData = new();
	readonly ArrayPoolBackedObjectPool<LocalMeshPolygonGroup, LocalMeshBuilder> _meshPolyGroupPool;
	readonly ArrayPoolBackedObjectPool<LocalMeshAnimationTable, LocalMeshBuilder> _meshAnimationTablePool;
	readonly ArrayPoolBackedMap<ResourceHandle<Mesh>, LocalMeshAnimationTable> _activeMeshAnimationTables = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	bool _isDisposed = false;
	nuint _prevHandleId = 0;
	int _meshAnimDirectoryVersion = 0;
	LocalDynamicVertexBufferImplProvider? _dynamicVertexBufferImplProvider;

	internal LocalDynamicVertexBufferImplProvider DynamicVertexBufferImplProvider => _dynamicVertexBufferImplProvider ??= new(this);

	public LocalMeshBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
		_meshPolyGroupPool = new(&CreateNewPolyGroupInstance, this);
		_meshAnimationTablePool = new(&CreateNewMeshAnimationTable, this);
		_mutableVertexLeaseTracker = new(null, true, globals.InEnhancedSecurityEnvironment);
		_dynamicVertexLeaseTracker = new(null, true, globals.InEnhancedSecurityEnvironment, &HandleDynamicVertexLeaseDisposal, this);
		_dynamicIndexLeaseTracker = new(null, true, globals.InEnhancedSecurityEnvironment, &HandleDynamicIndexLeaseDisposal, this);
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

	public ScopedSpanLease<MeshVertex> GetPooledVertexBuffer(int vertexCount) {
		ThrowIfThisIsDisposed();
		return _globals.HeapPool.CreateSpanLease<MeshVertex>(vertexCount);
	}
	public ScopedSpanLease<VertexTriangle> GetPooledTriangleBuffer(int triangleCount) {
		ThrowIfThisIsDisposed();
		return _globals.HeapPool.CreateSpanLease<VertexTriangle>(triangleCount);
	}

	public Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, in MeshCreationConfig config) {
		return ProcessVerticesAndCreateMesh(vertices, triangles, in config, 0);
	}

	public Mesh CreateMesh(ReadOnlySpan<MeshVertexSkeletal> vertices, ReadOnlySpan<VertexTriangle> triangles, ReadOnlySpan<SkeletalAnimationNode> skeletalNodes, in MeshCreationConfig config) {
		var boneCount = CalculateAndValidateBoneCount(skeletalNodes);

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
		AttachSkeletonToMesh(result, boneCount, skeletalNodes, config.OriginTranslation, config.LinearRescalingFactor);
		return result;
	}

	internal static int CalculateAndValidateBoneCount(ReadOnlySpan<SkeletalAnimationNode> skeletalNodes) {
		var boneCount = 0;
		for (var i = 0; i < skeletalNodes.Length; ++i) {
			if (skeletalNodes[i].CorrespondingBoneIndex >= boneCount) boneCount = skeletalNodes[i].CorrespondingBoneIndex!.Value + 1;
		}

		if (boneCount > IMeshBuilder.MaxSkeletalBoneCount) {
			throw new ArgumentException($"TinyFFR only supports a maximum of {IMeshBuilder.MaxSkeletalBoneCount} skeletal bones (given nodes refer to a bone at index {boneCount - 1}).");
		}

		return boneCount;
	}

	internal void AttachSkeletonToMesh(Mesh mesh, int boneCount, ReadOnlySpan<SkeletalAnimationNode> skeletalNodes, Vect originTranslation, float linearRescalingFactor) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		var modelImportTransformMatrix = Matrix4x4.CreateTranslation(-(originTranslation.ToVector3())) * Matrix4x4.CreateScale(linearRescalingFactor);
		var animTable = _meshAnimationTablePool.Rent();
		animTable.SetSkeleton(mesh, boneCount, skeletalNodes, modelImportTransformMatrix);
		_activeMeshAnimationTables.Add(mesh.Handle, animTable);
	}

	internal static void ValidateMeshData<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, in MeshCreationConfig config) where TVertex : unmanaged, IMeshVertex {
		static void CheckTriangleIndex(char indexChar, int triangleIndex, int value, int numVertices) {
			if (value < 0 || value >= numVertices) {
				throw new ArgumentException($"Index '{indexChar}' in triangle #{triangleIndex} (0-indexed) is \"{value}\"; " +
											$"expected a non-negative value smaller than the number of vertices ({numVertices}).");
			}
		}

		if (vertices.Length == 0) throw new ArgumentException("Vertices span must not be empty!", nameof(vertices));
		if (triangles.Length == 0) throw new ArgumentException("Triangles span must not be empty!", nameof(triangles));

		if (config.AllowsPerInstanceVertexMutation && typeof(TVertex) != typeof(MeshVertex)) {
			throw new ArgumentException($"Per-instance vertex mutation is only supported for non-skeletal meshes.", nameof(config));
		}

		for (var i = 0; i < triangles.Length; ++i) {
			CheckTriangleIndex('A', i, triangles[i].IndexA, vertices.Length);
			CheckTriangleIndex('B', i, triangles[i].IndexB, vertices.Length);
			CheckTriangleIndex('C', i, triangles[i].IndexC, vertices.Length);
		}
	}

	internal static bool GetShouldGenerateWireframeData<TVertex>(in MeshCreationConfig config) where TVertex : unmanaged, IMeshVertex {
		return config.GenerateWireframeData
			&& !config.AllowsPerInstanceVertexMutation
			&& typeof(TVertex) == typeof(MeshVertex);
	}

	internal static void FlipTriangleWindings(Span<VertexTriangle> triangles) {
		for (var i = 0; i < triangles.Length; ++i) {
			triangles[i] = new VertexTriangle(triangles[i].IndexB, triangles[i].IndexA, triangles[i].IndexC);
		}
	}

	internal static void ApplyVertexTransforms<TVertex>(Span<TVertex> vertices, in MeshCreationConfig config) where TVertex : unmanaged, IMeshVertex {
		// ReSharper disable once CompareOfFloatsByEqualityOperator Direct comparison with 1f is correct and exact
		if (!config.InvertTextureU && !config.InvertTextureV && config.OriginTranslation == Vect.Zero && config.LinearRescalingFactor == 1f) return;

		for (var v = 0; v < vertices.Length; ++v) {
			vertices[v] = vertices[v] with {
				Location = (vertices[v].Location - config.OriginTranslation).ScaledFromOriginBy(config.LinearRescalingFactor),
				TextureCoords = (
					config.InvertTextureU ? 1f - vertices[v].TextureCoords.X : vertices[v].TextureCoords.X,
					config.InvertTextureV ? 1f - vertices[v].TextureCoords.Y : vertices[v].TextureCoords.Y
				)
			};
		}
	}

	// Maintainer's note: Do not replace this with the CalculateBoundingBox override that takes a margin parameter;
	// the code below applies the margin to the user's override if there is one.
	internal static PositionedCuboid CalculateMeshBoundingBox<TVertex>(ReadOnlySpan<TVertex> transformedVertices, in MeshCreationConfig config) where TVertex : unmanaged, IMeshVertex {
		return (config.BoundingBoxOverride ?? MathUtils.CalculateBoundingBox(transformedVertices))
			.WithAllExtentsAdjustedBy(config.BoundingBoxAdditionalMargin);
	}

	Mesh ProcessVerticesAndCreateMesh<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, in MeshCreationConfig config, int boneCount) where TVertex : unmanaged, IMeshVertex {
		ThrowIfThisIsDisposed();
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ValidateMeshData(vertices, triangles, in config);

		var generateWireframeData = GetShouldGenerateWireframeData<TVertex>(in config);

		_prevHandleId++;
		var handle = new ResourceHandle<Mesh>(_prevHandleId);

		var tempVertexBuffer = _globals.CreateGpuHoldingBufferAndCopyData(vertices);
		var tempIndexBuffer = _globals.CreateGpuHoldingBufferAndCopyData(triangles);

		if (config.FlipTriangles) FlipTriangleWindings(tempIndexBuffer.AsSpan<VertexTriangle>());
		ApplyVertexTransforms(tempVertexBuffer.AsSpan<TVertex>(), in config);

		var boundingBox = CalculateMeshBoundingBox<TVertex>(tempVertexBuffer.AsSpan<TVertex>(), in config);

		return CompleteMeshCreation<TVertex>(
			handle,
			tempVertexBuffer,
			tempIndexBuffer,
			vertices.Length,
			triangles.Length,
			boundingBox,
			config.AllowsPerInstanceVertexMutation,
			generateWireframeData,
			config.Name,
			boneCount
		);
	}

	internal Mesh CreateMeshFromPreValidatedAndTransformedData<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, PositionedCuboid boundingBox, bool allowsPerInstanceVertexMutation, bool generateWireframeData, ReadOnlySpan<char> name, int boneCount) where TVertex : unmanaged, IMeshVertex {
		ThrowIfThisIsDisposed();
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();

		_prevHandleId++;
		var handle = new ResourceHandle<Mesh>(_prevHandleId);

		var tempVertexBuffer = _globals.CreateGpuHoldingBufferAndCopyData(vertices);
		var tempIndexBuffer = _globals.CreateGpuHoldingBufferAndCopyData(triangles);

		return CompleteMeshCreation<TVertex>(
			handle,
			tempVertexBuffer,
			tempIndexBuffer,
			vertices.Length,
			triangles.Length,
			boundingBox,
			allowsPerInstanceVertexMutation,
			generateWireframeData,
			name,
			boneCount
		);
	}

	Mesh CompleteMeshCreation<TVertex>(ResourceHandle<Mesh> handle, TemporaryLoadSpaceBuffer tempVertexBuffer, TemporaryLoadSpaceBuffer tempIndexBuffer, int vertexCount, int triangleCount, PositionedCuboid boundingBox, bool allowsPerInstanceVertexMutation, bool generateWireframeData, ReadOnlySpan<char> name, int boneCount) where TVertex : unmanaged, IMeshVertex {
		var indexBufferCount = checked(triangleCount * 3);

		if (generateWireframeData) {
			GenerateAndStoreWireframeBuffers(handle, tempVertexBuffer.AsSpan<MeshVertex>(), tempIndexBuffer.AsSpan<VertexTriangle>());
		}

		var bakeryVertexBufferCopy = GetBakeryBufferCopyIfEnabled(MemoryMarshal.AsBytes(tempVertexBuffer.AsSpan<TVertex>()[..vertexCount]));
		var bakeryIndexBufferCopy = GetBakeryBufferCopyIfEnabled(MemoryMarshal.AsBytes(tempIndexBuffer.AsSpan<VertexTriangle>()[..triangleCount]));

		UIntPtr vbHandle;
		if (typeof(TVertex) == typeof(MeshVertex)) {
			AllocateVertexBuffer(tempVertexBuffer.BufferIdentity, (MeshVertex*) tempVertexBuffer.DataPtr, vertexCount, out vbHandle).ThrowIfFailure();
			if (allowsPerInstanceVertexMutation) {
				_defaultMutableVerticesMap[handle] = _globals.HeapPool.BorrowAndCopy(tempVertexBuffer.AsSpan<MeshVertex>());
			}
		}
		else if (typeof(TVertex) == typeof(MeshVertexSkeletal)) {
			AllocateSkeletalVertexBuffer(tempVertexBuffer.BufferIdentity, (MeshVertexSkeletal*) tempVertexBuffer.DataPtr, vertexCount, out vbHandle).ThrowIfFailure();
		}
		else {
			throw new InvalidOperationException($"Unexpected mesh vertex type '{typeof(TVertex)}'.");
		}

		AllocateIndexBuffer(tempIndexBuffer.BufferIdentity, (VertexTriangle*) tempIndexBuffer.DataPtr, indexBufferCount, out var ibHandle).ThrowIfFailure();

		_vertexBufferRefCounts.Add(vbHandle, 1);
		_indexBufferRefCounts.Add(ibHandle, 1);
		_activeMeshes.Add(handle, new(new MeshBufferData(vbHandle, ibHandle, 0, indexBufferCount, boneCount), boundingBox));
		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, name, DefaultMeshName);
		var result = new Mesh(handle, this);

		if (bakeryVertexBufferCopy is { } bvbc && bakeryIndexBufferCopy is { } bibc) {
			try {
				RegisterInBakery<TVertex>(result, bvbc.Span, bibc.Span, vertexCount, triangleCount, boundingBox, boneCount, allowsPerInstanceVertexMutation, generateWireframeData, name);
			}
			finally {
				bvbc.Dispose();
				bibc.Dispose();
			}
		}

		return result;
	}

	PooledHeapMemory<byte>? GetBakeryBufferCopyIfEnabled(ReadOnlySpan<byte> data) {
		if (!_globals.Bakery.Enabled) return null;
		var result = _globals.HeapPool.Borrow(data.Length);
		data.CopyTo(result.Span);
		return result;
	}

	void RegisterInBakery<TVertex>(Mesh resource, ReadOnlySpan<byte> vertexData, ReadOnlySpan<byte> indexData, int vertexCount, int triangleCount, PositionedCuboid boundingBox, int boneCount, bool allowsPerInstanceVertexMutation, bool generateWireframeData, ReadOnlySpan<char> name) where TVertex : unmanaged, IMeshVertex {
		var bakery = _globals.Bakery;

		bakery.StartResourceBake(resource);
		bakery.AddResourceBakeValue(resource, LocalAssetBakery.ResourceNameSectionName, name);
		bakery.AddResourceBakeValue(resource, MeshBakingSchema.IsSkeletal, typeof(TVertex) == typeof(MeshVertexSkeletal));
		bakery.AddResourceBakeValue(resource, MeshBakingSchema.VertexCount, vertexCount);
		bakery.AddResourceBakeValue(resource, MeshBakingSchema.TriangleCount, triangleCount);
		bakery.AddResourceBakeValue(resource, MeshBakingSchema.BoundingBox, boundingBox);
		bakery.AddResourceBakeValue(resource, MeshBakingSchema.BoneCount, boneCount);
		bakery.AddResourceBakeValue(resource, MeshBakingSchema.AllowsPerInstanceVertexMutation, allowsPerInstanceVertexMutation);
		bakery.AddResourceBakeValue(resource, MeshBakingSchema.GeneratesWireframeData, generateWireframeData);
		bakery.AddResourceBakeValue(resource, MeshBakingSchema.VertexData, vertexData);
		bakery.AddResourceBakeValue(resource, MeshBakingSchema.IndexData, indexData);
		bakery.CompleteResourceBakePendingFinalization(resource, this, &FinalizeBake);
	}

	static void FinalizeBake(object invoker, LocalAssetBakery bakery, Mesh resource) {
		var self = (LocalMeshBuilder) invoker;
		if (!self._activeMeshAnimationTables.TryGetValue(resource.GetHandleWithoutDisposeCheck(), out var animTable)) return;
		if (!animTable.SkeletonIsSet) return;
		animTable.WriteBakeData(bakery, resource);
	}

	internal void RestoreBakedSkeleton(
		Mesh mesh,
		int nodeCount,
		int boneCount,
		int firstParentedNodeIndex,
		Matrix4x4 modelImportTransformMatrix,
		ReadOnlySpan<Matrix4x4> defaultLocalTransforms,
		ReadOnlySpan<Matrix4x4> bindPoseInversions,
		ReadOnlySpan<int> parentIndices,
		ReadOnlySpan<int> boneToNodeMap,
		ReadOnlySpan<int> mutationTargetIndexMap
	) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		var animTable = _meshAnimationTablePool.Rent();
		animTable.SetSkeletonFromBakedData(
			mesh,
			nodeCount,
			boneCount,
			firstParentedNodeIndex,
			modelImportTransformMatrix,
			defaultLocalTransforms,
			bindPoseInversions,
			parentIndices,
			boneToNodeMap,
			mutationTargetIndexMap
		);
		_activeMeshAnimationTables.Add(mesh.Handle, animTable);
	}

	internal void RestoreBakedAnimation(
		Mesh mesh,
		ReadOnlySpan<SkeletalAnimationScalingKeyframe> scalingKeyframes,
		ReadOnlySpan<SkeletalAnimationRotationKeyframe> rotationKeyframes,
		ReadOnlySpan<SkeletalAnimationTranslationKeyframe> translationKeyframes,
		ReadOnlySpan<SkeletalAnimationNodeMutationDescriptor> nodeMutations,
		float defaultCompletionTimeSeconds,
		ReadOnlySpan<char> name
	) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		_activeMeshAnimationTables[mesh.Handle].AddPreProcessedAnimation(scalingKeyframes, rotationKeyframes, translationKeyframes, nodeMutations, defaultCompletionTimeSeconds, name);
	}

	internal void RestoreBakedNodeName(Mesh mesh, int nodeIndex, ReadOnlySpan<char> name) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		_activeMeshAnimationTables[mesh.Handle].SetNodeNameFromBakedData(nodeIndex, name);
	}

	public MeshBufferData GetBufferData(ResourceHandle<Mesh> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeMeshes[handle].BufferData;
	}

	public MeshBufferData? GetWireframeBufferData(ResourceHandle<Mesh> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeMeshWireframeBufferData.TryGetValue(handle, out var data) ? data : null;
	}

	void GenerateAndStoreWireframeBuffers(ResourceHandle<Mesh> handle, ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles) {
		var numIndices = checked(triangles.Length * 3);
		var wireframeVerticesBuffer = _globals.CreateGpuHoldingBuffer<MeshVertexPrimitive>(numIndices);
		var wireframeTrianglesBuffer = _globals.CreateGpuHoldingBuffer<VertexTriangle>(triangles.Length);
		
		var wireframeVertices = wireframeVerticesBuffer.AsSpan<MeshVertexPrimitive>();
		var wireframeTriangles = wireframeTrianglesBuffer.AsSpan<VertexTriangle>();

		for (var i = 0; i < triangles.Length; ++i) {
			var vertA = vertices[triangles[i].IndexA];
			var vertB = vertices[triangles[i].IndexB];
			var vertC = vertices[triangles[i].IndexC];
			wireframeVertices[i * 3 + 0] = new MeshVertexPrimitive(vertA.Location, new(1f, 0f, 0f, 1f), vertA.TangentRotation);
			wireframeVertices[i * 3 + 1] = new MeshVertexPrimitive(vertB.Location, new(0f, 1f, 0f, 1f), vertB.TangentRotation);
			wireframeVertices[i * 3 + 2] = new MeshVertexPrimitive(vertC.Location, new(0f, 0f, 1f, 1f), vertC.TangentRotation);
			
			wireframeTriangles[i] = new VertexTriangle(i * 3, i * 3 + 1, i * 3 + 2);
		}

		AllocateVertexBufferPrimitive(
			wireframeVerticesBuffer.BufferIdentity, 
			(MeshVertexPrimitive*) wireframeVerticesBuffer.DataPtr, 
			numIndices, 
			out var vbHandle
		).ThrowIfFailure();
		AllocateIndexBuffer(
			wireframeTrianglesBuffer.BufferIdentity, 
			(VertexTriangle*) wireframeTrianglesBuffer.DataPtr, 
			numIndices, 
			out var ibHandle
		).ThrowIfFailure();

		_activeMeshWireframeBufferData.Add(handle, new MeshBufferData(vbHandle, ibHandle, 0, numIndices, 0));
	}

	public PositionedCuboid GetBoundingBox(ResourceHandle<Mesh> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeMeshes[handle].BoundingBox;
	}

	public bool GetAllowsPerInstanceVertexMutation(ResourceHandle<Mesh> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _defaultMutableVerticesMap.ContainsKey(handle);
	}

	public ScopedReadOnlySpanLease<MeshVertex> BorrowDefaultVerticesSpan(ResourceHandle<Mesh> handle, Range range) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_defaultMutableVerticesMap.TryGetValue(handle, out var result)) {
			throw new InvalidOperationException(
				$"Can not load or modify vertices for instances of {HandleToInstance(handle)} as it was not created " +
				$"with the '{nameof(MeshCreationConfig.AllowsPerInstanceVertexMutation)}' flag set to true."
			);
		}

		return _mutableVertexLeaseTracker.CreateScopedLeaseOrThrow(handle, result.Span[range]);
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
		++_meshAnimDirectoryVersion;
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
		++_meshAnimDirectoryVersion;
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

	#region Dynamic Vertex Buffers
	public DynamicVertexBuffer CreateDynamicVertexBuffer(int initialVertexCapacity, int initialIndexCapacity, ReadOnlySpan<char> name = default) {
		return CreateDynamicVertexBuffer(initialVertexCapacity, initialIndexCapacity, usesImGuiVertices: false, name);
	}
	public DynamicVertexBuffer CreateImGuiDynamicVertexBuffer(int initialVertexCapacity, int initialIndexCapacity, ReadOnlySpan<char> name = default) {
		return CreateDynamicVertexBuffer(initialVertexCapacity, initialIndexCapacity, usesImGuiVertices: true, name);
	}
	DynamicVertexBuffer CreateDynamicVertexBuffer(int initialVertexCapacity, int initialIndexCapacity, bool usesImGuiVertices, ReadOnlySpan<char> name) {
		ThrowIfThisIsDisposed();
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		
		if (initialVertexCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(initialVertexCapacity), initialVertexCapacity, "Initial vertex capacity must be positive.");
		if (initialIndexCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(initialIndexCapacity), initialIndexCapacity, "Initial index capacity must be positive.");

		var vbHandle = AllocateDynamicVertexBuffer(initialVertexCapacity, usesImGuiVertices);
		var ibHandle = AllocateDynamicIndexBuffer(initialIndexCapacity);
		_vertexBufferRefCounts.Add(vbHandle, 1);
		_indexBufferRefCounts.Add(ibHandle, 1);

		PooledHeapMemory<MeshVertex>? vertexMirror = null;
		PooledHeapMemory<ushort>? indexMirror = null;
		if (!usesImGuiVertices) {
			var vertices = _globals.HeapPool.Borrow<MeshVertex>(initialVertexCapacity);
			vertices.Span.Clear();
			vertexMirror = vertices;
			var indices = _globals.HeapPool.Borrow<ushort>(initialIndexCapacity);
			indices.Span.Clear();
			indexMirror = indices;
		}

		var handle = (ResourceHandle<DynamicVertexBuffer>) (++_prevHandleId);
		_activeDynamicVertexBuffers.Add(handle, new(
			vbHandle,
			ibHandle,
			initialVertexCapacity,
			initialIndexCapacity,
			usesImGuiVertices,
			vertexMirror,
			indexMirror,
			vertexMirror is { } mirror ? CalculateFullBoundingBox(mirror) : default
		));
		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, name, DefaultDynamicVertexBufferName);
		return HandleToInstance(handle);
	}

	UIntPtr AllocateDynamicVertexBuffer(int capacity, bool usesImGuiVertices) {
		if (usesImGuiVertices) {
			var buffer = _globals.CreateGpuHoldingBuffer<MeshVertexImGui>(capacity);
			buffer.AsSpan<MeshVertexImGui>().Clear();
			AllocateVertexBufferImGui(buffer.BufferIdentity, (MeshVertexImGui*) buffer.DataPtr, capacity, out var result).ThrowIfFailure();
			return result;
		}
		else {
			var buffer = _globals.CreateGpuHoldingBuffer<MeshVertex>(capacity);
			buffer.AsSpan<MeshVertex>().Clear();
			AllocateVertexBuffer(buffer.BufferIdentity, (MeshVertex*) buffer.DataPtr, capacity, out var result).ThrowIfFailure();
			return result;
		}
	}
	UIntPtr AllocateDynamicIndexBuffer(int capacity) {
		var buffer = _globals.CreateGpuHoldingBuffer<ushort>(capacity);
		buffer.AsSpan<ushort>().Clear();
		AllocateIndexBufferUShort(buffer.BufferIdentity, (ushort*) buffer.DataPtr, capacity, out var result).ThrowIfFailure();
		return result;
	}
	static PositionedCuboid CalculateFullBoundingBox(PooledHeapMemory<MeshVertex> vertices) {
		return MathUtils.CalculateBoundingBox<MeshVertex>(vertices.Span, MeshCreationConfig.DefaultBoundingBoxAdditionalMargin);
	}

	public int GetVertexBufferSize(ResourceHandle<DynamicVertexBuffer> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeDynamicVertexBuffers[handle].VertexCapacity;
	}
	public int GetIndexBufferSize(ResourceHandle<DynamicVertexBuffer> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeDynamicVertexBuffers[handle].IndexCapacity;
	}

	public void ResizeVertexBuffer(ResourceHandle<DynamicVertexBuffer> handle, int newBufferSize) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (newBufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(newBufferSize), newBufferSize, "Vertex capacity must be positive.");
		var data = _activeDynamicVertexBuffers[handle];
		if (data.VertexCapacity == newBufferSize) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		ThrowIfAnyActiveDynamicBufferLeases(handle);

		_vertexBufferRefCounts.Remove(data.VertexBufferHandle);
		LocalFrameSynchronizationManager.QueueResourceDisposal(data.VertexBufferHandle, &DisposeVertexBuffer);
		var newVbHandle = AllocateDynamicVertexBuffer(newBufferSize, data.UsesImGuiVertices);
		_vertexBufferRefCounts.Add(newVbHandle, 1);

		var newVertices = data.Vertices;
		if (data.Vertices is { } oldVertices) {
			var replacement = _globals.HeapPool.Borrow<MeshVertex>(newBufferSize);
			replacement.Span.Clear();
			oldVertices.Span[..Int32.Min(oldVertices.Span.Length, newBufferSize)].CopyTo(replacement.Span);
			oldVertices.Dispose();
			newVertices = replacement;
			UploadVertices(newVbHandle, replacement.Span, 0);
		}
		_activeDynamicVertexBuffers[handle] = data with { VertexBufferHandle = newVbHandle, VertexCapacity = newBufferSize, Vertices = newVertices };
	}

	public void ResizeIndexBuffer(ResourceHandle<DynamicVertexBuffer> handle, int newBufferSize) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (newBufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(newBufferSize), newBufferSize, "Index capacity must be positive.");
		var data = _activeDynamicVertexBuffers[handle];
		if (data.IndexCapacity == newBufferSize) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		ThrowIfAnyActiveDynamicBufferLeases(handle);

		_indexBufferRefCounts.Remove(data.IndexBufferHandle);
		LocalFrameSynchronizationManager.QueueResourceDisposal(data.IndexBufferHandle, &DisposeIndexBuffer);
		var newIbHandle = AllocateDynamicIndexBuffer(newBufferSize);
		_indexBufferRefCounts.Add(newIbHandle, 1);

		var newIndices = data.Indices;
		if (data.Indices is { } oldIndices) {
			var replacement = _globals.HeapPool.Borrow<ushort>(newBufferSize);
			replacement.Span.Clear();
			oldIndices.Span[..Int32.Min(oldIndices.Span.Length, newBufferSize)].CopyTo(replacement.Span);
			oldIndices.Dispose();
			newIndices = replacement;
			UploadIndices(newIbHandle, replacement.Span, 0);
		}
		_activeDynamicVertexBuffers[handle] = data with { IndexBufferHandle = newIbHandle, IndexCapacity = newBufferSize, Indices = newIndices };
	}

	void UploadVertices(UIntPtr vertexBufferHandle, ReadOnlySpan<MeshVertex> vertices, int offset) {
		if (vertices.Length == 0) return;
		var holdingBuffer = _globals.CreateGpuHoldingBufferAndCopyData(vertices);
		UpdateVertexBuffer(vertexBufferHandle, holdingBuffer.BufferIdentity, (MeshVertex*) holdingBuffer.DataPtr, vertices.Length, offset).ThrowIfFailure();
	}
	void UploadIndices(UIntPtr indexBufferHandle, ReadOnlySpan<ushort> indices, int offset) {
		if (indices.Length == 0) return;
		var holdingBuffer = _globals.CreateGpuHoldingBufferAndCopyData(indices);
		UpdateIndexBufferUShort(indexBufferHandle, holdingBuffer.BufferIdentity, (ushort*) holdingBuffer.DataPtr, indices.Length, offset).ThrowIfFailure();
	}

	DynamicVertexBufferData GetMirroredBufferDataOrThrow(ResourceHandle<DynamicVertexBuffer> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var result = _activeDynamicVertexBuffers[handle];
		if (result.UsesImGuiVertices) {
			throw new InvalidOperationException(
				$"Can not borrow or recalculate bounding boxes for {HandleToInstance(handle)} because it was created as an ImGui vertex buffer. " +
				$"ImGui buffers do not retain a CPU-side copy of their data. This is a bug in TinyFFR."
			);
		}
		return result;
	}
	void ThrowIfAnyActiveDynamicBufferLeases(ResourceHandle<DynamicVertexBuffer> handle) {
		var name = _globals.GetResourceName(handle.Ident, DefaultDynamicVertexBufferName);
		_dynamicVertexLeaseTracker.ThrowIfAnyActiveRentals(handle, nameof(DynamicVertexBuffer), name);
		_dynamicIndexLeaseTracker.ThrowIfAnyActiveRentals(handle, nameof(DynamicVertexBuffer), name);
	}

	public ScopedReadOnlySpanLease<MeshVertex> BorrowVerticesSpanReadOnly(ResourceHandle<DynamicVertexBuffer> handle) {
		var data = GetMirroredBufferDataOrThrow(handle);
		return _dynamicVertexLeaseTracker.CreateScopedLeaseOrThrow(handle, (ReadOnlySpan<MeshVertex>) data.Vertices!.Value.Span);
	}
	public ScopedSpanLease<MeshVertex> BorrowVerticesSpan(ResourceHandle<DynamicVertexBuffer> handle, Range range, bool recalculateBoundingBoxOnLeaseDispose, bool overwriteChildMeshBoundingBoxes) {
		var data = GetMirroredBufferDataOrThrow(handle);
		return _dynamicVertexLeaseTracker.CreateScopedLeaseOrThrow(
			handle,
			data.Vertices!.Value.Span[range],
			new DynamicBufferLeaseData(range, recalculateBoundingBoxOnLeaseDispose, overwriteChildMeshBoundingBoxes)
		);
	}
	public ScopedReadOnlySpanLease<ushort> BorrowIndicesSpanReadOnly(ResourceHandle<DynamicVertexBuffer> handle) {
		var data = GetMirroredBufferDataOrThrow(handle);
		return _dynamicIndexLeaseTracker.CreateScopedLeaseOrThrow(handle, (ReadOnlySpan<ushort>) data.Indices!.Value.Span);
	}
	public ScopedSpanLease<ushort> BorrowIndicesSpan(ResourceHandle<DynamicVertexBuffer> handle, Range range, bool recalculateBoundingBoxOnLeaseDispose, bool overwriteChildMeshBoundingBoxes) {
		var data = GetMirroredBufferDataOrThrow(handle);
		return _dynamicIndexLeaseTracker.CreateScopedLeaseOrThrow(
			handle,
			data.Indices!.Value.Span[range],
			new DynamicBufferLeaseData(range, recalculateBoundingBoxOnLeaseDispose, overwriteChildMeshBoundingBoxes)
		);
	}

	static void HandleDynamicVertexLeaseDisposal(object? builder, ResourceHandle handle, DynamicBufferLeaseData leaseData, int _) {
		((LocalMeshBuilder) builder!).ExecuteDynamicVertexMutation((ResourceHandle<DynamicVertexBuffer>) handle, leaseData);
	}
	static void HandleDynamicIndexLeaseDisposal(object? builder, ResourceHandle handle, DynamicBufferLeaseData leaseData, int _) {
		((LocalMeshBuilder) builder!).ExecuteDynamicIndexMutation((ResourceHandle<DynamicVertexBuffer>) handle, leaseData);
	}
	void ExecuteDynamicVertexMutation(ResourceHandle<DynamicVertexBuffer> handle, DynamicBufferLeaseData leaseData) {
		var data = GetMirroredBufferDataOrThrow(handle);
		var mirror = data.Vertices!.Value.Span;
		var (startIndex, length) = leaseData.Range.GetOffsetAndLength(mirror.Length);
		UploadVertices(data.VertexBufferHandle, mirror.Slice(startIndex, length), startIndex);
		if (leaseData.RecalculateBoundingBox) RecalculateBoundingBox(handle, leaseData.OverwriteChildMeshBoundingBoxes);
	}
	void ExecuteDynamicIndexMutation(ResourceHandle<DynamicVertexBuffer> handle, DynamicBufferLeaseData leaseData) {
		var data = GetMirroredBufferDataOrThrow(handle);
		var mirror = data.Indices!.Value.Span;
		var (startIndex, length) = leaseData.Range.GetOffsetAndLength(mirror.Length);
		UploadIndices(data.IndexBufferHandle, mirror.Slice(startIndex, length), startIndex);
		if (leaseData.RecalculateBoundingBox) RecalculateBoundingBox(handle, leaseData.OverwriteChildMeshBoundingBoxes);
	}

	public void TriggerManualBoundingBoxRecalculation(ResourceHandle<DynamicVertexBuffer> handle, bool overwriteChildMeshBoundingBoxes) {
		RecalculateBoundingBox(handle, overwriteChildMeshBoundingBoxes);
	}
	void RecalculateBoundingBox(ResourceHandle<DynamicVertexBuffer> handle, bool overwriteChildMeshBoundingBoxes) {
		var data = GetMirroredBufferDataOrThrow(handle);
		SetBoundingBox(handle, CalculateFullBoundingBox(data.Vertices!.Value), overwriteChildMeshBoundingBoxes);
	}

	public void SetBoundingBox(ResourceHandle<DynamicVertexBuffer> handle, PositionedCuboid newBoundingBox, bool overwriteChildMeshBoundingBoxes) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_activeDynamicVertexBuffers[handle] = _activeDynamicVertexBuffers[handle] with { BoundingBox = newBoundingBox };
		if (!overwriteChildMeshBoundingBoxes) return;
		foreach (var kvp in _meshViewParents) {
			if (kvp.Value != handle) continue;
			ApplyBoundingBoxToMeshAndInstances(kvp.Key, newBoundingBox);
		}
	}
	public void SetBoundingBox(ResourceHandle<DynamicVertexBuffer> handle, Mesh mesh, PositionedCuboid newBoundingBox) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var meshHandle = mesh.Handle;
		if (!_meshViewParents.TryGetValue(meshHandle, out var parentHandle) || parentHandle != handle) {
			throw new ArgumentException($"Given {nameof(Mesh)} ({mesh}) was not created from {HandleToInstance(handle)}.", nameof(mesh));
		}
		ApplyBoundingBoxToMeshAndInstances(meshHandle, newBoundingBox);
	}
	void ApplyBoundingBoxToMeshAndInstances(ResourceHandle<Mesh> meshHandle, PositionedCuboid newBoundingBox) {
		if (!_activeMeshes.TryGetValue(meshHandle, out var meshData)) return;
		_activeMeshes[meshHandle] = meshData with { BoundingBox = newBoundingBox };
		foreach (var instance in _globals.DependencyTracker.GetDependentsOfGivenType<Mesh, ModelInstance, IModelInstanceImplProvider>(HandleToInstance(meshHandle))) {
			instance.SetNonTransformedBoundingBox(newBoundingBox);
		}
	}

	public void SetVerticesImGui(ResourceHandle<DynamicVertexBuffer> handle, ReadOnlySpan<MeshVertexImGui> vertices, int offset) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (vertices.Length == 0) return;
		var data = _activeDynamicVertexBuffers[handle];
		if (!data.UsesImGuiVertices) throw new InvalidOperationException($"{HandleToInstance(handle)} was not created as an ImGui vertex buffer. This is a bug in TinyFFR.");
		if (offset < 0 || offset + vertices.Length > data.VertexCapacity) {
			throw new ArgumentOutOfRangeException(nameof(vertices), vertices.Length, $"Writing {vertices.Length} vertices at this offset ({offset}) would exceed the buffer capacity of {data.VertexCapacity}.");
		}

		var holdingBuffer = _globals.CreateGpuHoldingBufferAndCopyData(vertices);
		UpdateVertexBufferImGui(data.VertexBufferHandle, holdingBuffer.BufferIdentity, (MeshVertexImGui*) holdingBuffer.DataPtr, vertices.Length, offset).ThrowIfFailure();
	}
	public void SetIndicesImGui(ResourceHandle<DynamicVertexBuffer> handle, ReadOnlySpan<ushort> indices, int offset) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (indices.Length == 0) return;
		var data = _activeDynamicVertexBuffers[handle];
		if (!data.UsesImGuiVertices) throw new InvalidOperationException($"{HandleToInstance(handle)} was not created as an ImGui vertex buffer. This is a bug in TinyFFR.");
		if (offset < 0 || offset + indices.Length > data.IndexCapacity) {
			throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Writing {indices.Length} indices at this offset would exceed the buffer capacity of {data.IndexCapacity}.");
		}

		UploadIndices(data.IndexBufferHandle, indices, offset);
	}

	public Mesh CreateMeshView(ResourceHandle<DynamicVertexBuffer> handle, Range indicesRange, PositionedCuboid? boundingBoxOverride) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var data = _activeDynamicVertexBuffers[handle];
		int offset, count;
		try {
			(offset, count) = indicesRange.GetOffsetAndLength(data.IndexCapacity);
			if (offset < 0) throw new ArgumentOutOfRangeException(nameof(indicesRange), indicesRange, "Index start must not be negative.");
			if (count < 0) throw new ArgumentOutOfRangeException(nameof(indicesRange), indicesRange, "Index count must not be negative.");
			if (offset + count > data.IndexCapacity) {
				throw new ArgumentOutOfRangeException(nameof(indicesRange), indicesRange, $"Index range [{offset}, {offset + count}) exceeds the index buffer capacity of {data.IndexCapacity}.");
			}
		}
		catch (ArgumentOutOfRangeException e) {
			throw new ArgumentOutOfRangeException($"Index range is invalid for the index buffer size of this {HandleToInstance(handle)} (size = {data.IndexCapacity}).", e);
		}

		if (boundingBoxOverride == null && data.UsesImGuiVertices) {
			throw new InvalidOperationException(
				$"A bounding box must be supplied explicitly when creating a {nameof(Mesh)} from {HandleToInstance(handle)} " +
				$"because it was created as an ImGui vertex buffer and therefore can not calculate one. This is a bug in TinyFFR."
			);
		}

		var viewHandle = (ResourceHandle<Mesh>) (++_prevHandleId);
		_vertexBufferRefCounts[data.VertexBufferHandle] += 1;
		_indexBufferRefCounts[data.IndexBufferHandle] += 1;
		_activeMeshes.Add(viewHandle, new(
			new MeshBufferData(data.VertexBufferHandle, data.IndexBufferHandle, offset, count, 0),
			boundingBoxOverride ?? data.BoundingBox
		));
		_meshViewParents.Add(viewHandle, handle);
		_globals.StoreResourceNameOrDefaultIfEmpty(viewHandle.Ident, default, DefaultMeshName);
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(viewHandle), HandleToInstance(handle));
		return HandleToInstance(viewHandle);
	}

	public string GetNameAsNewStringObject(ResourceHandle<DynamicVertexBuffer> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultDynamicVertexBufferName));
	}
	public int GetNameLength(ResourceHandle<DynamicVertexBuffer> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultDynamicVertexBufferName).Length;
	}
	public void CopyName(ResourceHandle<DynamicVertexBuffer> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultDynamicVertexBufferName, destinationBuffer);
	}

	public bool IsDynamicVertexBufferDisposed(ResourceHandle<DynamicVertexBuffer> handle) => _isDisposed || !_activeDynamicVertexBuffers.ContainsKey(handle);

	public void Dispose(ResourceHandle<DynamicVertexBuffer> handle) => Dispose(handle, removeFromMap: true);

	void Dispose(ResourceHandle<DynamicVertexBuffer> handle, bool removeFromMap) {
		if (IsDynamicVertexBufferDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		var data = _activeDynamicVertexBuffers[handle];
		if (_vertexBufferRefCounts.TryGetValue(data.VertexBufferHandle, out var vbRefCount)) {
			if (vbRefCount <= 1) {
				_vertexBufferRefCounts.Remove(data.VertexBufferHandle);
				LocalFrameSynchronizationManager.QueueResourceDisposal(data.VertexBufferHandle, &DisposeVertexBuffer);
			}
			else _vertexBufferRefCounts[data.VertexBufferHandle] = vbRefCount - 1;
		}
		if (_indexBufferRefCounts.TryGetValue(data.IndexBufferHandle, out var ibRefCount)) {
			if (ibRefCount <= 1) {
				_indexBufferRefCounts.Remove(data.IndexBufferHandle);
				LocalFrameSynchronizationManager.QueueResourceDisposal(data.IndexBufferHandle, &DisposeIndexBuffer);
			}
			else _indexBufferRefCounts[data.IndexBufferHandle] = ibRefCount - 1;
		}
		data.Vertices?.Dispose();
		data.Indices?.Dispose();
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromMap) _activeDynamicVertexBuffers.Remove(handle);
	}

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<DynamicVertexBuffer> handle) {
		ObjectDisposedException.ThrowIf(IsDynamicVertexBufferDisposed(handle), typeof(DynamicVertexBuffer));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	DynamicVertexBuffer HandleToInstance(ResourceHandle<DynamicVertexBuffer> h) => new(h, DynamicVertexBufferImplProvider);
	#endregion

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
	IndirectEnumerable<object, MeshAnimation> IResourceDirectory<MeshAnimation>.AllActiveInstances {
		get {
			static LocalMeshBuilder CastSelf(object self) => self as LocalMeshBuilder ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetCount(object self) {
				var builder = CastSelf(self);
				var count = 0;
				for (var i = 0; i < builder._activeMeshAnimationTables.Count; ++i) count += builder._activeMeshAnimationTables.GetPairAtIndex(i).Value.Count;
				return count;
			}
			static int GetVersion(object self) => CastSelf(self)._meshAnimDirectoryVersion;
			static MeshAnimation GetItem(object self, int index) {
				var builder = CastSelf(self);
				for (var i = 0; i < builder._activeMeshAnimationTables.Count; ++i) {
					var table = builder._activeMeshAnimationTables.GetPairAtIndex(i).Value;
					if (index < table.Count) return table.GetAnimationAtUnstableIndex(index);
					index -= table.Count;
				}
				throw new InvalidOperationException($"Index '{index}' out of range.");
			}

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
	public bool ResourceNameMatchIsMatching(MeshAnimation resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var handle = resource.GetHandleWithoutDisposeCheck();
		var resourceName = _globals.GetMandatoryResourceName(handle.Ident);
		return allowPartialMatch
			? resourceName.Contains(name, comparisonType)
			: resourceName.Equals(name, comparisonType);
	}
	IndirectEnumerable<object, MeshNode> IResourceDirectory<MeshNode>.AllActiveInstances {
		get {
			static LocalMeshBuilder CastSelf(object self) => self as LocalMeshBuilder ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetCount(object self) {
				var builder = CastSelf(self);
				var count = 0;
				for (var i = 0; i < builder._activeMeshAnimationTables.Count; ++i) count += builder._activeMeshAnimationTables.GetPairAtIndex(i).Value.GetNodeCount();
				return count;
			}
			static int GetVersion(object self) => CastSelf(self)._activeMeshAnimationTables.Version;
			static MeshNode GetItem(object self, int index) {
				var builder = CastSelf(self);
				for (var i = 0; i < builder._activeMeshAnimationTables.Count; ++i) {
					var table = builder._activeMeshAnimationTables.GetPairAtIndex(i).Value;
					var nodeCount = table.GetNodeCount();
					if (index < nodeCount) return table.GetNode(index);
					index -= nodeCount;
				}
				throw new InvalidOperationException($"Index '{index}' out of range.");
			}

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
	public bool ResourceNameMatchIsMatching(MeshNode resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var nameLen = resource.GetNameLength();
		using var nameBuffer = _globals.HeapPool.Borrow<char>(nameLen);
		resource.CopyName(nameBuffer.Span);
		return allowPartialMatch
			? nameBuffer.Span.Contains(name, comparisonType)
			: nameBuffer.Span.Equals(name, comparisonType);
	}
	IndirectEnumerable<object, DynamicVertexBuffer> IResourceDirectory<DynamicVertexBuffer>.AllActiveInstances {
		get {
			static LocalMeshBuilder CastSelf(object self) => self as LocalMeshBuilder ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetCount(object self) => CastSelf(self)._activeDynamicVertexBuffers.Count;
			static int GetVersion(object self) => CastSelf(self)._activeDynamicVertexBuffers.Version;
			static DynamicVertexBuffer GetItem(object self, int index) => CastSelf(self).HandleToInstance(CastSelf(self)._activeDynamicVertexBuffers.GetPairAtIndex(index).Key);

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
	public bool ResourceNameMatchIsMatching(DynamicVertexBuffer resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var handle = resource.GetHandleWithoutDisposeCheck();
		ThrowIfThisOrHandleIsDisposed(handle);
		return allowPartialMatch
			? _globals.GetResourceName(handle.Ident, DefaultDynamicVertexBufferName).Contains(name, comparisonType)
			: _globals.GetResourceName(handle.Ident, DefaultDynamicVertexBufferName).Equals(name, comparisonType);
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_vertex_buffer_primitive")]
	static extern InteropResult AllocateVertexBufferPrimitive(
		nuint bufferId,
		MeshVertexPrimitive* verticesPtr,
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_vertex_buffer_imgui")]
	static extern InteropResult AllocateVertexBufferImGui(
		nuint bufferId,
		MeshVertexImGui* verticesPtr,
		int numVertices,
		out UIntPtr outBufferHandle
	);
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "update_vertex_buffer")]
	public static extern InteropResult UpdateVertexBuffer(
		UIntPtr bufferHandle,
		nuint bufferId,
		MeshVertex* verticesPtr,
		int vertexCount,
		int startingIndex
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "update_vertex_buffer_imgui")]
	static extern InteropResult UpdateVertexBufferImGui(
		UIntPtr bufferHandle,
		nuint bufferId,
		MeshVertexImGui* verticesPtr,
		int numVertices,
		int startingIndex
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_index_buffer_ushort")]
	static extern InteropResult AllocateIndexBufferUShort(
		nuint bufferId,
		ushort* indicesPtr,
		int numIndices,
		out UIntPtr outBufferHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "update_index_buffer_ushort")]
	static extern InteropResult UpdateIndexBufferUShort(
		UIntPtr bufferHandle,
		nuint bufferId,
		ushort* indicesPtr,
		int numIndices,
		int startingIndex
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
		_mutableVertexLeaseTracker.ThrowIfAnyActiveRentals(handle, nameof(Mesh), _globals.GetResourceName(handle.Ident, DefaultMeshName));
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		_globals.Bakery.DiscardBakeryDataIfPresent(HandleToInstance(handle));
		
#pragma warning disable CA2000 // Compiler incorrectly assumes animTable is going out of scope here and warns me to invoke Dispose() on it
		if (_activeMeshAnimationTables.Remove(handle, out var animTable)) {
#pragma warning restore CA2000
			animTable.Recycle();
			_meshAnimationTablePool.Return(animTable);
		}
		
		if (_defaultMutableVerticesMap.Remove(handle, out var mutableVerts)) {
			mutableVerts.Dispose();
		}
		if (_activeMeshWireframeBufferData.Remove(handle, out var wireframeBufferData)) {
			LocalFrameSynchronizationManager.QueueResourceDisposal(wireframeBufferData.VertexBufferHandle, &DisposeVertexBuffer);
			LocalFrameSynchronizationManager.QueueResourceDisposal(wireframeBufferData.IndexBufferHandle, &DisposeIndexBuffer);
		}
		var meshData = _activeMeshes[handle];
		var bufferData = meshData.BufferData;
		var curVbRefCount = _vertexBufferRefCounts[bufferData.VertexBufferHandle];
		var curIbRefCount = _indexBufferRefCounts[bufferData.IndexBufferHandle];
		if (curVbRefCount <= 1) {
			_vertexBufferRefCounts.Remove(bufferData.VertexBufferHandle);
			LocalFrameSynchronizationManager.QueueResourceDisposal(bufferData.VertexBufferHandle, &DisposeVertexBuffer);
		}
		else {
			_vertexBufferRefCounts[bufferData.VertexBufferHandle] = curVbRefCount - 1;
		}
		if (curIbRefCount <= 1) {
			_indexBufferRefCounts.Remove(bufferData.IndexBufferHandle);
			LocalFrameSynchronizationManager.QueueResourceDisposal(bufferData.IndexBufferHandle, &DisposeIndexBuffer);
		}
		else {
			_indexBufferRefCounts[bufferData.IndexBufferHandle] = curIbRefCount - 1;
		}
		if (_meshViewParents.Remove(handle, out var parentBufferHandle)) {
			_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), HandleToInstance(parentBufferHandle));
		}
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromMap) _activeMeshes.Remove(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _activeDynamicVertexBuffers) Dispose(kvp.Key, removeFromMap: false);
			foreach (var kvp in _activeMeshes) Dispose(kvp.Key, removeFromMap: false);
			_mutableVertexLeaseTracker.Dispose();
			_dynamicVertexLeaseTracker.Dispose();
			_dynamicIndexLeaseTracker.Dispose();
			_defaultMutableVerticesMap.Dispose();
			_activeMeshWireframeBufferData.Dispose();
			_activeMeshes.Dispose();
			_activeMeshAnimationTables.Dispose();
			_activeDynamicVertexBuffers.Dispose();
			_meshViewParents.Dispose();

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