// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;

namespace Egodystonic.TinyFFR.Assets.Materials.Local;

interface IShaderPackageConstants {
	bool SupportsShadows { get; }

	bool HasEffectUvTransform { get; }
	bool HasEffectColorMap { get; }
	bool HasEffectEmissiveMap { get; }
	bool HasEffectAbsorptionTransmissionMap { get; }
	bool HasEffectOrmMap { get; }
	bool HasEffectOpacity { get; }

	ReadOnlySpan<byte> GetEffectUvTransformParamOrThrow();
	ReadOnlySpan<byte> GetEffectColorMapTexParamOrThrow();
	ReadOnlySpan<byte> GetEffectEmissiveMapTexParamOrThrow();
	ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapTexParamOrThrow();
	ReadOnlySpan<byte> GetEffectOrmMapTexParamOrThrow();
	ReadOnlySpan<byte> GetEffectColorMapDistanceParamOrThrow();
	ReadOnlySpan<byte> GetEffectEmissiveMapDistanceParamOrThrow();
	ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapDistanceParamOrThrow();
	ReadOnlySpan<byte> GetEffectOrmMapDistanceParamOrThrow();
	ReadOnlySpan<byte> GetEffectOpacityParamOrThrow();
}

#pragma warning disable CA1001 // Warning about the ArrayPoolBackMaps not being disposed; but we know they will live for the entire lifetime of the application
static class LocalShaderPackageConstants {
	const string ResourceNamespace = "Assets.Materials.Local.Shaders.CompiledObjects.";
	const string ShaderResourceExtension = ".filamat.zip";
	const string ShaderWithEffectsSuffix = "_withfx";

	public static ref readonly byte ParamRef(ReadOnlySpan<byte> param) => ref MemoryMarshal.GetReference(param);
	public static int ParamLen(ReadOnlySpan<byte> param) => param.Length;

	public static StandardMaterialShaderConstants StandardMaterialShader { get; } = new();
	public sealed class StandardMaterialShaderConstants : IShaderPackageConstants {
		[Flags]
		public enum Flags {
			Anisotropy = 1 << 0,
			ClearCoat = 1 << 1,
			Emissive = 1 << 2,
			Normals = 1 << 3,
			Orm = 1 << 4,
		}
		public enum AlphaModeVariant {
			AlphaOff,
			AlphaOn,
			AlphaOnBlended
		}
		public enum OrmReflectanceVariant {
			Off,
			On
		}

		readonly ArrayPoolBackedMap<(bool SupportsEffects, Flags Flags, AlphaModeVariant AlphaMode, OrmReflectanceVariant OrmReflectance), string> _resourceNameMap;

		public StandardMaterialShaderConstants() {
			const string ShaderNameStart = ResourceNamespace + "standard";
			const string ShaderNameStartWithEffects = ShaderNameStart + ShaderWithEffectsSuffix;
			const string AlphaModeVariantStart = "_alphamode=";
			const string OrmReflectanceVariantStart = "_ormreflectance=";
			const Flags LastFlag = Flags.Orm;
			const AlphaModeVariant FirstAlphaMode = AlphaModeVariant.AlphaOff;
			const AlphaModeVariant LastAlphaMode = AlphaModeVariant.AlphaOnBlended;
			const OrmReflectanceVariant FirstOrmReflectance = OrmReflectanceVariant.Off;
			const OrmReflectanceVariant LastOrmReflectance = OrmReflectanceVariant.On;
			
			_resourceNameMap = new();

			Span<char> stringBuildSpace = stackalloc char[1000];

			for (var flag = (Flags) 0; flag < (Flags) ((int) LastFlag << 1); ++flag) {
				for (var vAlphaMode = FirstAlphaMode; vAlphaMode <= LastAlphaMode; ++vAlphaMode) {
					for (var vOrmReflectance = FirstOrmReflectance; vOrmReflectance <= LastOrmReflectance; ++vOrmReflectance) {
						ShaderNameStartWithEffects.CopyTo(stringBuildSpace);
						var emptySpaceSpan = stringBuildSpace[ShaderNameStartWithEffects.Length..];

						Write(ref emptySpaceSpan, AlphaModeVariantStart);
						Write(
							ref emptySpaceSpan, 
							vAlphaMode switch {
								AlphaModeVariant.AlphaOff => "alphaoff",
								AlphaModeVariant.AlphaOn => "alphaon",
								AlphaModeVariant.AlphaOnBlended => "alphaonblended",
								_ => throw new ArgumentOutOfRangeException()
							}
						);

						Write(ref emptySpaceSpan, OrmReflectanceVariantStart);
						Write(
							ref emptySpaceSpan,
							vOrmReflectance switch {
								OrmReflectanceVariant.Off => "off",
								OrmReflectanceVariant.On => "on",
								_ => throw new ArgumentOutOfRangeException()
							}
						);

						WriteIfFlagExists(ref emptySpaceSpan, "_anisotropy", (int) flag, (int) Flags.Anisotropy);
						WriteIfFlagExists(ref emptySpaceSpan, "_clearcoat", (int) flag, (int) Flags.ClearCoat);
						WriteIfFlagExists(ref emptySpaceSpan, "_emissive", (int) flag, (int) Flags.Emissive);
						WriteIfFlagExists(ref emptySpaceSpan, "_normals", (int) flag, (int) Flags.Normals);
						WriteIfFlagExists(ref emptySpaceSpan, "_orm", (int) flag, (int) Flags.Orm);

						Write(ref emptySpaceSpan, ShaderResourceExtension);

						_resourceNameMap.Add(
							(true, flag, vAlphaMode, vOrmReflectance),
							new String(stringBuildSpace[..^emptySpaceSpan.Length])
						);
						stringBuildSpace[..ShaderNameStart.Length].CopyTo(stringBuildSpace[ShaderWithEffectsSuffix.Length..]);
						_resourceNameMap.Add(
							(false, flag, vAlphaMode, vOrmReflectance),
							new String(stringBuildSpace[ShaderWithEffectsSuffix.Length..^emptySpaceSpan.Length])
						);
					}
				}
			}
		}

