// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.IO;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Local;

sealed unsafe class LocalAssetLoader : IAssetLoader, IDisposable {
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly LocalMeshBuilder _meshBuilder;
	readonly LocalMaterialBuilder _materialBuilder;
	readonly InteropStringBuffer _assetFilePathBuffer;
	readonly FixedByteBufferPool _vertexTriangleBufferPool;
	bool _isDisposed = false;

	public IMeshBuilder MeshBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _meshBuilder;
	public IMaterialBuilder MaterialBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _materialBuilder;

	public LocalAssetLoader(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config) {
		ArgumentNullException.ThrowIfNull(globals);
		ArgumentNullException.ThrowIfNull(config);

		_globals = globals;
		_meshBuilder = new LocalMeshBuilder(globals);
		_materialBuilder = new LocalMaterialBuilder(globals, config);
		_assetFilePathBuffer = new InteropStringBuffer(config.MaxAssetFilePathLengthChars, addOneForNullTerminator: true);
		_vertexTriangleBufferPool = new FixedByteBufferPool(config.MaxAssetVertexIndexBufferSizeBytes);
	}

	#region Textures
	public Texture LoadTexture(in TextureReadConfig readConfig, in TextureCreationConfig config) {
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();
		config.ThrowIfInvalid();

		try {
			_assetFilePathBuffer.ConvertFromUtf16(readConfig.FilePath);
			LoadTextureFileInToMemory(
				in _assetFilePathBuffer.BufferRef,
				readConfig.IncludeWAlphaChannel,
				out var width,
				out var height,
				out var texelBuffer
			).ThrowIfFailure();

			try {
				if (width < 0 || height < 0) throw new InvalidOperationException($"Loaded texture had width/height of {width}/{height}.");
				var texelCount = width * height;

				if (readConfig.IncludeWAlphaChannel) {
					return _materialBuilder.CreateTexture(
						new ReadOnlySpan<TexelRgba32>(texelBuffer, texelCount),
						new() { Height = height, Width = width },
						config
					);
				}
				else {
					return _materialBuilder.CreateTexture(
						new ReadOnlySpan<TexelRgb24>(texelBuffer, texelCount),
						new() { Height = height, Width = width },
						config
					);
				}
			}
			finally {
				UnloadTextureFileFromMemory(texelBuffer).ThrowIfFailure();
			}
		}
		catch (Exception e) {
			if (!File.Exists(readConfig.FilePath.ToString())) throw new InvalidOperationException($"File '{readConfig.FilePath}' does not exist.", e);
			else throw;
		}
	}
	public TextureReadMetadata ReadTextureMetadata(in TextureReadConfig readConfig) {
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();

		try {
			_assetFilePathBuffer.ConvertFromUtf16(readConfig.FilePath);
			LoadTextureFileInToMemory(
				in _assetFilePathBuffer.BufferRef,
				readConfig.IncludeWAlphaChannel,
				out var width,
				out var height,
				out var texelBuffer
			).ThrowIfFailure();

			try {
				return new(width, height);
			}
			finally {
				UnloadTextureFileFromMemory(texelBuffer).ThrowIfFailure();
			}
		}
		catch (Exception e) {
			if (!File.Exists(readConfig.FilePath.ToString())) throw new InvalidOperationException($"File '{readConfig.FilePath}' does not exist.", e);
			else throw;
		}
	}
	public void ReadTexture<TTexel>(in TextureReadConfig readConfig, Span<TTexel> destinationBuffer) where TTexel : unmanaged, ITexel<TTexel> {
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();

		var includeWChannel = TTexel.BlitType switch {
			TexelType.Rgb24 => false,
			TexelType.Rgba32 => true,
			_ => throw new ArgumentOutOfRangeException(nameof(TTexel), "Unknown texel blit type.")
		};

		try {
			_assetFilePathBuffer.ConvertFromUtf16(readConfig.FilePath);
			LoadTextureFileInToMemory(
				in _assetFilePathBuffer.BufferRef,
				includeWChannel,
				out var width,
				out var height,
				out var texelBuffer
			).ThrowIfFailure();

			try {
				if (width < 0 || height < 0) throw new InvalidOperationException($"Loaded texture had width/height of {width}/{height}.");
				var texelCount = width * height;

				if (destinationBuffer.Length < texelCount) {
					throw new ArgumentException($"Given destination buffer size ({destinationBuffer.Length}) is too small to accomodate texture data ({texelCount} texels).");
				}

				var destinationBufferAsBytes = MemoryMarshal.AsBytes(destinationBuffer);
				if (includeWChannel) {
					MemoryMarshal.AsBytes(new ReadOnlySpan<TexelRgba32>(texelBuffer, texelCount)).CopyTo(destinationBufferAsBytes);
				}
				else {
					MemoryMarshal.AsBytes(new ReadOnlySpan<TexelRgb24>(texelBuffer, texelCount)).CopyTo(destinationBufferAsBytes);
				}
			}
			finally {
				UnloadTextureFileFromMemory(texelBuffer).ThrowIfFailure();
			}
		}
		catch (Exception e) {
			if (!File.Exists(readConfig.FilePath.ToString())) throw new InvalidOperationException($"File '{readConfig.FilePath}' does not exist.", e);
			else throw;
		}
	}

