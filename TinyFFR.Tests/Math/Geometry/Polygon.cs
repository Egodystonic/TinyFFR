// Created on 2025-02-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using NUnit.Framework.Internal;

namespace Egodystonic.TinyFFR;

[TestFixture]
class PolygonTest {
	const float TestTolerance = 1E-3f;

	Polygon CreatePolygon(params Location[] vertices) => new(vertices);
	Polygon CreatePolygon(Direction normal, params Location[] vertices) => new(vertices, normal);
	Polygon CreatePolygon(Direction normal, bool isWoundClockwise, params Location[] vertices) => new(vertices, normal, isWoundClockwise);

	Polygon AcwTri => CreatePolygon(Direction.Backward, false, (0f, 1f, 0f), (1.5f, -0.5f, 0f), (-1.5f, -0.5f, 0f));
	Polygon AcwSquare => CreatePolygon(Direction.Backward, false, (1f, -1f, 0f), (-1f, -1f, 0f), (-1f, 1f, 0f), (1f, 1f, 0f));
	Polygon AcwCircle => CreatePolygon(Direction.Backward, false, Enumerable.Range(0, 100).Select(i => (Location.Origin >> (0f, 1f, 0f)).RotatedBy((Direction.Backward % 90f).ScaledBy(i / 25f)).AsLocation()).ToArray());
	Polygon AcwLine => CreatePolygon(Direction.Backward, false, (1f, 0f, 0f), (-1f, 0f, 0f));
	Polygon AcwPoint => CreatePolygon(Direction.Backward, false, (0f, 0f, 0f));
	Polygon CwTri => CreatePolygon(Direction.Backward, true, (0f, 1f, 0f), (-1.5f, -0.5f, 0f), (1.5f, -0.5f, 0f));
	Polygon CwSquare => CreatePolygon(Direction.Backward, true, (1f, 1f, 0f), (-1f, 1f, 0f), (-1f, -1f, 0f), (1f, -1f, 0f));
	Polygon CwCircle => CreatePolygon(Direction.Backward, true, Enumerable.Range(0, 100).Select(i => (Location.Origin >> (0f, 1f, 0f)).RotatedBy((Direction.Forward % 90f).ScaledBy(i / 25f)).AsLocation()).ToArray());
	Polygon CwLine => CreatePolygon(Direction.Backward, true, (1f, 0f, 0f), (-1f, 0f, 0f));
	Polygon CwPoint => CreatePolygon(Direction.Backward, true, (0f, 0f, 0f));

	[Test]
	public void ShouldCorrectlySetProperties() {
		void AssertPoly(Polygon p, int expectedVertexCount, int expectedEdgeCount, int expectedTriangleCount, Direction expectedNormal, bool expectedWinding) {
			Assert.AreEqual(expectedVertexCount, p.VertexCount);
			Assert.AreEqual(expectedVertexCount, p.Vertices.Length);
			Assert.AreEqual(expectedEdgeCount, p.EdgeCount);
			Assert.AreEqual(expectedTriangleCount, p.TriangleCount);
			Assert.AreEqual(expectedNormal, p.Normal);
			Assert.AreEqual(expectedWinding, p.IsWoundClockwise);
		}
		
		Assert.AreEqual(new Location(0f, 1f, 0f), AcwTri.Vertices[0]);
		Assert.AreEqual(new Location(1.5f, -0.5f, 0f), AcwTri.Vertices[1]);
		Assert.AreEqual(new Location(-1.5f, -0.5f, 0f), AcwTri.Vertices[2]);

		AssertPoly(AcwTri, 3, 3, 1, Direction.Backward, false);
		AssertPoly(AcwSquare, 4, 4, 2, Direction.Backward, false);
		AssertPoly(AcwCircle, 100, 100, 98, Direction.Backward, false);
		AssertPoly(AcwLine, 2, 1, 0, Direction.Backward, false);
		AssertPoly(AcwPoint, 1, 0, 0, Direction.Backward, false);
		AssertPoly(CwTri, 3, 3, 1, Direction.Backward, true);
		AssertPoly(CwSquare, 4, 4, 2, Direction.Backward, true);
		AssertPoly(CwCircle, 100, 100, 98, Direction.Backward, true);
		AssertPoly(CwLine, 2, 1, 0, Direction.Backward, true);
		AssertPoly(CwPoint, 1, 0, 0, Direction.Backward, true);
	}

