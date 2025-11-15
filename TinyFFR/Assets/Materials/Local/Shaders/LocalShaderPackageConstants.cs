// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using static Egodystonic.TinyFFR.Assets.Materials.Local.LocalShaderPackageConstants.StandardMaterialShaderConstants;

namespace Egodystonic.TinyFFR.Assets.Materials.Local;

#pragma warning disable CA1001 // Warning about the ArrayPoolBackMaps not being disposed; but we know they will live for the entire lifetime of the application
static class LocalShaderPackageConstants {
	const string ResourceNamespace = "Assets.Materials.Local.Shaders.CompiledObjects.";
	const string ShaderResourceExtension = ".filamat";

	public static ref readonly byte ParamRef(ReadOnlySpan<byte> param) => ref MemoryMarshal.GetReference(param);
	public static int ParamLen(ReadOnlySpan<byte> param) => param.Length;

	public static StandardMaterialShaderConstants StandardMaterialShader { get; } = new();
	public sealed class StandardMaterialShaderConstants {
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

		readonly ArrayPoolBackedMap<(Flags Flags, AlphaModeVariant AlphaMode, OrmReflectanceVariant OrmReflectance), string> _resourceNameMap;

		public StandardMaterialShaderConstants() {
			const string ShaderNameStart = ResourceNamespace + "standard";
			const string AlphaModeVariantStart = "_alphamode=";
			const string OrmReflectanceVariantStart = "_ormreflectance=";
			const Flags LastFlag = Flags.Orm;
			const AlphaModeVariant FirstAlphaMode = AlphaModeVariant.AlphaOff;
			const AlphaModeVariant LastAlphaMode = AlphaModeVariant.AlphaOnBlended;
			const OrmReflectanceVariant FirstOrmReflectance = OrmReflectanceVariant.Off;
			const OrmReflectanceVariant LastOrmReflectance = OrmReflectanceVariant.On;
			
			_resourceNameMap = new();

			Span<char> stringBuildSpace = stackalloc char[1000];
			ShaderNameStart.CopyTo(stringBuildSpace);

			for (var flag = (Flags) 0; flag <= (Flags) (((int) LastFlag << 1) - 1); ++flag) {
				for (var vAlphaMode = FirstAlphaMode; vAlphaMode <= LastAlphaMode; ++vAlphaMode) {
					for (var vOrmReflectance = FirstOrmReflectance; vOrmReflectance <= LastOrmReflectance; ++vOrmReflectance) {
						var emptySpaceSpan = stringBuildSpace[ShaderNameStart.Length..];

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
							(flag, vAlphaMode, vOrmReflectance),
							new String(stringBuildSpace[..^emptySpaceSpan.Length])
						);
					}
				}
			}
		}

		public string GetShaderResourceName(Flags flags, AlphaModeVariant alphaMode, OrmReflectanceVariant ormReflectance) {
			return _resourceNameMap[(flags, alphaMode, ormReflectance)];
		}

