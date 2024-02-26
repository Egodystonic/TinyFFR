// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Runtime.CompilerServices;

namespace Egodystonic.TinyFFR;

[TestFixture]
class Orientation3DTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyCombineBits() {
		void AssertIntEquals<TEnum1, TEnum2>(TEnum1 expected, TEnum2 actual) where TEnum1 : Enum where TEnum2 : Enum {
			Assert.AreEqual((int) (object) expected, (int) (object) actual);
		}

		AssertIntEquals(Axis.X, XAxisOrientation3D.Left | XAxisOrientation3D.Right);
		AssertIntEquals(Axis.Y, YAxisOrientation3D.Up | YAxisOrientation3D.Down);
		AssertIntEquals(Axis.Z, ZAxisOrientation3D.Forward | ZAxisOrientation3D.Backward);

		AssertIntEquals(CardinalOrientation3D.Left, XAxisOrientation3D.Left);
		AssertIntEquals(CardinalOrientation3D.Right, XAxisOrientation3D.Right);
		AssertIntEquals(CardinalOrientation3D.Up, YAxisOrientation3D.Up);
		AssertIntEquals(CardinalOrientation3D.Down, YAxisOrientation3D.Down);
		AssertIntEquals(CardinalOrientation3D.Forward, ZAxisOrientation3D.Forward);
		AssertIntEquals(CardinalOrientation3D.Backward, ZAxisOrientation3D.Backward);

		AssertIntEquals(DiagonalOrientation3D.UpLeftForward, CardinalOrientation3D.Up | CardinalOrientation3D.Left | CardinalOrientation3D.Forward);
		AssertIntEquals(DiagonalOrientation3D.UpRightForward, CardinalOrientation3D.Up | CardinalOrientation3D.Right | CardinalOrientation3D.Forward);
		AssertIntEquals(DiagonalOrientation3D.UpLeftBackward, CardinalOrientation3D.Up | CardinalOrientation3D.Left | CardinalOrientation3D.Backward);
		AssertIntEquals(DiagonalOrientation3D.UpRightBackward, CardinalOrientation3D.Up | CardinalOrientation3D.Right | CardinalOrientation3D.Backward);
		AssertIntEquals(DiagonalOrientation3D.DownLeftForward, CardinalOrientation3D.Down | CardinalOrientation3D.Left | CardinalOrientation3D.Forward);
		AssertIntEquals(DiagonalOrientation3D.DownRightForward, CardinalOrientation3D.Down | CardinalOrientation3D.Right | CardinalOrientation3D.Forward);
		AssertIntEquals(DiagonalOrientation3D.DownLeftBackward, CardinalOrientation3D.Down | CardinalOrientation3D.Left | CardinalOrientation3D.Backward);
		AssertIntEquals(DiagonalOrientation3D.DownRightBackward, CardinalOrientation3D.Down | CardinalOrientation3D.Right | CardinalOrientation3D.Backward);

		AssertIntEquals(Orientation3D.Left, CardinalOrientation3D.Left);
		AssertIntEquals(Orientation3D.Right, CardinalOrientation3D.Right);
		AssertIntEquals(Orientation3D.Up, CardinalOrientation3D.Up);
		AssertIntEquals(Orientation3D.Down, CardinalOrientation3D.Down);
		AssertIntEquals(Orientation3D.Forward, CardinalOrientation3D.Forward);
		AssertIntEquals(Orientation3D.Backward, CardinalOrientation3D.Backward);

		AssertIntEquals(Orientation3D.UpLeft, CardinalOrientation3D.Up | CardinalOrientation3D.Left);
		AssertIntEquals(Orientation3D.UpRight, CardinalOrientation3D.Up | CardinalOrientation3D.Right);
		AssertIntEquals(Orientation3D.UpBackward, CardinalOrientation3D.Up | CardinalOrientation3D.Backward);
		AssertIntEquals(Orientation3D.UpForward, CardinalOrientation3D.Up | CardinalOrientation3D.Forward);
		AssertIntEquals(Orientation3D.DownLeft, CardinalOrientation3D.Down | CardinalOrientation3D.Left);
		AssertIntEquals(Orientation3D.DownRight, CardinalOrientation3D.Down | CardinalOrientation3D.Right);
		AssertIntEquals(Orientation3D.DownBackward, CardinalOrientation3D.Down | CardinalOrientation3D.Backward);
		AssertIntEquals(Orientation3D.DownForward, CardinalOrientation3D.Down | CardinalOrientation3D.Forward);
		AssertIntEquals(Orientation3D.LeftForward, CardinalOrientation3D.Left | CardinalOrientation3D.Forward);
		AssertIntEquals(Orientation3D.LeftBackward, CardinalOrientation3D.Left | CardinalOrientation3D.Backward);
		AssertIntEquals(Orientation3D.RightForward, CardinalOrientation3D.Right | CardinalOrientation3D.Forward);
		AssertIntEquals(Orientation3D.RightBackward, CardinalOrientation3D.Right | CardinalOrientation3D.Backward);

