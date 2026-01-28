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

sealed unsafe class LocalAssetLoader : ILocalAssetLoader, IModelImplProvider, IDisposable {
	readonly record struct BackdropTextureData(UIntPtr SkyboxTextureHandle, UIntPtr IblTextureHandle);
	const string DefaultModelName = "Unnamed Model";
	const string DefaultBackdropTextureName = "Unnamed Backdrop Texture";
	const string HdrPreprocessorNameWin = "cmgen.exe";
	const string HdrPreprocessorNameLinux = "cmgen";
	const string HdrPreprocessorNameMacos = "cmgen_mac";
	const string HdrPreprocessorResourceNameStart = "Assets.Local.";
	const string HdrPreprocessedSkyboxFileSearch = "*_skybox.ktx";
	const string HdrPreprocessedIblFileSearch = "*_ibl.ktx";
	readonly string _hdrPreprocessorFilePath;
	readonly string _hdrPreprocessorResourceName;
	readonly BackdropTextureImplProvider _backdropTextureImplProvider;
	readonly LocalBuiltInTexturePathLibrary _builtInTextureLibrary = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly LocalMeshBuilder _meshBuilder;
	readonly LocalTextureBuilder _textureBuilder;
	readonly LocalMaterialBuilder _materialBuilder;
	readonly InteropStringBuffer _assetFilePathBuffer;
	readonly FixedByteBufferPool _vertexTriangleBufferPool;
	readonly FixedByteBufferPool _ktxFileBufferPool;
	readonly TimeSpan _maxHdrProcessingTime;
	readonly ArrayPoolBackedMap<ResourceHandle<Model>, ResourceGroup> _loadedModels = new();
	readonly ArrayPoolBackedMap<ResourceHandle<BackdropTexture>, BackdropTextureData> _loadedBackdropTextures = new();
	readonly Lazy<ResourceGroup> _testMaterialTextures;
	nuint _prevBackdropTextureHandle = 0;
	bool _isDisposed = false;
	bool _hdrPreprocessorHasBeenExtracted = false;

	public IMeshBuilder MeshBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _meshBuilder;
	public ITextureBuilder TextureBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _textureBuilder;
	public IMaterialBuilder MaterialBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _materialBuilder;
	public IBuiltInTexturePathLibrary BuiltInTexturePaths => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _builtInTextureLibrary;

	public LocalAssetLoader(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config) {
		ArgumentNullException.ThrowIfNull(globals);
		ArgumentNullException.ThrowIfNull(config);

		_globals = globals;
		_testMaterialTextures = new(CreateTestMaterialTextures);
		_meshBuilder = new LocalMeshBuilder(globals);
		_textureBuilder = new LocalTextureBuilder(globals, config);
		_materialBuilder = new LocalMaterialBuilder(globals, config, _textureBuilder, _testMaterialTextures);
		_assetFilePathBuffer = new InteropStringBuffer(config.MaxAssetFilePathLengthChars, addOneForNullTerminator: true);
		_vertexTriangleBufferPool = new FixedByteBufferPool(config.MaxAssetVertexIndexBufferSizeBytes);
		_ktxFileBufferPool = new FixedByteBufferPool(config.MaxKtxFileBufferSizeBytes);
		_maxHdrProcessingTime = config.MaxHdrProcessingTime;
		_backdropTextureImplProvider = new BackdropTextureImplProvider(this);

		if (OperatingSystem.IsWindows()) {
			_hdrPreprocessorFilePath = Path.Combine(LocalFileSystemUtils.ApplicationDataDirectoryPath, HdrPreprocessorNameWin);
			_hdrPreprocessorResourceName = HdrPreprocessorResourceNameStart + HdrPreprocessorNameWin;
		}
		else if (OperatingSystem.IsMacOS()) {
			_hdrPreprocessorFilePath = Path.Combine(LocalFileSystemUtils.ApplicationDataDirectoryPath, HdrPreprocessorNameMacos);
			_hdrPreprocessorResourceName = HdrPreprocessorResourceNameStart + HdrPreprocessorNameMacos;
		}
		else {
			_hdrPreprocessorFilePath = Path.Combine(LocalFileSystemUtils.ApplicationDataDirectoryPath, HdrPreprocessorNameLinux);
			_hdrPreprocessorResourceName = HdrPreprocessorResourceNameStart + HdrPreprocessorNameLinux;
		}
	}

	#region Read / Load Texture
	public Texture LoadTexture(ReadOnlySpan<char> filePath, in TextureCreationConfig config, in TextureReadConfig readConfig) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		
		switch (_builtInTextureLibrary.GetLikelyBuiltInTextureType(filePath)) {
			case LocalBuiltInTexturePathLibrary.BuiltInTextureType.Texel:
				var builtInTexel = _builtInTextureLibrary.TryGetBuiltInTexel(filePath);
				var builtInRgb = builtInTexel?.First;
				var builtInRgba = builtInTexel?.Second;

				if (builtInRgb is { } rgb) {
					return _textureBuilder.CreateTexture(
						new ReadOnlySpan<TexelRgb24>(in rgb),
						new() { Dimensions = new(1, 1) },
						config
					);
				}
				else if (builtInRgba is { } rgba) {
					return _textureBuilder.CreateTexture(
						new ReadOnlySpan<TexelRgba32>(in rgba),
						new() { Dimensions = new(1, 1) },
						config
					);
				}
				break;
			case LocalBuiltInTexturePathLibrary.BuiltInTextureType.EmbeddedResourceTexture:
				var embeddedTextureAssetData = _builtInTextureLibrary.TryGetBuiltInEmbeddedResourceTexture(filePath);
				if (embeddedTextureAssetData is { } tuple) {
					if (tuple.ContainsAlpha) {
						return _textureBuilder.CreateTexture(
							MemoryMarshal.Cast<byte, TexelRgba32>(tuple.DataRef.AsSpan)[..tuple.Dimensions.Area],
							new TextureGenerationConfig { Dimensions = tuple.Dimensions },
							config
						);
					}
					else {
						return _textureBuilder.CreateTexture(
							MemoryMarshal.Cast<byte, TexelRgb24>(tuple.DataRef.AsSpan)[..tuple.Dimensions.Area],
							new TextureGenerationConfig { Dimensions = tuple.Dimensions },
							config
						);
					}
				}
				break;
		}

