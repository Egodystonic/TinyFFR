// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Scene;

public interface IObjectBuilder {
	ModelInstance CreateModelInstance(Mesh mesh, Material material, Location? initialPosition = null, Rotation? initialRotation = null, Vect? initialScaling = null, ReadOnlySpan<char> name = default) {
		return CreateModelInstance(
			mesh,
			material,
			new Transform(
				translation: initialPosition?.AsVect() ?? ModelInstanceCreationConfig.DefaultInitialTransform.Translation,
				rotation: initialRotation ?? ModelInstanceCreationConfig.DefaultInitialTransform.Rotation,
				scaling: initialScaling ?? ModelInstanceCreationConfig.DefaultInitialTransform.Scaling
			),
			name
		);
	}
	ModelInstance CreateModelInstance(Mesh mesh, Material material, Transform initialTransform, ReadOnlySpan<char> name = default) {
		return CreateModelInstance(
			mesh,
			material,
			new ModelInstanceCreationConfig {
				InitialTransform = initialTransform,
				Name = name
			}
		);
	}
	ModelInstance CreateModelInstance(Mesh mesh, Material material, in ModelInstanceCreationConfig config);
}