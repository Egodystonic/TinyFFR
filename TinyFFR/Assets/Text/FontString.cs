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
	
	public XYPair<float> Size => Font.Implementation.GetStringSize(Font.GetHandleWithoutDisposeCheck(), StringHandle);

	public FontString(Font font, nuint stringHandle) {
		Font = font;
		StringHandle = stringHandle;
	}

	internal Mesh GetStringMesh() => Font.Implementation.GetStringMesh(Font.GetHandleWithoutDisposeCheck(), StringHandle);

	public void Dispose() => Font.Implementation.DisposeString(Font.GetHandleWithoutDisposeCheck(), StringHandle);
}