		AssertIntEquals(Orientation3D.UpLeftForward, DiagonalOrientation3D.UpLeftForward);
		AssertIntEquals(Orientation3D.UpRightForward, DiagonalOrientation3D.UpRightForward);
		AssertIntEquals(Orientation3D.UpLeftBackward, DiagonalOrientation3D.UpLeftBackward);
		AssertIntEquals(Orientation3D.UpRightBackward, DiagonalOrientation3D.UpRightBackward);
		AssertIntEquals(Orientation3D.DownLeftForward, DiagonalOrientation3D.DownLeftForward);
		AssertIntEquals(Orientation3D.DownRightForward, DiagonalOrientation3D.DownRightForward);
		AssertIntEquals(Orientation3D.DownLeftBackward, DiagonalOrientation3D.DownLeftBackward);
		AssertIntEquals(Orientation3D.DownRightBackward, DiagonalOrientation3D.DownRightBackward);
	}

	[Test]
	public void ShouldCorrectlyCast() {
		void AssertForEnum<TIn, TOut>(Func<TIn, TOut> conversionFunc) where TIn : struct, Enum where TOut : struct, Enum {
			Assert.AreEqual(typeof(int), Enum.GetUnderlyingType(typeof(TIn)));
			Assert.AreEqual(typeof(int), Enum.GetUnderlyingType(typeof(TOut)));
			foreach (var name in Enum.GetNames<TIn>()) {
				var val = Enum.Parse<TIn>(name);
				Assert.AreEqual(Enum.Parse<TOut>(name), conversionFunc(val));
				Assert.AreEqual((int) (object) Unsafe.As<TIn, TOut>(ref val), (int) (object) val);
			}
		}

		AssertForEnum<XAxisOrientation3D, CardinalOrientation3D>(v => v.AsCardinalOrientation());
		AssertForEnum<YAxisOrientation3D, CardinalOrientation3D>(v => v.AsCardinalOrientation());
		AssertForEnum<ZAxisOrientation3D, CardinalOrientation3D>(v => v.AsCardinalOrientation());
		AssertForEnum<XAxisOrientation3D, Orientation3D>(v => v.AsGeneralOrientation());
		AssertForEnum<YAxisOrientation3D, Orientation3D>(v => v.AsGeneralOrientation());
		AssertForEnum<ZAxisOrientation3D, Orientation3D>(v => v.AsGeneralOrientation());

		AssertForEnum<CardinalOrientation3D, Orientation3D>(v => v.AsGeneralOrientation());
		AssertForEnum<DiagonalOrientation3D, Orientation3D>(v => v.AsGeneralOrientation());
	}

	[Test]
	public void ShouldCorrectlyAscertainWhetherIsCardinal() {
		foreach (var orientation in Enum.GetValues<Orientation3D>()) {
			if (orientation == Orientation3D.None) continue;
			Assert.AreEqual(((int[]) Enum.GetValuesAsUnderlyingType<CardinalOrientation3D>()).Contains((int) orientation), orientation.IsCardinal());
		}
	}
	[Test]
	public void ShouldCorrectlyAscertainWhetherIsDiagonal() {
		foreach (var orientation in Enum.GetValues<Orientation3D>()) {
			if (orientation == Orientation3D.None) continue;
			Assert.AreEqual(((int[]) Enum.GetValuesAsUnderlyingType<DiagonalOrientation3D>()).Contains((int) orientation), orientation.IsDiagonal());
		}
	}

	[Test]
	public void ShouldCorrectlyConvertToDirection() {
		Assert.AreEqual(Direction.None, XAxisOrientation3D.None.ToDirection());
		Assert.AreEqual(Direction.Left, XAxisOrientation3D.Left.ToDirection());
		Assert.AreEqual(Direction.Right, XAxisOrientation3D.Right.ToDirection());

		Assert.AreEqual(Direction.None, YAxisOrientation3D.None.ToDirection());
		Assert.AreEqual(Direction.Up, YAxisOrientation3D.Up.ToDirection());
		Assert.AreEqual(Direction.Down, YAxisOrientation3D.Down.ToDirection());

		Assert.AreEqual(Direction.None, ZAxisOrientation3D.None.ToDirection());
		Assert.AreEqual(Direction.Forward, ZAxisOrientation3D.Forward.ToDirection());
		Assert.AreEqual(Direction.Backward, ZAxisOrientation3D.Backward.ToDirection());

		Assert.AreEqual(Direction.None, CardinalOrientation3D.None.ToDirection());
		Assert.AreEqual(Direction.Left, CardinalOrientation3D.Left.ToDirection());
		Assert.AreEqual(Direction.Right, CardinalOrientation3D.Right.ToDirection());
		Assert.AreEqual(Direction.Up, CardinalOrientation3D.Up.ToDirection());
		Assert.AreEqual(Direction.Down, CardinalOrientation3D.Down.ToDirection());
		Assert.AreEqual(Direction.Forward, CardinalOrientation3D.Forward.ToDirection());
		Assert.AreEqual(Direction.Backward, CardinalOrientation3D.Backward.ToDirection());
	}

	[Test]
	public void ShouldCorrectlyAscertainAxis() {
		Assert.AreEqual(Axis.None, CardinalOrientation3D.None.GetAxis());
		Assert.AreEqual(Axis.X, CardinalOrientation3D.Left.GetAxis());
		Assert.AreEqual(Axis.X, CardinalOrientation3D.Right.GetAxis());
		Assert.AreEqual(Axis.Y, CardinalOrientation3D.Up.GetAxis());
		Assert.AreEqual(Axis.Y, CardinalOrientation3D.Down.GetAxis());
		Assert.AreEqual(Axis.Z, CardinalOrientation3D.Forward.GetAxis());
		Assert.AreEqual(Axis.Z, CardinalOrientation3D.Backward.GetAxis());
	}

	[Test]
	public void ShouldCorrectlyGrabAxes() {
		void AssertOrientation(Orientation3D input, XAxisOrientation3D expectedX, YAxisOrientation3D expectedY, ZAxisOrientation3D expectedZ) {
			Assert.AreEqual(expectedX, input.GetXAxis());
			Assert.AreEqual(expectedY, input.GetYAxis());
			Assert.AreEqual(expectedZ, input.GetZAxis());

			if (input.IsDiagonal()) {
				var diag = (DiagonalOrientation3D) input;
				Assert.AreEqual(expectedX, diag.GetXAxis());
				Assert.AreEqual(expectedY, diag.GetYAxis());
				Assert.AreEqual(expectedZ, diag.GetZAxis());
			}
		}

		AssertOrientation(Orientation3D.None, XAxisOrientation3D.None, YAxisOrientation3D.None, ZAxisOrientation3D.None);

		AssertOrientation(Orientation3D.Left, XAxisOrientation3D.Left, YAxisOrientation3D.None, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.Right, XAxisOrientation3D.Right, YAxisOrientation3D.None, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.Up, XAxisOrientation3D.None, YAxisOrientation3D.Up, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.Down, XAxisOrientation3D.None, YAxisOrientation3D.Down, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.Forward, XAxisOrientation3D.None, YAxisOrientation3D.None, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.Backward, XAxisOrientation3D.None, YAxisOrientation3D.None, ZAxisOrientation3D.Backward);

		AssertOrientation(Orientation3D.UpLeft, XAxisOrientation3D.Left, YAxisOrientation3D.Up, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.UpRight, XAxisOrientation3D.Right, YAxisOrientation3D.Up, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.UpBackward, XAxisOrientation3D.None, YAxisOrientation3D.Up, ZAxisOrientation3D.Backward);
		AssertOrientation(Orientation3D.UpForward, XAxisOrientation3D.None, YAxisOrientation3D.Up, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.DownLeft, XAxisOrientation3D.Left, YAxisOrientation3D.Down, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.DownRight, XAxisOrientation3D.Right, YAxisOrientation3D.Down, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.DownBackward, XAxisOrientation3D.None, YAxisOrientation3D.Down, ZAxisOrientation3D.Backward);
		AssertOrientation(Orientation3D.DownForward, XAxisOrientation3D.None, YAxisOrientation3D.Down, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.LeftForward, XAxisOrientation3D.Left, YAxisOrientation3D.None, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.LeftBackward, XAxisOrientation3D.Left, YAxisOrientation3D.None, ZAxisOrientation3D.Backward);
		AssertOrientation(Orientation3D.RightForward, XAxisOrientation3D.Right, YAxisOrientation3D.None, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.RightBackward, XAxisOrientation3D.Right, YAxisOrientation3D.None, ZAxisOrientation3D.Backward);

		AssertOrientation(Orientation3D.UpLeftForward, XAxisOrientation3D.Left, YAxisOrientation3D.Up, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.UpRightForward, XAxisOrientation3D.Right, YAxisOrientation3D.Up, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.UpLeftBackward, XAxisOrientation3D.Left, YAxisOrientation3D.Up, ZAxisOrientation3D.Backward);
		AssertOrientation(Orientation3D.UpRightBackward, XAxisOrientation3D.Right, YAxisOrientation3D.Up, ZAxisOrientation3D.Backward);
		AssertOrientation(Orientation3D.DownLeftForward, XAxisOrientation3D.Left, YAxisOrientation3D.Down, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.DownRightForward, XAxisOrientation3D.Right, YAxisOrientation3D.Down, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.DownLeftBackward, XAxisOrientation3D.Left, YAxisOrientation3D.Down, ZAxisOrientation3D.Backward);
		AssertOrientation(Orientation3D.DownRightBackward, XAxisOrientation3D.Right, YAxisOrientation3D.Down, ZAxisOrientation3D.Backward);
	}

	[Test]
	public void ShouldCorrectlyGetAxisSigns() {
		void AssertAxes(Orientation3D input, int expectedX, int expectedY, int expectedZ) {
			Assert.AreEqual(expectedX, input.GetXAxis().GetAxisSign());
			Assert.AreEqual(expectedY, input.GetYAxis().GetAxisSign());
			Assert.AreEqual(expectedZ, input.GetZAxis().GetAxisSign());
			Assert.AreEqual(expectedX, input.GetAxisSign(Axis.X));
			Assert.AreEqual(expectedY, input.GetAxisSign(Axis.Y));
			Assert.AreEqual(expectedZ, input.GetAxisSign(Axis.Z));
		}

		AssertAxes(Orientation3D.None, 0, 0, 0);

		AssertAxes(Orientation3D.Left, 1, 0, 0);
		AssertAxes(Orientation3D.Right, -1, 0, 0);
		AssertAxes(Orientation3D.Up, 0, 1, 0);
		AssertAxes(Orientation3D.Down, 0, -1, 0);
		AssertAxes(Orientation3D.Forward, 0, 0, 1);
		AssertAxes(Orientation3D.Backward, 0, 0, -1);

		AssertAxes(Orientation3D.UpLeft, 1, 1, 0);
		AssertAxes(Orientation3D.UpRight, -1, 1, 0);
		AssertAxes(Orientation3D.UpBackward, 0, 1, -1);
		AssertAxes(Orientation3D.UpForward, 0, 1, 1);
		AssertAxes(Orientation3D.DownLeft, 1, -1, 0);
		AssertAxes(Orientation3D.DownRight, -1, -1, 0);
		AssertAxes(Orientation3D.DownBackward, 0, -1, -1);
		AssertAxes(Orientation3D.DownForward, 0, -1, 1);
		AssertAxes(Orientation3D.LeftForward, 1, 0, 1);
		AssertAxes(Orientation3D.LeftBackward, 1, 0, -1);
		AssertAxes(Orientation3D.RightForward, -1, 0, 1);
		AssertAxes(Orientation3D.RightBackward, -1, 0, -1);

		AssertAxes(Orientation3D.UpLeftForward, 1, 1, 1);
		AssertAxes(Orientation3D.UpRightForward, -1, 1, 1);
		AssertAxes(Orientation3D.UpLeftBackward, 1, 1, -1);
		AssertAxes(Orientation3D.UpRightBackward, -1, 1, -1);
		AssertAxes(Orientation3D.DownLeftForward, 1, -1, 1);
		AssertAxes(Orientation3D.DownRightForward, -1, -1, 1);
		AssertAxes(Orientation3D.DownLeftBackward, 1, -1, -1);
		AssertAxes(Orientation3D.DownRightBackward, -1, -1, -1);
	}
}