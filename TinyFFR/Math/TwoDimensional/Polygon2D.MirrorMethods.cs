// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Edge = Egodystonic.TinyFFR.Pair<Egodystonic.TinyFFR.XYPair<float>, Egodystonic.TinyFFR.XYPair<float>>;

namespace Egodystonic.TinyFFR;

partial struct XYPair<T> : 
	IDistanceMeasurable<Polygon2D>, 
	IContainable<Polygon2D> {
	public float DistanceFrom(Polygon2D polygon) => polygon.DistanceFrom(Cast<float>());
	public float DistanceSquaredFrom(Polygon2D polygon) => polygon.DistanceSquaredFrom(Cast<float>());
	public bool IsContainedWithin(Polygon2D polygon) => polygon.Contains(Cast<float>());
	public XYPair<float> ClosestPointOn(Polygon2D polygon) => polygon.PointClosestTo(Cast<float>());
	public Edge ClosestEdgeOn(Polygon2D polygon) => polygon.EdgeClosestTo(Cast<float>());
}