// Created on 2026-04-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// How heavily a camera controller smooths its movement towards the values you set on it.
/// </summary>
/// <remarks>
/// Camera controllers do not snap straight to the values you give them; they ease towards them over the following frames, which makes the camera feel like a
/// physical object rather than something teleporting around. This enum selects how much easing is applied. Each value corresponds to a "half-life" — the time the
/// camera takes to cover half the remaining distance to its target — so stronger smoothing looks more fluid but adds more lag between setting a value and the camera
/// actually getting there. The exact half-life each value maps to varies per controller and per property; use the controller's <c>SetCustom...SmoothingStrength</c>
/// methods to specify one directly instead.
/// </remarks>
public enum SmoothingStrength {
	/// <summary>
	/// No smoothing at all; the camera moves to each value the moment it is set.
	/// </summary>
	None = 0,
	/// <summary>
	/// The least amount of smoothing that still takes the edge off sudden movements.
	/// </summary>
	VeryMild,
	/// <summary>
	/// A small amount of smoothing.
	/// </summary>
	Mild,
	/// <summary>
	/// A middling amount of smoothing.
	/// </summary>
	Moderate,
	/// <summary>
	/// A large amount of smoothing.
	/// </summary>
	Strong,
	/// <summary>
	/// The most smoothing available; fluid, but with a noticeable lag behind the values you set.
	/// </summary>
	VeryStrong
}