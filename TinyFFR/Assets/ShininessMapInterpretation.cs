// Created on 2026-10-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Assets;

/// <summary>
/// Describes how a texture map stored in a model file's "shininess" slot should be interpreted.
/// </summary>
/// <remarks>
/// Older file formats such as FBX and OBJ predate physically-based materials and have no dedicated slot for a roughness map. Strictly,
/// their shininess slot holds a glossiness map (where brighter means shinier), but some exporters (most notably Blender's) use it to
/// store an unmodified roughness map instead (where brighter means rougher). Reading the map the wrong way round swaps glossy and
/// rough surfaces.
/// </remarks>
public enum ShininessMapInterpretation {
	/// <summary>
	/// Treats the map as a roughness map if the file reports that it was exported from Blender, and as a glossiness map otherwise.
	/// </summary>
	Auto,
	/// <summary>
	/// Always treats the map as a glossiness map, so brighter texels produce shinier surfaces.
	/// </summary>
	Glossiness,
	/// <summary>
	/// Always treats the map as a roughness map, so brighter texels produce rougher surfaces.
	/// </summary>
	/// <remarks>
	/// Choose this for files exported by Blender that do not record where they came from, such as Blender-exported OBJ files.
	/// </remarks>
	Roughness
}
