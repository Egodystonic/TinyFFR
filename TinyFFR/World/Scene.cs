// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// One of the backdrops supplied with TinyFFR, usable as a scene's background without having to author or load an image of your own.
/// </summary>
/// <remarks>
/// <para>
/// A scene's backdrop is what fills the parts of the image no object covers, and it also supplies the ambient light that objects pick up from their surroundings.
/// That is why a scene with a backdrop set tends to look markedly more natural than one without.
/// </para>
/// <para>
/// In the interest of keeping TinyFFR's delivered NuGet package size down the supplied backdrops are somewhat lower-resolution and are just provided to help you "get something going".
/// You will probably want to use your own <see cref="BackdropTexture"/> eventually for a production setting.
/// </para>
/// </remarks>
public enum BuiltInSceneBackdrop {
	/// <summary>
	/// No backdrop. Note that this is usually what you should use when
	/// adding a <see cref="Scene"/> to a <see cref="Renderer"/> with <see cref="RenderCompositionType.RetainPreviousScenes"/> in a <see cref="RendererCompositor"/>.
	/// </summary>
	None,
	/// <summary>
	/// A daytime sky with clouds.
	/// </summary>
	Clouds,
	/// <summary>
	/// A night sky filled with stars.
	/// </summary>
	Starfield
}

