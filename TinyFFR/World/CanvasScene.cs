// Created on 2026-08-04 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

public readonly struct CanvasScene : IDisposable, IStringSpanNameEnabled, IEquatable<CanvasScene> {
	public const int ZPriorityMax = 100;
	public const int ZPriorityMin = -100;
	public const int ZPriorityDefault = 0;
	internal const int ZPriorityRange = ZPriorityMax - ZPriorityMin;
	
	public Scene UnderlyingScene { get; }
	
	public Camera Camera {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingScene.Implementation.GetCanvasCamera(UnderlyingScene.GetHandleWithoutDisposeCheck());
	}

	internal CanvasScene(Scene underlyingScene) {
		UnderlyingScene = underlyingScene;
	}

	
	
	
	
	
	
	
	
	
	
	
	
	
	

	// public DiagonalOrientation2D Origin {
	// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	// 	get => Implementation.GetCanvasOrigin(Handle);
	// }
	// public XYPair<int> SizePixels {
	// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	// 	get => Implementation.GetCanvasSizePixels(Handle);
	// }
	//
	// #region Coordinates
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public Location GetLocation(Orientation2D anchor, XYPair<int> anchorOffset, int layer = 0) => Implementation.GetCanvasLocation(Handle, anchor, anchorOffset, layer);
	//
	// public Location GetLocation(Orientation2D anchor, XYPair<float> fractionalOffset, int layer = 0) {
	// 	return Implementation.GetCanvasLocation(Handle, anchor, SizePixels.ScaledByReal(fractionalOffset), layer);
	// }
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public Location GetLocation(XYPair<int> pixelCoord, int layer = 0) => Implementation.GetCanvasLocation(Handle, pixelCoord, layer);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public XYPair<int> GetPixelCoord(Location worldLocation) => Implementation.GetCanvasPixelCoord(Handle, worldLocation);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public XYPair<int> GetPixelCoordFromCursor(XYPair<int> windowRelativeCursorPosition) => Implementation.GetCanvasPixelCoordFromCursor(Handle, windowRelativeCursorPosition);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public bool Contains(XYPair<int> pixelCoord) => Implementation.CanvasContains(Handle, pixelCoord);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public XYPair<float> GetSize(XYPair<int> pixelSize) => pixelSize.Cast<float>();
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public XYPair<float> GetSize(XYPair<float> fractionalSize) => Implementation.GetCanvasSize(Handle, fractionalSize);
	// #endregion
	//
	// #region Docking
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void Add(QuadInstance quad, in CanvasDock dock) => Implementation.AddCanvasItem(Handle, quad, in dock);
	// public void Add(QuadInstance quad, Orientation2D anchor, XYPair<int> anchorOffset, XYPair<int> pixelSize, int layer = 0) {
	// 	Add(quad, new CanvasDock(anchor, anchorOffset, pixelSize, layer));
	// }
	// public void Add(QuadInstance quad, Orientation2D anchor, XYPair<float> fractionalOffset, XYPair<float> fractionalSize, int layer = 0) {
	// 	Add(quad, new CanvasDock(anchor, fractionalOffset, fractionalSize, layer));
	// }
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void Add(TextInstance text, in CanvasDock dock) => Implementation.AddCanvasItem(Handle, text, in dock);
	// public void Add(TextInstance text, Orientation2D anchor, XYPair<int> anchorOffset, int pixelHeight, int layer = 0) {
	// 	Add(text, new CanvasDock(anchor, anchorOffset, new XYPair<int>(0, pixelHeight), layer));
	// }
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void SetDock(QuadInstance quad, in CanvasDock dock) => Implementation.SetCanvasItemDock(Handle, quad.UnderlyingModelInstance, in dock);
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void SetDock(TextInstance text, in CanvasDock dock) => Implementation.SetCanvasItemDock(Handle, text.UnderlyingModelInstance, in dock);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public CanvasDock GetDock(QuadInstance quad) => Implementation.GetCanvasItemDock(Handle, quad.UnderlyingModelInstance);
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public CanvasDock GetDock(TextInstance text) => Implementation.GetCanvasItemDock(Handle, text.UnderlyingModelInstance);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public bool IsDocked(QuadInstance quad) => Implementation.ContainsCanvasItem(Handle, quad.UnderlyingModelInstance);
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public bool IsDocked(TextInstance text) => Implementation.ContainsCanvasItem(Handle, text.UnderlyingModelInstance);
	// #endregion
	//
	// #region Scene Passthrough
	// public IndirectEnumerable<Scene, ModelInstance> ContainedModelInstances {
	// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	// 	get => UnderlyingScene.ContainedModelInstances;
	// }
	// public IndirectEnumerable<Scene, Light> ContainedLights {
	// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	// 	get => UnderlyingScene.ContainedLights;
	// }
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void Add(ModelInstance modelInstance) => UnderlyingScene.Add(modelInstance);
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void Remove(ModelInstance modelInstance) => UnderlyingScene.Remove(modelInstance);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void Add(ModelInstanceGroup modelInstanceGroup) => UnderlyingScene.Add(modelInstanceGroup);
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void Remove(ModelInstanceGroup modelInstanceGroup) => UnderlyingScene.Remove(modelInstanceGroup);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void Add<TLight>(TLight light) where TLight : ILight<TLight> => UnderlyingScene.Add(light);
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void Remove<TLight>(TLight light) where TLight : ILight<TLight> => UnderlyingScene.Remove(light);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void Add(QuadInstance quad) => UnderlyingScene.Add(quad);
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void Remove(QuadInstance quad) => UnderlyingScene.Remove(quad);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void Add(MutableGridInstance grid) => UnderlyingScene.Add(grid);
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void Remove(MutableGridInstance grid) => UnderlyingScene.Remove(grid);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void Add(TextInstance text) => UnderlyingScene.Add(text);
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void Remove(TextInstance text) => UnderlyingScene.Remove(text);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void RemoveAll(bool includeModelInstances = true, bool includeLights = true, bool includePrimitives = true) => UnderlyingScene.RemoveAll(includeModelInstances, includeLights, includePrimitives);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void SetBackdrop(BuiltInSceneBackdrop backdrop, float backdropIntensity = 1f, Rotation? rotation = null) => UnderlyingScene.SetBackdrop(backdrop, backdropIntensity, rotation);
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void SetBackdrop(BackdropTexture backdrop, float backdropIntensity = 1f, Rotation? rotation = null) => UnderlyingScene.SetBackdrop(backdrop, backdropIntensity, rotation);
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void SetBackdrop(ColorVect color, float indirectLightingIntensity = 1f) => UnderlyingScene.SetBackdrop(color, indirectLightingIntensity);
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void RemoveBackdrop() => UnderlyingScene.RemoveBackdrop();
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public ScenePrimitive AddPrimitive() => UnderlyingScene.AddPrimitive();
	// #endregion
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public string GetNameAsNewStringObject() => UnderlyingScene.GetNameAsNewStringObject();
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public int GetNameLength() => UnderlyingScene.GetNameLength();
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void CopyName(Span<char> destinationBuffer) => UnderlyingScene.CopyName(destinationBuffer);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void Dispose() => UnderlyingScene.Dispose();
	//
	// public static implicit operator Scene(CanvasScene operand) => operand.UnderlyingScene;
	//
	// public override string ToString() => $"Canvas {UnderlyingScene}";
	//
	// #region Equality
	// public bool Equals(CanvasScene other) => UnderlyingScene.Equals(other.UnderlyingScene);
	// public override bool Equals(object? obj) => obj is CanvasScene other && Equals(other);
	// public override int GetHashCode() => UnderlyingScene.GetHashCode();
	// public static bool operator ==(CanvasScene left, CanvasScene right) => left.Equals(right);
	// public static bool operator !=(CanvasScene left, CanvasScene right) => !left.Equals(right);
	// #endregion
}
