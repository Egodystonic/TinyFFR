// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

public readonly record struct FontString : IDisposable {
	public Font Font { get; }
	internal nuint StringHandle { get; }

	public FontString(Font font, UIntPtr stringHandle) {
		Font = font;
		StringHandle = stringHandle;
	}

	internal Mesh GetStringMesh() => Font.Implementation.GetStringMesh(Font.Handle, StringHandle);

	public void Dispose() => Font.Implementation.DisposeString(Font.Handle, StringHandle);
}