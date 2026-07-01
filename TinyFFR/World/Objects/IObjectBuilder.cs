// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
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
	ModelInstance CreateModelInstance(Mesh mesh, Material? material = null, Location? initialPosition = null, Rotation? initialRotation = null, Vect? initialScaling = null, ReadOnlySpan<char> name = default) {
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
	ModelInstance CreateModelInstance(Mesh mesh, Material? material, Transform initialTransform, ReadOnlySpan<char> name = default) {
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
	ModelInstance CreateModelInstance(Mesh mesh, Material? material, in ModelInstanceCreationConfig config);

	
	ModelInstanceGroup CreateModelInstances(ReadOnlySpan<Model> models, in ModelInstanceCreationConfig config);
	ModelInstanceGroup CreateModelInstances<TModelList>(TModelList models, in ModelInstanceCreationConfig config) where TModelList : IReadOnlyList<Model>;
	ModelInstanceGroup GroupModelInstances(params ReadOnlySpan<ModelInstance> instances) => GroupModelInstances(instances, true, default);
	ModelInstanceGroup GroupModelInstances(ReadOnlySpan<ModelInstance> instances, bool disposingGroupDisposesInstances, ReadOnlySpan<char> name);
	ModelInstanceGroup GroupModelInstances<TInstanceList>(TInstanceList instances, bool disposingGroupDisposesInstances = true, ReadOnlySpan<char> name = default) where TInstanceList : IReadOnlyList<ModelInstance>;
	
	#region QuadMesh
	QuadInstance CreateQuadInstance(QuadMesh mesh, Material material, Location? position = null, XYPair<float>? size = null, Direction? facingDirection = null, Direction? uprightDirection = null, Orientation2D positionAnchor = Orientation2D.None, ReadOnlySpan<char> name = default) {
		return CreateQuadInstance(
			mesh,
			material,
			new ModelInstanceCreationConfig {
				InitialTransform = QuadMesh.CalculateTransformForStandardQuadMesh(
					position ?? Location.Origin, 
					size ?? XYPair<float>.One, 
					facingDirection ?? Direction.Forward, 
					uprightDirection, 
					positionAnchor
				),
				Name = name
			}
		);
	}
	QuadInstance CreateQuadInstance(QuadMesh mesh, Material material, in ModelInstanceCreationConfig config) {
		return new QuadInstance(CreateModelInstance(mesh.UnderlyingMesh, material, in config));
	}
	#endregion
	
	#region MutableGridMesh
	MutableGridInstance CreateMutableGridInstance(MutableGridMesh mesh, Material material, Location? position = null, XYPair<float>? size = null, ReadOnlySpan<char> name = default) {
		return CreateMutableGridInstance(
			mesh,
			material,
			new ModelInstanceCreationConfig {
				InitialTransform = mesh.CalculateTransform(
					position ?? Location.Origin, 
					size ?? XYPair<float>.One
				),
				Name = name
			}
		);
	}
	MutableGridInstance CreateMutableGridInstance(MutableGridMesh mesh, Material material, in ModelInstanceCreationConfig config) {
		return new MutableGridInstance(CreateModelInstance(mesh.UnderlyingMesh, material, in config), mesh);
	}
	#endregion
	
	#region Text
	TextInstance CreateTextInstance(Font font, FontPen pen, ReadOnlySpan<char> initialText = default) => new(font, CreateModelInstance(font.CreateMeshForText(initialText), pen.GetPenMaterial()));
	#endregion
}