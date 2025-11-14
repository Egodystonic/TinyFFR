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
sealed unsafe class LocalMaterialBuilder : IMaterialBuilder, IMaterialImplProvider, IDisposable {
	readonly record struct TextureData(XYPair<int> Dimensions, TexelType TexelType);
	const string DefaultMaterialName = "Unnamed Material";
	const string DefaultTextureName = "Unnamed Texture";
	const string TestMaterialName = "Test Material";
	readonly TextureImplProvider _textureImplProvider;
	readonly ArrayPoolBackedMap<ResourceHandle<Texture>, TextureData> _loadedTextures = new();
	readonly ArrayPoolBackedMap<string, UIntPtr> _loadedShaderPackages = new();
	readonly ArrayPoolBackedVector<ResourceHandle<Material>> _activeMaterials = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly Lazy<ResourceGroup> _testMaterialTextures;
	bool _isDisposed = false;

	// This is a private embedded 'delegating' object to help provide distinction between some default interface methods
	// on both IMaterialImplProvider and ITextureImplProvider. 
	sealed class TextureImplProvider : ITextureImplProvider {
		readonly LocalMaterialBuilder _owner;

		public TextureImplProvider(LocalMaterialBuilder owner) => _owner = owner;

		public XYPair<int> GetDimensions(ResourceHandle<Texture> handle) => _owner.GetDimensions(handle);
		public TexelType GetTexelType(ResourceHandle<Texture> handle) => _owner.GetTexelType(handle);
		public string GetNameAsNewStringObject(ResourceHandle<Texture> handle) => _owner.GetNameAsNewStringObject(handle);
		public int GetNameLength(ResourceHandle<Texture> handle) => _owner.GetNameLength(handle);
		public void CopyName(ResourceHandle<Texture> handle, Span<char> destinationBuffer) => _owner.CopyName(handle, destinationBuffer);
		public bool IsDisposed(ResourceHandle<Texture> handle) => _owner.IsDisposed(handle);
		public void Dispose(ResourceHandle<Texture> handle) => _owner.Dispose(handle);
		public override string ToString() => _owner.ToString();
	}

	public LocalMaterialBuilder(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
		_textureImplProvider = new(this);
		_testMaterialTextures = new(CreateTestMaterialTextures);
	}

	#region Textures
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

		var shouldSwizzle = config.XRedFinalOutputSource != ColorChannel.R
						|| config.YGreenFinalOutputSource != ColorChannel.G
						|| config.ZBlueFinalOutputSource != ColorChannel.B
						|| config.WAlphaFinalOutputSource != ColorChannel.A;
		var shouldPreprocess = config.InvertXRedChannel || config.InvertYGreenChannel || config.InvertZBlueChannel || config.InvertWAlphaChannel || shouldSwizzle;
		if (!shouldPreprocess) return;

		for (var i = 0; i < texelCount; ++i) {
			if (config.InvertXRedChannel) buffer[i] = buffer[i].WithInvertedChannelIfPresent(0);
			if (config.InvertYGreenChannel) buffer[i] = buffer[i].WithInvertedChannelIfPresent(1);
			if (config.InvertZBlueChannel) buffer[i] = buffer[i].WithInvertedChannelIfPresent(2);
			if (config.InvertWAlphaChannel) buffer[i] = buffer[i].WithInvertedChannelIfPresent(3);
			if (shouldSwizzle) {
				buffer[i] = buffer[i].SwizzlePresentChannels(
					config.XRedFinalOutputSource,
					config.YGreenFinalOutputSource,
					config.ZBlueFinalOutputSource,
					config.WAlphaFinalOutputSource
				);
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
					config.IsLinearColorspace,
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
					config.IsLinearColorspace,
					out outHandle
				).ThrowIfFailure();
				break;
			default:
				throw new InvalidOperationException($"Unknown or unsupported texel type '{typeof(TTexel)}' (BlitType property '{TTexel.BlitType}').");
		}

