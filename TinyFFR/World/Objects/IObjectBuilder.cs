// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public interface IObjectBuilder {
	private void GetMeshAndMaterialFromGroup(ResourceGroup meshAndMaterialGroup, out Mesh outMesh, out Material outMaterial) {
		if (meshAndMaterialGroup.Meshes.Count < 1) throw new ArgumentException($"Given {nameof(ResourceGroup)} does not contain any {nameof(Mesh)} instances.", nameof(meshAndMaterialGroup));
		if (meshAndMaterialGroup.Materials.Count < 1) throw new ArgumentException($"Given {nameof(ResourceGroup)} does not contain any {nameof(Material)} instances.", nameof(meshAndMaterialGroup));

		outMesh = meshAndMaterialGroup.Meshes[0];
		outMaterial = meshAndMaterialGroup.Materials[0];
	}

	ModelInstance CreateModelInstance(ResourceGroup meshAndMaterialGroup, Location? initialPosition = null, Rotation? initialRotation = null, Vect? initialScaling = null, ReadOnlySpan<char> name = default) {
		GetMeshAndMaterialFromGroup(meshAndMaterialGroup, out var mesh, out var material);
		return CreateModelInstance(
			mesh,
			material,
			initialPosition,
			initialRotation,
			initialScaling,
			name
		);
	}
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

	ModelInstance CreateModelInstance(ResourceGroup meshAndMaterialGroup, Transform initialTransform, ReadOnlySpan<char> name = default) {
		GetMeshAndMaterialFromGroup(meshAndMaterialGroup, out var mesh, out var material);
		return CreateModelInstance(
			mesh,
			material,
			initialTransform, 
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