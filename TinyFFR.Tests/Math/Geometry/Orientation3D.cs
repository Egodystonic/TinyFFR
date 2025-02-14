// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Runtime.CompilerServices;

namespace Egodystonic.TinyFFR;

[TestFixture]
class OrientationTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyCombineBits() {
		void AssertIntEquals<TEnum1, TEnum2>(TEnum1 expected, TEnum2 actual) where TEnum1 : Enum where TEnum2 : Enum {
			Assert.AreEqual((int) (object) expected, (int) (object) actual);
		}

		AssertIntEquals(CardinalOrientation.Left, XAxisOrientation.Left);
		AssertIntEquals(CardinalOrientation.Right, XAxisOrientation.Right);
		AssertIntEquals(CardinalOrientation.Up, YAxisOrientation.Up);
		AssertIntEquals(CardinalOrientation.Down, YAxisOrientation.Down);
		AssertIntEquals(CardinalOrientation.Forward, ZAxisOrientation.Forward);
		AssertIntEquals(CardinalOrientation.Backward, ZAxisOrientation.Backward);

		AssertIntEquals(IntercardinalOrientation.LeftUp, CardinalOrientation.Up | CardinalOrientation.Left);
		AssertIntEquals(IntercardinalOrientation.RightUp, CardinalOrientation.Up | CardinalOrientation.Right);
		AssertIntEquals(IntercardinalOrientation.UpForward, CardinalOrientation.Up | CardinalOrientation.Forward);
		AssertIntEquals(IntercardinalOrientation.UpBackward, CardinalOrientation.Up | CardinalOrientation.Backward);
		AssertIntEquals(IntercardinalOrientation.LeftDown, CardinalOrientation.Down | CardinalOrientation.Left);
		AssertIntEquals(IntercardinalOrientation.RightDown, CardinalOrientation.Down | CardinalOrientation.Right);
		AssertIntEquals(IntercardinalOrientation.DownForward, CardinalOrientation.Down | CardinalOrientation.Forward);
		AssertIntEquals(IntercardinalOrientation.DownBackward, CardinalOrientation.Down | CardinalOrientation.Backward);
		AssertIntEquals(IntercardinalOrientation.LeftForward, CardinalOrientation.Forward | CardinalOrientation.Left);
		AssertIntEquals(IntercardinalOrientation.LeftBackward, CardinalOrientation.Backward | CardinalOrientation.Left);
		AssertIntEquals(IntercardinalOrientation.RightForward, CardinalOrientation.Forward | CardinalOrientation.Right);
		AssertIntEquals(IntercardinalOrientation.RightBackward, CardinalOrientation.Backward | CardinalOrientation.Right);

		AssertIntEquals(DiagonalOrientation.LeftUpForward, CardinalOrientation.Up | CardinalOrientation.Left | CardinalOrientation.Forward);
		AssertIntEquals(DiagonalOrientation.RightUpForward, CardinalOrientation.Up | CardinalOrientation.Right | CardinalOrientation.Forward);
		AssertIntEquals(DiagonalOrientation.LeftUpBackward, CardinalOrientation.Up | CardinalOrientation.Left | CardinalOrientation.Backward);
		AssertIntEquals(DiagonalOrientation.RightUpBackward, CardinalOrientation.Up | CardinalOrientation.Right | CardinalOrientation.Backward);
		AssertIntEquals(DiagonalOrientation.LeftDownForward, CardinalOrientation.Down | CardinalOrientation.Left | CardinalOrientation.Forward);
		AssertIntEquals(DiagonalOrientation.RightDownForward, CardinalOrientation.Down | CardinalOrientation.Right | CardinalOrientation.Forward);
		AssertIntEquals(DiagonalOrientation.LeftDownBackward, CardinalOrientation.Down | CardinalOrientation.Left | CardinalOrientation.Backward);
		AssertIntEquals(DiagonalOrientation.RightDownBackward, CardinalOrientation.Down | CardinalOrientation.Right | CardinalOrientation.Backward);

