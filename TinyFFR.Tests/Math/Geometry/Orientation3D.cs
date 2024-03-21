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

		AssertIntEquals(CardinalOrientation3D.Left, XAxisOrientation3D.Left);
		AssertIntEquals(CardinalOrientation3D.Right, XAxisOrientation3D.Right);
		AssertIntEquals(CardinalOrientation3D.Up, YAxisOrientation3D.Up);
		AssertIntEquals(CardinalOrientation3D.Down, YAxisOrientation3D.Down);
		AssertIntEquals(CardinalOrientation3D.Forward, ZAxisOrientation3D.Forward);
		AssertIntEquals(CardinalOrientation3D.Backward, ZAxisOrientation3D.Backward);

		AssertIntEquals(IntercardinalOrientation3D.LeftUp, CardinalOrientation3D.Up | CardinalOrientation3D.Left);
		AssertIntEquals(IntercardinalOrientation3D.RightUp, CardinalOrientation3D.Up | CardinalOrientation3D.Right);
		AssertIntEquals(IntercardinalOrientation3D.UpForward, CardinalOrientation3D.Up | CardinalOrientation3D.Forward);
		AssertIntEquals(IntercardinalOrientation3D.UpBackward, CardinalOrientation3D.Up | CardinalOrientation3D.Backward);
		AssertIntEquals(IntercardinalOrientation3D.LeftDown, CardinalOrientation3D.Down | CardinalOrientation3D.Left);
		AssertIntEquals(IntercardinalOrientation3D.RightDown, CardinalOrientation3D.Down | CardinalOrientation3D.Right);
		AssertIntEquals(IntercardinalOrientation3D.DownForward, CardinalOrientation3D.Down | CardinalOrientation3D.Forward);
		AssertIntEquals(IntercardinalOrientation3D.DownBackward, CardinalOrientation3D.Down | CardinalOrientation3D.Backward);
		AssertIntEquals(IntercardinalOrientation3D.LeftForward, CardinalOrientation3D.Forward | CardinalOrientation3D.Left);
		AssertIntEquals(IntercardinalOrientation3D.LeftBackward, CardinalOrientation3D.Backward | CardinalOrientation3D.Left);
		AssertIntEquals(IntercardinalOrientation3D.RightForward, CardinalOrientation3D.Forward | CardinalOrientation3D.Right);
		AssertIntEquals(IntercardinalOrientation3D.RightBackward, CardinalOrientation3D.Backward | CardinalOrientation3D.Right);

		AssertIntEquals(DiagonalOrientation3D.LeftUpForward, CardinalOrientation3D.Up | CardinalOrientation3D.Left | CardinalOrientation3D.Forward);
		AssertIntEquals(DiagonalOrientation3D.RightUpForward, CardinalOrientation3D.Up | CardinalOrientation3D.Right | CardinalOrientation3D.Forward);
		AssertIntEquals(DiagonalOrientation3D.LeftUpBackward, CardinalOrientation3D.Up | CardinalOrientation3D.Left | CardinalOrientation3D.Backward);
		AssertIntEquals(DiagonalOrientation3D.RightUpBackward, CardinalOrientation3D.Up | CardinalOrientation3D.Right | CardinalOrientation3D.Backward);
		AssertIntEquals(DiagonalOrientation3D.LeftDownForward, CardinalOrientation3D.Down | CardinalOrientation3D.Left | CardinalOrientation3D.Forward);
		AssertIntEquals(DiagonalOrientation3D.RightDownForward, CardinalOrientation3D.Down | CardinalOrientation3D.Right | CardinalOrientation3D.Forward);
		AssertIntEquals(DiagonalOrientation3D.LeftDownBackward, CardinalOrientation3D.Down | CardinalOrientation3D.Left | CardinalOrientation3D.Backward);
		AssertIntEquals(DiagonalOrientation3D.RightDownBackward, CardinalOrientation3D.Down | CardinalOrientation3D.Right | CardinalOrientation3D.Backward);

		AssertIntEquals(Orientation3D.Left, CardinalOrientation3D.Left);
		AssertIntEquals(Orientation3D.Right, CardinalOrientation3D.Right);
		AssertIntEquals(Orientation3D.Up, CardinalOrientation3D.Up);
		AssertIntEquals(Orientation3D.Down, CardinalOrientation3D.Down);
		AssertIntEquals(Orientation3D.Forward, CardinalOrientation3D.Forward);
		AssertIntEquals(Orientation3D.Backward, CardinalOrientation3D.Backward);

		AssertIntEquals(Orientation3D.LeftUp, CardinalOrientation3D.Up | CardinalOrientation3D.Left);
		AssertIntEquals(Orientation3D.RightUp, CardinalOrientation3D.Up | CardinalOrientation3D.Right);
		AssertIntEquals(Orientation3D.UpBackward, CardinalOrientation3D.Up | CardinalOrientation3D.Backward);
		AssertIntEquals(Orientation3D.UpForward, CardinalOrientation3D.Up | CardinalOrientation3D.Forward);
		AssertIntEquals(Orientation3D.LeftDown, CardinalOrientation3D.Down | CardinalOrientation3D.Left);
		AssertIntEquals(Orientation3D.RightDown, CardinalOrientation3D.Down | CardinalOrientation3D.Right);
		AssertIntEquals(Orientation3D.DownBackward, CardinalOrientation3D.Down | CardinalOrientation3D.Backward);
		AssertIntEquals(Orientation3D.DownForward, CardinalOrientation3D.Down | CardinalOrientation3D.Forward);
		AssertIntEquals(Orientation3D.LeftForward, CardinalOrientation3D.Left | CardinalOrientation3D.Forward);
		AssertIntEquals(Orientation3D.LeftBackward, CardinalOrientation3D.Left | CardinalOrientation3D.Backward);
		AssertIntEquals(Orientation3D.RightForward, CardinalOrientation3D.Right | CardinalOrientation3D.Forward);
		AssertIntEquals(Orientation3D.RightBackward, CardinalOrientation3D.Right | CardinalOrientation3D.Backward);

		AssertIntEquals(Orientation3D.LeftUpForward, DiagonalOrientation3D.LeftUpForward);
		AssertIntEquals(Orientation3D.RightUpForward, DiagonalOrientation3D.RightUpForward);
		AssertIntEquals(Orientation3D.LeftUpBackward, DiagonalOrientation3D.LeftUpBackward);
		AssertIntEquals(Orientation3D.RightUpBackward, DiagonalOrientation3D.RightUpBackward);
		AssertIntEquals(Orientation3D.LeftDownForward, DiagonalOrientation3D.LeftDownForward);
		AssertIntEquals(Orientation3D.RightDownForward, DiagonalOrientation3D.RightDownForward);
		AssertIntEquals(Orientation3D.LeftDownBackward, DiagonalOrientation3D.LeftDownBackward);
		AssertIntEquals(Orientation3D.RightDownBackward, DiagonalOrientation3D.RightDownBackward);
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
		AssertForEnum<IntercardinalOrientation3D, Orientation3D>(v => v.AsGeneralOrientation());
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
	public void ShouldCorrectlyAscertainWhetherIsIntercardinal() {
		foreach (var orientation in Enum.GetValues<Orientation3D>()) {
			if (orientation == Orientation3D.None) continue;
			Assert.AreEqual(((int[]) Enum.GetValuesAsUnderlyingType<IntercardinalOrientation3D>()).Contains((int) orientation), orientation.IsIntercardinal());
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
	public void ShouldCorrectlyAscertainUnspecifiedAxis() {
		Assert.AreEqual(Axis.None, IntercardinalOrientation3D.None.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Z, IntercardinalOrientation3D.LeftUp.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Z, IntercardinalOrientation3D.RightUp.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.X, IntercardinalOrientation3D.UpForward.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.X, IntercardinalOrientation3D.UpBackward.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Z, IntercardinalOrientation3D.LeftDown.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Z, IntercardinalOrientation3D.RightDown.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.X, IntercardinalOrientation3D.DownForward.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.X, IntercardinalOrientation3D.DownBackward.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Y, IntercardinalOrientation3D.LeftForward.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Y, IntercardinalOrientation3D.LeftBackward.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Y, IntercardinalOrientation3D.RightForward.GetUnspecifiedAxis());
		Assert.AreEqual(Axis.Y, IntercardinalOrientation3D.RightBackward.GetUnspecifiedAxis());
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

			if (input.IsIntercardinal()) {
				var ic = (IntercardinalOrientation3D) input;
				Assert.AreEqual(expectedX, ic.GetXAxis());
				Assert.AreEqual(expectedY, ic.GetYAxis());
				Assert.AreEqual(expectedZ, ic.GetZAxis());
			}
		}

		AssertOrientation(Orientation3D.None, XAxisOrientation3D.None, YAxisOrientation3D.None, ZAxisOrientation3D.None);

		AssertOrientation(Orientation3D.Left, XAxisOrientation3D.Left, YAxisOrientation3D.None, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.Right, XAxisOrientation3D.Right, YAxisOrientation3D.None, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.Up, XAxisOrientation3D.None, YAxisOrientation3D.Up, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.Down, XAxisOrientation3D.None, YAxisOrientation3D.Down, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.Forward, XAxisOrientation3D.None, YAxisOrientation3D.None, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.Backward, XAxisOrientation3D.None, YAxisOrientation3D.None, ZAxisOrientation3D.Backward);

		AssertOrientation(Orientation3D.LeftUp, XAxisOrientation3D.Left, YAxisOrientation3D.Up, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.RightUp, XAxisOrientation3D.Right, YAxisOrientation3D.Up, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.UpBackward, XAxisOrientation3D.None, YAxisOrientation3D.Up, ZAxisOrientation3D.Backward);
		AssertOrientation(Orientation3D.UpForward, XAxisOrientation3D.None, YAxisOrientation3D.Up, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.LeftDown, XAxisOrientation3D.Left, YAxisOrientation3D.Down, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.RightDown, XAxisOrientation3D.Right, YAxisOrientation3D.Down, ZAxisOrientation3D.None);
		AssertOrientation(Orientation3D.DownBackward, XAxisOrientation3D.None, YAxisOrientation3D.Down, ZAxisOrientation3D.Backward);
		AssertOrientation(Orientation3D.DownForward, XAxisOrientation3D.None, YAxisOrientation3D.Down, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.LeftForward, XAxisOrientation3D.Left, YAxisOrientation3D.None, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.LeftBackward, XAxisOrientation3D.Left, YAxisOrientation3D.None, ZAxisOrientation3D.Backward);
		AssertOrientation(Orientation3D.RightForward, XAxisOrientation3D.Right, YAxisOrientation3D.None, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.RightBackward, XAxisOrientation3D.Right, YAxisOrientation3D.None, ZAxisOrientation3D.Backward);

		AssertOrientation(Orientation3D.LeftUpForward, XAxisOrientation3D.Left, YAxisOrientation3D.Up, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.RightUpForward, XAxisOrientation3D.Right, YAxisOrientation3D.Up, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.LeftUpBackward, XAxisOrientation3D.Left, YAxisOrientation3D.Up, ZAxisOrientation3D.Backward);
		AssertOrientation(Orientation3D.RightUpBackward, XAxisOrientation3D.Right, YAxisOrientation3D.Up, ZAxisOrientation3D.Backward);
		AssertOrientation(Orientation3D.LeftDownForward, XAxisOrientation3D.Left, YAxisOrientation3D.Down, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.RightDownForward, XAxisOrientation3D.Right, YAxisOrientation3D.Down, ZAxisOrientation3D.Forward);
		AssertOrientation(Orientation3D.LeftDownBackward, XAxisOrientation3D.Left, YAxisOrientation3D.Down, ZAxisOrientation3D.Backward);
		AssertOrientation(Orientation3D.RightDownBackward, XAxisOrientation3D.Right, YAxisOrientation3D.Down, ZAxisOrientation3D.Backward);
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

			if (input.IsCardinal()) {
				Assert.AreEqual(expectedX, ((CardinalOrientation3D) input).GetAxisSign(Axis.X));
				Assert.AreEqual(expectedY, ((CardinalOrientation3D) input).GetAxisSign(Axis.Y));
				Assert.AreEqual(expectedZ, ((CardinalOrientation3D) input).GetAxisSign(Axis.Z));
			}

			if (input.IsIntercardinal()) {
				Assert.AreEqual(expectedX, ((IntercardinalOrientation3D) input).GetAxisSign(Axis.X));
				Assert.AreEqual(expectedY, ((IntercardinalOrientation3D) input).GetAxisSign(Axis.Y));
				Assert.AreEqual(expectedZ, ((IntercardinalOrientation3D) input).GetAxisSign(Axis.Z));
			}

			if (input.IsDiagonal()) {
				Assert.AreEqual(expectedX, ((DiagonalOrientation3D) input).GetAxisSign(Axis.X));
				Assert.AreEqual(expectedY, ((DiagonalOrientation3D) input).GetAxisSign(Axis.Y));
				Assert.AreEqual(expectedZ, ((DiagonalOrientation3D) input).GetAxisSign(Axis.Z));
			}
		}

		AssertAxes(Orientation3D.None, 0, 0, 0);

		AssertAxes(Orientation3D.Left, 1, 0, 0);
		AssertAxes(Orientation3D.Right, -1, 0, 0);
		AssertAxes(Orientation3D.Up, 0, 1, 0);
		AssertAxes(Orientation3D.Down, 0, -1, 0);
		AssertAxes(Orientation3D.Forward, 0, 0, 1);
		AssertAxes(Orientation3D.Backward, 0, 0, -1);

		AssertAxes(Orientation3D.LeftUp, 1, 1, 0);
		AssertAxes(Orientation3D.RightUp, -1, 1, 0);
		AssertAxes(Orientation3D.UpBackward, 0, 1, -1);
		AssertAxes(Orientation3D.UpForward, 0, 1, 1);
		AssertAxes(Orientation3D.LeftDown, 1, -1, 0);
		AssertAxes(Orientation3D.RightDown, -1, -1, 0);
		AssertAxes(Orientation3D.DownBackward, 0, -1, -1);
		AssertAxes(Orientation3D.DownForward, 0, -1, 1);
		AssertAxes(Orientation3D.LeftForward, 1, 0, 1);
		AssertAxes(Orientation3D.LeftBackward, 1, 0, -1);
		AssertAxes(Orientation3D.RightForward, -1, 0, 1);
		AssertAxes(Orientation3D.RightBackward, -1, 0, -1);

		AssertAxes(Orientation3D.LeftUpForward, 1, 1, 1);
		AssertAxes(Orientation3D.RightUpForward, -1, 1, 1);
		AssertAxes(Orientation3D.LeftUpBackward, 1, 1, -1);
		AssertAxes(Orientation3D.RightUpBackward, -1, 1, -1);
		AssertAxes(Orientation3D.LeftDownForward, 1, -1, 1);
		AssertAxes(Orientation3D.RightDownForward, -1, -1, 1);
		AssertAxes(Orientation3D.LeftDownBackward, 1, -1, -1);
		AssertAxes(Orientation3D.RightDownBackward, -1, -1, -1);
	}

	[Test]
	public void ShouldCorrectlyReplaceAxisSigns() { // Wrote this really tired, sorry. I know it sucks
		foreach (var orientation in Enum.GetValues<Orientation3D>()) {
			foreach (var axis in OrientationUtils.AllAxes) {
				var posOrientation = orientation.WithAxisSign(axis, -1);
				var zeroOrientation = orientation.WithAxisSign(axis, 0);
				var negOrientation = orientation.WithAxisSign(axis, 1);

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
	}

	[Test]
	public void ShouldCorrectlyCombine() {
		void AssertPair<T1, T2>(Orientation3D expectation, T1 input1, T2 input2) where T1 : notnull where T2 : notnull {
			var actual = Orientation3DExtensions.Plus((dynamic) input1, (dynamic) input2);
			Assert.AreEqual(expectation, actual);

			actual = Orientation3DExtensions.Plus((dynamic) input2, (dynamic) input1);
			Assert.AreEqual(expectation, actual);
		}
		void AssertTrio<T1, T2, T3>(Orientation3D expectation, T1 input1, T2 input2, T3 input3) where T1 : notnull where T2 : notnull where T3 : notnull {
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

		foreach (var x in Enum.GetValues<XAxisOrientation3D>()) {
			foreach (var y in Enum.GetValues<YAxisOrientation3D>()) {
				foreach (var z in Enum.GetValues<ZAxisOrientation3D>()) {
					AssertPair((Orientation3D) ((int) x | (int) y), x, y);
					AssertPair((Orientation3D) ((int) x | (int) z), x, z);
					AssertPair((Orientation3D) ((int) y | (int) z), y, z);
					AssertTrio((Orientation3D) ((int) x | (int) y | (int) z), x, y, z);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyCreateAxesFromSigns() {
		Assert.AreEqual(XAxisOrientation3D.None, Orientation3DExtensions.CreateXAxisOrientationFromValueSign(0));
		Assert.AreEqual(XAxisOrientation3D.Left, Orientation3DExtensions.CreateXAxisOrientationFromValueSign(1));
		Assert.AreEqual(XAxisOrientation3D.Right, Orientation3DExtensions.CreateXAxisOrientationFromValueSign(-1));

		Assert.AreEqual(YAxisOrientation3D.None, Orientation3DExtensions.CreateYAxisOrientationFromValueSign(0));
		Assert.AreEqual(YAxisOrientation3D.Up, Orientation3DExtensions.CreateYAxisOrientationFromValueSign(1));
		Assert.AreEqual(YAxisOrientation3D.Down, Orientation3DExtensions.CreateYAxisOrientationFromValueSign(-1));

		Assert.AreEqual(ZAxisOrientation3D.None, Orientation3DExtensions.CreateZAxisOrientationFromValueSign(0));
		Assert.AreEqual(ZAxisOrientation3D.Forward, Orientation3DExtensions.CreateZAxisOrientationFromValueSign(1));
		Assert.AreEqual(ZAxisOrientation3D.Backward, Orientation3DExtensions.CreateZAxisOrientationFromValueSign(-1));
	}
}