// Created on 2026-10-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Text;

/// <summary>
/// Provides the implementation behind <see cref="FontPen"/>.
/// </summary>
public interface IFontPenImplProvider : IDisposableResourceImplProvider<FontPen> {
	/// <summary>
	/// Invoked via <see cref="FontPen.Font"/>.
	/// </summary>
	Font GetFont(ResourceHandle<FontPen> handle);
	/// <summary>
	/// Invoked internally to obtain the material a <see cref="FontPen"/> draws with.
	/// </summary>
	Material GetMaterial(ResourceHandle<FontPen> handle);
}