		AssertIntEquals(Orientation.Left, CardinalOrientation.Left);
		AssertIntEquals(Orientation.Right, CardinalOrientation.Right);
		AssertIntEquals(Orientation.Up, CardinalOrientation.Up);
		AssertIntEquals(Orientation.Down, CardinalOrientation.Down);
		AssertIntEquals(Orientation.Forward, CardinalOrientation.Forward);
		AssertIntEquals(Orientation.Backward, CardinalOrientation.Backward);

		AssertIntEquals(Orientation.LeftUp, CardinalOrientation.Up | CardinalOrientation.Left);
		AssertIntEquals(Orientation.RightUp, CardinalOrientation.Up | CardinalOrientation.Right);
		AssertIntEquals(Orientation.UpBackward, CardinalOrientation.Up | CardinalOrientation.Backward);
		AssertIntEquals(Orientation.UpForward, CardinalOrientation.Up | CardinalOrientation.Forward);
		AssertIntEquals(Orientation.LeftDown, CardinalOrientation.Down | CardinalOrientation.Left);
		AssertIntEquals(Orientation.RightDown, CardinalOrientation.Down | CardinalOrientation.Right);
		AssertIntEquals(Orientation.DownBackward, CardinalOrientation.Down | CardinalOrientation.Backward);
		AssertIntEquals(Orientation.DownForward, CardinalOrientation.Down | CardinalOrientation.Forward);
		AssertIntEquals(Orientation.LeftForward, CardinalOrientation.Left | CardinalOrientation.Forward);
		AssertIntEquals(Orientation.LeftBackward, CardinalOrientation.Left | CardinalOrientation.Backward);
		AssertIntEquals(Orientation.RightForward, CardinalOrientation.Right | CardinalOrientation.Forward);
		AssertIntEquals(Orientation.RightBackward, CardinalOrientation.Right | CardinalOrientation.Backward);

		AssertIntEquals(Orientation.LeftUpForward, DiagonalOrientation.LeftUpForward);
		AssertIntEquals(Orientation.RightUpForward, DiagonalOrientation.RightUpForward);
		AssertIntEquals(Orientation.LeftUpBackward, DiagonalOrientation.LeftUpBackward);
		AssertIntEquals(Orientation.RightUpBackward, DiagonalOrientation.RightUpBackward);
		AssertIntEquals(Orientation.LeftDownForward, DiagonalOrientation.LeftDownForward);
		AssertIntEquals(Orientation.RightDownForward, DiagonalOrientation.RightDownForward);
		AssertIntEquals(Orientation.LeftDownBackward, DiagonalOrientation.LeftDownBackward);
		AssertIntEquals(Orientation.RightDownBackward, DiagonalOrientation.RightDownBackward);
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

		AssertForEnum<XAxisOrientation, CardinalOrientation>(v => v.AsCardinalOrientation());
		AssertForEnum<YAxisOrientation, CardinalOrientation>(v => v.AsCardinalOrientation());
		AssertForEnum<ZAxisOrientation, CardinalOrientation>(v => v.AsCardinalOrientation());
		AssertForEnum<XAxisOrientation, Orientation>(v => v.AsGeneralOrientation());
		AssertForEnum<YAxisOrientation, Orientation>(v => v.AsGeneralOrientation());
		AssertForEnum<ZAxisOrientation, Orientation>(v => v.AsGeneralOrientation());