	public Texture LoadAndCombineOrmTextures(in TextureReadConfig occlusionMapReadConfig = default, in TextureReadConfig roughnessMapReadConfig = default, in TextureReadConfig metallicMapReadConfig = default, in TextureCreationConfig config = default) {
		var defaultOcclusion = occlusionMapReadConfig.FilePath.IsEmpty;
		var defaultRoughness = roughnessMapReadConfig.FilePath.IsEmpty;
		var defaultMetallic = metallicMapReadConfig.FilePath.IsEmpty;
		
		var dimensions = (XYPair<int>?) null;

		if (!defaultOcclusion) {
			var metadata = ReadTextureMetadata(occlusionMapReadConfig);
			if (dimensions == null) dimensions = (metadata.Width, metadata.Height);
			else if (dimensions.Value.X != metadata.Width || dimensions.Value.Y != metadata.Height) {
				throw new InvalidOperationException("All given textures must have identical dimensions.");
			}
		}
		if (!defaultRoughness) {
			var metadata = ReadTextureMetadata(roughnessMapReadConfig);
			if (dimensions == null) dimensions = (metadata.Width, metadata.Height);
			else if (dimensions.Value.X != metadata.Width || dimensions.Value.Y != metadata.Height) {
				throw new InvalidOperationException("All given textures must have identical dimensions.");
			}
		}
		if (!defaultMetallic) {
			var metadata = ReadTextureMetadata(metallicMapReadConfig);
			if (dimensions == null) dimensions = (metadata.Width, metadata.Height);
			else if (dimensions.Value.X != metadata.Width || dimensions.Value.Y != metadata.Height) {
				throw new InvalidOperationException("All given textures must have identical dimensions.");
			}
		}

		if (dimensions == null) return _materialBuilder.DefaultOrmMap;

		var resultBuffer = _globals.HeapPool.Borrow<TexelRgb24>(dimensions.Value.Area);
		var readBuffer = _globals.HeapPool.Borrow<TexelRgb24>(dimensions.Value.Area);

		try {
			IMaterialBuilder.DefaultTexelOrm.ToRgb24(out var o, out var r, out var m);
			
			if (defaultOcclusion) {
				for (var i = 0; i < resultBuffer.Buffer.Length; ++i) resultBuffer.Buffer[i] = resultBuffer.Buffer[i] with { R = o };
			}
			else {
				ReadTexture(occlusionMapReadConfig, readBuffer.Buffer);
				for (var i = 0; i < resultBuffer.Buffer.Length; ++i) resultBuffer.Buffer[i] = resultBuffer.Buffer[i] with { R = readBuffer.Buffer[i].R };
			}

			if (defaultRoughness) {
				for (var i = 0; i < resultBuffer.Buffer.Length; ++i) resultBuffer.Buffer[i] = resultBuffer.Buffer[i] with { G = r };
			}
			else {
				ReadTexture(roughnessMapReadConfig, readBuffer.Buffer);
				for (var i = 0; i < resultBuffer.Buffer.Length; ++i) resultBuffer.Buffer[i] = resultBuffer.Buffer[i] with { G = readBuffer.Buffer[i].R };
			}

			if (defaultMetallic) {
				for (var i = 0; i < resultBuffer.Buffer.Length; ++i) resultBuffer.Buffer[i] = resultBuffer.Buffer[i] with { B = m };
			}
			else {
				ReadTexture(metallicMapReadConfig, readBuffer.Buffer);
				for (var i = 0; i < resultBuffer.Buffer.Length; ++i) resultBuffer.Buffer[i] = resultBuffer.Buffer[i] with { B = readBuffer.Buffer[i].R };
			}

			return _materialBuilder.CreateTexture(
				(ReadOnlySpan<TexelRgb24>) resultBuffer.Buffer,
				new TextureGenerationConfig { Height = dimensions.Value.X, Width = dimensions.Value.Y },
				config
			);
		}
		finally {
			readBuffer.Dispose();
			resultBuffer.Dispose();
		}
	}
	#endregion

