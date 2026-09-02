// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;
using static Egodystonic.TinyFFR.Assets.Baking.BakedResourceSchemata;

namespace Egodystonic.TinyFFR.Assets.Local;

unsafe partial class LocalAssetLoader {
	const int MaxNameLengthForStackAlloc = 2048;

	[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 3 * 8)]
	readonly struct NodeHandle {
		[FieldOffset(0)] readonly UIntPtr _node;
		[FieldOffset(8)] readonly UIntPtr _bone;
		[FieldOffset(16)] readonly int _boneIndex;
		[FieldOffset(20)] readonly int _;
	}

	readonly record struct NameSlice(int StartIndex, int Length);

	readonly record struct GatheredAnimation(
		PooledHeapMemory<SkeletalAnimationScalingKeyframe> ScalingKeyframes,
		PooledHeapMemory<SkeletalAnimationRotationKeyframe> RotationKeyframes,
		PooledHeapMemory<SkeletalAnimationTranslationKeyframe> TranslationKeyframes,
		PooledHeapMemory<SkeletalAnimationNodeMutationDescriptor> Mutations,
		float DurationSeconds,
		NameSlice Name
	);

	sealed class MeshSkeletalGatherBuffers : IDisposable {
		public ArrayPoolBackedVector<GatheredAnimation> Animations { get; } = new();
		public ArrayPoolBackedVector<char> NameChars { get; } = new();
		public ArrayPoolBackedVector<NameSlice> NodeNameSlices { get; } = new();
		public int NumAnimationsWithTransferredOwnership { get; set; } = 0;

		public NameSlice AppendName(ReadOnlySpan<char> name) {
			var startIndex = NameChars.Count;
			for (var i = 0; i < name.Length; ++i) NameChars.Add(name[i]);
			return new NameSlice(startIndex, name.Length);
		}

		public ReadOnlySpan<char> GetName(NameSlice slice) => NameChars.AsSpan.Slice(slice.StartIndex, slice.Length);

		public void DisposeUntransferredBuffersAndReset() {
			for (var i = NumAnimationsWithTransferredOwnership; i < Animations.Count; ++i) {
				var animation = Animations[i];
				animation.ScalingKeyframes.Dispose();
				animation.RotationKeyframes.Dispose();
				animation.TranslationKeyframes.Dispose();
				animation.Mutations.Dispose();
			}
			NumAnimationsWithTransferredOwnership = 0;
			Animations.Clear();
			NameChars.ClearWithoutZeroingMemory();
			NodeNameSlices.ClearWithoutZeroingMemory();
		}

		public void Dispose() {
			DisposeUntransferredBuffersAndReset();
			Animations.Dispose();
			NameChars.Dispose();
			NodeNameSlices.Dispose();
		}
	}

	readonly LocalMeshBuilder _meshBuilder;
	readonly int _maxAnimationAndNodeNameLengthChars;
	readonly WorkerJobSyncHelper<LocalAssetLoader, MeshLoadContext, MeshLoadConfig> _meshLoadWorkerSyncHelper;

	#region Creation
	sealed class MeshLoadContext : WorkerJobSyncHelper<LocalAssetLoader, MeshLoadContext, MeshLoadConfig>.WorkerJobSyncHelperContext {
		// Primary thread owned
		public PooledHeapMemory<char>? FilePath { get; set; } = null;

		// Worker thread owned
		public InteropStringBuffer? NameBuffer { get; set; } = null;
		public UIntPtr AssetHandle { get; set; } = UIntPtr.Zero;
		public PooledHeapMemory<byte>? VertexData { get; set; } = null;
		public PooledHeapMemory<byte>? TriangleData { get; set; } = null;
		public PooledHeapMemory<byte>? InternalNodeData { get; set; } = null;
		public PooledHeapMemory<SkeletalAnimationNode>? SkeletalNodes { get; set; } = null;
		public MeshSkeletalGatherBuffers GatherBuffers { get; } = new();
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

		public override void TearDown() {
			if (AssetHandle != UIntPtr.Zero) {
				UnloadAssetFileFromMemory(AssetHandle).ThrowIfFailure();
				AssetHandle = UIntPtr.Zero;
			}
			GatherBuffers.DisposeUntransferredBuffersAndReset();
			NameBuffer?.Dispose();
			NameBuffer = null;
			VertexData?.Dispose();
			VertexData = null;
			TriangleData?.Dispose();
			TriangleData = null;
			InternalNodeData?.Dispose();
			InternalNodeData = null;
			SkeletalNodes?.Dispose();
			SkeletalNodes = null;
			FilePath?.Dispose();
			FilePath = null;
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
			HeapPool = null!;
			Self = null!;
		}

		public override void Dispose() {
			GatherBuffers.Dispose();
		}
	}

	public Mesh LoadMesh(ReadOnlySpan<char> filePath, in MeshCreationConfig config, in MeshReadConfig readConfig) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();
		config.ThrowIfInvalid();

		var contextWrapper = _meshLoadWorkerSyncHelper.CreateContextWrapper();
		contextWrapper.Context.FilePath = _globals.HeapPool.BorrowAndCopy(filePath);
		contextWrapper.Context.SetName(config.Name);

		return contextWrapper.DispatchResourceReturningSynchronousOperation(&LoadMeshCore, new MeshLoadConfig { CreationConfig = config, ReadConfig = readConfig });
	}

	public TinyFfrAsyncOperation<Mesh> LoadMeshAsync(ReadOnlySpan<char> filePath, in MeshCreationConfig config, in MeshReadConfig readConfig) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();
		config.ThrowIfInvalid();

		var contextWrapper = _meshLoadWorkerSyncHelper.CreateContextWrapper();
		contextWrapper.Context.FilePath = _globals.HeapPool.BorrowAndCopy(filePath);
		contextWrapper.Context.SetName(config.Name);

		return contextWrapper.DispatchResourceReturningAsynchronousOperation(&LoadMeshCore, new MeshLoadConfig { CreationConfig = config, ReadConfig = readConfig });
	}

	static Mesh LoadMeshCore(MeshLoadContext context, in MeshLoadConfig config) {
		if (context.FilePath is not { } filePath) {
			throw new InvalidOperationException("No file path set in context (this is a bug in TinyFFR).");
		}

		var readConfig = config.ReadConfig;
		var creationConfig = config.CreationConfig;

		try {
			var assetHandle = OpenAssetFileOnWorker(context, filePath.Span, in readConfig);

			var metadata = GetAmalgamatedMeshMetadataFromOpenedFile(assetHandle);
			if (metadata.SubMeshCount <= 0) throw new ArgumentException($"Given file '{filePath.Span}' does not contain any mesh data.");

			var loadSkeletalAnimationData = readConfig.LoadSkeletalAnimationDataIfPresent;
			var skeletalSubMeshIndex = readConfig.SubMeshIndex ?? 0;
			if (loadSkeletalAnimationData) {
				if (metadata.SubMeshCount != 1 && readConfig.SubMeshIndex == null) {
					Console.WriteLine($"Can not load skeletal animation data for file '{filePath.Span}' as it contains multiple sub-meshes and no {nameof(MeshReadConfig.SubMeshIndex)} was given " +
									  $"(TinyFFR can not currently amalgamate multi-mesh animations in to a single object; use {nameof(LoadAll)}(...) instead).");
					loadSkeletalAnimationData = false;
				}
			}
			if (loadSkeletalAnimationData) {
				GetLoadedAssetMeshSkeletalBoneCount(assetHandle, skeletalSubMeshIndex, out var boneCount).ThrowIfFailure();
				switch (boneCount) {
					case <= 0:
						loadSkeletalAnimationData = false;
						break;
					case > IMeshBuilder.MaxSkeletalBoneCount:
						Console.WriteLine($"Can not load skeletal animation data for file '{filePath.Span}' as its bone count ({boneCount}) is higher than the maximum TinyFFR supports ({IMeshBuilder.MaxSkeletalBoneCount}).");
						loadSkeletalAnimationData = false;
						break;
				}
			}

			context.IsSkeletal = loadSkeletalAnimationData;
			context.AllowsPerInstanceVertexMutation = creationConfig.AllowsPerInstanceVertexMutation;
			context.OriginTranslation = creationConfig.OriginTranslation;
			context.LinearRescalingFactor = creationConfig.LinearRescalingFactor;

			if (loadSkeletalAnimationData) {
				context.GenerateWireframeData = LocalMeshBuilder.GetShouldGenerateWireframeData<MeshVertexSkeletal>(in creationConfig);
				GatherMeshDataOnWorker<MeshVertexSkeletal>(context, assetHandle, metadata, in readConfig, in creationConfig);
				GatherSkeletalDataOnWorker(context, assetHandle, skeletalSubMeshIndex, in readConfig);
			}
			else {
				context.GenerateWireframeData = LocalMeshBuilder.GetShouldGenerateWireframeData<MeshVertex>(in creationConfig);
				GatherMeshDataOnWorker<MeshVertex>(context, assetHandle, metadata, in readConfig, in creationConfig);
			}

			UnloadAssetFileFromMemory(context.AssetHandle).ThrowIfFailure();
			context.AssetHandle = UIntPtr.Zero;

			return context.GenerateResourceOnPrimaryAndWait(&CompleteMeshLoad);
		}
		catch (Exception e) {
			if (!File.Exists(new String(filePath.Span))) throw new InvalidOperationException($"File '{filePath.Span}' does not exist.", e);
			else throw;
		}
	}

	static UIntPtr OpenAssetFileOnWorker(MeshLoadContext context, ReadOnlySpan<char> filePath, in MeshReadConfig readConfig) {
		var pathBuffer = context.Self.AssetFilePathBuffer;
		pathBuffer.ConvertFromUtf16(filePath);

		LoadAssetFileInToMemory(
			in pathBuffer.AsRef,
			readConfig.FixCommonExportErrors,
			readConfig.OptimizeForGpu,
			out var assetHandle
		).ThrowIfFailure();
		context.AssetHandle = assetHandle;
		return assetHandle;
	}

	static void GatherMeshDataOnWorker<TVertex>(MeshLoadContext context, UIntPtr assetHandle, MeshReadMetadata metadata, in MeshReadConfig readConfig, in MeshCreationConfig creationConfig) where TVertex : unmanaged, IMeshVertex {
		int vertexCount;
		int triangleCount;
		if (readConfig.SubMeshIndex is { } subMeshIndex) {
			GetLoadedAssetMeshVertexCount(assetHandle, subMeshIndex, out vertexCount).ThrowIfFailure();
			GetLoadedAssetMeshTriangleCount(assetHandle, subMeshIndex, out triangleCount).ThrowIfFailure();
		}
		else {
			vertexCount = metadata.TotalVertexCount;
			triangleCount = metadata.TotalTriangleCount;
		}

		var vertexBufferSizeBytes = (long) vertexCount * sizeof(TVertex);
		var triangleBufferSizeBytes = (long) triangleCount * sizeof(VertexTriangle);
		ThrowIfAssetBufferSizeExceedsMaximum(vertexBufferSizeBytes, $"mesh vertex data ({vertexCount} vertices)");
		ThrowIfAssetBufferSizeExceedsMaximum(triangleBufferSizeBytes, $"mesh triangle data ({triangleCount} triangles)");

		var vertexData = context.HeapPool.Borrow<byte>((int) vertexBufferSizeBytes);
		context.VertexData = vertexData;
		var triangleData = context.HeapPool.Borrow<byte>((int) triangleBufferSizeBytes);
		context.TriangleData = triangleData;
		context.VertexCount = vertexCount;
		context.TriangleCount = triangleCount;

		fixed (byte* vertexBufferPtr = vertexData.Span)
		fixed (byte* triangleBufferPtr = triangleData.Span) {
			var vertexPtr = (TVertex*) vertexBufferPtr;
			var trianglePtr = (VertexTriangle*) triangleBufferPtr;

			if (readConfig.SubMeshIndex is { } copySubMeshIndex) {
				CopySubMeshDataFromAsset(assetHandle, readConfig.CorrectFlippedOrientation, copySubMeshIndex, vertexPtr, vertexCount, trianglePtr, triangleCount);
			}
			else {
				CopyAllMeshDataFromAsset(assetHandle, metadata, readConfig.CorrectFlippedOrientation, vertexPtr, vertexCount, trianglePtr, triangleCount);
			}

			var vertexSpan = new Span<TVertex>(vertexPtr, vertexCount);
			var triangleSpan = new Span<VertexTriangle>(trianglePtr, triangleCount);

			LocalMeshBuilder.ValidateMeshData<TVertex>(vertexSpan, triangleSpan, in creationConfig);
			if (creationConfig.FlipTriangles) LocalMeshBuilder.FlipTriangleWindings(triangleSpan);
			LocalMeshBuilder.ApplyVertexTransforms(vertexSpan, in creationConfig);
			context.BoundingBox = LocalMeshBuilder.CalculateMeshBoundingBox<TVertex>(vertexSpan, in creationConfig);
		}
	}

	static void GatherSkeletalDataOnWorker(MeshLoadContext context, UIntPtr assetHandle, int subMeshIndex, in MeshReadConfig readConfig) {
		GetLoadedAssetMeshSkeletalNodeCount(assetHandle, subMeshIndex, out var nodeCount).ThrowIfFailure();
		ThrowIfAssetBufferSizeExceedsMaximum((long) nodeCount * sizeof(NodeHandle), $"mesh skeletal node data ({nodeCount} nodes)");

		var internalNodeData = context.HeapPool.Borrow<byte>(nodeCount * sizeof(NodeHandle));
		context.InternalNodeData = internalNodeData;
		var translatedNodeBuffer = context.HeapPool.Borrow<SkeletalAnimationNode>(nodeCount);
		context.SkeletalNodes = translatedNodeBuffer;
		context.NodeCount = nodeCount;

		var nameBuffer = new InteropStringBuffer(context.Self._maxAnimationAndNodeNameLengthChars, addOneForNullTerminator: true);
		context.NameBuffer = nameBuffer;

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

			context.BoneCount = LocalMeshBuilder.CalculateAndValidateBoneCount(translatedNodeBuffer.Span);

			GatherMeshAnimations(assetHandle, nodeHandles, nodeCount, readConfig.AnimationTicksPerSecondOverride, nameBuffer, context.HeapPool, context.GatherBuffers);
			GatherNodeNames(new ReadOnlySpan<NodeHandle>(nodeHandles, nodeCount), maxNodeNameLength, nameBuffer, context.HeapPool, context.GatherBuffers);
		}
	}

	static Mesh CompleteMeshLoad(MeshLoadContext context) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		if (context.VertexData is not { } vertexData || context.TriangleData is not { } triangleData) {
			throw new InvalidOperationException("Mesh vertex and/or triangle data was null (this is a bug in TinyFFR).");
		}

		var meshBuilder = context.Self._meshBuilder;
		var triangles = MemoryMarshal.Cast<byte, VertexTriangle>(triangleData.Span)[..context.TriangleCount];

		if (!context.IsSkeletal) {
			return meshBuilder.CreateMeshFromPreValidatedAndTransformedData(
				MemoryMarshal.Cast<byte, MeshVertex>(vertexData.Span)[..context.VertexCount],
				triangles,
				context.BoundingBox,
				context.AllowsPerInstanceVertexMutation,
				context.GenerateWireframeData,
				context.Name,
				0
			);
		}

		if (context.SkeletalNodes is not { } skeletalNodes) {
			throw new InvalidOperationException("Skeletal node data was null (this is a bug in TinyFFR).");
		}

		var boneCount = context.BoneCount;
		var mesh = meshBuilder.CreateMeshFromPreValidatedAndTransformedData(
			MemoryMarshal.Cast<byte, MeshVertexSkeletal>(vertexData.Span)[..context.VertexCount],
			triangles,
			context.BoundingBox,
			context.AllowsPerInstanceVertexMutation,
			context.GenerateWireframeData,
			context.Name,
			boneCount == 0 ? 1 : boneCount
		);

		if (boneCount == 0) {
			var defaultNode = new SkeletalAnimationNode(Matrix4x4.Identity, Matrix4x4.Identity, null, 0);
			meshBuilder.AttachSkeletonToMesh(mesh, 1, new ReadOnlySpan<SkeletalAnimationNode>(in defaultNode), context.OriginTranslation, context.LinearRescalingFactor);
		}
		else {
			meshBuilder.AttachSkeletonToMesh(mesh, boneCount, skeletalNodes.Span[..context.NodeCount], context.OriginTranslation, context.LinearRescalingFactor);
		}

		AttachGatheredMeshAnimations(meshBuilder, mesh, context.GatherBuffers);
		ApplyGatheredNodeNames(meshBuilder, mesh, context.GatherBuffers);
		return mesh;
	}
	#endregion

	#region Reading
	public MeshReadMetadata ReadMeshMetadata(ReadOnlySpan<char> filePath, in MeshReadConfig readConfig) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();

		try {
			var pathBuffer = AssetFilePathBuffer;
			pathBuffer.ConvertFromUtf16(filePath);
			LoadAssetFileInToMemory(
				in pathBuffer.AsRef,
				readConfig.FixCommonExportErrors,
				readConfig.OptimizeForGpu,
				out var assetHandle
			).ThrowIfFailure();

			try {
				return GetAmalgamatedMeshMetadataFromOpenedFile(assetHandle);
			}
			finally {
				UnloadAssetFileFromMemory(assetHandle).ThrowIfFailure();
			}
		}
		catch (Exception e) {
			if (!File.Exists(filePath.ToString())) throw new InvalidOperationException($"File '{filePath}' does not exist.", e);
			else throw;
		}
	}

	static MeshReadMetadata GetAmalgamatedMeshMetadataFromOpenedFile(UIntPtr assetHandle) {
		GetLoadedAssetMeshCount(assetHandle, out var meshCount).ThrowIfFailure();

		checked {
			var totalVertexCount = 0;
			var totalTriangleCount = 0;

			for (var i = 0; i < meshCount; ++i) {
				GetLoadedAssetMeshVertexCount(assetHandle, i, out var vCount).ThrowIfFailure();
				GetLoadedAssetMeshTriangleCount(assetHandle, i, out var tCount).ThrowIfFailure();
				totalVertexCount += vCount;
				totalTriangleCount += tCount;
			}

			return new(totalVertexCount, totalTriangleCount, meshCount);
		}
	}

	public MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer, in MeshReadConfig readConfig) {
		return ReadMesh<MeshVertex>(filePath, vertexBuffer, triangleBuffer, in readConfig);
	}
	public MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertexSkeletal> vertexBuffer, Span<VertexTriangle> triangleBuffer, in MeshReadConfig readConfig) {
		return ReadMesh<MeshVertexSkeletal>(filePath, vertexBuffer, triangleBuffer, in readConfig);
	}

	MeshReadCountData ReadMesh<TVertex>(ReadOnlySpan<char> filePath, Span<TVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer, in MeshReadConfig readConfig) where TVertex : unmanaged, IMeshVertex {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();

		try {
			var pathBuffer = AssetFilePathBuffer;
			pathBuffer.ConvertFromUtf16(filePath);
			LoadAssetFileInToMemory(
				in pathBuffer.AsRef,
				readConfig.FixCommonExportErrors,
				readConfig.OptimizeForGpu,
				out var assetHandle
			).ThrowIfFailure();

			try {
				var metadata = GetAmalgamatedMeshMetadataFromOpenedFile(assetHandle);

				int vertexCount;
				int triangleCount;
				if (readConfig.SubMeshIndex is { } subMeshIndex) {
					GetLoadedAssetMeshVertexCount(assetHandle, subMeshIndex, out vertexCount).ThrowIfFailure();
					GetLoadedAssetMeshTriangleCount(assetHandle, subMeshIndex, out triangleCount).ThrowIfFailure();
				}
				else {
					vertexCount = metadata.TotalVertexCount;
					triangleCount = metadata.TotalTriangleCount;
				}

				ThrowIfAssetBufferSizeExceedsMaximum((long) vertexCount * sizeof(TVertex), $"mesh vertex data ({vertexCount} vertices)");
				ThrowIfAssetBufferSizeExceedsMaximum((long) triangleCount * sizeof(VertexTriangle), $"mesh triangle data ({triangleCount} triangles)");

				using var vertexData = _globals.HeapPool.Borrow<TVertex>(vertexCount);
				using var triangleData = _globals.HeapPool.Borrow<VertexTriangle>(triangleCount);

				fixed (TVertex* vertexPtr = vertexData.Span)
				fixed (VertexTriangle* trianglePtr = triangleData.Span) {
					if (readConfig.SubMeshIndex is { } copySubMeshIndex) {
						CopySubMeshDataFromAsset(assetHandle, readConfig.CorrectFlippedOrientation, copySubMeshIndex, vertexPtr, vertexCount, trianglePtr, triangleCount);
					}
					else {
						CopyAllMeshDataFromAsset(assetHandle, metadata, readConfig.CorrectFlippedOrientation, vertexPtr, vertexCount, trianglePtr, triangleCount);
					}
				}

				vertexData.Span[..vertexCount].CopyTo(vertexBuffer);
				triangleData.Span[..triangleCount].CopyTo(triangleBuffer);
				return new(vertexCount, triangleCount);
			}
			finally {
				UnloadAssetFileFromMemory(assetHandle).ThrowIfFailure();
			}
		}
		catch (Exception e) {
			if (!File.Exists(filePath.ToString())) throw new InvalidOperationException($"File '{filePath}' does not exist.", e);
			else throw;
		}
	}

	static void CopyAllMeshDataFromAsset<TVertex>(UIntPtr assetHandle, MeshReadMetadata metadata, bool correctFlippedOrientation, TVertex* vertexBufferPtr, int vertexBufferCount, VertexTriangle* triangleBufferPtr, int triangleBufferCount) where TVertex : unmanaged, IMeshVertex {
		var tBufferPtr = triangleBufferPtr;

		checked {
			if (typeof(TVertex) == typeof(MeshVertex)) {
				var vBufferPtr = (MeshVertex*) vertexBufferPtr;

				for (var i = 0; i < metadata.SubMeshCount; ++i) {
					GetLoadedAssetMeshVertexCount(assetHandle, i, out var vCount).ThrowIfFailure();
					GetLoadedAssetMeshTriangleCount(assetHandle, i, out var tCount).ThrowIfFailure();
					CopyLoadedAssetMeshVertices(assetHandle, i, (int) (vertexBufferCount - (vBufferPtr - (MeshVertex*) vertexBufferPtr)), vBufferPtr).ThrowIfFailure();
					CopyLoadedAssetMeshTriangles(assetHandle, i, correctFlippedOrientation, (int) (triangleBufferCount - (tBufferPtr - triangleBufferPtr)), tBufferPtr).ThrowIfFailure();
					vBufferPtr += vCount;
					tBufferPtr += tCount;
				}
			}
			else if (typeof(TVertex) == typeof(MeshVertexSkeletal)) {
				var vBufferPtr = (MeshVertexSkeletal*) vertexBufferPtr;

				for (var i = 0; i < metadata.SubMeshCount; ++i) {
					GetLoadedAssetMeshVertexCount(assetHandle, i, out var vCount).ThrowIfFailure();
					GetLoadedAssetMeshTriangleCount(assetHandle, i, out var tCount).ThrowIfFailure();
					CopyLoadedAssetMeshSkeletalVertices(assetHandle, i, (int) (vertexBufferCount - (vBufferPtr - (MeshVertexSkeletal*) vertexBufferPtr)), vBufferPtr).ThrowIfFailure();
					CopyLoadedAssetMeshTriangles(assetHandle, i, correctFlippedOrientation, (int) (triangleBufferCount - (tBufferPtr - triangleBufferPtr)), tBufferPtr).ThrowIfFailure();
					vBufferPtr += vCount;
					tBufferPtr += tCount;
				}
			}
			else {
				throw new InvalidOperationException($"Unknown vertex type '{typeof(TVertex)}'.");
			}
		}
	}

	static void CopySubMeshDataFromAsset<TVertex>(UIntPtr assetHandle, bool correctFlippedOrientation, int subMeshIndex, TVertex* vertexBufferPtr, int vertexBufferCount, VertexTriangle* triangleBufferPtr, int triangleBufferCount) where TVertex : unmanaged, IMeshVertex {
		CopyLoadedAssetMeshTriangles(assetHandle, subMeshIndex, correctFlippedOrientation, triangleBufferCount, triangleBufferPtr).ThrowIfFailure();

		if (typeof(TVertex) == typeof(MeshVertex)) {
			CopyLoadedAssetMeshVertices(assetHandle, subMeshIndex, vertexBufferCount, (MeshVertex*) vertexBufferPtr).ThrowIfFailure();
		}
		else if (typeof(TVertex) == typeof(MeshVertexSkeletal)) {
			CopyLoadedAssetMeshSkeletalVertices(assetHandle, subMeshIndex, vertexBufferCount, (MeshVertexSkeletal*) vertexBufferPtr).ThrowIfFailure();
		}
		else {
			throw new InvalidOperationException($"Unknown vertex type '{typeof(TVertex)}'.");
		}
	}
	#endregion

	#region Skeletal Gather / Apply
	static void GatherNodeNames(ReadOnlySpan<NodeHandle> nodeHandles, int maxNodeNameLength, InteropStringBuffer nameBuffer, ThreadSafeHeapPoolWrapper heapPool, MeshSkeletalGatherBuffers destination) {
		const string FallbackNodeNamePrefix = "node_";
		Span<char> stackNameBuffer = stackalloc char[MaxNameLengthForStackAlloc];

		if (maxNodeNameLength > nameBuffer.Length) {
			throw new InvalidOperationException($"Node name length too long; increase {nameof(LocalAssetLoaderConfig.MaxAnimationAndNodeNameLengthChars)} in {nameof(LocalAssetLoaderConfig)}!");
		}

		for (var i = 0; i < nodeHandles.Length; ++i) {
			CopyLoadedAssetSkeletalNodeName(
				nodeHandles[i],
				nameBuffer.AsPointer,
				nameBuffer.Length
			).ThrowIfFailure();

			var nodeNameUtf16Length = nameBuffer.GetUtf16Length();

			if (nodeNameUtf16Length == 0) {
				var nodeNameBuffer = stackNameBuffer;
				FallbackNodeNamePrefix.CopyTo(nodeNameBuffer);
				if (i.TryFormat(nodeNameBuffer[FallbackNodeNamePrefix.Length..], out var additionalCharsWritten, default, null)) {
					nodeNameUtf16Length = FallbackNodeNamePrefix.Length + additionalCharsWritten;
				}
				else nodeNameUtf16Length = FallbackNodeNamePrefix.Length;
				destination.NodeNameSlices.Add(destination.AppendName(nodeNameBuffer[..nodeNameUtf16Length]));
			}
			else {
				using var nodeNameHeapBuffer = nodeNameUtf16Length > MaxNameLengthForStackAlloc
					? heapPool.Borrow<char>(nodeNameUtf16Length)
					: (PooledHeapMemory<char>?) null;
				var nodeNameBuffer = nodeNameHeapBuffer.HasValue ? nodeNameHeapBuffer.Value.Span : stackNameBuffer;
				nameBuffer.ConvertToUtf16(nodeNameBuffer);
				destination.NodeNameSlices.Add(destination.AppendName(nodeNameBuffer[..nodeNameUtf16Length]));
			}
		}
	}

	static void ApplyGatheredNodeNames(LocalMeshBuilder meshBuilder, Mesh mesh, MeshSkeletalGatherBuffers buffers) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		for (var i = 0; i < buffers.NodeNameSlices.Count; ++i) {
			meshBuilder.SetSkeletonNodeName(mesh, i, buffers.GetName(buffers.NodeNameSlices[i]));
		}
	}

	static void GatherMeshAnimations(UIntPtr assetHandle, NodeHandle* nodeHandleBuffer, int nodeHandleBufferCount, float? animTicksPerSecOverride, InteropStringBuffer nameBuffer, ThreadSafeHeapPoolWrapper heapPool, MeshSkeletalGatherBuffers destination) {
		GetLoadedAssetMeshSkeletalAnimationCount(
			assetHandle,
			out var animationCount
		).ThrowIfFailure();

		if (animationCount <= 0) return;

		Span<char> stackNameBuffer = stackalloc char[MaxNameLengthForStackAlloc];

		for (var a = 0; a < animationCount; ++a) {
			GetLoadedAssetSkeletalAnimationMetadata(
				assetHandle,
				a,
				animTicksPerSecOverride ?? 0f,
				out var nameLenBytes,
				out var animationDurationSeconds,
				out var animationChannelCount
			).ThrowIfFailure();

			if (nameLenBytes > nameBuffer.Length) {
				throw new InvalidOperationException($"Animation name length too long; increase {nameof(LocalAssetLoaderConfig.MaxAnimationAndNodeNameLengthChars)} in {nameof(LocalAssetLoaderConfig)}!");
			}
			CopyLoadedAssetSkeletalAnimationName(
				assetHandle,
				a,
				nameBuffer.AsPointer,
				nameBuffer.Length
			).ThrowIfFailure();

			var animNameUtf16Length = nameBuffer.GetUtf16Length();
			using var animNameHeapBuffer = animNameUtf16Length > MaxNameLengthForStackAlloc
				? heapPool.Borrow<char>(animNameUtf16Length)
				: (PooledHeapMemory<char>?) null;
			var animNameBuffer = animNameHeapBuffer.HasValue ? animNameHeapBuffer.Value.Span : stackNameBuffer;
			if (animNameUtf16Length == 0) {
				const string FallbackAnimationNamePrefix = "anim_";
				FallbackAnimationNamePrefix.CopyTo(animNameBuffer);
				if (a.TryFormat(animNameBuffer[FallbackAnimationNamePrefix.Length..], out var additionalCharsWritten, default, null)) {
					animNameUtf16Length = FallbackAnimationNamePrefix.Length + additionalCharsWritten;
				}
				else animNameUtf16Length = FallbackAnimationNamePrefix.Length;
			}
			else nameBuffer.ConvertToUtf16(animNameBuffer);

			var totalTranslationKeyframes = 0;
			var totalRotationKeyframes = 0;
			var totalScalingKeyframes = 0;
			var highestSingleChannelScalingKeyframeCount = 0;
			var highestSingleChannelRotationKeyframeCount = 0;
			var highestSingleChannelTranslationKeyframeCount = 0;
			var mutations = heapPool.Borrow<SkeletalAnimationNodeMutationDescriptor>(animationChannelCount);
			var channelsToInclude = heapPool.Borrow<int>(animationChannelCount);
			var numChannelsIncluded = 0;

			for (var c = 0; c < animationChannelCount; ++c) {
				var getMetadataResult = GetLoadedAssetSkeletalAnimationChannelMetadata(
					assetHandle,
					a,
					c,
					nodeHandleBuffer,
					nodeHandleBufferCount,
					out var nodeIndex,
					out var numScalingKeyframes,
					out var numRotationKeyframes,
					out var numTranslationKeyframes
				);
				if (!getMetadataResult) {
					mutations.Dispose();
					channelsToInclude.Dispose();
					getMetadataResult.ThrowIfFailure();
					throw new InvalidOperationException();
				}
				if (nodeIndex < 0 || nodeIndex >= nodeHandleBufferCount) continue;

				channelsToInclude.Span[numChannelsIncluded] = c;
				mutations.Span[numChannelsIncluded] = new SkeletalAnimationNodeMutationDescriptor(
					nodeIndex,
					totalScalingKeyframes, numScalingKeyframes,
					totalRotationKeyframes, numRotationKeyframes,
					totalTranslationKeyframes, numTranslationKeyframes
				);
				totalScalingKeyframes += numScalingKeyframes;
				totalRotationKeyframes += numRotationKeyframes;
				totalTranslationKeyframes += numTranslationKeyframes;
				highestSingleChannelScalingKeyframeCount = Int32.Max(highestSingleChannelScalingKeyframeCount, numScalingKeyframes);
				highestSingleChannelRotationKeyframeCount = Int32.Max(highestSingleChannelRotationKeyframeCount, numRotationKeyframes);
				highestSingleChannelTranslationKeyframeCount = Int32.Max(highestSingleChannelTranslationKeyframeCount, numTranslationKeyframes);
				numChannelsIncluded++;
			}

			var scalingKeyframes = heapPool.Borrow<SkeletalAnimationScalingKeyframe>(totalScalingKeyframes);
			var rotationKeyframes = heapPool.Borrow<SkeletalAnimationRotationKeyframe>(totalRotationKeyframes);
			var translationKeyframes = heapPool.Borrow<SkeletalAnimationTranslationKeyframe>(totalTranslationKeyframes);
			using var scalingVectorBuffer = heapPool.Borrow<Vector3>(highestSingleChannelScalingKeyframeCount);
			using var scalingTimeCodeBuffer = heapPool.Borrow<float>(highestSingleChannelScalingKeyframeCount);
			using var rotationQuaternionBuffer = heapPool.Borrow<Quaternion>(highestSingleChannelRotationKeyframeCount);
			using var rotationTimeCodeBuffer = heapPool.Borrow<float>(highestSingleChannelRotationKeyframeCount);
			using var translationVectorBuffer = heapPool.Borrow<Vector3>(highestSingleChannelTranslationKeyframeCount);
			using var translationTimeCodeBuffer = heapPool.Borrow<float>(highestSingleChannelTranslationKeyframeCount);

			try {
				fixed (Vector3* scalingVectorPtr = scalingVectorBuffer.Span)
				fixed (float* scalingTimeCodePtr = scalingTimeCodeBuffer.Span)
				fixed (Quaternion* rotationQuaternionPtr = rotationQuaternionBuffer.Span)
				fixed (float* rotationTimeCodePtr = rotationTimeCodeBuffer.Span)
				fixed (Vector3* translationVectorPtr = translationVectorBuffer.Span)
				fixed (float* translationTimeCodePtr = translationTimeCodeBuffer.Span) {
					for (var i = 0; i < numChannelsIncluded; ++i) {
						var c = channelsToInclude.Span[i];
						CopyLoadedAssetSkeletalAnimationChannelData(
							assetHandle,
							a,
							c,
							animTicksPerSecOverride ?? 0f,
							scalingVectorPtr,
							scalingTimeCodePtr,
							highestSingleChannelScalingKeyframeCount,
							rotationQuaternionPtr,
							rotationTimeCodePtr,
							highestSingleChannelRotationKeyframeCount,
							translationVectorPtr,
							translationTimeCodePtr,
							highestSingleChannelTranslationKeyframeCount
						).ThrowIfFailure();

						for (var s = 0; s < mutations.Span[i].ScalingKeyframeCount; ++s) {
							scalingKeyframes.Span[mutations.Span[i].ScalingKeyframeStartIndex + s] = new SkeletalAnimationScalingKeyframe(
								scalingTimeCodeBuffer.Span[s],
								Vect.FromVector3(scalingVectorBuffer.Span[s])
							);
						}
						for (var r = 0; r < mutations.Span[i].RotationKeyframeCount; ++r) {
							rotationKeyframes.Span[mutations.Span[i].RotationKeyframeStartIndex + r] = new SkeletalAnimationRotationKeyframe(
								rotationTimeCodeBuffer.Span[r],
								rotationQuaternionBuffer.Span[r]
							);
						}
						for (var t = 0; t < mutations.Span[i].TranslationKeyframeCount; ++t) {
							translationKeyframes.Span[mutations.Span[i].TranslationKeyframeStartIndex + t] = new SkeletalAnimationTranslationKeyframe(
								translationTimeCodeBuffer.Span[t],
								Vect.FromVector3(translationVectorBuffer.Span[t])
							);
						}
					}
				}

				if (numChannelsIncluded != animationChannelCount) {
					var newMutationsBuffer = heapPool.Borrow<SkeletalAnimationNodeMutationDescriptor>(numChannelsIncluded);
					mutations.Span[..numChannelsIncluded].CopyTo(newMutationsBuffer.Span);
					mutations.Dispose();
					mutations = newMutationsBuffer;
				}

				destination.Animations.Add(new GatheredAnimation(
					scalingKeyframes,
					rotationKeyframes,
					translationKeyframes,
					mutations,
					animationDurationSeconds,
					destination.AppendName(animNameBuffer[..animNameUtf16Length])
				));
			}
			catch {
				scalingKeyframes.Dispose();
				rotationKeyframes.Dispose();
				translationKeyframes.Dispose();
				mutations.Dispose();
				throw;
			}
			finally {
				channelsToInclude.Dispose();
			}
		}
	}

	static void AttachGatheredMeshAnimations(LocalMeshBuilder meshBuilder, Mesh mesh, MeshSkeletalGatherBuffers buffers) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		for (var i = 0; i < buffers.Animations.Count; ++i) {
			var animation = buffers.Animations[i];
			meshBuilder.AttachAnimationAndTransferBufferOwnership(
				mesh,
				animation.ScalingKeyframes,
				animation.RotationKeyframes,
				animation.TranslationKeyframes,
				animation.Mutations,
				animation.DurationSeconds,
				buffers.GetName(animation.Name)
			);
			buffers.NumAnimationsWithTransferredOwnership = i + 1;
		}
	}
	#endregion
	
	#region Baking
	public Mesh LoadBakedMesh(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default) {
		return _globals.Bakery.Load<Mesh, Mesh, LocalAssetLoader>(this, bakedAssetFilePath, name, &LoadBakedMeshCore);
	}

	public TinyFfrAsyncOperation<Mesh> LoadBakedMeshAsync(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default) {
		return _globals.Bakery.LoadAsync<Mesh, Mesh, LocalAssetLoader>(this, bakedAssetFilePath, name, &LoadBakedMeshCore);
	}

	static Mesh LoadBakedMeshCore(LocalAssetBakery.AssetLoadContext ctx) {
		static Mesh Finalize(LocalAssetBakery.AssetLoadContext ctx) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();

			var assetData = ctx.AssetData;
			var self = ctx.Invoker<LocalAssetLoader>();
			var name = ctx.StoredOrOverridingName;

			var isSkeletal = assetData.Extract<bool>(MeshBakingSchema.IsSkeletal);
			var vertexCount = assetData.Extract<int>(MeshBakingSchema.VertexCount);
			var triangleCount = assetData.Extract<int>(MeshBakingSchema.TriangleCount);
			var boundingBox = assetData.Extract<PositionedCuboid>(MeshBakingSchema.BoundingBox);
			var boneCount = assetData.Extract<int>(MeshBakingSchema.BoneCount);
			var allowsPerInstanceVertexMutation = assetData.Extract<bool>(MeshBakingSchema.AllowsPerInstanceVertexMutation);
			var generateWireframeData = assetData.Extract<bool>(MeshBakingSchema.GeneratesWireframeData);
			var triangles = assetData.ExtractSpan<VertexTriangle>(MeshBakingSchema.IndexData)[..triangleCount];

			Mesh result;
			if (isSkeletal) {
				result = self._meshBuilder.CreateMeshFromPreValidatedAndTransformedData(
					assetData.ExtractSpan<MeshVertexSkeletal>(MeshBakingSchema.VertexData)[..vertexCount],
					triangles,
					boundingBox,
					allowsPerInstanceVertexMutation,
					generateWireframeData,
					name,
					boneCount
				);
			}
			else {
				result = self._meshBuilder.CreateMeshFromPreValidatedAndTransformedData(
					assetData.ExtractSpan<MeshVertex>(MeshBakingSchema.VertexData)[..vertexCount],
					triangles,
					boundingBox,
					allowsPerInstanceVertexMutation,
					generateWireframeData,
					name,
					boneCount
				);
			}

			if (!isSkeletal) return result;

			try {
				RestoreSkeletalData(self, result, assetData);
			}
			catch {
				result.Dispose();
				throw;
			}

			return result;
		}

		return ctx.GenerateResourceOnPrimaryAndWait(&Finalize);
	}

	static void RestoreSkeletalData(LocalAssetLoader self, Mesh mesh, LoadedBakedAsset assetData) {
		var nodeCount = assetData.Extract<int>(MeshBakingSchema.SkeletonNodeCount);
		var boneCount = assetData.Extract<int>(MeshBakingSchema.BoneCount);

		self._meshBuilder.RestoreBakedSkeleton(
			mesh,
			nodeCount,
			boneCount,
			assetData.Extract<int>(MeshBakingSchema.SkeletonFirstParentedNodeIndex),
			assetData.Extract<Matrix4x4>(MeshBakingSchema.SkeletonModelImportTransform),
			assetData.ExtractSpan<Matrix4x4>(MeshBakingSchema.SkeletonDefaultLocalTransforms)[..nodeCount],
			assetData.ExtractSpan<Matrix4x4>(MeshBakingSchema.SkeletonBindPoseInversions)[..boneCount],
			assetData.ExtractSpan<int>(MeshBakingSchema.SkeletonParentIndices)[..nodeCount],
			assetData.ExtractSpan<int>(MeshBakingSchema.SkeletonBoneToNodeMap)[..boneCount],
			assetData.ExtractSpan<int>(MeshBakingSchema.SkeletonMutationTargetIndexMap)[..nodeCount]
		);

		var animationEntries = assetData.ExtractSpan<MeshBakingSchema.BakedAnimationEntry>(MeshBakingSchema.AnimationTable);
		var scaling = assetData.ExtractSpan<SkeletalAnimationScalingKeyframe>(MeshBakingSchema.AnimationScalingKeyframes);
		var rotation = assetData.ExtractSpan<SkeletalAnimationRotationKeyframe>(MeshBakingSchema.AnimationRotationKeyframes);
		var translation = assetData.ExtractSpan<SkeletalAnimationTranslationKeyframe>(MeshBakingSchema.AnimationTranslationKeyframes);
		var mutations = assetData.ExtractSpan<SkeletalAnimationNodeMutationDescriptor>(MeshBakingSchema.AnimationMutationDescriptors);
		var animationNameChars = assetData.ExtractString(MeshBakingSchema.AnimationNameChars);

		foreach (var entry in animationEntries) {
			self._meshBuilder.RestoreBakedAnimation(
				mesh,
				scaling.Slice(entry.ScalingStart, entry.ScalingCount),
				rotation.Slice(entry.RotationStart, entry.RotationCount),
				translation.Slice(entry.TranslationStart, entry.TranslationCount),
				mutations.Slice(entry.MutationStart, entry.MutationCount),
				entry.DefaultCompletionTimeSeconds,
				animationNameChars.Slice(entry.NameStart, entry.NameLength)
			);
		}

		var nodeNameEntries = assetData.ExtractSpan<MeshBakingSchema.BakedNodeNameEntry>(MeshBakingSchema.NodeNameTable);
		var nodeNameChars = assetData.ExtractString(MeshBakingSchema.NodeNameChars);
		foreach (var entry in nodeNameEntries) {
			self._meshBuilder.RestoreBakedNodeName(mesh, entry.NodeIndex, nodeNameChars.Slice(entry.NameStart, entry.NameLength));
		}
	}
	#endregion

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_count")]
	static extern InteropResult GetLoadedAssetMeshCount(
		UIntPtr assetHandle,
		out int outMeshCount
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_vertex_count")]
	static extern InteropResult GetLoadedAssetMeshVertexCount(
		UIntPtr assetHandle,
		int meshIndex,
		out int outVertexCount
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_triangle_count")]
	static extern InteropResult GetLoadedAssetMeshTriangleCount(
		UIntPtr assetHandle,
		int meshIndex,
		out int outTriangleCount
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "copy_loaded_asset_mesh_triangles")]
	static extern InteropResult CopyLoadedAssetMeshTriangles(
		UIntPtr assetHandle,
		int meshIndex,
		InteropBool correctFlippedOrientation,
		int bufferSizeTriangles,
		VertexTriangle* triangleBufferPtr
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "copy_loaded_asset_mesh_vertices")]
	static extern InteropResult CopyLoadedAssetMeshVertices(
		UIntPtr assetHandle,
		int meshIndex,
		int bufferSizeVertices,
		MeshVertex* vertexBufferPtr
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "copy_loaded_asset_mesh_skeletal_vertices")]
	static extern InteropResult CopyLoadedAssetMeshSkeletalVertices(
		UIntPtr assetHandle,
		int meshIndex,
		int bufferSizeVertices,
		MeshVertexSkeletal* vertexBufferPtr
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_skeletal_bone_count")]
	static extern InteropResult GetLoadedAssetMeshSkeletalBoneCount(
		UIntPtr assetHandle,
		int meshIndex,
		out int boneCount
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_skeletal_node_count")]
	static extern InteropResult GetLoadedAssetMeshSkeletalNodeCount(
		UIntPtr assetHandle,
		int meshIndex,
		out int nodeCount
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "generate_loaded_asset_mesh_skeletal_node_flat_buffer")]
	static extern InteropResult GenerateLoadedAssetMeshSkeletalNodeFlatBuffer(
		UIntPtr assetHandle,
		int meshIndex,
		NodeHandle* nodeHandleBuffer,
		int nodeHandleBufferCount
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_skeletal_node")]
	static extern InteropResult GetLoadedAssetMeshSkeletalNode(
		NodeHandle* nodeHandleBuffer,
		int nodeHandleBufferCount,
		int nodeIndex,
		out Matrix4x4 outInverseBindPoseMatrix,
		out Matrix4x4 outDefaultTransformMatrix,
		out int outParentNodeIndex,
		out int outBoneIndex,
		out int nameLengthBytes
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_skeletal_animation_count")]
	static extern InteropResult GetLoadedAssetMeshSkeletalAnimationCount(
		UIntPtr assetHandle,
		out int outAnimationCount
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_skeletal_animation_metadata")]
	static extern InteropResult GetLoadedAssetSkeletalAnimationMetadata(
		UIntPtr assetHandle,
		int animationIndex,
		float ticksPerSecondOverride,
		out int outNameLengthBytes,
		out float outDurationSeconds,
		out int outChannelCount
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "copy_loaded_asset_mesh_skeletal_animation_name")]
	static extern InteropResult CopyLoadedAssetSkeletalAnimationName(
		UIntPtr assetHandle,
		int animationIndex,
		byte* utf8NameBuffer,
		int bufferLengthBytes
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "copy_loaded_asset_mesh_skeletal_node_name")]
	static extern InteropResult CopyLoadedAssetSkeletalNodeName(
		NodeHandle nodeHandle,
		byte* utf8NameBuffer,
		int bufferLengthBytes
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_skeletal_animation_channel_metadata")]
	static extern InteropResult GetLoadedAssetSkeletalAnimationChannelMetadata(
		UIntPtr assetHandle,
		int animationIndex,
		int channelIndex,
		NodeHandle* nodeHandleBuffer,
		int nodeHandleBufferCount,
		out int outNodeIndex,
		out int outScalingKeyframeCount,
		out int outRotationKeyframeCount,
		out int outTranslationKeyframeCount
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "copy_loaded_asset_mesh_skeletal_animation_channel_data")]
	static extern InteropResult CopyLoadedAssetSkeletalAnimationChannelData(
		UIntPtr assetHandle,
		int animationIndex,
		int channelIndex,
		float ticksPerSecondOverride,
		Vector3* scalingVectorBuffer,
		float* scalingTimeCodeBuffer,
		int scalingBufferCount,
		Quaternion* rotationQuaternionBuffer,
		float* rotationTimeCodeBuffer,
		int rotationBufferCount,
		Vector3* translationVectorBuffer,
		float* translationTimeCodeBuffer,
		int translationBufferCount
	);
	#endregion
}
