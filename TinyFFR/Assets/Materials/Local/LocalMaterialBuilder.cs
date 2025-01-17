// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Globalization;
using System.Reflection;
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
	readonly record struct TextureData(XYPair<uint> Dimensions);
	const string DefaultMaterialName = "Unnamed Material";
	const string DefaultTextureName = "Unnamed Texture";
	const string DefaultColorMapName = "Default Color Map";
	const string DefaultNormalMapName = "Default Normal Map";
	const string DefaultOrmMapName = "Default Orm Map";
	readonly TextureImplProvider _textureImplProvider;
	readonly ArrayPoolBackedMap<TextureHandle, TextureData> _loadedTextures = new();
	readonly ArrayPoolBackedMap<string, UIntPtr> _loadedShaderPackages = new();
	readonly ArrayPoolBackedVector<MaterialHandle> _activeMaterials = new();
	readonly FixedByteBufferPool _shaderResourceBufferPool;
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly Lazy<Texture> _defaultColorMap;
	readonly Lazy<Texture> _defaultNormalMap;
	readonly Lazy<Texture> _defaultOrmMap;
	bool _isDisposed = false;

	// This is a private embedded 'delegating' object to help provide distinction between some default interface methods
	// on both IMaterialImplProvider and ITextureImplProvider. 
	sealed class TextureImplProvider : ITextureImplProvider {
		readonly LocalMaterialBuilder _owner;

		public TextureImplProvider(LocalMaterialBuilder owner) => _owner = owner;

		public XYPair<uint> GetDimensions(TextureHandle handle) => _owner.GetDimensions(handle);
		public ReadOnlySpan<char> GetName(TextureHandle handle) => _owner.GetName(handle);
		public bool IsDisposed(TextureHandle handle) => _owner.IsDisposed(handle);
		public void Dispose(TextureHandle handle) => _owner.Dispose(handle);
		public override string ToString() => _owner.ToString();
	}

	public Texture DefaultColorMap => _defaultColorMap.Value;
	public Texture DefaultNormalMap => _defaultNormalMap.Value;
	public Texture DefaultOrmMap => _defaultOrmMap.Value;

	public LocalMaterialBuilder(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
		_shaderResourceBufferPool = new FixedByteBufferPool(config.MaxShaderBufferSizeBytes);
		_textureImplProvider = new(this);

		_defaultColorMap = new(() => (this as IMaterialBuilder).CreateColorMap(
			TexturePattern.PlainFill(new ColorVect(StandardColor.White)), name: DefaultColorMapName
		));
		_defaultNormalMap = new(() => (this as IMaterialBuilder).CreateNormalMap(
			TexturePattern.PlainFill(Direction.Up), name: DefaultNormalMapName
		));
		_defaultOrmMap = new(() => (this as IMaterialBuilder).CreateOrmMap(
			TexturePattern.PlainFill(1f), 
			TexturePattern.PlainFill(0.4f), 
			TexturePattern.PlainFill(0f), 
			name: DefaultOrmMapName
		));
	}

	Texture IMaterialBuilder.CreateTextureUsingPreallocatedBuffer<TTexel>(IMaterialBuilder.PreallocatedBuffer<TTexel> preallocatedBuffer, in TextureCreationConfig config) => CreateTextureUsingPreallocatedBuffer(preallocatedBuffer, in config);
	Texture CreateTextureUsingPreallocatedBuffer<TTexel>(IMaterialBuilder.PreallocatedBuffer<TTexel> preallocatedBuffer, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		config.ThrowIfInvalid();
		if (preallocatedBuffer.Buffer == default) throw InvalidObjectException.InvalidDefault(typeof(IMaterialBuilder.PreallocatedBuffer<TTexel>));

		var dataPointer = Unsafe.AsPointer(ref MemoryMarshal.GetReference(preallocatedBuffer.Buffer));
		var dataLength = preallocatedBuffer.Buffer.Length * sizeof(TTexel);

		UIntPtr outHandle;
		switch (TTexel.Type) {
			case TexelType.Rgb24:
				LoadTextureRgb24(
					preallocatedBuffer.BufferId,
					(TexelRgb24*) dataPointer,
					dataLength,
					(uint) config.Width,
					(uint) config.Height,
					config.GenerateMipMaps,
					out outHandle
				).ThrowIfFailure();
				break;
			case TexelType.Rgba32:
				LoadTextureRgba32(
					preallocatedBuffer.BufferId,
					(TexelRgba32*) dataPointer,
					dataLength,
					(uint) config.Width,
					(uint) config.Height,
					config.GenerateMipMaps,
					out outHandle
				).ThrowIfFailure();
				break;
			default:
				throw new InvalidOperationException($"Unknown or unsupported texel type '{typeof(TTexel)}' (Type property '{TTexel.Type}').");
		}

		var handle = (TextureHandle) outHandle;
		_globals.StoreResourceNameIfNotDefault(handle.Ident, config.Name);
		_loadedTextures.Add(handle, new(((uint) config.Width, (uint) config.Height)));
		return HandleToInstance(handle);
	}

	IMaterialBuilder.PreallocatedBuffer<TTexel> IMaterialBuilder.PreallocateBuffer<TTexel>(int texelCount) => PreallocateBuffer<TTexel>(texelCount);
	IMaterialBuilder.PreallocatedBuffer<TTexel> PreallocateBuffer<TTexel>(int texelCount) where TTexel : unmanaged, ITexel<TTexel> {
		var buffer = _globals.CreateGpuHoldingBuffer<TTexel>(texelCount);
		return new(buffer.BufferIdentity, buffer.AsSpan<TTexel>());
	}

	public Texture CreateTexture<TTexel>(ReadOnlySpan<TTexel> texels, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var width = config.Width;
		var height = config.Height;

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
		texels.CopyTo(buffer.Buffer);
		return CreateTextureUsingPreallocatedBuffer(buffer, in config);
	}

	public Material CreateOpaqueMaterial(in OpaqueMaterialCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var shaderConstants = OpaqueMaterialShader;
		var shaderPackageHandle = GetOrLoadShaderPackageHandle(shaderConstants.ResourceName);
		CreateMaterial(
			shaderPackageHandle,
			out var outHandle
		).ThrowIfFailure();
		var handle = (MaterialHandle) outHandle;

		_globals.StoreResourceNameIfNotDefault(handle.Ident, config.Name);
		_activeMaterials.Add(handle);
		var result = HandleToInstance(handle);

		SetMaterialParameterTexture(
			handle,
			in ParamRef(shaderConstants.ParamColorMap),
			ParamLen(shaderConstants.ParamColorMap),
			config.ColorMap.Handle
		).ThrowIfFailure();
		_globals.DependencyTracker.RegisterDependency(result, config.ColorMap);

		SetMaterialParameterTexture(
			handle,
			in ParamRef(shaderConstants.ParamNormalMap),
			ParamLen(shaderConstants.ParamNormalMap),
			config.NormalMap.Handle
		).ThrowIfFailure();
		_globals.DependencyTracker.RegisterDependency(result, config.NormalMap);

		SetMaterialParameterTexture(
			handle,
			in ParamRef(shaderConstants.ParamOrmMap),
			ParamLen(shaderConstants.ParamOrmMap),
			config.OrmMap.Handle
		).ThrowIfFailure();
		_globals.DependencyTracker.RegisterDependency(result, config.OrmMap);

		return result;
	}

	public XYPair<uint> GetDimensions(TextureHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedTextures[handle].Dimensions;
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

		var (buffer, sizeBytes) = OpenResource(_shaderResourceBufferPool, resourceName);
		try {
			LoadShaderPackage(
				(byte*) buffer.StartPtr,
				sizeBytes,
				out var newHandle
			).ThrowIfFailure();
			_loadedShaderPackages.Add(resourceName, newHandle);
			return newHandle;
		}
		finally {
			_shaderResourceBufferPool.Return(buffer);
		}
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
	public bool IsDisposed(TextureHandle handle) => _isDisposed || !_loadedTextures.ContainsKey(handle);
	public bool IsDisposed(MaterialHandle handle) => _isDisposed || !_activeMaterials.Contains(handle);

	public void Dispose(TextureHandle handle) => Dispose(handle, removeFromCollection: true);
	void Dispose(TextureHandle handle, bool removeFromCollection) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		DisposeTexture(handle).ThrowIfFailure();
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromCollection) _loadedTextures.Remove(handle);
	}

	public void Dispose(MaterialHandle handle) => Dispose(handle, removeFromCollection: true);
	void Dispose(MaterialHandle handle, bool removeFromCollection) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		_globals.DependencyTracker.DeregisterAllDependencies(HandleToInstance(handle));
		DisposeMaterial(handle).ThrowIfFailure();
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromCollection) _activeMaterials.Remove(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var mat in _activeMaterials) Dispose(mat, removeFromCollection: false);
			foreach (var tex in _loadedTextures.Keys) Dispose(tex, removeFromCollection: false);
			foreach (var packageHandle in _loadedShaderPackages.Values) DisposeShaderPackage(packageHandle).ThrowIfFailure();

			_activeMaterials.Dispose();
			_loadedTextures.Dispose();
			_loadedShaderPackages.Dispose();
			_shaderResourceBufferPool.Dispose();
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