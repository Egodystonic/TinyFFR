// Created on 2026-09-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// Which point along an arrow mesh becomes the mesh's origin, i.e. the point that sits exactly where an object using the mesh is positioned, and that the object rotates and scales around.
/// </summary>
/// <remarks>
/// The origin is always on the arrow's tail-to-head axis; this enum only chooses how far along that axis it lies.
/// </remarks>
public enum ArrowMeshOrigin {
	/// <summary>
	/// The centre of the flat end of the arrow's stem.
	/// </summary>
	Tail,
	/// <summary>
	/// The pointed tip of the arrow's head.
	/// </summary>
	HeadTip,
	/// <summary>
	/// The point halfway between the tail and the tip of the head.
	/// </summary>
	Centre
}