		try {
			_assetFilePathBuffer.ConvertFromUtf16(filePath);
			GetTextureFileData(
				in _assetFilePathBuffer.AsRef,
				out _,
				out _,
				out var channelCount
			).ThrowIfFailure();

			var includeAlpha = channelCount > 3 && readConfig.IncludeWAlphaChannel;

			LoadTextureFileInToMemory(
				in _assetFilePathBuffer.AsRef,
				includeWAlphaChannel: includeAlpha,
				out var width,
				out var height,
				out var texelBuffer
			).ThrowIfFailure();

			try {
				if (width < 0 || height < 0) throw new InvalidOperationException($"Loaded texture had width/height of {width}/{height}.");
				var texelCount = width * height;

				if (includeAlpha) {
					return _textureBuilder.CreateTexture(
						new ReadOnlySpan<TexelRgba32>(texelBuffer, texelCount),
						new() { Dimensions = new(width, height) },
						config
					);
				}
				else {
					return _textureBuilder.CreateTexture(
						new ReadOnlySpan<TexelRgb24>(texelBuffer, texelCount),
						new() { Dimensions = new(width, height) },
						config
					);
				}
			}
			finally {
				UnloadTextureFileFromMemory(texelBuffer).ThrowIfFailure();
			}
		}
		catch (Exception e) {
			if (!File.Exists(filePath.ToString())) throw new InvalidOperationException($"File '{filePath}' does not exist.", e);
			else throw;
		}
	}
	public TextureReadMetadata ReadTextureMetadata(ReadOnlySpan<char> filePath) {
		ThrowIfThisIsDisposed();

		switch (_builtInTextureLibrary.GetLikelyBuiltInTextureType(filePath)) {
			case LocalBuiltInTexturePathLibrary.BuiltInTextureType.Texel:
				var builtInTexel = _builtInTextureLibrary.TryGetBuiltInTexel(filePath);
				if (builtInTexel.HasValue) return new TextureReadMetadata((1, 1), builtInTexel.Value.Second.HasValue);
				break;
			case LocalBuiltInTexturePathLibrary.BuiltInTextureType.EmbeddedResourceTexture:
				var embeddedResourceData = _builtInTextureLibrary.TryGetBuiltInEmbeddedResourceTexture(filePath);
				if (embeddedResourceData is { } tuple) return new TextureReadMetadata(tuple.Dimensions, tuple.ContainsAlpha);
				break;
		}

		try {
			_assetFilePathBuffer.ConvertFromUtf16(filePath);
			GetTextureFileData(
				in _assetFilePathBuffer.AsRef,
				out var width,
				out var height,
				out var channelCount
			).ThrowIfFailure();

			return new((width, height), channelCount > 3);
		}
		catch (Exception e) {
			if (!File.Exists(filePath.ToString())) throw new InvalidOperationException($"File '{filePath}' does not exist.", e);
			else throw;
		}
	}
	public int ReadTexture<TTexel>(ReadOnlySpan<char> filePath, in TextureProcessingConfig processingConfig, Span<TTexel> destinationBuffer) where TTexel : unmanaged, ITexel<TTexel> {
		ThrowIfThisIsDisposed();

		switch (_builtInTextureLibrary.GetLikelyBuiltInTextureType(filePath)) {
			case LocalBuiltInTexturePathLibrary.BuiltInTextureType.Texel:
				var builtInTexel = _builtInTextureLibrary.TryGetBuiltInTexel(filePath);
				if (builtInTexel.HasValue) {
					if (destinationBuffer.Length < 1) {
						throw new ArgumentException($"Given destination buffer size ({destinationBuffer.Length}) is too small to accomodate texture data ({1} texels).");
					}

					switch (TTexel.BlitType) {
						case TexelType.Rgba32: {
								var localTexelCopy = builtInTexel.Value.First?.ToRgba32()
													 ?? builtInTexel.Value.Second
													 ?? throw new InvalidOperationException("Unexpected null texel pair (this is a bug in TinyFFR).");
								destinationBuffer[0] = Unsafe.As<TexelRgba32, TTexel>(ref localTexelCopy);
								break;
							}
						case TexelType.Rgb24: {
								var localTexelCopy = builtInTexel.Value.First
													 ?? builtInTexel.Value.Second?.ToRgb24()
													 ?? throw new InvalidOperationException("Unexpected null texel pair (this is a bug in TinyFFR).");
								destinationBuffer[0] = Unsafe.As<TexelRgb24, TTexel>(ref localTexelCopy);
								break;
							}
						default:
							throw new ArgumentOutOfRangeException(nameof(TTexel), "Unknown texel blit type.");
					}

					TextureUtils.ProcessTexture(destinationBuffer, (1, 1), in processingConfig);
					return 1;
				}
				break;
			case LocalBuiltInTexturePathLibrary.BuiltInTextureType.EmbeddedResourceTexture:
				var embeddedResourceData = _builtInTextureLibrary.TryGetBuiltInEmbeddedResourceTexture(filePath);
				if (embeddedResourceData is { } tuple) {
					if (destinationBuffer.Length < tuple.Dimensions.Area) {
						throw new ArgumentException($"Given destination buffer size ({destinationBuffer.Length}) is too small to accomodate texture data ({tuple.Dimensions.Area} texels).");
					}

					if (tuple.ContainsAlpha) {
						var texelData = MemoryMarshal.Cast<byte, TexelRgba32>(tuple.DataRef.AsSpan)[..tuple.Dimensions.Area];
						switch (TTexel.BlitType) {
							case TexelType.Rgba32: {
								MemoryMarshal.Cast<TexelRgba32, TTexel>(texelData).CopyTo(destinationBuffer);
								break;
							}
							case TexelType.Rgb24: {
								for (var i = 0; i < texelData.Length; ++i) {
									var convertedTexel = texelData[i].ToRgb24();
									destinationBuffer[i] = Unsafe.As<TexelRgb24, TTexel>(ref convertedTexel);
								}
								break;
							}
							default: throw new ArgumentOutOfRangeException(nameof(TTexel), "Unknown texel blit type.");
						}
						return texelData.Length;
					}
					else {
						var texelData = MemoryMarshal.Cast<byte, TexelRgb24>(tuple.DataRef.AsSpan)[..tuple.Dimensions.Area];
						switch (TTexel.BlitType) {
							case TexelType.Rgba32: {
								for (var i = 0; i < texelData.Length; ++i) {
									var convertedTexel = texelData[i].ToRgba32();
									destinationBuffer[i] = Unsafe.As<TexelRgba32, TTexel>(ref convertedTexel);
								}
								break;
							}
							case TexelType.Rgb24: {
								MemoryMarshal.Cast<TexelRgb24, TTexel>(texelData).CopyTo(destinationBuffer);
								break;
							}
							default: throw new ArgumentOutOfRangeException(nameof(TTexel), "Unknown texel blit type.");
						}
						return texelData.Length;
					}
				}
				break;
		}
		

		var includeWChannel = TTexel.BlitType switch {
			TexelType.Rgb24 => false,
			TexelType.Rgba32 => true,
			_ => throw new ArgumentOutOfRangeException(nameof(TTexel), "Unknown texel blit type.")
		};

		try {
			_assetFilePathBuffer.ConvertFromUtf16(filePath);
			LoadTextureFileInToMemory(
				in _assetFilePathBuffer.AsRef,
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

				TextureUtils.ProcessTexture(destinationBuffer, (width, height), in processingConfig);
				return texelCount;
			}
			finally {
				UnloadTextureFileFromMemory(texelBuffer).ThrowIfFailure();
			}
		}
		catch (Exception e) {
			var filePathAsStr = filePath.ToString();
			if (!File.Exists(filePathAsStr)) throw new InvalidOperationException($"File '{filePath}' does not exist (full path \"{Path.GetFullPath(filePathAsStr)}\").", e);
			else throw;
		}
	}
	#endregion

	#region Read / Load Combined Texture
	XYPair<int> GetCombinedTextureDimensions(XYPair<int> aDimensions, XYPair<int> bDimensions, XYPair<int>? cDimensions, XYPair<int>? dDimensions, out bool allDimensionsMatched) {
		cDimensions ??= aDimensions;
		dDimensions ??= bDimensions;
		allDimensionsMatched = aDimensions == bDimensions && bDimensions == cDimensions && cDimensions == dDimensions;

		if (allDimensionsMatched) {
			return aDimensions;
		}

		return new(
			Int32.Max(Int32.Max(Int32.Max(aDimensions.X, bDimensions.X), cDimensions.Value.X), dDimensions.Value.X),
			Int32.Max(Int32.Max(Int32.Max(aDimensions.Y, bDimensions.Y), cDimensions.Value.Y), dDimensions.Value.Y)
		);
	}
	PooledHeapMemory<TexelRgba32> ReadTextureForCombination(ReadOnlySpan<char> filePath, TextureReadMetadata metadata, in TextureProcessingConfig processingConfig) {
		var result = _globals.HeapPool.Borrow<TexelRgba32>(metadata.Dimensions.Area);
		ReadTexture(filePath, in processingConfig, result.Buffer);
		return result;
	}
	int CalculateWrappedIndexForCombination(XYPair<int> dimensions, int x, int y) => dimensions.X * (y % dimensions.Y) + (x % dimensions.X);
	
	void CombineTextures<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig, TextureReadMetadata aMetadata,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig, TextureReadMetadata bMetadata,
		TextureCombinationConfig combinationConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> {
		const int NumTexturesBeingCombined = 3;

		aProcessingConfig.ThrowIfInvalid();
		bProcessingConfig.ThrowIfInvalid();
		combinationConfig.ThrowIfInvalid(NumTexturesBeingCombined);

		using var aPool = ReadTextureForCombination(aFilePath, aMetadata, in aProcessingConfig);
		using var bPool = ReadTextureForCombination(bFilePath, bMetadata, in bProcessingConfig);
		var destDimensions = GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, null, null, out var allDimensionsMatch);
		if (destinationBuffer.Length < destDimensions.Area) {
			throw new ArgumentException(
				$"Destination buffer length needs to be at least {destDimensions.Area} " +
				$"(output combined texture would have dimensions {destDimensions}). " +
				$"Calculate the dimensions of the output texture first using {nameof(ReadCombinedTextureMetadata)}.",
				nameof(destinationBuffer)
			);
		}

		var aBuffer = aPool.Buffer;
		var bBuffer = bPool.Buffer;
		Span<TexelRgba32> localSampleBuffer = stackalloc TexelRgba32[NumTexturesBeingCombined];

		if (allDimensionsMatch) {
			for (var i = 0; i < destDimensions.Area; ++i) {
				localSampleBuffer[0] = aBuffer[i];
				localSampleBuffer[1] = bBuffer[i];
				destinationBuffer[i] = TTexel.ConvertFrom(combinationConfig.SelectTexel(localSampleBuffer));
			}
		}
		else {
			for (var x = 0; x < destDimensions.X; ++x) {
				for (var y = 0; y < destDimensions.Y; ++y) {
					localSampleBuffer[0] = aBuffer[CalculateWrappedIndexForCombination(aMetadata.Dimensions, x, y)];
					localSampleBuffer[1] = bBuffer[CalculateWrappedIndexForCombination(bMetadata.Dimensions, x, y)];
					destinationBuffer[destDimensions.X * y + x] = TTexel.ConvertFrom(combinationConfig.SelectTexel(localSampleBuffer));
				}
			}
		}
	}
	void CombineTextures<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig, TextureReadMetadata aMetadata,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig, TextureReadMetadata bMetadata,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig, TextureReadMetadata cMetadata,
		TextureCombinationConfig combinationConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> {
		const int NumTexturesBeingCombined = 3;

		aProcessingConfig.ThrowIfInvalid();
		bProcessingConfig.ThrowIfInvalid();
		cProcessingConfig.ThrowIfInvalid();
		combinationConfig.ThrowIfInvalid(NumTexturesBeingCombined);

		using var aPool = ReadTextureForCombination(aFilePath, aMetadata, in aProcessingConfig);
		using var bPool = ReadTextureForCombination(bFilePath, bMetadata, in bProcessingConfig);
		using var cPool = ReadTextureForCombination(cFilePath, cMetadata, in cProcessingConfig);
		var destDimensions = GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions, null, out var allDimensionsMatch);
		if (destinationBuffer.Length < destDimensions.Area) {
			throw new ArgumentException(
				$"Destination buffer length needs to be at least {destDimensions.Area} " +
				$"(output combined texture would have dimensions {destDimensions}). " +
				$"Calculate the dimensions of the output texture first using {nameof(ReadCombinedTextureMetadata)}.",
				nameof(destinationBuffer)
			);
		}

		var aBuffer = aPool.Buffer;
		var bBuffer = bPool.Buffer;
		var cBuffer = cPool.Buffer;
		Span<TexelRgba32> localSampleBuffer = stackalloc TexelRgba32[NumTexturesBeingCombined];

		if (allDimensionsMatch) {
			for (var i = 0; i < destDimensions.Area; ++i) {
				localSampleBuffer[0] = aBuffer[i];
				localSampleBuffer[1] = bBuffer[i];
				localSampleBuffer[2] = cBuffer[i];
				destinationBuffer[i] = TTexel.ConvertFrom(combinationConfig.SelectTexel(localSampleBuffer));
			}
		}
		else {
			for (var x = 0; x < destDimensions.X; ++x) {
				for (var y = 0; y < destDimensions.Y; ++y) {
					localSampleBuffer[0] = aBuffer[CalculateWrappedIndexForCombination(aMetadata.Dimensions, x, y)];
					localSampleBuffer[1] = bBuffer[CalculateWrappedIndexForCombination(bMetadata.Dimensions, x, y)];
					localSampleBuffer[2] = cBuffer[CalculateWrappedIndexForCombination(cMetadata.Dimensions, x, y)];
					destinationBuffer[destDimensions.X * y + x] = TTexel.ConvertFrom(combinationConfig.SelectTexel(localSampleBuffer));
				}
			}
		}
	}
	void CombineTextures<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig, TextureReadMetadata aMetadata,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig, TextureReadMetadata bMetadata,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig, TextureReadMetadata cMetadata,
		ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig, TextureReadMetadata dMetadata,
		TextureCombinationConfig combinationConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> {
		const int NumTexturesBeingCombined = 4;

		aProcessingConfig.ThrowIfInvalid();
		bProcessingConfig.ThrowIfInvalid();
		cProcessingConfig.ThrowIfInvalid();
		dProcessingConfig.ThrowIfInvalid();
		combinationConfig.ThrowIfInvalid(NumTexturesBeingCombined);

		using var aPool = ReadTextureForCombination(aFilePath, aMetadata, in aProcessingConfig);
		using var bPool = ReadTextureForCombination(bFilePath, bMetadata, in bProcessingConfig);
		using var cPool = ReadTextureForCombination(cFilePath, cMetadata, in cProcessingConfig);
		using var dPool = ReadTextureForCombination(dFilePath, cMetadata, in dProcessingConfig);
		var destDimensions = GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions, dMetadata.Dimensions, out var allDimensionsMatch);
		if (destinationBuffer.Length < destDimensions.Area) {
			throw new ArgumentException(
				$"Destination buffer length needs to be at least {destDimensions.Area} " +
				$"(output combined texture would have dimensions {destDimensions}). " +
				$"Calculate the dimensions of the output texture first using {nameof(ReadCombinedTextureMetadata)}.",
				nameof(destinationBuffer)
			);
		}

		var aBuffer = aPool.Buffer;
		var bBuffer = bPool.Buffer;
		var cBuffer = cPool.Buffer;
		var dBuffer = dPool.Buffer;
		Span<TexelRgba32> localSampleBuffer = stackalloc TexelRgba32[NumTexturesBeingCombined];

		if (allDimensionsMatch) {
			for (var i = 0; i < destDimensions.Area; ++i) {
				localSampleBuffer[0] = aBuffer[i];
				localSampleBuffer[1] = bBuffer[i];
				localSampleBuffer[2] = cBuffer[i];
				localSampleBuffer[3] = dBuffer[i];
				destinationBuffer[i] = TTexel.ConvertFrom(combinationConfig.SelectTexel(localSampleBuffer));
			}
		}
		else {
			for (var x = 0; x < destDimensions.X; ++x) {
				for (var y = 0; y < destDimensions.Y; ++y) {
					localSampleBuffer[0] = aBuffer[CalculateWrappedIndexForCombination(aMetadata.Dimensions, x, y)];
					localSampleBuffer[1] = bBuffer[CalculateWrappedIndexForCombination(bMetadata.Dimensions, x, y)];
					localSampleBuffer[2] = cBuffer[CalculateWrappedIndexForCombination(cMetadata.Dimensions, x, y)];
					localSampleBuffer[3] = dBuffer[CalculateWrappedIndexForCombination(cMetadata.Dimensions, x, y)];
					destinationBuffer[destDimensions.X * y + x] = TTexel.ConvertFrom(combinationConfig.SelectTexel(localSampleBuffer));
				}
			}
		}
	}

	public Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) {
		var aMetadata = ReadTextureMetadata(aFilePath);
		var bMetadata = ReadTextureMetadata(bFilePath);
		var destDimensions = GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, null, null, out _);

		if (combinationConfig.OutputTextureWAlphaChannelSource == null) {
			using var destPool = _globals.HeapPool.Borrow<TexelRgb24>(destDimensions.Area);
			CombineTextures(aFilePath, in aProcessingConfig, aMetadata, bFilePath, in bProcessingConfig, bMetadata, combinationConfig, destPool.Buffer);
			return _textureBuilder.CreateTexture(destPool.Buffer, new TextureGenerationConfig { Dimensions = destDimensions }, in finalOutputConfig);
		}
		else {
			using var destPool = _globals.HeapPool.Borrow<TexelRgba32>(destDimensions.Area);
			CombineTextures(aFilePath, in aProcessingConfig, aMetadata, bFilePath, in bProcessingConfig, bMetadata, combinationConfig, destPool.Buffer);
			return _textureBuilder.CreateTexture(destPool.Buffer, new TextureGenerationConfig { Dimensions = destDimensions }, in finalOutputConfig);
		}
	}
	public Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) {
		var aMetadata = ReadTextureMetadata(aFilePath);
		var bMetadata = ReadTextureMetadata(bFilePath);
		var cMetadata = ReadTextureMetadata(cFilePath);
		var destDimensions = GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions, null, out _);

		if (combinationConfig.OutputTextureWAlphaChannelSource == null) {
			using var destPool = _globals.HeapPool.Borrow<TexelRgb24>(destDimensions.Area);
			CombineTextures(aFilePath, in aProcessingConfig, aMetadata, bFilePath, in bProcessingConfig, bMetadata, cFilePath, in cProcessingConfig, cMetadata, combinationConfig, destPool.Buffer);
			return _textureBuilder.CreateTexture(destPool.Buffer, new TextureGenerationConfig { Dimensions = destDimensions }, in finalOutputConfig);
		}
		else {
			using var destPool = _globals.HeapPool.Borrow<TexelRgba32>(destDimensions.Area);
			CombineTextures(aFilePath, in aProcessingConfig, aMetadata, bFilePath, in bProcessingConfig, bMetadata, cFilePath, in cProcessingConfig, cMetadata, combinationConfig, destPool.Buffer);
			return _textureBuilder.CreateTexture(destPool.Buffer, new TextureGenerationConfig { Dimensions = destDimensions }, in finalOutputConfig);
		}
	}
	public Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) {
		var aMetadata = ReadTextureMetadata(aFilePath);
		var bMetadata = ReadTextureMetadata(bFilePath);
		var cMetadata = ReadTextureMetadata(cFilePath);
		var dMetadata = ReadTextureMetadata(dFilePath);
		var destDimensions = GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions, dMetadata.Dimensions, out _);

		if (combinationConfig.OutputTextureWAlphaChannelSource == null) {
			using var destPool = _globals.HeapPool.Borrow<TexelRgb24>(destDimensions.Area);
			CombineTextures(aFilePath, in aProcessingConfig, aMetadata, bFilePath, in bProcessingConfig, bMetadata, cFilePath, in cProcessingConfig, cMetadata, dFilePath, in dProcessingConfig, dMetadata, combinationConfig, destPool.Buffer);
			return _textureBuilder.CreateTexture(destPool.Buffer, new TextureGenerationConfig { Dimensions = destDimensions }, in finalOutputConfig);
		}
		else {
			using var destPool = _globals.HeapPool.Borrow<TexelRgba32>(destDimensions.Area);
			CombineTextures(aFilePath, in aProcessingConfig, aMetadata, bFilePath, in bProcessingConfig, bMetadata, cFilePath, in cProcessingConfig, cMetadata, dFilePath, in dProcessingConfig, dMetadata, combinationConfig, destPool.Buffer);
			return _textureBuilder.CreateTexture(destPool.Buffer, new TextureGenerationConfig { Dimensions = destDimensions }, in finalOutputConfig);
		}
	}

	public TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath) {
		var aMetadata = ReadTextureMetadata(aFilePath);
		var bMetadata = ReadTextureMetadata(bFilePath);
		return new(
			GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, null, null, out _), 
			aMetadata.IncludesAlphaChannel || bMetadata.IncludesAlphaChannel
		);
	}
	public TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath, ReadOnlySpan<char> cFilePath) {
		var aMetadata = ReadTextureMetadata(aFilePath);
		var bMetadata = ReadTextureMetadata(bFilePath);
		var cMetadata = ReadTextureMetadata(cFilePath);
		return new(
			GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions, null, out _),
			aMetadata.IncludesAlphaChannel || bMetadata.IncludesAlphaChannel || cMetadata.IncludesAlphaChannel
		);
	}
	public TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath, ReadOnlySpan<char> cFilePath, ReadOnlySpan<char> dFilePath) {
		var aMetadata = ReadTextureMetadata(aFilePath);
		var bMetadata = ReadTextureMetadata(bFilePath);
		var cMetadata = ReadTextureMetadata(cFilePath);
		var dMetadata = ReadTextureMetadata(dFilePath);
		return new(
			GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions, dMetadata.Dimensions, out _),
			aMetadata.IncludesAlphaChannel || bMetadata.IncludesAlphaChannel || cMetadata.IncludesAlphaChannel || dMetadata.IncludesAlphaChannel
		);
	}

	public int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> {
		var aMetadata = ReadTextureMetadata(aFilePath);
		var bMetadata = ReadTextureMetadata(bFilePath);
		var destDimensions = GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, null, null, out _);

		CombineTextures(aFilePath, in aProcessingConfig, aMetadata, bFilePath, in bProcessingConfig, bMetadata, combinationConfig, destinationBuffer);
		TextureUtils.ProcessTexture(destinationBuffer, destDimensions, in finalOutputProcessingConfig);
		return destDimensions.Area;
	}
	public int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> {
		var aMetadata = ReadTextureMetadata(aFilePath);
		var bMetadata = ReadTextureMetadata(bFilePath);
		var cMetadata = ReadTextureMetadata(cFilePath);
		var destDimensions = GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions, null, out _);

		CombineTextures(aFilePath, in aProcessingConfig, aMetadata, bFilePath, in bProcessingConfig, bMetadata, cFilePath, in cProcessingConfig, cMetadata, combinationConfig, destinationBuffer);
		TextureUtils.ProcessTexture(destinationBuffer, destDimensions, in finalOutputProcessingConfig);
		return destDimensions.Area;
	}
	public int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> {
		var aMetadata = ReadTextureMetadata(aFilePath);
		var bMetadata = ReadTextureMetadata(bFilePath);
		var cMetadata = ReadTextureMetadata(cFilePath);
		var dMetadata = ReadTextureMetadata(dFilePath);
		var destDimensions = GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions, dMetadata.Dimensions, out _);

		CombineTextures(aFilePath, in aProcessingConfig, aMetadata, bFilePath, in bProcessingConfig, bMetadata, cFilePath, in cProcessingConfig, cMetadata, dFilePath, in dProcessingConfig, dMetadata, combinationConfig, destinationBuffer);
		TextureUtils.ProcessTexture(destinationBuffer, destDimensions, in finalOutputProcessingConfig);
		return destDimensions.Area;
	}
	#endregion

	#region Meshes
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
	#endregion

	#region Environment / Backdrop
	// This is a private embedded 'delegating' object to help provide distinction between some default interface methods
	// on both IModelImplProvider and IBackdropTextureImplProvider. 
	sealed class BackdropTextureImplProvider : IBackdropTextureImplProvider {
		readonly LocalAssetLoader _owner;

		public BackdropTextureImplProvider(LocalAssetLoader owner) => _owner = owner;

		public UIntPtr GetSkyboxTextureHandle(ResourceHandle<BackdropTexture> handle) => _owner.GetSkyboxTextureHandle(handle);
		public UIntPtr GetIndirectLightingTextureHandle(ResourceHandle<BackdropTexture> handle) => _owner.GetIndirectLightingTextureHandle(handle);
		public string GetNameAsNewStringObject(ResourceHandle<BackdropTexture> handle) => _owner.GetNameAsNewStringObject(handle);
		public int GetNameLength(ResourceHandle<BackdropTexture> handle) => _owner.GetNameLength(handle);
		public void CopyName(ResourceHandle<BackdropTexture> handle, Span<char> destinationBuffer) => _owner.CopyName(handle, destinationBuffer);
		public bool IsDisposed(ResourceHandle<BackdropTexture> handle) => _owner.IsDisposed(handle);
		public void Dispose(ResourceHandle<BackdropTexture> handle) => _owner.Dispose(handle);
		public override string ToString() => _owner.ToString();
	}
	
	void ExtractHdrPreprocessorIfNecessary() {
		if (_hdrPreprocessorHasBeenExtracted) return;

		try {
			var data = EmbeddedResourceResolver.GetResource(_hdrPreprocessorResourceName);
			File.WriteAllBytes(_hdrPreprocessorFilePath, data.AsSpan);
			if (!OperatingSystem.IsWindows()) {
				var chmodProc = Process.Start("chmod", $"+x \"{_hdrPreprocessorFilePath}\"");
				if (!chmodProc.WaitForExit(_maxHdrProcessingTime) || chmodProc.ExitCode != 0) {
					throw new InvalidOperationException($"Could not set execution permission on extracted HDR preprocessor executable (" +
														$"{(chmodProc.HasExited ? $"0x{chmodProc.ExitCode.ToString("x", CultureInfo.InvariantCulture)}" : "timed out")}).");
				}
			}
		}
		catch (Exception e) {
			throw new InvalidOperationException($"Could not extract HDR preprocessor executable ({_hdrPreprocessorResourceName}) " +
												$"to target directory ({LocalFileSystemUtils.ApplicationDataDirectoryPath}).", e);
		}

		_hdrPreprocessorHasBeenExtracted = true;
	}

	public void PreprocessHdrTextureToBackdropTextureDirectory(ReadOnlySpan<char> hdrFilePath, ReadOnlySpan<char> destinationDirectoryPath) {
		ThrowIfThisIsDisposed();

		var destDirString = destinationDirectoryPath.ToString();
		var fileString = hdrFilePath.ToString();

		ExtractHdrPreprocessorIfNecessary();

		if (!File.Exists(_hdrPreprocessorFilePath)) {
			throw new InvalidOperationException($"Can not preprocess HDR textures as the preprocessor executable ({_hdrPreprocessorResourceName}) " +
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
	public BackdropTexture LoadBackdropTextureFromPreprocessedHdrDirectory(ReadOnlySpan<char> directoryPath, in BackdropTextureCreationConfig config) {
		try {
			var dirPathString = directoryPath.ToString();
			var skyboxFile = Directory.GetFiles(dirPathString, HdrPreprocessedSkyboxFileSearch).FirstOrDefault();
			var iblFile = Directory.GetFiles(dirPathString, HdrPreprocessedIblFileSearch).FirstOrDefault();

			if (skyboxFile == null || iblFile == null) {
				throw new InvalidOperationException($"Could not find skybox ({HdrPreprocessedSkyboxFileSearch}) and/or IBL ({HdrPreprocessedIblFileSearch}) file in given directory ({dirPathString}).");
			}

			return LoadBackdropTexture(skyboxFile, iblFile, config);
		}
		catch (Exception e) {
			throw new InvalidOperationException("Could not load processed HDR directory.", e);
		}
	}
	public BackdropTexture LoadBackdropTexture(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, in BackdropTextureCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		try {
			checked {
				using var skyboxFs = new FileStream(skyboxKtxFilePath.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read);
				using var iblFs = new FileStream(iblKtxFilePath.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read);

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

				++_prevBackdropTextureHandle;
				var handle = (ResourceHandle<BackdropTexture>) _prevBackdropTextureHandle;
				_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, config.Name, DefaultBackdropTextureName);
				_loadedBackdropTextures.Add(_prevBackdropTextureHandle, new(skyboxTextureHandle, iblTextureHandle));
				return HandleToInstance(handle);
			}
		}
		catch (Exception e) {
			if (!File.Exists(skyboxKtxFilePath.ToString())) throw new InvalidOperationException($"File '{skyboxKtxFilePath}' does not exist.", e);
			if (!File.Exists(iblKtxFilePath.ToString())) throw new InvalidOperationException($"File '{iblKtxFilePath}' does not exist.", e);
			throw new InvalidOperationException("Error occured when reading and/or loading skybox or IBL file.", e);
		}
	}

	public UIntPtr GetSkyboxTextureHandle(ResourceHandle<BackdropTexture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedBackdropTextures[handle].SkyboxTextureHandle;
	}
	public UIntPtr GetIndirectLightingTextureHandle(ResourceHandle<BackdropTexture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedBackdropTextures[handle].IblTextureHandle;
	}

	public string GetNameAsNewStringObject(ResourceHandle<BackdropTexture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultBackdropTextureName));
	}
	public int GetNameLength(ResourceHandle<BackdropTexture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultBackdropTextureName).Length;
	}
	public void CopyName(ResourceHandle<BackdropTexture> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultBackdropTextureName, destinationBuffer);
	}
	#endregion
	
	#region Load Generic / Combined
	public ResourceGroup Load(ReadOnlySpan<char> filePath, in AssetCreationConfig config, in AssetReadConfig readConfig) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		readConfig.ThrowIfInvalid();
		
		try {
			_assetFilePathBuffer.ConvertFromUtf16(filePath);
			
			LoadAssetFileInToMemory(
				in _assetFilePathBuffer.AsRef,
				readConfig.MeshConfig.FixCommonExportErrors,
				readConfig.MeshConfig.OptimizeForGpu,
				out var assetHandle
			).ThrowIfFailure();

			try {
				GetLoadedAssetMeshCount(assetHandle, out var meshCount).ThrowIfFailure(); 
				GetLoadedAssetMaterialCount(assetHandle, out var materialCount).ThrowIfFailure(); 
				GetLoadedAssetTextureCount(assetHandle, out var textureCount).ThrowIfFailure(); 
				
				var result = _globals.ResourceGroupProvider.CreateGroup(
					disposeContainedResourcesWhenDisposed: true,
					name: config.Name,
					meshCount + materialCount + textureCount
				);
				
				for (var t = 0; t < textureCount; ++t) {
					
				}
				
				for (var m = 0; m < materialCount; ++m) {
					
				}
				
				for (var m = 0; m < meshCount; ++m) {
					GetLoadedAssetMeshVertexCount(assetHandle, m, out var vCount).ThrowIfFailure();
					GetLoadedAssetMeshTriangleCount(assetHandle, m, out var tCount).ThrowIfFailure();
					
					var fixedVertexBuffer = _vertexTriangleBufferPool.Rent<MeshVertex>(vCount);
					var fixedTriangleBuffer = _vertexTriangleBufferPool.Rent<VertexTriangle>(tCount);
					try {
						CopyLoadedAssetMeshVertices(assetHandle, m, fixedVertexBuffer.Size<MeshVertex>(), (MeshVertex*) fixedVertexBuffer.StartPtr).ThrowIfFailure();
						CopyLoadedAssetMeshTriangles(assetHandle, m, fixedTriangleBuffer.Size<VertexTriangle>(), (VertexTriangle*) fixedTriangleBuffer.StartPtr).ThrowIfFailure();
					}
					finally {
						_vertexTriangleBufferPool.Return(fixedVertexBuffer);
						_vertexTriangleBufferPool.Return(fixedTriangleBuffer);
					}
					
					var mesh = _meshBuilder.CreateMesh(
						fixedVertexBuffer.AsReadOnlySpan<MeshVertex>(vCount),
						fixedTriangleBuffer.AsReadOnlySpan<VertexTriangle>(tCount),
						config.MeshConfig
					);
					
					result.Add(mesh);
				}
				
				result.Seal();
				return result;
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

	public Mesh GetMesh(ResourceHandle<Model> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedModels[handle].Meshes[0];
	}
	public Material GetMaterial(ResourceHandle<Model> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedModels[handle].Materials[0];
	}
	public IndirectEnumerable<Model, Texture> GetTextures(ResourceHandle<Model> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new IndirectEnumerable<Model, Texture>(
			HandleToInstance(handle),
			0,
			&GetModelTextureCount,
			&GetModelVersion,
			&GetModelTextureAtIndex
		);
	}
	static int GetModelTextureCount(Model m) {
		var @this = m.Implementation as LocalAssetLoader;
		if (@this == null) throw new InvalidOperationException("Model textures enumerated against differing implementation.");
		var handle = m.Handle; // Throws if disposed
		return @this._loadedModels[handle].Textures.Count;
	}
	static Texture GetModelTextureAtIndex(Model m, int index) {
		var @this = m.Implementation as LocalAssetLoader;
		if (@this == null) throw new InvalidOperationException("Model textures enumerated against differing implementation.");
		var handle = m.Handle; // Throws if disposed
		return @this._loadedModels[handle].Textures[index];
	}
	static int GetModelVersion(Model _) => 0;

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
	#endregion

	ResourceGroup CreateTestMaterialTextures() {
		var result = _globals.ResourceGroupProvider.CreateGroup(
			disposeContainedResourcesWhenDisposed: true,
			name: LocalMaterialBuilder.TestMaterialName + " Texture Group"
		);

		result.Add(LoadTexture(
			_builtInTextureLibrary.UvTestingTexture,
			new TextureCreationConfig { GenerateMipMaps = true, IsLinearColorspace = false, Name = LocalMaterialBuilder.TestMaterialName + " Color Map", ProcessingToApply = TextureProcessingConfig.None },
			new TextureReadConfig { IncludeWAlphaChannel = false }
		));

		result.Add(TextureBuilder.CreateNormalMap(
			TexturePattern.Rectangles(
				interiorSize: (128, 128),
				borderSize: (8, 8),
				paddingSize: (0, 0),
				interiorValue: SphericalTranslation.ZeroZero,
				borderRightValue: new SphericalTranslation(Orientation2D.Right.ToPolarAngle()!.Value, 45f),
				borderTopValue: new SphericalTranslation(Orientation2D.Up.ToPolarAngle()!.Value, 45f),
				borderLeftValue: new SphericalTranslation(Orientation2D.Left.ToPolarAngle()!.Value, 45f),
				borderBottomValue: new SphericalTranslation(Orientation2D.Down.ToPolarAngle()!.Value, 45f),
				paddingValue: SphericalTranslation.ZeroZero,
				repetitions: (8, 8)
			),
			name: LocalMaterialBuilder.TestMaterialName + " Normal Map"
		));

		return result;
	}

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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_texture_file_data")]
	static extern InteropResult GetTextureFileData(
		ref readonly byte utf8FileNameBufferPtr,
		out int outWidth,
		out int outHeight,
		out int outChannelCount
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
	Model HandleToInstance(ResourceHandle<Model> h) => new(h, this);
	BackdropTexture HandleToInstance(ResourceHandle<BackdropTexture> h) => new(h, _backdropTextureImplProvider);

	public override string ToString() => _isDisposed ? "TinyFFR Local Asset Loader [Disposed]" : "TinyFFR Local Asset Loader";

	#region Disposal
	public bool IsDisposed(ResourceHandle<Model> handle) => _isDisposed || !_loadedModels.ContainsKey(handle);
	public bool IsDisposed(ResourceHandle<BackdropTexture> handle) => _isDisposed || !_loadedBackdropTextures.ContainsKey(handle);

	public void Dispose(ResourceHandle<Model> handle) => Dispose(handle, removeFromCollection: true);
	void Dispose(ResourceHandle<Model> handle, bool removeFromCollection) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		_loadedModels[handle].Dispose();
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromCollection) _loadedModels.Remove(handle);
	}

	public void Dispose(ResourceHandle<BackdropTexture> handle) => Dispose(handle, removeFromCollection: true);
	void Dispose(ResourceHandle<BackdropTexture> handle, bool removeFromCollection) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		var data = _loadedBackdropTextures[handle];
		LocalFrameSynchronizationManager.QueueResourceDisposal(data.IblTextureHandle, &UnloadIblFileFromMemory);
		LocalFrameSynchronizationManager.QueueResourceDisposal(data.SkyboxTextureHandle, &UnloadSkyboxFileFromMemory);
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromCollection) _loadedBackdropTextures.Remove(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var model in _loadedModels.Keys) Dispose(model, removeFromCollection: false);
			foreach (var backdropTex in _loadedBackdropTextures.Keys) Dispose(backdropTex, removeFromCollection: false);
			_ktxFileBufferPool.Dispose();
			_vertexTriangleBufferPool.Dispose();
			_assetFilePathBuffer.Dispose();
			_meshBuilder.Dispose();
			_materialBuilder.Dispose();

			if (_testMaterialTextures.IsValueCreated) {
				_testMaterialTextures.Value.Dispose(disposeContainedResources: true);
			}

			_textureBuilder.Dispose();
			_loadedBackdropTextures.Dispose();
			_loadedModels.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(IAssetLoader));
	}
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Model> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Model));
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<BackdropTexture> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(BackdropTexture));
	#endregion
}