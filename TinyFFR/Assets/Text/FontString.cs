// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

/// <summary>
/// A string of text that has been laid out with a particular font, ready to be rendered.
/// </summary>
/// <remarks>
/// <para>
/// Preparing a string works out where each character sits and builds the geometry to draw it, which is why it is done once and
/// kept rather than repeated every frame. Text that changes constantly — a frame counter, say — is better served by a small set
/// of prepared strings than by preparing a new one each frame.
/// </para>
/// <para>
/// Dispose a prepared string when it is no longer needed; this does not dispose the font.
/// </para>
/// </remarks>
public readonly record struct FontString : IResourceSpecialization<FontString, Font> {
	/// <summary>
	/// The font this string was prepared with.
	/// </summary>
	public Font Font { get; }
	internal nuint StringHandle { get; }
	
	/// <summary>
	/// How large this string is when drawn at the font's natural size, in world units (metres).
	/// </summary>
	/// <remarks>
	/// This is what a text object's layout scales from, and is also useful for sizing anything that has to fit around the text.
	/// </remarks>
	public XYPair<float> Size => Font.Implementation.GetStringSize(Font.GetHandleWithoutDisposeCheck(), StringHandle);

	/// <summary>
	/// Constructs a new <see cref="FontString"/> around an existing prepared string belonging to the given font.
	/// </summary>
	/// <remarks>
	/// Strings are normally obtained from <c>Font.CreateString</c> rather than constructed directly.
	/// </remarks>
	/// <param name="font">The font the string belongs to.</param>
	/// <param name="stringHandle">The identifier of the prepared string within that font.</param>
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

	/// <summary>
	/// Disposes this prepared string, releasing the geometry built for it.
	/// </summary>
	/// <remarks>
	/// The font itself is not disposed, and its other strings are unaffected.
	/// </remarks>
	public void Dispose() => Font.Implementation.DisposeString(Font.GetHandleWithoutDisposeCheck(), StringHandle);
}