		public ReadOnlySpan<byte> ParamColorMap => "color_map"u8;
		public ReadOnlySpan<byte> ParamNormalMap => "normal_map"u8;
		public ReadOnlySpan<byte> ParamOrmMap => "orm_map"u8;
		public ReadOnlySpan<byte> ParamEmissiveMap => "emissive_map"u8;
		public ReadOnlySpan<byte> ParamAnisotropyMap => "anisotropy_map"u8;
		public ReadOnlySpan<byte> ParamClearCoatMap => "clearcoat_map"u8;
	}

	public static TransmissiveMaterialShaderConstants TransmissiveMaterialShader { get; } = new();
	public sealed class TransmissiveMaterialShaderConstants {
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
			Disabled,
			Low,
			High
		}
		public enum RefractionTypeVariant {
			Thin,
			Thick
		}

		readonly ArrayPoolBackedMap<(Flags Flags, AlphaModeVariant AlphaMode, RefractionQualityVariant RefractionQuality, RefractionTypeVariant RefractionType), string> _resourceNameMap;

		public TransmissiveMaterialShaderConstants() {
			const string ShaderNameStart = ResourceNamespace + "transmissive";
			const string AlphaModeVariantStart = "_alphamode=";
			const string RefractionQualityVariantStart = "_refractionquality=";
			const string RefractionTypeVariantStart = "_refractiontype=";
			const Flags LastFlag = Flags.Orm;
			const AlphaModeVariant FirstAlphaMode = AlphaModeVariant.AlphaOff;
			const AlphaModeVariant LastAlphaMode = AlphaModeVariant.AlphaOnBlended;
			const RefractionQualityVariant FirstRefractionQuality = RefractionQualityVariant.Disabled;
			const RefractionQualityVariant LastRefractionQuality = RefractionQualityVariant.High;
			const RefractionTypeVariant FirstRefractionType = RefractionTypeVariant.Thin;
			const RefractionTypeVariant LastRefractionType = RefractionTypeVariant.Thick;

			_resourceNameMap = new();

			Span<char> stringBuildSpace = stackalloc char[1000];
			ShaderNameStart.CopyTo(stringBuildSpace);

			for (var flag = (Flags) 0; flag <= (Flags) (((int) LastFlag << 1) - 1); ++flag) {
				for (var vAlphaMode = FirstAlphaMode; vAlphaMode <= LastAlphaMode; ++vAlphaMode) {
					for (var vRefractionQuality = FirstRefractionQuality; vRefractionQuality <= LastRefractionQuality; ++vRefractionQuality) {
						for (var vRefractionType = FirstRefractionType; vRefractionType <= LastRefractionType; ++vRefractionType) {
							var emptySpaceSpan = stringBuildSpace[ShaderNameStart.Length..];

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
									RefractionQualityVariant.Disabled => "disabled",
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
								(flag, vAlphaMode, vRefractionQuality, vRefractionType),
								new String(stringBuildSpace[..^emptySpaceSpan.Length])
							);
						}
					}
				}
			}
		}

		public string GetShaderResourceName(Flags flags, AlphaModeVariant alphaMode, RefractionQualityVariant refractionQuality, RefractionTypeVariant refractionType) {
			return _resourceNameMap[(flags, alphaMode, refractionQuality, refractionType)];
		}

		public ReadOnlySpan<byte> ParamSurfaceThickness => "surface_thickness"u8;
		public ReadOnlySpan<byte> ParamColorMap => "color_map"u8;
		public ReadOnlySpan<byte> ParamAbsorptionTransmissionMap => "at_map"u8;
		public ReadOnlySpan<byte> ParamNormalMap => "normal_map"u8;
		public ReadOnlySpan<byte> ParamOrmMap => "orm_map"u8;
		public ReadOnlySpan<byte> ParamEmissiveMap => "emissive_map"u8;
		public ReadOnlySpan<byte> ParamAnisotropyMap => "anisotropy_map"u8;
	}

	public static SimpleMaterialShaderConstants SimpleMaterialShader { get; } = new();
	public sealed class SimpleMaterialShaderConstants {
		[Flags]
		public enum Flags {
			Emissive = 1 << 0,
		}
		public enum AlphaModeVariant {
			AlphaOff,
			AlphaOn
		}

		readonly ArrayPoolBackedMap<(Flags Flags, AlphaModeVariant AlphaMode), string> _resourceNameMap;

		public SimpleMaterialShaderConstants() {
			const string ShaderNameStart = ResourceNamespace + "simple";
			const string AlphaModeVariantStart = "_alphamode=";
			const Flags LastFlag = Flags.Emissive;
			const AlphaModeVariant FirstAlphaMode = AlphaModeVariant.AlphaOff;
			const AlphaModeVariant LastAlphaMode = AlphaModeVariant.AlphaOn;

			_resourceNameMap = new();

			Span<char> stringBuildSpace = stackalloc char[1000];
			ShaderNameStart.CopyTo(stringBuildSpace);

			for (var flag = (Flags) 0; flag <= (Flags) (((int) LastFlag << 1) - 1); ++flag) {
				for (var vAlphaMode = FirstAlphaMode; vAlphaMode <= LastAlphaMode; ++vAlphaMode) {
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

					WriteIfFlagExists(ref emptySpaceSpan, "_emissive", (int) flag, (int) Flags.Emissive);

					Write(ref emptySpaceSpan, ShaderResourceExtension);

					_resourceNameMap.Add(
						(flag, vAlphaMode),
						new String(stringBuildSpace[..^emptySpaceSpan.Length])
					);
				}
			}
		}

		public string GetShaderResourceName(Flags flags, AlphaModeVariant alphaMode) {
			return _resourceNameMap[(flags, alphaMode)];
		}

		public ReadOnlySpan<byte> ParamColorMap => "color_map"u8;
		public ReadOnlySpan<byte> ParamEmissiveMap => "emissive_map"u8;
	}

	static void Write(ref Span<char> dest, string str) {
		str.CopyTo(dest);
		dest = dest[str.Length..];
	}
	static void WriteIfFlagExists(ref Span<char> dest, string str, int flags, int flagToCheck) {
		if ((flags & flagToCheck) == flagToCheck) Write(ref dest, str);
	}
}