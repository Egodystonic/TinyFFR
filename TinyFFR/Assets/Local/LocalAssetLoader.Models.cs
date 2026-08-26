// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Assets.Local;

unsafe partial class LocalAssetLoader : IResourceDirectory<Model> {
	const int ResourceNameIndexSpaceMax = 20;
	const string MeshNameSuffix = " mesh ";
	
	const string DefaultModelName = "Unnamed Model";
	readonly WorkerJobSyncHelper<LocalAssetLoader, ModelLoadContext, ModelLoadConfig> _modelLoadWorkerSyncHelper;
	readonly ArrayPoolBackedMap<ResourceHandle<Model>, (Mesh Mesh, Material Material)> _loadedModels = new();
	nuint _prevModelHandle = 0;
	
	public Model CreateModel(Mesh mesh, Material material, ReadOnlySpan<char> name) {
		ThrowIfThisIsDisposed();
		++_prevModelHandle;
		var handle = (ResourceHandle<Model>) _prevModelHandle;
		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, name, DefaultModelName);
		_loadedModels.Add(_prevModelHandle, (mesh, material));
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), mesh);
		_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), material);
		return HandleToInstance(handle);
	}
	
	sealed class ModelLoadSubMeshData : IDisposable {
		public PooledHeapMemory<byte>? VertexData { get; set; } = null;
		public PooledHeapMemory<byte>? TriangleData { get; set; } = null;
		public PooledHeapMemory<byte>? InternalNodeData { get; set; } = null;
		public PooledHeapMemory<SkeletalAnimationNode>? SkeletalNodes { get; set; } = null;
		public MeshSkeletalGatherBuffers SkeletalDataRegistry { get; } = new();
		public ModelLoadMaterialTextureRegistry TextureRegistry { get; } = new();
		public GatheredMaterialData? GatheredMaterial { get; set; } = null;
		public int MaterialAssetIndex { get; set; } = -1;
		public int VertexCount { get; set; } = 0;
		public int TriangleCount { get; set; } = 0;
		public int NodeCount { get; set; } = 0;
		public int BoneCount { get; set; } = 0;
		public bool IsSkeletal { get; set; } = false;
		public bool AllowsPerInstanceVertexMutation { get; set; } = false;
		public bool GenerateWireframeData { get; set; } = false;
		public PositionedCuboid BoundingBox { get; set; } = default;
		public Vect OriginTranslation { get; set; } = Vect.Zero;
		public float LinearRescalingFactor { get; set; } = 1f;
		public NameSlice Name { get; set; } = default;

		public void DisposeBuffersAndReset() {
			SkeletalDataRegistry.DisposeUntransferredBuffersAndReset();
			TextureRegistry.DisposeBuffersAndReset();
			VertexData?.Dispose();
			VertexData = null;
			TriangleData?.Dispose();
			TriangleData = null;
			GatheredMaterial = null;
			MaterialAssetIndex = -1;
			InternalNodeData?.Dispose();
			InternalNodeData = null;
			SkeletalNodes?.Dispose();
			SkeletalNodes = null;
			VertexCount = 0;
			TriangleCount = 0;
			NodeCount = 0;
			BoneCount = 0;
			IsSkeletal = false;
			AllowsPerInstanceVertexMutation = false;
			GenerateWireframeData = false;
			BoundingBox = default;
			OriginTranslation = Vect.Zero;
			LinearRescalingFactor = 1f;
			Name = default;
		}

		public void Dispose() {
			DisposeBuffersAndReset();
			SkeletalDataRegistry.Dispose();
		}
	}

	sealed class ModelLoadContext : WorkerJobSyncHelper<LocalAssetLoader, ModelLoadContext, ModelLoadConfig>.WorkerJobSyncHelperContext {
		// Primary thread owned
		public PooledHeapMemory<char>? FilePath { get; set; } = null;
		public int TotalResourceCountHint { get; set; } = 0;

		// Worker thread owned
		public InteropStringBuffer? NameBuffer { get; set; } = null;
		public UIntPtr AssetHandle { get; set; } = UIntPtr.Zero;
		public ModelLoadSubMeshData CurrentSubMeshData { get; } = new();
		public ArrayPoolBackedMap<int, Material> AssetIndexToMaterialMap { get; } = new();

		// Handed across each primary thread hop
		public ResourceGroup? Group { get; set; } = null;

		public override void TearDown() {
			if (AssetHandle != UIntPtr.Zero) {
				UnloadAssetFileFromMemory(AssetHandle).ThrowIfFailure();
				AssetHandle = UIntPtr.Zero;
			}
			if (Group is { IsSealed: false } incompleteGroup) incompleteGroup.Dispose(disposeContainedResources: true);
			Group = null;
			CurrentSubMeshData.DisposeBuffersAndReset();
			AssetIndexToMaterialMap.Clear();
			NameBuffer?.Dispose();
			NameBuffer = null;
			if (HeapPoolSerializedConfig is { } config) {
				ModelLoadConfig.DisposeAllocatedHeapStorage(config.Span);
				config.Dispose();
				HeapPoolSerializedConfig = null;
			}
			FilePath?.Dispose();
			FilePath = null;
			TotalResourceCountHint = 0;
			HeapPool = null!;
			Self = null!;
		}
	}

	public ResourceGroup LoadAll(ReadOnlySpan<char> filePath, in ModelCreationConfig config, in ModelReadConfig readConfig) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		readConfig.ThrowIfInvalid();

		var contextWrapper = _modelLoadWorkerSyncHelper.CreateContextWrapper();
		contextWrapper.Context.FilePath = _globals.HeapPool.BorrowAndCopy(filePath);
		contextWrapper.Context.SetName(config.Name.IsEmpty ? Path.GetFileName(filePath) : config.Name);

		return contextWrapper.DispatchResourceReturningSynchronousOperation(&LoadAllCore, new ModelLoadConfig { CreationConfig = config, ReadConfig = readConfig });
	}

	public TinyFfrAsyncOperation<ResourceGroup> LoadAllAsync(ReadOnlySpan<char> filePath, in ModelCreationConfig config, in ModelReadConfig readConfig) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		readConfig.ThrowIfInvalid();

		var contextWrapper = _modelLoadWorkerSyncHelper.CreateContextWrapper();
		contextWrapper.Context.FilePath = _globals.HeapPool.BorrowAndCopy(filePath);
		contextWrapper.Context.SetName(config.Name.IsEmpty ? Path.GetFileName(filePath) : config.Name);

		return contextWrapper.DispatchResourceReturningAsynchronousOperation(&LoadAllCore, new ModelLoadConfig { CreationConfig = config, ReadConfig = readConfig });
	}

	static ResourceGroup LoadAllCore(ModelLoadContext context, in ModelLoadConfig config) {
		if (context.FilePath is not { } filePath) {
			throw new InvalidOperationException("No file path set in context (this is a bug in TinyFFR).");
		}

		var self = context.Self;
		var creationConfig = config.CreationConfig;
		var readConfig = config.ReadConfig;

		try {
			var pathBuffer = self.AssetFilePathBuffer;
			pathBuffer.ConvertFromUtf16(filePath.Span);

			LoadAssetFileInToMemory(
				in pathBuffer.AsRef,
				readConfig.MeshConfig.FixCommonExportErrors,
				readConfig.MeshConfig.OptimizeForGpu,
				out var assetHandle
			).ThrowIfFailure();
			context.AssetHandle = assetHandle;

			pathBuffer.ConvertFromUtf16(Path.GetDirectoryName(filePath.Span));

			GetLoadedAssetMeshCount(assetHandle, out var meshCount).ThrowIfFailure();
			GetLoadedAssetMaterialCount(assetHandle, out var materialCount).ThrowIfFailure();
			GetLoadedAssetTextureCount(assetHandle, out var textureCount).ThrowIfFailure();
			context.TotalResourceCountHint = meshCount + materialCount + textureCount;

			var resourceGroupName = context.Name;
			Span<char> meshNameBuffer = stackalloc char[SpanUtils.GetConcatenatedLength(resourceGroupName, MeshNameSuffix) + ResourceNameIndexSpaceMax];
			SpanUtils.Concatenate(meshNameBuffer, resourceGroupName, MeshNameSuffix);
			var meshNameWriteStartIndex = SpanUtils.GetConcatenatedLength(resourceGroupName, MeshNameSuffix);

			for (var i = 0; i < meshCount; ++i) {
				_ = i.TryFormat(meshNameBuffer[meshNameWriteStartIndex..], out var idxCharsCount, provider: CultureInfo.InvariantCulture);
				var meshName = meshNameBuffer[..(meshNameWriteStartIndex + idxCharsCount)];

				GetLoadedAssetMeshSkeletalBoneCount(assetHandle, i, out var boneCount).ThrowIfFailure();
				var loadSkeletalAnimationData = boneCount > 0 && readConfig.MeshConfig.LoadSkeletalAnimationDataIfPresent;
				if (loadSkeletalAnimationData && boneCount > IMeshBuilder.MaxSkeletalBoneCount) {
					Console.WriteLine($"Can not load skeletal animation data for file '{filePath.Span}' (sub-mesh {i}) as its bone count ({boneCount}) is higher than the maximum TinyFFR supports ({IMeshBuilder.MaxSkeletalBoneCount}).");
					loadSkeletalAnimationData = false;
				}

				self.GatherSubMeshOnWorker(context, assetHandle, i, in readConfig, creationConfig.MeshConfig with { Name = meshName }, loadSkeletalAnimationData);

				GetLoadedAssetMeshMaterialIndex(assetHandle, i, out var matIndex).ThrowIfFailure();
				if (matIndex < 0 || matIndex >= materialCount) throw new InvalidOperationException($"Mesh at index '{i}' references material at index '{matIndex}' but asset only contains {materialCount} materials.");

				context.CurrentSubMeshData.MaterialAssetIndex = matIndex;
				var isNewMaterial = !context.AssetIndexToMaterialMap.ContainsKey(matIndex);
				if (isNewMaterial) {
					context.CurrentSubMeshData.GatheredMaterial = self.GatherAssetMaterial(
						assetHandle,
						matIndex,
						resourceGroupName,
						creationConfig.TextureConfig,
						in readConfig,
						in pathBuffer.AsRef,
						context.CurrentSubMeshData.TextureRegistry,
						context.HeapPool
					);
				}

				context.DispatchOnPrimaryAndWait(&CreateSubMeshResourcesOnPrimary);

				context.CurrentSubMeshData.DisposeBuffersAndReset();
			}

			UnloadAssetFileFromMemory(context.AssetHandle).ThrowIfFailure();
			context.AssetHandle = UIntPtr.Zero;

			return context.GenerateResourceOnPrimaryAndWait(&CompleteModelLoad);
		}
		catch (Exception e) {
			if (!File.Exists(new String(filePath.Span))) throw new InvalidOperationException($"File '{filePath.Span}' does not exist.", e);
			else throw;
		}
	}

	static ResourceGroup GetOrCreateGroupOnPrimary(ModelLoadContext context) {
		if (context.Group is { } existingGroup) return existingGroup;
		var newGroup = context.Self._globals.ResourceGroupProvider.CreateGroup(
			disposeContainedResourcesWhenDisposed: true,
			name: context.Name,
			context.TotalResourceCountHint > 0 ? context.TotalResourceCountHint : LocalResourceGroupImplProvider.DefaultInitialCapacity
		);
		context.Group = newGroup;
		return newGroup;
	}

	static void CreateSubMeshResourcesOnPrimary(ModelLoadContext context) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		var self = context.Self;
		var group = GetOrCreateGroupOnPrimary(context);

		var mesh = self.CreateSubMeshOnPrimary(context.CurrentSubMeshData);
		group.Add(mesh);

		Material material;
		if (context.CurrentSubMeshData.GatheredMaterial is { } gatheredMaterial) {
			material = self.MaterializeAssetMaterial(gatheredMaterial, context.CurrentSubMeshData.TextureRegistry, group);
			group.Add(material);
			context.AssetIndexToMaterialMap[context.CurrentSubMeshData.MaterialAssetIndex] = material;
		}
		else {
			material = context.AssetIndexToMaterialMap[context.CurrentSubMeshData.MaterialAssetIndex];
		}

		group.Add(self.CreateModel(mesh, material, default));
	}

	static ResourceGroup CompleteModelLoad(ModelLoadContext context) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		var group = GetOrCreateGroupOnPrimary(context);
		group.Seal();
		return group;
	}

	void GatherSubMeshOnWorker(ModelLoadContext context, UIntPtr assetHandle, int subMeshIndex, in ModelReadConfig readConfig, in MeshCreationConfig meshConfig, bool loadSkeletalAnimationData) {
		var buffers = context.CurrentSubMeshData;
		buffers.IsSkeletal = loadSkeletalAnimationData;
		buffers.OriginTranslation = meshConfig.OriginTranslation;
		buffers.LinearRescalingFactor = meshConfig.LinearRescalingFactor;
		buffers.Name = buffers.SkeletalDataRegistry.AppendName(meshConfig.Name);

		if (loadSkeletalAnimationData) {
			GatherSubMeshVertexDataOnWorker<MeshVertexSkeletal>(assetHandle, subMeshIndex, readConfig.MeshConfig.CorrectFlippedOrientation, in meshConfig, buffers, context.HeapPool);
			GatherSubMeshSkeletalDataOnWorker(context, assetHandle, subMeshIndex, in readConfig);
		}
		else {
			GatherSubMeshVertexDataOnWorker<MeshVertex>(assetHandle, subMeshIndex, readConfig.MeshConfig.CorrectFlippedOrientation, in meshConfig, buffers, context.HeapPool);
		}
	}

	static void GatherSubMeshVertexDataOnWorker<TVertex>(UIntPtr assetHandle, int subMeshIndex, bool correctFlippedOrientation, in MeshCreationConfig meshConfig, ModelLoadSubMeshData buffers, ThreadSafeHeapPoolWrapper heapPool) where TVertex : unmanaged, IMeshVertex {
		GetLoadedAssetMeshVertexCount(assetHandle, subMeshIndex, out var vertexCount).ThrowIfFailure();
		GetLoadedAssetMeshTriangleCount(assetHandle, subMeshIndex, out var triangleCount).ThrowIfFailure();
		ThrowIfAssetBufferSizeExceedsMaximum((long) vertexCount * sizeof(TVertex), $"mesh vertex data ({vertexCount} vertices)");
		ThrowIfAssetBufferSizeExceedsMaximum((long) triangleCount * sizeof(VertexTriangle), $"mesh triangle data ({triangleCount} triangles)");

		var vertexData = heapPool.Borrow<byte>(vertexCount * sizeof(TVertex));
		buffers.VertexData = vertexData;
		var triangleData = heapPool.Borrow<byte>(triangleCount * sizeof(VertexTriangle));
		buffers.TriangleData = triangleData;
		buffers.VertexCount = vertexCount;
		buffers.TriangleCount = triangleCount;
		buffers.AllowsPerInstanceVertexMutation = meshConfig.AllowsPerInstanceVertexMutation;
		buffers.GenerateWireframeData = LocalMeshBuilder.GetShouldGenerateWireframeData<TVertex>(in meshConfig);

		fixed (byte* vertexBufferPtr = vertexData.Span)
		fixed (byte* triangleBufferPtr = triangleData.Span) {
			var vertexPtr = (TVertex*) vertexBufferPtr;
			var trianglePtr = (VertexTriangle*) triangleBufferPtr;

			CopySubMeshDataFromAsset(assetHandle, correctFlippedOrientation, subMeshIndex, vertexPtr, vertexCount, trianglePtr, triangleCount);

			var vertexSpan = new Span<TVertex>(vertexPtr, vertexCount);
			var triangleSpan = new Span<VertexTriangle>(trianglePtr, triangleCount);

			LocalMeshBuilder.ValidateMeshData<TVertex>(vertexSpan, triangleSpan, in meshConfig);
			if (meshConfig.FlipTriangles) LocalMeshBuilder.FlipTriangleWindings(triangleSpan);
			LocalMeshBuilder.ApplyVertexTransforms(vertexSpan, in meshConfig);
			buffers.BoundingBox = LocalMeshBuilder.CalculateMeshBoundingBox<TVertex>(vertexSpan, in meshConfig);
		}
	}

	static void GatherSubMeshSkeletalDataOnWorker(ModelLoadContext context, UIntPtr assetHandle, int subMeshIndex, in ModelReadConfig readConfig) {
		var buffers = context.CurrentSubMeshData;
		GetLoadedAssetMeshSkeletalNodeCount(assetHandle, subMeshIndex, out var nodeCount).ThrowIfFailure();
		ThrowIfAssetBufferSizeExceedsMaximum((long) nodeCount * sizeof(NodeHandle), $"mesh skeletal node data ({nodeCount} nodes)");

		var internalNodeData = context.HeapPool.Borrow<byte>(nodeCount * sizeof(NodeHandle));
		buffers.InternalNodeData = internalNodeData;
		var translatedNodeBuffer = context.HeapPool.Borrow<SkeletalAnimationNode>(nodeCount);
		buffers.SkeletalNodes = translatedNodeBuffer;
		buffers.NodeCount = nodeCount;

		InteropStringBuffer nameBuffer;
		if (context.NameBuffer is { } existingNameBuffer) {
			nameBuffer = existingNameBuffer;
		}
		else {
			nameBuffer = new InteropStringBuffer(context.Self._maxAnimationAndNodeNameLengthChars, addOneForNullTerminator: true);
			context.NameBuffer = nameBuffer;
		}

		fixed (byte* internalNodeBufferPtr = internalNodeData.Span) {
			var nodeHandles = (NodeHandle*) internalNodeBufferPtr;
			GenerateLoadedAssetMeshSkeletalNodeFlatBuffer(assetHandle, subMeshIndex, nodeHandles, nodeCount).ThrowIfFailure();

			var maxNodeNameLength = 0;
			for (var n = 0; n < nodeCount; ++n) {
				GetLoadedAssetMeshSkeletalNode(
					nodeHandles,
					nodeCount,
					n,
					out var inverseBindPoseMatrix,
					out var defaultTransformMatrix,
					out var parentNodeIndex,
					out var boneIndex,
					out var nameLength
				).ThrowIfFailure();

				translatedNodeBuffer.Span[n] = new(
					defaultTransformMatrix,
					inverseBindPoseMatrix,
					parentNodeIndex >= 0 ? parentNodeIndex : null,
					boneIndex >= 0 ? boneIndex : null
				);
				maxNodeNameLength = Int32.Max(nameLength, maxNodeNameLength);
			}

			buffers.BoneCount = LocalMeshBuilder.CalculateAndValidateBoneCount(translatedNodeBuffer.Span);

			GatherMeshAnimations(assetHandle, nodeHandles, nodeCount, readConfig.MeshConfig.AnimationTicksPerSecondOverride, nameBuffer, context.HeapPool, buffers.SkeletalDataRegistry);
			GatherNodeNames(new ReadOnlySpan<NodeHandle>(nodeHandles, nodeCount), maxNodeNameLength, nameBuffer, context.HeapPool, buffers.SkeletalDataRegistry);
		}
	}

	Mesh CreateSubMeshOnPrimary(ModelLoadSubMeshData buffers) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		if (buffers.VertexData is not { } vertexData || buffers.TriangleData is not { } triangleData) {
			throw new InvalidOperationException("Mesh vertex and/or triangle data was null (this is a bug in TinyFFR).");
		}

		var name = buffers.SkeletalDataRegistry.GetName(buffers.Name);
		var triangles = MemoryMarshal.Cast<byte, VertexTriangle>(triangleData.Span)[..buffers.TriangleCount];

		if (!buffers.IsSkeletal) {
			return _meshBuilder.CreateMeshFromPreValidatedAndTransformedData(
				MemoryMarshal.Cast<byte, MeshVertex>(vertexData.Span)[..buffers.VertexCount],
				triangles,
				buffers.BoundingBox,
				buffers.AllowsPerInstanceVertexMutation,
				buffers.GenerateWireframeData,
				name,
				0
			);
		}

		if (buffers.SkeletalNodes is not { } skeletalNodes) {
			throw new InvalidOperationException("Skeletal node data was null (this is a bug in TinyFFR).");
		}

		var boneCount = buffers.BoneCount;
		var mesh = _meshBuilder.CreateMeshFromPreValidatedAndTransformedData(
			MemoryMarshal.Cast<byte, MeshVertexSkeletal>(vertexData.Span)[..buffers.VertexCount],
			triangles,
			buffers.BoundingBox,
			buffers.AllowsPerInstanceVertexMutation,
			buffers.GenerateWireframeData,
			name,
			boneCount == 0 ? 1 : boneCount
		);

		if (boneCount == 0) {
			var defaultNode = new SkeletalAnimationNode(Matrix4x4.Identity, Matrix4x4.Identity, null, 0);
			_meshBuilder.AttachSkeletonToMesh(mesh, 1, new ReadOnlySpan<SkeletalAnimationNode>(in defaultNode), buffers.OriginTranslation, buffers.LinearRescalingFactor);
		}
		else {
			_meshBuilder.AttachSkeletonToMesh(mesh, boneCount, skeletalNodes.Span[..buffers.NodeCount], buffers.OriginTranslation, buffers.LinearRescalingFactor);
		}

		AttachGatheredMeshAnimations(_meshBuilder, mesh, buffers.SkeletalDataRegistry);
		ApplyGatheredNodeNames(_meshBuilder, mesh, buffers.SkeletalDataRegistry);
		return mesh;
	}

	public Mesh GetMesh(ResourceHandle<Model> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedModels[handle].Mesh;
	}
	public Material GetMaterial(ResourceHandle<Model> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedModels[handle].Material;
	}

	public string GetNameAsNewStringObject(ResourceHandle<Model> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultBackdropTextureName));
	}
	public int GetNameLength(ResourceHandle<Model> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultBackdropTextureName).Length;
	}
	public void CopyName(ResourceHandle<Model> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultBackdropTextureName, destinationBuffer);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Model HandleToInstance(ResourceHandle<Model> h) => new(h, this);

	#region Resource Directory
	public IndirectEnumerable<object, Model> AllActiveInstances {
		get {
			static LocalAssetLoader CastSelf(object self) => self as LocalAssetLoader ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetCount(object self) => CastSelf(self)._loadedModels.Count;
			static int GetVersion(object self) => CastSelf(self)._loadedModels.Version;
			static Model GetItem(object self, int index) => CastSelf(self).HandleToInstance(CastSelf(self)._loadedModels.GetPairAtIndex(index).Key);

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
	public bool ResourceNameMatchIsMatching(Model resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var handle = resource.GetHandleWithoutDisposeCheck();
		ThrowIfThisOrHandleIsDisposed(handle);
		return allowPartialMatch
			? _globals.GetResourceName(handle.Ident, DefaultModelName).Contains(name, comparisonType)
			: _globals.GetResourceName(handle.Ident, DefaultModelName).Equals(name, comparisonType);
	}
	#endregion

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "load_asset_file_in_to_memory")]
	static extern InteropResult LoadAssetFileInToMemory(
		ref readonly byte utf8FileNameBufferPtr,
		InteropBool fixCommonImporterErrors,
		InteropBool optimize,
		out UIntPtr outAssetHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_material_count")]
	static extern InteropResult GetLoadedAssetMaterialCount(
		UIntPtr assetHandle,
		out int outMaterialCount
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_texture_count")]
	static extern InteropResult GetLoadedAssetTextureCount(
		UIntPtr assetHandle,
		out int outTextureCount
	);
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_material_index")]
	static extern InteropResult GetLoadedAssetMeshMaterialIndex(
		UIntPtr assetHandle,
		int meshIndex,
		out int outMaterialIndex
	);
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_texture_size")]
	static extern InteropResult GetLoadedAssetTextureSize(
		UIntPtr assetHandle,
		int textureIndex,
		ref readonly byte utf8AssetRootDirPathBufferPtr,
		out int outWidth,
		out int outHeight
	);
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_texture_data")]
	static extern InteropResult GetLoadedAssetTextureData(
		UIntPtr assetHandle,
		int textureIndex,
		ref readonly byte utf8AssetRootDirPathBufferPtr,
		void* dataBufferPtr,
		int bufferLengthBytes,
		out int outWidth,
		out int outHeight
	);
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_texture_path_len")]
	static extern InteropResult GetLoadedAssetTextureExternalPathLength(
		UIntPtr assetHandle,
		int materialIndex,
		int textureIndex,
		ref readonly byte utf8AssetRootDirPathBufferPtr,
		out int stringLengthCharsWithoutNullTerminator
	);
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_texture_path")]
	static extern InteropResult GetLoadedAssetTextureExternalPath(
		UIntPtr assetHandle,
		int materialIndex,
		int textureIndex,
		ref readonly byte utf8AssetRootDirPathBufferPtr,
		byte* stringBufferPtr,
		int stringBufferLengthBytes
	);
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_material_data")]
	static extern InteropResult GetLoadedAssetMaterialData(
		UIntPtr assetHandle,
		int materialIndex,
		AssetMaterialParamGroup* paramGroupPtr,
		out int outAlphaFormat,
		out float outRefractionThickness
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "unload_asset_file_from_memory")]
	static extern InteropResult UnloadAssetFileFromMemory(
		UIntPtr assetHandle
	);
	#endregion
	
	#region Disposal
	public bool IsDisposed(ResourceHandle<Model> handle) => _isDisposed || !_loadedModels.ContainsKey(handle);

	public void Dispose(ResourceHandle<Model> handle) => Dispose(handle, removeFromCollection: true);
	void Dispose(ResourceHandle<Model> handle, bool removeFromCollection) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		_globals.DependencyTracker.DeregisterAllDependencies(HandleToInstance(handle));
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromCollection) _loadedModels.Remove(handle);
	}
	
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Model> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Model));
	#endregion
}