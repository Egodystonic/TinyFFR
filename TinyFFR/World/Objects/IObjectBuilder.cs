// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics.CodeAnalysis;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Builder interface that allows you to create the objects placed in a <see cref="Scene"/> — model instances, quads, grids and text.
/// </summary>
public interface IObjectBuilder {
	/// <summary>
	/// Creates a new <see cref="ModelInstance"/>; i.e. an occurrence of a <see cref="Model"/> placed in the world.
	/// </summary>
	/// <param name="model">The model (mesh and material together) to create an instance of.</param>
	/// <param name="initialPosition">Where the new instance should be. If <see langword="null"/>, the origin is used.</param>
	/// <param name="initialRotation">How the new instance should be oriented. If <see langword="null"/>, it is left unrotated.</param>
	/// <param name="initialScaling">How large the new instance should be. If <see langword="null"/>, it is left unscaled.</param>
	/// <param name="name">Optional name for the new object.</param>
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
	/// <summary>
	/// Creates a new <see cref="ModelInstance"/>; i.e. an occurrence of a model (constituted of a <see cref="Mesh"/> and optional <see cref="Material"/>) placed in the world.
	/// </summary>
	/// <param name="mesh">The mesh giving the new instance its shape.</param>
	/// <param name="material">The material giving the new instance its surface, or <see langword="null"/> to use the built-in default material.</param>
	/// <param name="initialPosition">Where the new instance should be. If <see langword="null"/>, the origin is used.</param>
	/// <param name="initialRotation">How the new instance should be oriented. If <see langword="null"/>, it is left unrotated.</param>
	/// <param name="initialScaling">How large the new instance should be. If <see langword="null"/>, it is left unscaled.</param>
	/// <param name="name">Optional name for the new object.</param>
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
	/// <summary>
	/// Creates a <see cref="ModelInstanceGroup"/> containing one new instance of each of the given <paramref name="models"/>.
	/// </summary>
	/// <param name="models">The models to create instances of.</param>
	/// <param name="initialPosition">Where the new instance should be. If <see langword="null"/>, the origin is used.</param>
	/// <param name="initialRotation">How the new instance should be oriented. If <see langword="null"/>, it is left unrotated.</param>
	/// <param name="initialScaling">How large the new instance should be. If <see langword="null"/>, it is left unscaled.</param>
	/// <param name="name">Optional name for the new object.</param>
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
	/// <summary>
	/// Creates a <see cref="ModelInstanceGroup"/> containing one new instance of each of the given <paramref name="models"/>.
	/// </summary>
	/// <param name="models">The models to create instances of.</param>
	/// <param name="initialPosition">Where the new instance should be. If <see langword="null"/>, the origin is used.</param>
	/// <param name="initialRotation">How the new instance should be oriented. If <see langword="null"/>, it is left unrotated.</param>
	/// <param name="initialScaling">How large the new instance should be. If <see langword="null"/>, it is left unscaled.</param>
	/// <param name="name">Optional name for the new object.</param>
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
	
	

