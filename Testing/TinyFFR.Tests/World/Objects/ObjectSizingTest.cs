// Created on 2026-10-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR.World;

[TestFixture]
class ObjectSizingTest {
	const float TestTolerance = 0.001f;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	static void AssertVectEqual(Vect expected, Vect actual) {
		Assert.AreEqual(expected.X, actual.X, TestTolerance, $"X: expected {expected}, was {actual}");
		Assert.AreEqual(expected.Y, actual.Y, TestTolerance, $"Y: expected {expected}, was {actual}");
		Assert.AreEqual(expected.Z, actual.Z, TestTolerance, $"Z: expected {expected}, was {actual}");
	}

	[Test]
	public void MeshShouldCorrectlyCalculateScalingForSize() {
		using var factory = new LocalTinyFfrFactory();
		using var mesh = factory.MeshBuilder.CreateCuboid(new Cuboid(2f, 1f, 4f));
		using var customMarginMesh = factory.MeshBuilder.CreateCuboid(new Cuboid(2f, 1f, 4f), false, new MeshGenerationConfig(), new MeshCreationConfig { BoundingBoxAdditionalMargin = 0.5f });

		AssertVectEqual(new Vect(2f, 4f, 1f), mesh.CalculateScalingForSize(new Vect(4f, 4f, 4f)));
		AssertVectEqual(Vect.One, mesh.CalculateScalingForSize(new Vect(2f, 1f, 4f)));
		AssertVectEqual(new Vect(2f, 4f, 1f), customMarginMesh.CalculateScalingForSize(new Vect(4f, 4f, 4f), boundingBoxMargin: 0.5f));
		AssertVectEqual(
			new Vect(4f, 4f, 4f) / customMarginMesh.BoundingBox.WithAllExtentsAdjustedBy(-MeshCreationConfig.DefaultBoundingBoxAdditionalMargin).Extents,
			customMarginMesh.CalculateScalingForSize(new Vect(4f, 4f, 4f))
		);
	}

