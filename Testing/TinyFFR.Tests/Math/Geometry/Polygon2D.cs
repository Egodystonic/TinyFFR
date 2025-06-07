// Created on 2025-02-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Assets.Meshes;
using NUnit.Framework.Internal;
using Vertex = Egodystonic.TinyFFR.XYPair<float>;
using Edge = Egodystonic.TinyFFR.Pair<Egodystonic.TinyFFR.XYPair<float>, Egodystonic.TinyFFR.XYPair<float>>;

namespace Egodystonic.TinyFFR;

[TestFixture]
class Polygon2DTest {
	const float TestTolerance = 1E-3f;

	Polygon2D CreatePolygon(params Vertex[] vertices) => new(vertices);
	Polygon2D CreatePolygon(bool isWoundClockwise, params Vertex[] vertices) => new(vertices, isWoundClockwise);
	// Polygon2D CreatePrecalcPolygon(params Vertex[] vertices) => Polygon2D.FromVerticesWithGeometricPrecalculations(vertices);
	// Polygon2D CreatePrecalcPolygon(bool isWoundClockwise, params Vertex[] vertices) => Polygon2D.FromVerticesWithGeometricPrecalculations(vertices, isWoundClockwise);

	Polygon2D AcwTri => CreatePolygon(false, (0f, 1f), (-1.5f, -0.5f), (1.5f, -0.5f));
	Polygon2D AcwSquare => CreatePolygon(false, (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f));
	Polygon2D AcwCircle => CreatePolygon(false, Enumerable.Range(0, 100).Select(i => new Vertex(0f, 1f).RotatedAroundOriginBy(3.6f * i)).ToArray());
	Polygon2D AcwLine => CreatePolygon(false, (-1f, 0f), (1f, 0f));
	Polygon2D AcwPoint => CreatePolygon(false, (0f, 0f));
	Polygon2D CwTri => CreatePolygon(true, (0f, 1f), (1.5f, -0.5f), (-1.5f, -0.5f));
	Polygon2D CwSquare => CreatePolygon(true, (-1f, 1f), (1f, 1f), (1f, -1f), (-1f, -1f));
	Polygon2D CwCircle => CreatePolygon(true, Enumerable.Range(0, 100).Select(i => new Vertex(0f, 1f).RotatedAroundOriginBy(-3.6f * i)).ToArray());
	Polygon2D CwLine => CreatePolygon(true, (-1f, 0f), (1f, 0f));
	Polygon2D CwPoint => CreatePolygon(true, (0f, 0f));
	// Polygon2D PrecalcAcwTri => CreatePrecalcPolygon(false, (0f, 1f), (1.5f, -0.5f), (-1.5f, -0.5f));
	// Polygon2D PrecalcAcwSquare => CreatePrecalcPolygon(false, (1f, -1f), (-1f, -1f), (-1f, 1f), (1f, 1f));
	// Polygon2D PrecalcAcwCircle => CreatePrecalcPolygon(false, Enumerable.Range(0, 100).Select(i => new Vertex(0f, 1f).RotatedAroundOriginBy(3.6f * i)).ToArray());
	// Polygon2D PrecalcAcwLine => CreatePrecalcPolygon(false, (1f, 0f), (-1f, 0f));
	// Polygon2D PrecalcAcwPoint => CreatePrecalcPolygon(false, (0f, 0f));
	// Polygon2D PrecalcCwTri => CreatePrecalcPolygon(true, (0f, 1f), (-1.5f, -0.5f), (1.5f, -0.5f));
	// Polygon2D PrecalcCwSquare => CreatePrecalcPolygon(true, (1f, 1f), (-1f, 1f), (-1f, -1f), (1f, -1f));
	// Polygon2D PrecalcCwCircle => CreatePrecalcPolygon(true, Enumerable.Range(0, 100).Select(i => new Vertex(0f, 1f).RotatedAroundOriginBy(-3.6f * i)).ToArray());
	// Polygon2D PrecalcCwLine => CreatePrecalcPolygon(true, (1f, 0f), (-1f, 0f));
	// Polygon2D PrecalcCwPoint => CreatePrecalcPolygon(true, (0f, 0f));