	/// <summary>
	/// Creates a new <see cref="ModelInstance"/>; i.e. an occurrence of a <see cref="Model"/> placed in the world.
	/// </summary>
	/// <param name="model">The model (mesh and material together) to create an instance of.</param>
	/// <param name="initialTransform">Where the new instance should be, how it should be oriented, and how large it should be.</param>
	/// <param name="name">Optional name for the new object.</param>
	ModelInstance CreateModelInstance(Model model, Transform initialTransform, ReadOnlySpan<char> name = default) {
		return CreateModelInstance(
			model.Mesh,
			model.Material,
			initialTransform, 
			name
		);
	}
	/// <summary>
	/// Creates a new <see cref="ModelInstance"/>; i.e. an occurrence of a model (constituted of a <see cref="Mesh"/> and optional <see cref="Material"/>) placed in the world.
	/// </summary>
	/// <param name="mesh">The mesh giving the new instance its shape.</param>
	/// <param name="material">The material giving the new instance its surface, or <see langword="null"/> to use the built-in default material.</param>
	/// <param name="initialTransform">Where the new instance should be, how it should be oriented, and how large it should be.</param>
	/// <param name="name">Optional name for the new object.</param>
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
	/// <summary>
	/// Creates a <see cref="ModelInstanceGroup"/> containing one new instance of each of the given <paramref name="models"/>.
	/// </summary>
	/// <param name="models">The models to create instances of.</param>
	/// <param name="initialTransform">Where the new instance should be, how it should be oriented, and how large it should be.</param>
	/// <param name="name">Optional name for the new object.</param>
	ModelInstanceGroup CreateModelInstances(ReadOnlySpan<Model> models, Transform initialTransform, ReadOnlySpan<char> name = default) {
		return CreateModelInstances(
			models,
			new ModelInstanceCreationConfig {
				InitialTransform = initialTransform,
				Name = name
			}
		);
	}
	/// <summary>
	/// Creates a <see cref="ModelInstanceGroup"/> containing one new instance of each of the given <paramref name="models"/>.
	/// </summary>
	/// <param name="models">The models to create instances of.</param>
	/// <param name="initialTransform">Where the new instance should be, how it should be oriented, and how large it should be.</param>
	/// <param name="name">Optional name for the new object.</param>
	ModelInstanceGroup CreateModelInstances(IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Model> models, Transform initialTransform, ReadOnlySpan<char> name = default) {
		return CreateModelInstances(
			models,
			new ModelInstanceCreationConfig {
				InitialTransform = initialTransform,
				Name = name
			}
		);
	}
	
	
	/// <summary>
	/// Creates a new <see cref="ModelInstance"/>; i.e. an occurrence of a <see cref="Model"/> placed in the world.
	/// </summary>
	/// <param name="model">The model (mesh and material together) to create an instance of.</param>
	/// <param name="config">Configuration for the new object, including its name and initial transform.</param>
	ModelInstance CreateModelInstance(Model model, in ModelInstanceCreationConfig config) => CreateModelInstance(model.Mesh, model.Material, in config);
	/// <summary>
	/// Creates a new <see cref="ModelInstance"/>; i.e. an occurrence of a model (constituted of a <see cref="Mesh"/> and optional <see cref="Material"/>) placed in the world.
	/// </summary>
	/// <param name="mesh">The mesh giving the new instance its shape.</param>
	/// <param name="material">The material giving the new instance its surface, or <see langword="null"/> to use the built-in default material.</param>
	/// <param name="config">Configuration for the new object, including its name and initial transform.</param>
	ModelInstance CreateModelInstance(Mesh mesh, Material? material, in ModelInstanceCreationConfig config);

	
	/// <summary>
	/// Creates a <see cref="ModelInstanceGroup"/> containing one new instance of each of the given <paramref name="models"/>.
	/// </summary>
	/// <param name="models">The models to create instances of.</param>
	/// <param name="config">Configuration for the new object, including its name and initial transform.</param>
	ModelInstanceGroup CreateModelInstances(ReadOnlySpan<Model> models, in ModelInstanceCreationConfig config);
	/// <summary>
	/// Creates a <see cref="ModelInstanceGroup"/> containing one new instance of each of the given <paramref name="models"/>.
	/// </summary>
	/// <typeparam name="TModelList">The type of list supplied.</typeparam>
	/// <param name="models">The models to create instances of.</param>
	/// <param name="config">Configuration for the new object, including its name and initial transform.</param>
	ModelInstanceGroup CreateModelInstances<TModelList>(TModelList models, in ModelInstanceCreationConfig config) where TModelList : IReadOnlyList<Model>;
	/// <summary>
	/// Groups existing model instances together so they can be transformed as one.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The instances keep working individually; the group is an additional handle that transforms them together.
	/// </para>
	/// <para>
	/// When using this overload, the returned <see cref="ModelInstanceGroup"/> will dispose all contained <see cref="ModelInstance"/>s when
	/// it itself is disposed.
	/// </para>
	/// </remarks>
	ModelInstanceGroup GroupModelInstances(params ReadOnlySpan<ModelInstance> instances) => GroupModelInstances(instances, true, default);
	/// <summary>
	/// Groups existing model instances together so they can be transformed as one.
	/// </summary>
	/// <remarks>
	/// The instances keep working individually; the group is an additional handle that transforms them together.
	/// </remarks>
	/// <param name="instances">The instances to group together.</param>
	/// <param name="disposingGroupDisposesInstances">Whether disposing the group should also dispose the instances in it. Defaults to <see langword="true"/>.</param>
	/// <param name="name">Optional name for the new object.</param>
	ModelInstanceGroup GroupModelInstances(ReadOnlySpan<ModelInstance> instances, bool disposingGroupDisposesInstances, ReadOnlySpan<char> name);
	/// <summary>
	/// Groups existing model instances together so they can be transformed as one.
	/// </summary>
	/// <remarks>
	/// The instances keep working individually; the group is an additional handle that transforms them together.
	/// </remarks>
	/// <typeparam name="TInstanceList">The type of list supplied.</typeparam>
	/// <param name="instances">The instances to group together.</param>
	/// <param name="disposingGroupDisposesInstances">Whether disposing the group should also dispose the instances in it. Defaults to <see langword="true"/>.</param>
	/// <param name="name">Optional name for the new object.</param>
	ModelInstanceGroup GroupModelInstances<TInstanceList>(TInstanceList instances, bool disposingGroupDisposesInstances = true, ReadOnlySpan<char> name = default) where TInstanceList : IReadOnlyList<ModelInstance>;
	
