// Created on 2025-03-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Represents a scene object that has a colour, expressed as a hue, a saturation and a lightness (HSL).
/// </summary>
/// <remarks>
/// HSL describes a colour the way a person tends to think about one (which colour it is, how vivid it is, and how bright it is), which makes it more convenient
/// than red/green/blue for adjustments such as "make this a little more washed out" or "shift this towards green".
/// </remarks>
public interface IColoredSceneObject {
	/// <summary>
	/// Which colour this object is, as an angle around the colour wheel.
	/// </summary>
	/// <remarks>
	/// Any angle is accepted and wrapped to a full turn; see <see cref="ColorVect.RedHueAngle"/>, <see cref="ColorVect.GreenHueAngle"/> and
	/// <see cref="ColorVect.BlueHueAngle"/> for reference points.
	/// </remarks>
	Angle ColorHue { get; set; }
	/// <summary>
	/// How vivid this object's colour is, clamped to <c>0f &lt;= n &lt;= 1f</c>; <c>0f</c> is a shade of grey, <c>1f</c> is fully saturated.
	/// </summary>
	float ColorSaturation { get; set; }
	/// <summary>
	/// How bright this object's colour is, clamped to <c>0f &lt;= n &lt;= 1f</c>; <c>0f</c> is black, <c>1f</c> is white, with the most vivid colours at <c>0.5f</c>.
	/// </summary>
	float ColorLightness { get; set; }

	/// <summary>
	/// Rotates this object's <see cref="ColorHue"/> around the colour wheel by <paramref name="adjustment"/>.
	/// </summary>
	/// <param name="adjustment">How far around the colour wheel to shift this object's hue. Any angle is accepted and wrapped to a full turn.</param>
	void AdjustColorHueBy(Angle adjustment);
	/// <summary>
	/// Adds <paramref name="adjustment"/> to this object's <see cref="ColorSaturation"/>.
	/// </summary>
	/// <param name="adjustment">The amount to add. May be negative, to wash the colour out; the result is clamped to <c>0f &lt;= n &lt;= 1f</c>.</param>
	void AdjustColorSaturationBy(float adjustment);
	/// <summary>
	/// Adds <paramref name="adjustment"/> to this object's <see cref="ColorLightness"/>.
	/// </summary>
	/// <param name="adjustment">The amount to add. May be negative, to darken the colour; the result is clamped to <c>0f &lt;= n &lt;= 1f</c>.</param>
	void AdjustColorLightnessBy(float adjustment);
}