		public string GetShaderResourceName(bool supportsEffects, Flags flags, AlphaModeVariant alphaMode, OrmReflectanceVariant ormReflectance) {
			return _resourceNameMap[(supportsEffects, flags, alphaMode, ormReflectance)];
		}

		public bool SupportsShadows { get; } = true;

		public ReadOnlySpan<byte> ParamColorMap => "color_map"u8;
		public ReadOnlySpan<byte> ParamNormalMap => "normal_map"u8;
		public ReadOnlySpan<byte> ParamOrmMap => "orm_map"u8;
		public ReadOnlySpan<byte> ParamEmissiveMap => "emissive_map"u8;
		public ReadOnlySpan<byte> ParamAnisotropyMap => "anisotropy_map"u8;
		public ReadOnlySpan<byte> ParamClearCoatMap => "clearcoat_map"u8;
		public ReadOnlySpan<byte> ParamEffectUvTransform => "uv_transform"u8;
		public ReadOnlySpan<byte> ParamEffectColorMapBlend => "color_map_blend"u8;
		public ReadOnlySpan<byte> ParamEffectColorMapBlendDistance => "color_map_blend_distance"u8;
		public ReadOnlySpan<byte> ParamEffectOrmMapBlend => "orm_map_blend"u8;
		public ReadOnlySpan<byte> ParamEffectOrmMapBlendDistance => "orm_map_blend_distance"u8;
		public ReadOnlySpan<byte> ParamEffectEmissiveMapBlend => "emissive_map_blend"u8;
		public ReadOnlySpan<byte> ParamEffectEmissiveMapBlendDistance => "emissive_map_blend_distance"u8;

		public bool HasEffectUvTransform { get; } = true;
		public bool HasEffectColorMap { get; } = true;
		public bool HasEffectEmissiveMap { get; } = true;
		public bool HasEffectAbsorptionTransmissionMap { get; } = false;
		public bool HasEffectOrmMap { get; } = true;
		public bool HasEffectOpacity { get; } = false;
		public ReadOnlySpan<byte> GetEffectUvTransformParamOrThrow() => ParamEffectUvTransform;
		public ReadOnlySpan<byte> GetEffectColorMapTexParamOrThrow() => ParamEffectColorMapBlend;
		public ReadOnlySpan<byte> GetEffectColorMapDistanceParamOrThrow() => ParamEffectColorMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectEmissiveMapTexParamOrThrow() => ParamEffectEmissiveMapBlend;
		public ReadOnlySpan<byte> GetEffectEmissiveMapDistanceParamOrThrow() => ParamEffectEmissiveMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapTexParamOrThrow() => ParamEffectOrmMapBlend;
		public ReadOnlySpan<byte> GetEffectOrmMapDistanceParamOrThrow() => ParamEffectOrmMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectOpacityParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
	}

