// Created on 2026-01-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// How a camera flattens the three-dimensional scene on to a two-dimensional image.
/// </summary>
public enum CameraProjectionType {
	/// <summary>
	/// Objects further from the camera appear smaller, and parallel lines converge as they recede, as they do in a photograph or to the human eye. This is the typical, default style for 3D rendering.
	/// </summary>
	Perspective,
	/// <summary>
	/// Objects appear the same size no matter how far away they are, and parallel lines stay parallel.
	/// This is the projection used for technical and architectural drawings, isometric games, and map-like or diagrammatic views, where it matters that two objects
	/// of equal size measure the same on screen regardless of their distance.
	/// </summary>
	Orthographic
}