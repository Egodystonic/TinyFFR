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
	const string ResourceNamespace = "Egodystonic.TinyFFR.Assets.Materials.Local.Shaders.";

	public static (FixedByteBufferPool.FixedByteBuffer Buffer, int SizeBytes) OpenResource(FixedByteBufferPool pool, string resourceName)	{
		using var stream = typeof(LocalShaderPackageConstants).Assembly.GetManifestResourceStream(resourceName)
						?? throw new InvalidOperationException($"Resource '{resourceName}' not found in assembly.");

		var sizeBytes = checked((int) stream.Length);
		var result = (Buffer: pool.Rent(sizeBytes), SizeBytes: sizeBytes);
		stream.ReadExactly(result.Buffer.AsByteSpan[..result.SizeBytes]);
		return result;
	}
	public static ref readonly byte ParamRef(ReadOnlySpan<byte> param) => ref MemoryMarshal.GetReference(param);
	public static int ParamLen(ReadOnlySpan<byte> param) => param.Length;

	public static StandardPbrShaderConstants StandardPbrShader { get; } = new();
	public sealed class StandardPbrShaderConstants {
		public string ResourceName { get; } = ResourceNamespace + "standard_pbr.filamat";

		public ReadOnlySpan<byte> ParamAlbedo => "albedo"u8;
	}
}