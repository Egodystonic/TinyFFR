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
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Resources;
using System.Security;
using static Egodystonic.TinyFFR.Assets.Materials.Local.LocalShaderPackageConstants;

namespace Egodystonic.TinyFFR.Assets.Materials.Local;

[SuppressUnmanagedCodeSecurity]
sealed unsafe class LocalMaterialBuilder : IMaterialBuilder, IMaterialImplProvider, IDisposable {
	readonly record struct MaterialData(IShaderPackageConstants PackageConstants, bool SupportsPerInstanceEffects, Texture? EffectBlendColorMap, Texture? EffectBlendOrmMap, Texture? EffectBlendEmissiveMap, Texture? EffectBlendAbsorptionTransmissionMap);

	internal const string TestMaterialName = "Test Material";
	const string DefaultMaterialName = "Unnamed Material";
	readonly ArrayPoolBackedMap<string, UIntPtr> _loadedShaderPackages = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Material>, MaterialData> _activeMaterials = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly Lazy<ResourceGroup> _testMaterialTexturesRef;
	bool _isDisposed = false;

	public ITextureBuilder TextureBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IMaterialBuilder)) : field;

	public LocalMaterialBuilder(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config, ITextureBuilder texBuilderRef, Lazy<ResourceGroup> testMaterialTexturesRef) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
		TextureBuilder = texBuilderRef;
		_testMaterialTexturesRef = testMaterialTexturesRef;
	}

	

	public Material CreateTestMaterial(bool ignoresLighting = true) {
		ThrowIfThisIsDisposed();
		
		var textureGroup = _testMaterialTexturesRef.Value;
		
		if (ignoresLighting) {
			return CreateSimpleMaterial(new SimpleMaterialCreationConfig {
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

		var shaderResourceName = shaderConstants.GetShaderResourceName(config.EnablePerInstanceEffects, flags, alphaModeVariant);
		var result = InstantiateMaterial(shaderResourceName, config.Name, shaderConstants, config.EnablePerInstanceEffects);

		ApplyMaterialParam(result, config.ColorMap, shaderConstants.ParamColorMap);
		ApplyMaterialParam(result, config.EmissiveMap, shaderConstants.ParamEmissiveMap);

		if (config.EnablePerInstanceEffects) SetUpDefaultEffectsParameters(result, shaderConstants);

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
		var result = InstantiateMaterial(shaderResourceName, config.Name, shaderConstants, config.EnablePerInstanceEffects);

		ApplyMaterialParam(result, config.ColorMap, shaderConstants.ParamColorMap);
		ApplyMaterialParam(result, config.AnisotropyMap, shaderConstants.ParamAnisotropyMap);
		ApplyMaterialParam(result, config.ClearCoatMap, shaderConstants.ParamClearCoatMap);
		ApplyMaterialParam(result, config.EmissiveMap, shaderConstants.ParamEmissiveMap);
		ApplyMaterialParam(result, config.NormalMap, shaderConstants.ParamNormalMap);
		ApplyMaterialParam(result, config.OcclusionRoughnessMetallicReflectanceMap, shaderConstants.ParamOrmMap);

		if (config.EnablePerInstanceEffects) SetUpDefaultEffectsParameters(result, shaderConstants);

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
		var result = InstantiateMaterial(shaderResourceName, config.Name, shaderConstants, config.EnablePerInstanceEffects);

		ApplyMaterialParam(result, config.ColorMap, shaderConstants.ParamColorMap);
		ApplyMaterialParam(result, config.RefractionThickness, shaderConstants.ParamSurfaceThickness);
		ApplyMaterialParam(result, config.AbsorptionTransmissionMap, shaderConstants.ParamAbsorptionTransmissionMap);
		ApplyMaterialParam(result, config.AnisotropyMap, shaderConstants.ParamAnisotropyMap);
		ApplyMaterialParam(result, config.EmissiveMap, shaderConstants.ParamEmissiveMap);
		ApplyMaterialParam(result, config.NormalMap, shaderConstants.ParamNormalMap);
		ApplyMaterialParam(result, config.OcclusionRoughnessMetallicReflectanceMap, shaderConstants.ParamOrmMap);

		if (config.EnablePerInstanceEffects) SetUpDefaultEffectsParameters(result, shaderConstants);

		return result;
	}

	void SetUpDefaultEffectsParameters(Material mat, IShaderPackageConstants packageConstants) {
		if (packageConstants.HasEffectUvTransform) {
			var identityMat = Matrix4x4.Identity;
			ApplyMaterialParam(mat, in identityMat, packageConstants.GetEffectUvTransformParamOrThrow());
		}
	}

	Material InstantiateMaterial(string shaderResourceName, ReadOnlySpan<char> resourceName, IShaderPackageConstants packageConstants, bool supportsPerInstanceEffects) {
		var shaderPackageHandle = GetOrLoadShaderPackageHandle(shaderResourceName);

		CreateMaterial(
			shaderPackageHandle,
			out var outHandle
		).ThrowIfFailure();
		var handle = (ResourceHandle<Material>) outHandle;

		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, resourceName, DefaultMaterialName);
		_activeMaterials.Add(handle, new MaterialData(packageConstants, supportsPerInstanceEffects, null, null, null, null));
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
		return _activeMaterials[handle].SupportsPerInstanceEffects;
	}

	public Material Duplicate(ResourceHandle<Material> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		DuplicateMaterial(
			handle,
			out var newMaterialHandle
		).ThrowIfFailure();

		_globals.StoreResourceNameOrDefaultIfEmpty(new ResourceHandle<Material>(newMaterialHandle).Ident, _globals.GetResourceName(handle.Ident, DefaultMaterialName), DefaultMaterialName);
		_activeMaterials.Add(newMaterialHandle, _activeMaterials[handle]);
		return HandleToInstance(newMaterialHandle);
	}

	public void SetEffectTransform(ResourceHandle<Material> handle, Transform2D newTransform) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var matData = _activeMaterials[handle];
		if (!matData.SupportsPerInstanceEffects || !matData.PackageConstants.HasEffectUvTransform) return;

		var transformMat = (newTransform with { Scaling = newTransform.Scaling.Reciprocal ?? XYPair<float>.Zero }).ToMatrix();
		var value = Matrix4x4.Identity;
		value.M11 = transformMat.M11;
		value.M12 = transformMat.M12;
		value.M21 = transformMat.M21;
		value.M22 = transformMat.M22;
		value.M31 = -transformMat.M31; // Deliberately inverts X-axis translation
		value.M32 = -transformMat.M32; // Deliberately inverts Y-axis translation

		var uvTransformParam = matData.PackageConstants.GetEffectUvTransformParamOrThrow();
		ApplyMaterialParam(HandleToInstance(handle), in value, uvTransformParam);
	}
	public void SetEffectBlendTexture(ResourceHandle<Material> handle, MaterialEffectMapType mapType, Texture mapTexture) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var matData = _activeMaterials[handle];
		if (!matData.SupportsPerInstanceEffects) return;

		ReadOnlySpan<byte> param;

		switch (mapType) {
			case MaterialEffectMapType.Color:
				if (!matData.PackageConstants.HasEffectColorMap) return;
				if (matData.EffectBlendColorMap != null) _globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), matData.EffectBlendColorMap.Value);
				_activeMaterials[handle] = matData with { EffectBlendColorMap = mapTexture };
				param = matData.PackageConstants.GetEffectColorMapTexParamOrThrow();
				break;
			case MaterialEffectMapType.OcclusionRoughnessMetallic:
				if (!matData.PackageConstants.HasEffectOrmMap) return;
				if (matData.EffectBlendOrmMap != null) _globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), matData.EffectBlendOrmMap.Value);
				_activeMaterials[handle] = matData with { EffectBlendOrmMap = mapTexture };
				param = matData.PackageConstants.GetEffectOrmMapTexParamOrThrow();
				break;
			case MaterialEffectMapType.Emissive:
				if (!matData.PackageConstants.HasEffectEmissiveMap) return;
				if (matData.EffectBlendEmissiveMap != null) _globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), matData.EffectBlendEmissiveMap.Value);
				_activeMaterials[handle] = matData with { EffectBlendEmissiveMap = mapTexture };
				param = matData.PackageConstants.GetEffectEmissiveMapTexParamOrThrow();
				break;
			case MaterialEffectMapType.AbsorptionTransmission:
				if (!matData.PackageConstants.HasEffectAbsorptionTransmissionMap) return;
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
		if (!matData.SupportsPerInstanceEffects) return;

		ReadOnlySpan<byte> param;

		switch (mapType) {
			case MaterialEffectMapType.Color:
				if (!matData.PackageConstants.HasEffectColorMap) return;
				param = matData.PackageConstants.GetEffectColorMapDistanceParamOrThrow();
				break;
			case MaterialEffectMapType.OcclusionRoughnessMetallic:
				if (!matData.PackageConstants.HasEffectOrmMap) return;
				param = matData.PackageConstants.GetEffectOrmMapDistanceParamOrThrow();
				break;
			case MaterialEffectMapType.Emissive:
				if (!matData.PackageConstants.HasEffectEmissiveMap) return;
				param = matData.PackageConstants.GetEffectEmissiveMapDistanceParamOrThrow();
				break;
			case MaterialEffectMapType.AbsorptionTransmission:
				if (!matData.PackageConstants.HasEffectAbsorptionTransmissionMap) return;
				param = matData.PackageConstants.GetEffectAbsorptionTransmissionMapDistanceParamOrThrow();
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(mapType), mapType, null);
		}

		ApplyMaterialParam(HandleToInstance(handle), distance, param);
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
		UIntPtr textureHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_material_parameter_real")]
	static extern InteropResult SetMaterialParameterReal(
		UIntPtr materialHandle,
		ref readonly byte utf8ParameterNameBuffer,
		int parameterNameBufferLength,
		float val
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

	public override string ToString() => _isDisposed ? "TinyFFR Local Material Builder [Disposed]" : "TinyFFR Local Material Builder";

	#region Disposal
	public bool IsDisposed(ResourceHandle<Material> handle) => _isDisposed || !_activeMaterials.ContainsKey(handle);

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
			foreach (var kvp in _activeMaterials) Dispose(kvp.Key, removeFromCollection: false);
			foreach (var packageHandle in _loadedShaderPackages.Values) DisposeShaderPackage(packageHandle).ThrowIfFailure();

			_activeMaterials.Dispose();
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