// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

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
	readonly record struct TextureData(XYPair<int> Dimensions, TexelType TexelType, bool AllowsDynamicWrites, bool ContainsMipMaps, TextureRenderingConfig RenderingConfig);
	const string DefaultTextureName = "Unnamed Texture";
	readonly ArrayPoolBackedMap<ResourceHandle<Texture>, TextureData> _loadedTextures = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	bool _isDisposed = false;

	public LocalTextureBuilder(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
	}

	#region Buffer Allocation
	// Maintainer's note: The buffer is disposed on the native side when it's asynchronously loaded on to the GPU
	Texture ITextureBuilder.CreateTextureAndDisposePreallocatedBuffer<TTexel>(ITextureBuilder.PreallocatedBuffer<TTexel> preallocatedBuffer, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) => CreateTextureAndDisposePreallocatedBuffer(preallocatedBuffer, in generationConfig, in config);
	Texture CreateTextureAndDisposePreallocatedBuffer<TTexel>(ITextureBuilder.PreallocatedBuffer<TTexel> preallocatedBuffer, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();

		if (preallocatedBuffer.Span.IsEmpty) throw InvalidObjectException.InvalidDefault(typeof(ITextureBuilder.PreallocatedBuffer<TTexel>));
		ITextureBuilder.PrepareTexelsForCreation(preallocatedBuffer.Span, in generationConfig, in config);

		return CreateTextureAndDisposePreallocatedBuffer(
			preallocatedBuffer,
			generationConfig.Dimensions,
			config.GenerateMipMaps,
			config.IsLinearColorspace,
			config.AllowsDynamicWrites,
			config.RenderingConfig,
			config.Name
		);
	}

	Texture CreateTextureAndDisposePreallocatedBuffer<TTexel>(ITextureBuilder.PreallocatedBuffer<TTexel> preallocatedBuffer, XYPair<int> dimensions, bool generateMipMaps, bool isLinearColorspace, bool allowsDynamicWrites, TextureRenderingConfig renderingConfig, ReadOnlySpan<char> name) where TTexel : unmanaged, ITexel<TTexel> {
		var dataPointer = Unsafe.AsPointer(ref MemoryMarshal.GetReference(preallocatedBuffer.Span));
		var dataLength = preallocatedBuffer.Span.Length * sizeof(TTexel);

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
					isLinearColorspace,
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
					isLinearColorspace,
					out outHandle
				).ThrowIfFailure();
				break;
			default:
				throw new InvalidOperationException($"Unknown or unsupported texel type '{typeof(TTexel)}' (BlitType property '{TTexel.BlitType}').");
		}

		var handle = (ResourceHandle<Texture>) outHandle;
		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, name, DefaultTextureName);
		_loadedTextures.Add(handle, new(dimensions, TTexel.BlitType, allowsDynamicWrites, generateMipMaps, renderingConfig));
		return HandleToInstance(handle);
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
	
	public Texture CreateTextureWithoutProcessing<TTexel>(ReadOnlySpan<TTexel> texels, XYPair<int> dimensions, bool generateMipMaps, bool isLinearColorspace, bool allowsDynamicWrites, TextureRenderingConfig renderingConfig, ReadOnlySpan<char> name) where TTexel : unmanaged, ITexel<TTexel> {
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
		return CreateTextureAndDisposePreallocatedBuffer(buffer, dimensions, generateMipMaps, isLinearColorspace, allowsDynamicWrites, renderingConfig, name);
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