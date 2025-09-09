// Created on 2025-09-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.World.Camera;

[TestFixture]
class RendererCreationConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromHeapStorageFormat() {
		var testConfigA = new CameraCreationConfig {
			Position = Location.Random(),
			ViewDirection = Direction.Random(),
			UpDirection = Direction.Random(),
			FieldOfView = Angle.Random(),
			AspectRatio = Real.Random(),
			FieldOfViewIsVertical = true,
			NearPlaneDistance = 10f,
			FarPlaneDistance = 20f,
			Name = "Aa Aa"
		};
		var testConfigB = new CameraCreationConfig {
			Position = Location.Random(),
			ViewDirection = Direction.Random(),
			UpDirection = Direction.Random(),
			FieldOfView = Angle.Random(),
			AspectRatio = Real.Random(),
			FieldOfViewIsVertical = false,
			NearPlaneDistance = 100f,
			FarPlaneDistance = 200f,
			Name = "BBBbbb"
		};

		static void ComparisonFunc(CameraCreationConfig expected, CameraCreationConfig actual) {
			Assert.AreEqual(expected.Position, actual.Position);
			Assert.AreEqual(expected.ViewDirection, actual.ViewDirection);
			Assert.AreEqual(expected.UpDirection, actual.UpDirection);
			Assert.AreEqual(expected.FieldOfView, actual.FieldOfView);
			Assert.AreEqual(expected.AspectRatio, actual.AspectRatio);
			Assert.AreEqual(expected.FieldOfViewIsVertical, actual.FieldOfViewIsVertical);
			Assert.AreEqual(expected.NearPlaneDistance, actual.NearPlaneDistance);
			Assert.AreEqual(expected.FarPlaneDistance, actual.FarPlaneDistance);
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertHeapSerializationWithObjects<CameraCreationConfig>()
			.Next(testConfigA.Position)
			.Next(testConfigA.ViewDirection)
			.Next(testConfigA.UpDirection)
			.Next(testConfigA.FieldOfView)
			.Next(testConfigA.AspectRatio)
			.Next(testConfigA.FieldOfViewIsVertical)
			.Next(testConfigA.NearPlaneDistance)
			.Next(testConfigA.FarPlaneDistance)
			.Next("Aa Aa")
			.For(testConfigA);

		AssertHeapSerializationWithObjects<CameraCreationConfig>()
			.Next(testConfigB.Position)
			.Next(testConfigB.ViewDirection)
			.Next(testConfigB.UpDirection)
			.Next(testConfigB.FieldOfView)
			.Next(testConfigB.AspectRatio)
			.Next(testConfigB.FieldOfViewIsVertical)
			.Next(testConfigB.NearPlaneDistance)
			.Next(testConfigB.FarPlaneDistance)
			.Next("BBBbbb")
			.For(testConfigB);

		AssertPropertiesAccountedFor<CameraCreationConfig>()
			.Including(nameof(CameraCreationConfig.Position))
			.Including(nameof(CameraCreationConfig.ViewDirection))
			.Including(nameof(CameraCreationConfig.UpDirection))
			.Including(nameof(CameraCreationConfig.FieldOfView))
			.Including(nameof(CameraCreationConfig.AspectRatio))
			.Including(nameof(CameraCreationConfig.FieldOfViewIsVertical))
			.Including(nameof(CameraCreationConfig.NearPlaneDistance))
			.Including(nameof(CameraCreationConfig.FarPlaneDistance))
			.Including(nameof(CameraCreationConfig.Name))
			.End();
	}
}