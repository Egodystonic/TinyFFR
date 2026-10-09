// Created on 2026-09-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Linq;
using System.Reflection;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR.World;

[TestFixture]
class SceneObjectTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	static SceneObjectAdapterFunctionTable[] GetAllFunctionTables() {
		return typeof(SceneObjectAdapterFunctionTable)
			.GetProperties(BindingFlags.Public | BindingFlags.Static)
			.Where(p => p.PropertyType == typeof(SceneObjectAdapterFunctionTable))
			.Select(p => {
				try {
					return (SceneObjectAdapterFunctionTable) p.GetValue(null)!;
				}
				catch (TargetInvocationException e) when (e.InnerException != null) {
					throw e.InnerException;
				}
			})
			.ToArray();
	}

	[Test]
	public void EveryFunctionTableShouldMatchItsTypeFlags() {
		Assert.DoesNotThrow(() => GetAllFunctionTables());
	}

	[Test]
	public void EverySceneObjectTypeShouldHaveExactlyOneFunctionTable() {
		var tableTypes = GetAllFunctionTables().Select(t => t.SceneObjectType).ToArray();
		var expectedTypes = Enum.GetValues<SceneObjectType>();

		Assert.That(tableTypes, Is.EquivalentTo(expectedTypes));
	}

	[Test]
	public void SceneObjectTypeIdsShouldBeUnique() {
		const int TypeIdMask = SceneObjectTypeExtensions.TypeIdReservedBitCount - 1;
		var ids = Enum.GetValues<SceneObjectType>().Select(t => (int) t & TypeIdMask).ToArray();

		Assert.That(ids, Is.Unique);
		Assert.That(ids.Max(), Is.LessThan(SceneObjectTypeExtensions.TypeIdReservedBitCount));
	}

	static SceneObjectType Get<T>() where T : ISceneObject => T.SceneObjectType;

	[Test]
	public void StaticSceneObjectTypesShouldMatchTheirTables() {
		var cameraType = typeof(ModelInstance).Assembly.GetType("Egodystonic.TinyFFR.World.Camera", throwOnError: true)!;
		var getCameraSceneObjectType = typeof(SceneObjectTest).GetMethod(nameof(Get), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(cameraType);

		Assert.AreEqual(SceneObjectType.ModelInstance, Get<ModelInstance>());
		Assert.AreEqual(SceneObjectType.ModelInstanceGroup, Get<ModelInstanceGroup>());
		Assert.AreEqual(SceneObjectType.MutableGridInstance, Get<MutableGridInstance>());
		Assert.AreEqual(SceneObjectType.QuadInstance, Get<QuadInstance>());
		Assert.AreEqual(SceneObjectType.CameraLockedQuadInstance, Get<CameraLockedQuadInstance>());
		Assert.AreEqual(SceneObjectType.TextInstance, Get<TextInstance>());
		Assert.AreEqual(SceneObjectType.CameraLockedTextInstance, Get<CameraLockedTextInstance>());
		Assert.AreEqual(SceneObjectType.PointLight, Get<PointLight>());
		Assert.AreEqual(SceneObjectType.SpotLight, Get<SpotLight>());
		Assert.AreEqual(SceneObjectType.DirectionalLight, Get<DirectionalLight>());
		Assert.AreEqual(SceneObjectType.Camera, (SceneObjectType) getCameraSceneObjectType.Invoke(null, null)!);
		Assert.AreEqual(SceneObjectType.None, Get<Light>());
		Assert.AreEqual(SceneObjectType.None, Get<SceneObject>());
	}
	[Test]
	public void DefaultSceneObjectShouldBeANoOp() {
		var obj = default(SceneObject);

		Assert.AreEqual(SceneObjectType.None, obj.Type);
		Assert.IsFalse(obj.Type.IsPositioned());
		Assert.IsFalse(obj.Type.IsOriented());
		Assert.IsFalse(obj.Type.IsScaled());
		Assert.IsFalse(obj.Type.IsMaterialReceiving());
		Assert.IsFalse(obj.Type.IsColored());
		Assert.IsFalse(obj.Type.IsStoredAsModelInstance());

		Assert.DoesNotThrow(() => {
			obj.Position = new Location(1f, 2f, 3f);
			obj.MoveBy(new Vect(1f, 0f, 0f));
			obj.Rotation = 90f % Direction.Up;
			obj.RotationQuaternion = System.Numerics.Quaternion.Identity;
			obj.RotateBy(90f % Direction.Up);
			obj.RotateBy(System.Numerics.Quaternion.Identity);
			obj.Scaling = new Vect(2f);
			obj.ScaleBy(2f);
			obj.ScaleBy(new Vect(2f));
			obj.AdjustScaleBy(1f);
			obj.AdjustScaleBy(new Vect(1f));
			obj.Transform = Transform.None;
			obj.RotateBy(90f % Direction.Up, Location.Origin);
			obj.RotateBy(System.Numerics.Quaternion.Identity, Location.Origin);
			obj.SetMaterial(default);
			obj.SetDefaultMaterialBaseColor(StandardColor.White);
			obj.SetDefaultMaterialShadingStyle(default);
			obj.ColorHue = 90f;
			obj.ColorSaturation = 0.5f;
			obj.ColorLightness = 0.5f;
			obj.AdjustColorHueBy(10f);
			obj.AdjustColorSaturationBy(0.1f);
			obj.AdjustColorLightnessBy(0.1f);
			obj.DisposeUnderlyingObject();
			obj.SetSize(new Vect(2f));
			_ = obj.ToString();
		});

		Assert.AreEqual(Location.Origin, obj.Position);
		Assert.AreEqual(Rotation.None, obj.Rotation);
		Assert.AreEqual(System.Numerics.Quaternion.Identity, obj.RotationQuaternion);
		Assert.AreEqual(Vect.One, obj.Scaling);
		Assert.AreEqual(Transform.None, obj.Transform);
		Assert.AreEqual(Angle.Zero, obj.ColorHue);
		Assert.AreEqual(0f, obj.ColorSaturation);
		Assert.AreEqual(0f, obj.ColorLightness);
		Assert.AreEqual("Empty Scene Object", obj.GetNameAsNewStringObject());
		Assert.AreEqual("Empty Scene Object".Length, obj.GetNameLength());
		var nameBuffer = new char[obj.GetNameLength()];
		obj.CopyName(nameBuffer);
		Assert.AreEqual("Empty Scene Object", new String(nameBuffer));
		Assert.AreEqual("Scene Object (None)", obj.ToString());
		Assert.IsFalse(obj.Type.IsSizable());
		Assert.AreEqual(default(SceneObject), obj);

		Assert.Throws<InvalidCastException>(() => _ = (ModelInstance) obj);
		Assert.Throws<InvalidCastException>(() => _ = (ModelInstanceGroup) obj);
		Assert.Throws<InvalidCastException>(() => _ = (PointLight) obj);
		Assert.Throws<InvalidCastException>(() => _ = (MutableGridInstance) obj);
	}

	[Test]
	public void CameraLockedInstancesShouldRoundTripThroughSceneObjects() {
		using var factory = new LocalTinyFfrFactory();
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var quadMesh = factory.MeshBuilder.CreateQuad();
		using var quad = factory.ObjectBuilder.CreateCameraLockedQuadInstance(quadMesh, material, lockedUprightDirection: Direction.Left, positionAnchor: Orientation2D.UpLeft, scalingMode: CameraLockedScalingMode.ViewportFractionalFixedWidth, lockStyle: CameraLockStyle.FaceCameraPlane);

		SceneObject quadSceneObject = quad;
		var roundTrippedQuad = (CameraLockedQuadInstance) quadSceneObject;
		Assert.AreEqual(quad, roundTrippedQuad);
		Assert.AreEqual(quad.LockedUprightDirection, roundTrippedQuad.LockedUprightDirection);
		Assert.AreEqual(quad.PositionAnchor, roundTrippedQuad.PositionAnchor);
		Assert.AreEqual(quad.ScalingMode, roundTrippedQuad.ScalingMode);
		Assert.AreEqual(quad.LockStyle, roundTrippedQuad.LockStyle);
		Assert.AreEqual((SceneObject) quad, quadSceneObject);
		Assert.AreEqual(((SceneObject) quad).GetHashCode(), quadSceneObject.GetHashCode());
		Assert.Throws<InvalidCastException>(() => _ = (CameraLockedTextInstance) quadSceneObject);
		Assert.Throws<InvalidCastException>(() => _ = (CameraLockedQuadInstance) (SceneObject) quad.UnderlyingQuadInstance);

		using var font = factory.AssetLoader.LoadFont();
		using var pen = font.CreatePen(ColorVect.WhiteOpaque);
		using var @string = font.CreateString("Round Trip");
		using var text = factory.ObjectBuilder.CreateCameraLockedTextInstance(pen, @string, lockedUprightDirection: Direction.Right, scalingMode: CameraLockedScalingMode.ViewportFractionalFixedHeight, lockStyle: CameraLockStyle.FaceCameraPlane);

		SceneObject textSceneObject = text;
		var roundTrippedText = (CameraLockedTextInstance) textSceneObject;
		Assert.AreEqual(text, roundTrippedText);
		Assert.AreEqual(text.LockedUprightDirection, roundTrippedText.LockedUprightDirection);
		Assert.AreEqual(text.PositionAnchor, roundTrippedText.PositionAnchor);
		Assert.AreEqual(text.ScalingMode, roundTrippedText.ScalingMode);
		Assert.AreEqual(text.LockStyle, roundTrippedText.LockStyle);
		Assert.Throws<InvalidCastException>(() => _ = (CameraLockedQuadInstance) textSceneObject);
	}

	[Test]
	public void ScenesShouldAddAndRemoveEverySceneObjectType() {
		using var factory = new LocalTinyFfrFactory();
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var quadMesh = factory.MeshBuilder.CreateQuad();
		using var cuboidMesh = factory.MeshBuilder.CreateCuboid(Cuboid.UnitCube);
		using var gridMesh = factory.MeshBuilder.CreateMutableGrid(new XYPair<int>(4, 4));
		using var font = factory.AssetLoader.LoadFont();
		using var pen = font.CreatePen(ColorVect.WhiteOpaque);
		using var @string = font.CreateString("Scene Object");
		using var scene = factory.SceneBuilder.CreateScene();
		var cameraPosition = new Location(3f, 2f, -6f);
		using var camera = factory.CameraBuilder.CreateCamera(cameraPosition, (Location.Origin - cameraPosition).Direction);

		using var modelInstance = factory.ObjectBuilder.CreateModelInstance(cuboidMesh, material);
		var groupedInstances = new[] { factory.ObjectBuilder.CreateModelInstance(cuboidMesh, material), factory.ObjectBuilder.CreateModelInstance(cuboidMesh, material) };
		using var group = factory.ObjectBuilder.GroupModelInstances(groupedInstances);
		using var grid = factory.ObjectBuilder.CreateMutableGridInstance(gridMesh, material);
		using var quad = factory.ObjectBuilder.CreateQuadInstance(quadMesh, material);
		using var text = factory.ObjectBuilder.CreateTextInstance(pen, @string);
		var camLockedPosition = new Location(1f, 0.5f, 0f);
		using var camLockedQuad = factory.ObjectBuilder.CreateCameraLockedQuadInstance(quadMesh, material, position: camLockedPosition);
		using var camLockedQuadAddedTyped = factory.ObjectBuilder.CreateCameraLockedQuadInstance(quadMesh, material, position: camLockedPosition);
		using var camLockedQuadAddedPlain = factory.ObjectBuilder.CreateCameraLockedQuadInstance(quadMesh, material, position: camLockedPosition);
		using var camLockedText = factory.ObjectBuilder.CreateCameraLockedTextInstance(pen, @string);
		using var pointLight = factory.LightBuilder.CreatePointLight();
		using var spotLight = factory.LightBuilder.CreateSpotLight();
		using var directionalLight = factory.LightBuilder.CreateDirectionalLight();

		var sceneObjects = new SceneObject[] { modelInstance, group, grid, quad, text, camLockedQuad, camLockedText, pointLight, spotLight, directionalLight };
		foreach (var sceneObject in sceneObjects) scene.Add(sceneObject);
		scene.Add(camLockedQuadAddedTyped);
		scene.Add(camLockedQuadAddedPlain.UnderlyingQuadInstance);

		var expectedModelInstances = new[] {
			modelInstance, groupedInstances[0], groupedInstances[1], grid.UnderlyingModelInstance, quad.UnderlyingModelInstance, text.UnderlyingModelInstance,
			camLockedQuad.UnderlyingQuadInstance.UnderlyingModelInstance, camLockedText.UnderlyingTextInstance.UnderlyingModelInstance,
			camLockedQuadAddedTyped.UnderlyingQuadInstance.UnderlyingModelInstance, camLockedQuadAddedPlain.UnderlyingQuadInstance.UnderlyingModelInstance
		};
		CollectionAssert.AreEquivalent(expectedModelInstances, scene.ContainedModelInstances.ToArray());
		Assert.AreEqual(3, scene.ContainedLights.Count);

		((LocalSceneBuilder) scene.Implementation).PrepareCameraSensitiveObjectsForRender(scene.Handle, camera);
		var viaSceneObjectRotation = camLockedQuad.UnderlyingQuadInstance.UnderlyingModelInstance.Rotation;
		Assert.AreEqual(camLockedQuadAddedTyped.UnderlyingQuadInstance.UnderlyingModelInstance.Rotation, viaSceneObjectRotation);
		Assert.AreNotEqual(camLockedQuadAddedPlain.UnderlyingQuadInstance.UnderlyingModelInstance.Rotation, viaSceneObjectRotation);

		Assert.DoesNotThrow(() => scene.Add(camera));
		Assert.DoesNotThrow(() => scene.Remove(camera));
		Assert.AreEqual(expectedModelInstances.Length, scene.ContainedModelInstances.Count);
		Assert.AreEqual(3, scene.ContainedLights.Count);
		Assert.DoesNotThrow(() => scene.Add(default(SceneObject)));
		Assert.DoesNotThrow(() => scene.Remove(default(SceneObject)));
		Assert.AreEqual(expectedModelInstances.Length, scene.ContainedModelInstances.Count);
		Assert.AreEqual(3, scene.ContainedLights.Count);

		foreach (var sceneObject in sceneObjects) scene.Remove(sceneObject);
		scene.Remove(camLockedQuadAddedTyped);
		scene.Remove(camLockedQuadAddedPlain.UnderlyingQuadInstance);
		Assert.AreEqual(0, scene.ContainedModelInstances.Count);
		Assert.AreEqual(0, scene.ContainedLights.Count);
	}
}
