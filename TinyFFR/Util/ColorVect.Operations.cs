// Created on 2024-10-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;

namespace Egodystonic.TinyFFR;

partial struct ColorVect {
	public ColorVect Normalized => new(Single.Clamp(Red, 0f, 1f), Single.Clamp(Green, 0f, 1f), Single.Clamp(Blue, 0f, 1f), Single.Clamp(Alpha, 0f, 1f));

	public ColorVect WithHue(Angle newHue) => FromHueSaturationLightness(newHue, Saturation, Lightness);
	public ColorVect WithSaturation(float newSaturation) => FromHueSaturationLightness(Hue, newSaturation, Lightness);
	public ColorVect WithLightness(float newLightness) => FromHueSaturationLightness(Hue, Saturation, newLightness);
}