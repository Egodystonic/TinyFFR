// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

public readonly record struct FontPen : IResourceSpecialization<FontPen, Font> {
	public Font Font { get; }
	internal nuint PenHandle { get; }

	public FontPen(Font font, UIntPtr penHandle) {
		Font = font;
		PenHandle = penHandle;
	}
	
	#region Specialization
	static IntPtr IResourceSpecialization<FontPen, Font>.SpecializationTypeIdentifier => typeof(FontPen).TypeHandle.Value;
	int IResourceSpecialization<FontPen, Font>.SpecializationDataLength => sizeof(ulong);
	static void IResourceSpecialization<FontPen, Font>.Smuggle(FontPen resource, Span<byte> specializationDataBuffer, out Font outBaseResource, out ResourceStub? additionalResourceRef) {
		additionalResourceRef = null;
		BinaryPrimitives.WriteUInt64LittleEndian(specializationDataBuffer, resource.PenHandle);
		outBaseResource = resource.Font;
	}
	static FontPen IResourceSpecialization<FontPen, Font>.DeSmuggle(Font baseResource, ReadOnlySpan<byte> specializationDataBuffer, ResourceStub? additionalResourceRef) {
		return new(baseResource, (nuint) BinaryPrimitives.ReadUInt64LittleEndian(specializationDataBuffer));	
	}
	#endregion

	internal Material GetPenMaterial() => Font.Implementation.GetPenMaterial(Font.GetHandleWithoutDisposeCheck(), PenHandle);

	public void Dispose() => Font.Implementation.DisposePen(Font.GetHandleWithoutDisposeCheck(), PenHandle);
}