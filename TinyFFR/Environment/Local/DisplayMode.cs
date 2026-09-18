// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Local;

/// <summary>
/// A combination of resolution and refresh rate that a <see cref="Display"/> supports being set to.
/// </summary>
/// <remarks>
/// Displays advertise a fixed set of modes they can operate in (see <see cref="Display.SupportedDisplayModes"/>); a mode pairs the number of pixels
/// the display shows with how many times per second it redraws them.
/// </remarks>
/// <param name="Resolution">The number of pixels the display shows in this mode (<c>X</c> = width, <c>Y</c> = height).</param>
/// <param name="RefreshRateHz">How many times per second the display redraws its image in this mode, in hertz.</param>
public readonly record struct DisplayMode(XYPair<int> Resolution, int RefreshRateHz) {
	/// <summary>
	/// This mode's <see cref="Resolution"/> reduced to its smallest whole-number ratio (e.g. a 1920x1080 resolution gives <c>16:9</c>).
	/// </summary>
	public XYPair<int> AspectRatio => Resolution / (int) BigInteger.GreatestCommonDivisor(Resolution.X, Resolution.Y);

	/// <inheritdoc/>
	public override string ToString() {
		var ar = AspectRatio;
		return $"{Resolution.X:#} x {Resolution.Y:#} ({ar.X}:{ar.Y}) @ {RefreshRateHz}Hz";
	}
}