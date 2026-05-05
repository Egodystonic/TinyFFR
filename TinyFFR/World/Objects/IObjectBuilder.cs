// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

public interface IObjectBuilder {
	ModelInstance CreateModelInstance(Model model, Location? initialPosition = null, Rotation? initialRotation = null, Vect? initialScaling = null, ReadOnlySpan<char> name = default) {
		return CreateModelInstance(
			model.Mesh,
			model.Material,
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
	ModelInstanceGroup CreateModelInstances(ReadOnlySpan<Model> models, Location? initialPosition = null, Rotation? initialRotation = null, Vect? initialScaling = null, ReadOnlySpan<char> name = default) {
		return CreateModelInstances(
			models,
			new Transform(
				translation: initialPosition?.AsVect() ?? ModelInstanceCreationConfig.DefaultInitialTransform.Translation,
				rotation: initialRotation ?? ModelInstanceCreationConfig.DefaultInitialTransform.Rotation,
				scaling: initialScaling ?? ModelInstanceCreationConfig.DefaultInitialTransform.Scaling
			),
			name
		);
	}
	ModelInstanceGroup CreateModelInstances(IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Model> models, Location? initialPosition = null, Rotation? initialRotation = null, Vect? initialScaling = null, ReadOnlySpan<char> name = default) {
		return CreateModelInstances(
			models,
			new Transform(
				translation: initialPosition?.AsVect() ?? ModelInstanceCreationConfig.DefaultInitialTransform.Translation,
				rotation: initialRotation ?? ModelInstanceCreationConfig.DefaultInitialTransform.Rotation,
				scaling: initialScaling ?? ModelInstanceCreationConfig.DefaultInitialTransform.Scaling
			),
			name
		);
	}
	
	

	ModelInstance CreateModelInstance(Model model, Transform initialTransform, ReadOnlySpan<char> name = default) {
		return CreateModelInstance(
			model.Mesh,
			model.Material,
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
	ModelInstanceGroup CreateModelInstances(ReadOnlySpan<Model> models, Transform initialTransform, ReadOnlySpan<char> name = default) {
		return CreateModelInstances(
			models,
			new ModelInstanceCreationConfig {
				InitialTransform = initialTransform,
				Name = name
			}
		);
	}
	ModelInstanceGroup CreateModelInstances(IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Model> models, Transform initialTransform, ReadOnlySpan<char> name = default) {
		return CreateModelInstances(
			models,
			new ModelInstanceCreationConfig {
				InitialTransform = initialTransform,
				Name = name
			}
		);
	}
	
	

	ModelInstance CreateModelInstance(Model model, in ModelInstanceCreationConfig config) => CreateModelInstance(model.Mesh, model.Material, in config);
	ModelInstance CreateModelInstance(Mesh mesh, Material material, in ModelInstanceCreationConfig config);
	
	ModelInstanceGroup CreateModelInstances(ReadOnlySpan<Model> models, in ModelInstanceCreationConfig config);
	ModelInstanceGroup CreateModelInstances(IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Model> models, in ModelInstanceCreationConfig config);
	ModelInstanceGroup GroupModelInstances(params ReadOnlySpan<ModelInstance> instances) => GroupModelInstances(instances, default);
	ModelInstanceGroup GroupModelInstances(ReadOnlySpan<ModelInstance> instances, ReadOnlySpan<char> name);
	ModelInstanceGroup GroupModelInstances(IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, ModelInstance> instances, ReadOnlySpan<char> name = default);
}