	[Test]
	public void ShouldCorrectlySetProperties() {
		void AssertPoly(Polygon2D p, int expectedVertexCount, int expectedEdgeCount, int expectedTriangleCount, bool expectedWinding) {
			Assert.AreEqual(expectedVertexCount, p.VertexCount);
			Assert.AreEqual(expectedVertexCount, p.Vertices.Length);
			Assert.AreEqual(expectedEdgeCount, p.EdgeCount);
			Assert.AreEqual(expectedTriangleCount, p.TriangleCount);
			Assert.AreEqual(expectedWinding, p.IsWoundClockwise);
		}
		
		Assert.AreEqual(new Vertex(0f, 1f), AcwTri.Vertices[0]);
		Assert.AreEqual(new Vertex(-1.5f, -0.5f), AcwTri.Vertices[1]);
		Assert.AreEqual(new Vertex(1.5f, -0.5f), AcwTri.Vertices[2]);

		AssertPoly(AcwTri, 3, 3, 1, false);
		AssertPoly(AcwSquare, 4, 4, 2, false);
		AssertPoly(AcwCircle, 100, 100, 98, false);
		AssertPoly(AcwLine, 2, 1, 0, false);
		AssertPoly(AcwPoint, 1, 0, 0, false);
		AssertPoly(CwTri, 3, 3, 1, true);
		AssertPoly(CwSquare, 4, 4, 2, true);
		AssertPoly(CwCircle, 100, 100, 98, true);
		AssertPoly(CwLine, 2, 1, 0, true);
		AssertPoly(CwPoint, 1, 0, 0, true);
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
		// ReSharper restore EqualExpressionComparison

		Assert.AreEqual(true, CreatePolygon(new Vertex(0f, 0f)).Equals(CreatePolygon(new Vertex(0f, 0f)), 0f));
		Assert.AreEqual(true, CreatePolygon(new Vertex(0f, 0f)).Equals(CreatePolygon(new Vertex(0f, 0f)), 0.1f));
		Assert.AreEqual(false, CreatePolygon(new Vertex(0f, 0f)).Equals(CreatePolygon(new Vertex(0f, 1f)), 0.9f));
		Assert.AreEqual(false, CreatePolygon(true, new Vertex(0f, 0f)).Equals(CreatePolygon(false, new Vertex(0f, 1f)), 1.1f));
	}

