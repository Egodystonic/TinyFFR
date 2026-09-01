// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Resources;
using System.Security;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Rendering.Local;
using Egodystonic.TinyFFR.Threading;
using static Egodystonic.TinyFFR.Assets.Materials.Local.LocalShaderPackageConstants;

namespace Egodystonic.TinyFFR.Assets.Materials.Local;

[SuppressUnmanagedCodeSecurity]
sealed unsafe class LocalTextureBuilder : ITextureBuilder, ITextureImplProvider, IResourceDirectory<Texture>, IDisposable {
	readonly record struct TextureData(XYPair<int> Dimensions, TexelType TexelType, bool AllowsDynamicWrites, bool ContainsMipMaps, TextureRenderingConfig RenderingConfig, TextureCompressionFormat CompressionFormat);
	const string DefaultTextureName = "Unnamed Texture";
	readonly ArrayPoolBackedMap<ResourceHandle<Texture>, TextureData> _loadedTextures = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	bool _isDisposed = false;

	public LocalTextureBuilder(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
	}

	#region Buffer Allocation
	public static void CheckConfigValidityAndProcessTexture<TTexel>(Span<TTexel> texels, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		generationConfig.ThrowIfInvalid();
		config.ThrowIfInvalid();

		if (generationConfig.Dimensions.Area > texels.Length) {
			throw new ArgumentException(
				$"Given config width/height require a buffer of {generationConfig.Dimensions.X}x{generationConfig.Dimensions.Y}={generationConfig.Dimensions.Area} texels, " +
				$"but supplied texel buffer only has {texels.Length} texels.",
				nameof(config)
			);
		}

		TextureUtils.ProcessTexture(texels, generationConfig.Dimensions, config.ProcessingToApply);
	}
	
	// Maintainer's note: The buffer is disposed on the native side when it's asynchronously loaded on to the GPU
	Texture ITextureBuilder.CreateTextureAndDisposePreallocatedBuffer<TTexel>(ITextureBuilder.PreallocatedBuffer<TTexel> preallocatedBuffer, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) => CreateTextureAndDisposePreallocatedBuffer(preallocatedBuffer, in generationConfig, in config);
	Texture CreateTextureAndDisposePreallocatedBuffer<TTexel>(ITextureBuilder.PreallocatedBuffer<TTexel> preallocatedBuffer, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();

		if (preallocatedBuffer.Span.IsEmpty) throw InvalidObjectException.InvalidDefault(typeof(ITextureBuilder.PreallocatedBuffer<TTexel>));
		CheckConfigValidityAndProcessTexture(preallocatedBuffer.Span, in generationConfig, in config);

		return CreateTextureAndDisposePreallocatedBuffer(
			preallocatedBuffer,
			generationConfig.Dimensions,
			config.GenerateMipMaps,
			config.AllowsDynamicWrites,
			config.RenderingConfig,
			config.CompressionQuality,
			config.DataType,
			config.Name
		);
	}

