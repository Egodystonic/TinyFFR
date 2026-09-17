// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using static Egodystonic.TinyFFR.World.ScenePrimitive;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// How large a primitive should be drawn.
/// </summary>
/// <remarks>
/// <para>
/// These are deliberately vague sizes rather than measurements, because a primitive's job is to be visible rather than to be to scale.
/// </para>
/// <para>
/// Every overload that sets a primitive's size can also take an exact value; see <see cref="ScenePrimitive.ConvertPointPrimitiveSize"/> and its siblings
/// to convert between the two size metrics.
/// </para>
/// </remarks>
public enum ScenePrimitiveSize {
	/// <summary>
	/// A middling size.
	/// </summary>
	Medium,
	/// <summary>
	/// The smallest size; unobtrusive, but easy to lose sight of.
	/// </summary>
	VerySmall,
	/// <summary>
	/// Smaller than <see cref="Medium"/>.
	/// </summary>
	Small,
	/// <summary>
	/// Larger than <see cref="Medium"/>.
	/// </summary>
	Large,
	/// <summary>
	/// The largest size; hard to miss, but liable to obscure what it is marking.
	/// </summary>
	VeryLarge
}

/// <summary>
/// A simple shape or object drawn in a scene to help you see what your code is doing or add diagnostic scene details: Some examples include a point marker, a line, a box, a grid or a piece of text.
/// </summary>
/// <remarks>
/// Primitives are diagnostic aids or for adding extra context to a scene; and are not considered scene content in themselves.
/// They are deliberately excluded from scene queries, so a line drawn to visualise a ray does not itself
/// register as a hit along that ray, and they can be removed en masse via <see cref="Scene.RemoveAll"/>. Create one with <see cref="Scene.AddPrimitive"/> or one of
/// the <c>AddPrimitive</c> extension methods, then dispose it to remove it.
/// </remarks>
public readonly record struct ScenePrimitive : IDisposable {
	/// <summary>
	/// The default for whether a primitive keeps a constant apparent size on screen: <see langword="true"/>.
	/// </summary>
	public const bool DefaultConstantScreenSizeFlag = true;
	/// <summary>
	/// The default for whether markers are drawn at the ends of a line or ray primitive: <see langword="true"/>.
	/// </summary>
	public const bool DefaultIncludeEndpointsFlag = true;
	/// <summary>
	/// The size a primitive is drawn at unless another is given: <see cref="ScenePrimitiveSize.Small"/>.
	/// </summary>
	public const ScenePrimitiveSize DefaultSize = ScenePrimitiveSize.Small;
	internal const float PointToLineSizeRatio = 0.2f;
	internal const float PointToLineSizeRatioReciprocal = 1f / PointToLineSizeRatio;
	internal const float DefaultGridSize = 2f;
	internal const float DefaultGridMajorLines = 8f;
	internal const float DefaultGridMinorLines = 64f;
	/// <summary>
	/// The colours used for flat primitives (points, lines, rays and text) unless others are given: white with a black secondary.
	/// </summary>
	public static readonly PrimitivePaintbrush DefaultPaintbrush2d = new(ColorVect.WhiteOpaque, ColorVect.BlackOpaque);
	/// <summary>
	/// The colours used for solid primitives (boxes and spheres) unless others are given: plain white.
	/// </summary>
	public static readonly PrimitivePaintbrush DefaultPaintbrush3d = new(ColorVect.WhiteOpaque);
	/// <summary>
	/// The colours used for grid primitives unless others are given: red axes, white major lines and grey minor lines.
	/// </summary>
	public static readonly PrimitivePaintbrush DefaultPaintbrushGrid = new(ColorVect.RedOpaque, ColorVect.WhiteOpaque, new ColorVect(0.35f, 0.35f, 0.35f, 1f));
	
	readonly Scene _parentScene;
	readonly nuint _primitiveHandle;

	internal ScenePrimitive(Scene parentScene, nuint primitiveHandle) {
		_parentScene = parentScene;
		_primitiveHandle = primitiveHandle;
	}
	
	/// <summary>
	/// Returns the concrete size, in metres, that a <see cref="ScenePrimitiveSize"/> corresponds to for a point marker.
	/// </summary>
	/// <param name="m">The size to convert.</param>
	public static float ConvertPointPrimitiveSize(ScenePrimitiveSize m) {
		return m switch {
			ScenePrimitiveSize.VerySmall => 0.005f,
			ScenePrimitiveSize.Small => 0.0125f,
			ScenePrimitiveSize.Large => 0.0275f,
			ScenePrimitiveSize.VeryLarge => 0.035f,
			_ => 0.02f
		};
	}
	/// <summary>
	/// Returns the concrete size, in metres, that a <see cref="ScenePrimitiveSize"/> corresponds to for a piece of text.
	/// </summary>
	/// <param name="m">The size to convert.</param>
	public static float ConvertStringPrimitiveSize(ScenePrimitiveSize m) {
		return m switch {
			ScenePrimitiveSize.VerySmall => 0.0125f,
			ScenePrimitiveSize.Small => 0.02f,
			ScenePrimitiveSize.Large => 0.035f,
			ScenePrimitiveSize.VeryLarge => 0.05f,
			_ => 0.0275f
		};
	}
	/// <summary>
	/// Returns the concrete size, in metres, that a <see cref="ScenePrimitiveSize"/> corresponds to for a line.
	/// </summary>
	/// <param name="m">The size to convert.</param>
	public static float ConvertLinePrimitiveSize(ScenePrimitiveSize m) => ConvertPointPrimitiveSize(m) * PointToLineSizeRatio;

	/// <summary>
	/// Sets the colours this primitive is drawn with.
	/// </summary>
	/// <param name="paintbrush">The colours to use.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPaintbrush(in PrimitivePaintbrush paintbrush) => _parentScene.SetPrimitivePaintbrush(_primitiveHandle, in paintbrush);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => _parentScene.DisposePrimitive(_primitiveHandle);
	
	/// <summary>
	/// Sets this primitive to draw a point marker.
	/// </summary>
	/// <remarks>
	/// Replaces whatever geometry this primitive was drawing before.
	/// </remarks>
	/// <param name="point">Where to draw the point.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public void SetGeometryPoint(Location point, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometryPoint(point, ConvertPointPrimitiveSize(size), constantScreenSize);
	/// <summary>
	/// Sets this primitive to draw a point marker.
	/// </summary>
	/// <remarks>
	/// Replaces whatever geometry this primitive was drawing before.
	/// </remarks>
	/// <param name="point">Where to draw the point.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryPoint(Location point, float size, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometryPoint(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, point, size, constantScreenSize);
	
	/// <summary>
	/// Sets this primitive to draw a piece of text.
	/// </summary>
	/// <remarks>
	/// Replaces whatever geometry this primitive was drawing before.
	/// </remarks>
	/// <param name="position">Where to draw the text.</param>
	/// <param name="str">The text to draw.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public void SetGeometryString(Location position, ReadOnlySpan<char> str, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometryString(position, str, ConvertStringPrimitiveSize(size), constantScreenSize);
	/// <summary>
	/// Sets this primitive to draw a piece of text.
	/// </summary>
	/// <remarks>
	/// Replaces whatever geometry this primitive was drawing before.
	/// </remarks>
	/// <param name="position">Where to draw the text.</param>
	/// <param name="str">The text to draw.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryString(Location position, ReadOnlySpan<char> str, float size, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometryString(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, position, str, size, constantScreenSize);
	
	/// <summary>
	/// Sets this primitive to draw a shape.
	/// </summary>
	/// <remarks>
	/// Replaces whatever geometry this primitive was drawing before.
	/// </remarks>
	/// <param name="cuboid">The box to draw.</param>
	/// <param name="wireframe">If <see langword="true"/>, only the edges of the shape are drawn rather than its surfaces.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(PositionedRotatedCuboid cuboid, bool wireframe = false) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, cuboid, wireframe);
	
	/// <summary>
	/// Sets this primitive to draw a shape.
	/// </summary>
	/// <remarks>
	/// Replaces whatever geometry this primitive was drawing before.
	/// </remarks>
	/// <param name="sphere">The sphere to draw.</param>
	/// <param name="wireframe">If <see langword="true"/>, only the edges of the shape are drawn rather than its surfaces.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(PositionedSphere sphere, bool wireframe = false) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, sphere, wireframe);
	
	/// <summary>
	/// Sets this primitive to draw a shape.
	/// </summary>
	/// <remarks>
	/// Replaces whatever geometry this primitive was drawing before.
	/// </remarks>
	/// <param name="ray">The ray to draw.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="includeEndpoints">If <see langword="true"/>, markers are drawn at both ends of the ray as well as along it.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public void SetGeometryShape(BoundedRay ray, ScenePrimitiveSize size = DefaultSize, bool includeEndpoints = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometryShape(ray, ConvertLinePrimitiveSize(size), includeEndpoints, constantScreenSize);
	/// <summary>
	/// Sets this primitive to draw a shape.
	/// </summary>
	/// <remarks>
	/// Replaces whatever geometry this primitive was drawing before.
	/// </remarks>
	/// <param name="ray">The ray to draw.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="includeEndpoints">If <see langword="true"/>, markers are drawn at both ends of the ray as well as along it.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(BoundedRay ray, float size, bool includeEndpoints = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, ray, size, includeEndpoints, constantScreenSize);
	
	/// <summary>
	/// Sets this primitive to draw a shape.
	/// </summary>
	/// <remarks>
	/// Replaces whatever geometry this primitive was drawing before.
	/// </remarks>
	/// <param name="ray">The ray to draw.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="includeStartPoint">If <see langword="true"/>, a marker is drawn at the start of the ray as well as along it.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public void SetGeometryShape(Ray ray, ScenePrimitiveSize size = DefaultSize, bool includeStartPoint = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometryShape(ray, ConvertLinePrimitiveSize(size), includeStartPoint, constantScreenSize);
	/// <summary>
	/// Sets this primitive to draw a shape.
	/// </summary>
	/// <remarks>
	/// Replaces whatever geometry this primitive was drawing before.
	/// </remarks>
	/// <param name="ray">The ray to draw.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="includeStartPoint">If <see langword="true"/>, a marker is drawn at the start of the ray as well as along it.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(Ray ray, float size, bool includeStartPoint = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, ray, size, includeStartPoint, constantScreenSize);
	
	/// <summary>
	/// Sets this primitive to draw a shape.
	/// </summary>
	/// <remarks>
	/// Replaces whatever geometry this primitive was drawing before.
	/// </remarks>
	/// <param name="line">The line to draw.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public void SetGeometryShape(Line line, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => SetGeometryShape(line, ConvertLinePrimitiveSize(size), constantScreenSize);
	/// <summary>
	/// Sets this primitive to draw a shape.
	/// </summary>
	/// <remarks>
	/// Replaces whatever geometry this primitive was drawing before.
	/// </remarks>
	/// <param name="line">The line to draw.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(Line line, float size, bool constantScreenSize = DefaultConstantScreenSizeFlag) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, line, size, constantScreenSize);
	
	/// <summary>
	/// Sets this primitive to draw a shape.
	/// </summary>
	/// <remarks>
	/// Replaces whatever geometry this primitive was drawing before.
	/// </remarks>
	/// <param name="plane">The plane to draw.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetGeometryShape(Plane plane) => _parentScene.Implementation.SetPrimitiveGeometryShape(_parentScene.GetHandleWithoutDisposeCheck(), _primitiveHandle, plane);
	
	/// <summary>
	/// Sets this primitive to draw a reference grid.
	/// </summary>
	/// <remarks>
	/// Replaces whatever geometry this primitive was drawing before.
	/// </remarks>
	/// <param name="gridCentre">Where the centre of the grid sits.</param>
	/// <param name="gridNormal">Which way the grid faces; the grid is drawn on the plane at right angles to this. Defaults to <see cref="Direction.Up"/>.</param>
	/// <param name="gridX">Which direction the grid’s lines run along. Defaults to a direction perpendicular to the grid normal.</param>
	/// <param name="gridSize">How far the grid extends from its centre, in metres.</param>
	/// <param name="majorGridLineSpacing">How far apart the heavier grid lines are, in metres.</param>
	/// <param name="minorGridLineSpacing">How far apart the lighter grid lines are, in metres.</param>
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

	/// <inheritdoc />
	public override string ToString() => "Scene Primitive";
}

