// Created on 2026-04-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Reflection;

namespace Egodystonic.TinyFFR;

[TestFixture]
class PositionedSphereTest {
	const float TestTolerance = 0.01f;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyDelegateMembers() {
		var testType = typeof(PositionedSphere);
		var implType = typeof(TranslatedConvexShape<Sphere>);
		
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
			var translatedShapeType = typeof(TranslatedShape<Sphere>);
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
		
		object? Normalize(object? value) => value is PositionedSphere ps ? (TranslatedConvexShape<Sphere>) ps : value;

		

		object? RandomArg(Type type) {
			if (type == testType) return (object) PositionedSphere.Random();
			if (type == typeof(float)) return Random.Shared.NextSingle() * 200f - 100f;
			if (type == typeof(object)) return null;
			if (type == typeof(string)) return PositionedSphere.Random().ToString("G", null);
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
				implArgs[j] = arg is PositionedSphere argPs ? (object) (TranslatedConvexShape<Sphere>) argPs : arg;
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
			var input = PositionedSphere.Random();
			var castInput = (TranslatedConvexShape<Sphere>) input;
			
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
	public void ShouldCorrectlyImplementSplitting() {
		var sphere = new PositionedSphere(10f, new Location(3f, 5f, -2f));

		Assert.IsFalse(sphere.TrySplit(new Plane(Direction.Up, new Location(0f, 20f, 0f)), out _, out _));

		Assert.IsTrue(sphere.TrySplit(new Plane(Direction.Up, new Location(3f, 5f, -2f)), out var centrePoint, out var radius));
		AssertToleranceEquals(new Location(3f, 5f, -2f), centrePoint, TestTolerance);
		Assert.AreEqual(10f, radius, TestTolerance);

		Assert.IsTrue(sphere.TrySplit(new Plane(Direction.Up, new Location(0f, 10f, 0f)), out centrePoint, out radius));
		AssertToleranceEquals(new Location(3f, 10f, -2f), centrePoint, TestTolerance);
		Assert.AreEqual(8.66025f, radius, TestTolerance);

		Assert.IsTrue(sphere.TrySplit(new Plane(Direction.Forward, new Location(3f, 5f, 5f)), out centrePoint, out radius));
		AssertToleranceEquals(new Location(3f, 5f, 5f), centrePoint, TestTolerance);
		Assert.AreEqual(7.1414f, radius, TestTolerance);

		Assert.IsFalse(sphere.TrySplit(new Plane(Direction.Up, new Location(0f, 15.5f, 0f)), out _, out _));
	}
	
	[Test]
	public void ShouldCorrectlyGenerateRandomLocations() {
		const int NumIterations = 100_000;

		var sphere = new PositionedSphere(10f, new Location(3f, 5f, -2f));

		for (var i = 0; i < NumIterations; ++i) {
			var l = Location.Random(sphere);
			Assert.IsTrue(sphere.Contains(l));
		}
	}

	[Test]
	public void ShouldCorrectlyCalculateEnclosingAndEnclosedCubes() {
		var sphere = new PositionedSphere(7.4f, new Location(3f, 5f, -2f));

		var smallest = sphere.SmallestEnclosingCube;
		AssertToleranceEquals(sphere.Position, smallest.Position, TestTolerance);
		AssertToleranceEquals(sphere.ToStandardSphere().SmallestEnclosingCube, smallest.ToStandardCuboid(), TestTolerance);
		Assert.AreEqual(sphere.Diameter, smallest.Width, TestTolerance);
		Assert.AreEqual(sphere.Diameter, smallest.Height, TestTolerance);
		Assert.AreEqual(sphere.Diameter, smallest.Depth, TestTolerance);

		var largest = sphere.LargestEnclosedCube;
		AssertToleranceEquals(sphere.Position, largest.Position, TestTolerance);
		AssertToleranceEquals(sphere.ToStandardSphere().LargestEnclosedCube, largest.ToStandardCuboid(), TestTolerance);
		var expectedSide = sphere.Diameter * MathUtils.SquareRootOfThreeReciprocal;
		Assert.AreEqual(expectedSide, largest.Width, TestTolerance);
		Assert.AreEqual(expectedSide, largest.Height, TestTolerance);
		Assert.AreEqual(expectedSide, largest.Depth, TestTolerance);

		AssertToleranceEquals(sphere, largest.SmallestEnclosingSphere, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyMeasureDistanceAndIntersectionsWithOtherSpheres() {
		var sphere = new PositionedSphere(3f, new Location(0f, 0f, 0f));

		var farSphere = new PositionedSphere(2f, new Location(10f, 0f, 0f));
		Assert.AreEqual(5f, sphere.DistanceFrom(farSphere), TestTolerance);
		Assert.AreEqual(5f, farSphere.DistanceFrom(sphere), TestTolerance);
		Assert.IsFalse(sphere.IsIntersectedBy(farSphere));
		Assert.IsFalse(farSphere.IsIntersectedBy(sphere));

		var touchingSphere = new PositionedSphere(2f, new Location(5f, 0f, 0f));
		Assert.AreEqual(0f, sphere.DistanceFrom(touchingSphere), TestTolerance);
		Assert.AreEqual(0f, touchingSphere.DistanceFrom(sphere), TestTolerance);
		Assert.IsFalse(sphere.IsIntersectedBy(touchingSphere));
		Assert.IsFalse(touchingSphere.IsIntersectedBy(sphere));

		var overlappingSphere = new PositionedSphere(2f, new Location(4f, 0f, 0f));
		Assert.AreEqual(0f, sphere.DistanceFrom(overlappingSphere), TestTolerance);
		Assert.AreEqual(0f, overlappingSphere.DistanceFrom(sphere), TestTolerance);
		Assert.IsTrue(sphere.IsIntersectedBy(overlappingSphere));
		Assert.IsTrue(overlappingSphere.IsIntersectedBy(sphere));

		var coincidentSphere = new PositionedSphere(3f, new Location(0f, 0f, 0f));
		Assert.AreEqual(0f, sphere.DistanceFrom(coincidentSphere), TestTolerance);
		Assert.AreEqual(0f, coincidentSphere.DistanceFrom(sphere), TestTolerance);
		Assert.IsTrue(sphere.IsIntersectedBy(coincidentSphere));
		Assert.IsTrue(coincidentSphere.IsIntersectedBy(sphere));

		var insideSphere = new PositionedSphere(0.5f, new Location(1f, 0f, 0f));
		Assert.AreEqual(0f, sphere.DistanceFrom(insideSphere), TestTolerance);
		Assert.AreEqual(0f, insideSphere.DistanceFrom(sphere), TestTolerance);
		Assert.IsTrue(sphere.IsIntersectedBy(insideSphere));
		Assert.IsTrue(insideSphere.IsIntersectedBy(sphere));

		var diagonalSphere = new PositionedSphere(1f, new Location(3f, 4f, 0f));
		Assert.AreEqual(1f, sphere.DistanceFrom(diagonalSphere), TestTolerance);
		Assert.AreEqual(1f, diagonalSphere.DistanceFrom(sphere), TestTolerance);
		Assert.IsFalse(sphere.IsIntersectedBy(diagonalSphere));
		Assert.IsFalse(diagonalSphere.IsIntersectedBy(sphere));
	}

	[Test]
	public void ShouldCorrectlyMeasureDistanceAndIntersectionWithCuboid() {
		var sphere = new PositionedSphere(1f, new Location(0f, 0f, 0f));

		var farCuboid = new PositionedCuboid(4f, 6f, 2f, new Location(10f, 0f, 0f));
		Assert.AreEqual(7f, sphere.DistanceFrom(farCuboid), TestTolerance);
		Assert.AreEqual(7f, farCuboid.DistanceFrom(sphere), TestTolerance);
		Assert.IsFalse(sphere.IsIntersectedBy(farCuboid));
		Assert.IsFalse(farCuboid.IsIntersectedBy(sphere));

		var touchingCuboid = new PositionedCuboid(4f, 6f, 2f, new Location(3f, 0f, 0f));
		Assert.AreEqual(0f, sphere.DistanceFrom(touchingCuboid), TestTolerance);
		Assert.AreEqual(0f, touchingCuboid.DistanceFrom(sphere), TestTolerance);
		Assert.IsFalse(sphere.IsIntersectedBy(touchingCuboid));
		Assert.IsFalse(touchingCuboid.IsIntersectedBy(sphere));

		var overlappingCuboid = new PositionedCuboid(4f, 6f, 2f, new Location(2.5f, 0f, 0f));
		Assert.AreEqual(0f, sphere.DistanceFrom(overlappingCuboid), TestTolerance);
		Assert.AreEqual(0f, overlappingCuboid.DistanceFrom(sphere), TestTolerance);
		Assert.IsTrue(sphere.IsIntersectedBy(overlappingCuboid));
		Assert.IsTrue(overlappingCuboid.IsIntersectedBy(sphere));

		var enclosingCuboid = new PositionedCuboid(10f, 10f, 10f, new Location(0f, 0f, 0f));
		Assert.AreEqual(0f, sphere.DistanceFrom(enclosingCuboid), TestTolerance);
		Assert.AreEqual(0f, enclosingCuboid.DistanceFrom(sphere), TestTolerance);
		Assert.IsTrue(sphere.IsIntersectedBy(enclosingCuboid));
		Assert.IsTrue(enclosingCuboid.IsIntersectedBy(sphere));

		var cornerCuboid = new PositionedCuboid(2f, 2f, 2f, new Location(5f, 5f, 0f));
		var cornerExpected = MathF.Sqrt(32f) - 1f;
		Assert.AreEqual(cornerExpected, sphere.DistanceFrom(cornerCuboid), TestTolerance);
		Assert.AreEqual(cornerExpected, cornerCuboid.DistanceFrom(sphere), TestTolerance);
		Assert.IsFalse(sphere.IsIntersectedBy(cornerCuboid));
		Assert.IsFalse(cornerCuboid.IsIntersectedBy(sphere));
	}

	[Test]
	public void ShouldCorrectlyMeasureDistanceAndIntersectionWithRotatedCuboid() {
		var sphere = new PositionedSphere(1f, new Location(0f, 0f, 0f));

		var unrotated = new PositionedRotatedCuboid(4f, 6f, 2f, new Location(10f, 0f, 0f), Rotation.None);
		Assert.AreEqual(7f, sphere.DistanceFrom(unrotated), TestTolerance);
		Assert.AreEqual(7f, unrotated.DistanceFrom(sphere), TestTolerance);
		Assert.IsFalse(sphere.IsIntersectedBy(unrotated));
		Assert.IsFalse(unrotated.IsIntersectedBy(sphere));

		var rotatedCube = new PositionedRotatedCuboid(2f, 2f, 2f, new Location(5f, 0f, 0f), new Rotation(45f, Direction.Up));
		var rotExpected = 4f - MathF.Sqrt(2f);
		Assert.AreEqual(rotExpected, sphere.DistanceFrom(rotatedCube), TestTolerance);
		Assert.AreEqual(rotExpected, rotatedCube.DistanceFrom(sphere), TestTolerance);
		Assert.IsFalse(sphere.IsIntersectedBy(rotatedCube));
		Assert.IsFalse(rotatedCube.IsIntersectedBy(sphere));

		var enclosing = new PositionedRotatedCuboid(10f, 10f, 10f, new Location(0f, 0f, 0f), new Rotation(33f, new Direction(1f, 2f, 3f)));
		Assert.AreEqual(0f, sphere.DistanceFrom(enclosing), TestTolerance);
		Assert.AreEqual(0f, enclosing.DistanceFrom(sphere), TestTolerance);
		Assert.IsTrue(sphere.IsIntersectedBy(enclosing));
		Assert.IsTrue(enclosing.IsIntersectedBy(sphere));

		var overlapping = new PositionedRotatedCuboid(2f, 2f, 2f, new Location(1f, 0f, 0f), new Rotation(45f, Direction.Up));
		Assert.AreEqual(0f, sphere.DistanceFrom(overlapping), TestTolerance);
		Assert.AreEqual(0f, overlapping.DistanceFrom(sphere), TestTolerance);
		Assert.IsTrue(sphere.IsIntersectedBy(overlapping));
		Assert.IsTrue(overlapping.IsIntersectedBy(sphere));

		var touching = new PositionedRotatedCuboid(2f, 2f, 2f, new Location(1f + MathF.Sqrt(2f), 0f, 0f), new Rotation(45f, Direction.Up));
		Assert.AreEqual(0f, sphere.DistanceFrom(touching), TestTolerance);
		Assert.AreEqual(0f, touching.DistanceFrom(sphere), TestTolerance);
		Assert.IsFalse(sphere.IsIntersectedBy(touching));
		Assert.IsFalse(touching.IsIntersectedBy(sphere));
	}
}