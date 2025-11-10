// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Buffers;
using System.Globalization;
using System.IO;
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

namespace Egodystonic.TinyFFR.Assets.Materials.Local;

static class LocalShaderPackageConstants {
	const string ResourceNamespace = "Assets.Materials.Local.Shaders.CompiledObjects.";

	public static ref readonly byte ParamRef(ReadOnlySpan<byte> param) => ref MemoryMarshal.GetReference(param);
	public static int ParamLen(ReadOnlySpan<byte> param) => param.Length;

	#region Opaque Material
	public static OpaqueMaterialShaderConstants OpaqueMaterialShader { get; } = new();
	public sealed class OpaqueMaterialShaderConstants {
		public string ResourceName { get; } = ResourceNamespace + "opaque.filamat";

		public ReadOnlySpan<byte> ParamColorMap => "color_map"u8;
		public ReadOnlySpan<byte> ParamNormalMap => "normal_map"u8;
		public ReadOnlySpan<byte> ParamOrmMap => "orm_map"u8;
	}
	#endregion

	#region Alpha-Aware [Mask] Material
	public interface IAlphaAwareMaterialShader {
		public string ResourceName { get; }

		public ReadOnlySpan<byte> ParamColorMap { get; }
		public ReadOnlySpan<byte> ParamNormalMap { get; }
		public ReadOnlySpan<byte> ParamOrmMap { get; }
	}

	public static AlphaAwareMaterialShaderConstants AlphaAwareMaterialShader { get; } = new();
	public sealed class AlphaAwareMaterialShaderConstants : IAlphaAwareMaterialShader {
		public string ResourceName { get; } = ResourceNamespace + "alpha_aware.filamat";

		public ReadOnlySpan<byte> ParamColorMap => "color_map"u8;
		public ReadOnlySpan<byte> ParamNormalMap => "normal_map"u8;
		public ReadOnlySpan<byte> ParamOrmMap => "orm_map"u8;
	}

	public static AlphaAwareMaskMaterialShaderConstants AlphaAwareMaskMaterialShader { get; } = new();
	public sealed class AlphaAwareMaskMaterialShaderConstants : IAlphaAwareMaterialShader {
		public string ResourceName { get; } = ResourceNamespace + "alpha_aware_mask.filamat";

		public ReadOnlySpan<byte> ParamColorMap => "color_map"u8;
		public ReadOnlySpan<byte> ParamNormalMap => "normal_map"u8;
		public ReadOnlySpan<byte> ParamOrmMap => "orm_map"u8;
	}
	#endregion
}