// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

public readonly record struct FontPen : IDisposable {
	public Font Font { get; }
	internal nuint PenHandle { get; }

	public FontPen(Font font, UIntPtr penHandle) {
		Font = font;
		PenHandle = penHandle;
	}

	internal Material GetPenMaterial() => Font.Implementation.GetPenMaterial(Font.Handle, PenHandle);

	public void Dispose() => Font.Implementation.Dispose(Font.Handle, PenHandle);
}