	#region Meshes
	public Mesh LoadMesh(in MeshReadConfig readConfig, in MeshCreationConfig config) {
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();
		config.ThrowIfInvalid();

		try {
			_assetFilePathBuffer.ConvertFromUtf16(readConfig.FilePath);
			LoadAssetFileInToMemory(
				in _assetFilePathBuffer.BufferRef,
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
							CopyLoadedAssetMeshVertices(assetHandle, i, (int) (fixedVertexBuffer.Size<MeshVertex>() - (vBufferPtr - (MeshVertex*) fixedVertexBuffer.StartPtr)), vBufferPtr);
							CopyLoadedAssetMeshTriangles(assetHandle, i, (int) (fixedTriangleBuffer.Size<VertexTriangle>() - (tBufferPtr - (VertexTriangle*) fixedTriangleBuffer.StartPtr)), tBufferPtr);
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
			if (!File.Exists(readConfig.FilePath.ToString())) throw new InvalidOperationException($"File '{readConfig.FilePath}' does not exist.", e);
			else throw;
		}
	}
	public MeshReadMetadata ReadMeshMetadata(in MeshReadConfig readConfig) {
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();

		try {
			_assetFilePathBuffer.ConvertFromUtf16(readConfig.FilePath);
			LoadAssetFileInToMemory(
				in _assetFilePathBuffer.BufferRef,
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
			if (!File.Exists(readConfig.FilePath.ToString())) throw new InvalidOperationException($"File '{readConfig.FilePath}' does not exist.", e);
			else throw;
		}
	}
	public void ReadMesh(in MeshReadConfig readConfig, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer) {
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();

		try {
			_assetFilePathBuffer.ConvertFromUtf16(readConfig.FilePath);
			LoadAssetFileInToMemory(
				in _assetFilePathBuffer.BufferRef,
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
							CopyLoadedAssetMeshVertices(assetHandle, i, (int) (fixedVertexBuffer.Size<MeshVertex>() - (vBufferPtr - (MeshVertex*) fixedVertexBuffer.StartPtr)), vBufferPtr);
							CopyLoadedAssetMeshTriangles(assetHandle, i, (int) (fixedTriangleBuffer.Size<VertexTriangle>() - (tBufferPtr - (VertexTriangle*) fixedTriangleBuffer.StartPtr)), tBufferPtr);
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
				}
			}
			finally {
				UnloadAssetFileFromMemory(assetHandle).ThrowIfFailure();
			}
		}
		catch (Exception e) {
			if (!File.Exists(readConfig.FilePath.ToString())) throw new InvalidOperationException($"File '{readConfig.FilePath}' does not exist.", e);
			else throw;
		}
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_count")]
	static extern InteropResult GetLoadedAssetMeshCount(
		UIntPtr assetHandle,
		out int outMeshCount
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "copy_loaded_asset_mesh_triangles")]
	static extern InteropResult CopyLoadedAssetMeshTriangles(
		UIntPtr assetHandle,
		int meshIndex,
		int bufferSizeTriangles,
		VertexTriangle* triangleBufferPtr
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "unload_asset_file_from_memory")]
	static extern InteropResult UnloadAssetFileFromMemory(
		UIntPtr assetHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "load_texture_file_in_to_memory")]
	static extern InteropResult LoadTextureFileInToMemory(
		ref readonly byte utf8FileNameBufferPtr,
		InteropBool includeWAlphaChannel,
		out int outWidth,
		out int outHeight,
		out void* outTexelBufferPtr
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "unload_texture_file_from_memory")]
	static extern InteropResult UnloadTextureFileFromMemory(
		void* texelBufferPtr
	);
	#endregion

	public override string ToString() => _isDisposed ? "TinyFFR Local Asset Loader [Disposed]" : "TinyFFR Local Asset Loader";

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			_vertexTriangleBufferPool.Dispose();
			_assetFilePathBuffer.Dispose();
			_meshBuilder.Dispose();
			_materialBuilder.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(IAssetLoader));
	}
	#endregion
}