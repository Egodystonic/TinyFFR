// Created on 2026-10-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Numerics;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Threading;

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

	static void WriteWaveHeights(Span<MutableGridVertex> block, XYPair<int> offset, XYPair<int> dimensions, int seed) {
		for (var i = 0; i < block.Length; ++i) {
			var x = offset.X + i % dimensions.X;
			var y = offset.Y + i / dimensions.X;
			block[i] = new MutableGridVertex(MathF.Sin(x * 0.07f + seed) * MathF.Cos(y * 0.05f), new XYPair<float>(MathF.Sin(y * 0.3f) * 0.4f, MathF.Cos(x * 0.2f) * 0.4f));
		}
	}

	sealed class GridReadContext {
		public required MutableGridVertex[] Destination { get; init; }
	}

	static void CopyGridBlock(ReadOnlySpan<MutableGridVertex> block, XYPair<int> offset, XYPair<int> dimensions, GridReadContext context) {
		block.CopyTo(context.Destination.AsSpan(offset.Y * dimensions.X, block.Length));
	}

	static unsafe (MeshVertex[] Vertices, MutableGridVertex[] Grid) RunParallelGridWrite(int? workerThreadCount, XYPair<int> gridDimensions) {
		using var factory = new LocalTinyFfrFactory(factoryConfig: new LocalTinyFfrFactoryConfig { ThreadingConfig = new ThreadingConfig { WorkerThreadCount = workerThreadCount } });
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var gridMesh = factory.MeshBuilder.CreateMutableGrid(gridDimensions);
		using var grid = factory.ObjectBuilder.CreateMutableGridInstance(gridMesh, material);

		grid.WriteVerticesParallelized(&WriteWaveHeights, 3, permitLateralDisplacement: true, recalculateNormals: true);

		var gridCopy = new MutableGridVertex[gridDimensions.Area];
		grid.ReadVerticesParallelized(&CopyGridBlock, new GridReadContext { Destination = gridCopy });
		using var vertices = grid.UnderlyingModelInstance.BorrowVerticesSpanReadOnly();
		return (vertices.Span.ToArray(), gridCopy);
	}

	[Test]
	public void ParallelizedWritesShouldMatchSerialExecution() {
		var gridDimensions = new XYPair<int>(203, 157);
		var (serialVertices, serialGrid) = RunParallelGridWrite(0, gridDimensions);
		var (parallelVertices, parallelGrid) = RunParallelGridWrite(6, gridDimensions);

		var expectedGrid = new MutableGridVertex[gridDimensions.Area];
		WriteWaveHeights(expectedGrid, XYPair<int>.Zero, gridDimensions, 3);
		Assert.IsTrue(expectedGrid.AsSpan().SequenceEqual(serialGrid));
		Assert.IsTrue(expectedGrid.AsSpan().SequenceEqual(parallelGrid));
		Assert.AreEqual(serialVertices.Length, parallelVertices.Length);
		for (var i = 0; i < serialVertices.Length; ++i) Assert.AreEqual(serialVertices[i], parallelVertices[i], $"Vertex {i}");
	}

	static void OffsetVertices(Span<MeshVertex> block, int startIndex, float offset) {
		for (var i = 0; i < block.Length; ++i) block[i] = block[i] with { Location = block[i].Location + new Vect(offset, -offset, startIndex + i) };
	}

	[Test]
	public unsafe void ParallelizedModelInstanceVertexWritesShouldMatchSerialLeases() {
		using var factory = new LocalTinyFfrFactory(factoryConfig: new LocalTinyFfrFactoryConfig { ThreadingConfig = new ThreadingConfig { WorkerThreadCount = 4 } });
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var gridMesh = factory.MeshBuilder.CreateMutableGrid(new XYPair<int>(120, 90));
		using var serialGrid = factory.ObjectBuilder.CreateMutableGridInstance(gridMesh, material);
		using var parallelGrid = factory.ObjectBuilder.CreateMutableGridInstance(gridMesh, material);

		using (var lease = serialGrid.UnderlyingModelInstance.BorrowVerticesSpan(false)) OffsetVertices(lease.Span, 0, 0.25f);
		parallelGrid.UnderlyingModelInstance.WriteVerticesParallelized(&OffsetVertices, 0.25f, recalculateBoundingBoxOnLeaseDispose: false);

		using var expected = serialGrid.UnderlyingModelInstance.BorrowVerticesSpanReadOnly();
		var actual = new MeshVertex[expected.Span.Length];
		parallelGrid.UnderlyingModelInstance.ReadVerticesParallelized(&CopyVertexBlock, actual);
		Assert.IsTrue(expected.Span.SequenceEqual(actual));
	}

	static void CopyVertexBlock(ReadOnlySpan<MeshVertex> block, int startIndex, MeshVertex[] destination) => block.CopyTo(destination.AsSpan(startIndex));
}
