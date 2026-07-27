// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using static Egodystonic.TinyFFR.World.ScenePrimitive;

namespace Egodystonic.TinyFFR.World;

public readonly record struct ScenePrimitive : IDisposable {
	public const bool DefaultConstantScreenSizeFlag = true;
	internal const float PointToLineSizeRatio = 0.2f;
	internal const float PointToLineSizeRatioReciprocal = 1f / PointToLineSizeRatio;
	public static readonly PrimitivePaintbrush DefaultPaintbrush2d = new(ColorVect.WhiteOpaque, ColorVect.BlackOpaque);
	public static readonly PrimitivePaintbrush DefaultPaintbrush3d = new(ColorVect.WhiteOpaque, null);
	
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
			Magnitude.VerySmall => 0.005f,
			Magnitude.Small => 0.0125f,
			Magnitude.Large => 0.0275f,
			Magnitude.VeryLarge => 0.035f,
			_ => 0.02f
		};
	}
	public static float ConvertLineMagnitudeToSize(Magnitude m) => ConvertPointMagnitudeToSize(m) * PointToLineSizeRatio;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPaintbrush(in PrimitivePaintbrush paintbrush) => _parentScene.SetPrimitivePaintbrush(_primitiveHandle, in paintbrush);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => _parentScene.DisposePrimitive(_primitiveHandle);
	
	public void SetGeometry(Location point, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometry(point, ConvertPointMagnitudeToSize(size), constantScreenSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometry(Location point, float size, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometry(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, point, size, constantScreenSize);
	
	public void SetGeometry(Location position, ReadOnlySpan<char> str, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometry(position, str, ConvertPointMagnitudeToSize(size), constantScreenSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometry(Location position, ReadOnlySpan<char> str, float size, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometry(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, position, str, size, constantScreenSize);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometry(PositionedRotatedCuboid cuboid, bool wireframe = false) => _parentScene.Implementation.SetPrimitiveGeometry(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, cuboid, wireframe);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometry(PositionedSphere sphere, bool wireframe = false) => _parentScene.Implementation.SetPrimitiveGeometry(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, sphere, wireframe);
	
	public void SetGeometry(BoundedRay ray, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometry(ray, ConvertLineMagnitudeToSize(size), constantScreenSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometry(BoundedRay ray, float size, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometry(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, ray, size, constantScreenSize);
	
	public void SetGeometry(Ray ray, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometry(ray, ConvertLineMagnitudeToSize(size), constantScreenSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometry(Ray ray, float size, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometry(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, ray, size, constantScreenSize);
	
	// public void SetGeometry(Line line, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometry(line, ConvertLineMagnitudeToSize(size), constantScreenSize);
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void SetGeometry(Line line, float size, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometry(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, line, size, constantScreenSize);
}

partial struct Scene {
	public ScenePrimitive AddPrimitive(Location point, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitive(point, in DefaultPaintbrush2d, ConvertPointMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitive(Location point, in PrimitivePaintbrush paintbrush, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitive(point, in paintbrush, ConvertPointMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitive(Location point, in PrimitivePaintbrush paintbrush, float size, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometry(point, size, constantScreenSize);
		return result;
	}
	
	public ScenePrimitive AddPrimitive(Location position, ReadOnlySpan<char> str, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitive(position, str, in DefaultPaintbrush2d, ConvertStringMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitive(Location position, ReadOnlySpan<char> str, in PrimitivePaintbrush paintbrush, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitive(position, str, in paintbrush, ConvertStringMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitive(Location position, ReadOnlySpan<char> str, in PrimitivePaintbrush paintbrush, float size, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometry(position, str, size, constantScreenSize);
		return result;
	}
	
	public ScenePrimitive AddPrimitive(PositionedRotatedCuboid cuboid, bool wireframe = false) => AddPrimitive(cuboid, in DefaultPaintbrush3d, wireframe);
	public ScenePrimitive AddPrimitive(PositionedRotatedCuboid cuboid, in PrimitivePaintbrush paintbrush, bool wireframe = false) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometry(cuboid, wireframe);
		return result;
	}
	
	public ScenePrimitive AddPrimitive(PositionedSphere sphere, bool wireframe = false) => AddPrimitive(sphere, in DefaultPaintbrush3d, wireframe);
	public ScenePrimitive AddPrimitive(PositionedSphere sphere, in PrimitivePaintbrush paintbrush, bool wireframe = false) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometry(sphere, wireframe);
		return result;
	}
	
	public ScenePrimitive AddPrimitive(BoundedRay ray, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitive(ray, in DefaultPaintbrush2d, ConvertLineMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitive(BoundedRay ray, in PrimitivePaintbrush paintbrush, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitive(ray, in paintbrush, ConvertLineMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitive(BoundedRay ray, in PrimitivePaintbrush paintbrush, float size, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometry(ray, size, constantScreenSize);
		return result;
	}
	
	public ScenePrimitive AddPrimitive(Ray ray, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitive(ray, in DefaultPaintbrush2d, ConvertLineMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitive(Ray ray, in PrimitivePaintbrush paintbrush, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitive(ray, in paintbrush, ConvertLineMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitive(Ray ray, in PrimitivePaintbrush paintbrush, float size, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometry(ray, size, constantScreenSize);
		return result;
	}
}