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
using Egodystonic.TinyFFR.Rendering.Local.Sync;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Local;

unsafe partial class LocalAssetLoader {
	readonly record struct MeshDataCopyResult(FixedByteBufferPool.FixedByteBuffer VertexBuffer, int NumVerticesWritten, FixedByteBufferPool.FixedByteBuffer TriangleBuffer, int NumTrianglesWritten);
	[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 3 * 8)]
	readonly struct NodeHandle {
		[FieldOffset(0)] readonly UIntPtr _node;
		[FieldOffset(8)] readonly UIntPtr _bone;
		[FieldOffset(16)] readonly int _boneIndex;
		[FieldOffset(20)] readonly int _;
	} 
	
	readonly LocalMeshBuilder _meshBuilder;
	readonly FixedByteBufferPool _vertexTriangleBufferPool;
	readonly InteropStringBuffer _animationAndNodeNameBuffer;
	readonly FixedByteBufferPool _skeletalAnimationKeyframeDataPool;
	readonly FixedByteBufferPool _skeletalNodeBufferPool;
	
	public Mesh LoadMesh(ReadOnlySpan<char> filePath, in MeshCreationConfig config, in MeshReadConfig readConfig) {
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();
		config.ThrowIfInvalid();

		try {
			_assetFilePathBuffer.ConvertFromUtf16(filePath);
			LoadAssetFileInToMemory(
				in _assetFilePathBuffer.AsRef,
				readConfig.FixCommonExportErrors,
				readConfig.OptimizeForGpu,
				out var assetHandle
			).ThrowIfFailure();

			try {
				var metadata = GetAmalgamatedMeshMetadataFromOpenedFile(assetHandle);
				if (metadata.SubMeshCount <= 0) throw new ArgumentException($"Given file '{filePath}' does not contain any mesh data.");
				
				var loadSkeletalAnimationData = readConfig.LoadSkeletalAnimationDataIfPresent;
				var skeletalSubMeshIndex = readConfig.SubMeshIndex ?? 0;
				if (loadSkeletalAnimationData) {
					if (metadata.SubMeshCount != 1 && readConfig.SubMeshIndex == null) {
						Console.WriteLine($"Can not load skeletal animation data for file '{filePath}' as it contains multiple sub-meshes and no {nameof(MeshReadConfig.SubMeshIndex)} was given " +
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
							Console.WriteLine($"Can not load skeletal animation data for file '{filePath}' as its bone count ({boneCount}) is higher than the maximum TinyFFR supports ({IMeshBuilder.MaxSkeletalBoneCount}).");
							loadSkeletalAnimationData = false;
							break;
					}
				}
				
				var copyResult = loadSkeletalAnimationData
					? CopyMeshDataFromAsset<MeshVertexSkeletal>(assetHandle, metadata, in readConfig)
					: CopyMeshDataFromAsset<MeshVertex>(assetHandle, metadata, in readConfig);

				try {
					if (loadSkeletalAnimationData) {
						GetLoadedAssetMeshSkeletalNodeCount(assetHandle, skeletalSubMeshIndex, out var nodeCount).ThrowIfFailure();
						
						var internalNodeBuffer = _skeletalNodeBufferPool.Rent<NodeHandle>(nodeCount);
						var translatedNodeBuffer = _globals.HeapPool.Borrow<SkeletalAnimationNode>(nodeCount);
						
						try {
							GenerateLoadedAssetMeshSkeletalNodeFlatBuffer(
								assetHandle,
								skeletalSubMeshIndex,
								(NodeHandle*) internalNodeBuffer.StartPtr,
								nodeCount
							).ThrowIfFailure();
							
							var maxNodeNameLength = 0;
							for (var n = 0; n < nodeCount; ++n) {
								GetLoadedAssetMeshSkeletalNode(
									(NodeHandle*) internalNodeBuffer.StartPtr,
									nodeCount,
									n,
									out var inverseBindPoseMatrix,
									out var defaultTransformMatrix,
									out var parentNodeIndex,
									out var boneIndex,
									out var nameLength
								).ThrowIfFailure();
								
								translatedNodeBuffer.Buffer[n] = new(
									defaultTransformMatrix,
									inverseBindPoseMatrix,
									parentNodeIndex >= 0 ? parentNodeIndex : null,
									boneIndex >= 0 ? boneIndex : null
								);
								maxNodeNameLength = Int32.Max(nameLength, maxNodeNameLength);
							}

							var result = _meshBuilder.CreateMesh(
								copyResult.VertexBuffer.AsReadOnlySpan<MeshVertexSkeletal>(copyResult.NumVerticesWritten),
								copyResult.TriangleBuffer.AsReadOnlySpan<VertexTriangle>(copyResult.NumTrianglesWritten),
								translatedNodeBuffer.Buffer,
								config
							);
						
							LoadAndAttachMeshAnimations(assetHandle, (NodeHandle*) internalNodeBuffer.StartPtr, nodeCount, readConfig.AnimationTicksPerSecondOverride, result);
							LoadAndSetNodeNames(internalNodeBuffer.AsReadOnlySpan<NodeHandle>(nodeCount), maxNodeNameLength, result);
							return result;
						}
						finally {
							_skeletalNodeBufferPool.Return(internalNodeBuffer);
							translatedNodeBuffer.Dispose();
						}
					}
					else {
						return _meshBuilder.CreateMesh(
							copyResult.VertexBuffer.AsReadOnlySpan<MeshVertex>(copyResult.NumVerticesWritten),
							copyResult.TriangleBuffer.AsReadOnlySpan<VertexTriangle>(copyResult.NumTrianglesWritten),
							config
						);
					}
				}
				finally {
					_vertexTriangleBufferPool.Return(copyResult.VertexBuffer);
					_vertexTriangleBufferPool.Return(copyResult.TriangleBuffer);
				}
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
	
	public MeshReadMetadata ReadMeshMetadata(ReadOnlySpan<char> filePath, in MeshReadConfig readConfig) {
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();

		try {
			_assetFilePathBuffer.ConvertFromUtf16(filePath);
			LoadAssetFileInToMemory(
				in _assetFilePathBuffer.AsRef,
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
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();

		try {
			_assetFilePathBuffer.ConvertFromUtf16(filePath);
			LoadAssetFileInToMemory(
				in _assetFilePathBuffer.AsRef,
				readConfig.FixCommonExportErrors,
				readConfig.OptimizeForGpu,
				out var assetHandle
			).ThrowIfFailure();
			
			var metadata = GetAmalgamatedMeshMetadataFromOpenedFile(assetHandle);

			try {
				var copyResult = CopyMeshDataFromAsset<TVertex>(assetHandle, metadata, in readConfig);
				try {
					copyResult.VertexBuffer.AsReadOnlySpan<TVertex>(copyResult.NumVerticesWritten).CopyTo(vertexBuffer);
					copyResult.TriangleBuffer.AsReadOnlySpan<VertexTriangle>(copyResult.NumTrianglesWritten).CopyTo(triangleBuffer);
					return new(copyResult.NumVerticesWritten, copyResult.NumTrianglesWritten);
				}
				finally {
					_vertexTriangleBufferPool.Return(copyResult.VertexBuffer);
					_vertexTriangleBufferPool.Return(copyResult.TriangleBuffer);
				}
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
	
	MeshDataCopyResult CopyMeshDataFromAsset<TVertex>(UIntPtr assetHandle, MeshReadMetadata metadata, in MeshReadConfig readConfig) where TVertex : unmanaged, IMeshVertex {
		if (readConfig.SubMeshIndex is { } subMeshIndex) return CopySubMeshDataFromAsset<TVertex>(assetHandle, readConfig.CorrectFlippedOrientation, subMeshIndex);
		else return CopyAllMeshDataFromAsset<TVertex>(assetHandle, metadata, readConfig.CorrectFlippedOrientation);
	}
	
	MeshDataCopyResult CopyAllMeshDataFromAsset<TVertex>(UIntPtr assetHandle, MeshReadMetadata metadata, bool correctFlippedOrientation) where TVertex : unmanaged, IMeshVertex {
		var vertexBuffer = _vertexTriangleBufferPool.Rent<TVertex>(metadata.TotalVertexCount);
		var triangleBuffer = _vertexTriangleBufferPool.Rent<VertexTriangle>(metadata.TotalTriangleCount);
	
		var tBufferPtr = (VertexTriangle*) triangleBuffer.StartPtr;

		checked {
			if (typeof(TVertex) == typeof(MeshVertex)) {
				var vBufferPtr = (MeshVertex*) vertexBuffer.StartPtr;
		
				for (var i = 0; i < metadata.SubMeshCount; ++i) {
					GetLoadedAssetMeshVertexCount(assetHandle, i, out var vCount).ThrowIfFailure();
					GetLoadedAssetMeshTriangleCount(assetHandle, i, out var tCount).ThrowIfFailure();
					CopyLoadedAssetMeshVertices(assetHandle, i, (int) (vertexBuffer.Size<MeshVertex>() - (vBufferPtr - (MeshVertex*) vertexBuffer.StartPtr)), vBufferPtr);
					CopyLoadedAssetMeshTriangles(assetHandle, i, correctFlippedOrientation, (int) (triangleBuffer.Size<VertexTriangle>() - (tBufferPtr - (VertexTriangle*) triangleBuffer.StartPtr)), tBufferPtr);
					vBufferPtr += vCount;
					tBufferPtr += tCount;
				}
			}
			else if (typeof(TVertex) == typeof(MeshVertexSkeletal)) {
				var vBufferPtr = (MeshVertexSkeletal*) vertexBuffer.StartPtr;
		
				for (var i = 0; i < metadata.SubMeshCount; ++i) {
					GetLoadedAssetMeshVertexCount(assetHandle, i, out var vCount).ThrowIfFailure();
					GetLoadedAssetMeshTriangleCount(assetHandle, i, out var tCount).ThrowIfFailure();
					CopyLoadedAssetMeshSkeletalVertices(assetHandle, i, (int) (vertexBuffer.Size<MeshVertexSkeletal>() - (vBufferPtr - (MeshVertexSkeletal*) vertexBuffer.StartPtr)), vBufferPtr);
					CopyLoadedAssetMeshTriangles(assetHandle, i, correctFlippedOrientation, (int) (triangleBuffer.Size<VertexTriangle>() - (tBufferPtr - (VertexTriangle*) triangleBuffer.StartPtr)), tBufferPtr);
					vBufferPtr += vCount;
					tBufferPtr += tCount;
				}
			}
			else {
				throw new InvalidOperationException($"Unknown vertex type '{typeof(TVertex)}'.");
			}
		}
		
		return new(vertexBuffer, metadata.TotalVertexCount, triangleBuffer, metadata.TotalTriangleCount);
	}
	
	MeshDataCopyResult CopySubMeshDataFromAsset<TVertex>(UIntPtr assetHandle, bool correctFlippedOrientation, int subMeshIndex) where TVertex : unmanaged, IMeshVertex {	
		GetLoadedAssetMeshVertexCount(assetHandle, subMeshIndex, out var subMeshVertexCount).ThrowIfFailure();
		GetLoadedAssetMeshTriangleCount(assetHandle, subMeshIndex, out var subMeshTriangleCount).ThrowIfFailure();
		
		var vertexBuffer = _vertexTriangleBufferPool.Rent<TVertex>(subMeshVertexCount);
		var triangleBuffer = _vertexTriangleBufferPool.Rent<VertexTriangle>(subMeshTriangleCount);
	
		CopyLoadedAssetMeshTriangles(assetHandle, subMeshIndex, correctFlippedOrientation, triangleBuffer.Size<VertexTriangle>(), (VertexTriangle*) triangleBuffer.StartPtr);
			
		if (typeof(TVertex) == typeof(MeshVertex)) {
			CopyLoadedAssetMeshVertices(assetHandle, subMeshIndex, vertexBuffer.Size<MeshVertex>(), (MeshVertex*) vertexBuffer.StartPtr);
		}
		else if (typeof(TVertex) == typeof(MeshVertexSkeletal)) {
			CopyLoadedAssetMeshSkeletalVertices(assetHandle, subMeshIndex, vertexBuffer.Size<MeshVertexSkeletal>(), (MeshVertexSkeletal*) vertexBuffer.StartPtr);
		}
		else {
			throw new InvalidOperationException($"Unknown vertex type '{typeof(TVertex)}'.");
		}
		
		return new(vertexBuffer, subMeshVertexCount, triangleBuffer, subMeshTriangleCount);
	}
	
	void LoadAndSetNodeNames(ReadOnlySpan<NodeHandle> nodeHandles, int maxNodeNameLength, Mesh mesh) {
		const int MaxNameLengthForStackAlloc = 2048;
		Span<char> stackNameBuffer = stackalloc char[MaxNameLengthForStackAlloc];
		
		if (maxNodeNameLength > _animationAndNodeNameBuffer.Length) {
			throw new InvalidOperationException($"Node name length too long; increase {nameof(LocalAssetLoaderConfig.MaxAnimationAndNodeNameLengthChars)} in {nameof(LocalAssetLoaderConfig)}!");
		}
		
		for (var i = 0; i < nodeHandles.Length; ++i) {
			CopyLoadedAssetSkeletalNodeName(
				nodeHandles[i],
				_animationAndNodeNameBuffer.AsPointer,
				_animationAndNodeNameBuffer.Length
			).ThrowIfFailure();
			
			var nodeNameUtf16Length = _animationAndNodeNameBuffer.GetUtf16Length();
			
			if (nodeNameUtf16Length == 0) {
				const string FallbackNodeNamePrefix = "node_";
				var nodeNameBuffer = stackNameBuffer;
				FallbackNodeNamePrefix.CopyTo(nodeNameBuffer);
				if (i.TryFormat(nodeNameBuffer[FallbackNodeNamePrefix.Length..], out var additionalCharsWritten, default, null)) {
					nodeNameUtf16Length = FallbackNodeNamePrefix.Length + additionalCharsWritten;
				}
				else nodeNameUtf16Length = FallbackNodeNamePrefix.Length;
				_meshBuilder.SetSkeletonNodeName(mesh, i, nodeNameBuffer[..nodeNameUtf16Length]);
			}
			else {
				using var nodeNameHeapBuffer = nodeNameUtf16Length > MaxNameLengthForStackAlloc
					? _globals.HeapPool.Borrow<char>(nodeNameUtf16Length)
					: (PooledHeapMemory<char>?) null;
				var nodeNameBuffer = nodeNameHeapBuffer.HasValue ? nodeNameHeapBuffer.Value.Buffer : stackNameBuffer;
				_animationAndNodeNameBuffer.ConvertToUtf16(nodeNameBuffer);
				_meshBuilder.SetSkeletonNodeName(mesh, i, nodeNameBuffer[..nodeNameUtf16Length]);
			}
		}
	}

	void LoadAndAttachMeshAnimations(UIntPtr assetHandle, NodeHandle* nodeHandleBuffer, int nodeHandleBufferCount, float? animTicksPerSecOverride, Mesh mesh) {
		const int MaxNameLengthForStackAlloc = 2048;
		
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

			if (nameLenBytes > _animationAndNodeNameBuffer.Length) {
				throw new InvalidOperationException($"Animation name length too long; increase {nameof(LocalAssetLoaderConfig.MaxAnimationAndNodeNameLengthChars)} in {nameof(LocalAssetLoaderConfig)}!");
			}
			CopyLoadedAssetSkeletalAnimationName(
				assetHandle,
				a,
				_animationAndNodeNameBuffer.AsPointer,
				_animationAndNodeNameBuffer.Length
			).ThrowIfFailure();
			
			var animNameUtf16Length = _animationAndNodeNameBuffer.GetUtf16Length();
			using var animNameHeapBuffer = animNameUtf16Length > MaxNameLengthForStackAlloc
				? _globals.HeapPool.Borrow<char>(animNameUtf16Length)
				: (PooledHeapMemory<char>?) null;
			var animNameBuffer = animNameHeapBuffer.HasValue ? animNameHeapBuffer.Value.Buffer : stackNameBuffer;
			if (animNameUtf16Length == 0) {
				const string FallbackAnimationNamePrefix = "anim_";
				FallbackAnimationNamePrefix.CopyTo(animNameBuffer);
				if (a.TryFormat(animNameBuffer[FallbackAnimationNamePrefix.Length..], out var additionalCharsWritten, default, null)) {
					animNameUtf16Length = FallbackAnimationNamePrefix.Length + additionalCharsWritten;
				}
				else animNameUtf16Length = FallbackAnimationNamePrefix.Length;
			}
			else _animationAndNodeNameBuffer.ConvertToUtf16(animNameBuffer);

			var totalTranslationKeyframes = 0;
			var totalRotationKeyframes = 0;
			var totalScalingKeyframes = 0;
			var highestSingleChannelScalingKeyframeCount = 0;
			var highestSingleChannelRotationKeyframeCount = 0;
			var highestSingleChannelTranslationKeyframeCount = 0;
			var mutations = _globals.HeapPool.Borrow<SkeletalAnimationNodeMutationDescriptor>(animationChannelCount);
			var channelsToInclude = _globals.HeapPool.Borrow<int>(animationChannelCount);
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
				
				channelsToInclude.Buffer[numChannelsIncluded] = c;
				mutations.Buffer[numChannelsIncluded] = new SkeletalAnimationNodeMutationDescriptor(
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

			var scalingKeyframes = _globals.HeapPool.Borrow<SkeletalAnimationScalingKeyframe>(totalScalingKeyframes);
			var rotationKeyframes = _globals.HeapPool.Borrow<SkeletalAnimationRotationKeyframe>(totalRotationKeyframes);
			var translationKeyframes = _globals.HeapPool.Borrow<SkeletalAnimationTranslationKeyframe>(totalTranslationKeyframes);
			var scalingVectorBuffer = _skeletalAnimationKeyframeDataPool.Rent<Vector3>(highestSingleChannelScalingKeyframeCount);
			var scalingTimeCodeBuffer = _skeletalAnimationKeyframeDataPool.Rent<float>(highestSingleChannelScalingKeyframeCount);
			var rotationQuaternionBuffer = _skeletalAnimationKeyframeDataPool.Rent<Quaternion>(highestSingleChannelRotationKeyframeCount);
			var rotationTimeCodeBuffer = _skeletalAnimationKeyframeDataPool.Rent<float>(highestSingleChannelRotationKeyframeCount);
			var translationVectorBuffer = _skeletalAnimationKeyframeDataPool.Rent<Vector3>(highestSingleChannelTranslationKeyframeCount);
			var translationTimeCodeBuffer = _skeletalAnimationKeyframeDataPool.Rent<float>(highestSingleChannelTranslationKeyframeCount);

			try {
				for (var i = 0; i < numChannelsIncluded; ++i) {
					var c = channelsToInclude.Buffer[i];
					CopyLoadedAssetSkeletalAnimationChannelData(
						assetHandle,
						a, 
						c,
						animTicksPerSecOverride ?? 0f,
						(Vector3*) scalingVectorBuffer.StartPtr,
						(float*) scalingTimeCodeBuffer.StartPtr,
						highestSingleChannelScalingKeyframeCount,
						(Quaternion*) rotationQuaternionBuffer.StartPtr,
						(float*) rotationTimeCodeBuffer.StartPtr,
						highestSingleChannelRotationKeyframeCount,
						(Vector3*) translationVectorBuffer.StartPtr,
						(float*) translationTimeCodeBuffer.StartPtr,
						highestSingleChannelTranslationKeyframeCount
					).ThrowIfFailure();

					for (var s = 0; s < mutations.Buffer[i].ScalingKeyframeCount; ++s) {
						scalingKeyframes.Buffer[mutations.Buffer[i].ScalingKeyframeStartIndex + s] = new SkeletalAnimationScalingKeyframe(
							scalingTimeCodeBuffer.AsReadOnlySpan<float>()[s],
							Vect.FromVector3(scalingVectorBuffer.AsReadOnlySpan<Vector3>()[s])
						);
					}
					for (var r = 0; r < mutations.Buffer[i].RotationKeyframeCount; ++r) {
						rotationKeyframes.Buffer[mutations.Buffer[i].RotationKeyframeStartIndex + r] = new SkeletalAnimationRotationKeyframe(
							rotationTimeCodeBuffer.AsReadOnlySpan<float>()[r],
							rotationQuaternionBuffer.AsReadOnlySpan<Quaternion>()[r]
						);
					}
					for (var t = 0; t < mutations.Buffer[i].TranslationKeyframeCount; ++t) {
						translationKeyframes.Buffer[mutations.Buffer[i].TranslationKeyframeStartIndex + t] = new SkeletalAnimationTranslationKeyframe(
							translationTimeCodeBuffer.AsReadOnlySpan<float>()[t],
							Vect.FromVector3(translationVectorBuffer.AsReadOnlySpan<Vector3>()[t])
						);
					}
				}
				
				if (numChannelsIncluded != animationChannelCount) {
					var newMutationsBuffer = _globals.HeapPool.Borrow<SkeletalAnimationNodeMutationDescriptor>(numChannelsIncluded);
					mutations.Buffer[..numChannelsIncluded].CopyTo(newMutationsBuffer.Buffer);
					mutations.Dispose();
					mutations = newMutationsBuffer;
				}

				_meshBuilder.AttachAnimationAndTransferBufferOwnership(
					mesh,
					scalingKeyframes,
					rotationKeyframes,
					translationKeyframes,
					mutations,
					animationDurationSeconds,
					animNameBuffer[..animNameUtf16Length]
				);
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
				_skeletalAnimationKeyframeDataPool.Return(scalingVectorBuffer);
				_skeletalAnimationKeyframeDataPool.Return(scalingTimeCodeBuffer);
				_skeletalAnimationKeyframeDataPool.Return(rotationQuaternionBuffer);
				_skeletalAnimationKeyframeDataPool.Return(rotationTimeCodeBuffer);
				_skeletalAnimationKeyframeDataPool.Return(translationVectorBuffer);
				_skeletalAnimationKeyframeDataPool.Return(translationTimeCodeBuffer);
			}
		}
	}

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