	#region QuadMesh
	/// <summary>
	/// Creates a new instance of a flat quad; i.e. a rectangle in the world; using a pre-allocated <see cref="QuadMesh"/> that describes the quad geometry.
	/// </summary>
	/// <param name="mesh">The mesh giving the new instance its shape.</param>
	/// <param name="material">The material giving the new instance its surface, or <see langword="null"/> to use the built-in default material.</param>
	/// <param name="position">Where the new object should be. If <see langword="null"/>, the origin is used.</param>
	/// <param name="size">How large the new object should be.</param>
	/// <param name="facingDirection">See the equivalent parameter on the other overloads.</param>
	/// <param name="uprightDirection">See the equivalent parameter on the other overloads.</param>
	/// <param name="positionAnchor">Which part of the object is placed at its position (or its centre if <see cref="Orientation2D.None"/>).</param>
	/// <param name="name">Optional name for the new object.</param>
	QuadInstance CreateQuadInstance(QuadMesh mesh, Material material, Location? position = null, XYPair<float>? size = null, Direction? facingDirection = null, Direction? uprightDirection = null, Orientation2D positionAnchor = Orientation2D.None, ReadOnlySpan<char> name = default) {
		return CreateQuadInstance(
			mesh,
			material,
			new ModelInstanceCreationConfig {
				InitialTransform = QuadMesh.CalculateTransformForStandardQuadMesh(
					position ?? Location.Origin,
					size ?? XYPair<float>.One,
					facingDirection ?? Direction.Backward,
					uprightDirection,
					positionAnchor
				),
				Name = name
			}
		);
	}
	/// <summary>
	/// Creates a new instance of a flat quad; i.e. a rectangle in the world; using a pre-allocated <see cref="QuadMesh"/> that describes the quad geometry.
	/// </summary>
	/// <param name="mesh">The mesh giving the new instance its shape.</param>
	/// <param name="material">The material giving the new instance its surface, or <see langword="null"/> to use the built-in default material.</param>
	/// <param name="config">Configuration for the new object, including its name and initial transform.</param>
	QuadInstance CreateQuadInstance(QuadMesh mesh, Material material, in ModelInstanceCreationConfig config) {
		return new QuadInstance(CreateModelInstance(mesh.UnderlyingMesh, material, in config));
	}
	
