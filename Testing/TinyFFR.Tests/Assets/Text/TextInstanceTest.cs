// Created on 2026-10-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

[TestFixture]
class TextInstanceTest {
	const float TestTolerance = 0.001f;
	static readonly Location TestPosition = new(1f, 2f, 3f);
	static readonly Location TestNewPosition = new(-4f, 0.5f, 7f);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	static void AssertLocation(Location expected, Location actual, string message) {
		Assert.IsTrue(expected.Equals(actual, TestTolerance), $"{message}: expected {expected}, was {actual}");
	}
	
	[Test]
	public void CameraLockedTextPositionShouldBeAnchorPoint() {
		using var factory = new LocalTinyFfrFactory();
		using var font = factory.AssetLoader.LoadFont();
		using var pen = font.CreatePen(StandardColor.White);
		using var str = font.CreateString("Hello");
		using var longerStr = font.CreateString("Hello, world!");

		foreach (var scalingMode in new[] { CameraLockedScalingMode.Standard, CameraLockedScalingMode.ViewportFractionalFixedHeightPlusPreservedAspectRatio }) {
			foreach (var anchor in Enum.GetValues<Orientation2D>()) {
				var label = $"{scalingMode}/{anchor}";
				using var text = factory.ObjectBuilder.CreateCameraLockedTextInstance(pen, str, position: TestPosition, layout: new TextLayout(0.1f, anchor), scalingMode: scalingMode);
				AssertLocation(TestPosition, text.Position, label + " at creation");

				text.Position = TestNewPosition;
				AssertLocation(TestNewPosition, text.Position, label + " after setting Position");

				text.ScaleBy(2f);
				AssertLocation(TestNewPosition, text.Position, label + " after ScaleBy");
				text.Scaling = text.Scaling * 0.5f;
				AssertLocation(TestNewPosition, text.Position, label + " after setting Scaling");

				text.String = longerStr;
				AssertLocation(TestNewPosition, text.Position, label + " after changing String");
			}
		}
	}

	[Test]
	public void PositionShouldBeLayoutAnchorPoint() {
		using var factory = new LocalTinyFfrFactory();
		using var font = factory.AssetLoader.LoadFont();
		using var pen = font.CreatePen(StandardColor.White);
		using var str = font.CreateString("Hello");
		using var longerStr = font.CreateString("Hello, world!");

		foreach (var anchor in Enum.GetValues<Orientation2D>()) {
			var label = anchor.ToString();
			var layout = new TextLayout(0.5f, anchor);
			using var text = factory.ObjectBuilder.CreateTextInstance(pen, str, position: TestPosition, layout: layout);
			Assert.AreEqual(layout, text.Layout);
			AssertLocation(TestPosition, text.Position, label + " at creation");
			AssertLocation(TestPosition, ((SceneObject) text).Position, label + " via SceneObject");

			text.Position = TestNewPosition;
			AssertLocation(TestNewPosition, text.Position, label + " after setting Position");

			text.Scaling = text.Scaling * 1.5f;
			AssertLocation(TestNewPosition, text.Position, label + " after setting Scaling");
			text.ScaleBy(2f);
			AssertLocation(TestNewPosition, text.Position, label + " after ScaleBy");
			text.AdjustScaleBy(-0.1f);
			AssertLocation(TestNewPosition, text.Position, label + " after AdjustScaleBy");

			text.Rotation = 30f % Direction.Up;
			AssertLocation(TestNewPosition, text.Position, label + " after setting Rotation");
			text.RotateBy(45f % Direction.Forward);
			AssertLocation(TestNewPosition, text.Position, label + " after RotateBy");

			text.String = longerStr;
			AssertLocation(TestNewPosition, text.Position, label + " after changing String");

			text.MoveBy(new Vect(1f, 0f, 0f));
			AssertLocation(TestNewPosition + new Vect(1f, 0f, 0f), text.Position, label + " after MoveBy");

			var relaidOut = new TextLayout(0.25f, Orientation2D.None);
			text.SetTransform(TestPosition, Direction.Backward, Direction.Up, relaidOut);
			Assert.AreEqual(relaidOut, text.Layout);
			AssertLocation(TestPosition, text.Position, label + " after re-laying out with no anchor");
			AssertLocation(TestPosition, text.Transform.Translation.AsLocation(), label + " centre after re-laying out with no anchor");
		}
	}

	[Test]
	public void AnchoredPositionShouldDifferFromCentre() {
		using var factory = new LocalTinyFfrFactory();
		using var font = factory.AssetLoader.LoadFont();
		using var pen = font.CreatePen(StandardColor.White);
		using var str = font.CreateString("Hello");

		using var text = factory.ObjectBuilder.CreateTextInstance(pen, str, position: TestPosition, layout: new TextLayout(0.5f, Orientation2D.Down));
		AssertLocation(TestPosition, text.Position, "anchored position");
		var expectedOffset = font.GetTextInstanceAnchorOffset(str.Size, text.Scaling, Orientation2D.Down);
		Assert.Greater(expectedOffset.Length, 0.01f);
		Assert.AreEqual(expectedOffset.Length, text.Position.DistanceFrom(text.Transform.Translation.AsLocation()), TestTolerance);
	}
}