	[Test]
	public void FlatMeshShouldKeepUnitScalingOnItsFlatAxis() {
		using var factory = new LocalTinyFfrFactory();
		using var quad = factory.MeshBuilder.CreateQuad();
		var mesh = quad.UnderlyingMesh;
		var extents = mesh.BoundingBox.WithAllExtentsAdjustedBy(-MeshCreationConfig.DefaultBoundingBoxAdditionalMargin).Extents;
		var target = new Vect(3f, 3f, 3f);

		var scaling = mesh.CalculateScalingForSize(target);

		foreach (var axis in new[] { Axis.X, Axis.Y, Axis.Z }) {
			var extent = extents[axis];
			var axisScaling = scaling[axis];
			if (extent < 1E-4f) Assert.AreEqual(1f, axisScaling, TestTolerance);
			else Assert.AreEqual(3f / extent, axisScaling, TestTolerance);
		}

		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, material);
		instance.SetSize(target);
		AssertVectEqual(scaling, instance.Scaling);
		Assert.AreNotEqual(Vect.Zero, instance.Scaling);
	}

	[Test]
	public void ModelInstanceShouldCorrectlySetSize() {
		using var factory = new LocalTinyFfrFactory();
		using var mesh = factory.MeshBuilder.CreateCuboid(new Cuboid(2f, 1f, 4f));
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, material);

		instance.SetScaling(new Vect(7f, 7f, 7f));
		instance.SetSize(new Vect(4f, 4f, 4f));
		AssertVectEqual(new Vect(2f, 4f, 1f), instance.Scaling);

		instance.SetSize(new Vect(4f, 4f, 4f));
		AssertVectEqual(new Vect(2f, 4f, 1f), instance.Scaling);

		((ISizableSceneObject) instance).SetSize(new Vect(1f, 1f, 1f));
		AssertVectEqual(new Vect(0.5f, 1f, 0.25f), instance.Scaling);

		var worldBounds = instance.GetWorldSpaceBoundingBox().WithAllExtentsAdjustedBy(-MeshCreationConfig.DefaultBoundingBoxAdditionalMargin * 0.25f);
		Assert.AreEqual(1f, worldBounds.Depth, TestTolerance);
	}

	[Test]
	public void ModelInstanceGroupShouldCorrectlySetSize() {
		using var factory = new LocalTinyFfrFactory();
		using var meshA = factory.MeshBuilder.CreateCuboid(new Cuboid(2f, 2f, 2f));
		using var meshB = factory.MeshBuilder.CreateCuboid(new Cuboid(2f, 2f, 2f), false, new MeshGenerationConfig(), new MeshCreationConfig { OriginTranslation = new Vect(-4f, 0f, 0f) });
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var group = factory.ObjectBuilder.CreateModelInstances([meshA, meshB], material);

		var combined = group.CalculateCombinedBoundingBox().WithAllExtentsAdjustedBy(-MeshCreationConfig.DefaultBoundingBoxAdditionalMargin);
		AssertVectEqual(new Vect(6f, 2f, 2f), combined.Extents);
		Assert.AreEqual(2f, combined.Position.X, TestTolerance);

		group.SetScaling(5f);
		group.SetSize(new Vect(12f, 1f, 4f));
		foreach (var instance in group) AssertVectEqual(new Vect(2f, 0.5f, 2f), instance.Scaling);

		((ISizableSceneObject) group).SetSize(new Vect(6f, 2f, 2f));
		foreach (var instance in group) AssertVectEqual(Vect.One, instance.Scaling);
	}

	[Test]
	public void ModelBundleShouldCorrectlyCalculateCombinedBoundingBoxAndScaling() {
		using var factory = new LocalTinyFfrFactory();
		using var resourceGroup = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true);
		var meshA = factory.MeshBuilder.CreateCuboid(new Cuboid(2f, 2f, 2f));
		var meshB = factory.MeshBuilder.CreateCuboid(new Cuboid(2f, 2f, 2f), false, new MeshGenerationConfig(), new MeshCreationConfig { OriginTranslation = new Vect(-4f, 0f, 0f) });
		resourceGroup.Add(meshA);
		resourceGroup.Add(meshB);
		resourceGroup.Seal();
		var bundle = new ModelBundle(resourceGroup);
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var group = factory.ObjectBuilder.CreateModelInstances([meshA, meshB], material);

		Assert.AreEqual(group.CalculateCombinedBoundingBox(), bundle.CalculateCombinedBoundingBox());
		AssertVectEqual(new Vect(2f, 0.5f, 2f), bundle.CalculateScalingForSize(new Vect(12f, 1f, 4f)));

		group.SetSize(new Vect(12f, 1f, 4f));
		AssertVectEqual(bundle.CalculateScalingForSize(new Vect(12f, 1f, 4f)), group.Scaling);
	}

	[Test]
	public void SceneObjectShouldSetSizeOnlyForSizableTypes() {
		foreach (var type in Enum.GetValues<SceneObjectType>()) {
			var expected = type is SceneObjectType.ModelInstance or SceneObjectType.ModelInstanceGroup;
			Assert.AreEqual(expected, type.IsSizable(), type.ToString());
		}

		using var factory = new LocalTinyFfrFactory();
		using var mesh = factory.MeshBuilder.CreateCuboid(new Cuboid(2f, 1f, 4f));
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, material);
		using var group = factory.ObjectBuilder.CreateModelInstances([mesh], material);
		using var light = factory.LightBuilder.CreatePointLight();

		SceneObject instanceObject = instance;
		instanceObject.SetSize(new Vect(4f, 4f, 4f));
		AssertVectEqual(new Vect(2f, 4f, 1f), instance.Scaling);

		SceneObject groupObject = group;
		groupObject.SetSize(new Vect(1f, 1f, 1f));
		AssertVectEqual(new Vect(0.5f, 1f, 0.25f), group.Scaling);

		SceneObject lightObject = light;
		Assert.DoesNotThrow(() => lightObject.SetSize(new Vect(4f, 4f, 4f)));
		Assert.DoesNotThrow(() => default(SceneObject).SetSize(new Vect(4f, 4f, 4f)));
	}
}
