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

					var fixedVertexBuffer = _vertexTriangleBufferPool.Rent<MeshVertex>(totalVertexCount);
					var fixedTriangleBuffer = _vertexTriangleBufferPool.Rent<VertexTriangle>(totalTriangleCount);

					try {
						var vBufferPtr = (MeshVertex*) fixedVertexBuffer.StartPtr;
						var tBufferPtr = (VertexTriangle*) fixedTriangleBuffer.StartPtr;

						for (var i = 0; i < meshCount; ++i) {
							GetLoadedAssetMeshVertexCount(assetHandle, i, out var vCount).ThrowIfFailure();
							GetLoadedAssetMeshTriangleCount(assetHandle, i, out var tCount).ThrowIfFailure();
							CopyLoadedAssetMeshVertices(assetHandle, i, readConfig.CorrectFlippedOrientation, (int) (fixedVertexBuffer.Size<MeshVertex>() - (vBufferPtr - (MeshVertex*) fixedVertexBuffer.StartPtr)), vBufferPtr);
							CopyLoadedAssetMeshTriangles(assetHandle, i, readConfig.CorrectFlippedOrientation, (int) (fixedTriangleBuffer.Size<VertexTriangle>() - (tBufferPtr - (VertexTriangle*) fixedTriangleBuffer.StartPtr)), tBufferPtr);
							vBufferPtr += vCount;
							tBufferPtr += tCount;
						}

						return _meshBuilder.CreateMesh(
							fixedVertexBuffer.AsReadOnlySpan<MeshVertex>(totalVertexCount),
							fixedTriangleBuffer.AsReadOnlySpan<VertexTriangle>(totalTriangleCount),
							config
						);
					}
					finally {
						_vertexTriangleBufferPool.Return(fixedVertexBuffer);
						_vertexTriangleBufferPool.Return(fixedTriangleBuffer);
					}
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

					return new(totalVertexCount, totalTriangleCount);
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
	public MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer, in MeshReadConfig readConfig) {
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

					if (vertexBuffer.Length < totalVertexCount) {
						throw new ArgumentException($"Given vertex buffer size ({vertexBuffer.Length}) is too small to accomodate mesh data ({totalVertexCount} vertices).");
					}
					if (triangleBuffer.Length < totalTriangleCount) {
						throw new ArgumentException($"Given triangle buffer size ({triangleBuffer.Length}) is too small to accomodate mesh data ({totalTriangleCount} triangles).");
					}

					var fixedVertexBuffer = _vertexTriangleBufferPool.Rent<MeshVertex>(totalVertexCount);
					var fixedTriangleBuffer = _vertexTriangleBufferPool.Rent<VertexTriangle>(totalTriangleCount);

					try {
						var vBufferPtr = (MeshVertex*) fixedVertexBuffer.StartPtr;
						var tBufferPtr = (VertexTriangle*) fixedTriangleBuffer.StartPtr;

						for (var i = 0; i < meshCount; ++i) {
							GetLoadedAssetMeshVertexCount(assetHandle, i, out var vCount).ThrowIfFailure();
							GetLoadedAssetMeshTriangleCount(assetHandle, i, out var tCount).ThrowIfFailure();
							CopyLoadedAssetMeshVertices(assetHandle, i, readConfig.CorrectFlippedOrientation, (int) (fixedVertexBuffer.Size<MeshVertex>() - (vBufferPtr - (MeshVertex*) fixedVertexBuffer.StartPtr)), vBufferPtr);
							CopyLoadedAssetMeshTriangles(assetHandle, i, readConfig.CorrectFlippedOrientation, (int) (fixedTriangleBuffer.Size<VertexTriangle>() - (tBufferPtr - (VertexTriangle*) fixedTriangleBuffer.StartPtr)), tBufferPtr);
							vBufferPtr += vCount;
							tBufferPtr += tCount;
						}

						fixedVertexBuffer.AsReadOnlySpan<MeshVertex>(totalVertexCount).CopyTo(vertexBuffer);
						fixedTriangleBuffer.AsReadOnlySpan<VertexTriangle>(totalTriangleCount).CopyTo(triangleBuffer);
					}
					finally {
						_vertexTriangleBufferPool.Return(fixedVertexBuffer);
						_vertexTriangleBufferPool.Return(fixedTriangleBuffer);
					}
					return new(totalVertexCount, totalTriangleCount);
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