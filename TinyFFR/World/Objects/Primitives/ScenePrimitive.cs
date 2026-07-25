// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using static Egodystonic.TinyFFR.World.ScenePrimitive;

namespace Egodystonic.TinyFFR.World;

public readonly record struct ScenePrimitive : IDisposable {
	public const bool DefaultConstantScreenSizeFlag = true;
	public static readonly PrimitivePaintbrush DefaultPaintbrushPoint = new(ColorVect.WhiteOpaque, ColorVect.BlackOpaque);
	public static readonly PrimitivePaintbrush DefaultPaintbrushString = new(ColorVect.WhiteOpaque, ColorVect.BlackOpaque);
	
	readonly Scene _parentScene;
	readonly nuint _primitiveHandle;

	internal ScenePrimitive(Scene parentScene, nuint primitiveHandle) {
		_parentScene = parentScene;
		_primitiveHandle = primitiveHandle;
	}
	
	public static float ConvertMagnitudeToSize(Magnitude m) {
		return m switch {
			Magnitude.VerySmall => 0.005f,
			Magnitude.Small => 0.0125f,
			Magnitude.Large => 0.0275f,
			Magnitude.VeryLarge => 0.035f,
			_ => 0.02f
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPaintbrush(in PrimitivePaintbrush paintbrush) => _parentScene.SetPrimitivePaintbrush(_primitiveHandle, in paintbrush);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => _parentScene.DisposePrimitive(_primitiveHandle);
	
	public void SetGeometry(Location point, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometry(point, ConvertMagnitudeToSize(size), constantScreenSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometry(Location point, float size, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometry(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, point, size, constantScreenSize);
	
	public void SetGeometry(Location position, ReadOnlySpan<char> str, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometry(position, str, ConvertMagnitudeToSize(size), constantScreenSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometry(Location position, ReadOnlySpan<char> str, float size, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometry(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, position, str, size, constantScreenSize);
}

partial struct Scene {
	public ScenePrimitive AddPrimitive(Location point, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitive(point, in DefaultPaintbrushPoint, ConvertMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitive(Location point, in PrimitivePaintbrush paintbrush, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitive(point, in paintbrush, ConvertMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitive(Location point, in PrimitivePaintbrush paintbrush, float size, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometry(point, size, constantScreenSize);
		return result;
	}
	
	public ScenePrimitive AddPrimitive(Location position, ReadOnlySpan<char> str, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitive(position, str, in DefaultPaintbrushPoint, ConvertMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitive(Location position, ReadOnlySpan<char> str, in PrimitivePaintbrush paintbrush, Magnitude size = Magnitude.Default, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitive(position, str, in paintbrush, ConvertMagnitudeToSize(size), constantScreenSize);
	public ScenePrimitive AddPrimitive(Location position, ReadOnlySpan<char> str, in PrimitivePaintbrush paintbrush, float size, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometry(position, str, size, constantScreenSize);
		return result;
	}
}