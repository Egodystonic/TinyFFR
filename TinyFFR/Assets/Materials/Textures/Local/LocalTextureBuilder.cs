// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering.Local.Sync;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Resources;
using System.Security;
using static Egodystonic.TinyFFR.Assets.Materials.Local.LocalShaderPackageConstants;

namespace Egodystonic.TinyFFR.Assets.Materials.Local;

[SuppressUnmanagedCodeSecurity]
sealed unsafe class LocalTextureBuilder : ITextureBuilder, ITextureImplProvider, IDisposable {
	readonly record struct TextureData(XYPair<int> Dimensions, TexelType TexelType);
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
		generationConfig.ThrowIfInvalid();
		config.ThrowIfInvalid();

		if (preallocatedBuffer.Span.IsEmpty) throw InvalidObjectException.InvalidDefault(typeof(ITextureBuilder.PreallocatedBuffer<TTexel>));
		if (generationConfig.Dimensions.Area > preallocatedBuffer.Span.Length) {
			throw new ArgumentException(
				$"Given config width/height require a buffer of {generationConfig.Dimensions.X}x{generationConfig.Dimensions.Y}={generationConfig.Dimensions.Area} texels, " +
				$"but supplied texel buffer only has {preallocatedBuffer.Span.Length} texels.",
				nameof(config)
			);
		}
		TextureUtils.ProcessTexture(preallocatedBuffer.Span, generationConfig.Dimensions, config.ProcessingToApply);

		var dataPointer = Unsafe.AsPointer(ref MemoryMarshal.GetReference(preallocatedBuffer.Span));
		var dataLength = preallocatedBuffer.Span.Length * sizeof(TTexel);

		UIntPtr outHandle;
		switch (TTexel.BlitType) {
			case TexelType.Rgb24:
				LoadTextureRgb24(
					preallocatedBuffer.BufferId,
					(TexelRgb24*) dataPointer,
					dataLength,
					(uint) generationConfig.Dimensions.X,
					(uint) generationConfig.Dimensions.Y,
					config.GenerateMipMaps,
					config.IsLinearColorspace,
					out outHandle
				).ThrowIfFailure();
				break;
			case TexelType.Rgba32:
				LoadTextureRgba32(
					preallocatedBuffer.BufferId,
					(TexelRgba32*) dataPointer,
					dataLength,
					(uint) generationConfig.Dimensions.X,
					(uint) generationConfig.Dimensions.Y,
					config.GenerateMipMaps,
					config.IsLinearColorspace,
					out outHandle
				).ThrowIfFailure();
				break;
			default:
				throw new InvalidOperationException($"Unknown or unsupported texel type '{typeof(TTexel)}' (BlitType property '{TTexel.BlitType}').");
		}

		var handle = (ResourceHandle<Texture>) outHandle;
		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, config.Name, DefaultTextureName);
		_loadedTextures.Add(handle, new(generationConfig.Dimensions, TTexel.BlitType));
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_texture")]
	static extern InteropResult DisposeTexture(
		UIntPtr textureHandle
	);
	#endregion

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Texture HandleToInstance(ResourceHandle<Texture> h) => new(h, this);

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