	public static TransmissiveMaterialShaderConstants TransmissiveMaterialShader { get; } = new();
	public sealed class TransmissiveMaterialShaderConstants : IShaderPackageConstants {
		[Flags]
		public enum Flags {
			Anisotropy = 1 << 0,
			Emissive = 1 << 1,
			Normals = 1 << 2,
			Orm = 1 << 3,
		}
		
		public enum AlphaModeVariant {
			AlphaOff,
			AlphaOn,
			AlphaOnBlended
		}
		public enum RefractionQualityVariant {
			Low,
			High
		}
		public enum RefractionTypeVariant {
			Thin,
			Thick
		}

		readonly ArrayPoolBackedMap<(bool SupportsEffects, Flags Flags, AlphaModeVariant AlphaMode, RefractionQualityVariant RefractionQuality, RefractionTypeVariant RefractionType), string> _resourceNameMap;

		public TransmissiveMaterialShaderConstants() {
			const string ShaderNameStart = ResourceNamespace + "transmissive";
			const string ShaderNameStartWithEffects = ShaderNameStart + ShaderWithEffectsSuffix;
			const string AlphaModeVariantStart = "_alphamode=";
			const string RefractionQualityVariantStart = "_refractionquality=";
			const string RefractionTypeVariantStart = "_refractiontype=";
			const Flags LastFlag = Flags.Orm;
			const AlphaModeVariant FirstAlphaMode = AlphaModeVariant.AlphaOff;
			const AlphaModeVariant LastAlphaMode = AlphaModeVariant.AlphaOnBlended;
			const RefractionQualityVariant FirstRefractionQuality = RefractionQualityVariant.Low;
			const RefractionQualityVariant LastRefractionQuality = RefractionQualityVariant.High;
			const RefractionTypeVariant FirstRefractionType = RefractionTypeVariant.Thin;
			const RefractionTypeVariant LastRefractionType = RefractionTypeVariant.Thick;

			_resourceNameMap = new();

			Span<char> stringBuildSpace = stackalloc char[1000];

			for (var flag = (Flags) 0; flag < (Flags) ((int) LastFlag << 1); ++flag) {
				for (var vAlphaMode = FirstAlphaMode; vAlphaMode <= LastAlphaMode; ++vAlphaMode) {
					for (var vRefractionQuality = FirstRefractionQuality; vRefractionQuality <= LastRefractionQuality; ++vRefractionQuality) {
						for (var vRefractionType = FirstRefractionType; vRefractionType <= LastRefractionType; ++vRefractionType) {
							ShaderNameStartWithEffects.CopyTo(stringBuildSpace);
							var emptySpaceSpan = stringBuildSpace[ShaderNameStartWithEffects.Length..];

							Write(ref emptySpaceSpan, AlphaModeVariantStart);
							Write(
								ref emptySpaceSpan,
								vAlphaMode switch {
									AlphaModeVariant.AlphaOff => "alphaoff",
									AlphaModeVariant.AlphaOn => "alphaon",
									AlphaModeVariant.AlphaOnBlended => "alphaonblended",
									_ => throw new ArgumentOutOfRangeException()
								}
							);

							Write(ref emptySpaceSpan, RefractionQualityVariantStart);
							Write(
								ref emptySpaceSpan,
								vRefractionQuality switch {
									RefractionQualityVariant.Low => "low",
									RefractionQualityVariant.High => "high",
									_ => throw new ArgumentOutOfRangeException()
								}
							);

							Write(ref emptySpaceSpan, RefractionTypeVariantStart);
							Write(
								ref emptySpaceSpan,
								vRefractionType switch {
									RefractionTypeVariant.Thin => "thin",
									RefractionTypeVariant.Thick => "thick",
									_ => throw new ArgumentOutOfRangeException()
								}
							);

							WriteIfFlagExists(ref emptySpaceSpan, "_anisotropy", (int) flag, (int) Flags.Anisotropy);
							WriteIfFlagExists(ref emptySpaceSpan, "_emissive", (int) flag, (int) Flags.Emissive);
							WriteIfFlagExists(ref emptySpaceSpan, "_normals", (int) flag, (int) Flags.Normals);
							WriteIfFlagExists(ref emptySpaceSpan, "_orm", (int) flag, (int) Flags.Orm);

							Write(ref emptySpaceSpan, ShaderResourceExtension);

							_resourceNameMap.Add(
								(true, flag, vAlphaMode, vRefractionQuality, vRefractionType),
								new String(stringBuildSpace[..^emptySpaceSpan.Length])
							);
							stringBuildSpace[..ShaderNameStart.Length].CopyTo(stringBuildSpace[ShaderWithEffectsSuffix.Length..]);
							_resourceNameMap.Add(
								(false, flag, vAlphaMode, vRefractionQuality, vRefractionType),
								new String(stringBuildSpace[ShaderWithEffectsSuffix.Length..^emptySpaceSpan.Length])
							);
						}
					}
				}
			}
		}

