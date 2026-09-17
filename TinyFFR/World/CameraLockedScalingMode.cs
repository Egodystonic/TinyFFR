// Created on 2026-07-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// How an object that is locked to face the camera is sized on screen.
/// </summary>
/// <remarks>
/// <para>
/// By default such an object is sized in world units, so it shrinks as the camera moves away from it, exactly like any other object in the scene. The
/// <c>ViewportFractional</c> modes instead size it as a fraction of the rendered image, so it keeps the same apparent size on screen no matter how far away it is
/// or what resolution the image is rendered at — which is what you want for labels, markers and other user-interface-like elements.
/// </para>
/// <para>
/// In the fractional modes a scaling value of <c>1f</c> means "the full width (or height) of the screen/image/viewport", so <c>0.25f</c> means "a quarter of the screen/image/viewport".
/// </para>
/// <para>
/// More often than not, the option you most likely will want for fixed-size in-world UI elements is <see cref="ViewportFractionalFixedHeightPlusPreservedAspectRatio"/>.
/// </para>
/// </remarks>
public enum CameraLockedScalingMode {
	/// <summary>
	/// The object is sized in world units, so it appears smaller the further it is from the camera (like any other object in the scene).
	/// </summary>
	Standard,
	/// <summary>
	/// The object's width is a fraction of the screen/image/viewport's width; its height remains in world units.
	/// </summary>
	ViewportFractionalFixedWidth,
	/// <summary>
	/// The object's height is a fraction of the screen/image/viewport's height; its width remains in world units.
	/// </summary>
	ViewportFractionalFixedHeight,
	/// <summary>
	/// The object's width and height are fractions of the screen/image/viewport's width and height respectively.
	/// </summary>
	/// <remarks>
	/// Because each axis is tied to a different dimension of the screen/image/viewport, the object's shape stretches or squashes as the screen/image/viewport's aspect ratio changes. Use one of the
	/// <c>PreservedAspectRatio</c> modes if the object should keep its shape.
	/// </remarks>
	ViewportFractionalFixedWidthAndHeight,
	/// <summary>
	/// The object's width is a fraction of the screen/image/viewport's width, and its height is scaled to match so that the object keeps its shape.
	/// </summary>
	ViewportFractionalFixedWidthPlusPreservedAspectRatio,
	/// <summary>
	/// The object's height is a fraction of the screen/image/viewport's height, and its width is scaled to match so that the object keeps its shape.
	/// </summary>
	/// <remarks>
	/// This is the recommended mode for fixed-size in-world UI elements like text/symbols.
	/// </remarks>
	ViewportFractionalFixedHeightPlusPreservedAspectRatio
}
