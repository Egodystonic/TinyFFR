// Created on 2026-10-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Numerics;
using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR.Assets.Meshes;

[TestFixture]
class MutableGridInstanceTest {
	const float TestTolerance = 0.001f;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	static Vector3 NormalOf(MeshVertex v) => Vector3.Transform(Vector3.UnitZ, v.TangentRotation);
	static Vector3 TangentOf(MeshVertex v) => Vector3.Transform(Vector3.UnitX, v.TangentRotation);

	static void AssertVector(Vector3 expected, Vector3 actual, string message) {
		Assert.IsTrue(Vector3.Distance(expected, actual) < TestTolerance, $"{message}: expected {expected}, was {actual}");
	}

	[Test]
	public void ManagedTangentRotationShouldMatchNative() {
		var rng = new Random(1234);
		var frames = new List<(Direction T, Direction B, Direction N)> {
			(Direction.Right, Direction.Forward, Direction.Up),
			(Direction.Right, Direction.Backward, Direction.Down),
			(Direction.Left, Direction.Up, Direction.Forward),
			(Direction.Up, Direction.Left, Direction.Backward),
			(Direction.Left, Direction.Down, Direction.Backward),
		};
		for (var i = 0; i < 500; ++i) {
			var n = new Vect(rng.NextSingle() * 2f - 1f, rng.NextSingle() * 2f - 1f, rng.NextSingle() * 2f - 1f).Direction;
			if (n == Direction.None) continue;
			var t = n.AnyOrthogonal();
			var b = Direction.FromDualOrthogonalization(n, t);
			frames.Add((t, b, n));
			frames.Add((t, -b, n));
		}

		foreach (var (t, b, n) in frames) {
			var expected = IMeshVertex.CalculateTangentRotation(t, b, n);
			var actual = IMeshVertex.CalculateTangentRotationManaged(t.ToVector3(), b.ToVector3(), n.ToVector3());
			Assert.IsTrue(
				MathF.Abs(expected.X - actual.X) < TestTolerance
				&& MathF.Abs(expected.Y - actual.Y) < TestTolerance
				&& MathF.Abs(expected.Z - actual.Z) < TestTolerance
				&& MathF.Abs(expected.W - actual.W) < TestTolerance,
				$"T {t} B {b} N {n}: expected {expected}, was {actual}"
			);
		}
	}

	[Test]
	public void NormalsShouldOnlyBeRecalculatedWhenRequested() {
		using var factory = new LocalTinyFfrFactory();
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var gridMesh = factory.MeshBuilder.CreateMutableGrid(new XYPair<int>(8, 8));
		using var grid = factory.ObjectBuilder.CreateMutableGridInstance(gridMesh, material);
		const float Slope = 0.75f;
		var area = gridMesh.GridDimensions.Area;
		var up = gridMesh.UpDir.ToVector3();
		var right = gridMesh.XDir.ToVector3();

		void WriteSlope(bool recalculateNormals) {
			using var lease = grid.BorrowVerticesSpan(permitLateralDisplacement: false, recalculateNormals: recalculateNormals);
			for (var i = 0; i < lease.Span.Length; ++i) lease.Span[i] = new MutableGridVertex(Slope * grid.GetVertexCoordinateNormalized(i).X);
		}

		WriteSlope(recalculateNormals: false);
		using (var vertices = grid.UnderlyingModelInstance.BorrowVerticesSpanReadOnly()) {
			for (var i = 0; i < area; ++i) {
				AssertVector(up, NormalOf(vertices.Span[i]), $"front vertex {i} without recalculation");
				AssertVector(-up, NormalOf(vertices.Span[i + area]), $"back vertex {i} without recalculation");
			}
		}

		WriteSlope(recalculateNormals: true);
		var expectedNormal = Vector3.Normalize(up - right * Slope);
		var expectedTangent = Vector3.Normalize(right + up * Slope);
		using (var vertices = grid.UnderlyingModelInstance.BorrowVerticesSpanReadOnly()) {
			for (var i = 0; i < area; ++i) {
				AssertVector(expectedNormal, NormalOf(vertices.Span[i]), $"front vertex {i} normal");
				AssertVector(expectedTangent, TangentOf(vertices.Span[i]), $"front vertex {i} tangent");
				AssertVector(-expectedNormal, NormalOf(vertices.Span[i + area]), $"back vertex {i} normal");
				AssertVector(vertices.Span[i].Location.ToVector3(), vertices.Span[i + area].Location.ToVector3(), $"back vertex {i} location");
			}
		}
	}

	[Test]
	public void LateralDisplacementShouldOnlyApplyWhenPermitted() {
		using var factory = new LocalTinyFfrFactory();
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var gridMesh = factory.MeshBuilder.CreateMutableGrid(new XYPair<int>(5, 5), twoSided: false);
		using var grid = factory.ObjectBuilder.CreateMutableGridInstance(gridMesh, material);
		var centreIndex = grid.GetVertexIndex((2, 2));

		using (var defaults = gridMesh.UnderlyingMesh.BorrowDefaultVerticesSpan()) {
			var original = defaults.Span[centreIndex].Location;
			foreach (var permit in new[] { false, true }) {
				using (var lease = grid.BorrowVerticesSpan(permitLateralDisplacement: permit, recalculateNormals: true)) {
					lease.Span[centreIndex] = new MutableGridVertex(0.5f, new XYPair<float>(1f, 0f));
				}
				using var vertices = grid.UnderlyingModelInstance.BorrowVerticesSpanReadOnly();
				var expected = original + gridMesh.UpDir * 0.5f + (permit ? gridMesh.XDir * (0.5f / 4f) : Vect.Zero);
				AssertVector(expected.ToVector3(), vertices.Span[centreIndex].Location.ToVector3(), $"permit={permit}");
			}
		}
	}
}
