// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

/// <summary>
/// A particular combination of colours for drawing text with: Fill, outline, background.
/// </summary>
/// <remarks>
/// <para>
/// A pen is created from a font and holds the material that text drawn with it uses, so one font can serve any number of
/// differently-coloured pens without being loaded again.
/// </para>
/// <para>
/// Dispose a pen when it is no longer needed; this does not dispose the font.
/// </para>
/// </remarks>
public readonly record struct FontPen : IResourceSpecialization<FontPen, Font> {
	/// <summary>
	/// The font this pen draws with.
	/// </summary>
	public Font Font { get; }
	internal nuint PenHandle { get; }

	/// <summary>
	/// Constructs a new <see cref="FontPen"/> around an existing pen belonging to the given font.
	/// </summary>
	/// <remarks>
	/// Pens are normally obtained from <c>Font.CreatePen</c> rather than constructed directly.
	/// </remarks>
	/// <param name="font">The font the pen belongs to.</param>
	/// <param name="penHandle">The identifier of the pen within that font.</param>
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

	/// <summary>
	/// Disposes this pen, releasing the material it draws with.
	/// </summary>
	/// <remarks>
	/// The font itself is not disposed, and its other pens are unaffected.
	/// </remarks>
	public void Dispose() => Font.Implementation.DisposePen(Font.GetHandleWithoutDisposeCheck(), PenHandle);
}