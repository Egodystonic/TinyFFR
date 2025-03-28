﻿// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Threading;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources.Memory;
using Vertex = Egodystonic.TinyFFR.XYPair<float>;
using Edge = Egodystonic.TinyFFR.Pair<Egodystonic.TinyFFR.XYPair<float>, Egodystonic.TinyFFR.XYPair<float>>;

namespace Egodystonic.TinyFFR;

// TODO a lot of the stuff below is a nice-to-have but we will add it at a later date when I have time to properly test it. Check also Polygon2D.MirrorMethods.cs
partial struct Polygon2D /*: 
	IContainer<Polygon2D, Vertex>,
	IContainer<Polygon2D, Polygon2D>,
	IContainable<Polygon2D, Polygon2D>,
	IDistanceMeasurable<Polygon2D, Vertex>,
	IIntersectionDeterminable<Edge, Vertex>*/ {

	public Vertex Centroid {
		get {
			var result = Vertex.Zero;
			foreach (var vertex in Vertices) {
				result += vertex;
			}
			return result / VertexCount;
		}
	}

	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public Polygon ToPolygon(Span<Location> vertexDest, Direction normal) => ToPolygon(vertexDest, new DimensionConverter(normal));
	// public Polygon ToPolygon(Span<Location> vertexDest, DimensionConverter dimensionConverter) {
	// 	if (vertexDest.Length < VertexCount) {
	// 		throw new ArgumentException($"Destination span for converted vertices must be at least as large as '{nameof(VertexCount)}' ({VertexCount}).", nameof(vertexDest));
	// 	}
	//
	// 	for (var i = 0; i < VertexCount; ++i) vertexDest[i] = dimensionConverter.ConvertLocation(Vertices[i]);
	// 	return new(vertexDest, dimensionConverter.ZBasis, Single.IsPositiveInfinity(_containmentRadius));
	// }
	//
	// static Vertex? GetEdgeIntersection(Edge e1, Edge e2) {
	// 	// Implementation from https://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect
	// 	var qMinusP = e2.First - e1.First;
	// 	var r = e1.Second - e1.First;
	// 	var s = e2.Second - e2.First;
	// 	var qMinusPCrossR = qMinusP.Cross(r);
	// 	var rCrossS = r.Cross(s);
	//
	// 	// Parallel
	// 	if (rCrossS == 0f) {
	// 		// Not colinear
	// 		if (qMinusPCrossR != 0f) return null;
	// 		
	// 		var rLenSq = r.LengthSquared;
	// 		var t0 = qMinusP.Dot(r) / rLenSq;
	// 		var t1 = t0 + s.Dot(r) / rLenSq;
	//
	// 		// Colinear, but not overlapping
	// 		if (t0 is < 0f or > 1f && t1 is < 0f or > 1f) return null;
	// 		
	// 		// Overlapping
	// 		return e1.First + t0 * r;
	// 	}
	//
	// 	var t = qMinusP.Cross(s / rCrossS);
	// 	var u = qMinusP.Cross(r / rCrossS);
	//
	// 	// Infinitely extended lines meet outside the edge bounds
	// 	if (t is < 0f or > 1f || u is < 0f or > 1f) return null;
	//
	// 	// Meet inside the edge bounds
	// 	return e1.First + t * r;
	// }
	//
	// public Edge GetEdge(int index) {
	// 	var edgeCount = EdgeCount;
	// 	if (index >= edgeCount || index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, $"Expected positive value less than edge count ({EdgeCount}).");
	//
	// 	var secondVertexIndex = index + 1;
	// 	if (secondVertexIndex == VertexCount) secondVertexIndex = 0;
	// 	return new(Vertices[index], Vertices[secondVertexIndex]);
	// }
	//
	// public bool Contains(Vertex point) {
	// 	if (point.LengthSquared > _containmentRadiusSquared) return false;
	//
	// 	var rightVect = new Edge(point, new(point.X + _containmentRadius * 3f));
	//
	// 	var numIntersections = 0;
	// 	for (var i = 0; i < EdgeCount; ++i) {
	// 		if (GetEdgeIntersection(GetEdge(i), rightVect) != null) ++numIntersections;
	// 	}
	//
	// 	return (numIntersections & 0b1) == 0b1;
	// }
	// public bool Contains(Polygon2D polygon) {
	// 	if (polygon._containmentRadiusSquared > _containmentRadiusSquared && !Single.IsPositiveInfinity(polygon._containmentRadiusSquared)) return false;
	// 	foreach (var vertex in polygon.Vertices) {
	// 		if (!Contains(vertex)) return false;
	// 	}
	// 	return true;
	// }
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public bool IsContainedWithin(Polygon2D polygon) => polygon.Contains(this);
	//
	// public Edge EdgeClosestTo(Vertex point) {
	// 	var edgeCount = EdgeCount;
	// 	if (edgeCount < 1) throw new InvalidOperationException("This polygon has no edges.");
	// 	
	// 	var result = GetEdge(0);
	// 	var resultDist = point.ClosestPointOn2DBoundedRay(result.First, result.Second).DistanceSquaredFrom(point);
	// 	
	// 	for (var i = 1; i < edgeCount; ++i) {
	// 		var nextEdge = GetEdge(i);
	// 		var edgeDist = point.ClosestPointOn2DBoundedRay(nextEdge.First, nextEdge.Second).DistanceSquaredFrom(point);
	// 		if (edgeDist < resultDist) {
	// 			result = nextEdge;
	// 			resultDist = edgeDist;
	// 		}
	// 	}
	//
	// 	return result;
	// }
	//
	// public Vertex EdgePointClosestTo(Vertex point) {
	// 	var edgeCount = EdgeCount;
	// 	if (edgeCount < 1) throw new InvalidOperationException("This polygon has no edges.");
	//
	// 	var firstEdge = GetEdge(0);
	// 	var result = point.ClosestPointOn2DBoundedRay(firstEdge.First, firstEdge.Second);
	// 	var resultDist = point.DistanceSquaredFrom(result);
	//
	// 	for (var i = 1; i < edgeCount; ++i) {
	// 		var nextEdge = GetEdge(i);
	// 		var nextPoint = point.ClosestPointOn2DBoundedRay(nextEdge.First, nextEdge.Second);
	// 		var nextDist = point.DistanceSquaredFrom(nextPoint);
	// 		if (nextDist < resultDist) {
	// 			result = nextPoint;
	// 			resultDist = nextDist;
	// 		}
	// 	}
	//
	// 	return result;
	// }
	//
	// public Vertex PointClosestTo(Vertex point) {
	// 	if (Contains(point)) return point;
	// 	return EdgePointClosestTo(point);
	// }
	//
	// public float DistanceFrom(Vertex point) => PointClosestTo(point).DistanceFrom(point);
	// public float DistanceSquaredFrom(Vertex point) => PointClosestTo(point).DistanceSquaredFrom(point);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public bool IsIntersectedBy(Edge edge) => IntersectionWith(edge).HasValue;
	// public Vertex? IntersectionWith(Edge edge) {
	// 	for (var i = 0; i < EdgeCount; ++i) {
	// 		var possibleIntersection = GetEdgeIntersection(GetEdge(i), edge);
	// 		if (possibleIntersection != null) return possibleIntersection.Value;
	// 	}
	// 	return null;
	// }
	// Vertex IIntersectionDeterminable<Edge, Vertex>.FastIntersectionWith(Edge edge) => IntersectionWith(edge) ?? default;

	static readonly ThreadLocal<ArrayPoolBackedVector<int>> _threadStaticIndexList = new(() => new());
	public void Triangulate(Span<VertexTriangle> dest) {
		static bool CrossMatchesSign(Vertex centreVertex, Vertex previousVertex, Vertex nextVertex, int sign) {
			var prevToCentre = centreVertex - previousVertex;
			var centreToNext = nextVertex - centreVertex;
			return MathF.Sign(prevToCentre.Cross(centreToNext)) == sign;
		}
		static int GetNthNonClippedIndex(ArrayPoolBackedVector<int> clippedIndices, int n) {
			foreach (var clippedIndex in clippedIndices) {
				if (clippedIndex > n) break;
				n++;
			}
			return n;
		}
		static bool PointIsInsideTriangle(Vertex point, Vertex a, Vertex b, Vertex c) {
			var s1 = MathF.Sign((point - a).Cross(b - a));
			var s2 = MathF.Sign((point - b).Cross(c - b));
			var s3 = MathF.Sign((point - c).Cross(a - c));

			return s1 == s2 && s2 == s3;
		}

		var vertexCount = VertexCount;
		var triangleCount = TriangleCount;
		if (triangleCount < 1) return;
		if (dest.Length < triangleCount) {
			throw new ArgumentException($"Destination span is not large enough for this polygon. Required size = {TriangleCount}, actual size = {dest.Length}", nameof(dest));
		}

		var clippedIndices = _threadStaticIndexList.Value!;
		clippedIndices.ClearWithoutZeroingMemory();

		var desiredEarSign = IsWoundClockwise ? -1 : 1;

		var numVerticesRemaining = vertexCount;
		while (numVerticesRemaining > 3) {
			var foundAnEar = false;
			for (var i = 0; i < numVerticesRemaining; ++i) {
				var n = i;
				var nMinusOne = (i > 0 ? i : numVerticesRemaining) - 1;
				var nPlusOne = i < (numVerticesRemaining - 1) ? i + 1 : 0;

				var centreIndex = GetNthNonClippedIndex(clippedIndices, n);
				var prevIndex = GetNthNonClippedIndex(clippedIndices, nMinusOne);
				var nextIndex = GetNthNonClippedIndex(clippedIndices, nPlusOne);
				var a = Vertices[prevIndex];
				var b = Vertices[centreIndex];
				var c = Vertices[nextIndex];
				
				if (!CrossMatchesSign(b, a, c, desiredEarSign)) continue;

				var containsOtherVertices = false;
				for (var j = 0; j < numVerticesRemaining; ++j) {
					if (j == n || j == nMinusOne || j == nPlusOne) continue;
					if (PointIsInsideTriangle(Vertices[GetNthNonClippedIndex(clippedIndices, j)], a, b, c)) {
						containsOtherVertices = true;
						break;
					}
				}
				if (containsOtherVertices) continue;

				clippedIndices.Add(centreIndex);
				dest[vertexCount - numVerticesRemaining] = new(prevIndex, centreIndex, nextIndex);
				foundAnEar = true;
				break;
			}

			if (!foundAnEar) {
				throw new InvalidOperationException(
					"Could not triangulate the polygon, " +
					"it's possible the incorrect winding order was supplied or the polygon is degenerate/non-simple."
				);
			}
			--numVerticesRemaining;
		}

		var x = GetNthNonClippedIndex(clippedIndices, 0);
		var y = GetNthNonClippedIndex(clippedIndices, 1);
		var z = GetNthNonClippedIndex(clippedIndices, 2);
		dest[triangleCount - 1] = CrossMatchesSign(Vertices[y], Vertices[x], Vertices[z], desiredEarSign) ? new(x, y, z) : new(z, y, x);
	}
}