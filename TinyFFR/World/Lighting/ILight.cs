// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.World;

readonly record struct LightShadowFidelityData(uint MapSize, byte CascadeCount);

public interface ILight : IColoredSceneObject {
	LightType Type { get; }
	ColorVect Color { get; set; }
	float Brightness { get; set; }
	bool CastsShadows { get; set; }

	void AdjustBrightnessBy(float adjustment);
	void ScaleBrightnessBy(float scalar);
	Light AsBaseLight();

	internal void SetShadowFidelity(LightShadowFidelityData fidelityArgs);
}
public interface ILight<TSelf> : ILight, IDisposableResource<TSelf, ILightImplProvider> where TSelf : ILight<TSelf> {
	internal static abstract TSelf FromBaseLight(Light l);
	internal static abstract LightType SelfType { get; }
}