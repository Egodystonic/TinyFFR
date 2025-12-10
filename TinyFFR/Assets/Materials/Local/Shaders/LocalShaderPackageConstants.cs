// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;

namespace Egodystonic.TinyFFR.Assets.Materials.Local;

interface IShaderPackageConstants {
	bool HasEffectUvTransform { get; }
	bool HasEffectColorMap { get; }
	bool HasEffectEmissiveMap { get; }
	bool HasEffectAbsorptionTransmissionMap { get; }
	bool HasEffectOrmMap { get; }

	ReadOnlySpan<byte> GetEffectUvTransformParamOrThrow();
	ReadOnlySpan<byte> GetEffectColorMapTexParamOrThrow();
	ReadOnlySpan<byte> GetEffectEmissiveMapTexParamOrThrow();
	ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapTexParamOrThrow();
	ReadOnlySpan<byte> GetEffectOrmMapTexParamOrThrow();
	ReadOnlySpan<byte> GetEffectColorMapDistanceParamOrThrow();
	ReadOnlySpan<byte> GetEffectEmissiveMapDistanceParamOrThrow();
	ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapDistanceParamOrThrow();
	ReadOnlySpan<byte> GetEffectOrmMapDistanceParamOrThrow();
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
		public ReadOnlySpan<byte> GetEffectUvTransformParamOrThrow() => ParamEffectUvTransform;
		public ReadOnlySpan<byte> GetEffectColorMapTexParamOrThrow() => ParamEffectColorMapBlend;
		public ReadOnlySpan<byte> GetEffectColorMapDistanceParamOrThrow() => ParamEffectColorMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectEmissiveMapTexParamOrThrow() => ParamEffectEmissiveMapBlend;
		public ReadOnlySpan<byte> GetEffectEmissiveMapDistanceParamOrThrow() => ParamEffectEmissiveMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapTexParamOrThrow() => ParamEffectOrmMapBlend;
		public ReadOnlySpan<byte> GetEffectOrmMapDistanceParamOrThrow() => ParamEffectOrmMapBlendDistance;
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
		public ReadOnlySpan<byte> GetEffectUvTransformParamOrThrow() => ParamEffectUvTransform;
		public ReadOnlySpan<byte> GetEffectColorMapTexParamOrThrow() => ParamEffectColorMapBlend;
		public ReadOnlySpan<byte> GetEffectColorMapDistanceParamOrThrow() => ParamEffectColorMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectEmissiveMapTexParamOrThrow() => ParamEffectEmissiveMapBlend;
		public ReadOnlySpan<byte> GetEffectEmissiveMapDistanceParamOrThrow() => ParamEffectEmissiveMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapTexParamOrThrow() => ParamAbsorptionTransmissionMap;
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapDistanceParamOrThrow() => ParamEffectAbsorptionTransmissionMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectOrmMapTexParamOrThrow() => ParamEffectOrmMapBlend;
		public ReadOnlySpan<byte> GetEffectOrmMapDistanceParamOrThrow() => ParamEffectOrmMapBlendDistance;
	}

	public static SimpleMaterialShaderConstants SimpleMaterialShader { get; } = new();
	public sealed class SimpleMaterialShaderConstants : IShaderPackageConstants {
		[Flags]
		public enum Flags {
			Emissive = 1 << 0,
		}
		public enum AlphaModeVariant {
			AlphaOff,
			AlphaOn
		}

		readonly ArrayPoolBackedMap<(bool SupportsEffects, Flags Flags, AlphaModeVariant AlphaMode), string> _resourceNameMap;

		public SimpleMaterialShaderConstants() {
			const string ShaderNameStart = ResourceNamespace + "simple";
			const string ShaderNameStartWithEffects = ShaderNameStart + ShaderWithEffectsSuffix;
			const string AlphaModeVariantStart = "_alphamode=";
			const Flags LastFlag = Flags.Emissive;
			const AlphaModeVariant FirstAlphaMode = AlphaModeVariant.AlphaOff;
			const AlphaModeVariant LastAlphaMode = AlphaModeVariant.AlphaOn;

			_resourceNameMap = new();

			Span<char> stringBuildSpace = stackalloc char[1000];

			for (var flag = (Flags) 0; flag < (Flags) ((int) LastFlag << 1); ++flag) {
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

					WriteIfFlagExists(ref emptySpaceSpan, "_emissive", (int) flag, (int) Flags.Emissive);

					Write(ref emptySpaceSpan, ShaderResourceExtension);

					_resourceNameMap.Add(
						(true, flag, vAlphaMode),
						new String(stringBuildSpace[..^emptySpaceSpan.Length])
					);
					stringBuildSpace[..ShaderNameStart.Length].CopyTo(stringBuildSpace[ShaderWithEffectsSuffix.Length..]);
					_resourceNameMap.Add(
						(false, flag, vAlphaMode),
						new String(stringBuildSpace[ShaderWithEffectsSuffix.Length..^emptySpaceSpan.Length])
					);
				}
			}
		}

		public string GetShaderResourceName(bool supportsEffects, Flags flags, AlphaModeVariant alphaMode) {
			return _resourceNameMap[(supportsEffects, flags, alphaMode)];
		}

		public ReadOnlySpan<byte> ParamColorMap => "color_map"u8;
		public ReadOnlySpan<byte> ParamEmissiveMap => "emissive_map"u8;
		public ReadOnlySpan<byte> ParamEffectUvTransform => "uv_transform"u8;
		public ReadOnlySpan<byte> ParamEffectColorMapBlend => "color_map_blend"u8;
		public ReadOnlySpan<byte> ParamEffectColorMapBlendDistance => "color_map_blend_distance"u8;
		public ReadOnlySpan<byte> ParamEffectEmissiveMapBlend => "emissive_map_blend"u8;
		public ReadOnlySpan<byte> ParamEffectEmissiveMapBlendDistance => "emissive_map_blend_distance"u8;

		public bool HasEffectUvTransform { get; } = true;
		public bool HasEffectColorMap { get; } = true;
		public bool HasEffectEmissiveMap { get; } = true;
		public bool HasEffectAbsorptionTransmissionMap { get; } = false;
		public bool HasEffectOrmMap { get; } = false;
		public ReadOnlySpan<byte> GetEffectUvTransformParamOrThrow() => ParamEffectUvTransform;
		public ReadOnlySpan<byte> GetEffectColorMapTexParamOrThrow() => ParamEffectColorMapBlend;
		public ReadOnlySpan<byte> GetEffectColorMapDistanceParamOrThrow() => ParamEffectColorMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectEmissiveMapTexParamOrThrow() => ParamEffectEmissiveMapBlend;
		public ReadOnlySpan<byte> GetEffectEmissiveMapDistanceParamOrThrow() => ParamEffectEmissiveMapBlendDistance;
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectAbsorptionTransmissionMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapTexParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
		public ReadOnlySpan<byte> GetEffectOrmMapDistanceParamOrThrow() => throw new InvalidOperationException("Bug in TinyFFR (or concurrency failure).");
	}

	static void Write(ref Span<char> dest, string str) {
		str.CopyTo(dest);
		dest = dest[str.Length..];
	}
	static void WriteIfFlagExists(ref Span<char> dest, string str, int flags, int flagToCheck) {
		if ((flags & flagToCheck) == flagToCheck) Write(ref dest, str);
	}
}