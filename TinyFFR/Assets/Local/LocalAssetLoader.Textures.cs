// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.IO;
using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Assets.Local;

unsafe partial class LocalAssetLoader {
	readonly LocalBuiltInTexturePathLibrary _builtInTextureLibrary = new();
	readonly LocalTextureBuilder _textureBuilder;
	readonly LocalMaterialBuilder _materialBuilder;
	readonly Lazy<ResourceGroup> _testMaterialTextures;

	#region Read / Load Texture
	#region Async / Shared Loading Core
	readonly WorkerJobSyncHelper<LocalAssetLoader, TextureLoadContext, TextureLoadConfig> _textureLoadWorkerSyncHelper;
	readonly WorkerJobSyncHelper<LocalAssetLoader, CombinedTextureLoadContext, TextureCombinedLoadConfig> _combinedTextureLoadWorkerSyncHelper;

	internal sealed class TextureCreationMetadata {
		public IntPtr OwnedStbTexelBufferPtr { get; set; } = 0;
		public IntPtr BorrowedTexelBufferPtr { get; set; } = 0;
		public PooledHeapMemory<byte>? OwnedTexelData { get; set; } = null;
		public XYPair<int> Dimensions { get; set; } = default;
		public bool IsRgba { get; set; } = false;
		public bool GenerateMipMaps { get; set; } = false;
		public bool AllowsDynamicWrites { get; set; } = false;
		public TextureRenderingConfig RenderingConfig { get; set; } = new();
		public Quality? CompressionQuality { get; set; } = null;
		public TextureDataType DataType { get; set; } = TextureDataType.LinearData;
		public PooledHeapMemory<byte>? CompressedData { get; set; } = null;
		public TextureCompressionFormat CompressionFormat { get; set; } = TextureCompressionFormat.None;
		public int CompressedLevelCount { get; set; } = 0;

		public ReadOnlySpan<TexelRgba32> Rgba32Texels => MemoryMarshal.Cast<byte, TexelRgba32>(TexelBytes)[..Dimensions.Area];
		public ReadOnlySpan<TexelRgb24> Rgb24Texels => MemoryMarshal.Cast<byte, TexelRgb24>(TexelBytes)[..Dimensions.Area];

		ReadOnlySpan<byte> TexelBytes {
			get {
				var texelSizeBytes = IsRgba ? sizeof(TexelRgba32) : sizeof(TexelRgb24);
				if (OwnedTexelData is { } owned) return owned.Span;
				var ptr = OwnedStbTexelBufferPtr != IntPtr.Zero ? OwnedStbTexelBufferPtr : BorrowedTexelBufferPtr;
				if (ptr == IntPtr.Zero) throw new InvalidOperationException("No texel data set in context (this is a bug in TinyFFR).");
				return new ReadOnlySpan<byte>((void*) ptr, Dimensions.Area * texelSizeBytes);
			}
		}

		public void TearDown() {
			if (OwnedStbTexelBufferPtr != 0) {
				UnloadTextureFileFromMemory((void*) OwnedStbTexelBufferPtr).ThrowIfFailure();
				OwnedStbTexelBufferPtr = 0;
			}
			BorrowedTexelBufferPtr = 0;
			OwnedTexelData?.Dispose();
			OwnedTexelData = null;
			CompressedData?.Dispose();
			CompressedData = null;
			CompressionFormat = TextureCompressionFormat.None;
			CompressedLevelCount = 0;
			CompressionQuality = null;
			DataType = TextureDataType.LinearData;
			Dimensions = default;
			IsRgba = false;
			GenerateMipMaps = false;
			AllowsDynamicWrites = false;
			RenderingConfig = new();
		}
	}

	sealed unsafe class TextureLoadContext : WorkerJobSyncHelper<LocalAssetLoader, TextureLoadContext, TextureLoadConfig>.WorkerJobSyncHelperContext {
		public TextureCreationMetadata CreationMetadata { get; } = new();

		// Primary thread owned
		public PooledHeapMemory<char>? FilePath { get; set; } = null;
		public EmbeddedResourceResolver.ResourceDataRef? BuiltInEmbeddedDataRef { get; set; } = null;
		public PooledHeapMemory<byte>? BuiltInTexelData { get; set; } = null;
		public XYPair<int> BuiltInDimensions { get; set; } = default;
		public bool BuiltInContainsAlpha { get; set; } = false;
		public bool HasBuiltInSource { get; set; } = false;

		public override void TearDown() {
			CreationMetadata.TearDown();
			BuiltInTexelData?.Dispose();
			BuiltInTexelData = null;
			BuiltInEmbeddedDataRef = null;
			BuiltInDimensions = default;
			BuiltInContainsAlpha = false;
			HasBuiltInSource = false;
			FilePath?.Dispose();
			FilePath = null;
			HeapPool = null!;
			Self = null!;
		}
		