	Texture CreateTextureAndDisposePreallocatedBuffer<TTexel>(ITextureBuilder.PreallocatedBuffer<TTexel> preallocatedBuffer, XYPair<int> dimensions, bool generateMipMaps, bool allowsDynamicWrites, TextureRenderingConfig renderingConfig, Quality? compressionQuality, TextureDataType dataType, ReadOnlySpan<char> name) where TTexel : unmanaged, ITexel<TTexel> {
		var compressionFormat = TextureCompressor.GetRecommendedFormat(
			dimensions,
			TTexel.BlitType,
			dataType,
			compressionQuality,
			allowsDynamicWrites,
			generateMipMaps
		);

		if (compressionFormat != TextureCompressionFormat.None && compressionQuality is { } cq) {
			return CreateCompressedTextureAndDisposePreallocatedBuffer(preallocatedBuffer, dimensions, generateMipMaps, renderingConfig, compressionFormat, cq, dataType, name);
		}

		var dataPointer = Unsafe.AsPointer(ref MemoryMarshal.GetReference(preallocatedBuffer.Span));
		var dataLength = preallocatedBuffer.Span.Length * sizeof(TTexel);

		var bakeryBufferCopy = GetBakeryTexelBufferCopyIfEnabled(MemoryMarshal.AsBytes(preallocatedBuffer.Span[..dimensions.Area]));

		UIntPtr outHandle;
		switch (TTexel.BlitType) {
			case TexelType.Rgb24:
				LoadTextureRgb24(
					preallocatedBuffer.BufferId,
					(TexelRgb24*) dataPointer,
					dataLength,
					(uint) dimensions.X,
					(uint) dimensions.Y,
					generateMipMaps,
					dataType.UsesLinearColorspace(),
					out outHandle
				).ThrowIfFailure();
				break;
			case TexelType.Rgba32:
				LoadTextureRgba32(
					preallocatedBuffer.BufferId,
					(TexelRgba32*) dataPointer,
					dataLength,
					(uint) dimensions.X,
					(uint) dimensions.Y,
					generateMipMaps,
					dataType.UsesLinearColorspace(),
					out outHandle
				).ThrowIfFailure();
				break;
			default:
				throw new InvalidOperationException($"Unknown or unsupported texel type '{typeof(TTexel)}' (BlitType property '{TTexel.BlitType}').");
		}

		var handle = (ResourceHandle<Texture>) outHandle;
		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, name, DefaultTextureName);
		_loadedTextures.Add(handle, new(dimensions, TTexel.BlitType, allowsDynamicWrites, generateMipMaps, renderingConfig, TextureCompressionFormat.None));
		var result = HandleToInstance(handle);

		if (bakeryBufferCopy is { } bbc) {
			try {
				RegisterInBakery(result, dimensions, TTexel.BlitType == TexelType.Rgba32, generateMipMaps, allowsDynamicWrites, dataType, renderingConfig, TextureCompressionFormat.None, 0, bbc.Span, name);
			}
			finally {
				bbc.Dispose();
			}
		}

