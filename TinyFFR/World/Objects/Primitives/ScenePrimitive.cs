// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using static Egodystonic.TinyFFR.World.ScenePrimitive;

namespace Egodystonic.TinyFFR.World;

public enum ScenePrimitiveSize {
	Medium,
	VerySmall,
	Small,
	Large,
	VeryLarge
}

public readonly record struct ScenePrimitive : IDisposable {
	public const bool DefaultConstantScreenSizeFlag = true;
	public const bool DefaultIncludeEndpointsFlag = true;
	public const ScenePrimitiveSize DefaultSize = ScenePrimitiveSize.Small;
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
	
	public static float ConvertPointPrimitiveSize(ScenePrimitiveSize m) {
		return m switch {
			ScenePrimitiveSize.VerySmall => 0.005f,
			ScenePrimitiveSize.Small => 0.0125f,
			ScenePrimitiveSize.Large => 0.0275f,
			ScenePrimitiveSize.VeryLarge => 0.035f,
			_ => 0.02f
		};
	}
	public static float ConvertStringPrimitiveSize(ScenePrimitiveSize m) {
		return m switch {
			ScenePrimitiveSize.VerySmall => 0.0125f,
			ScenePrimitiveSize.Small => 0.02f,
			ScenePrimitiveSize.Large => 0.035f,
			ScenePrimitiveSize.VeryLarge => 0.05f,
			_ => 0.0275f
		};
	}
	public static float ConvertLinePrimitiveSize(ScenePrimitiveSize m) => ConvertPointPrimitiveSize(m) * PointToLineSizeRatio;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPaintbrush(in PrimitivePaintbrush paintbrush) => _parentScene.SetPrimitivePaintbrush(_primitiveHandle, in paintbrush);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => _parentScene.DisposePrimitive(_primitiveHandle);
	
	public void SetGeometryPoint(Location point, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometryPoint(point, ConvertPointPrimitiveSize(size), constantScreenSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryPoint(Location point, float size, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometryPoint(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, point, size, constantScreenSize);
	
	public void SetGeometryString(Location position, ReadOnlySpan<char> str, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometryString(position, str, ConvertStringPrimitiveSize(size), constantScreenSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryString(Location position, ReadOnlySpan<char> str, float size, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometryString(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, position, str, size, constantScreenSize);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(PositionedRotatedCuboid cuboid, bool wireframe = false) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, cuboid, wireframe);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(PositionedSphere sphere, bool wireframe = false) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, sphere, wireframe);
	
	public void SetGeometryShape(BoundedRay ray, ScenePrimitiveSize size = DefaultSize, bool includeEndpoints = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometryShape(ray, ConvertLinePrimitiveSize(size), includeEndpoints, constantScreenSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(BoundedRay ray, float size, bool includeEndpoints = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, ray, size, includeEndpoints, constantScreenSize);
	
	public void SetGeometryShape(Ray ray, ScenePrimitiveSize size = DefaultSize, bool includeStartPoint = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometryShape(ray, ConvertLinePrimitiveSize(size), includeStartPoint, constantScreenSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(Ray ray, float size, bool includeStartPoint = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, ray, size, includeStartPoint, constantScreenSize);
	
	public void SetGeometryShape(Line line, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometryShape(line, ConvertLinePrimitiveSize(size), constantScreenSize);
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
	public ScenePrimitive AddPrimitivePoint(Location point, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitivePoint(point, in DefaultPaintbrush2d, ConvertPointPrimitiveSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitivePoint(Location point, in PrimitivePaintbrush paintbrush, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitivePoint(point, in paintbrush, ConvertPointPrimitiveSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitivePoint(Location point, in PrimitivePaintbrush paintbrush, float size, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryPoint(point, size, constantScreenSize);
		return result;
	}
	
	public ScenePrimitive AddPrimitiveString(Location position, ReadOnlySpan<char> str, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveString(position, str, in DefaultPaintbrush2d, ConvertStringPrimitiveSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitiveString(Location position, ReadOnlySpan<char> str, in PrimitivePaintbrush paintbrush, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveString(position, str, in paintbrush, ConvertStringPrimitiveSize(size), constantScreenSize);
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
	
	public ScenePrimitive AddPrimitiveShape(BoundedRay ray, ScenePrimitiveSize size = DefaultSize, bool includeEndpoints = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(ray, in DefaultPaintbrush2d, ConvertLinePrimitiveSize(size), includeEndpoints, constantScreenSize);
	public ScenePrimitive AddPrimitiveShape(BoundedRay ray, in PrimitivePaintbrush paintbrush, ScenePrimitiveSize size = DefaultSize, bool includeEndpoints = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(ray, in paintbrush, ConvertLinePrimitiveSize(size), includeEndpoints, constantScreenSize);
	public ScenePrimitive AddPrimitiveShape(BoundedRay ray, in PrimitivePaintbrush paintbrush, float size, bool includeEndpoints, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryShape(ray, size, includeEndpoints, constantScreenSize);
		return result;
	}
	
	public ScenePrimitive AddPrimitiveShape(Ray ray, ScenePrimitiveSize size = DefaultSize, bool includeStartPoint = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(ray, in DefaultPaintbrush2d, ConvertLinePrimitiveSize(size), includeStartPoint, constantScreenSize);
	public ScenePrimitive AddPrimitiveShape(Ray ray, in PrimitivePaintbrush paintbrush, ScenePrimitiveSize size = DefaultSize, bool includeStartPoint = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(ray, in paintbrush, ConvertLinePrimitiveSize(size), includeStartPoint, constantScreenSize);
	public ScenePrimitive AddPrimitiveShape(Ray ray, in PrimitivePaintbrush paintbrush, float size, bool includeStartPoint, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryShape(ray, size, includeStartPoint, constantScreenSize);
		return result;
	}
	
	public ScenePrimitive AddPrimitiveShape(Line line, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(line, in DefaultPaintbrush2d, ConvertLinePrimitiveSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitiveShape(Line line, in PrimitivePaintbrush paintbrush, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(line, in paintbrush, ConvertLinePrimitiveSize(size), constantScreenSize);
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