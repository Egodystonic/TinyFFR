// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

public readonly record struct FontString : IResourceSpecialization<FontString, Font> {
	public Font Font { get; }
	internal nuint StringHandle { get; }
	
	public XYPair<float> Size => Font.Implementation.GetStringSize(Font.GetHandleWithoutDisposeCheck(), StringHandle);

	public FontString(Font font, nuint stringHandle) {
		Font = font;
		StringHandle = stringHandle;
	}
	
	#region Specialization
	static IntPtr IResourceSpecialization<FontString, Font>.SpecializationTypeIdentifier => typeof(FontString).TypeHandle.Value;
	int IResourceSpecialization<FontString, Font>.SpecializationDataLength => sizeof(ulong);
	static void IResourceSpecialization<FontString, Font>.Smuggle(FontString resource, Span<byte> specializationDataBuffer, out Font outBaseResource, out ResourceStub? additionalResourceRef) {
		additionalResourceRef = null;
		BinaryPrimitives.WriteUInt64LittleEndian(specializationDataBuffer, resource.StringHandle);
		outBaseResource = resource.Font;
	}
	static FontString IResourceSpecialization<FontString, Font>.DeSmuggle(Font baseResource, ReadOnlySpan<byte> specializationDataBuffer, ResourceStub? additionalResourceRef) {
		return new(baseResource, (nuint) BinaryPrimitives.ReadUInt64LittleEndian(specializationDataBuffer));	
	}
	#endregion

	internal Mesh GetStringMesh() => Font.Implementation.GetStringMesh(Font.GetHandleWithoutDisposeCheck(), StringHandle);

	public void Dispose() => Font.Implementation.DisposeString(Font.GetHandleWithoutDisposeCheck(), StringHandle);
}