		public string GetShaderResourceName(bool supportsEffects, Flags flags, AlphaModeVariant alphaMode, RefractionQualityVariant refractionQuality, RefractionTypeVariant refractionType) {
			return _resourceNameMap[(supportsEffects, flags, alphaMode, refractionQuality, refractionType)];
		}

		public bool SupportsShadows { get; } = true;

		public ReadOnlySpan<byte> ParamSurfaceThickness => "surface_thickness"u8;
		public ReadOnlySpan<byte> ParamColorMap => "color_map"u8;
		public ReadOnlySpan<byte> ParamAbsorptionTransmissionMap => "at_map"u8;
		public ReadOnlySpan<byte> ParamNormalMap => "normal_map"u8;
		public ReadOnlySpan<byte> ParamOrmMap => "orm_map"u8;
		public ReadOnlySpan<byte> ParamEmissiveMap => "emissive_map"u8;
		public ReadOnlySpan<byte> ParamAnisotropyMap => "anisotropy_map"u8;
		public ReadOnlySpan<byte> ParamEffectUvTransform => "uv_transform"u8;
		public ReadOnlySpan<byte> ParamEffectColorMapBlend => "color_map_blend"u8;
		public ReadOnlySpan<byte> ParamEffectColorMapBlendDistance => "color_map_blend_distance"u8;
		public ReadOnlySpan<byte> ParamEffectAbsorptionTransmissionMapBlend => "at_map_blend"u8;
		public ReadOnlySpan<byte> ParamEffectAbsorptionTransmissionMapBlendDistance => "at_map_blend_distance"u8;
		public ReadOnlySpan<byte> ParamEffectOrmMapBlend => "orm_map_blend"u8;
		public ReadOnlySpan<byte> ParamEffectOrmMapBlendDistance => "orm_map_blend_distance"u8;
		public ReadOnlySpan<byte> ParamEffectEmissiveMapBlend => "emissive_map_blend"u8;
		public ReadOnlySpan<byte> ParamEffectEmissiveMapBlendDistance => "emissive_map_blend_distance"u8;

		public bool HasEffectUvTransform { get; } = true;
		public bool HasEffectColorMap { get; } = true;
		public bool HasEffectEmissiveMap { get; } = true;
		public bool HasEffectAbsorptionTransmissionMap { get; } = true;
		public bool HasEffectOrmMap { get; } = true;
		public bool HasEffectOpacity { get; } = false;
		public ReadOnlySpan<byte> GetEffectUvTransformParamOrThrow() => ParamEffectUvTransform;
		public ReadOnlySpan<byte> GetEffectColorMapTexParamOrThrow() => ParamEffectColorMapBlend;
		public ReadOnlySpan<byte> GetEffectColorMapDistanceParamOrThrow() => ParamEffectColorMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectEmissiveMapTexParamOrThrow() => ParamEffectEmissiveMapBlend;
		public ReadOnlySpan<byte> GetEffectEmissiveMapDistanceParamOrThrow() => ParamEffectEmissiveMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapTexParamOrThrow() => ParamEffectAbsorptionTransmissionMapBlend;
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapDistanceParamOrThrow() => ParamEffectAbsorptionTransmissionMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectOrmMapTexParamOrThrow() => ParamEffectOrmMapBlend;
		public ReadOnlySpan<byte> GetEffectOrmMapDistanceParamOrThrow() => ParamEffectOrmMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectOpacityParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
	}

	public static LightingIgnoringMaterialShaderConstants LightingIgnoringMaterialShader { get; } = new();
	public sealed class LightingIgnoringMaterialShaderConstants : IShaderPackageConstants {
		public enum AlphaModeVariant {
			AlphaOff,
			AlphaOn
		}