	[Test]
	public void ShouldCorrectlyCalculateCentroid() {
		AssertToleranceEquals((0f, 0f), AcwTri.Centroid, TestTolerance);
		AssertToleranceEquals((0f, 0f), CwTri.Centroid, TestTolerance);

		AssertToleranceEquals((0f, 0f), AcwSquare.Centroid, TestTolerance);
		AssertToleranceEquals((0f, 0f), CwSquare.Centroid, TestTolerance);

		AssertToleranceEquals((0f, 0f), AcwCircle.Centroid, TestTolerance);
		AssertToleranceEquals((0f, 0f), CwCircle.Centroid, TestTolerance);

		AssertToleranceEquals((0f, 0f), AcwLine.Centroid, TestTolerance);
		AssertToleranceEquals((0f, 0f), CwLine.Centroid, TestTolerance);

		AssertToleranceEquals((0f, 0f), AcwPoint.Centroid, TestTolerance);
		AssertToleranceEquals((0f, 0f), CwPoint.Centroid, TestTolerance);



		var buffer = new Vertex[100];
		void MovePolyAndTestCentroid(Polygon2D p) {
			var offset = Vertex.Random();
			for (var v = 0; v < p.VertexCount; ++v) {
				buffer[v] = p.Vertices[v] + offset;
			}

			AssertToleranceEquals(offset, CreatePolygon(p.IsWoundClockwise, buffer.AsSpan(0, p.VertexCount).ToArray()).Centroid, TestTolerance);
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
	public void ShouldCorrectlyTriangulate() {
		void AssertTriangulation(Polygon2D poly, params VertexTriangle[] expectation) {
			var buffer = new VertexTriangle[expectation.Length];
			poly.Triangulate(buffer);
			try {
				Assert.IsTrue(expectation.SequenceEqual(buffer));
			}
			catch (Exception) {
				Console.WriteLine($"Expectation: {String.Join(" | ", expectation)}");
				Console.WriteLine($"     Actual: {String.Join(" | ", buffer)}");
				throw;
			}
		}

		// Note: If we change the algorithm these tests may break, they currently rely on the specific order
		// the ear-clipping algorithm checks vertices. However, there are many correct possible solutions for the triangulation,
		// so if these are failing it is not necessarily indicative of a broken algorithm-- just a changed one. Feel free to change this
		// if you're 100% sure.

		var circleExpectation = new VertexTriangle[98];
		for (var i = 0; i < 97; ++i) {
			circleExpectation[i] = new(99, i, i + 1);
		}
		circleExpectation[^1] = new(97, 98, 99);

		AssertTriangulation(AcwTri, new VertexTriangle(0, 1, 2));
		AssertTriangulation(AcwSquare, new VertexTriangle(3, 0, 1), new(1, 2, 3));
		AssertTriangulation(AcwCircle, circleExpectation);
		AssertTriangulation(AcwLine, default(VertexTriangle));
		AssertTriangulation(AcwPoint, default(VertexTriangle));

		AssertTriangulation(CwTri, new VertexTriangle(0, 1, 2));
		AssertTriangulation(CwSquare, new VertexTriangle(3, 0, 1), new(1, 2, 3));
		AssertTriangulation(CwCircle, circleExpectation);
		AssertTriangulation(CwLine, default(VertexTriangle));
		AssertTriangulation(CwPoint, default(VertexTriangle));

		var buffer = new VertexTriangle[100];

		// Tests that we throw exceptions when wrong winding order is supplied
		Assert.Catch(() => CreatePolygon(isWoundClockwise: false, (-1f, 1f), (1f, 1f), (1f, -1f), (-1f, -1f)).Triangulate(buffer));
		Assert.Catch(() => CreatePolygon(isWoundClockwise: true, (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f)).Triangulate(buffer));

		// Tests that we throw exceptions when polygon is degenerate
		var nonSimpleVertices = new Vertex[] {
			(-1, -1f),
			(1f, 1f),
			(-1f, 1f),
			(1f, -1f),
			(0f, 1f),
			(1f, 0f),
			(-1f, 0f),
			(0f, -2f)
		};
		Assert.Catch(() => CreatePolygon(isWoundClockwise: false, nonSimpleVertices).Triangulate(buffer));
		Assert.Catch(() => CreatePolygon(isWoundClockwise: true, nonSimpleVertices).Triangulate(buffer));

		// Tests for simple and correct polygons that have trickier geometry
		var acwVertsWithCwTriangleEmbedded = new Vertex[] {
			(0f, 1f), (1.5f, -0.5f), (-1.5f, -0.5f), // First three points are a CW triangle
			(-1.5f, -1.5f), (4f, -1.5f) // Last two points take it back around to join up in an ACW formation
		};
		AssertTriangulation(
			CreatePolygon(isWoundClockwise: false, acwVertsWithCwTriangleEmbedded),
			new VertexTriangle(4, 0, 1),
			new VertexTriangle(4, 1, 2),
			new VertexTriangle(2, 3, 4)
		);

		// We now make the first three verts tested in the algorithm (4, 0, 1) form a cw triangle, and check that the answer changes appropriately
		acwVertsWithCwTriangleEmbedded = new Vertex[] {
			(1.5f, -0.5f), (-1.5f, -0.5f),
			(-1.5f, -1.5f), (4f, -1.5f), (0f, 1f)
		};
		AssertTriangulation(
			CreatePolygon(isWoundClockwise: false, acwVertsWithCwTriangleEmbedded),
			new VertexTriangle(0, 1, 2),
			new VertexTriangle(0, 2, 3),
			new VertexTriangle(0, 3, 4)
		);
	}
}