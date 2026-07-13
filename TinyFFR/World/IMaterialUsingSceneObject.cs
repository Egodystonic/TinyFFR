// Created on 2025-03-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Assets.Materials;

namespace Egodystonic.TinyFFR.World;

public interface IMaterialUsingSceneObject {
	Material? Material { get; set; }
	MaterialEffectController? MaterialEffects { get; }
	void SetNullMaterialBaseColor(ColorVect baseColor);
	void SetNullMaterialShadingStyle(NullMaterialShadingStyle style);
}