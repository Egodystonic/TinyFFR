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
	
	ISceneImplProvider Implementation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingScene.Implementation;
	}
	ResourceHandle<Scene> SceneHandle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingScene.GetHandleWithoutDisposeCheck();
	}
	
	public Camera Camera {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingScene.Implementation.GetCanvasCamera(UnderlyingScene.GetHandleWithoutDisposeCheck());
	}

	internal CanvasScene(Scene underlyingScene) {
		UnderlyingScene = underlyingScene;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CanvasScene FromPreviouslyAllocatedUnderlyingScene(Scene underlyingScene) => new(underlyingScene);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CanvasTexture Add(Texture t) => Implementation.AddCanvasObject(SceneHandle, t);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CanvasTexture Add(Material m) => Implementation.AddCanvasObject(SceneHandle, m);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CanvasText Add(FontString s, FontPen pen) => Implementation.AddCanvasObject(SceneHandle, s, pen);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CanvasText Add(ReadOnlySpan<char> str, FontPen pen, TextJustification multiLineJustification = TextJustification.Center) => Implementation.AddCanvasObject(SceneHandle, str, pen, multiLineJustification);
	
	public XYPair<int> SizePixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasSizePixels(SceneHandle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<int> ConvertRenderTargetCoordToLocal(XYPair<int> renderTargetCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft, bool disableDpiScalingAdjustment = false) => Implementation.GetCanvasPrecisePixelCoord(SceneHandle, renderTargetCoord, coordOrigin, disableDpiScalingAdjustment);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<int> ConvertFractionToPixels(XYPair<float> fraction) => Implementation.ConvertCanvasFractionToPixels(SceneHandle, fraction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<float> ConvertPixelsToFraction(XYPair<int> pixels) => Implementation.ConvertCanvasPixelsToFraction(SceneHandle, pixels);

	public void SetBackgroundColor(ColorVect? color) {
		if (color is { } c) UnderlyingScene.SetBackdropWithoutIndirectLighting(c);
		else UnderlyingScene.RemoveBackdrop();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingScene.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingScene.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingScene.CopyName(destinationBuffer);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingScene.Dispose();
	
	public override string ToString() => $"Canvas {UnderlyingScene}";
	
	#region Equality
	public bool Equals(CanvasScene other) => UnderlyingScene.Equals(other.UnderlyingScene);
	public override bool Equals(object? obj) => obj is CanvasScene other && Equals(other);
	public override int GetHashCode() => UnderlyingScene.GetHashCode();
	public static bool operator ==(CanvasScene left, CanvasScene right) => left.Equals(right);
	public static bool operator !=(CanvasScene left, CanvasScene right) => !left.Equals(right);
	#endregion
}
