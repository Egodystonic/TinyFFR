// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// How an object that is locked to face the camera decides which way to turn.
/// </summary>
/// <remarks>
/// Some objects (text and flat quads, typically) can be kept permanently turned towards the camera so that they are always legible, a technique usually called
/// "billboarding". The two styles here differ only for objects away from the centre of the screen, where "towards the camera" and "square-on to the screen" are not
/// quite the same direction.
/// </remarks>
public enum CameraLockStyle {
	/// <summary>
	/// Each object turns to point directly at the camera's position, like a crowd all looking at one person.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Objects near the edges of the screen are therefore turned slightly differently from those in the middle, which looks correct for objects distributed through
	/// a scene but can make flat text towards the edge of the screen appear subtly skewed.
	/// </para>
	/// <para>
	/// A note on performance: Compared to <see cref="FaceCameraPlane"/> this choice has a considerably larger performance impact.
	/// </para>
	/// </remarks>
	FaceCameraPosition,
	/// <summary>
	/// Every object turns to sit square-on to the screen, all sharing the camera's orientation rather than aiming at its position.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Because every object ends up parallel to the screen, flat content such as text stays perfectly undistorted wherever it appears; this is usually the better
	/// choice for user-interface-like elements.
	/// </para>
	/// <para>
	/// A note on performance: Compared to <see cref="FaceCameraPosition"/> this choice has a much lighter performance impact.
	/// </para>
	/// </remarks>
	FaceCameraPlane
}
