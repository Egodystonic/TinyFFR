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
	
	public CanvasTexture Add(Texture t) {
		return UnderlyingScene.Implementation.AddCanvasObject(UnderlyingScene.GetHandleWithoutDisposeCheck(), t);
	}
	public CanvasText Add(FontString s) {
		return UnderlyingScene.Implementation.AddCanvasObject(UnderlyingScene.GetHandleWithoutDisposeCheck(), s);
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
