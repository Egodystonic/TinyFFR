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

sealed unsafe class LocalAssetLoader : ILocalAssetLoader, IEnvironmentCubemapImplProvider, IDisposable {
	readonly record struct CubemapData(UIntPtr SkyboxTextureHandle, UIntPtr IblTextureHandle);
	const string DefaultEnvironmentCubemapName = "Unnamed Environment Cubemap";
	const string HdrPreprocessorNameWin = "cmgen.exe";
	const string HdrPreprocessorNameLinux = "cmgen";
	const string HdrPreprocessorNameMacos = "cmgen_mac";
	const string HdrPreprocessorResourceNameStart = "Assets.Local.";
	const string HdrPreprocessedSkyboxFileSearch = "*_skybox.ktx";
	const string HdrPreprocessedIblFileSearch = "*_ibl.ktx";
	readonly string _hdrPreprocessorFilePath;
	readonly string _hdrPreprocessorResourceName;
	readonly LocalBuiltInTexturePathLibrary _builtInTextureLibrary = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly LocalMeshBuilder _meshBuilder;
	readonly LocalTextureBuilder _textureBuilder;
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
	public ITextureBuilder TextureBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _textureBuilder;
	public IMaterialBuilder MaterialBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _materialBuilder;
	public IBuiltInTexturePathLibrary BuiltInTexturePaths => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _builtInTextureLibrary;

	public LocalAssetLoader(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config) {
		ArgumentNullException.ThrowIfNull(globals);
		ArgumentNullException.ThrowIfNull(config);

		_globals = globals;
		_meshBuilder = new LocalMeshBuilder(globals);
		_textureBuilder = new LocalTextureBuilder(globals, config);
		_materialBuilder = new LocalMaterialBuilder(globals, config, this);
		_assetFilePathBuffer = new InteropStringBuffer(config.MaxAssetFilePathLengthChars, addOneForNullTerminator: true);
		_vertexTriangleBufferPool = new FixedByteBufferPool(config.MaxAssetVertexIndexBufferSizeBytes);
		_ktxFileBufferPool = new FixedByteBufferPool(config.MaxKtxFileBufferSizeBytes);
		_maxHdrProcessingTime = config.MaxHdrProcessingTime;

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
	public Texture LoadTexture(in TextureReadConfig readConfig, in TextureCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		
		var builtInTexel = _builtInTextureLibrary.GetBuiltInTexel(readConfig.FilePath);
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

		try {
			_assetFilePathBuffer.ConvertFromUtf16(readConfig.FilePath);
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
			if (!File.Exists(readConfig.FilePath.ToString())) throw new InvalidOperationException($"File '{readConfig.FilePath}' does not exist.", e);
			else throw;
		}
	}
	public TextureReadMetadata ReadTextureMetadata(ReadOnlySpan<char> filePath) {
		ThrowIfThisIsDisposed();

		var builtInTexel = _builtInTextureLibrary.GetBuiltInTexel(filePath);
		if (builtInTexel.HasValue) return new TextureReadMetadata((1, 1), builtInTexel.Value.Second.HasValue);

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

		var builtInTexel = _builtInTextureLibrary.GetBuiltInTexel(filePath);
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
	public Mesh LoadMesh(in MeshReadConfig readConfig, in MeshCreationConfig config) {
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();
		config.ThrowIfInvalid();

		try {
			_assetFilePathBuffer.ConvertFromUtf16(readConfig.FilePath);
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
			if (!File.Exists(readConfig.FilePath.ToString())) throw new InvalidOperationException($"File '{readConfig.FilePath}' does not exist.", e);
			else throw;
		}
	}
	public MeshReadCountData ReadMesh(in MeshReadConfig readConfig, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer) {
		ThrowIfThisIsDisposed();
		readConfig.ThrowIfInvalid();

		try {
			_assetFilePathBuffer.ConvertFromUtf16(readConfig.FilePath);
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
			if (!File.Exists(readConfig.FilePath.ToString())) throw new InvalidOperationException($"File '{readConfig.FilePath}' does not exist.", e);
			else throw;
		}
	}
	#endregion

	#region Environment / Cubemap
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

	public void PreprocessHdrTextureToEnvironmentCubemapDirectory(ReadOnlySpan<char> hdrFilePath, ReadOnlySpan<char> destinationDirectoryPath) {
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
				_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, config.Name, DefaultEnvironmentCubemapName);
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
			_textureBuilder.Dispose();
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