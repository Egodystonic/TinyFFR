// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Globalization;
using System.Resources;
using System.Security;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using static Egodystonic.TinyFFR.Assets.Materials.Local.LocalShaderPackageConstants;

namespace Egodystonic.TinyFFR.Assets.Materials.Local;

[SuppressUnmanagedCodeSecurity]
sealed unsafe class LocalMaterialBuilder : IMaterialBuilder, IMaterialImplProvider, IDisposable {
	const string DefaultMaterialName = "Unnamed Material";
	const string DefaultTextureName = "Unnamed Texture";
	readonly TextureImplProvider _textureImplProvider;
	readonly ArrayPoolBackedVector<TextureHandle> _loadedTextures = new();
	readonly ArrayPoolBackedMap<string, UIntPtr> _loadedShaderPackages = new();
	readonly ArrayPoolBackedVector<MaterialHandle> _activeMaterials = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ResourceManager _shaderResourceManager;
	bool _isDisposed = false;

	// This is a private embedded 'delegating' object to help provide distinction between some default interface methods
	// on both IMaterialImplProvider and ITextureImplProvider. 
	sealed class TextureImplProvider : ITextureImplProvider {
		readonly LocalMaterialBuilder _owner;

		public TextureImplProvider(LocalMaterialBuilder owner) => _owner = owner;

		public ReadOnlySpan<char> GetName(TextureHandle handle) => _owner.GetName(handle);
		public bool IsDisposed(TextureHandle handle) => _owner.IsDisposed(handle);
		public void Dispose(TextureHandle handle) => _owner.Dispose(handle);
		public override string ToString() => _owner.ToString();
	}

