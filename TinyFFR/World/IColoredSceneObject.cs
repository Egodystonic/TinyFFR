// Created on 2025-03-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.World;

public interface IColoredSceneObject {
	Angle ColorHue { get; set; }
	float ColorSaturation { get; set; }
	float ColorLightness { get; set; }
	
	void AdjustColorHueBy(Angle adjustment);
	void AdjustColorSaturationBy(float adjustment);
	void AdjustColorLightnessBy(float adjustment);
}