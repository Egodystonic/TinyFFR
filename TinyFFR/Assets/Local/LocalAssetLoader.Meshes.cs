// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.IO;
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
				var metadata = GetMeshMetadataFromOpenedFile(assetHandle);
				var useSkeletalVertices = metadata.TotalBoneCount > 0 && readConfig.LoadSkeletalDataIfPresent;
				var copyResult = useSkeletalVertices
					? CopyMeshDataFromAsset<MeshVertexSkeletal>(assetHandle, metadata, in readConfig)
					: CopyMeshDataFromAsset<MeshVertex>(assetHandle, metadata, in readConfig);
				
				try {
					if (useSkeletalVertices) {
						return _meshBuilder.CreateMesh(
							copyResult.VertexBuffer.AsReadOnlySpan<MeshVertexSkeletal>(copyResult.NumVerticesWritten),
							copyResult.TriangleBuffer.AsReadOnlySpan<VertexTriangle>(copyResult.NumTrianglesWritten),
							config
						);
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
				return GetMeshMetadataFromOpenedFile(assetHandle);
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
	
	static MeshReadMetadata GetMeshMetadataFromOpenedFile(UIntPtr assetHandle) {
		GetLoadedAssetMeshCount(assetHandle, out var meshCount).ThrowIfFailure();

		checked {
			var totalVertexCount = 0;
			var totalTriangleCount = 0;
			var totalBoneCount = 0;

			for (var i = 0; i < meshCount; ++i) {
				GetLoadedAssetMeshVertexCount(assetHandle, i, out var vCount).ThrowIfFailure();
				GetLoadedAssetMeshTriangleCount(assetHandle, i, out var tCount).ThrowIfFailure();
				GetLoadedAssetMeshBoneCount(assetHandle, i, out var bCount).ThrowIfFailure();
				totalVertexCount += vCount;
				totalTriangleCount += tCount;
				totalBoneCount += bCount;
			}

			return new(totalVertexCount, totalTriangleCount, totalBoneCount, meshCount);
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
			
			var metadata = GetMeshMetadataFromOpenedFile(assetHandle);

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
					CopyLoadedAssetMeshVertices(assetHandle, i, correctFlippedOrientation, (int) (vertexBuffer.Size<MeshVertex>() - (vBufferPtr - (MeshVertex*) vertexBuffer.StartPtr)), vBufferPtr);
					CopyLoadedAssetMeshTriangles(assetHandle, i, correctFlippedOrientation, (int) (triangleBuffer.Size<VertexTriangle>() - (tBufferPtr - (VertexTriangle*) triangleBuffer.StartPtr)), tBufferPtr);
					vBufferPtr += vCount;
					tBufferPtr += tCount;
				}
			}
			else if (typeof(TVertex) == typeof(MeshVertex)) {
				var vBufferPtr = (MeshVertexSkeletal*) vertexBuffer.StartPtr;
		
				for (var i = 0; i < metadata.SubMeshCount; ++i) {
					GetLoadedAssetMeshVertexCount(assetHandle, i, out var vCount).ThrowIfFailure();
					GetLoadedAssetMeshTriangleCount(assetHandle, i, out var tCount).ThrowIfFailure();
					CopyLoadedAssetMeshSkeletalVertices(assetHandle, i, correctFlippedOrientation, (int) (vertexBuffer.Size<MeshVertexSkeletal>() - (vBufferPtr - (MeshVertexSkeletal*) vertexBuffer.StartPtr)), vBufferPtr);
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
			CopyLoadedAssetMeshVertices(assetHandle, subMeshIndex, correctFlippedOrientation, vertexBuffer.Size<MeshVertex>(), (MeshVertex*) vertexBuffer.StartPtr);
		}
		else if (typeof(TVertex) == typeof(MeshVertexSkeletal)) {
			CopyLoadedAssetMeshSkeletalVertices(assetHandle, subMeshIndex, correctFlippedOrientation, vertexBuffer.Size<MeshVertexSkeletal>(), (MeshVertexSkeletal*) vertexBuffer.StartPtr);
		}
		else {
			throw new InvalidOperationException($"Unknown vertex type '{typeof(TVertex)}'.");
		}
		
		return new(vertexBuffer, subMeshVertexCount, triangleBuffer, subMeshTriangleCount);
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
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "copy_loaded_asset_mesh_vertices")]
	static extern InteropResult CopyLoadedAssetMeshVertices(
		UIntPtr assetHandle,
		int meshIndex,
		InteropBool correctFlippedOrientation,
		int bufferSizeVertices,
		MeshVertex* vertexBufferPtr
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "copy_loaded_asset_mesh_skeletal_vertices")]
	static extern InteropResult CopyLoadedAssetMeshSkeletalVertices(
		UIntPtr assetHandle,
		int meshIndex,
		InteropBool correctFlippedOrientation,
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
	#endregion
}