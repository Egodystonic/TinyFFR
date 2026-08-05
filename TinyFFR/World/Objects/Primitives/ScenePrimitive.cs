// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using static Egodystonic.TinyFFR.World.ScenePrimitive;

namespace Egodystonic.TinyFFR.World;

public readonly record struct ScenePrimitive : IDisposable {
	public const bool DefaultConstantScreenSizeFlag = true;
	public const bool DefaultIncludeEndpointsFlag = true;
	public const Magnitude DefaultMagnitude = Magnitude.Small;
	internal const float PointToLineSizeRatio = 0.2f;
	internal const float PointToLineSizeRatioReciprocal = 1f / PointToLineSizeRatio;
	internal const float DefaultGridSize = 2f;
	internal const float DefaultGridMajorLines = 8f;
	internal const float DefaultGridMinorLines = 64f;
	public static readonly PrimitivePaintbrush DefaultPaintbrush2d = new(ColorVect.WhiteOpaque, ColorVect.BlackOpaque);
	public static readonly PrimitivePaintbrush DefaultPaintbrush3d = new(ColorVect.WhiteOpaque);
	public static readonly PrimitivePaintbrush DefaultPaintbrushGrid = new(ColorVect.RedOpaque, ColorVect.WhiteOpaque, new ColorVect(0.35f, 0.35f, 0.35f, 1f));
	
	readonly Scene _parentScene;
	readonly nuint _primitiveHandle;

	internal ScenePrimitive(Scene parentScene, nuint primitiveHandle) {
		_parentScene = parentScene;
		_primitiveHandle = primitiveHandle;
	}
	
	public static float ConvertPointMagnitudeToSize(Magnitude m) {
		return m switch {
			Magnitude.VerySmall => 0.005f,
			Magnitude.Small => 0.0125f,
			Magnitude.Large => 0.0275f,
			Magnitude.VeryLarge => 0.035f,
			_ => 0.02f
		};
	}
	public static float ConvertStringMagnitudeToSize(Magnitude m) {
		return m switch {
			Magnitude.VerySmall => 0.0125f,
			Magnitude.Small => 0.02f,
			Magnitude.Large => 0.035f,
			Magnitude.VeryLarge => 0.05f,
			_ => 0.0275f
		};
	}
	public static float ConvertLineMagnitudeToSize(Magnitude m) => ConvertPointMagnitudeToSize(m) * PointToLineSizeRatio;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPaintbrush(in PrimitivePaintbrush paintbrush) => _parentScene.SetPrimitivePaintbrush(_primitiveHandle, in paintbrush);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => _parentScene.DisposePrimitive(_primitiveHandle);
	
	public void SetGeometryPoint(Location point, Magnitude size = DefaultMagnitude, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometryPoint(point, ConvertPointMagnitudeToSize(size), constantScreenSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryPoint(Location point, float size, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometryPoint(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, point, size, constantScreenSize);
	
	public void SetGeometryString(Location position, ReadOnlySpan<char> str, Magnitude size = DefaultMagnitude, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometryString(position, str, ConvertStringMagnitudeToSize(size), constantScreenSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryString(Location position, ReadOnlySpan<char> str, float size, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometryString(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, position, str, size, constantScreenSize);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(PositionedRotatedCuboid cuboid, bool wireframe = false) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, cuboid, wireframe);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(PositionedSphere sphere, bool wireframe = false) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, sphere, wireframe);
	
	public void SetGeometryShape(BoundedRay ray, Magnitude size = DefaultMagnitude, bool includeEndpoints = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometryShape(ray, ConvertLineMagnitudeToSize(size), includeEndpoints, constantScreenSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(BoundedRay ray, float size, bool includeEndpoints = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, ray, size, includeEndpoints, constantScreenSize);
	
	public void SetGeometryShape(Ray ray, Magnitude size = DefaultMagnitude, bool includeStartPoint = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometryShape(ray, ConvertLineMagnitudeToSize(size), includeStartPoint, constantScreenSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(Ray ray, float size, bool includeStartPoint = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, ray, size, includeStartPoint, constantScreenSize);
	
	public void SetGeometryShape(Line line, Magnitude size = DefaultMagnitude, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometryShape(line, ConvertLineMagnitudeToSize(size), constantScreenSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(Line line, float size, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, line, size, constantScreenSize);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(Plane plane) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, plane);
	
	public void SetGeometryGrid(Location gridCentre, Direction? gridNormal = null, Direction? gridX = null, float? gridSize = null, float? majorGridLineSpacing = null, float? minorGridLineSpacing = null) {
		var normal = gridNormal ?? Direction.Up;
		var size = gridSize ?? DefaultGridSize;
		_parentScene.Implementation.SetPrimitiveGeometryGrid(
			_parentScene.GetHandleWithoutDisposeCheck(),
			_primitiveHandle,
			gridCentre,
			normal,
			gridX ?? Direction.Left.OrthogonalizedAgainst(normal) ?? Direction.Forward.OrthogonalizedAgainst(normal) ?? Direction.Up.OrthogonalizedAgainst(normal) ?? normal.AnyOrthogonal(),
			size,
			majorGridLineSpacing ?? size * (1f / DefaultGridMajorLines),
			minorGridLineSpacing ?? size * (1f / DefaultGridMinorLines)
		);
	}

	public override string ToString() => "Scene Primitive";
}

partial struct Scene {
	public ScenePrimitive AddPrimitivePoint(Location point, Magnitude size = DefaultMagnitude, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitivePoint(point, in DefaultPaintbrush2d, ConvertPointMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitivePoint(Location point, in PrimitivePaintbrush paintbrush, Magnitude size = DefaultMagnitude, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitivePoint(point, in paintbrush, ConvertPointMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitivePoint(Location point, in PrimitivePaintbrush paintbrush, float size, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryPoint(point, size, constantScreenSize);
		return result;
	}
	
	public ScenePrimitive AddPrimitiveString(Location position, ReadOnlySpan<char> str, Magnitude size = DefaultMagnitude, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveString(position, str, in DefaultPaintbrush2d, ConvertStringMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitiveString(Location position, ReadOnlySpan<char> str, in PrimitivePaintbrush paintbrush, Magnitude size = DefaultMagnitude, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveString(position, str, in paintbrush, ConvertStringMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitiveString(Location position, ReadOnlySpan<char> str, in PrimitivePaintbrush paintbrush, float size, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryString(position, str, size, constantScreenSize);
		return result;
	}
	
	public ScenePrimitive AddPrimitiveShape(PositionedRotatedCuboid cuboid, bool wireframe = false) => AddPrimitiveShape(cuboid, in DefaultPaintbrush3d, wireframe);
	public ScenePrimitive AddPrimitiveShape(PositionedRotatedCuboid cuboid, in PrimitivePaintbrush paintbrush, bool wireframe = false) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryShape(cuboid, wireframe);
		return result;
	}
	
	public ScenePrimitive AddPrimitiveShape(PositionedSphere sphere, bool wireframe = false) => AddPrimitiveShape(sphere, in DefaultPaintbrush3d, wireframe);
	public ScenePrimitive AddPrimitiveShape(PositionedSphere sphere, in PrimitivePaintbrush paintbrush, bool wireframe = false) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryShape(sphere, wireframe);
		return result;
	}
	
	public ScenePrimitive AddPrimitiveShape(BoundedRay ray, Magnitude size = DefaultMagnitude, bool includeEndpoints = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(ray, in DefaultPaintbrush2d, ConvertLineMagnitudeToSize(size), includeEndpoints, constantScreenSize);
	public ScenePrimitive AddPrimitiveShape(BoundedRay ray, in PrimitivePaintbrush paintbrush, Magnitude size = DefaultMagnitude, bool includeEndpoints = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(ray, in paintbrush, ConvertLineMagnitudeToSize(size), includeEndpoints, constantScreenSize);
	public ScenePrimitive AddPrimitiveShape(BoundedRay ray, in PrimitivePaintbrush paintbrush, float size, bool includeEndpoints, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryShape(ray, size, includeEndpoints, constantScreenSize);
		return result;
	}
	
	public ScenePrimitive AddPrimitiveShape(Ray ray, Magnitude size = DefaultMagnitude, bool includeStartPoint = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(ray, in DefaultPaintbrush2d, ConvertLineMagnitudeToSize(size), includeStartPoint, constantScreenSize);
	public ScenePrimitive AddPrimitiveShape(Ray ray, in PrimitivePaintbrush paintbrush, Magnitude size = DefaultMagnitude, bool includeStartPoint = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(ray, in paintbrush, ConvertLineMagnitudeToSize(size), includeStartPoint, constantScreenSize);
	public ScenePrimitive AddPrimitiveShape(Ray ray, in PrimitivePaintbrush paintbrush, float size, bool includeStartPoint, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryShape(ray, size, includeStartPoint, constantScreenSize);
		return result;
	}
	
	public ScenePrimitive AddPrimitiveShape(Line line, Magnitude size = DefaultMagnitude, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(line, in DefaultPaintbrush2d, ConvertLineMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitiveShape(Line line, in PrimitivePaintbrush paintbrush, Magnitude size = DefaultMagnitude, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(line, in paintbrush, ConvertLineMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitiveShape(Line line, in PrimitivePaintbrush paintbrush, float size, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryShape(line, size, constantScreenSize);
		return result;
	}
	
	public ScenePrimitive AddPrimitiveShape(Plane plane) => AddPrimitiveShape(plane, in DefaultPaintbrush3d);
	public ScenePrimitive AddPrimitiveShape(Plane plane, in PrimitivePaintbrush paintbrush) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryShape(plane);
		return result;
	}
	
	public ScenePrimitive AddPrimitiveGrid(Location gridCentre, Direction? gridNormal = null, Direction? gridX = null, float? gridSize = null, float? majorGridLineSpacing = null, float? minorGridLineSpacing = null) => AddPrimitiveGrid(gridCentre, in DefaultPaintbrushGrid, gridNormal, gridX, gridSize, majorGridLineSpacing, minorGridLineSpacing);
	public ScenePrimitive AddPrimitiveGrid(Location gridCentre, in PrimitivePaintbrush paintbrush, Direction? gridNormal = null, Direction? gridX = null, float? gridSize = null, float? majorGridLineSpacing = null, float? minorGridLineSpacing = null) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryGrid(gridCentre, gridNormal, gridX, gridSize, majorGridLineSpacing, minorGridLineSpacing);
		return result;
	}
}