	[Test]
	public void ShouldCorrectlyCalculateNormalFromCoplanarVertices() {
		Assert.Catch(() => Polygon.CalculateNormalForAnticlockwiseCoplanarVertices(AcwLine.Vertices));
		Assert.Catch(() => Polygon.CalculateNormalForAnticlockwiseCoplanarVertices(AcwPoint.Vertices));

		Assert.AreEqual(Direction.Backward, Polygon.CalculateNormalForAnticlockwiseCoplanarVertices(AcwTri.Vertices));
		Assert.AreEqual(Direction.Backward, Polygon.CalculateNormalForAnticlockwiseCoplanarVertices(AcwSquare.Vertices));
		Assert.AreEqual(Direction.Backward, Polygon.CalculateNormalForAnticlockwiseCoplanarVertices(AcwCircle.Vertices));
		Assert.AreEqual(Direction.Forward, Polygon.CalculateNormalForAnticlockwiseCoplanarVertices(CwTri.Vertices));
		Assert.AreEqual(Direction.Forward, Polygon.CalculateNormalForAnticlockwiseCoplanarVertices(CwSquare.Vertices));
		Assert.AreEqual(Direction.Forward, Polygon.CalculateNormalForAnticlockwiseCoplanarVertices(CwCircle.Vertices));

		var acwVertsWithCwTriangleEmbedded = new Location[] {
			(0f, 1f, 0f), (-1.5f, -0.5f, 0f), (1.5f, -0.5f, 0f), // First three points are a CW triangle
			(1.5f, -1.5f, 0f), (-4f, -1.5f, 0f) // Last two points take it back around to join up in an ACW formation
		};

		// This loop makes sure that we get the same answer regardless of where the vertices appear in the list (so long as their overall order doesn't change)
		for (var i = 0; i < 5; ++i) {
			Assert.AreEqual(Direction.Backward, Polygon.CalculateNormalForAnticlockwiseCoplanarVertices(acwVertsWithCwTriangleEmbedded));
			acwVertsWithCwTriangleEmbedded = new[] {
				acwVertsWithCwTriangleEmbedded[1],
				acwVertsWithCwTriangleEmbedded[2],
				acwVertsWithCwTriangleEmbedded[3],
				acwVertsWithCwTriangleEmbedded[4],
				acwVertsWithCwTriangleEmbedded[0],
			};
		}

		// Now repeat again but expecting the opposite order for opposite ordering
		var cwVertsWithAcwTriangleEmbedded = acwVertsWithCwTriangleEmbedded.Reverse().ToArray();
		for (var i = 0; i < 5; ++i) {
			Assert.AreEqual(Direction.Forward, Polygon.CalculateNormalForAnticlockwiseCoplanarVertices(cwVertsWithAcwTriangleEmbedded));
			cwVertsWithAcwTriangleEmbedded = new[] {
				cwVertsWithAcwTriangleEmbedded[1],
				cwVertsWithAcwTriangleEmbedded[2],
				cwVertsWithAcwTriangleEmbedded[3],
				cwVertsWithAcwTriangleEmbedded[4],
				cwVertsWithAcwTriangleEmbedded[0],
			};
		}
	}