		readonly ArrayPoolBackedMap<(bool SupportsEffects, AlphaModeVariant AlphaMode), string> _resourceNameMap;

		public LightingIgnoringMaterialShaderConstants() {
			const string ShaderNameStart = ResourceNamespace + "lighting_ignoring";
			const string ShaderNameStartWithEffects = ShaderNameStart + ShaderWithEffectsSuffix;
			const string AlphaModeVariantStart = "_alphamode=";
			const AlphaModeVariant FirstAlphaMode = AlphaModeVariant.AlphaOff;
			const AlphaModeVariant LastAlphaMode = AlphaModeVariant.AlphaOn;

			_resourceNameMap = new();

			Span<char> stringBuildSpace = stackalloc char[1000];

			for (var vAlphaMode = FirstAlphaMode; vAlphaMode <= LastAlphaMode; ++vAlphaMode) {
				ShaderNameStartWithEffects.CopyTo(stringBuildSpace);
				var emptySpaceSpan = stringBuildSpace[ShaderNameStartWithEffects.Length..];

				Write(ref emptySpaceSpan, AlphaModeVariantStart);
				Write(
					ref emptySpaceSpan,
					vAlphaMode switch {
						AlphaModeVariant.AlphaOff => "alphaoff",
						AlphaModeVariant.AlphaOn => "alphaon",
						_ => throw new ArgumentOutOfRangeException()
					}
				);

				Write(ref emptySpaceSpan, ShaderResourceExtension);

				_resourceNameMap.Add(
					(true, vAlphaMode),
					new String(stringBuildSpace[..^emptySpaceSpan.Length])
				);
				stringBuildSpace[..ShaderNameStart.Length].CopyTo(stringBuildSpace[ShaderWithEffectsSuffix.Length..]);
				_resourceNameMap.Add(
					(false, vAlphaMode),
					new String(stringBuildSpace[ShaderWithEffectsSuffix.Length..^emptySpaceSpan.Length])
				);
			}
		}

		public string GetShaderResourceName(bool supportsEffects, AlphaModeVariant alphaMode) {
			return _resourceNameMap[(supportsEffects, alphaMode)];
		}

		public bool SupportsShadows { get; } = false;

		public ReadOnlySpan<byte> ParamColorMap => "color_map"u8;
		public ReadOnlySpan<byte> ParamEffectUvTransform => "uv_transform"u8;
		public ReadOnlySpan<byte> ParamEffectColorMapBlend => "color_map_blend"u8;
		public ReadOnlySpan<byte> ParamEffectColorMapBlendDistance => "color_map_blend_distance"u8;

		public bool HasEffectUvTransform { get; } = true;
		public bool HasEffectColorMap { get; } = true;
		public bool HasEffectEmissiveMap { get; } = true;
		public bool HasEffectAbsorptionTransmissionMap { get; } = false;
		public bool HasEffectOrmMap { get; } = false;
		public bool HasEffectOpacity { get; } = false;
		public ReadOnlySpan<byte> GetEffectUvTransformParamOrThrow() => ParamEffectUvTransform;
		public ReadOnlySpan<byte> GetEffectColorMapTexParamOrThrow() => ParamEffectColorMapBlend;
		public ReadOnlySpan<byte> GetEffectColorMapDistanceParamOrThrow() => ParamEffectColorMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectEmissiveMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectEmissiveMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOpacityParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
	}
	
	public static ColorKeyedMaterialShaderConstants ColorKeyedMaterialShader { get; } = new();
	public sealed class ColorKeyedMaterialShaderConstants : IShaderPackageConstants {
		public enum AlphaModeVariant {
			AlphaOff,
			AlphaOn
		}

		readonly ArrayPoolBackedMap<AlphaModeVariant, string> _resourceNameMap;