		AssertForEnum<CardinalOrientation, Orientation>(v => v.AsGeneralOrientation());
		AssertForEnum<DiagonalOrientation, Orientation>(v => v.AsGeneralOrientation());
		AssertForEnum<IntercardinalOrientation, Orientation>(v => v.AsGeneralOrientation());
	}

	[Test]
	public void ShouldCorrectlyAscertainWhetherIsCardinal() {
		foreach (var orientation in Enum.GetValues<Orientation>()) {
			if (orientation == Orientation.None) continue;
			Assert.AreEqual(((int[]) Enum.GetValuesAsUnderlyingType<CardinalOrientation>()).Contains((int) orientation), orientation.IsCardinal());
		}
	}
	[Test]
	public void ShouldCorrectlyAscertainWhetherIsDiagonal() {
		foreach (var orientation in Enum.GetValues<Orientation>()) {
			if (orientation == Orientation.None) continue;
			Assert.AreEqual(((int[]) Enum.GetValuesAsUnderlyingType<DiagonalOrientation>()).Contains((int) orientation), orientation.IsDiagonal());
		}
	}
	[Test]
	public void ShouldCorrectlyAscertainWhetherIsIntercardinal() {
		foreach (var orientation in Enum.GetValues<Orientation>()) {
			if (orientation == Orientation.None) continue;
			Assert.AreEqual(((int[]) Enum.GetValuesAsUnderlyingType<IntercardinalOrientation>()).Contains((int) orientation), orientation.IsIntercardinal());
		}
	}

	[Test]
	public void ShouldCorrectlyConvertToDirection() {
		Assert.AreEqual(Direction.None, XAxisOrientation.None.ToDirection());
		Assert.AreEqual(Direction.Left, XAxisOrientation.Left.ToDirection());
		Assert.AreEqual(Direction.Right, XAxisOrientation.Right.ToDirection());

		Assert.AreEqual(Direction.None, YAxisOrientation.None.ToDirection());
		Assert.AreEqual(Direction.Up, YAxisOrientation.Up.ToDirection());
		Assert.AreEqual(Direction.Down, YAxisOrientation.Down.ToDirection());

		Assert.AreEqual(Direction.None, ZAxisOrientation.None.ToDirection());
		Assert.AreEqual(Direction.Forward, ZAxisOrientation.Forward.ToDirection());
		Assert.AreEqual(Direction.Backward, ZAxisOrientation.Backward.ToDirection());

		Assert.AreEqual(Direction.None, CardinalOrientation.None.ToDirection());
		Assert.AreEqual(Direction.Left, CardinalOrientation.Left.ToDirection());
		Assert.AreEqual(Direction.Right, CardinalOrientation.Right.ToDirection());
		Assert.AreEqual(Direction.Up, CardinalOrientation.Up.ToDirection());
		Assert.AreEqual(Direction.Down, CardinalOrientation.Down.ToDirection());
		Assert.AreEqual(Direction.Forward, CardinalOrientation.Forward.ToDirection());
		Assert.AreEqual(Direction.Backward, CardinalOrientation.Backward.ToDirection());
	}

	[Test]
	public void ShouldCorrectlyAscertainAxis() {
		Assert.AreEqual(Axis.None, CardinalOrientation.None.GetAxis());
		Assert.AreEqual(Axis.X, CardinalOrientation.Left.GetAxis());
		Assert.AreEqual(Axis.X, CardinalOrientation.Right.GetAxis());
		Assert.AreEqual(Axis.Y, CardinalOrientation.Up.GetAxis());
		Assert.AreEqual(Axis.Y, CardinalOrientation.Down.GetAxis());
		Assert.AreEqual(Axis.Z, CardinalOrientation.Forward.GetAxis());
		Assert.AreEqual(Axis.Z, CardinalOrientation.Backward.GetAxis());
	}

	[Test]
	public void ShouldCorrectlyAscertainUnspecifiedAxis() {
		Assert.AreEqual(Axis.None, IntercardinalOrientation.None.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Z, IntercardinalOrientation.LeftUp.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Z, IntercardinalOrientation.RightUp.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.X, IntercardinalOrientation.UpForward.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.X, IntercardinalOrientation.UpBackward.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Z, IntercardinalOrientation.LeftDown.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Z, IntercardinalOrientation.RightDown.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.X, IntercardinalOrientation.DownForward.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.X, IntercardinalOrientation.DownBackward.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Y, IntercardinalOrientation.LeftForward.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Y, IntercardinalOrientation.LeftBackward.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Y, IntercardinalOrientation.RightForward.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Y, IntercardinalOrientation.RightBackward.GetUnspecifiedAxis());
	}

	[Test]
	public void ShouldCorrectlyGrabAxes() {
		void AssertOrientation(Orientation input, XAxisOrientation expectedX, YAxisOrientation expectedY, ZAxisOrientation expectedZ) {
			Assert.AreEqual(expectedX, input.GetXAxis());
			Assert.AreEqual(expectedY, input.GetYAxis());
			Assert.AreEqual(expectedZ, input.GetZAxis());

			if (input.IsDiagonal()) {
				var diag = (DiagonalOrientation) input;
				Assert.AreEqual(expectedX, diag.GetXAxis());
				Assert.AreEqual(expectedY, diag.GetYAxis());
				Assert.AreEqual(expectedZ, diag.GetZAxis());
			}

			if (input.IsIntercardinal()) {
				var ic = (IntercardinalOrientation) input;
				Assert.AreEqual(expectedX, ic.GetXAxis());
				Assert.AreEqual(expectedY, ic.GetYAxis());
				Assert.AreEqual(expectedZ, ic.GetZAxis());
			}
		}

		AssertOrientation(Orientation.None, XAxisOrientation.None, YAxisOrientation.None, ZAxisOrientation.None);

		AssertOrientation(Orientation.Left, XAxisOrientation.Left, YAxisOrientation.None, ZAxisOrientation.None);
		AssertOrientation(Orientation.Right, XAxisOrientation.Right, YAxisOrientation.None, ZAxisOrientation.None);
		AssertOrientation(Orientation.Up, XAxisOrientation.None, YAxisOrientation.Up, ZAxisOrientation.None);
		AssertOrientation(Orientation.Down, XAxisOrientation.None, YAxisOrientation.Down, ZAxisOrientation.None);
		AssertOrientation(Orientation.Forward, XAxisOrientation.None, YAxisOrientation.None, ZAxisOrientation.Forward);
		AssertOrientation(Orientation.Backward, XAxisOrientation.None, YAxisOrientation.None, ZAxisOrientation.Backward);

		AssertOrientation(Orientation.LeftUp, XAxisOrientation.Left, YAxisOrientation.Up, ZAxisOrientation.None);
		AssertOrientation(Orientation.RightUp, XAxisOrientation.Right, YAxisOrientation.Up, ZAxisOrientation.None);
		AssertOrientation(Orientation.UpBackward, XAxisOrientation.None, YAxisOrientation.Up, ZAxisOrientation.Backward);
		AssertOrientation(Orientation.UpForward, XAxisOrientation.None, YAxisOrientation.Up, ZAxisOrientation.Forward);
		AssertOrientation(Orientation.LeftDown, XAxisOrientation.Left, YAxisOrientation.Down, ZAxisOrientation.None);
		AssertOrientation(Orientation.RightDown, XAxisOrientation.Right, YAxisOrientation.Down, ZAxisOrientation.None);
		AssertOrientation(Orientation.DownBackward, XAxisOrientation.None, YAxisOrientation.Down, ZAxisOrientation.Backward);
		AssertOrientation(Orientation.DownForward, XAxisOrientation.None, YAxisOrientation.Down, ZAxisOrientation.Forward);
		AssertOrientation(Orientation.LeftForward, XAxisOrientation.Left, YAxisOrientation.None, ZAxisOrientation.Forward);
		AssertOrientation(Orientation.LeftBackward, XAxisOrientation.Left, YAxisOrientation.None, ZAxisOrientation.Backward);
		AssertOrientation(Orientation.RightForward, XAxisOrientation.Right, YAxisOrientation.None, ZAxisOrientation.Forward);
		AssertOrientation(Orientation.RightBackward, XAxisOrientation.Right, YAxisOrientation.None, ZAxisOrientation.Backward);

		AssertOrientation(Orientation.LeftUpForward, XAxisOrientation.Left, YAxisOrientation.Up, ZAxisOrientation.Forward);
		AssertOrientation(Orientation.RightUpForward, XAxisOrientation.Right, YAxisOrientation.Up, ZAxisOrientation.Forward);
		AssertOrientation(Orientation.LeftUpBackward, XAxisOrientation.Left, YAxisOrientation.Up, ZAxisOrientation.Backward);
		AssertOrientation(Orientation.RightUpBackward, XAxisOrientation.Right, YAxisOrientation.Up, ZAxisOrientation.Backward);
		AssertOrientation(Orientation.LeftDownForward, XAxisOrientation.Left, YAxisOrientation.Down, ZAxisOrientation.Forward);
		AssertOrientation(Orientation.RightDownForward, XAxisOrientation.Right, YAxisOrientation.Down, ZAxisOrientation.Forward);
		AssertOrientation(Orientation.LeftDownBackward, XAxisOrientation.Left, YAxisOrientation.Down, ZAxisOrientation.Backward);
		AssertOrientation(Orientation.RightDownBackward, XAxisOrientation.Right, YAxisOrientation.Down, ZAxisOrientation.Backward);
	}

	[Test]
	public void ShouldCorrectlyGetAxisSigns() {
		void AssertAxes(Orientation input, int expectedX, int expectedY, int expectedZ) {
			Assert.AreEqual(expectedX, input.GetXAxis().GetAxisSign());
			Assert.AreEqual(expectedY, input.GetYAxis().GetAxisSign());
			Assert.AreEqual(expectedZ, input.GetZAxis().GetAxisSign());
			Assert.AreEqual(expectedX, input.GetAxisSign(Axis.X));
			Assert.AreEqual(expectedY, input.GetAxisSign(Axis.Y));
			Assert.AreEqual(expectedZ, input.GetAxisSign(Axis.Z));

			if (input.IsCardinal()) {
				Assert.AreEqual(expectedX, ((CardinalOrientation) input).GetAxisSign(Axis.X));
				Assert.AreEqual(expectedY, ((CardinalOrientation) input).GetAxisSign(Axis.Y));
				Assert.AreEqual(expectedZ, ((CardinalOrientation) input).GetAxisSign(Axis.Z));
			}

			if (input.IsIntercardinal()) {
				Assert.AreEqual(expectedX, ((IntercardinalOrientation) input).GetAxisSign(Axis.X));
				Assert.AreEqual(expectedY, ((IntercardinalOrientation) input).GetAxisSign(Axis.Y));
				Assert.AreEqual(expectedZ, ((IntercardinalOrientation) input).GetAxisSign(Axis.Z));
			}

			if (input.IsDiagonal()) {
				Assert.AreEqual(expectedX, ((DiagonalOrientation) input).GetAxisSign(Axis.X));
				Assert.AreEqual(expectedY, ((DiagonalOrientation) input).GetAxisSign(Axis.Y));
				Assert.AreEqual(expectedZ, ((DiagonalOrientation) input).GetAxisSign(Axis.Z));
			}
		}

		AssertAxes(Orientation.None, 0, 0, 0);

		AssertAxes(Orientation.Left, 1, 0, 0);
		AssertAxes(Orientation.Right, -1, 0, 0);
		AssertAxes(Orientation.Up, 0, 1, 0);
		AssertAxes(Orientation.Down, 0, -1, 0);
		AssertAxes(Orientation.Forward, 0, 0, 1);
		AssertAxes(Orientation.Backward, 0, 0, -1);

		AssertAxes(Orientation.LeftUp, 1, 1, 0);
		AssertAxes(Orientation.RightUp, -1, 1, 0);
		AssertAxes(Orientation.UpBackward, 0, 1, -1);
		AssertAxes(Orientation.UpForward, 0, 1, 1);
		AssertAxes(Orientation.LeftDown, 1, -1, 0);
		AssertAxes(Orientation.RightDown, -1, -1, 0);
		AssertAxes(Orientation.DownBackward, 0, -1, -1);
		AssertAxes(Orientation.DownForward, 0, -1, 1);
		AssertAxes(Orientation.LeftForward, 1, 0, 1);
		AssertAxes(Orientation.LeftBackward, 1, 0, -1);
		AssertAxes(Orientation.RightForward, -1, 0, 1);
		AssertAxes(Orientation.RightBackward, -1, 0, -1);

		AssertAxes(Orientation.LeftUpForward, 1, 1, 1);
		AssertAxes(Orientation.RightUpForward, -1, 1, 1);
		AssertAxes(Orientation.LeftUpBackward, 1, 1, -1);
		AssertAxes(Orientation.RightUpBackward, -1, 1, -1);
		AssertAxes(Orientation.LeftDownForward, 1, -1, 1);
		AssertAxes(Orientation.RightDownForward, -1, -1, 1);
		AssertAxes(Orientation.LeftDownBackward, 1, -1, -1);
		AssertAxes(Orientation.RightDownBackward, -1, -1, -1);
	}

	[Test]
	public void ShouldCorrectlyReplaceAxisSigns() { // Wrote this really tired, sorry. I know it sucks
		foreach (var orientation in Enum.GetValues<Orientation>()) {
			foreach (var axis in OrientationUtils.AllAxes) {
				var posOrientation = orientation.WithAxisSign(axis, 1);
				var zeroOrientation = orientation.WithAxisSign(axis, 0);
				var negOrientation = orientation.WithAxisSign(axis, -1);

				Assert.AreEqual(1, posOrientation.GetAxisSign(axis));
				Assert.AreEqual(0, zeroOrientation.GetAxisSign(axis));
				Assert.AreEqual(-1, negOrientation.GetAxisSign(axis));

				foreach (var otherAxis in OrientationUtils.AllAxes) {
					if (otherAxis == axis) continue;
					Assert.AreEqual(orientation.GetAxisSign(otherAxis), posOrientation.GetAxisSign(otherAxis));
					Assert.AreEqual(orientation.GetAxisSign(otherAxis), zeroOrientation.GetAxisSign(otherAxis));
					Assert.AreEqual(orientation.GetAxisSign(otherAxis), negOrientation.GetAxisSign(otherAxis));
				}
			}

			Assert.AreEqual(orientation, orientation.WithAxisSign(Axis.None, -1));
			Assert.AreEqual(orientation, orientation.WithAxisSign(Axis.None, 0));
			Assert.AreEqual(orientation, orientation.WithAxisSign(Axis.None, 1));
		}

		// Loop above covers everything, these tests are just additional manual sanity checks
		Assert.AreEqual(Orientation.LeftUpForward, Orientation.RightUpForward.WithAxisSign(Axis.X, 1));
		Assert.AreEqual(Orientation.LeftUpForward, Orientation.LeftDownForward.WithAxisSign(Axis.Y, 1));
		Assert.AreEqual(Orientation.LeftUpForward, Orientation.LeftUpBackward.WithAxisSign(Axis.Z, 1));

		Assert.AreEqual(Orientation.RightUpForward, Orientation.LeftUpForward.WithAxisSign(Axis.X, -1));
		Assert.AreEqual(Orientation.LeftDownForward, Orientation.LeftUpForward.WithAxisSign(Axis.Y, -1));
		Assert.AreEqual(Orientation.LeftUpBackward, Orientation.LeftUpForward.WithAxisSign(Axis.Z, -1));

		Assert.AreEqual(Orientation.LeftUpForward, Orientation.LeftUpForward.WithAxisSign(Axis.X, 1));
		Assert.AreEqual(Orientation.LeftUpForward, Orientation.LeftUpForward.WithAxisSign(Axis.Y, 1));
		Assert.AreEqual(Orientation.LeftUpForward, Orientation.LeftUpForward.WithAxisSign(Axis.Z, 1));

		Assert.AreEqual(Orientation.UpForward, Orientation.LeftUpForward.WithAxisSign(Axis.X, 0));
		Assert.AreEqual(Orientation.LeftForward, Orientation.LeftUpForward.WithAxisSign(Axis.Y, 0));
		Assert.AreEqual(Orientation.LeftUp, Orientation.LeftUpForward.WithAxisSign(Axis.Z, 0));
	}

	[Test]
	public void ShouldCorrectlyCombine() {
		void AssertPair<T1, T2>(Orientation expectation, T1 input1, T2 input2) where T1 : notnull where T2 : notnull {
			var actual = Orientation3DExtensions.Plus((dynamic) input1, (dynamic) input2);
			Assert.AreEqual(expectation, actual);

			actual = Orientation3DExtensions.Plus((dynamic) input2, (dynamic) input1);
			Assert.AreEqual(expectation, actual);
		}
		void AssertTrio<T1, T2, T3>(Orientation expectation, T1 input1, T2 input2, T3 input3) where T1 : notnull where T2 : notnull where T3 : notnull {
			var actual = Orientation3DExtensions.Plus((dynamic) input1, (dynamic) input2, (dynamic) input3);
			Assert.AreEqual(expectation, actual);
			actual = Orientation3DExtensions.Plus((dynamic) input1, (dynamic) input3, (dynamic) input2);
			Assert.AreEqual(expectation, actual);

			actual = Orientation3DExtensions.Plus((dynamic) input2, (dynamic) input1, (dynamic) input3);
			Assert.AreEqual(expectation, actual);
			actual = Orientation3DExtensions.Plus((dynamic) input2, (dynamic) input3, (dynamic) input1);
			Assert.AreEqual(expectation, actual);

			actual = Orientation3DExtensions.Plus((dynamic) input3, (dynamic) input1, (dynamic) input2);
			Assert.AreEqual(expectation, actual);
			actual = Orientation3DExtensions.Plus((dynamic) input3, (dynamic) input2, (dynamic) input1);
			Assert.AreEqual(expectation, actual);
		}

		foreach (var x in Enum.GetValues<XAxisOrientation>()) {
			foreach (var y in Enum.GetValues<YAxisOrientation>()) {
				foreach (var z in Enum.GetValues<ZAxisOrientation>()) {
					AssertPair((Orientation) ((int) x | (int) y), x, y);
					AssertPair((Orientation) ((int) x | (int) z), x, z);
					AssertPair((Orientation) ((int) y | (int) z), y, z);
					AssertTrio((Orientation) ((int) x | (int) y | (int) z), x, y, z);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyCreateAxesFromSigns() {
		Assert.AreEqual(XAxisOrientation.None, Orientation3DExtensions.CreateXAxisOrientationFromValueSign(0));
		Assert.AreEqual(XAxisOrientation.Left, Orientation3DExtensions.CreateXAxisOrientationFromValueSign(1));
		Assert.AreEqual(XAxisOrientation.Right, Orientation3DExtensions.CreateXAxisOrientationFromValueSign(-1));

		Assert.AreEqual(YAxisOrientation.None, Orientation3DExtensions.CreateYAxisOrientationFromValueSign(0));
		Assert.AreEqual(YAxisOrientation.Up, Orientation3DExtensions.CreateYAxisOrientationFromValueSign(1));
		Assert.AreEqual(YAxisOrientation.Down, Orientation3DExtensions.CreateYAxisOrientationFromValueSign(-1));

		Assert.AreEqual(ZAxisOrientation.None, Orientation3DExtensions.CreateZAxisOrientationFromValueSign(0));
		Assert.AreEqual(ZAxisOrientation.Forward, Orientation3DExtensions.CreateZAxisOrientationFromValueSign(1));
		Assert.AreEqual(ZAxisOrientation.Backward, Orientation3DExtensions.CreateZAxisOrientationFromValueSign(-1));
	}
}