	[Test]
	public void ShouldCorrectlyImplementEquality() {
		// ReSharper disable EqualExpressionComparison
		Assert.AreEqual(true, AcwCircle.Equals(AcwCircle));
		Assert.AreEqual(true, AcwCircle == AcwCircle);
		Assert.AreEqual(false, CwCircle.Equals(AcwCircle));
		Assert.AreEqual(false, CwCircle == AcwCircle);
		
		Assert.AreEqual(false, AcwPoint == CwPoint);
		Assert.AreEqual(true, AcwPoint != CwPoint);

		Assert.AreEqual(false, CreatePolygon(Direction.Up, true, Location.Origin) == CreatePolygon(Direction.Down, true, Location.Origin));
		Assert.AreEqual(true, CreatePolygon(Direction.Up, true, Location.Origin) != CreatePolygon(Direction.Down, true, Location.Origin));
		// ReSharper restore EqualExpressionComparison

		Assert.AreEqual(true, CreatePolygon(Direction.Up, new Location(0f, 0f, 0f)).Equals(CreatePolygon(Direction.Up, new Location(0f, 0f, 0f)), 0f));
		Assert.AreEqual(true, CreatePolygon(Direction.Up, new Location(0f, 0f, 0f)).Equals(CreatePolygon(Direction.Up, new Location(0f, 0f, 0f)), 0.1f));
		Assert.AreEqual(false, CreatePolygon(Direction.Up, new Location(0f, 0f, 0f)).Equals(CreatePolygon(Direction.Up, new Location(0f, 0f, 1f)), 0.9f));
		Assert.AreEqual(true, CreatePolygon(Direction.Up, new Location(0f, 0f, 0f)).Equals(CreatePolygon(Direction.Up, new Location(0f, 0f, 1f)), 1.1f));
		Assert.AreEqual(false, CreatePolygon(Direction.Down, new Location(0f, 0f, 0f)).Equals(CreatePolygon(Direction.Up, new Location(0f, 0f, 1f)), 1.1f));
		Assert.AreEqual(false, CreatePolygon(Direction.Up, true, new Location(0f, 0f, 0f)).Equals(CreatePolygon(Direction.Up, false, new Location(0f, 0f, 1f)), 1.1f));
	}

	[Test]
	public void ShouldCorrectlyCalculateCentroid() {
		AssertToleranceEquals((0f, 0f, 0f), AcwTri.Centroid, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), CwTri.Centroid, TestTolerance);

