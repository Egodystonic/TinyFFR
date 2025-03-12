// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.World;

interface ILight : IDisposableResource, IPositionedSceneObject, IColoredSceneObject {
	ColorVect Color { get; set; }
	float Brightness { get; set; }

	void AdjustBrightnessBy(float adjustment);
	void ScaleBrightnessBy(float scalar);
}
interface ILight<out TSelf> : ILight, IDisposableResource<Light, ILightImplProvider> where TSelf : ILight {
	static abstract TSelf FromBaseLight(Light l);
}