partial struct Scene {
	/// <summary>
	/// Creates a new primitive in this scene drawing a point marker.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="point">Where to draw the point.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public ScenePrimitive AddPrimitivePoint(Location point, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitivePoint(point, in DefaultPaintbrush2d, ConvertPointPrimitiveSize(size), constantScreenSize);
	/// <summary>
	/// Creates a new primitive in this scene drawing a point marker.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="point">Where to draw the point.</param>
	/// <param name="paintbrush">The colours to draw this primitive with.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public ScenePrimitive AddPrimitivePoint(Location point, in PrimitivePaintbrush paintbrush, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitivePoint(point, in paintbrush, ConvertPointPrimitiveSize(size), constantScreenSize);
	/// <summary>
	/// Creates a new primitive in this scene drawing a point marker.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="point">Where to draw the point.</param>
	/// <param name="paintbrush">The colours to draw this primitive with.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public ScenePrimitive AddPrimitivePoint(Location point, in PrimitivePaintbrush paintbrush, float size, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryPoint(point, size, constantScreenSize);
		return result;
	}
	
	/// <summary>
	/// Creates a new primitive in this scene drawing a piece of text.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="position">Where to draw the text.</param>
	/// <param name="str">The text to draw.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public ScenePrimitive AddPrimitiveString(Location position, ReadOnlySpan<char> str, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveString(position, str, in DefaultPaintbrush2d, ConvertStringPrimitiveSize(size), constantScreenSize);
	/// <summary>
	/// Creates a new primitive in this scene drawing a piece of text.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="position">Where to draw the text.</param>
	/// <param name="str">The text to draw.</param>
	/// <param name="paintbrush">The colours to draw this primitive with.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public ScenePrimitive AddPrimitiveString(Location position, ReadOnlySpan<char> str, in PrimitivePaintbrush paintbrush, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveString(position, str, in paintbrush, ConvertStringPrimitiveSize(size), constantScreenSize);
	/// <summary>
	/// Creates a new primitive in this scene drawing a piece of text.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="position">Where to draw the text.</param>
	/// <param name="str">The text to draw.</param>
	/// <param name="paintbrush">The colours to draw this primitive with.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public ScenePrimitive AddPrimitiveString(Location position, ReadOnlySpan<char> str, in PrimitivePaintbrush paintbrush, float size, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryString(position, str, size, constantScreenSize);
		return result;
	}
	
	/// <summary>
	/// Creates a new primitive in this scene drawing a shape.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="cuboid">The box to draw.</param>
	/// <param name="wireframe">If <see langword="true"/>, only the edges of the shape are drawn rather than its surfaces.</param>
	public ScenePrimitive AddPrimitiveShape(PositionedRotatedCuboid cuboid, bool wireframe = false) => AddPrimitiveShape(cuboid, in DefaultPaintbrush3d, wireframe);
	/// <summary>
	/// Creates a new primitive in this scene drawing a shape.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="cuboid">The box to draw.</param>
	/// <param name="paintbrush">The colours to draw this primitive with.</param>
	/// <param name="wireframe">If <see langword="true"/>, only the edges of the shape are drawn rather than its surfaces.</param>
	public ScenePrimitive AddPrimitiveShape(PositionedRotatedCuboid cuboid, in PrimitivePaintbrush paintbrush, bool wireframe = false) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryShape(cuboid, wireframe);
		return result;
	}
	
	/// <summary>
	/// Creates a new primitive in this scene drawing a shape.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="sphere">The sphere to draw.</param>
	/// <param name="wireframe">If <see langword="true"/>, only the edges of the shape are drawn rather than its surfaces.</param>
	public ScenePrimitive AddPrimitiveShape(PositionedSphere sphere, bool wireframe = false) => AddPrimitiveShape(sphere, in DefaultPaintbrush3d, wireframe);
	/// <summary>
	/// Creates a new primitive in this scene drawing a shape.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="sphere">The sphere to draw.</param>
	/// <param name="paintbrush">The colours to draw this primitive with.</param>
	/// <param name="wireframe">If <see langword="true"/>, only the edges of the shape are drawn rather than its surfaces.</param>
	public ScenePrimitive AddPrimitiveShape(PositionedSphere sphere, in PrimitivePaintbrush paintbrush, bool wireframe = false) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryShape(sphere, wireframe);
		return result;
	}
	
	/// <summary>
	/// Creates a new primitive in this scene drawing a shape.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="ray">The ray to draw.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="includeEndpoints">If <see langword="true"/>, markers are drawn at both ends of the ray as well as along it.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public ScenePrimitive AddPrimitiveShape(BoundedRay ray, ScenePrimitiveSize size = DefaultSize, bool includeEndpoints = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(ray, in DefaultPaintbrush2d, ConvertLinePrimitiveSize(size), includeEndpoints, constantScreenSize);
	/// <summary>
	/// Creates a new primitive in this scene drawing a shape.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="ray">The ray to draw.</param>
	/// <param name="paintbrush">The colours to draw this primitive with.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="includeEndpoints">If <see langword="true"/>, markers are drawn at both ends of the ray as well as along it.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public ScenePrimitive AddPrimitiveShape(BoundedRay ray, in PrimitivePaintbrush paintbrush, ScenePrimitiveSize size = DefaultSize, bool includeEndpoints = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(ray, in paintbrush, ConvertLinePrimitiveSize(size), includeEndpoints, constantScreenSize);
	/// <summary>
	/// Creates a new primitive in this scene drawing a shape.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="ray">The ray to draw.</param>
	/// <param name="paintbrush">The colours to draw this primitive with.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="includeEndpoints">If <see langword="true"/>, markers are drawn at both ends of the ray as well as along it.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public ScenePrimitive AddPrimitiveShape(BoundedRay ray, in PrimitivePaintbrush paintbrush, float size, bool includeEndpoints, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryShape(ray, size, includeEndpoints, constantScreenSize);
		return result;
	}
	
	/// <summary>
	/// Creates a new primitive in this scene drawing a shape.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="ray">The ray to draw.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="includeStartPoint">If <see langword="true"/>, a marker is drawn at the start of the ray as well as along it.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public ScenePrimitive AddPrimitiveShape(Ray ray, ScenePrimitiveSize size = DefaultSize, bool includeStartPoint = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(ray, in DefaultPaintbrush2d, ConvertLinePrimitiveSize(size), includeStartPoint, constantScreenSize);
	/// <summary>
	/// Creates a new primitive in this scene drawing a shape.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="ray">The ray to draw.</param>
	/// <param name="paintbrush">The colours to draw this primitive with.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="includeStartPoint">If <see langword="true"/>, a marker is drawn at the start of the ray as well as along it.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public ScenePrimitive AddPrimitiveShape(Ray ray, in PrimitivePaintbrush paintbrush, ScenePrimitiveSize size = DefaultSize, bool includeStartPoint = DefaultIncludeEndpointsFlag, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(ray, in paintbrush, ConvertLinePrimitiveSize(size), includeStartPoint, constantScreenSize);
	/// <summary>
	/// Creates a new primitive in this scene drawing a shape.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="ray">The ray to draw.</param>
	/// <param name="paintbrush">The colours to draw this primitive with.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="includeStartPoint">If <see langword="true"/>, a marker is drawn at the start of the ray as well as along it.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public ScenePrimitive AddPrimitiveShape(Ray ray, in PrimitivePaintbrush paintbrush, float size, bool includeStartPoint, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryShape(ray, size, includeStartPoint, constantScreenSize);
		return result;
	}
	
	/// <summary>
	/// Creates a new primitive in this scene drawing a shape.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="line">The line to draw.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public ScenePrimitive AddPrimitiveShape(Line line, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(line, in DefaultPaintbrush2d, ConvertLinePrimitiveSize(size), constantScreenSize);
	/// <summary>
	/// Creates a new primitive in this scene drawing a shape.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="line">The line to draw.</param>
	/// <param name="paintbrush">The colours to draw this primitive with.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public ScenePrimitive AddPrimitiveShape(Line line, in PrimitivePaintbrush paintbrush, ScenePrimitiveSize size = DefaultSize, bool constantScreenSize = DefaultConstantScreenSizeFlag) => AddPrimitiveShape(line, in paintbrush, ConvertLinePrimitiveSize(size), constantScreenSize);
	/// <summary>
	/// Creates a new primitive in this scene drawing a shape.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="line">The line to draw.</param>
	/// <param name="paintbrush">The colours to draw this primitive with.</param>
	/// <param name="size">How large to draw the primitive.</param>
	/// <param name="constantScreenSize">If <see langword="true"/>, the primitive keeps the same apparent size on screen regardless of how far away it is, which keeps it visible at any distance.</param>
	public ScenePrimitive AddPrimitiveShape(Line line, in PrimitivePaintbrush paintbrush, float size, bool constantScreenSize) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryShape(line, size, constantScreenSize);
		return result;
	}
	
	/// <summary>
	/// Creates a new primitive in this scene drawing a shape.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="plane">The plane to draw.</param>
	public ScenePrimitive AddPrimitiveShape(Plane plane) => AddPrimitiveShape(plane, in DefaultPaintbrush3d);
	/// <summary>
	/// Creates a new primitive in this scene drawing a shape.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="plane">The plane to draw.</param>
	/// <param name="paintbrush">The colours to draw this primitive with.</param>
	public ScenePrimitive AddPrimitiveShape(Plane plane, in PrimitivePaintbrush paintbrush) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryShape(plane);
		return result;
	}
	
	/// <summary>
	/// Creates a new primitive in this scene drawing a reference grid.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="gridCentre">Where the centre of the grid sits.</param>
	/// <param name="gridNormal">Which way the grid faces; the grid is drawn on the plane at right angles to this. Defaults to <see cref="Direction.Up"/>.</param>
	/// <param name="gridX">Which direction the grid’s lines run along. Defaults to a direction perpendicular to the grid normal.</param>
	/// <param name="gridSize">How far the grid extends from its centre, in metres.</param>
	/// <param name="majorGridLineSpacing">How far apart the heavier grid lines are, in metres.</param>
	/// <param name="minorGridLineSpacing">How far apart the lighter grid lines are, in metres.</param>
	public ScenePrimitive AddPrimitiveGrid(Location gridCentre, Direction? gridNormal = null, Direction? gridX = null, float? gridSize = null, float? majorGridLineSpacing = null, float? minorGridLineSpacing = null) => AddPrimitiveGrid(gridCentre, in DefaultPaintbrushGrid, gridNormal, gridX, gridSize, majorGridLineSpacing, minorGridLineSpacing);
	/// <summary>
	/// Creates a new primitive in this scene drawing a reference grid.
	/// </summary>
	/// <remarks>
	/// Primitives are diagnostic aids rather than scene content: they are deliberately excluded from scene queries, so one drawn along a ray will not itself register as a hit. Dispose the returned primitive to remove it.
	/// </remarks>
	/// <param name="gridCentre">Where the centre of the grid sits.</param>
	/// <param name="paintbrush">The colours to draw this primitive with.</param>
	/// <param name="gridNormal">Which way the grid faces; the grid is drawn on the plane at right angles to this. Defaults to <see cref="Direction.Up"/>.</param>
	/// <param name="gridX">Which direction the grid’s lines run along. Defaults to a direction perpendicular to the grid normal.</param>
	/// <param name="gridSize">How far the grid extends from its centre, in metres.</param>
	/// <param name="majorGridLineSpacing">How far apart the heavier grid lines are, in metres.</param>
	/// <param name="minorGridLineSpacing">How far apart the lighter grid lines are, in metres.</param>
	public ScenePrimitive AddPrimitiveGrid(Location gridCentre, in PrimitivePaintbrush paintbrush, Direction? gridNormal = null, Direction? gridX = null, float? gridSize = null, float? majorGridLineSpacing = null, float? minorGridLineSpacing = null) {
		var result = AddPrimitive();
		result.SetPaintbrush(in paintbrush);
		result.SetGeometryGrid(gridCentre, gridNormal, gridX, gridSize, majorGridLineSpacing, minorGridLineSpacing);
		return result;
	}
}