		public override void Dispose() { /* no-op */ }
	}

	sealed unsafe class CombinedTextureLoadContext : WorkerJobSyncHelper<LocalAssetLoader, CombinedTextureLoadContext, TextureCombinedLoadConfig>.WorkerJobSyncHelperContext {
		public TextureCreationMetadata CreationMetadata { get; } = new();

		// Primary thread owned
		public PooledHeapMemory<char>? FilePathAMemory { get; set; } = null;
		public PooledHeapMemory<char>? FilePathBMemory { get; set; } = null;
		public PooledHeapMemory<char>? FilePathCMemory { get; set; } = null;
		public PooledHeapMemory<char>? FilePathDMemory { get; set; } = null;
		public ReadOnlySpan<char> FilePathA => FilePathAMemory is { } mem ? mem.Span : throw new InvalidOperationException("Expected file path A to exist (this is a bug in TinyFFR).");
		public ReadOnlySpan<char> FilePathB => FilePathBMemory is { } mem ? mem.Span : throw new InvalidOperationException("Expected file path B to exist (this is a bug in TinyFFR).");
		public ReadOnlySpan<char> FilePathC => FilePathCMemory is { } mem ? mem.Span : throw new InvalidOperationException("Expected file path C to exist (this is a bug in TinyFFR).");
		public ReadOnlySpan<char> FilePathD => FilePathDMemory is { } mem ? mem.Span : throw new InvalidOperationException("Expected file path D to exist (this is a bug in TinyFFR).");
		

		public override void TearDown() {
			CreationMetadata.TearDown();
			FilePathAMemory?.Dispose();
			FilePathAMemory = null;
			FilePathBMemory?.Dispose();
			FilePathBMemory = null;
			FilePathCMemory?.Dispose();
			FilePathCMemory = null;
			FilePathDMemory?.Dispose();
			FilePathDMemory = null;
			HeapPool = null!;
			Self = null!;
		}
		
		public override void Dispose() { /* no-op */ }
	}

	void SetUpTextureLoadContext(TextureLoadContext context, ReadOnlySpan<char> filePath, ReadOnlySpan<char> name) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		context.SetName(name);

		switch (_builtInTextureLibrary.GetLikelyBuiltInTextureType(filePath)) {
			case LocalBuiltInTexturePathLibrary.BuiltInTextureType.Texel:
				var builtInTexel = _builtInTextureLibrary.TryGetBuiltInTexel(filePath);
				if (builtInTexel?.First is { } rgb) {
					var rgbBuffer = _globals.HeapPool.Borrow<byte>(sizeof(TexelRgb24));
					context.BuiltInTexelData = rgbBuffer;
					MemoryMarshal.Cast<byte, TexelRgb24>(rgbBuffer.Span)[0] = rgb;
					context.BuiltInDimensions = new(1, 1);
					context.BuiltInContainsAlpha = false;
					context.HasBuiltInSource = true;
					return;
				}
				if (builtInTexel?.Second is { } rgba) {
					var rgbaBuffer = _globals.HeapPool.Borrow<byte>(sizeof(TexelRgba32));
					context.BuiltInTexelData = rgbaBuffer;
					MemoryMarshal.Cast<byte, TexelRgba32>(rgbaBuffer.Span)[0] = rgba;
					context.BuiltInDimensions = new(1, 1);
					context.BuiltInContainsAlpha = true;
					context.HasBuiltInSource = true;
					return;
				}
				break;
			case LocalBuiltInTexturePathLibrary.BuiltInTextureType.EmbeddedResourceTexture:
				var embeddedTextureAssetData = _builtInTextureLibrary.TryGetBuiltInEmbeddedResourceTexture(filePath);
				if (embeddedTextureAssetData is { } tuple) {
					context.BuiltInEmbeddedDataRef = tuple.DataRef;
					context.BuiltInDimensions = tuple.Dimensions;
					context.BuiltInContainsAlpha = tuple.ContainsAlpha;
					context.HasBuiltInSource = true;
					return;
				}
				break;
		}

