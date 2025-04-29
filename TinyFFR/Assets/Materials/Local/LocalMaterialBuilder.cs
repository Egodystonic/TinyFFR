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
using Egodystonic.TinyFFR.Rendering.Local.Sync;
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
	const string TestMaterialColorMapName = "Test Material Color Map";
	const string TestMaterialNormalMapName = "Test Material Normal Map";
	const string TestMaterialOrmMapName = "Test Material Orm Map";
	const string TestMaterialName = "Test Material";
	readonly TextureImplProvider _textureImplProvider;
	readonly ArrayPoolBackedMap<ResourceHandle<Texture>, TextureData> _loadedTextures = new();
	readonly ArrayPoolBackedMap<string, UIntPtr> _loadedShaderPackages = new();
	readonly ArrayPoolBackedVector<ResourceHandle<Material>> _activeMaterials = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly Lazy<Texture> _defaultColorMap;
	readonly Lazy<Texture> _defaultNormalMap;
	readonly Lazy<Texture> _defaultOrmMap;
	readonly Lazy<Material> _testMaterial;
	bool _isDisposed = false;

	// This is a private embedded 'delegating' object to help provide distinction between some default interface methods
	// on both IMaterialImplProvider and ITextureImplProvider. 
	sealed class TextureImplProvider : ITextureImplProvider {
		readonly LocalMaterialBuilder _owner;

		public TextureImplProvider(LocalMaterialBuilder owner) => _owner = owner;

		public XYPair<uint> GetDimensions(ResourceHandle<Texture> handle) => _owner.GetDimensions(handle);
		public ReadOnlySpan<char> GetName(ResourceHandle<Texture> handle) => _owner.GetName(handle);
		public bool IsDisposed(ResourceHandle<Texture> handle) => _owner.IsDisposed(handle);
		public void Dispose(ResourceHandle<Texture> handle) => _owner.Dispose(handle);
		public override string ToString() => _owner.ToString();
	}

	public Texture DefaultColorMap => _defaultColorMap.Value;
	public Texture DefaultNormalMap => _defaultNormalMap.Value;
	public Texture DefaultOrmMap => _defaultOrmMap.Value;
	public Material TestMaterial => _testMaterial.Value;

	public LocalMaterialBuilder(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
		_textureImplProvider = new(this);

		_defaultColorMap = new(() => (this as IMaterialBuilder).CreateColorMap(name: DefaultColorMapName));
		_defaultNormalMap = new(() => (this as IMaterialBuilder).CreateNormalMap(name: DefaultNormalMapName));
		_defaultOrmMap = new(() => (this as IMaterialBuilder).CreateOrmMap(name: DefaultOrmMapName));
		_testMaterial = new(CreateTestMaterial);
	}

	void ApplyConfig<TTexel>(Span<TTexel> buffer, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		const int MaxTextureWidthForStackRowSwap = 65_536;

		var texelCount = generationConfig.Width * generationConfig.Height;

		if (config.FlipX) {
			for (var y = 0; y < generationConfig.Height; ++y) {
				var row = buffer[(y * generationConfig.Width)..((y + 1) * generationConfig.Width)];
				for (var x = 0; x < generationConfig.Width / 2; ++x) {
					(row[x], row[^(x + 1)]) = (row[^(x + 1)], row[x]);
				}
			}
		}
		if (config.FlipY) {
			var rowSwapSpace = generationConfig.Width > MaxTextureWidthForStackRowSwap ? new TTexel[generationConfig.Width] : stackalloc TTexel[generationConfig.Width];
			for (var y = 0; y < generationConfig.Height / 2; ++y) {
				var lowerRow = buffer[(y * generationConfig.Width)..((y + 1) * generationConfig.Width)];
				var upperRow = buffer[((generationConfig.Height - (y + 1)) * generationConfig.Width)..((generationConfig.Height - y) * generationConfig.Width)];
				lowerRow.CopyTo(rowSwapSpace);
				upperRow.CopyTo(lowerRow);
				rowSwapSpace.CopyTo(upperRow);
			}
		}
		if (config.InvertXRedChannel || config.InvertYGreenChannel || config.InvertZBlueChannel || config.InvertWAlphaChannel) {
			for (var i = 0; i < texelCount; ++i) {
				if (config.InvertXRedChannel) buffer[i] = buffer[i].WithInvertedChannelIfPresent(0);
				if (config.InvertYGreenChannel) buffer[i] = buffer[i].WithInvertedChannelIfPresent(1);
				if (config.InvertZBlueChannel) buffer[i] = buffer[i].WithInvertedChannelIfPresent(2);
				if (config.InvertWAlphaChannel) buffer[i] = buffer[i].WithInvertedChannelIfPresent(3);
			}
		}
	}

	// Maintainer's note: The buffer is disposed on the native side when it's asynchronously loaded on to the GPU
	Texture IMaterialBuilder.CreateTextureAndDisposePreallocatedBuffer<TTexel>(IMaterialBuilder.PreallocatedBuffer<TTexel> preallocatedBuffer, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) => CreateTextureAndDisposePreallocatedBuffer(preallocatedBuffer, in generationConfig, in config);
	Texture CreateTextureAndDisposePreallocatedBuffer<TTexel>(IMaterialBuilder.PreallocatedBuffer<TTexel> preallocatedBuffer, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		config.ThrowIfInvalid();
		if (preallocatedBuffer.Buffer.IsEmpty) throw InvalidObjectException.InvalidDefault(typeof(IMaterialBuilder.PreallocatedBuffer<TTexel>));
		if (generationConfig.Width * generationConfig.Height > preallocatedBuffer.Buffer.Length) {
			throw new ArgumentException(
				$"Given config width/height require a buffer of {generationConfig.Width}x{generationConfig.Height}={(generationConfig.Width * generationConfig.Height)} texels, " +
				$"but supplied texel buffer only has {preallocatedBuffer.Buffer.Length} texels.", 
				nameof(config)
			);
		}
		ApplyConfig(preallocatedBuffer.Buffer, in generationConfig, in config);

		var dataPointer = Unsafe.AsPointer(ref MemoryMarshal.GetReference(preallocatedBuffer.Buffer));
		var dataLength = preallocatedBuffer.Buffer.Length * sizeof(TTexel);
		
		UIntPtr outHandle;
		switch (TTexel.BlitType) {
			case TexelType.Rgb24:
				LoadTextureRgb24(
					preallocatedBuffer.BufferId,
					(TexelRgb24*) dataPointer,
					dataLength,
					(uint) generationConfig.Width,
					(uint) generationConfig.Height,
					config.GenerateMipMaps,
					out outHandle
				).ThrowIfFailure();
				break;
			case TexelType.Rgba32:
				LoadTextureRgba32(
					preallocatedBuffer.BufferId,
					(TexelRgba32*) dataPointer,
					dataLength,
					(uint) generationConfig.Width,
					(uint) generationConfig.Height,
					config.GenerateMipMaps,
					out outHandle
				).ThrowIfFailure();
				break;
			default:
				throw new InvalidOperationException($"Unknown or unsupported texel type '{typeof(TTexel)}' (BlitType property '{TTexel.BlitType}').");
		}

		var handle = (ResourceHandle<Texture>) outHandle;
		_globals.StoreResourceNameIfNotEmpty(handle.Ident, config.Name);
		_loadedTextures.Add(handle, new(((uint) generationConfig.Width, (uint) generationConfig.Height)));
		return HandleToInstance(handle);
	}

	IMaterialBuilder.PreallocatedBuffer<TTexel> IMaterialBuilder.PreallocateBuffer<TTexel>(int texelCount) => PreallocateBuffer<TTexel>(texelCount);
	IMaterialBuilder.PreallocatedBuffer<TTexel> PreallocateBuffer<TTexel>(int texelCount) where TTexel : unmanaged, ITexel<TTexel> {
		var buffer = _globals.CreateGpuHoldingBuffer<TTexel>(texelCount);
		return new(buffer.BufferIdentity, buffer.AsSpan<TTexel>());
	}

	public Texture CreateTexture<TTexel>(ReadOnlySpan<TTexel> texels, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var width = generationConfig.Width;
		var height = generationConfig.Height;

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
		return CreateTextureAndDisposePreallocatedBuffer(buffer, in generationConfig, in config);
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
		var handle = (ResourceHandle<Material>) outHandle;

		_globals.StoreResourceNameIfNotEmpty(handle.Ident, config.Name);
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

	public XYPair<uint> GetDimensions(ResourceHandle<Texture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedTextures[handle].Dimensions;
	}

	public ReadOnlySpan<char> GetName(ResourceHandle<Texture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultTextureName);
	}
	public ReadOnlySpan<char> GetName(ResourceHandle<Material> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultMaterialName);
	}

	UIntPtr GetOrLoadShaderPackageHandle(string resourceName) {
		if (_loadedShaderPackages.TryGetValue(resourceName, out var result)) return result;

		var (bufferPtr, sizeBytes) = EmbeddedResourceResolver.GetResource(resourceName);
		LoadShaderPackage(
				(byte*) bufferPtr,
				sizeBytes,
				out var newHandle
			).ThrowIfFailure();
		_loadedShaderPackages.Add(resourceName, newHandle);
		return newHandle;
	}

	Material CreateTestMaterial() {
		var colorMap = (this as IMaterialBuilder).CreateColorMap(
			TexturePattern.ChequerboardBordered(
				new ColorVect(0.5f, 0.5f, 0.5f),
				8,
				new ColorVect(1f, 0f, 0f),
				new ColorVect(0f, 1f, 0f),
				new ColorVect(0f, 0f, 1f),
				new ColorVect(1f, 1f, 1f),
				repetitionCount: (8, 8),
				cellResolution: 128
			),
			name: TestMaterialColorMapName
		);
		var normalMap = (this as IMaterialBuilder).CreateNormalMap(
			TexturePattern.Rectangles(
				interiorSize: (128, 128),
				borderSize: (8, 8),
				paddingSize: (0, 0),
				interiorValue: Direction.Forward,
				borderRightValue: (-1f, 0f, 1f),
				borderTopValue: (0f, 1f, 1f),
				borderLeftValue: (1f, 0f, 1f),
				borderBottomValue: (0f, -1f, 1f),
				paddingValue: Direction.Forward,
				repetitions: (8, 8)
			),
			name: TestMaterialNormalMapName
		);
		var ormMap = (this as IMaterialBuilder).CreateOrmMap(
			TexturePattern.ChequerboardBordered<Real>(
				1f,
				4,
				1f,
				1f,
				1f,
				1f,
				repetitionCount: (8, 8),
				cellResolution: 64
			),
			TexturePattern.ChequerboardBordered<Real>(
				0.4f,
				4,
				0.7f,
				0.3f,
				0.5f,
				1f,
				repetitionCount: (32, 32),
				cellResolution: 64
			),
			TexturePattern.ChequerboardBordered<Real>(
				0f,
				4,
				0.7f,
				0.3f,
				1f,
				0f,
				repetitionCount: (24, 24),
				cellResolution: 64
			),
			name: TestMaterialOrmMapName
		);
		return (this as IMaterialBuilder).CreateOpaqueMaterial(colorMap, normalMap, ormMap, TestMaterialName);
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
	Texture HandleToInstance(ResourceHandle<Texture> h) => new(h, _textureImplProvider);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Material HandleToInstance(ResourceHandle<Material> h) => new(h, this);

	public override string ToString() => _isDisposed ? "TinyFFR Local Material Builder [Disposed]" : "TinyFFR Local Material Builder";

	#region Disposal
	public bool IsDisposed(ResourceHandle<Texture> handle) => _isDisposed || !_loadedTextures.ContainsKey(handle);
	public bool IsDisposed(ResourceHandle<Material> handle) => _isDisposed || !_activeMaterials.Contains(handle);

	public void Dispose(ResourceHandle<Texture> handle) => Dispose(handle, removeFromCollection: true);
	void Dispose(ResourceHandle<Texture> handle, bool removeFromCollection) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		LocalFrameSynchronizationManager.QueueResourceDisposal(handle, &DisposeTexture);
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromCollection) _loadedTextures.Remove(handle);
	}

	public void Dispose(ResourceHandle<Material> handle) => Dispose(handle, removeFromCollection: true);
	void Dispose(ResourceHandle<Material> handle, bool removeFromCollection) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		_globals.DependencyTracker.DeregisterAllDependencies(HandleToInstance(handle));
		LocalFrameSynchronizationManager.QueueResourceDisposal(handle, &DisposeMaterial);
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
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Texture> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Texture));
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Material> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Material));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}