/// <summary>
/// A collection of everything that can be rendered together: the objects in a world, the lights that illuminate them, the backdrop behind them and any fog between.
/// </summary>
/// <remarks>
/// Objects and lights must be explicitly added to a scene before they appear in it, and a single object or light may belong to several scenes at once.
/// </remarks>
public readonly partial struct Scene : IDisposableResource<Scene, ISceneImplProvider> {
	/// <summary>
	/// The illuminance, in lux, that a backdrop contributes to a scene at an intensity of <c>1f</c>: <c>10,000</c>.
	/// </summary>
	public const float DefaultLux = 10_000f;
	/// <summary>
	/// The largest permitted backdrop intensity: <c>1E15f</c>.
	/// </summary>
	public const float MaxBrightness = 1E15f;

	readonly ResourceHandle<Scene> _handle;
	readonly ISceneImplProvider _impl;

	internal ISceneImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Scene>();
	internal ResourceHandle<Scene> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Scene)) : _handle;

	ISceneImplProvider IResource<Scene, ISceneImplProvider>.Implementation => Implementation;
	ResourceHandle<Scene> IResource<Scene>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	internal Scene(ResourceHandle<Scene> handle, ISceneImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}
	
	/// <summary>
	/// Returns the <see cref="SceneQueryProvider"/> for this scene (that is an object that helps run location queries against objects in the scene)
	/// </summary>
	public SceneQueryProvider QueryProvider {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(this);
	}
	
	/// <summary>
	/// Every model instance currently in this scene.
	/// </summary>
	/// <remarks>
	/// This includes instances added indirectly, such as those backing text, quads and debug primitives, not only those added by <see cref="Add(ModelInstance)"/>.
	/// </remarks>
	public IndirectEnumerable<Scene, ModelInstance> ContainedModelInstances {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetModelInstances(_handle);
	}
	/// <summary>
	/// Every light currently in this scene, of every kind.
	/// </summary>
	public IndirectEnumerable<Scene, Light> ContainedLights {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetLights(_handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal int FindIntersections(BoundedRay ray, Span<ModelInstance> resultsDest, float rayThickness, bool disallowCachedBoundingBoxes) => Implementation.FindIntersections(_handle, ray, resultsDest, rayThickness, disallowCachedBoundingBoxes);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal int FindIntersections(Ray ray, Span<ModelInstance> resultsDest, float rayThickness, bool disallowCachedBoundingBoxes) => Implementation.FindIntersections(_handle, ray, resultsDest, rayThickness, disallowCachedBoundingBoxes);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal int FindIntersections(PositionedRotatedCuboid shape, Span<ModelInstance> resultsDest, bool disallowCachedBoundingBoxes) => Implementation.FindIntersections(_handle, shape, resultsDest, disallowCachedBoundingBoxes);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal int FindIntersections(PositionedCuboid shape, Span<ModelInstance> resultsDest, bool disallowCachedBoundingBoxes) => Implementation.FindIntersections(_handle, shape, resultsDest, disallowCachedBoundingBoxes);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal int FindIntersections(PositionedSphere shape, Span<ModelInstance> resultsDest, bool disallowCachedBoundingBoxes) => Implementation.FindIntersections(_handle, shape, resultsDest, disallowCachedBoundingBoxes);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static Scene IResource<Scene>.CreateFromHandleAndImpl(ResourceHandle<Scene> handle, IResourceImplProvider impl) {
		return new Scene(handle, impl as ISceneImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<Scene> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<Scene> IResource<Scene>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	/// <summary>
	/// Adds a model instance to this scene, so that it is rendered as part of it.
	/// </summary>
	/// <remarks>
	/// Adding something already in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <param name="modelInstance">The model instance to add.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(ModelInstance modelInstance) => Implementation.Add(_handle, modelInstance);
	/// <summary>
	/// Removes a model instance from this scene, so that it is no longer rendered as part of it.
	/// </summary>
	/// <remarks>
	/// Removing something not in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <param name="modelInstance">The model instance to remove.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Remove(ModelInstance modelInstance) => Implementation.Remove(_handle, modelInstance);
	
	/// <summary>
	/// Adds a group of model instances to this scene, so that it is rendered as part of it.
	/// </summary>
	/// <remarks>
	/// Adding something already in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <param name="modelInstanceGroup">The group of model instances to add.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(ModelInstanceGroup modelInstanceGroup) => Implementation.Add(_handle, modelInstanceGroup);
	/// <summary>
	/// Removes a group of model instances from this scene, so that it is no longer rendered as part of it.
	/// </summary>
	/// <remarks>
	/// Removing something not in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <param name="modelInstanceGroup">The group of model instances to remove.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Remove(ModelInstanceGroup modelInstanceGroup) => Implementation.Remove(_handle, modelInstanceGroup);

	/// <summary>
	/// Adds a light to this scene, so that it illuminates the objects in it.
	/// </summary>
	/// <remarks>
	/// Adding a light already in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <typeparam name="TLight">The kind of light being added.</typeparam>
	/// <param name="light">The light to add.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add<TLight>(TLight light) where TLight : ILight<TLight> => Implementation.Add(_handle, light);
	/// <summary>
	/// Removes a light from this scene, so that it no longer illuminates the objects in it.
	/// </summary>
	/// <remarks>
	/// Removing a light not in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <typeparam name="TLight">The kind of light being removed.</typeparam>
	/// <param name="light">The light to remove.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Remove<TLight>(TLight light) where TLight : ILight<TLight> => Implementation.Remove(_handle, light);
	
	/// <summary>
	/// Adds a flat quad to this scene, so that it is rendered as part of it.
	/// </summary>
	/// <remarks>
	/// Adding something already in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <param name="quad">The flat quad to add.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(QuadInstance quad) => Add(quad.UnderlyingModelInstance);
	/// <summary>
	/// Removes a flat quad from this scene, so that it is no longer rendered as part of it.
	/// </summary>
	/// <remarks>
	/// Removing something not in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <param name="quad">The flat quad to remove.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Remove(QuadInstance quad) => Remove(quad.UnderlyingModelInstance);
	
	/// <summary>
	/// Adds a grid to this scene, so that it is rendered as part of it.
	/// </summary>
	/// <remarks>
	/// Adding something already in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <param name="grid">The grid to add.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(MutableGridInstance grid) => Add(grid.UnderlyingModelInstance);
	/// <summary>
	/// Removes a grid from this scene, so that it is no longer rendered as part of it.
	/// </summary>
	/// <remarks>
	/// Removing something not in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <param name="grid">The grid to remove.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Remove(MutableGridInstance grid) => Remove(grid.UnderlyingModelInstance);
	
	/// <summary>
	/// Adds a piece of text to this scene, so that it is rendered as part of it.
	/// </summary>
	/// <remarks>
	/// Adding something already in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <param name="text">The piece of text to add.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(TextInstance text) => Add(text.UnderlyingModelInstance);
	/// <summary>
	/// Removes a piece of text from this scene, so that it is no longer rendered as part of it.
	/// </summary>
	/// <remarks>
	/// Removing something not in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <param name="text">The piece of text to remove.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Remove(TextInstance text) => Remove(text.UnderlyingModelInstance);

	/// <summary>
	/// Adds a camera-facing quad to this scene, so that it is rendered as part of it.
	/// </summary>
	/// <remarks>
	/// Adding something already in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <param name="quad">The camera-facing quad to add.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CameraLockedQuadInstance quad) => Implementation.Add(_handle, quad);
	/// <summary>
	/// Removes a camera-facing quad from this scene, so that it is no longer rendered as part of it.
	/// </summary>
	/// <remarks>
	/// Removing something not in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <param name="quad">The camera-facing quad to remove.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Remove(CameraLockedQuadInstance quad) => Implementation.Remove(_handle, quad);

	/// <summary>
	/// Adds a camera-facing text to this scene, so that it is rendered as part of it.
	/// </summary>
	/// <remarks>
	/// Adding something already in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <param name="text">The camera-facing text to add.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CameraLockedTextInstance text) => Implementation.Add(_handle, text);
	/// <summary>
	/// Removes a camera-facing text from this scene, so that it is no longer rendered as part of it.
	/// </summary>
	/// <remarks>
	/// Removing something not in this scene has no effect (i.e. this method is idempotent).
	/// </remarks>
	/// <param name="text">The camera-facing text to remove.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Remove(CameraLockedTextInstance text) => Implementation.Remove(_handle, text);
	
	/// <summary>
	/// Empties this scene of its contents, optionally keeping some categories of them.
	/// </summary>
	/// <param name="includeModelInstances">Whether to remove the objects in this scene. Defaults to <see langword="true"/>.</param>
	/// <param name="includeLights">Whether to remove the lights in this scene. Defaults to <see langword="true"/>.</param>
	/// <param name="includePrimitives">Whether to remove the debug primitives in this scene. Defaults to <see langword="true"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RemoveAll(bool includeModelInstances = true, bool includeLights = true, bool includePrimitives = true) => Implementation.RemoveAll(_handle, includeModelInstances, includeLights, includePrimitives);

	/// <summary>
	/// Sets this scene's backdrop to one of the built-in options.
	/// </summary>
	/// <remarks>
	/// A backdrop does two things at once: it fills the parts of the image no object covers, and it supplies the ambient light that objects pick up from their
	/// surroundings. That second part is what makes a scene with a backdrop look markedly more natural than one lit only by its own lights.
	/// </remarks>
	/// <param name="backdrop">Which built-in backdrop to use.</param>
	/// <param name="backdropIntensity">How brightly the backdrop is drawn and how strongly it lights the scene, where <c>1f</c> is <see cref="DefaultLux"/>. Capped at <see cref="MaxBrightness"/>. Defaults to <c>1f</c>.</param>
	/// <param name="rotation">How far the backdrop is turned about the scene, which is how you choose where the sun or a landmark sits. Defaults to no rotation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBackdrop(BuiltInSceneBackdrop backdrop, float backdropIntensity = 1f, Rotation? rotation = null) => Implementation.SetBackdrop(_handle, backdrop, backdropIntensity, rotation ?? Rotation.None);
	/// <summary>
	/// Sets this scene's backdrop to a loaded image.
	/// </summary>
	/// <remarks>
	/// A backdrop does two things at once: it fills the parts of the image no object covers, and it supplies the ambient light that objects pick up from their
	/// surroundings. That second part is what makes a scene with a backdrop look markedly more natural than one lit only by its own lights.
	/// </remarks>
	/// <param name="backdrop">The image to use as the backdrop.</param>
	/// <param name="backdropIntensity">How brightly the backdrop is drawn and how strongly it lights the scene, where <c>1f</c> is <see cref="DefaultLux"/>. Capped at <see cref="MaxBrightness"/>. Defaults to <c>1f</c>.</param>
	/// <param name="rotation">How far the backdrop is turned about the scene, which is how you choose where the sun or a landmark sits. Defaults to no rotation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBackdrop(BackdropTexture backdrop, float backdropIntensity = 1f, Rotation? rotation = null) => Implementation.SetBackdrop(_handle, backdrop, backdropIntensity, rotation ?? Rotation.None);
	/// <summary>
	/// Sets this scene's backdrop to a flat colour, which also lights the scene in that colour.
	/// </summary>
	/// <remarks>
	/// The simplest way to get even, neutral lighting without an image: a mid-grey backdrop lights a scene from every direction at once, in the way an overcast sky
	/// does. Use <see cref="SetBackdropWithoutIndirectLighting(ColorVect)"/> for a colour that fills the background but contributes no light.
	/// </remarks>
	/// <param name="color">The colour to fill the background with, and to light the scene in.</param>
	/// <param name="indirectLightingIntensity">How strongly the colour lights the scene, where <c>1f</c> is <see cref="DefaultLux"/>. Capped at <see cref="MaxBrightness"/>. Defaults to <c>1f</c>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBackdrop(ColorVect color, float indirectLightingIntensity = 1f) => Implementation.SetBackdrop(_handle, color, indirectLightingIntensity);
	/// <summary>
	/// Sets this scene's backdrop to a loaded image which is drawn behind the scene but contributes no light to it.
	/// </summary>
	/// <remarks>
	/// Use this where you want to light the scene entirely with your own lights whilst still having something behind it, or where the backdrop image is decorative
	/// rather than a plausible environment.
	/// </remarks>
	/// <param name="backdrop">The image to use as the backdrop.</param>
	/// <param name="backdropIntensity">How brightly the backdrop is drawn, where <c>1f</c> is its natural brightness. Capped at <see cref="MaxBrightness"/>. Defaults to <c>1f</c>.</param>
	/// <param name="rotation">How far the backdrop is turned about the scene. Defaults to no rotation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBackdropWithoutIndirectLighting(BackdropTexture backdrop, float backdropIntensity = 1f, Rotation? rotation = null) => Implementation.SetBackdropWithoutIndirectLighting(_handle, backdrop, backdropIntensity, rotation ?? Rotation.None);
	/// <summary>
	/// Sets this scene's backdrop to a flat colour which is drawn behind the scene but contributes no light to it.
	/// </summary>
	/// <param name="color">The colour to fill the background with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBackdropWithoutIndirectLighting(ColorVect color) => Implementation.SetBackdropWithoutIndirectLighting(_handle, color);
	/// <summary>
	/// Removes this scene's backdrop, leaving the background empty and removing the ambient light the backdrop was contributing.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RemoveBackdrop() => Implementation.RemoveBackdrop(_handle);

	/// <summary>
	/// Adds fog to this scene using one of the density presets.
	/// </summary>
	/// <param name="density">How thick the fog should be.</param>
	public void AddFog(FogDensity density) => AddFog(new FogDescriptor(density));
	/// <summary>
	/// Adds fog to this scene using one of the density presets, in a given colour.
	/// </summary>
	/// <param name="density">How thick the fog should be.</param>
	/// <param name="color">The colour of the fog.</param>
	public void AddFog(FogDensity density, ColorVect color) => AddFog(new FogDescriptor(density, color));
	/// <summary>
	/// Adds fog to this scene, configured in full.
	/// </summary>
	/// <remarks>
	/// A scene has at most one fog; adding fog again replaces whatever was there.
	/// </remarks>
	/// <param name="fogDescriptor">How the fog should look and behave.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AddFog(in FogDescriptor fogDescriptor) => Implementation.AddFog(_handle, in fogDescriptor);
	/// <summary>
	/// Removes this scene's fog, if it has any.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RemoveFog() => Implementation.RemoveFog(_handle);

	/// <summary>
	/// Creates a new primitive in this scene.
	/// </summary>
	/// <remarks>
	/// This overload creates the primitive and adds it to the scene, but it will not be visible until you set some geometry on it.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScenePrimitive AddPrimitive() => Implementation.CreatePrimitive(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetPrimitivePaintbrush(nuint primitiveHandle, in PrimitivePaintbrush paintbrush) => Implementation.SetPrimitivePaintbrush(_handle, primitiveHandle, in paintbrush);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void DisposePrimitive(nuint primitiveHandle) => Implementation.DisposePrimitive(_handle, primitiveHandle);

	/// <summary>
	/// Converts an illuminance in lux to the equivalent backdrop intensity value.
	/// </summary>
	/// <param name="lux">The illuminance to convert. Negative and non-finite values return <c>0f</c>.</param>
	public static float LuxToBrightness(float lux) {
		if (!lux.IsNonNegativeAndFinite()) return 0f;
		return Single.Min(MathF.Sqrt(lux / DefaultLux), MaxBrightness);
	}

	/// <summary>
	/// Converts a backdrop intensity value to the illuminance it represents, in lux.
	/// </summary>
	/// <param name="brightness">The intensity to convert. Negative and non-finite values are treated as <c>0f</c>.</param>
	public static float BrightnessToLux(float brightness) {
		if (!brightness.IsNonNegativeAndFinite()) return 0f;
		brightness = Single.Min(brightness, MaxBrightness);
		return DefaultLux * brightness * brightness;
	}

	/// <inheritdoc />
	public override string ToString() => $"Scene {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Disposal
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	#region Equality
	/// <inheritdoc />
	public bool Equals(Scene other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Scene other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	/// <summary>
	/// <see cref="Equals(Scene)"/>
	/// </summary>
	public static bool operator ==(Scene left, Scene right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(Scene)"/>
	/// </summary>
	public static bool operator !=(Scene left, Scene right) => !left.Equals(right);
	#endregion
}