		AssertToleranceEquals((0f, 0f, 0f), AcwSquare.Centroid, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), CwSquare.Centroid, TestTolerance);

		AssertToleranceEquals((0f, 0f, 0f), AcwCircle.Centroid, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), CwCircle.Centroid, TestTolerance);

		AssertToleranceEquals((0f, 0f, 0f), AcwLine.Centroid, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), CwLine.Centroid, TestTolerance);

		AssertToleranceEquals((0f, 0f, 0f), AcwPoint.Centroid, TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), CwPoint.Centroid, TestTolerance);



		var buffer = new Location[100];
		void MovePolyAndTestCentroid(Polygon p) {
			var offset = Vect.Random();
			for (var v = 0; v < p.VertexCount; ++v) {
				buffer[v] = p.Vertices[v] + offset;
			}

			AssertToleranceEquals(offset.AsLocation(), CreatePolygon(p.Normal, p.IsWoundClockwise, buffer.AsSpan(0, p.VertexCount).ToArray()).Centroid, TestTolerance);
		}
		for (var i = 0; i < 1000; ++i) {
			MovePolyAndTestCentroid(AcwTri);
			MovePolyAndTestCentroid(CwTri);
			MovePolyAndTestCentroid(AcwSquare);
			MovePolyAndTestCentroid(CwSquare);
			MovePolyAndTestCentroid(AcwCircle);
			MovePolyAndTestCentroid(CwCircle);
			MovePolyAndTestCentroid(AcwLine);
			MovePolyAndTestCentroid(CwLine);
			MovePolyAndTestCentroid(AcwPoint);
			MovePolyAndTestCentroid(CwPoint);
		}
	}

	[Test]
	public unsafe void ShouldCorrectlyConvertTo2DPolygon() {
		var buffer = new XYPair<float>[100];

		static string PolyToString(Polygon p) => String.Join(", ", p.Vertices.ToArray().Select(v => v.ToString()));
		static string Poly2DToString(Polygon2D p) => String.Join(", ", p.Vertices.ToArray().Select(v => v.ToString()));

		// Because the conversion to 2D without a specific dimension converter has
		// no "correct" orientation we just make sure our expectation matches in at least one orientation.
		// This is fine-- the vertices will still be in the right winding order and still ordered correctly with respect to each other.
		// Note that this only works at all when p's Normal is axis-aligned I think. Otherwise we need smarter code to make sure the vertices
		// in the 2D poly simply form the same shape but their actual rotation could be anything.
		void AssertAxisAlignedPolygon(Polygon2D expectation, Polygon p) {
			for (var i = 0; i < 4; ++i) {
				if (expectation.Equals(p.ToPolygon2D(buffer), TestTolerance)) return;
				expectation = new Polygon2D(expectation.Vertices.ToArray().Select(v => v.RotatedAroundOriginBy(90f)).ToArray(), expectation.IsWoundClockwise);
			}
			AssertToleranceEquals(expectation, p.ToPolygon2D(buffer), TestTolerance, &Poly2DToString); // Invoke this to throw assertion fail here
		}
		
		AssertAxisAlignedPolygon(new(new XYPair<float>[] { (0f, 1f), (-1.5f, -0.5f), (1.5f, -0.5f) }, false), AcwTri);
		AssertAxisAlignedPolygon(new(new XYPair<float>[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }, false), AcwSquare);
		AssertAxisAlignedPolygon(new(Enumerable.Range(0, 100).Select(i => new XYPair<float>(0f, 1f).RotatedAroundOriginBy(90f * (i / 25f))).ToArray(), false), AcwCircle);
		AssertAxisAlignedPolygon(new(new XYPair<float>[] { (-1f, 0f), (1f, 0f) }, false), AcwLine);
		AssertAxisAlignedPolygon(new(new XYPair<float>[] { (0f, 0f) }, false), AcwPoint);

		AssertAxisAlignedPolygon(new(new XYPair<float>[] { (0f, 1f), (1.5f, -0.5f), (-1.5f, -0.5f) }, true), CwTri);
		AssertAxisAlignedPolygon(new(new XYPair<float>[] { (-1f, 1f), (1f, 1f), (1f, -1f), (-1f, -1f) }, true), CwSquare);
		AssertAxisAlignedPolygon(new(Enumerable.Range(0, 100).Select(i => new XYPair<float>(0f, 1f).RotatedAroundOriginBy(-90f * (i / 25f))).ToArray(), true), CwCircle);
		AssertAxisAlignedPolygon(new(new XYPair<float>[] { (1f, 0f), (-1f, 0f) }, true), CwLine);
		AssertAxisAlignedPolygon(new(new XYPair<float>[] { (0f, 0f) }, true), CwPoint);

		AssertAxisAlignedPolygon(new(new XYPair<float>[] { (0f, 1f), (-1.5f, -0.5f), (1.5f, -0.5f) }, false), new Polygon(CwTri.Vertices, Direction.Forward, false));
		AssertAxisAlignedPolygon(new(new XYPair<float>[] { (0f, 1f), (1.5f, -0.5f), (-1.5f, -0.5f) }, true), new Polygon(AcwTri.Vertices, Direction.Forward, true));
		
		for (var i = 0; i < 1000; ++i) {
			var rotation = Rotation.Random();
			var rotatedAcwCircle = new Polygon(AcwCircle.Vertices.ToArray().Select(v => v.RotatedAroundOriginBy(rotation)).ToArray(), AcwCircle.Normal * rotation, false);
			var rotatedCwCircle = new Polygon(CwCircle.Vertices.ToArray().Select(v => v.RotatedAroundOriginBy(rotation)).ToArray(), CwCircle.Normal * rotation, false);

			var poly2D = rotatedAcwCircle.ToPolygon2D(buffer);
			for (var v = 1; v < poly2D.VertexCount; ++v) {
				AssertToleranceEquals(3.6f, poly2D.Vertices[v - 1].SignedAngleTo(poly2D.Vertices[v]), 0.1f);
				AssertToleranceEquals(poly2D.Vertices[v - 1].Length, poly2D.Vertices[v].Length, 0.1f);
			}

			poly2D = rotatedCwCircle.ToPolygon2D(buffer);
			for (var v = 1; v < poly2D.VertexCount; ++v) {
				AssertToleranceEquals(-3.6f, poly2D.Vertices[v - 1].SignedAngleTo(poly2D.Vertices[v]), 0.1f);
				AssertToleranceEquals(poly2D.Vertices[v - 1].Length, poly2D.Vertices[v].Length, 0.1f);
			}
		}
	}
}