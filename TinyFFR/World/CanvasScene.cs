// Created on 2026-08-04 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// A scene used for flat, two-dimensional content drawn over the screen/render: text, images and other user-interface elements. Created via the factory's <see cref="ISceneBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// Canvas coordinates can be given either in pixels or as fractions of the canvas size; <see cref="ConvertFractionToPixels"/> and
/// <see cref="ConvertPixelsToFraction"/> move between the two. In some cases you should use pixels (to avoid fractional rescaling
/// of UI textures) and in some cases you should use fractions (to auto-scale the canvas to the size of the viewport). When to use
/// either is down to preference and should be tested.
/// </para>
/// </remarks>
public readonly struct CanvasScene : IResourceSpecialization<CanvasScene, Scene>, IStringSpanNameEnabled, IEquatable<CanvasScene> {
	/// <summary>
	/// The highest permitted layer for a canvas object: <c>100</c>. Objects on higher layers are drawn in front of those on lower ones.
	/// </summary>
	public const int LayerMax = 100;
	/// <summary>
	/// The lowest permitted layer for a canvas object: <c>-100</c>.
	/// </summary>
	public const int LayerMin = -100;
	/// <summary>
	/// The layer a canvas object sits on unless told otherwise: <c>0</c>.
	/// </summary>
	public const int LayerDefault = 0;
	internal const int LayerRange = LayerMax - LayerMin;
	
	/// <summary>
	/// The scene this canvas is built on.
	/// </summary>
	public Scene UnderlyingScene { get; }
	
	ISceneImplProvider Implementation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingScene.Implementation;
	}
	ResourceHandle<Scene> SceneHandle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingScene.GetHandleWithoutDisposeCheck();
	}
	
	/// <summary>
	/// The camera this canvas is viewed through.
	/// </summary>
	/// <remarks>
	/// Managed by the canvas itself so that its contents map predictably on to the rendered image; you rarely need to touch it.
	/// </remarks>
	public Camera Camera {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingScene.Implementation.GetCanvasCamera(UnderlyingScene.GetHandleWithoutDisposeCheck());
	}

	/// <summary>
	/// Returns the <see cref="CanvasSceneQueryProvider"/> for this canvas (that is an object that helps find which canvas objects lie under a given pixel, e.g. for hit-testing the mouse cursor).
	/// </summary>
	public CanvasSceneQueryProvider QueryProvider {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(this);
	}

	internal CanvasScene(Scene underlyingScene) {
		UnderlyingScene = underlyingScene;
	}
	
	#region Specialization
	static IntPtr IResourceSpecialization<CanvasScene, Scene>.SpecializationTypeIdentifier => typeof(CanvasScene).TypeHandle.Value;
	int IResourceSpecialization<CanvasScene, Scene>.SpecializationDataLength => 0;
	static void IResourceSpecialization<CanvasScene, Scene>.Smuggle(CanvasScene resource, Span<byte> specializationDataBuffer, out Scene outBaseResource, out ResourceStub? additionalResourceRef) {
		additionalResourceRef = null;
		outBaseResource = resource.UnderlyingScene;
	}
	static CanvasScene IResourceSpecialization<CanvasScene, Scene>.DeSmuggle(Scene baseResource, ReadOnlySpan<byte> specializationDataBuffer, ResourceStub? additionalResourceRef) {
		return new(baseResource);	
	}
	#endregion
	
	/// <summary>
	/// Reinterprets an existing scene as a canvas scene.
	/// </summary>
	/// <remarks>
	/// Intended for recovering a <see cref="CanvasScene"/> from a <see cref="Scene"/> that was already created as one; it does not convert an ordinary scene in to a
	/// canvas.
	/// </remarks>
	/// <param name="underlyingScene">The scene to reinterpret.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CanvasScene FromPreviouslyAllocatedUnderlyingScene(Scene underlyingScene) => new(underlyingScene);

	/// <summary>
	/// Adds an image to this canvas.
	/// </summary>
	/// <param name="t">The texture to draw.</param>
	/// <param name="name">Optional name for the new object. If omitted, the object takes the name of <paramref name="t"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CanvasTexture Add(Texture t, ReadOnlySpan<char> name = default) => Implementation.AddCanvasObject(SceneHandle, t, name);
	/// <summary>
	/// Adds an image to this canvas, drawn with the given material.
	/// </summary>
	/// <remarks>
	/// Use this rather than the <see cref="Texture"/> overload where the element needs a material's own behaviour, such as transparency or a custom shader.
	/// </remarks>
	/// <param name="m">The material to draw with.</param>
	/// <param name="name">Optional name for the new object. If omitted, the object takes the name of <paramref name="m"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CanvasTexture Add(Material m, ReadOnlySpan<char> name = default) => Implementation.AddCanvasObject(SceneHandle, m, name);
	/// <summary>
	/// Adds a pre-built piece of text to this canvas.
	/// </summary>
	/// <param name="s">The text to draw. Should be created from the same <see cref="Font"/> as the given <paramref name="pen"/>.</param>
	/// <param name="pen">The pen to draw with. Should be created from the same <see cref="Font"/> as the given <paramref name="s"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CanvasText Add(FontString s, FontPen pen) => Implementation.AddCanvasObject(SceneHandle, s, pen);
	/// <summary>
	/// Adds text to this canvas.
	/// </summary>
	/// <param name="str">The text to draw.</param>
	/// <param name="pen">The pen to draw with (the pen's <see cref="Font"/> will be used to render the requested text).</param>
	/// <param name="multiLineJustification">How lines are aligned with one another when the text spans more than one line. Defaults to <see cref="TextJustification.Center"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CanvasText Add(ReadOnlySpan<char> str, FontPen pen, TextJustification multiLineJustification = TextJustification.Center) => Implementation.AddCanvasObject(SceneHandle, str, pen, multiLineJustification);
	
	/// <summary>
	/// How large this canvas is, in pixels.
	/// </summary>
	/// <remarks>
	/// This tracks the size of whatever the canvas is being rendered in to, so it changes when the window is resized.
	/// </remarks>
	public XYPair<int> SizePixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasSizePixels(SceneHandle);
	}

	/// <summary>
	/// Converts a coordinate in the render target (such as a mouse cursor position within a window) in to a coordinate on this canvas.
	/// This is what turns "where the user clicked" in to "where that is on the canvas", which is the first step of hit-testing a user-interface element.
	/// </summary>
	/// <remarks>
	/// This method accounts for render sub-area viewports (e.g. splitscreen/PiP) and DPI scaling.
	/// </remarks>
	/// <param name="renderTargetCoord">The coordinate to convert.</param>
	/// <param name="coordOrigin">Which corner of the render target <paramref name="renderTargetCoord"/> is measured from (or the centre if <see cref="DiagonalOrientation2D.None"/>). Defaults to <see cref="DiagonalOrientation2D.UpLeft"/>, which matches the convention used for window and cursor coordinates.</param>
	/// <param name="disableDpiScalingAdjustment">Pass <see langword="true"/> to skip the adjustment made for displays with scaling enabled, if the supplied coordinate is already in real pixels. Defaults to <see langword="false"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<int> ConvertRenderTargetCoordToLocal(XYPair<int> renderTargetCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft, bool disableDpiScalingAdjustment = false) => Implementation.GetCanvasPrecisePixelCoord(SceneHandle, renderTargetCoord, coordOrigin, disableDpiScalingAdjustment);

	/// <summary>
	/// Converts a coordinate expressed as a fraction of this canvas's size in to one expressed in pixels.
	/// </summary>
	/// <param name="fraction">The coordinate to convert, where <c>1f</c> on an axis is the full extent of the canvas along that axis.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<int> ConvertFractionToPixels(XYPair<float> fraction) => Implementation.ConvertCanvasFractionToPixels(SceneHandle, fraction);
	/// <summary>
	/// Converts a coordinate expressed in pixels in to one expressed as a fraction of this canvas's size.
	/// </summary>
	/// <param name="pixels">The coordinate to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<float> ConvertPixelsToFraction(XYPair<int> pixels) => Implementation.ConvertCanvasPixelsToFraction(SceneHandle, pixels);

	/// <summary>
	/// Sets the colour filling this canvas behind its contents, or clears it so that whatever is behind the canvas shows through.
	/// </summary>
	/// <param name="color">The colour to fill the canvas with, or <see langword="null"/> for a transparent background.</param>
	public void SetBackgroundColor(ColorVect? color) {
		if (color is { } c) UnderlyingScene.SetBackdropWithoutIndirectLighting(c);
		else UnderlyingScene.RemoveBackdrop();
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingScene.GetNameAsNewStringObject();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingScene.GetNameLength();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingScene.CopyName(destinationBuffer);
	
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingScene.Dispose();
	
	/// <inheritdoc />
	public override string ToString() => $"Canvas {UnderlyingScene}";
	
	#region Equality
	/// <inheritdoc />
	public bool Equals(CanvasScene other) => UnderlyingScene.Equals(other.UnderlyingScene);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is CanvasScene other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => UnderlyingScene.GetHashCode();
	/// <summary>
	/// <see cref="Equals(CanvasScene)"/>
	/// </summary>
	public static bool operator ==(CanvasScene left, CanvasScene right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(CanvasScene)"/>
	/// </summary>
	public static bool operator !=(CanvasScene left, CanvasScene right) => !left.Equals(right);
	#endregion
}
