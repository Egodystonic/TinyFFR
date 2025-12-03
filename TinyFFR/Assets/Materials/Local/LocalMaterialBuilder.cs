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
	const string DefaultMaterialName = "Unnamed Material";
	const string TestMaterialName = "Test Material";
	const string TestUvTexResourceName = "Assets.Materials.uv.png";
	readonly ArrayPoolBackedMap<string, UIntPtr> _loadedShaderPackages = new();
	readonly ArrayPoolBackedVector<ResourceHandle<Material>> _activeMaterials = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly Lazy<ResourceGroup> _testMaterialTextures;
	readonly LocalAssetLoader _owningAssetLoader;
	bool _isDisposed = false;

	public ITextureBuilder TextureBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IMaterialBuilder)) : _owningAssetLoader.TextureBuilder;

	public LocalMaterialBuilder(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config, LocalAssetLoader owningAssetLoader) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
		_owningAssetLoader = owningAssetLoader;
		_testMaterialTextures = new(CreateTestMaterialTextures);
	}

	ResourceGroup CreateTestMaterialTextures() {
		var result = _globals.ResourceGroupProvider.CreateGroup(
			disposeContainedResourcesWhenDisposed: true,
			name: TestMaterialName + " Texture Group"
		);

		try {
			var uvTexFilePath = Path.Combine(LocalFileSystemUtils.ApplicationDataDirectoryPath, TestUvTexResourceName);
			if (!File.Exists(uvTexFilePath)) {
				var uvRes = EmbeddedResourceResolver.GetResource(TestUvTexResourceName);
				File.WriteAllBytes(uvTexFilePath, uvRes.AsSpan);
			}
			result.Add((_owningAssetLoader as IAssetLoader).LoadTexture(uvTexFilePath, isLinearColorspace: false, name: TestMaterialName + " Color Map"));
		}
		catch (Exception e) when (LocalFileSystemUtils.ExceptionIndicatesGeneralIoError(e)) {
			Console.WriteLine($"Using generated color map for test material as UV test texture could not be extracted ({e}/{e.Message}).");
			result.Add(TextureBuilder.CreateColorMap(
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
		}

		result.Add(TextureBuilder.CreateNormalMap(
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

	public Material CreateTestMaterial(bool ignoresLighting = true) {
		ThrowIfThisIsDisposed();
		
		var textureGroup = _testMaterialTextures.Value;
		
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
	Material HandleToInstance(ResourceHandle<Material> h) => new(h, this);

	public override string ToString() => _isDisposed ? "TinyFFR Local Material Builder [Disposed]" : "TinyFFR Local Material Builder";

	#region Disposal
	public bool IsDisposed(ResourceHandle<Material> handle) => _isDisposed || !_activeMaterials.Contains(handle);

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
			foreach (var packageHandle in _loadedShaderPackages.Values) DisposeShaderPackage(packageHandle).ThrowIfFailure();

			_activeMaterials.Dispose();
			_loadedShaderPackages.Dispose();

			if (_testMaterialTextures.IsValueCreated) {
				_testMaterialTextures.Value.Dispose(disposeContainedResources: true);
			}
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Material> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Material));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}