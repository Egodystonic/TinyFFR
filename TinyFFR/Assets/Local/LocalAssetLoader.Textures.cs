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
	readonly LocalBuiltInTexturePathLibrary _builtInTextureLibrary = new();
	readonly LocalTextureBuilder _textureBuilder;
	readonly LocalMaterialBuilder _materialBuilder;
	readonly Lazy<ResourceGroup> _testMaterialTextures;

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
	PooledHeapMemory<TexelRgba32> ReadTextureForCombination(ReadOnlySpan<char> filePath, TextureReadMetadata metadata, in TextureProcessingConfig processingConfig) {
		processingConfig.ThrowIfInvalid();
		var result = _globals.HeapPool.Borrow<TexelRgba32>(metadata.Dimensions.Area);
		ReadTexture(filePath, in processingConfig, result.Buffer);
		return result;
	}
	
	void CombineTextures<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig, TextureReadMetadata aMetadata,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig, TextureReadMetadata bMetadata,
		TextureCombinationConfig combinationConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> {
		using var aPool = ReadTextureForCombination(aFilePath, aMetadata, in aProcessingConfig);
		using var bPool = ReadTextureForCombination(bFilePath, bMetadata, in bProcessingConfig);
		var destDimensions = TextureUtils.GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions);
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

		if (typeof(TTexel) == typeof(TexelRgba32)) {
			var rgbaBuffer = MemoryMarshal.Cast<TTexel, TexelRgba32>(destinationBuffer);
			TextureUtils.CombineTextures(aBuffer, aMetadata.Dimensions, bBuffer, bMetadata.Dimensions, combinationConfig, rgbaBuffer);
		}
		else {
			using var destPool = _globals.HeapPool.Borrow<TexelRgba32>(destDimensions.Area);
			TextureUtils.CombineTextures(aBuffer, aMetadata.Dimensions, bBuffer, bMetadata.Dimensions, combinationConfig, destPool.Buffer);
			for (var i = 0; i < destDimensions.Area; ++i) destinationBuffer[i] = TTexel.ConvertFrom(destPool.Buffer[i]);
		}
	}
	void CombineTextures<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig, TextureReadMetadata aMetadata,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig, TextureReadMetadata bMetadata,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig, TextureReadMetadata cMetadata,
		TextureCombinationConfig combinationConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> {
		using var aPool = ReadTextureForCombination(aFilePath, aMetadata, in aProcessingConfig);
		using var bPool = ReadTextureForCombination(bFilePath, bMetadata, in bProcessingConfig);
		using var cPool = ReadTextureForCombination(cFilePath, cMetadata, in cProcessingConfig);
		var destDimensions = TextureUtils.GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions);
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

		if (typeof(TTexel) == typeof(TexelRgba32)) {
			var rgbaBuffer = MemoryMarshal.Cast<TTexel, TexelRgba32>(destinationBuffer);
			TextureUtils.CombineTextures(aBuffer, aMetadata.Dimensions, bBuffer, bMetadata.Dimensions, cBuffer, cMetadata.Dimensions, combinationConfig, rgbaBuffer);
		}
		else {
			using var destPool = _globals.HeapPool.Borrow<TexelRgba32>(destDimensions.Area);
			TextureUtils.CombineTextures(aBuffer, aMetadata.Dimensions, bBuffer, bMetadata.Dimensions, cBuffer, cMetadata.Dimensions, combinationConfig, destPool.Buffer);
			for (var i = 0; i < destDimensions.Area; ++i) destinationBuffer[i] = TTexel.ConvertFrom(destPool.Buffer[i]);
		}
	}
	void CombineTextures<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig, TextureReadMetadata aMetadata,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig, TextureReadMetadata bMetadata,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig, TextureReadMetadata cMetadata,
		ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig, TextureReadMetadata dMetadata,
		TextureCombinationConfig combinationConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> {
		using var aPool = ReadTextureForCombination(aFilePath, aMetadata, in aProcessingConfig);
		using var bPool = ReadTextureForCombination(bFilePath, bMetadata, in bProcessingConfig);
		using var cPool = ReadTextureForCombination(cFilePath, cMetadata, in cProcessingConfig);
		using var dPool = ReadTextureForCombination(dFilePath, dMetadata, in dProcessingConfig);
		var destDimensions = TextureUtils.GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions, dMetadata.Dimensions);
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

		if (typeof(TTexel) == typeof(TexelRgba32)) {
			var rgbaBuffer = MemoryMarshal.Cast<TTexel, TexelRgba32>(destinationBuffer);
			TextureUtils.CombineTextures(aBuffer, aMetadata.Dimensions, bBuffer, bMetadata.Dimensions, cBuffer, cMetadata.Dimensions, dBuffer, dMetadata.Dimensions, combinationConfig, rgbaBuffer);
		}
		else {
			using var destPool = _globals.HeapPool.Borrow<TexelRgba32>(destDimensions.Area);
			TextureUtils.CombineTextures(aBuffer, aMetadata.Dimensions, bBuffer, bMetadata.Dimensions, cBuffer, cMetadata.Dimensions, dBuffer, dMetadata.Dimensions, combinationConfig, destPool.Buffer);
			for (var i = 0; i < destDimensions.Area; ++i) destinationBuffer[i] = TTexel.ConvertFrom(destPool.Buffer[i]);
		}
	}

	public Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) {
		var aMetadata = ReadTextureMetadata(aFilePath);
		var bMetadata = ReadTextureMetadata(bFilePath);
		var destDimensions = TextureUtils.GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions);

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
		var destDimensions = TextureUtils.GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions);

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
		var destDimensions = TextureUtils.GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions, dMetadata.Dimensions);

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
			TextureUtils.GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions), 
			aMetadata.IncludesAlphaChannel || bMetadata.IncludesAlphaChannel
		);
	}
	public TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath, ReadOnlySpan<char> cFilePath) {
		var aMetadata = ReadTextureMetadata(aFilePath);
		var bMetadata = ReadTextureMetadata(bFilePath);
		var cMetadata = ReadTextureMetadata(cFilePath);
		return new(
			TextureUtils.GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions),
			aMetadata.IncludesAlphaChannel || bMetadata.IncludesAlphaChannel || cMetadata.IncludesAlphaChannel
		);
	}
	public TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath, ReadOnlySpan<char> cFilePath, ReadOnlySpan<char> dFilePath) {
		var aMetadata = ReadTextureMetadata(aFilePath);
		var bMetadata = ReadTextureMetadata(bFilePath);
		var cMetadata = ReadTextureMetadata(cFilePath);
		var dMetadata = ReadTextureMetadata(dFilePath);
		return new(
			TextureUtils.GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions, dMetadata.Dimensions),
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
		var destDimensions = TextureUtils.GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions);

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
		var destDimensions = TextureUtils.GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions);

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
		var destDimensions = TextureUtils.GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions, dMetadata.Dimensions);

		CombineTextures(aFilePath, in aProcessingConfig, aMetadata, bFilePath, in bProcessingConfig, bMetadata, cFilePath, in cProcessingConfig, cMetadata, dFilePath, in dProcessingConfig, dMetadata, combinationConfig, destinationBuffer);
		TextureUtils.ProcessTexture(destinationBuffer, destDimensions, in finalOutputProcessingConfig);
		return destDimensions.Area;
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
	#endregion
}