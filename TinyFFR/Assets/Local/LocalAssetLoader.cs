// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
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

sealed unsafe class LocalAssetLoader : ILocalAssetLoader, IEnvironmentCubemapImplProvider, IDisposable {
	readonly record struct CubemapData(UIntPtr SkyboxTextureHandle, UIntPtr IblTextureHandle);
	const string DefaultEnvironmentCubemapName = "Unnamed Environment Cubemap";
	const string HdrPreprocessorExeName = "cmgen.exe";
	const string HdrPreprocessorResourceName = "Assets.Local." + HdrPreprocessorExeName;
	const string HdrPreprocessedSkyboxFileSearch = "*_skybox.ktx";
	const string HdrPreprocessedIblFileSearch = "*_ibl.ktx";
	readonly string _hdrPreprocessorFilePath = Path.Combine(LocalFileSystemUtils.ApplicationDataDirectoryPath, HdrPreprocessorExeName);
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly LocalMeshBuilder _meshBuilder;
	readonly LocalMaterialBuilder _materialBuilder;
	readonly InteropStringBuffer _assetFilePathBuffer;
	readonly FixedByteBufferPool _vertexTriangleBufferPool;
	readonly FixedByteBufferPool _ktxFileBufferPool;
	readonly TimeSpan _maxHdrProcessingTime;
	readonly ArrayPoolBackedMap<ResourceHandle<EnvironmentCubemap>, CubemapData> _loadedCubemaps = new();
	nuint _prevCubemapHandle = 0;
	bool _isDisposed = false;
	bool _hdrPreprocessorHasBeenExtracted = false;

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
		_ktxFileBufferPool = new FixedByteBufferPool(config.MaxKtxFileBufferSizeBytes);
		_maxHdrProcessingTime = config.MaxHdrProcessingTime;
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
			var filePathAsStr = readConfig.FilePath.ToString();
			if (!File.Exists(filePathAsStr)) throw new InvalidOperationException($"File '{readConfig.FilePath}' does not exist (full path \"{Path.GetFullPath(filePathAsStr)}\").", e);
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
			dimensions = (metadata.Width, metadata.Height);
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

	#region Environment / Cubemap
	void ExtractHdrPreprocessorIfNecessary() {
		if (_hdrPreprocessorHasBeenExtracted) return;

		try {
			var data = EmbeddedResourceResolver.GetResource(HdrPreprocessorResourceName);
			File.WriteAllBytes(_hdrPreprocessorFilePath, data.AsSpan);
		}
		catch (Exception e) {
			throw new InvalidOperationException($"Could not extract HDR preprocessor executable ({HdrPreprocessorExeName}) " +
												$"to target directory ({LocalFileSystemUtils.ApplicationDataDirectoryPath}).", e);
		}

		_hdrPreprocessorHasBeenExtracted = true;
	}

