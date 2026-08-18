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
using System.IO;
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
sealed unsafe class LocalMaterialBuilder : IMaterialBuilder, IMaterialImplProvider, IResourceDirectory<Material>, IDisposable {
	[Flags]
	enum SupportedEffectsFlags {
		None = 0,
		UvTransform = 1 << 0,
		ColorMapBlend = 1 << 1,
		OrmMapBlend = 1 << 2,
		EmissiveMapBlend = 1 << 3,
		AtMapBlend = 1 << 4,
		Opacity = 1 << 5,
	}
	readonly record struct MaterialData(IShaderPackageConstants PackageConstants, SupportedEffectsFlags SupportedEffects, Texture? EffectBlendColorMap, Texture? EffectBlendOrmMap, Texture? EffectBlendEmissiveMap, Texture? EffectBlendAbsorptionTransmissionMap) {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool SupportsEffect(SupportedEffectsFlags effect) => (SupportedEffects & effect) == effect;
	}

	internal const string TestMaterialName = "Test Material";
	const string DefaultMaterialName = "Unnamed Material";
	internal const string DefaultMaterialInstanceName = "Default Material";
	readonly ArrayPoolBackedMap<string, UIntPtr> _loadedShaderPackages = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Material>, MaterialData> _activeMaterials = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Material>, ArrayPoolBackedStringKeyMap<Texture>> _activeMaterialMappedTextures = new();
	readonly StringKeyMapPool<Texture> _activeMaterialMappedTexturesPool = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly Lazy<ResourceGroup> _testMaterialTexturesRef;
	bool _isDisposed = false;