	public LocalMaterialBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
		_shaderResourceManager = new ResourceManager(typeof(LocalMaterialBuilder));
		_textureImplProvider = new(this);
	}

	public Texture CreateTexture<TTexel>(ReadOnlySpan<TTexel> texels, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		ThrowIfThisIsDisposed();

		config.ThrowIfInvalid();
		var width = config.Width;
		var height = config.Height;

		if (width < 1) width = 1;
		if (height < 1) height = 1;

		var texelCount = width * height;
		if (texelCount > texels.Length) {
			throw new ArgumentException(
				$"Texture dimensions are {width}x{height}, requiring a texel span of length {texelCount} or greater, " +
				$"but actual span length was {texels.Length}.",
				nameof(texels)
			);
		}
		texels = texels[..texelCount];

		var buffer = _globals.CopySpanToTemporaryCpuBuffer(texels);
		UIntPtr outHandle;
		switch (TTexel.Type) {
			case TexelType.Rgb24:
				LoadTextureRgb24(
					buffer.BufferIdentity,
					(TexelRgb24*) buffer.DataPtr,
					buffer.DataLengthBytes,
					(uint) width,
					(uint) height,
					config.GenerateMipMaps,
					out outHandle
				).ThrowIfFailure();
				break;
			case TexelType.Rgba32:
				LoadTextureRgba32(
					buffer.BufferIdentity,
					(TexelRgba32*) buffer.DataPtr,
					buffer.DataLengthBytes,
					(uint) width,
					(uint) height,
					config.GenerateMipMaps,
					out outHandle
				).ThrowIfFailure();
				break;
			default:
				throw new InvalidOperationException($"Unknown or unsupported texel type '{typeof(TTexel)}'.");
		}

		var handle = (TextureHandle) outHandle;
		_globals.StoreResourceNameIfNotDefault(handle.Ident, config.Name);
		_loadedTextures.Add(handle);
		return HandleToInstance(handle);
	}

	public Material CreateStandardMaterial(in StandardMaterialCreationConfig config) {
		ThrowIfThisIsDisposed();

		var shaderConstants = StandardPbrShader;

		var shaderPackageHandle = GetOrLoadShaderPackageHandle(shaderConstants.ResourceName);

		CreateMaterial(
			shaderPackageHandle,
			out var outHandle
		).ThrowIfFailure();

		var handle = (MaterialHandle) outHandle;
		_globals.StoreResourceNameIfNotDefault(handle.Ident, config.Name);
		_activeMaterials.Add(handle);
		var result = HandleToInstance(handle);

		if (config.Albedo != null) {
			SetMaterialParameterTexture(
				handle,
				in ParamRef(shaderConstants.ParamAlbedo),
				ParamLen(shaderConstants.ParamAlbedo),
				config.Albedo.Value.Handle
			).ThrowIfFailure();
			_globals.DependencyTracker.RegisterDependency(result, config.Albedo.Value);
		} 

		return result;
	}

	public ReadOnlySpan<char> GetName(TextureHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultTextureName);
	}
	public ReadOnlySpan<char> GetName(MaterialHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultMaterialName);
	}

	UIntPtr GetOrLoadShaderPackageHandle(string resourceName) {
		if (_loadedShaderPackages.TryGetValue(resourceName, out var result)) return result;

		using var resourceStream = _shaderResourceManager.GetStream(resourceName, CultureInfo.InvariantCulture);
		if (resourceStream == null) throw new InvalidOperationException($"Could not load shader resource '{resourceName}'.");
		LoadShaderPackage(
			resourceStream.PositionPointer,
			checked((int) resourceStream.Length),
			out var newHandle
		).ThrowIfFailure();
		_loadedShaderPackages.Add(resourceName, newHandle);
		return newHandle;
	}

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "load_texture_rgb_24")]
	static extern InteropResult LoadTextureRgb24(
		nuint bufferId,
		TexelRgb24* bufferPtr,
		int bufferLength,
		uint width,
		uint height,
		InteropBool generateMipmaps,
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
		out UIntPtr outTextureHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_texture")]
	static extern InteropResult DisposeTexture(
		UIntPtr textureHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "load_shader_package")]
	static extern InteropResult LoadShaderPackage(
		byte* packageDataPtr,
		int packageDataLength,
		out UIntPtr outPackageHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "create_material")]
	static extern InteropResult CreateMaterial(
		UIntPtr shaderPackageHandle,
		out UIntPtr outMaterialHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_material_parameter_texture")]
	static extern InteropResult SetMaterialParameterTexture(
		UIntPtr materialHandle,
		ref readonly byte utf8ParameterNameBuffer,
		int parameterNameBufferLength,
		UIntPtr textureHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_material")]
	static extern InteropResult DisposeMaterial(
		UIntPtr materialHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_shader_package")]
	static extern InteropResult DisposeShaderPackage(
		UIntPtr packageHandle
	);
	#endregion

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Texture HandleToInstance(TextureHandle h) => new(h, _textureImplProvider);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Material HandleToInstance(MaterialHandle h) => new(h, this);

	public override string ToString() => _isDisposed ? "TinyFFR Local Material Builder [Disposed]" : "TinyFFR Local Material Builder";

	#region Disposal
	public bool IsDisposed(TextureHandle handle) => _isDisposed || !_loadedTextures.Contains(handle);
	public bool IsDisposed(MaterialHandle handle) => _isDisposed || !_activeMaterials.Contains(handle);

	public void Dispose(TextureHandle handle) => Dispose(handle, removeFromVect: true);
	void Dispose(TextureHandle handle, bool removeFromVect) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		DisposeTexture(handle).ThrowIfFailure();
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromVect) _loadedTextures.Remove(handle);
	}

	public void Dispose(MaterialHandle handle) => Dispose(handle, removeFromVect: true);
	void Dispose(MaterialHandle handle, bool removeFromVect) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		_globals.DependencyTracker.DeregisterAllDependencies(HandleToInstance(handle));
		DisposeMaterial(handle).ThrowIfFailure();
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromVect) _activeMaterials.Remove(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var mat in _activeMaterials) Dispose(mat, removeFromVect: false);
			foreach (var tex in _loadedTextures) Dispose(tex, removeFromVect: false);
			foreach (var packageHandle in _loadedShaderPackages.Values) DisposeShaderPackage(packageHandle).ThrowIfFailure();
			
			_activeMaterials.Dispose();
			_loadedTextures.Dispose();
			_loadedShaderPackages.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(TextureHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Texture));
	void ThrowIfThisOrHandleIsDisposed(MaterialHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Material));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}