	/// <summary>
	/// Creates a new flat quad that permanently faces the camera; using a pre-allocated <see cref="QuadMesh"/> that describes the quad geometry.
	/// </summary>
	/// <remarks>
	/// Camera-locked ("billboarded") objects always face the viewer, which is what keeps flat content such as icons and labels legible from any angle.
	/// </remarks>
	/// <param name="mesh">The mesh giving the new instance its shape.</param>
	/// <param name="material">The material giving the new instance its surface, or <see langword="null"/> to use the built-in default material.</param>
	/// <param name="position">Where the new object should be. If <see langword="null"/>, the origin is used.</param>
	/// <param name="size">How large the new object should be.</param>
	/// <param name="lockedUprightDirection">Which direction the object should keep as its "up". Pass <see cref="Direction.None"/> to let the object turn freely on every axis so that it always faces the camera squarely; pass any other direction to make it turn only about that axis, so that it stays upright as a tree or a signpost would.</param>
	/// <param name="positionAnchor">Which part of the object is placed at its position (or its centre if <see cref="Orientation2D.None"/>).</param>
	/// <param name="scalingMode">How the object is sized on screen; see <see cref="CameraLockedScalingMode"/>.</param>
	/// <param name="lockStyle">How the object decides which way to turn to face the camera; see <see cref="CameraLockStyle"/>.</param>
	/// <param name="name">Optional name for the new object.</param>
	CameraLockedQuadInstance CreateCameraLockedQuadInstance(QuadMesh mesh, Material material, Location? position = null, XYPair<float>? size = null, Direction? lockedUprightDirection = null, Orientation2D positionAnchor = Orientation2D.None, CameraLockedScalingMode scalingMode = CameraLockedScalingMode.Standard, CameraLockStyle lockStyle = CameraLockStyle.FaceCameraPosition, ReadOnlySpan<char> name = default) {
		return CreateCameraLockedQuadInstance(
			mesh,
			material,
			lockedUprightDirection ?? Direction.None,
			positionAnchor,
			scalingMode,
			lockStyle,
			new ModelInstanceCreationConfig {
				InitialTransform = QuadMesh.CalculateTransformForStandardQuadMesh(
					position ?? Location.Origin,
					size ?? XYPair<float>.One,
					Direction.Backward,
					Direction.Up,
					positionAnchor
				),
				Name = name
			}
		);
	}
	/// <summary>
	/// Creates a new flat quad that permanently faces the camera; using a pre-allocated <see cref="QuadMesh"/> that describes the quad geometry.
	/// </summary>
	/// <remarks>
	/// Camera-locked ("billboarded") objects always face the viewer, which is what keeps flat content such as icons and labels legible from any angle.
	/// </remarks>
	/// <param name="mesh">The mesh giving the new instance its shape.</param>
	/// <param name="material">The material giving the new instance its surface, or <see langword="null"/> to use the built-in default material.</param>
	/// <param name="lockedUprightDirection">Which direction the object should keep as its "up". Pass <see cref="Direction.None"/> to let the object turn freely on every axis so that it always faces the camera squarely; pass any other direction to make it turn only about that axis, so that it stays upright as a tree or a signpost would.</param>
	/// <param name="positionAnchor">Which part of the object is placed at its position (or its centre if <see cref="Orientation2D.None"/>).</param>
	/// <param name="scalingMode">How the object is sized on screen; see <see cref="CameraLockedScalingMode"/>.</param>
	/// <param name="lockStyle">How the object decides which way to turn to face the camera; see <see cref="CameraLockStyle"/>.</param>
	/// <param name="config">Configuration for the new object, including its name and initial transform.</param>
	CameraLockedQuadInstance CreateCameraLockedQuadInstance(QuadMesh mesh, Material material, Direction lockedUprightDirection, Orientation2D positionAnchor, CameraLockedScalingMode scalingMode, CameraLockStyle lockStyle, in ModelInstanceCreationConfig config) {
		return new CameraLockedQuadInstance(CreateQuadInstance(mesh, material, in config), lockedUprightDirection, positionAnchor, scalingMode, lockStyle);
	}
	#endregion
	