	public ITextureBuilder TextureBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IMaterialBuilder)) : field;

	public Material DefaultMaterial => new(new ResourceHandle<Material>(0U), this);

	public LocalMaterialBuilder(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config, ITextureBuilder texBuilderRef, Lazy<ResourceGroup> testMaterialTexturesRef) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
		TextureBuilder = texBuilderRef;
		_testMaterialTexturesRef = testMaterialTexturesRef;

		_globals.StoreResourceNameOrDefaultIfEmpty(default(ResourceHandle<Material>).Ident, DefaultMaterialInstanceName, DefaultMaterialName);
		_activeMaterials.Add(default, new MaterialData(DefaultMaterialShader, SupportedEffectsFlags.None, null, null, null, null));
	}
	
	public Material AllocateDefaultMaterialInstance(DefaultMaterialShaderConstants.ShadingModeVariant shadingMode, ColorVect baseColor) {
		ThrowIfThisIsDisposed();
		
		var shaderConstants = DefaultMaterialShader;

		var shaderResourceName = shaderConstants.GetShaderResourceName(shadingMode);
		var result = InstantiateMaterial(shaderResourceName, ReadOnlySpan<char>.Empty, shaderConstants);
		SetDefaultMaterialBaseColor(result, baseColor);

		return result;
	}
	public void SetDefaultMaterialBaseColor(Material material, ColorVect color) {
		ThrowIfThisOrHandleIsDisposed(material.GetHandleWithoutDisposeCheck());
		ApplyMaterialParam(material, color.AsVector4, DefaultMaterialShader.ParamBaseColor);
	}

	public Material AllocateTextMaterialInstance(Texture sdfAtlas, ColorVect textColor, ColorVect backgroundColor, ColorVect outlineColor, float outlineThickness, ReadOnlySpan<char> name) {
		ThrowIfThisIsDisposed();

		var shaderConstants = TextMaterialShader;

		var result = InstantiateMaterial(shaderConstants.ShaderResourceName, name, shaderConstants);
		ApplyMaterialParam(result, sdfAtlas, shaderConstants.ParamSdfMap);
		ApplyMaterialParam(result, textColor.AsVector4, shaderConstants.ParamTextColor);
		ApplyMaterialParam(result, backgroundColor.AsVector4, shaderConstants.ParamBackgroundColor);
		ApplyMaterialParam(result, outlineColor.AsVector4, shaderConstants.ParamOutlineColor);
		ApplyMaterialParam(result, outlineThickness, shaderConstants.ParamOutlineThickness);

		return result;
	}

	public Material AllocateCanvasMaterialInstance(Texture colorMap, ReadOnlySpan<char> name) {
		ThrowIfThisIsDisposed();

		var shaderConstants = CanvasMaterialShader;

		var result = InstantiateMaterial(shaderConstants.ShaderResourceName, name, shaderConstants);
		ApplyMaterialParam(result, colorMap, shaderConstants.ParamColorMap);

		var supportedEffects = SupportedEffectsFlags.UvTransform | SupportedEffectsFlags.ColorMapBlend | SupportedEffectsFlags.Opacity;
		SetUpDefaultEffectsParameters(result, shaderConstants, supportedEffects);
		_activeMaterials[result.Handle] = _activeMaterials[result.Handle] with { SupportedEffects = supportedEffects };

		return result;
	}

	public Material AllocateImGuiMaterialInstance(Texture colorMap, ReadOnlySpan<char> name) {
		ThrowIfThisIsDisposed();

		var shaderConstants = ImGuiMaterialShader;

		var result = InstantiateMaterial(shaderConstants.ShaderResourceName, name, shaderConstants);
		ApplyMaterialParam(result, colorMap, shaderConstants.ParamColorMap);

		return result;
	}

	public void SetImGuiMaterialColorMap(Material material, Texture colorMap) {
		ThrowIfThisIsDisposed();
		ApplyMaterialParam(material, colorMap, ImGuiMaterialShader.ParamColorMap);
	}

	public Material CreateTestMaterial(bool ignoresLighting) {
		ThrowIfThisIsDisposed();
		
		var textureGroup = _testMaterialTexturesRef.Value;
		
		if (ignoresLighting) {
			return CreateLightingIgnoringMaterial(new LightingIgnoringMaterialCreationConfig {
				ColorMap = textureGroup.Textures[0],
				Name = TestMaterialName
			});
		}
		else {
			return CreateStandardMaterial(new StandardMaterialCreationConfig {
				ColorMap = textureGroup.Textures[0],
				NormalMap = textureGroup.Textures[1],
				Name = TestMaterialName
			});
		}
	}

	public Material CreateLightingIgnoringMaterial(in LightingIgnoringMaterialCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var shaderConstants = LightingIgnoringMaterialShader;

		var alphaModeVariant = LightingIgnoringMaterialShaderConstants.AlphaModeVariant.AlphaOff;

		if (config.ColorMap.TexelType == TexelType.Rgba32) {
			alphaModeVariant = LightingIgnoringMaterialShaderConstants.AlphaModeVariant.AlphaOn;
		}

		var shaderResourceName = shaderConstants.GetShaderResourceName(config.EnablePerInstanceEffects, alphaModeVariant);
		var result = InstantiateMaterial(shaderResourceName, config.Name, shaderConstants);

		ApplyMaterialParam(result, config.ColorMap, shaderConstants.ParamColorMap);

		if (config.EnablePerInstanceEffects) {
			var supportedEffects = SupportedEffectsFlags.UvTransform | SupportedEffectsFlags.ColorMapBlend;
			
			SetUpDefaultEffectsParameters(result, shaderConstants, supportedEffects);
			_activeMaterials[result.Handle] = _activeMaterials[result.Handle] with { SupportedEffects = supportedEffects };
		}

		return result;
	}

	public Material CreateColorKeyedMaterial(in ColorKeyedMaterialCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var shaderConstants = ColorKeyedMaterialShader;

		var alphaModeVariant = config.BlendOutputAlphaWithScene
			? ColorKeyedMaterialShaderConstants.AlphaModeVariant.AlphaOn
			: ColorKeyedMaterialShaderConstants.AlphaModeVariant.AlphaOff;

		var shaderResourceName = shaderConstants.GetShaderResourceName(alphaModeVariant);
		var result = InstantiateMaterial(shaderResourceName, config.Name, shaderConstants);

		ApplyMaterialParam(result, config.KeyMap, shaderConstants.ParamKeyMap);
		ApplyMaterialParam(result, new Vector4(1f, 0f, 0f, 0f), shaderConstants.ParamXChannelColor);
		ApplyMaterialParam(result, new Vector4(0f, 1f, 0f, 0f), shaderConstants.ParamYChannelColor);
		ApplyMaterialParam(result, new Vector4(0f, 0f, 1f, 0f), shaderConstants.ParamZChannelColor);
		ApplyMaterialParam(result, new Vector4(0f, 0f, 0f, 1f), shaderConstants.ParamWChannelColor);

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

		var shaderResourceName = shaderConstants.GetShaderResourceName(config.EnablePerInstanceEffects, flags, alphaModeVariant, ormReflectanceVariant);
		var result = InstantiateMaterial(shaderResourceName, config.Name, shaderConstants);

		ApplyMaterialParam(result, config.ColorMap, shaderConstants.ParamColorMap);
		ApplyMaterialParam(result, config.AnisotropyMap, shaderConstants.ParamAnisotropyMap);
		ApplyMaterialParam(result, config.ClearCoatMap, shaderConstants.ParamClearCoatMap);
		ApplyMaterialParam(result, config.EmissiveMap, shaderConstants.ParamEmissiveMap);
		ApplyMaterialParam(result, config.NormalMap, shaderConstants.ParamNormalMap);
		ApplyMaterialParam(result, config.OcclusionRoughnessMetallicReflectanceMap, shaderConstants.ParamOrmMap);

		if (config.EnablePerInstanceEffects) {
			var supportedEffects = SupportedEffectsFlags.UvTransform | SupportedEffectsFlags.ColorMapBlend;
			if (config.EmissiveMap.HasValue) supportedEffects |= SupportedEffectsFlags.EmissiveMapBlend;
			if (config.OcclusionRoughnessMetallicReflectanceMap.HasValue) supportedEffects |= SupportedEffectsFlags.OrmMapBlend;

			SetUpDefaultEffectsParameters(result, shaderConstants, supportedEffects);
			_activeMaterials[result.Handle] = _activeMaterials[result.Handle] with { SupportedEffects = supportedEffects };
		}

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
			TransmissiveMaterialQuality.SkyboxOnlyReflectionsAndRefraction => TransmissiveMaterialShaderConstants.RefractionQualityVariant.Low,
			_ => TransmissiveMaterialShaderConstants.RefractionQualityVariant.High,
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

		var shaderResourceName = shaderConstants.GetShaderResourceName(config.EnablePerInstanceEffects, flags, alphaModeVariant, refractionQualityVariant, refractionTypeVariant);
		var result = InstantiateMaterial(shaderResourceName, config.Name, shaderConstants);

		ApplyMaterialParam(result, config.ColorMap, shaderConstants.ParamColorMap);
		ApplyMaterialParam(result, config.RefractionThickness, shaderConstants.ParamSurfaceThickness);
		ApplyMaterialParam(result, config.AbsorptionTransmissionMap, shaderConstants.ParamAbsorptionTransmissionMap);
		ApplyMaterialParam(result, config.AnisotropyMap, shaderConstants.ParamAnisotropyMap);
		ApplyMaterialParam(result, config.EmissiveMap, shaderConstants.ParamEmissiveMap);
		ApplyMaterialParam(result, config.NormalMap, shaderConstants.ParamNormalMap);
		ApplyMaterialParam(result, config.OcclusionRoughnessMetallicReflectanceMap, shaderConstants.ParamOrmMap);

		if (config.EnablePerInstanceEffects) {
			var supportedEffects = SupportedEffectsFlags.UvTransform | SupportedEffectsFlags.ColorMapBlend | SupportedEffectsFlags.AtMapBlend;
			if (config.EmissiveMap.HasValue) supportedEffects |= SupportedEffectsFlags.EmissiveMapBlend;
			if (config.OcclusionRoughnessMetallicReflectanceMap.HasValue) supportedEffects |= SupportedEffectsFlags.OrmMapBlend;

			SetUpDefaultEffectsParameters(result, shaderConstants, supportedEffects);
			_activeMaterials[result.Handle] = _activeMaterials[result.Handle] with { SupportedEffects = supportedEffects };
		}

		return result;
	}

	void SetUpDefaultEffectsParameters(Material mat, IShaderPackageConstants packageConstants, SupportedEffectsFlags supportedEffects) {
		if ((supportedEffects & SupportedEffectsFlags.UvTransform) != 0) {
			var identityMat = Matrix4x4.Identity;
			ApplyMaterialParam(mat, in identityMat, packageConstants.GetEffectUvTransformParamOrThrow());
		}
		if ((supportedEffects & SupportedEffectsFlags.Opacity) != 0) {
			ApplyMaterialParam(mat, 1f, packageConstants.GetEffectOpacityParamOrThrow());
		}
	}

	Material InstantiateMaterial(string shaderResourceName, ReadOnlySpan<char> resourceName, IShaderPackageConstants packageConstants) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		var shaderPackageHandle = GetOrLoadShaderPackageHandle(shaderResourceName);

		CreateMaterial(
			shaderPackageHandle,
			out var outHandle
		).ThrowIfFailure();
		var handle = (ResourceHandle<Material>) outHandle;

		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, resourceName, DefaultMaterialName);
		_activeMaterials.Add(handle, new MaterialData(packageConstants, 0, null, null, null, null));
		
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

	ArrayPoolBackedStringKeyMap<Texture> GetOrCreateAssociatedTextureMap(ResourceHandle<Material> handle) {
		if (_activeMaterialMappedTextures.TryGetValue(handle, out var result)) return result;
		result = _activeMaterialMappedTexturesPool.Rent();
		_activeMaterialMappedTextures.Add(handle, result);
		return result;
	}

	public Texture? TryGetAssociatedTexture(ResourceHandle<Material> handle, ReadOnlySpan<char> parameterName) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_activeMaterialMappedTextures.TryGetValue(handle, out var textureMap)) return null;
		return textureMap.TryGetValue(parameterName, out var result) ? result : null;
	}

	void ApplyMaterialParam(Material material, Texture? map, ReadOnlySpan<byte> param) {
		if (!map.HasValue) return;
		var renderingConfig = map.Value.RenderingConfig;

		Span<char> paramChars = stackalloc char[SpanUtils.GetUtf16Length(param)];
		var paramName = SpanUtils.ConvertUtf8ToUtf16(paramChars, param);
		var associatedTextures = GetOrCreateAssociatedTextureMap(material.Handle);
		var hasPreviousMap = associatedTextures.TryGetValue(paramName, out var previousMap);
		associatedTextures[paramName] = map.Value;
		if (hasPreviousMap && previousMap != map.Value) {
			var previousMapStillBound = false;
			foreach (var associatedTexture in associatedTextures.Values) {
				if (associatedTexture != previousMap) continue;
				previousMapStillBound = true;
				break;
			}
			if (!previousMapStillBound) _globals.DependencyTracker.DeregisterDependency(material, previousMap);
		}

		SetMaterialParameterTexture(
			material.Handle,
			in ParamRef(param),
			ParamLen(param),
			map.Value.Handle,
			!map.Value.ContainsMipMaps,
			renderingConfig.DisableTexelBlending,
			renderingConfig.DisableTextureRepeat,
			renderingConfig.AnisotropyLevel
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
	
	void ApplyMaterialParam(Material material, Vector4? val, ReadOnlySpan<byte> param) {
		if (!val.HasValue) return;
		SetMaterialParameterVect(
			material.Handle,
			in ParamRef(param),
			ParamLen(param),
			val.Value
		).ThrowIfFailure();
	}

	void ApplyMaterialParam(Material material, ref readonly Matrix4x4 matrixRef, ReadOnlySpan<byte> param) {
		SetMaterialParameterMatrix(
			material.Handle,
			in ParamRef(param),
			ParamLen(param),
			in matrixRef
		).ThrowIfFailure();
	}

	public bool GetSupportsPerInstanceEffects(ResourceHandle<Material> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeMaterials[handle].SupportedEffects != 0;
	}

	public bool GetSupportsColorKeying(ResourceHandle<Material> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeMaterials[handle].PackageConstants == ColorKeyedMaterialShader;
	}
	
	public bool GetIsDefault(ResourceHandle<Material> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return handle == DefaultMaterial.Handle;
	}

	public bool GetSupportsShadows(ResourceHandle<Material> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeMaterials[handle].PackageConstants.SupportsShadows;
	}

	public Material Duplicate(ResourceHandle<Material> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		DuplicateMaterial(
			handle,
			out var newMaterialHandle
		).ThrowIfFailure();

		_globals.StoreResourceNameOrDefaultIfEmpty(new ResourceHandle<Material>(newMaterialHandle).Ident, _globals.GetResourceName(handle.Ident, DefaultMaterialName), DefaultMaterialName);
		_activeMaterials.Add(newMaterialHandle, _activeMaterials[handle]);

		if (_activeMaterialMappedTextures.TryGetValue(handle, out var sourceTextureMap)) {
			var destTextureMap = _activeMaterialMappedTexturesPool.Rent();
			foreach (var kvp in sourceTextureMap) destTextureMap.Add(kvp.Key.AsSpan, kvp.Value);
			_activeMaterialMappedTextures.Add(newMaterialHandle, destTextureMap);
		}

		return HandleToInstance(newMaterialHandle);
	}

	public void SetEffectTransform(ResourceHandle<Material> handle, Transform2D newTransform) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var matData = _activeMaterials[handle];
		if (!matData.SupportsEffect(SupportedEffectsFlags.UvTransform)) return;

		var transformMat = (newTransform with { Scaling = newTransform.Scaling.Reciprocal ?? XYPair<float>.Zero }).ToMatrix();
		var value = Matrix4x4.Identity;
		value.M11 = transformMat.M11;
		value.M12 = transformMat.M21; // These two rows swapped to make filament column-major convention apply rotation correctly
		value.M21 = transformMat.M12; // These two rows swapped to make filament column-major convention apply rotation correctly
		value.M22 = transformMat.M22;
		value.M31 = -transformMat.M31; // Deliberately inverts X-axis translation
		value.M32 = -transformMat.M32; // Deliberately inverts Y-axis translation

		var uvTransformParam = matData.PackageConstants.GetEffectUvTransformParamOrThrow();
		ApplyMaterialParam(HandleToInstance(handle), in value, uvTransformParam);
	}
	public void SetEffectBlendTexture(ResourceHandle<Material> handle, MaterialEffectMapType mapType, Texture mapTexture) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var matData = _activeMaterials[handle];

		ReadOnlySpan<byte> param;

		switch (mapType) {
			case MaterialEffectMapType.Color:
				if (!matData.SupportsEffect(SupportedEffectsFlags.ColorMapBlend)) return;
				if (matData.EffectBlendColorMap != null) _globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), matData.EffectBlendColorMap.Value);
				_activeMaterials[handle] = matData with { EffectBlendColorMap = mapTexture };
				param = matData.PackageConstants.GetEffectColorMapTexParamOrThrow();
				break;
			case MaterialEffectMapType.OcclusionRoughnessMetallic:
				if (!matData.SupportsEffect(SupportedEffectsFlags.OrmMapBlend)) return;
				if (matData.EffectBlendOrmMap != null) _globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), matData.EffectBlendOrmMap.Value);
				_activeMaterials[handle] = matData with { EffectBlendOrmMap = mapTexture };
				param = matData.PackageConstants.GetEffectOrmMapTexParamOrThrow();
				break;
			case MaterialEffectMapType.Emissive:
				if (!matData.SupportsEffect(SupportedEffectsFlags.EmissiveMapBlend)) return;
				if (matData.EffectBlendEmissiveMap != null) _globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), matData.EffectBlendEmissiveMap.Value);
				_activeMaterials[handle] = matData with { EffectBlendEmissiveMap = mapTexture };
				param = matData.PackageConstants.GetEffectEmissiveMapTexParamOrThrow();
				break;
			case MaterialEffectMapType.AbsorptionTransmission:
				if (!matData.SupportsEffect(SupportedEffectsFlags.AtMapBlend)) return;
				if (matData.EffectBlendAbsorptionTransmissionMap != null) _globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), matData.EffectBlendAbsorptionTransmissionMap.Value);
				_activeMaterials[handle] = matData with { EffectBlendAbsorptionTransmissionMap = mapTexture };
				param = matData.PackageConstants.GetEffectAbsorptionTransmissionMapTexParamOrThrow();
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(mapType), mapType, null);
		}

		ApplyMaterialParam(HandleToInstance(handle), mapTexture, param);
	}
	public void SetEffectBlendDistance(ResourceHandle<Material> handle, MaterialEffectMapType mapType, float distance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var matData = _activeMaterials[handle];

		ReadOnlySpan<byte> param;

		switch (mapType) {
			case MaterialEffectMapType.Color:
				if (!matData.SupportsEffect(SupportedEffectsFlags.ColorMapBlend)) return;
				param = matData.PackageConstants.GetEffectColorMapDistanceParamOrThrow();
				break;
			case MaterialEffectMapType.OcclusionRoughnessMetallic:
				if (!matData.SupportsEffect(SupportedEffectsFlags.OrmMapBlend)) return;
				param = matData.PackageConstants.GetEffectOrmMapDistanceParamOrThrow();
				break;
			case MaterialEffectMapType.Emissive:
				if (!matData.SupportsEffect(SupportedEffectsFlags.EmissiveMapBlend)) return;
				param = matData.PackageConstants.GetEffectEmissiveMapDistanceParamOrThrow();
				break;
			case MaterialEffectMapType.AbsorptionTransmission:
				if (!matData.SupportsEffect(SupportedEffectsFlags.AtMapBlend)) return;
				param = matData.PackageConstants.GetEffectAbsorptionTransmissionMapDistanceParamOrThrow();
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(mapType), mapType, null);
		}

		if (!Single.IsFinite(distance)) distance = 0f;
		ApplyMaterialParam(HandleToInstance(handle), distance, param);
	}
	public void SetEffectOpacity(ResourceHandle<Material> handle, float opacity) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var matData = _activeMaterials[handle];

		if (!matData.SupportsEffect(SupportedEffectsFlags.Opacity)) return;

		if (!Single.IsFinite(opacity)) opacity = 1f;
		ApplyMaterialParam(HandleToInstance(handle), Single.Clamp(opacity, 0f, 1f), matData.PackageConstants.GetEffectOpacityParamOrThrow());
	}

	public void SetScissorRect(ResourceHandle<Material> handle, XYPair<int> viewportRelativeBottomLeftOffset, XYPair<int> dimensions) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (dimensions.X < 0 || dimensions.Y < 0) {
			throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, "Scissor rectangle dimensions must not be negative.");
		}
		SetMaterialScissor(
			handle,
			viewportRelativeBottomLeftOffset.X,
			viewportRelativeBottomLeftOffset.Y,
			dimensions.X,
			dimensions.Y
		).ThrowIfFailure();
	}
	public void ClearScissorRect(ResourceHandle<Material> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		UnsetMaterialScissor(handle).ThrowIfFailure();
	}

	public void SetKeyedColor(ResourceHandle<Material> handle, ColorChannel key, ColorVect color) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (_activeMaterials[handle].PackageConstants != ColorKeyedMaterialShader) return;
		var param = key switch {
			ColorChannel.R => ColorKeyedMaterialShader.ParamXChannelColor,
			ColorChannel.G => ColorKeyedMaterialShader.ParamYChannelColor,
			ColorChannel.B => ColorKeyedMaterialShader.ParamZChannelColor,
			ColorChannel.A => ColorKeyedMaterialShader.ParamWChannelColor,
			_ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
		};
		ApplyMaterialParam(HandleToInstance(handle), color.AsVector4, param);
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

	#region Native Methods
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "duplicate_material")]
	static extern InteropResult DuplicateMaterial(
		UIntPtr targetMaterialHandle,
		out UIntPtr outMaterialHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_material_parameter_texture")]
	static extern InteropResult SetMaterialParameterTexture(
		UIntPtr materialHandle,
		ref readonly byte utf8ParameterNameBuffer,
		int parameterNameBufferLength,
		UIntPtr textureHandle,
		InteropBool disableMinMapFiltering, 
		InteropBool disableBilinearFiltering, 
		InteropBool disableTextureRepeat, 
		float anisotropyLevel
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_material_parameter_real")]
	static extern InteropResult SetMaterialParameterReal(
		UIntPtr materialHandle,
		ref readonly byte utf8ParameterNameBuffer,
		int parameterNameBufferLength,
		float val
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_material_scissor")]
	static extern InteropResult SetMaterialScissor(
		UIntPtr materialHandle,
		int left,
		int bottom,
		int width,
		int height
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "unset_material_scissor")]
	static extern InteropResult UnsetMaterialScissor(
		UIntPtr materialHandle
	);
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_material_parameter_vect")]
	static extern InteropResult SetMaterialParameterVect(
		UIntPtr materialHandle,
		ref readonly byte utf8ParameterNameBuffer,
		int parameterNameBufferLength,
		Vector4 val
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_material_parameter_matrix")]
	static extern InteropResult SetMaterialParameterMatrix(
		UIntPtr materialHandle,
		ref readonly byte utf8ParameterNameBuffer,
		int parameterNameBufferLength,
		ref readonly Matrix4x4 matrixRef
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
	Material HandleToInstance(ResourceHandle<Material> h) => new(h, this);

	#region Resource Directory
	public IndirectEnumerable<object, Material> AllActiveInstances {
		get {
			static LocalMaterialBuilder CastSelf(object self) => self as LocalMaterialBuilder ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetCount(object self) => CastSelf(self)._activeMaterials.Count;
			static int GetVersion(object self) => CastSelf(self)._activeMaterials.Version;
			static Material GetItem(object self, int index) => CastSelf(self).HandleToInstance(CastSelf(self)._activeMaterials.GetPairAtIndex(index).Key);

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
	public bool ResourceNameMatchIsMatching(Material resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var handle = resource.GetHandleWithoutDisposeCheck();
		ThrowIfThisOrHandleIsDisposed(handle);
		return allowPartialMatch
			? _globals.GetResourceName(handle.Ident, DefaultMaterialName).Contains(name, comparisonType)
			: _globals.GetResourceName(handle.Ident, DefaultMaterialName).Equals(name, comparisonType);
	}
	#endregion

	public override string ToString() => _isDisposed ? "TinyFFR Local Material Builder [Disposed]" : "TinyFFR Local Material Builder";

	#region Disposal
	public bool IsDisposed(ResourceHandle<Material> handle) => _isDisposed || !_activeMaterials.ContainsKey(handle);

	public void Dispose(ResourceHandle<Material> handle) => Dispose(handle, removeFromCollection: true);
	void Dispose(ResourceHandle<Material> handle, bool removeFromCollection) {
		if (handle == DefaultMaterial.Handle) return; // Never dispose/evict the default-material
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		_globals.DependencyTracker.DeregisterAllDependencies(HandleToInstance(handle));
		LocalFrameSynchronizationManager.QueueResourceDisposal(handle, &DisposeMaterial);
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (_activeMaterialMappedTextures.TryGetValue(handle, out var textureMap)) {
			_activeMaterialMappedTexturesPool.Return(textureMap);
			_activeMaterialMappedTextures.Remove(handle);
		}
		if (removeFromCollection) _activeMaterials.Remove(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _activeMaterials) Dispose(kvp.Key, removeFromCollection: false);
			foreach (var packageHandle in _loadedShaderPackages.Values) DisposeShaderPackage(packageHandle).ThrowIfFailure();

			foreach (var textureMap in _activeMaterialMappedTextures.Values) textureMap.Dispose();

			_activeMaterials.Dispose();
			_activeMaterialMappedTextures.Dispose();
			_activeMaterialMappedTexturesPool.Dispose();
			_loadedShaderPackages.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Material> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Material));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}