		public ColorKeyedMaterialShaderConstants() {
			const string ShaderNameStart = ResourceNamespace + "keyed";
			const string AlphaModeVariantStart = "_alphamode=";
			const AlphaModeVariant FirstAlphaMode = AlphaModeVariant.AlphaOff;
			const AlphaModeVariant LastAlphaMode = AlphaModeVariant.AlphaOn;

			_resourceNameMap = new();

			Span<char> stringBuildSpace = stackalloc char[1000];

			for (var vAlphaMode = FirstAlphaMode; vAlphaMode <= LastAlphaMode; ++vAlphaMode) {
				ShaderNameStart.CopyTo(stringBuildSpace);
				var emptySpaceSpan = stringBuildSpace[ShaderNameStart.Length..];

				Write(ref emptySpaceSpan, AlphaModeVariantStart);
				Write(
					ref emptySpaceSpan,
					vAlphaMode switch {
						AlphaModeVariant.AlphaOff => "alphaoff",
						AlphaModeVariant.AlphaOn => "alphaon",
						_ => throw new ArgumentOutOfRangeException()
					}
				);

				Write(ref emptySpaceSpan, ShaderResourceExtension);

				_resourceNameMap.Add(
					vAlphaMode,
					new String(stringBuildSpace[..^emptySpaceSpan.Length])
				);
			}
		}

		public string GetShaderResourceName(AlphaModeVariant alphaMode) {
			return _resourceNameMap[alphaMode];
		}

		public bool SupportsShadows { get; } = false;

		public ReadOnlySpan<byte> ParamKeyMap => "key_map"u8;
		public ReadOnlySpan<byte> ParamXChannelColor => "x_channel_color"u8;
		public ReadOnlySpan<byte> ParamYChannelColor => "y_channel_color"u8;
		public ReadOnlySpan<byte> ParamZChannelColor => "z_channel_color"u8;
		public ReadOnlySpan<byte> ParamWChannelColor => "w_channel_color"u8;

		public bool HasEffectUvTransform { get; } = false;
		public bool HasEffectColorMap { get; } = false;
		public bool HasEffectEmissiveMap { get; } = false;
		public bool HasEffectAbsorptionTransmissionMap { get; } = false;
		public bool HasEffectOrmMap { get; } = false;
		public bool HasEffectOpacity { get; } = false;
		public ReadOnlySpan<byte> GetEffectUvTransformParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectColorMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectColorMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectEmissiveMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectEmissiveMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOpacityParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
	}

	public static DefaultMaterialShaderConstants DefaultMaterialShader { get; } = new();
	public sealed class DefaultMaterialShaderConstants : IShaderPackageConstants {
		public enum ShadingModeVariant {
			PlainOpaque,
			Plain3DOpaque,
			Plain,
			Plain3D,
			Wireframe
		}

		readonly ArrayPoolBackedMap<ShadingModeVariant, string> _resourceNameMap;
		
		public DefaultMaterialShaderConstants() {
			const string ShaderNameStart = ResourceNamespace + "default";
			const string ShadingModeVariantStart = "_shadingmode=";
			const ShadingModeVariant FirstShadingMode = ShadingModeVariant.PlainOpaque;
			const ShadingModeVariant LastShadingMode = ShadingModeVariant.Wireframe;
			
			_resourceNameMap = new();

			Span<char> stringBuildSpace = stackalloc char[1000];

			for (var vShadingMode = FirstShadingMode; vShadingMode <= LastShadingMode; ++vShadingMode) {
				ShaderNameStart.CopyTo(stringBuildSpace);
				var emptySpaceSpan = stringBuildSpace[ShaderNameStart.Length..];
				
				Write(ref emptySpaceSpan, ShadingModeVariantStart);
				Write(
					ref emptySpaceSpan,
					vShadingMode switch {
						ShadingModeVariant.PlainOpaque => "plainopaque",
						ShadingModeVariant.Plain3DOpaque => "plain3dopaque",
						ShadingModeVariant.Plain => "plain",
						ShadingModeVariant.Plain3D => "plain3d",
						ShadingModeVariant.Wireframe => "wireframe",
						_ => throw new ArgumentOutOfRangeException()
					}
				);

				Write(ref emptySpaceSpan, ShaderResourceExtension);

				_resourceNameMap.Add(
					vShadingMode,
					new String(stringBuildSpace[..^emptySpaceSpan.Length])
				);
			}
		}

		public string GetShaderResourceName(ShadingModeVariant shadingMode) {
			return _resourceNameMap[shadingMode];
		}

		public bool SupportsShadows { get; } = false;

		public ReadOnlySpan<byte> ParamBaseColor => "base_color"u8;

		public bool HasEffectUvTransform { get; } = false;
		public bool HasEffectColorMap { get; } = false;
		public bool HasEffectEmissiveMap { get; } = false;
		public bool HasEffectAbsorptionTransmissionMap { get; } = false;
		public bool HasEffectOrmMap { get; } = false;
		public bool HasEffectOpacity { get; } = false;
		public ReadOnlySpan<byte> GetEffectUvTransformParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectColorMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectColorMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectEmissiveMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectEmissiveMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOpacityParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
	}