	public void PreprocessHdrTextureToEnvironmentCubemapDirectory(ReadOnlySpan<char> hdrFilePath, ReadOnlySpan<char> destinationDirectoryPath) {
		ThrowIfThisIsDisposed();

		var destDirString = destinationDirectoryPath.ToString();
		var fileString = hdrFilePath.ToString();

		ExtractHdrPreprocessorIfNecessary();

		if (!File.Exists(_hdrPreprocessorFilePath)) {
			throw new InvalidOperationException($"Can not preprocess HDR textures as the preprocessor executable ({HdrPreprocessorExeName}) " +
												$"is not present at the expected location ({_hdrPreprocessorFilePath}).");
		}
		if (!File.Exists(fileString)) {
			throw new ArgumentException($"File '{fileString}' does not exist.", nameof(hdrFilePath));
		}
		
		try {
			var process = Process.Start(_hdrPreprocessorFilePath, "-q -f ktx -x \"" + destinationDirectoryPath.ToString() + "\" \"" + fileString + "\"");
			if (!process.WaitForExit(_maxHdrProcessingTime)) {
				try {
					process.Kill(entireProcessTree: true);
				}
#pragma warning disable CA1031 // "Don't catch & swallow exceptions" -- In this case we don't care if we couldn't kill the process, we're going to throw an exception anyway
				catch { /* no op */ }
#pragma warning restore CA1031

				throw new InvalidOperationException($"Aborting HDR preprocessing operation after timeout of {_maxHdrProcessingTime.ToStringMs()}. " +
													$"This value can be altered by setting the {nameof(LocalAssetLoaderConfig.MaxHdrProcessingTime)} configuration " +
													$"value on the {nameof(LocalAssetLoaderConfig)} instance passed in to the factory constructor.");
			}

			if (!Directory.Exists(destDirString) || Directory.GetFiles(destDirString, HdrPreprocessedSkyboxFileSearch).Length == 0 || Directory.GetFiles(destDirString, HdrPreprocessedIblFileSearch).Length == 0) {
				throw new InvalidOperationException($"Error when processing texture. Check arguments and file formats.");
			}
		}
		catch (Exception e) {
			throw new InvalidOperationException("Can not preprocess HDR textures as there was an issue encountered when running the preprocessor executable.", e);
		}
	}
	// TODO xmldoc that the directory should be empty other than the preprocessed hdr file contents
	public EnvironmentCubemap LoadEnvironmentCubemapFromPreprocessedHdrDirectory(ReadOnlySpan<char> directoryPath, in EnvironmentCubemapCreationConfig config) {
		try {
			var dirPathString = directoryPath.ToString();
			var skyboxFile = Directory.GetFiles(dirPathString, HdrPreprocessedSkyboxFileSearch).FirstOrDefault();
			var iblFile = Directory.GetFiles(dirPathString, HdrPreprocessedIblFileSearch).FirstOrDefault();

			if (skyboxFile == null || iblFile == null) {
				throw new InvalidOperationException($"Could not find skybox ({HdrPreprocessedSkyboxFileSearch}) and/or IBL ({HdrPreprocessedIblFileSearch}) file in given directory ({dirPathString}).");
			}

			return LoadEnvironmentCubemap(new() { IblKtxFilePath = iblFile, SkyboxKtxFilePath = skyboxFile }, config);
		}
		catch (Exception e) {
			throw new InvalidOperationException("Could not load processed HDR directory.", e);
		}
	}
	public EnvironmentCubemap LoadEnvironmentCubemap(in EnvironmentCubemapReadConfig readConfig, in EnvironmentCubemapCreationConfig config) {
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();
		config.ThrowIfInvalid();
		try {
			checked {
				using var skyboxFs = new FileStream(readConfig.SkyboxKtxFilePath.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read);
				using var iblFs = new FileStream(readConfig.IblKtxFilePath.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read);

				var skyboxFileLen = (int) skyboxFs.Length;
				var skyboxFixedBuffer = _ktxFileBufferPool.Rent(skyboxFileLen);
				skyboxFs.ReadExactly(skyboxFixedBuffer.AsByteSpan[..skyboxFileLen]);
				LoadSkyboxFileInToMemory(
						(byte*) skyboxFixedBuffer.StartPtr, 
						skyboxFileLen, 
						out var skyboxTextureHandle
				).ThrowIfFailure();
				_ktxFileBufferPool.Return(skyboxFixedBuffer);

				var iblFileLen = (int) iblFs.Length;
				var iblFixedBuffer = _ktxFileBufferPool.Rent(iblFileLen);
				iblFs.ReadExactly(iblFixedBuffer.AsByteSpan[..iblFileLen]);
				LoadIblFileInToMemory(
					(byte*) iblFixedBuffer.StartPtr,
					iblFileLen,
					out var iblTextureHandle
				).ThrowIfFailure();
				_ktxFileBufferPool.Return(iblFixedBuffer);

				++_prevCubemapHandle;
				var handle = (ResourceHandle<EnvironmentCubemap>) _prevCubemapHandle;
				_globals.StoreResourceNameIfNotEmpty(handle.Ident, config.Name);
				_loadedCubemaps.Add(_prevCubemapHandle, new(skyboxTextureHandle, iblTextureHandle));
				return HandleToInstance(handle);
			}
		}
		catch (Exception e) {
			if (!File.Exists(readConfig.SkyboxKtxFilePath.ToString())) throw new InvalidOperationException($"File '{readConfig.SkyboxKtxFilePath}' does not exist.", e);
			if (!File.Exists(readConfig.IblKtxFilePath.ToString())) throw new InvalidOperationException($"File '{readConfig.IblKtxFilePath}' does not exist.", e);
			throw new InvalidOperationException("Error occured when reading and/or loading skybox or IBL file.", e);
		}
	}

