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
	
	readonly LocalMeshBuilder _meshBuilder;
	readonly FixedByteBufferPool _vertexTriangleBufferPool;
	readonly FixedByteBufferPool _boneDataBufferPool;
	
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
				GetLoadedAssetMeshBoneCount(assetHandle, 0, out var boneCount).ThrowIfFailure();
				var loadSkeletalAnimationData = boneCount > 0 && readConfig.LoadSkeletalAnimationDataIfPresent;
				if (loadSkeletalAnimationData && boneCount > IMeshBuilder.MaxSkeletalBoneCount) {
					Console.WriteLine($"Can not load skeletal animation data for file '{filePath}' as its bone count ({boneCount}) is higher than the maximum TinyFFR supports ({IMeshBuilder.MaxSkeletalBoneCount}).");
					loadSkeletalAnimationData = false;
				}
				else if (loadSkeletalAnimationData && metadata.SubMeshCount != 1) {
					Console.WriteLine($"Can not load skeletal animation data for file '{filePath}' as it contains multiple sub-meshes " +
						$"(TinyFFR can not currently amalgamate multi-mesh animations in to a single object; use {nameof(LoadAll)}(...) instead).");
					loadSkeletalAnimationData = false;
				}
				var copyResult = loadSkeletalAnimationData
					? CopyMeshDataFromAsset<MeshVertexSkeletal>(assetHandle, metadata, in readConfig)
					: CopyMeshDataFromAsset<MeshVertex>(assetHandle, metadata, in readConfig);

				try {
					if (loadSkeletalAnimationData) {
						var parentIndicesBuffer = _boneDataBufferPool.Rent<int>(boneCount);
						var bindPoseInversionMatricesBuffer = _boneDataBufferPool.Rent<Matrix4x4>(boneCount);
						var defaultLocalTransformsBuffer = _boneDataBufferPool.Rent<Matrix4x4>(boneCount);

						try {
							GetLoadedAssetMeshBoneHierarchy(
								assetHandle, 
								0, 
								(int*) parentIndicesBuffer.StartPtr,
								(Matrix4x4*) bindPoseInversionMatricesBuffer.StartPtr, 
								(Matrix4x4*) defaultLocalTransformsBuffer.StartPtr, 
								boneCount
							).ThrowIfFailure();

							var result = _meshBuilder.CreateMesh(
								copyResult.VertexBuffer.AsReadOnlySpan<MeshVertexSkeletal>(copyResult.NumVerticesWritten),
								copyResult.TriangleBuffer.AsReadOnlySpan<VertexTriangle>(copyResult.NumTrianglesWritten),
								parentIndicesBuffer.AsReadOnlySpan<int>(boneCount), 
								bindPoseInversionMatricesBuffer.AsReadOnlySpan<Matrix4x4>(boneCount), 
								defaultLocalTransformsBuffer.AsReadOnlySpan<Matrix4x4>(boneCount),
								config
							);
						
							LoadAndAttachMeshAnimations(assetHandle, 0, boneCount, result);
							return result;
						}
						finally {
							_boneDataBufferPool.Return(parentIndicesBuffer);
							_boneDataBufferPool.Return(bindPoseInversionMatricesBuffer);
							_boneDataBufferPool.Return(defaultLocalTransformsBuffer);
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

	void LoadAndAttachMeshAnimations(UIntPtr assetHandle, int meshIndex, int boneCount, Mesh mesh) {
		if (boneCount <= 0) return;
		GetLoadedAssetAnimationCount(assetHandle, out var animCount).ThrowIfFailure();
		if (animCount <= 0) return;

		for (var animIdx = 0; animIdx < animCount; ++animIdx) {
			GetLoadedAssetAnimationData(assetHandle, animIdx, out var nameLen, out var durationSeconds, out var channelCount).ThrowIfFailure();

			Span<byte> nameUtf8 = nameLen <= 512 ? stackalloc byte[nameLen] : new byte[nameLen];
			fixed (byte* namePtr = nameUtf8) {
				CopyLoadedAssetAnimationName(assetHandle, animIdx, namePtr, nameLen).ThrowIfFailure();
			}
			Span<char> nameChars = nameLen <= 512 ? stackalloc char[nameLen] : new char[nameLen];
			var nameCharCount = Encoding.UTF8.GetChars(nameUtf8, nameChars);
			var name = nameChars[..nameCharCount];

			GetLoadedAssetAnimationTicksPerSecond(assetHandle, animIdx, out var tps).ThrowIfFailure();
			var ticksToSecondsMultiplier = tps > 0f ? 1f / tps : 1f / 25f;

			var totalPosKeys = 0;
			var totalRotKeys = 0;
			var totalScaleKeys = 0;
			var validChannelCount = 0;

			Span<AnimationChannelHeader> headers = channelCount <= 128 ? stackalloc AnimationChannelHeader[channelCount] : new AnimationChannelHeader[channelCount];

			for (var chIdx = 0; chIdx < channelCount; ++chIdx) {
				GetLoadedAssetAnimationChannelData(assetHandle, animIdx, chIdx, meshIndex,
					out var boneIdx, out var posKeyCount, out var rotKeyCount, out var scaleKeyCount).ThrowIfFailure();

				if (boneIdx < 0) continue;

				headers[validChannelCount] = new AnimationChannelHeader(
					boneIdx,
					totalPosKeys, posKeyCount,
					totalRotKeys, rotKeyCount,
					totalScaleKeys, scaleKeyCount
				);
				++validChannelCount;
				totalPosKeys += posKeyCount;
				totalRotKeys += rotKeyCount;
				totalScaleKeys += scaleKeyCount;
			}

			headers = headers[..validChannelCount];

			using var posKeysBorrow = _globals.HeapPool.Borrow<AnimationVectorKeyframe>(totalPosKeys);
			using var rotKeysBorrow = _globals.HeapPool.Borrow<AnimationQuaternionKeyframe>(totalRotKeys);
			using var scaleKeysBorrow = _globals.HeapPool.Borrow<AnimationVectorKeyframe>(totalScaleKeys);

			var posKeysSpan = posKeysBorrow.Buffer;
			var rotKeysSpan = rotKeysBorrow.Buffer;
			var scaleKeysSpan = scaleKeysBorrow.Buffer;

			var posOffset = 0;
			var rotOffset = 0;
			var scaleOffset = 0;
			for (var chIdx = 0; chIdx < channelCount; ++chIdx) {
				GetLoadedAssetAnimationChannelData(assetHandle, animIdx, chIdx, meshIndex,
					out var boneIdx, out var posKeyCount, out var rotKeyCount, out var scaleKeyCount).ThrowIfFailure();

				if (boneIdx < 0) continue;

				fixed (AnimationVectorKeyframe* posPtr = posKeysSpan[posOffset..]) {
					CopyLoadedAssetAnimationChannelPositionKeys(assetHandle, animIdx, chIdx, ticksToSecondsMultiplier, posPtr, posKeyCount).ThrowIfFailure();
				}
				fixed (AnimationQuaternionKeyframe* rotPtr = rotKeysSpan[rotOffset..]) {
					CopyLoadedAssetAnimationChannelRotationKeys(assetHandle, animIdx, chIdx, ticksToSecondsMultiplier, rotPtr, rotKeyCount).ThrowIfFailure();
				}
				fixed (AnimationVectorKeyframe* scalePtr = scaleKeysSpan[scaleOffset..]) {
					CopyLoadedAssetAnimationChannelScalingKeys(assetHandle, animIdx, chIdx, ticksToSecondsMultiplier, scalePtr, scaleKeyCount).ThrowIfFailure();
				}

				posOffset += posKeyCount;
				rotOffset += rotKeyCount;
				scaleOffset += scaleKeyCount;
			}

			_meshBuilder.AttachAnimation(mesh, name, durationSeconds,
				headers, posKeysSpan, rotKeysSpan, scaleKeysSpan);
		}
	}

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_count")]
	static extern InteropResult GetLoadedAssetMeshCount(
		UIntPtr assetHandle,
		out int outMeshCount
	);
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_animation_count")]
	static extern InteropResult GetLoadedAssetAnimationCount(
		UIntPtr assetHandle,
		out int outAnimationCount
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_bone_count")]
	static extern InteropResult GetLoadedAssetMeshBoneCount(
		UIntPtr assetHandle,
		int meshIndex,
		out int boneCount
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "copy_loaded_asset_mesh_triangles")]
	static extern InteropResult CopyLoadedAssetMeshTriangles(
		UIntPtr assetHandle,
		int meshIndex,
		InteropBool correctFlippedOrientation,
		int bufferSizeTriangles,
		VertexTriangle* triangleBufferPtr
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_animation_data")]
	static extern InteropResult GetLoadedAssetAnimationData(
		UIntPtr assetHandle, int animIndex,
		out int outNameLengthBytes, out float outDurationSeconds, out int outChannelCount
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_animation_ticks_per_second")]
	static extern InteropResult GetLoadedAssetAnimationTicksPerSecond(
		UIntPtr assetHandle, int animIndex,
		out float outTicksPerSecond
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "copy_loaded_asset_animation_name")]
	static extern InteropResult CopyLoadedAssetAnimationName(
		UIntPtr assetHandle, int animIndex,
		byte* nameBuffer, int bufferLength
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_animation_channel_data")]
	static extern InteropResult GetLoadedAssetAnimationChannelData(
		UIntPtr assetHandle, int animIndex, int channelIndex, int meshIndex,
		out int outBoneIndex, out int outPosKeyCount, out int outRotKeyCount, out int outScaleKeyCount
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "copy_loaded_asset_animation_channel_position_keys")]
	static extern InteropResult CopyLoadedAssetAnimationChannelPositionKeys(
		UIntPtr assetHandle, int animIndex, int channelIndex,
		float ticksToSecondsMultiplier, AnimationVectorKeyframe* buffer, int bufferSize
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "copy_loaded_asset_animation_channel_rotation_keys")]
	static extern InteropResult CopyLoadedAssetAnimationChannelRotationKeys(
		UIntPtr assetHandle, int animIndex, int channelIndex,
		float ticksToSecondsMultiplier, AnimationQuaternionKeyframe* buffer, int bufferSize
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "copy_loaded_asset_animation_channel_scaling_keys")]
	static extern InteropResult CopyLoadedAssetAnimationChannelScalingKeys(
		UIntPtr assetHandle, int animIndex, int channelIndex,
		float ticksToSecondsMultiplier, AnimationVectorKeyframe* buffer, int bufferSize
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_bone_hierarchy")]
	static extern InteropResult GetLoadedAssetMeshBoneHierarchy(
		UIntPtr assetHandle, int meshIndex,
		int* parentIndicesBuffer, Matrix4x4* inverseBindPoseBuffer, Matrix4x4* defaultLocalTransformBuffer, int boneCount
	);
	#endregion
}