	public static TextMaterialShaderConstants TextMaterialShader { get; } = new();
	public sealed class TextMaterialShaderConstants : IShaderPackageConstants {
		public string ShaderResourceName { get; } = ResourceNamespace + "text" + ShaderResourceExtension;

		public bool SupportsShadows { get; } = false;

		public ReadOnlySpan<byte> ParamSdfMap => "sdf_map"u8;
		public ReadOnlySpan<byte> ParamTextColor => "text_color"u8;
		public ReadOnlySpan<byte> ParamOutlineColor => "outline_color"u8;
		public ReadOnlySpan<byte> ParamBackgroundColor => "background_color"u8;
		public ReadOnlySpan<byte> ParamOutlineThickness => "outline_thickness"u8;

		public bool HasEffectUvTransform { get; } = false;
		public bool HasEffectColorMap { get; } = false;
		public bool HasEffectEmissiveMap { get; } = false;
		public bool HasEffectAbsorptionTransmissionMap { get; } = false;
		public bool HasEffectOrmMap { get; } = false;
		public bool HasEffectOpacity { get; } = false;
		public ReadOnlySpan<byte> GetEffectUvTransformParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectColorMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectColorMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectEmissiveMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectEmissiveMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOpacityParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
	}

	public static CanvasMaterialShaderConstants CanvasMaterialShader { get; } = new();
	public sealed class CanvasMaterialShaderConstants : IShaderPackageConstants {
		public string ShaderResourceName { get; } = ResourceNamespace + "canvas" + ShaderResourceExtension;

		public bool SupportsShadows { get; } = false;

		public ReadOnlySpan<byte> ParamColorMap => "color_map"u8;
		public ReadOnlySpan<byte> ParamEffectUvTransform => "uv_transform"u8;
		public ReadOnlySpan<byte> ParamEffectColorMapBlend => "color_map_blend"u8;
		public ReadOnlySpan<byte> ParamEffectColorMapBlendDistance => "color_map_blend_distance"u8;
		public ReadOnlySpan<byte> ParamEffectOpacity => "opacity"u8;

		public bool HasEffectUvTransform { get; } = true;
		public bool HasEffectColorMap { get; } = true;
		public bool HasEffectEmissiveMap { get; } = false;
		public bool HasEffectAbsorptionTransmissionMap { get; } = false;
		public bool HasEffectOrmMap { get; } = false;
		public bool HasEffectOpacity { get; } = true;
		public ReadOnlySpan<byte> GetEffectUvTransformParamOrThrow() => ParamEffectUvTransform;
		public ReadOnlySpan<byte> GetEffectColorMapTexParamOrThrow() => ParamEffectColorMapBlend;
		public ReadOnlySpan<byte> GetEffectColorMapDistanceParamOrThrow() => ParamEffectColorMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectEmissiveMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectEmissiveMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOpacityParamOrThrow() => ParamEffectOpacity;
	}

	public static ImGuiMaterialShaderConstants ImGuiMaterialShader { get; } = new();
	public sealed class ImGuiMaterialShaderConstants : IShaderPackageConstants {
		public string ShaderResourceName { get; } = ResourceNamespace + "imgui" + ShaderResourceExtension;

		public bool SupportsShadows { get; } = false;

		public ReadOnlySpan<byte> ParamColorMap => "color_map"u8;

		public bool HasEffectUvTransform { get; } = false;
		public bool HasEffectColorMap { get; } = false;
		public bool HasEffectEmissiveMap { get; } = false;
		public bool HasEffectAbsorptionTransmissionMap { get; } = false;
		public bool HasEffectOrmMap { get; } = false;
		public bool HasEffectOpacity { get; } = false;
		public ReadOnlySpan<byte> GetEffectUvTransformParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectColorMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectColorMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectEmissiveMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectEmissiveMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOpacityParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
	}

	static void Write(ref Span<char> dest, string str) {
		str.CopyTo(dest);
		dest = dest[str.Length..];
	}
	static void WriteIfFlagExists(ref Span<char> dest, string str, int flags, int flagToCheck) {
		if ((flags & flagToCheck) == flagToCheck) Write(ref dest, str);
	}
}