	#region MutableGridMesh
	/// <summary>
	/// Creates a new instance of a grid mesh whose vertices can be altered after creation; using a pre-allocated <see cref="MutableGridMesh"/> that describes the quad geometry.
	/// </summary>
	/// <param name="mesh">The mesh giving the new instance its shape.</param>
	/// <param name="material">The material giving the new instance its surface, or <see langword="null"/> to use the built-in default material.</param>
	/// <param name="position">Where the new object should be. If <see langword="null"/>, the origin is used.</param>
	/// <param name="size">How large the new object should be.</param>
	/// <param name="name">Optional name for the new object.</param>
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
	/// <summary>
	/// Creates a new instance of a grid mesh whose vertices can be altered after creation; using a pre-allocated <see cref="MutableGridMesh"/> that describes the quad geometry.
	/// </summary>
	/// <param name="mesh">The mesh giving the new instance its shape.</param>
	/// <param name="material">The material giving the new instance its surface, or <see langword="null"/> to use the built-in default material.</param>
	/// <param name="config">Configuration for the new object, including its name and initial transform.</param>
	MutableGridInstance CreateMutableGridInstance(MutableGridMesh mesh, Material material, in ModelInstanceCreationConfig config) {
		return new MutableGridInstance(CreateModelInstance(mesh.UnderlyingMesh, material, in config), mesh);
	}
	#endregion
	
	#region Text
	/// <summary>
	/// Creates a new instance of text drawn in the world; using a pre-created <see cref="FontPen"/> and <see cref="FontString"/> derived from a loaded <see cref="Font"/>.
	/// </summary>
	/// <param name="pen">How the text should be drawn — colour, thickness and similar.</param>
	/// <param name="string">The text to draw.</param>
	/// <param name="position">Where the new object should be. If <see langword="null"/>, the origin is used.</param>
	/// <param name="facingDirection">Which direction the text faces.</param>
	/// <param name="uprightDirection">Which direction counts as "upright" for the text.</param>
	/// <param name="layout">How the text should be laid out, such as how its lines are aligned.</param>
	/// <param name="name">Optional name for the new object.</param>
	TextInstance CreateTextInstance(FontPen pen, FontString @string, Location? position = null, Direction? facingDirection = null, Direction? uprightDirection = null, TextLayout? layout = null, ReadOnlySpan<char> name = default) {
		layout ??= new TextLayout();
		return CreateTextInstance(pen, @string, layout.Value, new ModelInstanceCreationConfig {
			Name = name,
			InitialTransform = @string.Font.GetTextInstanceTransform(@string.Size, position ?? Location.Origin, facingDirection ?? Direction.Backward, uprightDirection, layout.Value)
		});
	}
	/// <summary>
	/// Creates a new instance of text drawn in the world; using a pre-created <see cref="FontPen"/> and <see cref="FontString"/> derived from a loaded <see cref="Font"/>.
	/// </summary>
	/// <param name="pen">How the text should be drawn — colour, thickness and similar.</param>
	/// <param name="string">The text to draw.</param>
	/// <param name="layout">How the text should be laid out, such as how its lines are aligned.</param>
	/// <param name="config">Configuration for the new object, including its name and initial transform.</param>
	TextInstance CreateTextInstance(FontPen pen, FontString @string, TextLayout layout, in ModelInstanceCreationConfig config) {
		if (config.Name.IsEmpty) {
			const string InstanceNamePrefix = "Instance of ";
			const int MaxAutoNameLength = 5_000;
			var concatenatedLength = @string.GetStringMesh().GetNameLength() + InstanceNamePrefix.Length;
			if (concatenatedLength <= MaxAutoNameLength) {
				Span<char> autoName = stackalloc char[concatenatedLength];
				InstanceNamePrefix.CopyTo(autoName);
				@string.GetStringMesh().CopyName(autoName[InstanceNamePrefix.Length..]);
				return new TextInstance(CreateModelInstance(@string.GetStringMesh(), pen.GetPenMaterial(), config with { Name = autoName }), pen, @string, layout);
			}
		}
		
		return new TextInstance(CreateModelInstance(@string.GetStringMesh(), pen.GetPenMaterial(), in config), pen, @string, layout);
	}

