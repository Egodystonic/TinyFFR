// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.World;

interface ILight : IDisposableResource<LightHandle, ILightImplProvider>, IPositionedSceneObject {
	ColorVect Color { get; set; }
}
interface ILight<out TSelf> : ILight where TSelf : ILight {
	static abstract TSelf FromBaseLight(Light l);
}