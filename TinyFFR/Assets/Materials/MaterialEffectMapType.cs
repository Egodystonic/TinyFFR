using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public enum MaterialEffectMapType {
	Color = 0,
#pragma warning disable CA1069 // Duplicate constants -- is deliberate, ORM/ORMR is set the same way internally
	OcclusionRoughnessMetallic = 1,
	OcclusionRoughnessMetallicReflectance = 1,
#pragma warning restore CA1069
	Emissive = 2,
	AbsorptionTransmission = 3
}