	/// <summary>
	/// Creates a new piece of text in the world that permanently faces the camera; using a pre-created <see cref="FontPen"/> and <see cref="FontString"/> derived from a loaded <see cref="Font"/>.
	/// </summary>
	/// <remarks>
	/// Camera-locked ("billboarded") text always faces the viewer, so it stays legible from any angle — the usual way to draw a name or marker above an object in the world.
	/// </remarks>
	/// <param name="pen">How the text should be drawn — colour, thickness and similar.</param>
	/// <param name="string">The text to draw.</param>
	/// <param name="position">Where the new object should be. If <see langword="null"/>, the origin is used.</param>
	/// <param name="lockedUprightDirection">Which direction the object should keep as its "up". Pass <see cref="Direction.None"/> to let the object turn freely on every axis so that it always faces the camera squarely;
	/// pass any other direction to make it turn only about that axis, so that it stays upright as a tree or a signpost would.</param>
	/// <param name="layout">How the text should be laid out, such as how its lines are aligned.</param>
	/// <param name="scalingMode">How the object is sized on screen; see <see cref="CameraLockedScalingMode"/>.</param>
	/// <param name="lockStyle">How the object decides which way to turn to face the camera; <see cref="CameraLockStyle"/>.</param>
	/// <param name="name">Optional name for the new object.</param>
	CameraLockedTextInstance CreateCameraLockedTextInstance(FontPen pen, FontString @string, Location? position = null, Direction? lockedUprightDirection = null, TextLayout? layout = null, CameraLockedScalingMode scalingMode = CameraLockedScalingMode.Standard, CameraLockStyle lockStyle = CameraLockStyle.FaceCameraPosition, ReadOnlySpan<char> name = default) {
		layout ??= new TextLayout();
		return CreateCameraLockedTextInstance(
			pen,
			@string,
			lockedUprightDirection ?? Direction.None,
			layout.Value,
			scalingMode,
			lockStyle,
			new ModelInstanceCreationConfig {
				Name = name,
				InitialTransform = @string.Font.GetTextInstanceTransform(@string.Size, position ?? Location.Origin, Direction.Backward, Direction.Up, layout.Value)
			}
		);
	}
	/// <summary>
	/// Creates a new piece of text in the world that permanently faces the camera; using a pre-created <see cref="FontPen"/> and <see cref="FontString"/> derived from a loaded <see cref="Font"/>.
	/// </summary>
	/// <remarks>
	/// Camera-locked ("billboarded") text always faces the viewer, so it stays legible from any angle — the usual way to draw a name or marker above an object in the world.
	/// </remarks>
	/// <param name="pen">How the text should be drawn — colour, thickness and similar.</param>
	/// <param name="string">The text to draw.</param>
	/// <param name="lockedUprightDirection">Which direction the object should keep as its "up". Pass <see cref="Direction.None"/> to let the object turn freely on every axis so that it always faces the camera squarely;
	/// pass any other direction to make it turn only about that axis, so that it stays upright as a tree or a signpost would.</param>
	/// <param name="layout">How the text should be laid out, such as how its lines are aligned.</param>
	/// <param name="scalingMode">How the object is sized on screen; see <see cref="CameraLockedScalingMode"/>.</param>
	/// <param name="lockStyle">How the object decides which way to turn to face the camera; <see cref="CameraLockStyle"/>.</param>
	/// <param name="config">Configuration for the new object, including its name and initial transform.</param>
	CameraLockedTextInstance CreateCameraLockedTextInstance(FontPen pen, FontString @string, Direction lockedUprightDirection, TextLayout layout, CameraLockedScalingMode scalingMode, CameraLockStyle lockStyle, in ModelInstanceCreationConfig config) {
		return new CameraLockedTextInstance(CreateTextInstance(pen, @string, layout, in config), lockedUprightDirection, layout.PositionAnchor, scalingMode, lockStyle);
	}
	#endregion
}