		var handle = (ResourceHandle<Texture>) outHandle;
		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, config.Name, DefaultTextureName);
		_loadedTextures.Add(handle, new((generationConfig.Width, generationConfig.Height), TTexel.BlitType));
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
	public void ProcessTexture<TTexel>(Span<TTexel> texels, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
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

		ApplyConfig(texels, in generationConfig, in config);
	}

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

	#region Materials
	ResourceGroup CreateTestMaterialTextures() {
		var result = _globals.ResourceGroupProvider.CreateGroup(
			disposeContainedResourcesWhenDisposed: true,
			name: TestMaterialName + " Texture Group"
		);

		result.Add((this as IMaterialBuilder).CreateTexture(
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
			includeAlpha: false,
			name: TestMaterialName + " Color Map"
		));

		result.Add((this as IMaterialBuilder).CreateTexture(
			TexturePattern.Rectangles(
				interiorSize: (128, 128),
				borderSize: (8, 8),
				paddingSize: (0, 0),
				interiorValue: UnitSphericalCoordinate.ZeroZero,
				borderRightValue: new UnitSphericalCoordinate(Orientation2D.Right.ToPolarAngle()!.Value, 45f),
				borderTopValue: new UnitSphericalCoordinate(Orientation2D.Up.ToPolarAngle()!.Value, 45f),
				borderLeftValue: new UnitSphericalCoordinate(Orientation2D.Left.ToPolarAngle()!.Value, 45f),
				borderBottomValue: new UnitSphericalCoordinate(Orientation2D.Down.ToPolarAngle()!.Value, 45f),
				paddingValue: UnitSphericalCoordinate.ZeroZero,
				repetitions: (8, 8)
			),
			name: TestMaterialName + " Normal Map"
		));

		return result;
	}

	public Material CreateTestMaterial() {
		ThrowIfThisIsDisposed();
		
		var textureGroup = _testMaterialTextures.Value;
		
		return CreateStandardMaterial(new StandardMaterialCreationConfig {
			ColorMap = textureGroup.Textures[0],
			NormalMap = textureGroup.Textures[1],
			Name = TestMaterialName
		});
	}

	public Material CreateSimpleMaterial(in SimpleMaterialCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var shaderConstants = SimpleMaterialShader;

		var flags = (SimpleMaterialShaderConstants.Flags) 0;
		var alphaModeVariant = SimpleMaterialShaderConstants.AlphaModeVariant.AlphaOff;

		if (config.EmissiveMap.HasValue) flags |= SimpleMaterialShaderConstants.Flags.Emissive;
		if (config.ColorMap.TexelType == TexelType.Rgba32) {
			alphaModeVariant = SimpleMaterialShaderConstants.AlphaModeVariant.AlphaOn;
		}

		var shaderResourceName = shaderConstants.GetShaderResourceName(flags, alphaModeVariant);
		var result = InstantiateMaterial(shaderResourceName, config.Name);

		ApplyMaterialParam(result, config.ColorMap, shaderConstants.ParamColorMap);
		ApplyMaterialParam(result, config.EmissiveMap, shaderConstants.ParamEmissiveMap);

		return result;
	}

	public Material CreateStandardMaterial(in StandardMaterialCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var shaderConstants = StandardMaterialShader;
		
		var flags = (StandardMaterialShaderConstants.Flags) 0;
		var alphaModeVariant = StandardMaterialShaderConstants.AlphaModeVariant.AlphaOff;
		var ormReflectanceVariant = StandardMaterialShaderConstants.OrmReflectanceVariant.Off;

		if (config.AnisotropyMap.HasValue) flags |= StandardMaterialShaderConstants.Flags.Anisotropy;
		if (config.ClearCoatMap.HasValue) flags |= StandardMaterialShaderConstants.Flags.ClearCoat;
		if (config.EmissiveMap.HasValue) flags |= StandardMaterialShaderConstants.Flags.Emissive;
		if (config.NormalMap.HasValue) flags |= StandardMaterialShaderConstants.Flags.Normals;
		if (config.OcclusionRoughnessMetallicReflectanceMap.HasValue) {
			flags |= StandardMaterialShaderConstants.Flags.Orm;
			if (config.OcclusionRoughnessMetallicReflectanceMap.Value.TexelType == TexelType.Rgba32) {
				ormReflectanceVariant = StandardMaterialShaderConstants.OrmReflectanceVariant.On;
			}
		}
		if (config.ColorMap.TexelType == TexelType.Rgba32) {
			alphaModeVariant = config.AlphaMode switch {
				StandardMaterialAlphaMode.FullBlending => StandardMaterialShaderConstants.AlphaModeVariant.AlphaOnBlended,
				_ => StandardMaterialShaderConstants.AlphaModeVariant.AlphaOn
			};
		}

		var shaderResourceName = shaderConstants.GetShaderResourceName(flags, alphaModeVariant, ormReflectanceVariant);
		var result = InstantiateMaterial(shaderResourceName, config.Name);

		ApplyMaterialParam(result, config.ColorMap, shaderConstants.ParamColorMap);
		ApplyMaterialParam(result, config.AnisotropyMap, shaderConstants.ParamAnisotropyMap);
		ApplyMaterialParam(result, config.ClearCoatMap, shaderConstants.ParamClearCoatMap);
		ApplyMaterialParam(result, config.EmissiveMap, shaderConstants.ParamEmissiveMap);
		ApplyMaterialParam(result, config.NormalMap, shaderConstants.ParamNormalMap);
		ApplyMaterialParam(result, config.OcclusionRoughnessMetallicReflectanceMap, shaderConstants.ParamOrmMap);

		return result;
	}

	public Material CreateTransmissiveMaterial(in TransmissiveMaterialCreationConfig config) {
		const float ThinThickRefractionModelCrossoverThickness = 0.2f;
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var shaderConstants = TransmissiveMaterialShader;

		var flags = (TransmissiveMaterialShaderConstants.Flags) 0;
		var alphaModeVariant = TransmissiveMaterialShaderConstants.AlphaModeVariant.AlphaOff;
		var refractionQualityVariant = config.Quality switch {
			TransmissiveMaterialQuality.SkyboxReflectionsAndRefraction => TransmissiveMaterialShaderConstants.RefractionQualityVariant.Low,
			TransmissiveMaterialQuality.TrueReflectionsAndRefraction => TransmissiveMaterialShaderConstants.RefractionQualityVariant.High,
			_ => TransmissiveMaterialShaderConstants.RefractionQualityVariant.Disabled
		};
		var refractionTypeVariant = TransmissiveMaterialShaderConstants.RefractionTypeVariant.Thin;

		if (config.AnisotropyMap.HasValue) flags |= TransmissiveMaterialShaderConstants.Flags.Anisotropy;
		if (config.EmissiveMap.HasValue) flags |= TransmissiveMaterialShaderConstants.Flags.Emissive;
		if (config.NormalMap.HasValue) flags |= TransmissiveMaterialShaderConstants.Flags.Normals;
		if (config.OcclusionRoughnessMetallicReflectanceMap.HasValue) {
			flags |= TransmissiveMaterialShaderConstants.Flags.Orm;
		}
		if (config.ColorMap.TexelType == TexelType.Rgba32) {
			alphaModeVariant = config.AlphaMode switch {
				TransmissiveMaterialAlphaMode.FullBlending => TransmissiveMaterialShaderConstants.AlphaModeVariant.AlphaOnBlended,
				_ => TransmissiveMaterialShaderConstants.AlphaModeVariant.AlphaOn
			};
		}
		if (config.RefractionThickness >= ThinThickRefractionModelCrossoverThickness) {
			refractionTypeVariant = TransmissiveMaterialShaderConstants.RefractionTypeVariant.Thick;
		}

		var shaderResourceName = shaderConstants.GetShaderResourceName(flags, alphaModeVariant, refractionQualityVariant, refractionTypeVariant);
		var result = InstantiateMaterial(shaderResourceName, config.Name);

		ApplyMaterialParam(result, config.ColorMap, shaderConstants.ParamColorMap);
		ApplyMaterialParam(result, config.RefractionThickness, shaderConstants.ParamSurfaceThickness);
		ApplyMaterialParam(result, config.AbsorptionTransmissionMap, shaderConstants.ParamAbsorptionTransmissionMap);
		ApplyMaterialParam(result, config.AnisotropyMap, shaderConstants.ParamAnisotropyMap);
		ApplyMaterialParam(result, config.EmissiveMap, shaderConstants.ParamEmissiveMap);
		ApplyMaterialParam(result, config.NormalMap, shaderConstants.ParamNormalMap);
		ApplyMaterialParam(result, config.OcclusionRoughnessMetallicReflectanceMap, shaderConstants.ParamOrmMap);

		return result;
	}

	Material InstantiateMaterial(string shaderResourceName, ReadOnlySpan<char> resourceName) {
		var shaderPackageHandle = GetOrLoadShaderPackageHandle(shaderResourceName);

		CreateMaterial(
			shaderPackageHandle,
			out var outHandle
		).ThrowIfFailure();
		var handle = (ResourceHandle<Material>) outHandle;

		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, resourceName, DefaultMaterialName);
		_activeMaterials.Add(handle);
		return HandleToInstance(handle);
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

	void ApplyMaterialParam(Material material, Texture? map, ReadOnlySpan<byte> param) {
		if (!map.HasValue) return;
		SetMaterialParameterTexture(
			material.Handle,
			in ParamRef(param),
			ParamLen(param),
			map.Value.Handle
		).ThrowIfFailure();
		_globals.DependencyTracker.RegisterDependency(material, map.Value);
	}

	void ApplyMaterialParam(Material material, float? val, ReadOnlySpan<byte> param) {
		if (!val.HasValue) return;
		SetMaterialParameterReal(
			material.Handle,
			in ParamRef(param),
			ParamLen(param),
			val.Value
		).ThrowIfFailure();
	}

	public string GetNameAsNewStringObject(ResourceHandle<Material> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultMaterialName));
	}
	public int GetNameLength(ResourceHandle<Material> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultMaterialName).Length;
	}
	public void CopyName(ResourceHandle<Material> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultMaterialName, destinationBuffer);
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_material_parameter_real")]
	static extern InteropResult SetMaterialParameterReal(
		UIntPtr materialHandle,
		ref readonly byte utf8ParameterNameBuffer,
		int parameterNameBufferLength,
		float val
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

			if (_testMaterialTextures.IsValueCreated) {
				_testMaterialTextures.Value.Dispose(disposeContainedResources: true);
			}
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