	public UIntPtr GetSkyboxTextureHandle(ResourceHandle<EnvironmentCubemap> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedCubemaps[handle].SkyboxTextureHandle;
	}
	public UIntPtr GetIndirectLightingTextureHandle(ResourceHandle<EnvironmentCubemap> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedCubemaps[handle].IblTextureHandle;
	}

	public string GetNameAsNewStringObject(ResourceHandle<EnvironmentCubemap> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultEnvironmentCubemapName));
	}
	public int GetNameLength(ResourceHandle<EnvironmentCubemap> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultEnvironmentCubemapName).Length;
	}
	public void CopyName(ResourceHandle<EnvironmentCubemap> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultEnvironmentCubemapName, destinationBuffer);
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "load_skybox_file_in_to_memory")]
	static extern InteropResult LoadSkyboxFileInToMemory(
		byte* dataPtr,
		int dataLen,
		out UIntPtr outTextureHandle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "unload_skybox_file_from_memory")]
	static extern InteropResult UnloadSkyboxFileFromMemory(
		UIntPtr textureHandle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "load_ibl_file_in_to_memory")]
	static extern InteropResult LoadIblFileInToMemory(
		byte* dataPtr,
		int dataLen,
		out UIntPtr outTextureHandle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "unload_ibl_file_from_memory")]
	static extern InteropResult UnloadIblFileFromMemory(
		UIntPtr textureHandle
	);
	#endregion

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	EnvironmentCubemap HandleToInstance(ResourceHandle<EnvironmentCubemap> h) => new(h, this);

	public override string ToString() => _isDisposed ? "TinyFFR Local Asset Loader [Disposed]" : "TinyFFR Local Asset Loader";

	#region Disposal
	public bool IsDisposed(ResourceHandle<EnvironmentCubemap> handle) => _isDisposed || !_loadedCubemaps.ContainsKey(handle);

	public void Dispose(ResourceHandle<EnvironmentCubemap> handle) => Dispose(handle, removeFromCollection: true);
	void Dispose(ResourceHandle<EnvironmentCubemap> handle, bool removeFromCollection) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		var data = _loadedCubemaps[handle];
		LocalFrameSynchronizationManager.QueueResourceDisposal(data.IblTextureHandle, &UnloadIblFileFromMemory);
		LocalFrameSynchronizationManager.QueueResourceDisposal(data.SkyboxTextureHandle, &UnloadSkyboxFileFromMemory);
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromCollection) _loadedCubemaps.Remove(handle);
	}


	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var cubemap in _loadedCubemaps.Keys) Dispose(cubemap, removeFromCollection: false);
			_ktxFileBufferPool.Dispose();
			_vertexTriangleBufferPool.Dispose();
			_assetFilePathBuffer.Dispose();
			_meshBuilder.Dispose();
			_materialBuilder.Dispose();
			_loadedCubemaps.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(IAssetLoader));
	}
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<EnvironmentCubemap> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(EnvironmentCubemap));
	#endregion
}