		context.FilePath = _globals.HeapPool.BorrowAndCopy(filePath);
	}

	public Texture LoadTexture(ReadOnlySpan<char> filePath, in TextureCreationConfig config, in TextureReadConfig readConfig) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		readConfig.ThrowIfInvalid();

		var contextWrapper = _textureLoadWorkerSyncHelper.CreateContextWrapper();
		SetUpTextureLoadContext(contextWrapper.Context, filePath, config.Name);

		return contextWrapper.DispatchResourceReturningSynchronousOperation(&LoadTextureCore, new TextureLoadConfig { CreationConfig = config, ReadConfig = readConfig });
	}

	public TinyFfrAsyncOperation<Texture> LoadTextureAsync(ReadOnlySpan<char> filePath, in TextureCreationConfig config, in TextureReadConfig readConfig) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		readConfig.ThrowIfInvalid();

		var contextWrapper = _textureLoadWorkerSyncHelper.CreateContextWrapper();
		SetUpTextureLoadContext(contextWrapper.Context, filePath, config.Name);

		return contextWrapper.DispatchResourceReturningAsynchronousOperation(&LoadTextureCore, new TextureLoadConfig { CreationConfig = config, ReadConfig = readConfig });
	}

	static Texture LoadTextureCore(TextureLoadContext context, in TextureLoadConfig config) {
		var creationConfig = config.CreationConfig;
		var readConfig = config.ReadConfig;
		var processingConfig = creationConfig.ProcessingToApply;
		var creationMetadata = context.CreationMetadata;

		ApplyCreationConfigToMetadata(creationMetadata, in creationConfig);

		if (context.HasBuiltInSource) {
			LoadBuiltInTextureCore(context, readConfig.ForceWAlphaChannelPresence, in processingConfig);
		}
		else if (context.FilePath is { } filePath) {
			LoadFileTextureCore(context, filePath.Span, in readConfig, in processingConfig);
		}
		else {
			throw new InvalidOperationException("No file path or built-in source set in context (this is a bug in TinyFFR).");
		}

		CompressTextureIfRequested(creationMetadata, context.HeapPool);

		return context.GenerateResourceOnPrimaryAndWait(&CompleteTextureLoad);
	}

	internal static void ApplyCreationConfigToMetadata(TextureCreationMetadata data, in TextureCreationConfig config) {
		data.GenerateMipMaps = config.GenerateMipMaps;
		data.AllowsDynamicWrites = config.AllowsDynamicWrites;
		data.RenderingConfig = config.RenderingConfig;
		data.CompressionQuality = config.CompressionQuality;
		data.DataType = config.DataType;
	}

	internal static void CompressTextureIfRequested(TextureCreationMetadata data, ThreadSafeHeapPoolWrapper heapPool) {
		var sourceTexelType = data.IsRgba ? TexelType.Rgba32 : TexelType.Rgb24;
		var format = TextureCompressor.GetRecommendedFormat(
			data.Dimensions,
			sourceTexelType,
			data.DataType,
			data.CompressionQuality,
			data.AllowsDynamicWrites,
			data.GenerateMipMaps
		);
		if (format == TextureCompressionFormat.None) return;

		var levelCount = data.GenerateMipMaps ? TextureUtils.GetMipLevelCount(data.Dimensions) : 1;
		var totalSizeBytes = TextureCompressor.GetCompressedSizeBytes(data.Dimensions, format, data.GenerateMipMaps);
		var destination = heapPool.Borrow<byte>(totalSizeBytes);

		try {
			if (data.IsRgba) {
				TextureCompressor.Compress(data.Rgba32Texels, data.Dimensions, format, data.CompressionQuality!.Value, data.DataType, data.GenerateMipMaps, destination.Span);
			}
			else {
				TextureCompressor.Compress(data.Rgb24Texels, data.Dimensions, format, data.CompressionQuality!.Value, data.DataType, data.GenerateMipMaps, destination.Span);
			}
		}
		catch {
			destination.Dispose();
			throw;
		}

		data.CompressedData = destination;
		data.CompressionFormat = format;
		data.CompressedLevelCount = levelCount;
	}

	static void LoadBuiltInTextureCore(TextureLoadContext context, bool forceAlpha, in TextureProcessingConfig processingConfig) {
		var creationMetadata = context.CreationMetadata;
		creationMetadata.Dimensions = context.BuiltInDimensions;

		var sourceIsRgba = context.BuiltInContainsAlpha;
		var expandToRgba = forceAlpha && !sourceIsRgba;
		var mustCopy = expandToRgba || processingConfig.RequiresProcessing || context.BuiltInEmbeddedDataRef == null;

		ReadOnlySpan<byte> sourceBytes;
		if (context.BuiltInEmbeddedDataRef is { } dataRef) sourceBytes = dataRef.AsSpan;
		else if (context.BuiltInTexelData is { } builtInTexelData) sourceBytes = builtInTexelData.Span;
		else throw new InvalidOperationException("No built-in texture data set in context (this is a bug in TinyFFR).");

		var texelCount = creationMetadata.Dimensions.Area;

		if (!mustCopy) {
			creationMetadata.IsRgba = sourceIsRgba;
			creationMetadata.BorrowedTexelBufferPtr = (nint) context.BuiltInEmbeddedDataRef!.Value.DataPtr;
			return;
		}

		creationMetadata.IsRgba = sourceIsRgba || expandToRgba;
		var destTexelSizeBytes = creationMetadata.IsRgba ? sizeof(TexelRgba32) : sizeof(TexelRgb24);
		var destBuffer = context.HeapPool.Borrow<byte>(texelCount * destTexelSizeBytes);
		creationMetadata.OwnedTexelData = destBuffer;

		if (expandToRgba) {
			TextureUtils.Convert(
				MemoryMarshal.Cast<byte, TexelRgb24>(sourceBytes)[..texelCount],
				MemoryMarshal.Cast<byte, TexelRgba32>(destBuffer.Span)
			);
		}
		else {
			sourceBytes[..(texelCount * destTexelSizeBytes)].CopyTo(destBuffer.Span);
		}

		if (!processingConfig.RequiresProcessing) return;
		if (creationMetadata.IsRgba) TextureUtils.ProcessTexture(MemoryMarshal.Cast<byte, TexelRgba32>(destBuffer.Span), creationMetadata.Dimensions, in processingConfig);
		else TextureUtils.ProcessTexture(MemoryMarshal.Cast<byte, TexelRgb24>(destBuffer.Span), creationMetadata.Dimensions, in processingConfig);
	}

	static void LoadFileTextureCore(TextureLoadContext context, ReadOnlySpan<char> filePath, in TextureReadConfig readConfig, in TextureProcessingConfig processingConfig) {
		var creationMetadata = context.CreationMetadata;
		var forceAlpha = readConfig.ForceWAlphaChannelPresence;
		var includeWAlphaChannel = readConfig.IncludeWAlphaChannel;

		try {
			var pathBuffer = context.Self.AssetFilePathBuffer;
			pathBuffer.ConvertFromUtf16(filePath);
			GetTextureFileData(
				in pathBuffer.AsRef,
				out _,
				out _,
				out var channelCount
			).ThrowIfFailure();

			var includeAlpha = includeWAlphaChannel && (channelCount > 3 || forceAlpha);

			LoadTextureFileInToMemory(
				in pathBuffer.AsRef,
				includeWAlphaChannel: includeAlpha,
				out var width,
				out var height,
				out var texelBuffer
			).ThrowIfFailure();
			creationMetadata.OwnedStbTexelBufferPtr = (nint) texelBuffer;

			if (width < 0 || height < 0) throw new InvalidOperationException($"Loaded texture had width/height of {width}/{height}.");

			creationMetadata.Dimensions = new(width, height);
			creationMetadata.IsRgba = includeAlpha;

			if (!processingConfig.RequiresProcessing) return;
			var texelCount = width * height;
			if (includeAlpha) TextureUtils.ProcessTexture(new Span<TexelRgba32>(texelBuffer, texelCount), creationMetadata.Dimensions, in processingConfig);
			else TextureUtils.ProcessTexture(new Span<TexelRgb24>(texelBuffer, texelCount), creationMetadata.Dimensions, in processingConfig);
		}
		catch (Exception e) {
			if (!File.Exists(new String(filePath))) throw new InvalidOperationException($"File '{filePath}' does not exist.", e);
			else throw;
		}
	}

	static Texture CompleteTextureLoad(TextureLoadContext context) => CompleteTextureLoad(context.Self, context.CreationMetadata, context.Name);

	static Texture CompleteTextureLoad(LocalAssetLoader self, TextureCreationMetadata data, ReadOnlySpan<char> name) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();

		Texture result;
		if (data.CompressionFormat != TextureCompressionFormat.None && data.CompressedData is { } compressedData) {
			result = self._textureBuilder.CreateTextureFromCompressedBlocks(
				compressedData.Span,
				data.Dimensions,
				data.CompressionFormat,
				data.CompressedLevelCount,
				data.IsRgba ? TexelType.Rgba32 : TexelType.Rgb24,
				data.RenderingConfig,
				name
			);
		}
		else {
			result = data.IsRgba
				? self._textureBuilder.CreateTextureWithoutProcessing(data.Rgba32Texels, data.Dimensions, data.GenerateMipMaps, data.AllowsDynamicWrites, data.RenderingConfig, null, data.DataType, name)
				: self._textureBuilder.CreateTextureWithoutProcessing(data.Rgb24Texels, data.Dimensions, data.GenerateMipMaps, data.AllowsDynamicWrites, data.RenderingConfig, null, data.DataType, name);
		}

		self.RegisterInBakery(result, data, name);
		return result;
	}
	#endregion

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
			var pathBuffer = AssetFilePathBuffer;
			pathBuffer.ConvertFromUtf16(filePath);
			GetTextureFileData(
				in pathBuffer.AsRef,
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
						TextureUtils.ProcessTexture(destinationBuffer[..texelData.Length], tuple.Dimensions, in processingConfig);
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
						TextureUtils.ProcessTexture(destinationBuffer[..texelData.Length], tuple.Dimensions, in processingConfig);
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
			var pathBuffer = AssetFilePathBuffer;
			pathBuffer.ConvertFromUtf16(filePath);
			LoadTextureFileInToMemory(
				in pathBuffer.AsRef,
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
		var result = _globals.HeapPool.ThreadSafeWrapper.Borrow<TexelRgba32>(metadata.Dimensions.Area);
		ReadTexture(filePath, in processingConfig, result.Span);
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

		var aBuffer = aPool.Span;
		var bBuffer = bPool.Span;

		if (typeof(TTexel) == typeof(TexelRgba32)) {
			var rgbaBuffer = MemoryMarshal.Cast<TTexel, TexelRgba32>(destinationBuffer);
			TextureUtils.CombineTextures(aBuffer, aMetadata.Dimensions, bBuffer, bMetadata.Dimensions, combinationConfig, rgbaBuffer);
		}
		else {
			using var destPool = _globals.HeapPool.ThreadSafeWrapper.Borrow<TexelRgba32>(destDimensions.Area);
			TextureUtils.CombineTextures(aBuffer, aMetadata.Dimensions, bBuffer, bMetadata.Dimensions, combinationConfig, destPool.Span);
			for (var i = 0; i < destDimensions.Area; ++i) destinationBuffer[i] = TTexel.ConvertFrom(destPool.Span[i]);
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

		var aBuffer = aPool.Span;
		var bBuffer = bPool.Span;
		var cBuffer = cPool.Span;

		if (typeof(TTexel) == typeof(TexelRgba32)) {
			var rgbaBuffer = MemoryMarshal.Cast<TTexel, TexelRgba32>(destinationBuffer);
			TextureUtils.CombineTextures(aBuffer, aMetadata.Dimensions, bBuffer, bMetadata.Dimensions, cBuffer, cMetadata.Dimensions, combinationConfig, rgbaBuffer);
		}
		else {
			using var destPool = _globals.HeapPool.ThreadSafeWrapper.Borrow<TexelRgba32>(destDimensions.Area);
			TextureUtils.CombineTextures(aBuffer, aMetadata.Dimensions, bBuffer, bMetadata.Dimensions, cBuffer, cMetadata.Dimensions, combinationConfig, destPool.Span);
			for (var i = 0; i < destDimensions.Area; ++i) destinationBuffer[i] = TTexel.ConvertFrom(destPool.Span[i]);
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

		var aBuffer = aPool.Span;
		var bBuffer = bPool.Span;
		var cBuffer = cPool.Span;
		var dBuffer = dPool.Span;

		if (typeof(TTexel) == typeof(TexelRgba32)) {
			var rgbaBuffer = MemoryMarshal.Cast<TTexel, TexelRgba32>(destinationBuffer);
			TextureUtils.CombineTextures(aBuffer, aMetadata.Dimensions, bBuffer, bMetadata.Dimensions, cBuffer, cMetadata.Dimensions, dBuffer, dMetadata.Dimensions, combinationConfig, rgbaBuffer);
		}
		else {
			using var destPool = _globals.HeapPool.ThreadSafeWrapper.Borrow<TexelRgba32>(destDimensions.Area);
			TextureUtils.CombineTextures(aBuffer, aMetadata.Dimensions, bBuffer, bMetadata.Dimensions, cBuffer, cMetadata.Dimensions, dBuffer, dMetadata.Dimensions, combinationConfig, destPool.Span);
			for (var i = 0; i < destDimensions.Area; ++i) destinationBuffer[i] = TTexel.ConvertFrom(destPool.Span[i]);
		}
	}

	void SetUpCombinedTextureLoadContext(CombinedTextureLoadContext context, ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath, ReadOnlySpan<char> cFilePath, ReadOnlySpan<char> dFilePath, int sourceCount, ReadOnlySpan<char> name) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		context.SetName(name);
		context.FilePathAMemory = _globals.HeapPool.BorrowAndCopy(aFilePath);
		context.FilePathBMemory = _globals.HeapPool.BorrowAndCopy(bFilePath);
		if (sourceCount > 2) context.FilePathCMemory = _globals.HeapPool.BorrowAndCopy(cFilePath);
		if (sourceCount > 3) context.FilePathDMemory = _globals.HeapPool.BorrowAndCopy(dFilePath);
	}

	Texture DispatchSynchronousCombinedLoad(ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig, ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig, ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig, ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig, int sourceCount, TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		finalOutputConfig.ThrowIfInvalid();
		combinationConfig.ThrowIfInvalid(sourceCount);

		var contextWrapper = _combinedTextureLoadWorkerSyncHelper.CreateContextWrapper();
		SetUpCombinedTextureLoadContext(contextWrapper.Context, aFilePath, bFilePath, cFilePath, dFilePath, sourceCount, finalOutputConfig.Name);

		return contextWrapper.DispatchResourceReturningSynchronousOperation(
			&LoadCombinedTextureCore,
			new TextureCombinedLoadConfig {
				CreationConfig = finalOutputConfig,
				CombinationConfig = combinationConfig,
				ProcessingConfigA = aProcessingConfig,
				ProcessingConfigB = bProcessingConfig,
				ProcessingConfigC = cProcessingConfig,
				ProcessingConfigD = dProcessingConfig,
				SourceCount = sourceCount
			}
		);
	}

	TinyFfrAsyncOperation<Texture> DispatchAsynchronousCombinedLoad(ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig, ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig, ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig, ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig, int sourceCount, TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		finalOutputConfig.ThrowIfInvalid();
		combinationConfig.ThrowIfInvalid(sourceCount);

		var contextWrapper = _combinedTextureLoadWorkerSyncHelper.CreateContextWrapper();
		SetUpCombinedTextureLoadContext(contextWrapper.Context, aFilePath, bFilePath, cFilePath, dFilePath, sourceCount, finalOutputConfig.Name);

		return contextWrapper.DispatchResourceReturningAsynchronousOperation(
			&LoadCombinedTextureCore,
			new TextureCombinedLoadConfig {
				CreationConfig = finalOutputConfig,
				CombinationConfig = combinationConfig,
				ProcessingConfigA = aProcessingConfig,
				ProcessingConfigB = bProcessingConfig,
				ProcessingConfigC = cProcessingConfig,
				ProcessingConfigD = dProcessingConfig,
				SourceCount = sourceCount
			}
		);
	}

	public Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => DispatchSynchronousCombinedLoad(aFilePath, in aProcessingConfig, bFilePath, in bProcessingConfig, default, TextureProcessingConfig.None, default, TextureProcessingConfig.None, 2, combinationConfig, in finalOutputConfig);

	public Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => DispatchSynchronousCombinedLoad(aFilePath, in aProcessingConfig, bFilePath, in bProcessingConfig, cFilePath, in cProcessingConfig, default, TextureProcessingConfig.None, 3, combinationConfig, in finalOutputConfig);

	public Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => DispatchSynchronousCombinedLoad(aFilePath, in aProcessingConfig, bFilePath, in bProcessingConfig, cFilePath, in cProcessingConfig, dFilePath, in dProcessingConfig, 4, combinationConfig, in finalOutputConfig);

	public TinyFfrAsyncOperation<Texture> LoadCombinedTextureAsync(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => DispatchAsynchronousCombinedLoad(aFilePath, in aProcessingConfig, bFilePath, in bProcessingConfig, default, TextureProcessingConfig.None, default, TextureProcessingConfig.None, 2, combinationConfig, in finalOutputConfig);

	public TinyFfrAsyncOperation<Texture> LoadCombinedTextureAsync(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => DispatchAsynchronousCombinedLoad(aFilePath, in aProcessingConfig, bFilePath, in bProcessingConfig, cFilePath, in cProcessingConfig, default, TextureProcessingConfig.None, 3, combinationConfig, in finalOutputConfig);

	public TinyFfrAsyncOperation<Texture> LoadCombinedTextureAsync(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => DispatchAsynchronousCombinedLoad(aFilePath, in aProcessingConfig, bFilePath, in bProcessingConfig, cFilePath, in cProcessingConfig, dFilePath, in dProcessingConfig, 4, combinationConfig, in finalOutputConfig);

	static Texture LoadCombinedTextureCore(CombinedTextureLoadContext context, in TextureCombinedLoadConfig config) {
		var self = context.Self;
		var creationConfig = config.CreationConfig;
		var combinationConfig = config.CombinationConfig;
		var sourceCount = config.SourceCount;
		var data = context.CreationMetadata;

		ApplyCreationConfigToMetadata(data, in creationConfig);

		var aMetadata = self.ReadTextureMetadata(context.FilePathA);
		var bMetadata = self.ReadTextureMetadata(context.FilePathB);
		var cMetadata = sourceCount > 2 ? self.ReadTextureMetadata(context.FilePathC) : default;
		var dMetadata = sourceCount > 3 ? self.ReadTextureMetadata(context.FilePathD) : default;

		var destDimensions = sourceCount switch {
			2 => TextureUtils.GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions),
			3 => TextureUtils.GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions),
			_ => TextureUtils.GetCombinedTextureDimensions(aMetadata.Dimensions, bMetadata.Dimensions, cMetadata.Dimensions, dMetadata.Dimensions)
		};

		data.Dimensions = destDimensions;
		data.IsRgba = combinationConfig.OutputTextureWAlphaChannelSource != null;

		var destTexelSizeBytes = data.IsRgba ? sizeof(TexelRgba32) : sizeof(TexelRgb24);
		ThrowIfAssetBufferSizeExceedsMaximum((long) destDimensions.Area * destTexelSizeBytes, $"combined texture ({destDimensions.X}x{destDimensions.Y})");
		var destBuffer = context.HeapPool.Borrow<byte>(destDimensions.Area * destTexelSizeBytes);
		data.OwnedTexelData = destBuffer;

		if (data.IsRgba) {
			var destSpan = MemoryMarshal.Cast<byte, TexelRgba32>(destBuffer.Span);
			CombineSourcesOnWorker(context, in config, aMetadata, bMetadata, cMetadata, dMetadata, destSpan);
			TextureUtils.ProcessTexture(destSpan, destDimensions, creationConfig.ProcessingToApply);
		}
		else {
			var destSpan = MemoryMarshal.Cast<byte, TexelRgb24>(destBuffer.Span);
			CombineSourcesOnWorker(context, in config, aMetadata, bMetadata, cMetadata, dMetadata, destSpan);
			TextureUtils.ProcessTexture(destSpan, destDimensions, creationConfig.ProcessingToApply);
		}

		CompressTextureIfRequested(data, context.HeapPool);

		return context.GenerateResourceOnPrimaryAndWait(&CompleteTextureLoad);
	}

	static void CombineSourcesOnWorker<TTexel>(CombinedTextureLoadContext context, in TextureCombinedLoadConfig config, TextureReadMetadata aMetadata, TextureReadMetadata bMetadata, TextureReadMetadata cMetadata, TextureReadMetadata dMetadata, Span<TTexel> destinationBuffer) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> {
		var self = context.Self;
		switch (config.SourceCount) {
			case 2:
				self.CombineTextures(
					context.FilePathA, config.ProcessingConfigA, aMetadata,
					context.FilePathB, config.ProcessingConfigB, bMetadata,
					config.CombinationConfig, destinationBuffer
				);
				break;
			case 3:
				self.CombineTextures(
					context.FilePathA, config.ProcessingConfigA, aMetadata,
					context.FilePathB, config.ProcessingConfigB, bMetadata,
					context.FilePathC, config.ProcessingConfigC, cMetadata,
					config.CombinationConfig, destinationBuffer
				);
				break;
			default:
				self.CombineTextures(
					context.FilePathA, config.ProcessingConfigA, aMetadata,
					context.FilePathB, config.ProcessingConfigB, bMetadata,
					context.FilePathC, config.ProcessingConfigC, cMetadata,
					context.FilePathD, config.ProcessingConfigD, dMetadata,
					config.CombinationConfig, destinationBuffer
				);
				break;
		}
	}

	static Texture CompleteTextureLoad(CombinedTextureLoadContext context) => CompleteTextureLoad(context.Self, context.CreationMetadata, context.Name);

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
			new TextureCreationConfig { GenerateMipMaps = true, DataType = TextureDataType.ColorSrgb, Name = LocalMaterialBuilder.TestMaterialName + " Color Map", ProcessingToApply = TextureProcessingConfig.None },
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

	#region Baking
	const string BakerySectionTextureDimensionsX = "dimensions_x";
	const string BakerySectionTextureDimensionsY = "dimensions_y";
	const string BakerySectionTextureIsRgba = "is_rgba";
	const string BakerySectionTextureMipMapsEnabled = "mipmaps_enabled";
	const string BakerySectionTextureAllowsDynamicWrites = "allows_dynamic_writes";
	const string BakerySectionTextureDataType = "data_type";
	const string BakerySectionTextureCompressionFormat = "compression_format";
	const string BakerySectionTextureCompressedLevelCount = "compressed_level_count";
	const string BakerySectionTextureDisableTextureRepeat = "disable_texture_repeat";
	const string BakerySectionTextureDisableTexelBlending = "disable_texel_blending";
	const string BakerySectionTextureAnisotropicFilteringQuality = "anisotropic_filtering_quality";
	const string BakerySectionTextureAnisotropyLevel = "anisotropy_level";
	const string BakerySectionTextureTexelData = "texel_data";

	void RegisterInBakery(Texture resource, TextureCreationMetadata data, ReadOnlySpan<char> name) {
		var bakery = _globals.Bakery;
		if (!bakery.Enabled) return;

		var isCompressed = data.CompressionFormat != TextureCompressionFormat.None && data.CompressedData is not null;

		bakery.StartResourceBake(resource);
		bakery.AddResourceBakeValue(resource, LocalAssetBakery.ResourceNameSectionName, name);
		bakery.AddResourceBakeValue(resource, BakerySectionTextureDimensionsX, data.Dimensions.X);
		bakery.AddResourceBakeValue(resource, BakerySectionTextureDimensionsY, data.Dimensions.Y);
		bakery.AddResourceBakeValue(resource, BakerySectionTextureIsRgba, data.IsRgba);
		bakery.AddResourceBakeValue(resource, BakerySectionTextureMipMapsEnabled, data.GenerateMipMaps);
		bakery.AddResourceBakeValue(resource, BakerySectionTextureAllowsDynamicWrites, data.AllowsDynamicWrites);
		bakery.AddResourceBakeValue(resource, BakerySectionTextureDataType, data.DataType);
		bakery.AddResourceBakeValue(resource, BakerySectionTextureCompressionFormat, isCompressed ? data.CompressionFormat : TextureCompressionFormat.None);
		bakery.AddResourceBakeValue(resource, BakerySectionTextureCompressedLevelCount, isCompressed ? data.CompressedLevelCount : 0);
		bakery.AddResourceBakeValue(resource, BakerySectionTextureDisableTextureRepeat, data.RenderingConfig.DisableTextureRepeat);
		bakery.AddResourceBakeValue(resource, BakerySectionTextureDisableTexelBlending, data.RenderingConfig.DisableTexelBlending);
		bakery.AddResourceBakeValue(resource, BakerySectionTextureAnisotropicFilteringQuality, data.RenderingConfig.AnisotropicFilteringQuality);
		bakery.AddResourceBakeValue(resource, BakerySectionTextureAnisotropyLevel, data.RenderingConfig.AnisotropyLevel);

		if (isCompressed) {
			bakery.AddResourceBakeValue(resource, BakerySectionTextureTexelData, data.CompressedData!.Value.Span);
		}
		else {
			bakery.AddResourceBakeValue(
				resource,
				BakerySectionTextureTexelData,
				data.IsRgba ? MemoryMarshal.AsBytes(data.Rgba32Texels) : MemoryMarshal.AsBytes(data.Rgb24Texels)
			);
		}

		bakery.CompleteResourceBake(resource);
	}

	public Texture LoadBakedTexture(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default) {
		return _globals.Bakery.Load(this, bakedAssetFilePath, name, &LoadBakedTextureCore);
	}

	public TinyFfrAsyncOperation<Texture> LoadBakedTextureAsync(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default) {
		return _globals.Bakery.LoadAsync(this, bakedAssetFilePath, name, &LoadBakedTextureCore);
	}

	static Texture LoadBakedTextureCore(LocalAssetBakery.AssetLoadContext ctx) {
		static Texture Finalize(LocalAssetBakery.AssetLoadContext ctx) {
			var assetData = ctx.AssetData;
			var self = ctx.Invoker<LocalAssetLoader>();

			var dimensions = new XYPair<int>(
				assetData.Extract<int>(BakerySectionTextureDimensionsX),
				assetData.Extract<int>(BakerySectionTextureDimensionsY)
			);
			var isRgba = assetData.Extract<bool>(BakerySectionTextureIsRgba);
			var compressionFormat = assetData.Extract<TextureCompressionFormat>(BakerySectionTextureCompressionFormat);

			var renderingConfig = new TextureRenderingConfig {
				DisableTextureRepeat = assetData.Extract<bool>(BakerySectionTextureDisableTextureRepeat),
				DisableTexelBlending = assetData.Extract<bool>(BakerySectionTextureDisableTexelBlending),
				AnisotropicFilteringQuality = assetData.Extract<Quality>(BakerySectionTextureAnisotropicFilteringQuality),
				AnisotropyLevel = assetData.Extract<float>(BakerySectionTextureAnisotropyLevel)
			};

			if (compressionFormat != TextureCompressionFormat.None) {
				return self._textureBuilder.CreateTextureFromCompressedBlocks(
					assetData.ExtractSpan<byte>(BakerySectionTextureTexelData),
					dimensions,
					compressionFormat,
					assetData.Extract<int>(BakerySectionTextureCompressedLevelCount),
					isRgba ? TexelType.Rgba32 : TexelType.Rgb24,
					renderingConfig,
					ctx.StoredOrOverridingName
				);
			}

			var mipMapsEnabled = assetData.Extract<bool>(BakerySectionTextureMipMapsEnabled);
			var allowsDynamicWrites = assetData.Extract<bool>(BakerySectionTextureAllowsDynamicWrites);
			var dataType = assetData.Extract<TextureDataType>(BakerySectionTextureDataType);

			return isRgba
				? self._textureBuilder.CreateTextureWithoutProcessing(assetData.ExtractSpan<TexelRgba32>(BakerySectionTextureTexelData), dimensions, mipMapsEnabled, allowsDynamicWrites, renderingConfig, null, dataType, ctx.StoredOrOverridingName)
				: self._textureBuilder.CreateTextureWithoutProcessing(assetData.ExtractSpan<TexelRgb24>(BakerySectionTextureTexelData), dimensions, mipMapsEnabled, allowsDynamicWrites, renderingConfig, null, dataType, ctx.StoredOrOverridingName);
		}

		return ctx.GenerateResourceOnPrimaryAndWait(&Finalize);
	}
	#endregion

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