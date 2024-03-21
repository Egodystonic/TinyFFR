// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using static Egodystonic.TinyFFR.OrientationUtils;

namespace Egodystonic.TinyFFR;

[TestFixture]
class OrientationUtilsTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyInstantiateStaticReadonlyFields() {
		void AssertSpan<T>(ReadOnlySpan<T> span) where T : struct, Enum {
			Assert.AreEqual(Enum.GetValues<T>().Length - 1, span.Length);
			for (var i = 0; i < span.Length; ++i) {
				Assert.IsTrue(Enum.IsDefined(span[i]));
			}

			var spanArr = span.ToArray();
			foreach (var value in Enum.GetValues<T>()) {
				Assert.AreEqual(!value.Equals(default(T)), spanArr.Contains(value));
			}
		}

		AssertSpan(All3DOrientations);
		AssertSpan(AllAxes);
		AssertSpan(AllCardinals);
		AssertSpan(AllIntercardinals);
		AssertSpan(AllDiagonals);
		AssertSpan(All2DOrientations);
		AssertSpan(AllHorizontals);
		AssertSpan(AllVerticals);
	}

	[Test]
	public void ShouldCorrectlyCreateOrientations() {
		foreach (var orientation in Enum.GetValues<Orientation3D>()) {
			Assert.AreEqual(orientation, CreateOrientation(orientation.GetXAxis(), orientation.GetYAxis(), orientation.GetZAxis()));
		}
	}

	[Test]
	public void ShouldCorrectlyCreateAxesFromSigns() {
		Assert.AreEqual(XAxisOrientation3D.None, CreateXAxisOrientationFromValueSign(0));
		Assert.AreEqual(XAxisOrientation3D.Left, CreateXAxisOrientationFromValueSign(1));
		Assert.AreEqual(XAxisOrientation3D.Right, CreateXAxisOrientationFromValueSign(-1));

		Assert.AreEqual(YAxisOrientation3D.None, CreateYAxisOrientationFromValueSign(0));
		Assert.AreEqual(YAxisOrientation3D.Up, CreateYAxisOrientationFromValueSign(1));
		Assert.AreEqual(YAxisOrientation3D.Down, CreateYAxisOrientationFromValueSign(-1));

		Assert.AreEqual(ZAxisOrientation3D.None, CreateZAxisOrientationFromValueSign(0));
		Assert.AreEqual(ZAxisOrientation3D.Forward, CreateZAxisOrientationFromValueSign(1));
		Assert.AreEqual(ZAxisOrientation3D.Backward, CreateZAxisOrientationFromValueSign(-1));
	}
}