		return result;
	}

	public Texture CreateTextureFromCompressedBlocks(ReadOnlySpan<byte> blocks, XYPair<int> dimensions, TextureCompressionFormat compressionFormat, int levelCount, TexelType sourceTexelType, TextureDataType dataType, TextureRenderingConfig renderingConfig, ReadOnlySpan<char> name) {
		ThrowIfThisIsDisposed();
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		if (compressionFormat == TextureCompressionFormat.None) {
			throw new ArgumentOutOfRangeException(nameof(compressionFormat), compressionFormat, "Compression format must not be None.");
		}

		var maxLevelCount = TextureUtils.GetMipLevelCount(dimensions);
		if (levelCount < 1 || levelCount > maxLevelCount) {
			throw new ArgumentOutOfRangeException(
				nameof(levelCount),
				levelCount,
				$"Level count must be at least 1 and at most {maxLevelCount} for a {dimensions.X}x{dimensions.Y} texture."
			);
		}

		var expectedSizeBytes = TextureCompressor.GetCompressedSizeBytes(dimensions, compressionFormat, levelCount);
		if (blocks.Length < expectedSizeBytes) {
			throw new ArgumentException(
				$"Compressed block data for a {dimensions.X}x{dimensions.Y} {compressionFormat} texture requires {expectedSizeBytes} bytes, " +
				$"but only {blocks.Length} were supplied.",
				nameof(blocks)
			);
		}

		var buffer = _globals.CreateGpuHoldingBuffer(expectedSizeBytes);
		blocks[..expectedSizeBytes].CopyTo(buffer.AsSpan<byte>());
		return UploadCompressedBlocksAndStoreTextureData(buffer, dimensions, compressionFormat, levelCount, sourceTexelType, dataType, renderingConfig, name);
	}

	Texture UploadCompressedBlocksAndStoreTextureData(TemporaryLoadSpaceBuffer buffer, XYPair<int> dimensions, TextureCompressionFormat compressionFormat, int levelCount, TexelType sourceTexelType, TextureDataType dataType, TextureRenderingConfig renderingConfig, ReadOnlySpan<char> name) {
		var levelOffsets = stackalloc uint[levelCount];
		var levelSizes = stackalloc uint[levelCount];
		var runningOffset = 0;
		for (var level = 0; level < levelCount; ++level) {
			var levelSizeBytes = TextureCompressor.GetMipLevelSizeBytes(TextureUtils.GetMipLevelDimensions(dimensions, level), compressionFormat);
			levelOffsets[level] = (uint) runningOffset;
			levelSizes[level] = (uint) levelSizeBytes;
			runningOffset += levelSizeBytes;
		}

		var bakeryBufferCopy = GetBakeryTexelBufferCopyIfEnabled(buffer.AsSpan<byte>());

		LoadTextureCompressed(
			buffer.BufferIdentity,
			(void*) buffer.DataPtr,
			buffer.DataLengthBytes,
			(uint) dimensions.X,
			(uint) dimensions.Y,
			(int) compressionFormat,
			(uint) levelCount,
			levelOffsets,
			levelSizes,
			out var outHandle
		).ThrowIfFailure();

		var handle = (ResourceHandle<Texture>) outHandle;
		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, name, DefaultTextureName);
		_loadedTextures.Add(handle, new(dimensions, sourceTexelType, false, levelCount > 1, renderingConfig, compressionFormat));
		var result = HandleToInstance(handle);

		if (bakeryBufferCopy is { } bbc) {
			try {
				RegisterInBakery(result, dimensions, sourceTexelType == TexelType.Rgba32, levelCount > 1, false, dataType, renderingConfig, compressionFormat, levelCount, bbc.Span, name);
			}
			finally {
				bbc.Dispose();
			}
		}

		return result;
	}

	Texture CreateCompressedTextureAndDisposePreallocatedBuffer<TTexel>(ITextureBuilder.PreallocatedBuffer<TTexel> preallocatedBuffer, XYPair<int> dimensions, bool generateMipMaps, TextureRenderingConfig renderingConfig, TextureCompressionFormat compressionFormat, Quality compressionQuality, TextureDataType dataType, ReadOnlySpan<char> name) where TTexel : unmanaged, ITexel<TTexel> {
		var levelCount = generateMipMaps ? TextureUtils.GetMipLevelCount(dimensions) : 1;
		var totalSizeBytes = TextureCompressor.GetCompressedSizeBytes(dimensions, compressionFormat, generateMipMaps);
		var compressedBuffer = _globals.CreateGpuHoldingBuffer(totalSizeBytes);

		try {
			TextureCompressor.Compress(
				(ReadOnlySpan<TTexel>) preallocatedBuffer.Span,
				dimensions,
				compressionFormat,
				compressionQuality,
				dataType,
				generateMipMaps,
				compressedBuffer.AsSpan<byte>()
			);
		}
		finally {
			_globals.ReleaseGpuHoldingBufferWithoutGpuSubmission(preallocatedBuffer.BufferId);
		}

		return UploadCompressedBlocksAndStoreTextureData(compressedBuffer, dimensions, compressionFormat, levelCount, TTexel.BlitType, dataType, renderingConfig, name);
	}

	PooledHeapMemory<byte>? GetBakeryTexelBufferCopyIfEnabled(ReadOnlySpan<byte> data) {
		if (!_globals.BakeryIsEnabled) return null;
		var result = _globals.HeapPool.BorrowAndCopy(data);
		return result;
	}

	void RegisterInBakery(Texture resource, XYPair<int> dimensions, bool isRgba, bool mipMapsEnabled, bool allowsDynamicWrites, TextureDataType dataType, TextureRenderingConfig renderingConfig, TextureCompressionFormat compressionFormat, int compressedLevelCount, ReadOnlySpan<byte> texelData, ReadOnlySpan<char> name) {
		var bakery = _globals.Bakery;

		bakery.StartResourceBake(resource);
		bakery.AddResourceBakeValue(resource, LocalAssetBakery.ResourceNameSectionName, name);
		bakery.AddResourceBakeValue(resource, BakedResourceSchemata.TextureBakingSchema.DimensionsX, dimensions.X);
		bakery.AddResourceBakeValue(resource, BakedResourceSchemata.TextureBakingSchema.DimensionsY, dimensions.Y);
		bakery.AddResourceBakeValue(resource, BakedResourceSchemata.TextureBakingSchema.IsRgba, isRgba);
		bakery.AddResourceBakeValue(resource, BakedResourceSchemata.TextureBakingSchema.MipMapsEnabled, mipMapsEnabled);
		bakery.AddResourceBakeValue(resource, BakedResourceSchemata.TextureBakingSchema.AllowsDynamicWrites, allowsDynamicWrites);
		bakery.AddResourceBakeValue(resource, BakedResourceSchemata.TextureBakingSchema.DataType, dataType);
		bakery.AddResourceBakeValue(resource, BakedResourceSchemata.TextureBakingSchema.CompressionFormat, compressionFormat);
		bakery.AddResourceBakeValue(resource, BakedResourceSchemata.TextureBakingSchema.CompressedLevelCount, compressedLevelCount);
		bakery.AddResourceBakeValue(resource, BakedResourceSchemata.TextureBakingSchema.DisableTextureRepeat, renderingConfig.DisableTextureRepeat);
		bakery.AddResourceBakeValue(resource, BakedResourceSchemata.TextureBakingSchema.DisableTexelBlending, renderingConfig.DisableTexelBlending);
		bakery.AddResourceBakeValue(resource, BakedResourceSchemata.TextureBakingSchema.AnisotropicFilteringQuality, renderingConfig.AnisotropicFilteringQuality);
		bakery.AddResourceBakeValue(resource, BakedResourceSchemata.TextureBakingSchema.AnisotropyLevel, renderingConfig.AnisotropyLevel);
		bakery.AddResourceBakeValue(resource, BakedResourceSchemata.TextureBakingSchema.TexelData, texelData);
		bakery.CompleteResourceBake(resource);
	}

	ITextureBuilder.PreallocatedBuffer<TTexel> ITextureBuilder.PreallocateBuffer<TTexel>(int texelCount) => PreallocateBuffer<TTexel>(texelCount);
	ITextureBuilder.PreallocatedBuffer<TTexel> PreallocateBuffer<TTexel>(int texelCount) where TTexel : unmanaged, ITexel<TTexel> {
		var buffer = _globals.CreateGpuHoldingBuffer<TTexel>(texelCount);
		return new(buffer.BufferIdentity, buffer.AsSpan<TTexel>());
	}
	#endregion

	#region Texture Creation / Processing
	public Texture CreateTexture<TTexel>(ReadOnlySpan<TTexel> texels, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		ThrowIfThisIsDisposed();
		generationConfig.ThrowIfInvalid();
		config.ThrowIfInvalid();

		var width = generationConfig.Dimensions.X;
		var height = generationConfig.Dimensions.Y;

		var texelCount = width * height;
		if (texelCount > texels.Length) {
			throw new ArgumentException(
				$"Texture dimensions are {width}x{height}, requiring a texel span of length {texelCount} or greater, " +
				$"but actual span length was {texels.Length}.",
				nameof(texels)
			);
		}
		texels = texels[..texelCount];

		var buffer = PreallocateBuffer<TTexel>(texelCount);
		texels.CopyTo(buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, in generationConfig, in config);
	}

	public Texture CreateTextureWithAddedOpaqueAlpha(ReadOnlySpan<TexelRgb24> texels, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) {
		ThrowIfThisIsDisposed();
		generationConfig.ThrowIfInvalid();
		config.ThrowIfInvalid();

		var width = generationConfig.Dimensions.X;
		var height = generationConfig.Dimensions.Y;

		var texelCount = width * height;
		if (texelCount > texels.Length) {
			throw new ArgumentException(
				$"Texture dimensions are {width}x{height}, requiring a texel span of length {texelCount} or greater, " +
				$"but actual span length was {texels.Length}.",
				nameof(texels)
			);
		}

		var buffer = PreallocateBuffer<TexelRgba32>(texelCount);
		TextureUtils.Convert(texels, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, in generationConfig, in config);
	}
	
	public Texture CreateTextureWithoutProcessing<TTexel>(ReadOnlySpan<TTexel> texels, XYPair<int> dimensions, bool generateMipMaps, bool allowsDynamicWrites, TextureRenderingConfig renderingConfig, Quality? compressionQuality, TextureDataType dataTextureType, ReadOnlySpan<char> name) where TTexel : unmanaged, ITexel<TTexel> {
		ThrowIfThisIsDisposed();
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		if (dimensions.Area > texels.Length) {
			throw new ArgumentException(
				$"Texture dimensions are {dimensions.X}x{dimensions.Y}, requiring a texel span of length {dimensions.Area} or greater, " +
				$"but actual span length was {texels.Length}."
			);
		}

		var texelCount = dimensions.Area;
		var buffer = PreallocateBuffer<TTexel>(texelCount);
		texels[..texelCount].CopyTo(buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, dimensions, generateMipMaps, allowsDynamicWrites, renderingConfig, compressionQuality, dataTextureType, name);
	}
	#endregion

	#region Dynamic Overwrite
	public void OverwriteTexels<TTexel>(ResourceHandle<Texture> handle, ReadOnlySpan<TTexel> newTexels, XYPair<int> dimensions, XYPair<int> offset) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgb24>, IConversionSupplyingTexel<TTexel, TexelRgba32> {
		ThrowIfThisOrHandleIsDisposed(handle);
		var data = _loadedTextures[handle];

		if (!data.AllowsDynamicWrites) {
			throw new InvalidOperationException(
				$"Can not modify texels of {HandleToInstance(handle)} as it was not created with the " +
				$"'{nameof(TextureCreationConfig.AllowsDynamicWrites)}' flag set to true."
			);
		}
		
		if (offset.X < 0 || offset.Y < 0) {
			throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset X and Y can not be negative.");
		}
		if (dimensions.X < 1 || dimensions.Y < 1) {
			throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, "Dimensions X and Y must both be positive.");
		}
		var farEdge = offset + dimensions;
		if (farEdge.X > data.Dimensions.X || farEdge.Y > data.Dimensions.Y) {
			throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, $"Dimensions ({dimensions}) + offset ({offset}) exceeds texture dimensions (sum {farEdge} vs texture dimensions {data.Dimensions}).");
		}
		
		var writeArea = dimensions.Area;
		if (newTexels.Length < writeArea) {
			throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, $"Dimensions had area of {writeArea} texels but given texel buffer span had length {newTexels.Length}.");
		}
		
		var sourceBufferMatchesBlitType = TTexel.BlitType == data.TexelType;
		
		switch (data.TexelType) {
			case TexelType.Rgb24: {
				var holdingBuffer = _globals.CreateGpuHoldingBuffer<TexelRgb24>(writeArea);
				if (sourceBufferMatchesBlitType) newTexels[..writeArea].CopyTo(holdingBuffer.AsSpan<TTexel>());
				else TextureUtils.Convert(newTexels[..writeArea], holdingBuffer.AsSpan<TexelRgb24>());
				
				UpdateTextureRgb24(
					handle,
					holdingBuffer.BufferIdentity,
					(TexelRgb24*) holdingBuffer.DataPtr,
					holdingBuffer.DataLengthBytes,
					(uint) offset.X,
					(uint) offset.Y,
					(uint) dimensions.X,
					(uint) dimensions.Y
				).ThrowIfFailure();
				break;
			}
			case TexelType.Rgba32: {
				var holdingBuffer = _globals.CreateGpuHoldingBuffer<TexelRgba32>(writeArea);
				if (sourceBufferMatchesBlitType) newTexels[..writeArea].CopyTo(holdingBuffer.AsSpan<TTexel>());
				else TextureUtils.Convert(newTexels[..writeArea], holdingBuffer.AsSpan<TexelRgba32>());
				
				UpdateTextureRgba32(
					handle,
					holdingBuffer.BufferIdentity,
					(TexelRgba32*) holdingBuffer.DataPtr,
					holdingBuffer.DataLengthBytes,
					(uint) offset.X,
					(uint) offset.Y,
					(uint) dimensions.X,
					(uint) dimensions.Y
				).ThrowIfFailure();
				break;
			}
			default:
				throw new InvalidOperationException($"Unexpected texture texel type ({data.TexelType}).");
		}
	}
	#endregion

	#region Texture Properties
	public XYPair<int> GetDimensions(ResourceHandle<Texture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedTextures[handle].Dimensions;
	}
	public TexelType GetTexelType(ResourceHandle<Texture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedTextures[handle].TexelType;
	}
	public bool GetAllowsDynamicWrites(ResourceHandle<Texture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedTextures[handle].AllowsDynamicWrites;
	}
	public bool GetContainsMipMaps(ResourceHandle<Texture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedTextures[handle].ContainsMipMaps;
	}
	public TextureRenderingConfig GetRenderingConfig(ResourceHandle<Texture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedTextures[handle].RenderingConfig;
	}

	public string GetNameAsNewStringObject(ResourceHandle<Texture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultTextureName));
	}
	public int GetNameLength(ResourceHandle<Texture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultTextureName).Length;
	}
	public void CopyName(ResourceHandle<Texture> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultTextureName, destinationBuffer);
	}
	#endregion

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "load_texture_rgb_24")]
	static extern InteropResult LoadTextureRgb24(
		nuint bufferId,
		TexelRgb24* bufferPtr,
		int bufferLength,
		uint width,
		uint height,
		InteropBool generateMipmaps,
		InteropBool isLinearColorspace,
		out UIntPtr outTextureHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "load_texture_rgba_32")]
	static extern InteropResult LoadTextureRgba32(
		nuint bufferId,
		TexelRgba32* bufferPtr,
		int bufferLength,
		uint width,
		uint height,
		InteropBool generateMipmaps,
		InteropBool isLinearColorspace,
		out UIntPtr outTextureHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "update_texture_rgb_24")]
	static extern InteropResult UpdateTextureRgb24(
		UIntPtr textureHandle,
		nuint bufferId,
		TexelRgb24* bufferPtr,
		int bufferLength,
		uint xOffset,
		uint yOffset,
		uint width,
		uint height
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "update_texture_rgba_32")]
	static extern InteropResult UpdateTextureRgba32(
		UIntPtr textureHandle,
		nuint bufferId,
		TexelRgba32* bufferPtr,
		int bufferLength,
		uint xOffset,
		uint yOffset,
		uint width,
		uint height
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "load_texture_compressed")]
	static extern InteropResult LoadTextureCompressed(nuint bufferId, void* bufferPtr, int bufferLength, uint width, uint height, int formatId, uint levelCount, uint* levelOffsets, uint* levelSizes, out UIntPtr outTextureHandle);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_texture")]
	static extern InteropResult DisposeTexture(
		UIntPtr textureHandle
	);
	#endregion

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Texture HandleToInstance(ResourceHandle<Texture> h) => new(h, this);

	#region Resource Directory
	public IndirectEnumerable<object, Texture> AllActiveInstances {
		get {
			static LocalTextureBuilder CastSelf(object self) => self as LocalTextureBuilder ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetCount(object self) => CastSelf(self)._loadedTextures.Count;
			static int GetVersion(object self) => CastSelf(self)._loadedTextures.Version;
			static Texture GetItem(object self, int index) => CastSelf(self).HandleToInstance(CastSelf(self)._loadedTextures.GetPairAtIndex(index).Key);

			ThrowIfThisIsDisposed();
			return new(
				this,
				GetVersion(this),
				&GetCount,
				&GetVersion,
				&GetItem
			);
		}
	}
	public bool ResourceNameMatchIsMatching(Texture resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var handle = resource.GetHandleWithoutDisposeCheck();
		ThrowIfThisOrHandleIsDisposed(handle);
		return allowPartialMatch
			? _globals.GetResourceName(handle.Ident, DefaultTextureName).Contains(name, comparisonType)
			: _globals.GetResourceName(handle.Ident, DefaultTextureName).Equals(name, comparisonType);
	}
	#endregion

	public override string ToString() => _isDisposed ? "TinyFFR Local Material Builder [Disposed]" : "TinyFFR Local Material Builder";

	#region Disposal
	public bool IsDisposed(ResourceHandle<Texture> handle) => _isDisposed || !_loadedTextures.ContainsKey(handle);

	public void Dispose(ResourceHandle<Texture> handle) => Dispose(handle, removeFromCollection: true);
	void Dispose(ResourceHandle<Texture> handle, bool removeFromCollection) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		LocalFrameSynchronizationManager.QueueResourceDisposal(handle, &DisposeTexture);
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromCollection) _loadedTextures.Remove(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var tex in _loadedTextures.Keys) Dispose(tex, removeFromCollection: false);

			_loadedTextures.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Texture> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Texture));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}