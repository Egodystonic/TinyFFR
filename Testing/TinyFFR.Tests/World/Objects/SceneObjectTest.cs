// Created on 2026-09-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Linq;
using System.Reflection;

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
		var expectedTypes = Enum.GetValues<SceneObjectType>().Where(t => t != SceneObjectType.Unspecified).ToArray();

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
		Assert.AreEqual(SceneObjectType.MutableGridInstance, Get<Assets.Meshes.MutableGridInstance>());
		Assert.AreEqual(SceneObjectType.QuadInstance, Get<Assets.Meshes.QuadInstance>());
		Assert.AreEqual(SceneObjectType.CameraLockedQuadInstance, Get<Assets.Meshes.CameraLockedQuadInstance>());
		Assert.AreEqual(SceneObjectType.TextInstance, Get<Assets.Text.TextInstance>());
		Assert.AreEqual(SceneObjectType.CameraLockedTextInstance, Get<Assets.Text.CameraLockedTextInstance>());
		Assert.AreEqual(SceneObjectType.PointLight, Get<PointLight>());
		Assert.AreEqual(SceneObjectType.SpotLight, Get<SpotLight>());
		Assert.AreEqual(SceneObjectType.DirectionalLight, Get<DirectionalLight>());
		Assert.AreEqual(SceneObjectType.Camera, (SceneObjectType) getCameraSceneObjectType.Invoke(null, null)!);
		Assert.AreEqual(SceneObjectType.Unspecified, Get<Light>());
		Assert.AreEqual(SceneObjectType.Unspecified, Get<SceneObject>());
	}
}
