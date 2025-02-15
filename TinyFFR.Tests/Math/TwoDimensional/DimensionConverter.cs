// Created on 2025-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using NUnit.Framework.Internal;

namespace Egodystonic.TinyFFR;

[TestFixture]
class DimensionConverterTest {
	const float TestTolerance = 1E-3f;
	static readonly DimensionConverter TestConverter = new(Direction.Left, Direction.Up, Direction.Forward, Location.Origin);

	[Test]
	public void ShouldCorrectlyConstruct() {
		Assert.AreEqual(Direction.Left, TestConverter.XBasis);
		Assert.AreEqual(Direction.Up, TestConverter.YBasis);
		Assert.AreEqual(Direction.Forward, TestConverter.ZBasis);
		Assert.AreEqual(Location.Origin, TestConverter.Origin);

		Assert.AreEqual(TestConverter, new DimensionConverter(TestConverter.XBasis, TestConverter.YBasis, TestConverter.ZBasis));

		var dirList = new List<Direction>();
		for (var x = -2f; x <= 2f; x += 1f) {
			for (var y = -2f; y <= 2f; y += 1f) {
				for (var z = -2f; z <= 2f; z += 1f) {
					dirList.Add(new(x, y, z));
				}
			}
		}

		foreach (var dir in dirList) {
			if (dir == Direction.None) continue;
			var c = new DimensionConverter(dir);
			AssertToleranceEquals(90f, c.ZBasis ^ c.XBasis, TestTolerance);
			AssertToleranceEquals(90f, c.ZBasis ^ c.YBasis, TestTolerance);
			AssertToleranceEquals(90f, c.XBasis ^ c.YBasis, TestTolerance);
			Assert.AreEqual(Location.Origin, c.Origin);

			c = new DimensionConverter(dir, (1f, 2f, 3f));
			AssertToleranceEquals(90f, c.ZBasis ^ c.XBasis, TestTolerance);
			AssertToleranceEquals(90f, c.ZBasis ^ c.YBasis, TestTolerance);
			AssertToleranceEquals(90f, c.XBasis ^ c.YBasis, TestTolerance);
			Assert.AreEqual(new Location(1f, 2f, 3f), c.Origin);
		}
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeBasesInFactoryMethod() {
		void AssertOrthogonals(DimensionConverter c) {
			try {
				AssertToleranceEquals(90f, c.ZBasis ^ c.XBasis, TestTolerance);
				AssertToleranceEquals(90f, c.ZBasis ^ c.YBasis, TestTolerance);
				AssertToleranceEquals(90f, c.XBasis ^ c.YBasis, TestTolerance);
			}
			catch {
				Console.WriteLine(c);
				throw;
			}
		}
		
		var dirList = new List<Direction>();
		for (var x = -1f; x <= 1f; x += 1f) {
			for (var y = -1f; y <= 1f; y += 1f) {
				for (var z = -1f; z <= 1f; z += 1f) {
					if (x == 0f && y == 0f && z == 0f) continue;
					dirList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < dirList.Count; ++i) {
			for (var j = 0; j < dirList.Count; ++j) {
				for (var k = 0; k < dirList.Count; ++k) {
					var x = dirList[i];
					var y = dirList[j];
					var z = dirList[k];
					var dcX = DimensionConverter.FromBasesWithOrthogonalization(Axis.X, x, y, z, (1f, 2f, 3f));
					var dcY = DimensionConverter.FromBasesWithOrthogonalization(Axis.Y, x, y, z, (-1f, -2f, -3f));
					var dcZ = DimensionConverter.FromBasesWithOrthogonalization(Axis.Z, x, y, z);

					AssertOrthogonals(dcX);
					AssertOrthogonals(dcY);
					AssertOrthogonals(dcZ);
					Assert.AreEqual(x, dcX.XBasis);
					Assert.AreEqual(y, dcY.YBasis);
					Assert.AreEqual(z, dcZ.ZBasis);

					Assert.AreEqual(new Location(1f, 2f, 3f), dcX.Origin);
					Assert.AreEqual(new Location(-1f, -2f, -3f), dcY.Origin);
					Assert.AreEqual(new Location(0f, 0f, 0f), dcZ.Origin);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyConvertLocations() {
		AssertToleranceEquals(new Location(1f, 2f, 0f), TestConverter.ConvertLocation((1f, 2f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, 2f, 3f), TestConverter.ConvertLocation((1f, 2f), 3f), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(1f, 2f), TestConverter.ConvertLocation((1f, 2f, 3f)), TestTolerance);

		AssertToleranceEquals(new Location(-1f, -2f, 0f), TestConverter.ConvertLocation((-1f, -2f)), TestTolerance);
		AssertToleranceEquals(new Location(-1f, -2f, -3f), TestConverter.ConvertLocation((-1f, -2f), -3f), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(-1f, -2f), TestConverter.ConvertLocation((-1f, -2f, -3f)), TestTolerance);

		var c = new DimensionConverter(-TestConverter.XBasis, -TestConverter.YBasis, -TestConverter.ZBasis, Location.Origin);
		AssertToleranceEquals(new Location(-1f, -2f, 0f), c.ConvertLocation((1f, 2f)), TestTolerance);
		AssertToleranceEquals(new Location(-1f, -2f, -3f), c.ConvertLocation((1f, 2f), 3f), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(-1f, -2f), c.ConvertLocation((1f, 2f, 3f)), TestTolerance);

		AssertToleranceEquals(new Location(1f, 2f, 0f), c.ConvertLocation((-1f, -2f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, 2f, 3f), c.ConvertLocation((-1f, -2f), -3f), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(1f, 2f), c.ConvertLocation((-1f, -2f, -3f)), TestTolerance);

		c = new DimensionConverter(TestConverter.XBasis, TestConverter.YBasis, TestConverter.ZBasis, (-1f, -2f, -3f));
		AssertToleranceEquals(new Location(0f, 0f, -3f), c.ConvertLocation((1f, 2f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, 0f, 0f), c.ConvertLocation((1f, 2f), 3f), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(2f, 4f), c.ConvertLocation((1f, 2f, 3f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyConvertVects() {
		AssertToleranceEquals(new Vect(1f, 2f, 0f), TestConverter.ConvertVect((1f, 2f)), TestTolerance);
		AssertToleranceEquals(new Vect(1f, 2f, 3f), TestConverter.ConvertVect((1f, 2f), 3f), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(1f, 2f), TestConverter.ConvertVect((1f, 2f, 3f)), TestTolerance);

		AssertToleranceEquals(new Vect(-1f, -2f, 0f), TestConverter.ConvertVect((-1f, -2f)), TestTolerance);
		AssertToleranceEquals(new Vect(-1f, -2f, -3f), TestConverter.ConvertVect((-1f, -2f), -3f), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(-1f, -2f), TestConverter.ConvertVect((-1f, -2f, -3f)), TestTolerance);

		var c = new DimensionConverter(-TestConverter.XBasis, -TestConverter.YBasis, -TestConverter.ZBasis, Location.Origin);
		AssertToleranceEquals(new Vect(-1f, -2f, 0f), c.ConvertVect((1f, 2f)), TestTolerance);
		AssertToleranceEquals(new Vect(-1f, -2f, -3f), c.ConvertVect((1f, 2f), 3f), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(-1f, -2f), c.ConvertVect((1f, 2f, 3f)), TestTolerance);

		AssertToleranceEquals(new Vect(1f, 2f, 0f), c.ConvertVect((-1f, -2f)), TestTolerance);
		AssertToleranceEquals(new Vect(1f, 2f, 3f), c.ConvertVect((-1f, -2f), -3f), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(1f, 2f), c.ConvertVect((-1f, -2f, -3f)), TestTolerance);

		c = new DimensionConverter(TestConverter.XBasis, TestConverter.YBasis, TestConverter.ZBasis, (-1f, -2f, -3f));
		AssertToleranceEquals(new Vect(1f, 2f, 0f), c.ConvertVect((1f, 2f)), TestTolerance);
		AssertToleranceEquals(new Vect(1f, 2f, 3f), c.ConvertVect((1f, 2f), 3f), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(1f, 2f), c.ConvertVect((1f, 2f, 3f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyConvertDirections() {
		AssertToleranceEquals(new Direction(1f, 2f, 0f), TestConverter.ConvertDirection((1f, 2f)), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(1f, 2f).WithLengthOne(), TestConverter.ConvertDirection((1f, 2f, 3f)), TestTolerance);

		AssertToleranceEquals(new Direction(-1f, -2f, 0f), TestConverter.ConvertDirection((-1f, -2f)), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(-1f, -2f).WithLengthOne(), TestConverter.ConvertDirection((-1f, -2f, -3f)), TestTolerance);

		var c = new DimensionConverter(-TestConverter.XBasis, -TestConverter.YBasis, -TestConverter.ZBasis, Location.Origin);
		AssertToleranceEquals(new Direction(-1f, -2f, 0f), c.ConvertDirection((1f, 2f)), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(-1f, -2f).WithLengthOne(), c.ConvertDirection((1f, 2f, 3f)), TestTolerance);

		AssertToleranceEquals(new Direction(1f, 2f, 0f), c.ConvertDirection((-1f, -2f)), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(1f, 2f).WithLengthOne(), c.ConvertDirection((-1f, -2f, -3f)), TestTolerance);

		c = new DimensionConverter(TestConverter.XBasis, TestConverter.YBasis, TestConverter.ZBasis, (-1f, -2f, -3f));
		AssertToleranceEquals(new Direction(1f, 2f, 0f), c.ConvertDirection((1f, 2f)), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(1f, 2f).WithLengthOne(), c.ConvertDirection((1f, 2f, 3f)), TestTolerance);

		Assert.AreEqual(null, TestConverter.ConvertDirection(Direction.Forward));
		Assert.AreEqual(null, TestConverter.ConvertDirection(Direction.Backward));
		Assert.AreEqual(null, TestConverter.ConvertDirection((0f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyImplementEquality() {
		Assert.AreEqual(TestConverter, TestConverter);
		// ReSharper disable EqualExpressionComparison
		Assert.IsTrue(TestConverter == TestConverter);
		Assert.IsFalse(TestConverter != TestConverter);
		// ReSharper restore EqualExpressionComparison

		var c = new DimensionConverter(Direction.Right);
		Assert.AreNotEqual(c, TestConverter);
		Assert.IsFalse(c == TestConverter);
		Assert.IsTrue(c != TestConverter);
	}
}