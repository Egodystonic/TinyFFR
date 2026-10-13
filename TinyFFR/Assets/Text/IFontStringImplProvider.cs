// Created on 2026-10-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Text;

/// <summary>
/// Provides the implementation behind <see cref="FontString"/>.
/// </summary>
public interface IFontStringImplProvider : IDisposableResourceImplProvider<FontString> {
	/// <summary>
	/// Invoked via <see cref="FontString.Font"/>.
	/// </summary>
	Font GetFont(ResourceHandle<FontString> handle);
	/// <summary>
	/// Invoked internally to obtain the geometry a <see cref="FontString"/> is drawn from.
	/// </summary>
	Mesh GetMesh(ResourceHandle<FontString> handle);
	/// <summary>
	/// Invoked via <see cref="FontString.Size"/>.
	/// </summary>
	XYPair<float> GetSize(ResourceHandle<FontString> handle);
}
