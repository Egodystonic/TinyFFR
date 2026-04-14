// Created on 2026-04-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Reflection;

namespace Egodystonic.TinyFFR;

[TestFixture]
class PositionedRotatedCuboidTest {
	const float TestTolerance = 0.01f;
	static readonly PositionedRotatedCuboid TestCuboid = new(4f, 6f, 2f, new Location(10f, 20f, 30f), new Rotation(90f, Direction.Up));

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyDelegateMembers() {
		var testType = typeof(PositionedRotatedCuboid);
		var implType = typeof(TranslatedRotatedConvexShape<Cuboid>);
		
		bool HasByRefLikeParams(MethodInfo m) => m.GetParameters().Any(p => {
			var type = p.ParameterType;
			if (type.IsByRef) type = type.GetElementType()!;
			return type.IsByRefLike;
		});
		
		(PropertyInfo ImplProp, PropertyInfo TestProp)[] PrintAndFilterProperties(IEnumerable<PropertyInfo> props) {
			return props.Select(p => {
				var testProp = testType.GetProperty(p.Name);
				if (testProp == null) Console.WriteLine($"Skipping un-matched property '{p.Name}'.");
				else Console.WriteLine($"Including property '{p.Name}'.");
				return (ImplProp: p, TestProp: testProp);
			})
			.Where(tuple => tuple.TestProp != null)
			.ToArray()!;
		}
		
		(MethodInfo ImplMethod, MethodInfo TestMethod)[] PrintAndFilterMethods(IEnumerable<MethodInfo> methods) {
			var translatedShapeType = typeof(TranslatedShape<Cuboid>);
			return methods.Select(m => {
				var adjustedParamTypes = m.GetParameters()
					.Select(p => {
						var type = p.ParameterType;
						var isByRef = type.IsByRef;
						if (isByRef) type = type.GetElementType()!;
						if (type == implType || type == translatedShapeType) type = testType;
						return isByRef ? type.MakeByRefType() : type;
					})
					.ToArray();
				MethodInfo? testMethod;
				try { testMethod = testType.GetMethod(m.Name, adjustedParamTypes); }
				catch (AmbiguousMatchException) { testMethod = null; }
				var paramDesc = string.Join(", ", adjustedParamTypes.Select(t => t.Name));
				if (HasByRefLikeParams(m)) {
					Console.WriteLine($"Skipping by-ref param method '{m.Name}({paramDesc})'.");
					return (m, null);
				}
				if (m.DeclaringType == typeof(object) || m.DeclaringType == typeof(ValueType)) {
					Console.WriteLine($"Skipping root method '{m.Name}({paramDesc})'.");
					return (m, null);
				}
				if (m.Name.Contains("Random", StringComparison.Ordinal)) {
					Console.WriteLine($"Skipping random method '{m.Name}({paramDesc})'.");
					return (m, null);
				}
				if (testMethod == null) Console.WriteLine($"Skipping un-matched method '{m.Name}({paramDesc})'.");
				else Console.WriteLine($"Including method '{m.Name}({paramDesc})'.");
				return (ImplMethod: m, TestMethod: testMethod);
			})
			.Where(tuple => tuple.TestMethod != null)
			.ToArray()!;
		}
		
		object? Normalize(object? value) => value is PositionedRotatedCuboid ps ? (TranslatedRotatedConvexShape<Cuboid>) ps : value;

		

		object? RandomArg(Type type) {
			if (type == testType) return (object) PositionedRotatedCuboid.Random();
			if (type == typeof(float)) return Random.Shared.NextSingle() * 200f - 100f;
			if (type == typeof(object)) return null;
			if (type == typeof(string)) return PositionedRotatedCuboid.Random().ToString("G", null);
			if (type == typeof(IFormatProvider)) return null;
			var randomMethod = type.GetMethod("Random", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
			if (randomMethod != null) return randomMethod.Invoke(null, null);
			throw new InvalidOperationException($"Don't know how to generate random {type.Name}");
		}

		void AssertMethodDelegation(MethodInfo implMethod, MethodInfo testMethod, object? implInstance, object? testInstance, string inputDesc) {
			var implParams = implMethod.GetParameters();
			var testParams = testMethod.GetParameters();
			var testArgs = new object?[testParams.Length];
			var implArgs = new object?[implParams.Length];

			for (var j = 0; j < testParams.Length; j++) {
				if (testParams[j].IsOut) {
					testArgs[j] = null;
					implArgs[j] = null;
					continue;
				}
				var arg = RandomArg(testParams[j].ParameterType);
				testArgs[j] = arg;
				implArgs[j] = arg is PositionedRotatedCuboid argPs ? (object) (TranslatedRotatedConvexShape<Cuboid>) argPs : arg;
			}

			Exception? implException = null, testException = null;
			object? implResult = null, testResult = null;

			try { implResult = implMethod.Invoke(implInstance, implArgs); }
			catch (TargetInvocationException e) { implException = e.InnerException; }

			try { testResult = testMethod.Invoke(testInstance, testArgs); }
			catch (TargetInvocationException e) { testException = e.InnerException; }

			var methodDesc = $"method '{implMethod.Name}', {inputDesc}";

			if (implException != null || testException != null) {
				Assert.AreEqual(implException?.GetType(), testException?.GetType(), $"Exception type discrepancy for {methodDesc}");
				return;
			}

			Assert.AreEqual(Normalize(implResult), Normalize(testResult), $"Return value discrepancy for {methodDesc}");

			for (var j = 0; j < implParams.Length; j++) {
				if (!implParams[j].IsOut) continue;
				Assert.AreEqual(Normalize(implArgs[j]), Normalize(testArgs[j]), $"Out param discrepancy for {methodDesc}, param '{implParams[j].Name}'");
			}
		}

		var instanceFlags = BindingFlags.Public | BindingFlags.Instance;
		var staticFlags = BindingFlags.Public | BindingFlags.Static;
		var instanceProperties = PrintAndFilterProperties(implType.GetProperties(instanceFlags));
		var staticProperties = PrintAndFilterProperties(implType.GetProperties(staticFlags));
		var instanceMethods = PrintAndFilterMethods(implType.GetMethods(instanceFlags));
		var staticMethods = PrintAndFilterMethods(implType.GetMethods(staticFlags));
		
		for (var i = 0; i < 1000; ++i) {
			var input = PositionedRotatedCuboid.Random();
			var castInput = (TranslatedRotatedConvexShape<Cuboid>) input;
			
			foreach (var tuple in instanceProperties) {
				Assert.AreEqual(tuple.ImplProp.GetValue(castInput), tuple.TestProp.GetValue(input), $"Discrepancy for property '{tuple.ImplProp.Name}', input {input}");
			}

			foreach (var tuple in instanceMethods) {
				AssertMethodDelegation(tuple.ImplMethod, tuple.TestMethod, castInput, input, $"input {input}");
			}
		}
		
		foreach (var tuple in staticProperties) {
			Assert.AreEqual(tuple.ImplProp.GetValue(null), tuple.TestProp.GetValue(null), $"Discrepancy for property '{tuple.ImplProp.Name}'");
		}

		for (var i = 0; i < 1000; ++i) {
			foreach (var tuple in staticMethods) {
				AssertMethodDelegation(tuple.ImplMethod, tuple.TestMethod, null, null, $"iteration {i}");
			}
		}
	}

	[Test]
	public void ShouldCorrectlyCalculateCentroids() {
		AssertToleranceEquals(new Location(10f, 20f, 28f), TestCuboid.CentroidAt(CardinalOrientation.Left), TestTolerance);
		AssertToleranceEquals(new Location(10f, 20f, 32f), TestCuboid.CentroidAt(CardinalOrientation.Right), TestTolerance);
		AssertToleranceEquals(new Location(10f, 23f, 30f), TestCuboid.CentroidAt(CardinalOrientation.Up), TestTolerance);
		AssertToleranceEquals(new Location(10f, 17f, 30f), TestCuboid.CentroidAt(CardinalOrientation.Down), TestTolerance);
		AssertToleranceEquals(new Location(11f, 20f, 30f), TestCuboid.CentroidAt(CardinalOrientation.Forward), TestTolerance);
		AssertToleranceEquals(new Location(9f, 20f, 30f), TestCuboid.CentroidAt(CardinalOrientation.Backward), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCalculateCorners() {
		AssertToleranceEquals(new Location(11f, 23f, 28f), TestCuboid.CornerAt(DiagonalOrientation.LeftUpForward), TestTolerance);
		AssertToleranceEquals(new Location(9f, 23f, 28f), TestCuboid.CornerAt(DiagonalOrientation.LeftUpBackward), TestTolerance);
		AssertToleranceEquals(new Location(11f, 17f, 28f), TestCuboid.CornerAt(DiagonalOrientation.LeftDownForward), TestTolerance);
		AssertToleranceEquals(new Location(9f, 17f, 28f), TestCuboid.CornerAt(DiagonalOrientation.LeftDownBackward), TestTolerance);
		AssertToleranceEquals(new Location(11f, 23f, 32f), TestCuboid.CornerAt(DiagonalOrientation.RightUpForward), TestTolerance);
		AssertToleranceEquals(new Location(9f, 23f, 32f), TestCuboid.CornerAt(DiagonalOrientation.RightUpBackward), TestTolerance);
		AssertToleranceEquals(new Location(11f, 17f, 32f), TestCuboid.CornerAt(DiagonalOrientation.RightDownForward), TestTolerance);
		AssertToleranceEquals(new Location(9f, 17f, 32f), TestCuboid.CornerAt(DiagonalOrientation.RightDownBackward), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCalculateSides() {
		AssertToleranceEquals(new Plane(Direction.Backward, new Location(10f, 20f, 28f)), TestCuboid.SideAt(CardinalOrientation.Left), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Forward, new Location(10f, 20f, 32f)), TestCuboid.SideAt(CardinalOrientation.Right), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Up, new Location(10f, 23f, 30f)), TestCuboid.SideAt(CardinalOrientation.Up), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Down, new Location(10f, 17f, 30f)), TestCuboid.SideAt(CardinalOrientation.Down), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Left, new Location(11f, 20f, 30f)), TestCuboid.SideAt(CardinalOrientation.Forward), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Right, new Location(9f, 20f, 30f)), TestCuboid.SideAt(CardinalOrientation.Backward), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCalculateEdges() {
		void AssertEdge(IntercardinalOrientation orientation, Location expectedStart, Location expectedEnd) {
			Assert.IsTrue(TestCuboid.EdgeAt(orientation).IsEquivalentDisregardingDirection(new(expectedStart, expectedEnd), TestTolerance));
		}

		AssertEdge(IntercardinalOrientation.UpForward, new(11f, 23f, 28f), new(11f, 23f, 32f));
		AssertEdge(IntercardinalOrientation.UpBackward, new(9f, 23f, 28f), new(9f, 23f, 32f));
		AssertEdge(IntercardinalOrientation.DownForward, new(11f, 17f, 28f), new(11f, 17f, 32f));
		AssertEdge(IntercardinalOrientation.DownBackward, new(9f, 17f, 28f), new(9f, 17f, 32f));
		AssertEdge(IntercardinalOrientation.LeftForward, new(11f, 23f, 28f), new(11f, 17f, 28f));
		AssertEdge(IntercardinalOrientation.LeftBackward, new(9f, 23f, 28f), new(9f, 17f, 28f));
		AssertEdge(IntercardinalOrientation.RightForward, new(11f, 23f, 32f), new(11f, 17f, 32f));
		AssertEdge(IntercardinalOrientation.RightBackward, new(9f, 23f, 32f), new(9f, 17f, 32f));
		AssertEdge(IntercardinalOrientation.LeftUp, new(11f, 23f, 28f), new(9f, 23f, 28f));
		AssertEdge(IntercardinalOrientation.LeftDown, new(11f, 17f, 28f), new(9f, 17f, 28f));
		AssertEdge(IntercardinalOrientation.RightUp, new(11f, 23f, 32f), new(9f, 23f, 32f));
		AssertEdge(IntercardinalOrientation.RightDown, new(11f, 17f, 32f), new(9f, 17f, 32f));
	}

	[Test]
	public void ShouldCorrectlyEnumerateProperties() {
		Assert.AreEqual(8, TestCuboid.Corners.Count);
		var cornerCount = 0;
		foreach (var corner in TestCuboid.Corners) {
			AssertToleranceEquals(TestCuboid.CornerAt(OrientationUtils.AllDiagonals[cornerCount]), corner, TestTolerance);
			++cornerCount;
		}
		Assert.AreEqual(8, cornerCount);

		Assert.AreEqual(12, TestCuboid.Edges.Count);
		var edgeCount = 0;
		foreach (var edge in TestCuboid.Edges) {
			Assert.IsTrue(edge.IsEquivalentDisregardingDirection(TestCuboid.EdgeAt(OrientationUtils.AllIntercardinals[edgeCount]), TestTolerance));
			++edgeCount;
		}
		Assert.AreEqual(12, edgeCount);

		Assert.AreEqual(6, TestCuboid.Sides.Count);
		var sideCount = 0;
		foreach (var side in TestCuboid.Sides) {
			AssertToleranceEquals(TestCuboid.SideAt(OrientationUtils.AllCardinals[sideCount]), side, TestTolerance);
			++sideCount;
		}
		Assert.AreEqual(6, sideCount);

		Assert.AreEqual(6, TestCuboid.Centroids.Count);
		var centroidCount = 0;
		foreach (var centroid in TestCuboid.Centroids) {
			AssertToleranceEquals(TestCuboid.CentroidAt(OrientationUtils.AllCardinals[centroidCount]), centroid, TestTolerance);
			++centroidCount;
		}
		Assert.AreEqual(6, centroidCount);
	}

	[Test]
	public void ShouldCorrectlyPreservePositionAndRotationForWithMethods() {
		var withVol = TestCuboid.WithVolume(100f);
		Assert.AreEqual(TestCuboid.Position, withVol.Position);
		AssertToleranceEquals(TestCuboid.Rotation, withVol.Rotation, TestTolerance);
		Assert.AreEqual(100f, withVol.Volume, TestTolerance);

		var withSA = TestCuboid.WithSurfaceArea(200f);
		Assert.AreEqual(TestCuboid.Position, withSA.Position);
		AssertToleranceEquals(TestCuboid.Rotation, withSA.Rotation, TestTolerance);
		Assert.AreEqual(200f, withSA.SurfaceArea, TestTolerance);

		AssertToleranceEquals(TestCuboid.ToStandardCuboid(), TestCuboid.WithVolume(TestCuboid.Volume).ToStandardCuboid(), TestTolerance);
		AssertToleranceEquals(TestCuboid.ToStandardCuboid(), TestCuboid.WithSurfaceArea(TestCuboid.SurfaceArea).ToStandardCuboid(), TestTolerance);
	}
}