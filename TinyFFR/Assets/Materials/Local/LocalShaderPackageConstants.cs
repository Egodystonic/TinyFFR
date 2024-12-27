// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Globalization;
using System.Resources;
using System.Security;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials.Textures;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Materials.Local;

static class LocalShaderPackageConstants {
	public sealed class StandardPbrShaderConstants {
		public string ResourceName { get; } = "standard_pbr_shader.mat";
		
		public ReadOnlySpan<byte> ParamAlbedo => "albedo"u8;
	}

	public static StandardPbrShaderConstants StandardPbrShader { get; } = new();

	public static ref readonly byte ParamRef(ReadOnlySpan<byte> param) => ref MemoryMarshal.GetReference(param);
	public static int ParamLen(ReadOnlySpan<byte> param) => param.Length;
}