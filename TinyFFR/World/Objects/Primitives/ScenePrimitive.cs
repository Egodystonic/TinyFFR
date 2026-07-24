// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.World;

public readonly record struct ScenePrimitive : IDisposable {
	readonly Scene _parentScene;
	readonly nuint _primitiveHandle;

	internal ScenePrimitive(Scene parentScene, nuint primitiveHandle) {
		_parentScene = parentScene;
		_primitiveHandle = primitiveHandle;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPaintbrush(in PrimitivePaintbrush paintbrush) => _parentScene.SetPrimitivePaintbrush(_primitiveHandle, in paintbrush);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => _parentScene.DisposePrimitive(_primitiveHandle);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometry(Location point) => _parentScene.SetPrimitiveGeometry(_primitiveHandle, point);
}