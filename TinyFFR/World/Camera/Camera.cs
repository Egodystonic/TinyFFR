// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Represents a viewpoint on a scene: where it is viewed from, which way it is facing, and how the three-dimensional scene is flattened in to a two-dimensional image. Created via the factory's <see cref="ICameraBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// A camera on its own draws nothing; it is paired with a scene by a renderer, which is what produces an image. The same scene can be rendered from several cameras
/// at once, and a camera can be moved by hand or driven by an <see cref="ICameraController"/>.
/// </para>
/// <para>
/// Besides its placement, a camera carries the properties a real one would: a field of view, a near and far limit on what it can see, and an exposure controlling
/// how bright the resulting image is.
/// </para>
/// </remarks>
public readonly struct Camera : IDisposableResource<Camera, ICameraImplProvider>, IPositionedSceneObject, IOrientedSceneObject {
	/// <summary>
	/// The smallest permitted field of view: <c>0°</c>.
	/// </summary>
	public static readonly Angle FieldOfViewMin = Angle.Zero;
	/// <summary>
	/// The largest permitted field of view: <c>360°</c>.
	/// </summary>
	public static readonly Angle FieldOfViewMax = Angle.FullCircle;
	/// <summary>
	/// The smallest permitted <see cref="NearPlaneDistance"/>: <c>1E-5f</c>.
	/// </summary>
	public static readonly float NearPlaneDistanceMin = 1E-5f;
	/// <summary>
	/// The largest permitted ratio between <see cref="FarPlaneDistance"/> and <see cref="NearPlaneDistance"/>: <c>1E6f</c>.
	/// </summary>
	/// <remarks>
	/// The wider that ratio, the less precisely the renderer can tell which of two nearly-coincident surfaces is in front, so it is capped.
	/// </remarks>
	public static readonly float NearFarPlaneDistanceRatioMax = 1E6f;
	/// <summary>
	/// The default aperture used by <see cref="SetExposure(float, float, float)"/>: <c>f/16</c>.
	/// </summary>
	public static readonly float ApertureDefault = 16f;
	/// <summary>
	/// The smallest permitted aperture value: <c>f/0.5</c>.
	/// </summary>
	public static readonly float ApertureMin = 0.5f;
	/// <summary>
	/// The largest permitted aperture value: <c>f/64</c>.
	/// </summary>
	public static readonly float ApertureMax = 64f;
	/// <summary>
	/// The default shutter speed used by <see cref="SetExposure(float, float, float)"/>: <c>1/125</c> of a second.
	/// </summary>
	public static readonly float ShutterSpeedDefault = 1f / 125f;
	/// <summary>
	/// The shortest permitted shutter speed: <c>1/25,000</c> of a second.
	/// </summary>
	public static readonly float ShutterSpeedMin = 1f / 25_000f;
	/// <summary>
	/// The longest permitted shutter speed: <c>60</c> seconds.
	/// </summary>
	public static readonly float ShutterSpeedMax = 60f;
	/// <summary>
	/// The default sensitivity (ISO) used by <see cref="SetExposure(float, float, float)"/>: <c>100</c>.
	/// </summary>
	public static readonly float SensitivityDefault = 100f;
	/// <summary>
	/// The lowest permitted sensitivity (ISO): <c>10</c>.
	/// </summary>
	public static readonly float SensitivityMin = 10f;
	/// <summary>
	/// The highest permitted sensitivity (ISO): <c>204,800</c>.
	/// </summary>
	public static readonly float SensitivityMax = 204_800f;
	/// <summary>
	/// The default value for <see cref="Exposure"/>: <c>1f</c>.
	/// </summary>
	public static readonly float ExposureDefault = 1f;
	/// <summary>
	/// The largest permitted <see cref="Exposure"/>: <c>10f</c>.
	/// </summary>
	public static readonly float ExposureMax = 10f;
	/// <summary>
	/// The smallest permitted <see cref="Exposure"/>: <c>1/10f</c>.
	/// </summary>
	public static readonly float ExposureMin = 1f / ExposureMax;

	readonly ResourceHandle<Camera> _handle;
	readonly ICameraImplProvider _impl;

	internal ICameraImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Camera>();
	internal ResourceHandle<Camera> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Camera)) : _handle;

	ICameraImplProvider IResource<Camera, ICameraImplProvider>.Implementation => Implementation;
	ResourceHandle<Camera> IResource<Camera>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// Where this camera is in the world/scene.
	/// </summary>
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPosition(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPosition(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="Position"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="position">The new position for this camera.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	/// <summary>
	/// Which way this camera is looking.
	/// </summary>
	/// <remarks>
	/// Setting this also changes <see cref="UpDirection"/> to ensure orthogonality; use <see cref="SetViewAndUpDirection"/> with <c>enforceOrthogonality</c> set to <c>false</c> to override this.
	/// </remarks>
	public Direction ViewDirection {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetViewDirection(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetViewDirection(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="ViewDirection"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="direction">The new view direction for this camera.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetViewDirection(Direction direction) => ViewDirection = direction;

	/// <summary>
	/// Which way is "up" for this camera; i.e. which way in the world ends up pointing towards the top of the rendererd scene.
	/// </summary>
	/// <remarks>
	/// Setting this also changes <see cref="ViewDirection"/> to ensure orthogonality; use <see cref="SetViewAndUpDirection"/> with <c>enforceOrthogonality</c> set to <c>false</c> to override this.
	/// </remarks>
	public Direction UpDirection {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetUpDirection(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetUpDirection(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="UpDirection"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="direction">The new up direction for this camera.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetUpDirection(Direction direction) => UpDirection = direction;

	/// <summary>
	/// How wide an angle of the scene this camera takes in from side to side. Clamped to between <see cref="FieldOfViewMin"/> and <see cref="FieldOfViewMax"/>.
	/// Only meaningful when <see cref="ProjectionType"/> is <see cref="CameraProjectionType.Perspective"/> (the default).
	/// </summary>
	/// <remarks>
	/// <para>
	/// A wider field of view fits more of the scene in to the image but makes everything in it smaller, and exaggerates perspective towards the edges.
	/// </para>
	/// <para>
	/// This and <see cref="VerticalFieldOfView"/> are two aspects of the same setting, related by <see cref="AspectRatio"/>, so setting either changes the other.
	/// <see cref="VerticalFieldOfView"/> is usually the better one to fix, because it keeps the framing consistent as the window is made wider or narrower.
	/// </para>
	/// </remarks>
	public Angle HorizontalFieldOfView {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetHorizontalFieldOfView(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetHorizontalFieldOfView(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="HorizontalFieldOfView"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="fov">The new horizontal field of view.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetHorizontalFieldOfView(Angle fov) => HorizontalFieldOfView = fov;

	/// <summary>
	/// How wide an angle of the scene this camera takes in from top to bottom. Clamped to between <see cref="FieldOfViewMin"/> and <see cref="FieldOfViewMax"/>.
	/// Only meaningful when <see cref="ProjectionType"/> is <see cref="CameraProjectionType.Perspective"/> (the default).
	/// </summary>
	/// <remarks>
	/// <para>
	/// A wider field of view fits more of the scene in to the image but makes everything in it smaller, and exaggerates perspective towards the edges.
	/// </para>
	/// <para>
	/// This and <see cref="HorizontalFieldOfView"/> are two aspects of the same setting, related by <see cref="AspectRatio"/>, so setting either changes the other.
	/// <see cref="VerticalFieldOfView"/> is usually the better one to fix, because it keeps the framing consistent as the window is made wider or narrower.
	/// </para>
	/// </remarks>
	public Angle VerticalFieldOfView {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetVerticalFieldOfView(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetVerticalFieldOfView(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="VerticalFieldOfView"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="fov">The new vertical field of view.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetVerticalFieldOfView(Angle fov) => VerticalFieldOfView = fov;
	
	/// <summary>
	/// How tall a slice of the world fills the image, in metres.
	/// Only meaningful when <see cref="ProjectionType"/> is <see cref="CameraProjectionType.Orthographic"/>.
	/// </summary>
	/// <remarks>
	/// This is the orthographic equivalent of a field of view: because an orthographic camera does not converge,
	/// what it sees is a box of fixed size rather than a cone, and this sets the height of that box.
	/// </remarks>
	public float OrthographicHeight {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetOrthographicHeight(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetOrthographicHeight(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="OrthographicHeight"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="height">The new orthographic height, in metres.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetOrthographicHeight(float height) => OrthographicHeight = height;

	/// <summary>
	/// The width of the captured scene image, divided by its height.
	/// </summary>
	/// <remarks>
	/// In most cases the containing <see cref="Renderer"/> will keep this property in sync with the render target for you
	/// (unless that renderer was created with <see cref="RendererCreationConfig.AutoUpdateCameraAspectRatio"/> set to <c>false</c>);
	/// so you don't need to set it manually. 
	/// </remarks>
	public float AspectRatio {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetAspectRatio(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetAspectRatio(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="AspectRatio"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="ratio">The new aspect ratio (width divided by height).</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetAspectRatio(float ratio) => AspectRatio = ratio;

	/// <summary>
	/// How close an object may come to this camera and still be drawn, in metres. Must be at least <see cref="NearPlaneDistanceMin"/>.
	/// </summary>
	/// <remarks>
	/// Anything closet than this value is clipped away, which is why objects' surfaces disappear/appear to grow holes when a camera gets too close to them.
	/// Keep this as large as your scene allows: a very small near plane combined with a distant far plane costs depth precision and causes flickering where surfaces meet (see <see cref="NearFarPlaneDistanceRatioMax"/>).
	/// </remarks>
	public float NearPlaneDistance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetNearPlaneDistance(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetNearPlaneDistance(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="NearPlaneDistance"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="distance">The new near plane distance, in metres.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetNearPlaneDistance(float distance) => NearPlaneDistance = distance;

	/// <summary>
	/// How far away an object may be and still be drawn, in metres.
	/// </summary>
	/// <remarks>
	/// Anything beyond this is not drawn at all. The ratio between this and <see cref="NearPlaneDistance"/> is capped at <see cref="NearFarPlaneDistanceRatioMax"/>.
	/// </remarks>
	public float FarPlaneDistance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetFarPlaneDistance(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetFarPlaneDistance(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="FarPlaneDistance"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="distance">The new far plane distance, in metres.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetFarPlaneDistance(float distance) => FarPlaneDistance = distance;
	
	/// <summary>
	/// How bright the resulting image is, where <c>1f</c> is neutral. Raising this brightens the whole image, in the way that leaving a real shutter open for longer would.
	/// Clamped to between <see cref="ExposureMin"/> and <see cref="ExposureMax"/>.
	/// </summary>
	/// <remarks>
	/// This is the simple, single normalized way to control exposure; <see cref="SetExposure(float, float, float)"/> offers the same control in real photographic terms instead.
	/// </remarks>
	public float Exposure {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetExposure(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetExposure(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="Exposure"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="exposure">The new exposure value.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetExposure(float exposure) => Exposure = exposure;
	/// <summary>
	/// Sets this camera's exposure in real photographic terms, rather than as a single multiplier.
	/// </summary>
	/// <remarks>
	/// These are the three settings a photographer balances against one another, and they combine here exactly as they would on a real camera: a wider aperture, a
	/// longer shutter speed or a higher sensitivity each brightens the image. Use this where you want image brightness to track a physically-plausible camera setup;
	/// use <see cref="Exposure"/> where you simply want the picture brighter or darker.
	/// </remarks>
	/// <param name="aperture">How wide the lens opening is, as an f-number, where smaller numbers mean a wider opening and a brighter image. Clamped to between <see cref="ApertureMin"/> and <see cref="ApertureMax"/>.</param>
	/// <param name="shutterSpeed">How long the shutter stays open, in seconds; longer means brighter. Clamped to between <see cref="ShutterSpeedMin"/> and <see cref="ShutterSpeedMax"/>.</param>
	/// <param name="sensitivity">How sensitive the sensor is to light, as an ISO value; higher means brighter. Clamped to between <see cref="SensitivityMin"/> and <see cref="SensitivityMax"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetExposure(float aperture, float shutterSpeed, float sensitivity) => Implementation.SetExposure(_handle, aperture, shutterSpeed, sensitivity);
	
	/// <summary>
	/// How far away the plane of sharp focus is, in metres, or <see langword="null"/> for an image that is sharp at every distance.
	/// Setting a non-null value enables the depth-of-field effect. Defaults to <c>null</c>.
	/// </summary>
	/// <remarks>
	/// Setting this makes objects nearer or further than the given distance blur, in the way a real lens does. This is the effect usually called depth of field. It is
	/// what makes a shot feel photographic rather than computer-generated, but it costs rendering time, so leave it <see langword="null"/> where you do not want it.
	/// </remarks>
	public float? FocusDistance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetFocusDistance(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetFocusDistance(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="FocusDistance"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="focusDistance">The new focus distance in metres, or <see langword="null"/> to keep the whole image sharp.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetFocusDistance(float? focusDistance) => FocusDistance = focusDistance;
	
	/// <summary>
	/// How this camera flattens the 3D scene in to an 2D image.
	/// </summary>
	/// <remarks>
	/// This also affects which of the other properties are relevant: A perspective camera is shaped by <see cref="VerticalFieldOfView"/> (or <see cref="HorizontalFieldOfView"/>),
	/// whilst an orthographic one is shaped by <see cref="OrthographicHeight"/>.
	/// </remarks>
	public CameraProjectionType ProjectionType {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetProjectionType(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetProjectionType(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="ProjectionType"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="projectionType">The new projection type.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetProjectionType(CameraProjectionType projectionType) => ProjectionType = projectionType;

	Rotation IOrientedSceneObject.Rotation {
		get => Rotation.FromStartAndEndDirection(Direction.Forward, ViewDirection);
		set => ViewDirection = Direction.Forward * value;
	}

	// A camera's orientation is stored as a view direction rather than a quaternion, so unlike the model instance types
	// this can only convert rather than hand back stored state. The setter and RotateBy stay conversion-free though.
	Quaternion IOrientedSceneObject.RotationQuaternion {
		get => Rotation.FromStartAndEndDirection(Direction.Forward, ViewDirection).ToQuaternion();
		set => ViewDirection = Direction.Forward.RotatedBy(value);
	}

	internal Camera(ResourceHandle<Camera> handle, ICameraImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static Camera IResource<Camera>.CreateFromHandleAndImpl(ResourceHandle<Camera> handle, IResourceImplProvider impl) {
		return new Camera(handle, impl as ICameraImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<Camera> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<Camera> IResource<Camera>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	/// <summary>
	/// Sets this camera's <see cref="ViewDirection"/> and <see cref="UpDirection"/> together.
	/// </summary>
	/// <remarks>
	/// Preferable to setting the two properties one after the other, because it avoids the camera attempting to orthogonalize the values separately..
	/// </remarks>
	/// <param name="newViewDirection">The new value for <see cref="ViewDirection"/>.</param>
	/// <param name="newUpDirection">The new value for <see cref="UpDirection"/>.</param>
	/// <param name="enforceOrthogonality">If <see langword="true"/> (the default), <paramref name="newUpDirection"/> is straightened so that it is exactly at right angles to <paramref name="newViewDirection"/>.
	/// Pass <see langword="false"/> only if you wish to produce unusual effects or don't want the orthogonalization logic to be applied.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetViewAndUpDirection(Direction newViewDirection, Direction newUpDirection, bool enforceOrthogonality = true) {
		Implementation.SetViewAndUpDirection(_handle, newViewDirection, newUpDirection, enforceOrthogonality);
	}

	/// <summary>
	/// Returns this camera's projection matrix.
	/// </summary>
	/// <remarks>
	/// This describes the lens: how the cone (or box) of space the camera can see is flattened on to the image.
	/// Supplied for interoperating with other graphics code; everyday use of a camera does not require it.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Matrix4x4 GetProjectionMatrix() {
		Implementation.GetProjectionMatrix(_handle, out var result);
		return result;
	}
	/// <inheritdoc cref="GetProjectionMatrix()" />
	/// <param name="outProjectionMatrix">When this method returns, contains this camera's projection matrix.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetProjectionMatrix(out Matrix4x4 outProjectionMatrix) => Implementation.GetProjectionMatrix(_handle, out outProjectionMatrix);
	/// <summary>
	/// Overrides this camera's projection matrix directly.
	/// </summary>
	/// <remarks>
	/// Intended for interoperating with other graphics code that computes its own matrices or for creating effects not otherwise achievable via the properties exposed on this type.
	/// Setting this bypasses the properties that would otherwise determine it, so the two can disagree afterwards.
	/// </remarks>
	/// <param name="newProjectionMatrix">The matrix to use.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetProjectionMatrix(in Matrix4x4 newProjectionMatrix) => Implementation.SetProjectionMatrix(_handle, newProjectionMatrix);

	/// <summary>
	/// Returns this camera's model matrix.
	/// </summary>
	/// <remarks>
	/// This describes the camera as an object in the world: where it is and which way it is turned.
	/// Supplied for interoperating with other graphics code; everyday use of a camera does not require it.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Matrix4x4 GetModelMatrix() {
		Implementation.GetModelMatrix(_handle, out var result);
		return result;
	}
	/// <inheritdoc cref="GetModelMatrix()" />
	/// <param name="outModelMatrix">When this method returns, contains this camera's model matrix.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetModelMatrix(out Matrix4x4 outModelMatrix) => Implementation.GetModelMatrix(_handle, out outModelMatrix);
	/// <summary>
	/// Overrides this camera's model matrix directly.
	/// </summary>
	/// <remarks>
	/// Intended for interoperating with other graphics code that computes its own matrices or for creating effects not otherwise achievable via the properties exposed on this type.
	/// Setting this bypasses the properties that would otherwise determine it, so the two can disagree afterwards.
	/// </remarks>
	/// <param name="newModelMatrix">The matrix to use.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetModelMatrix(in Matrix4x4 newModelMatrix) => Implementation.SetModelMatrix(_handle, newModelMatrix);

	/// <summary>
	/// Returns this camera's view matrix.
	/// </summary>
	/// <remarks>
	/// This is the inverse of the model matrix: it transforms the world in to the frame of reference of the camera.
	/// Supplied for interoperating with other graphics code; everyday use of a camera does not require it.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Matrix4x4 GetViewMatrix() {
		Implementation.GetViewMatrix(_handle, out var result);
		return result;
	}
	/// <inheritdoc cref="GetViewMatrix()" />
	/// <param name="outViewMatrix">When this method returns, contains this camera's view matrix.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetViewMatrix(out Matrix4x4 outViewMatrix) => Implementation.GetViewMatrix(_handle, out outViewMatrix);
	/// <summary>
	/// Overrides this camera's view matrix directly.
	/// </summary>
	/// <remarks>
	/// Intended for interoperating with other graphics code that computes its own matrices or for creating effects not otherwise achievable via the properties exposed on this type.
	/// Setting this bypasses the properties that would otherwise determine it, so the two can disagree afterwards.
	/// </remarks>
	/// <param name="newViewMatrix">The matrix to use.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetViewMatrix(in Matrix4x4 newViewMatrix) => Implementation.SetViewMatrix(_handle, newViewMatrix);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveBy(Vect translation) => Implementation.Translate(_handle, translation);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Rotation rotation) => Implementation.Rotate(_handle, rotation);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Quaternion rotationQuaternion) => Implementation.Rotate(_handle, rotationQuaternion);

	/// <summary>
	/// Turns this camera to look at <paramref name="target"/>, allowing for any value of <see cref="UpDirection"/> that satisfies the constraint.
	/// </summary>
	/// <remarks>
	/// In most cases you'll want to constrain <see cref="UpDirection"/> and should use the overload: <see cref="LookAt(Location, Direction)"/>.
	/// </remarks>
	/// <param name="target">The point to look at. If this is the position of the camera itself, the view direction is left unchanged.</param>
	public void LookAt(Location target) => ViewDirection = Position.DirectionTo(target);
	/// <summary>
	/// Turns this camera to look at <paramref name="target"/> while constraining the <paramref name="upDirection"/>.
	/// </summary>
	/// <remarks>
	/// Usually preferable to the single-argument overload, because specifying the up direction fixes the roll of the camera rather than leaving it undefined.
	/// </remarks>
	/// <param name="target">The point to look at. If this is the position of the camera itself, the view direction is left unchanged.</param>
	/// <param name="upDirection">The new value for <see cref="UpDirection"/>; straightened against the resulting view direction.</param>
	public void LookAt(Location target, Direction upDirection) => SetViewAndUpDirection(Position.DirectionTo(target), upDirection);

	/// <summary>
	/// Returns a ray travelling straight forward from the centre of the near plane of this camera.
	/// </summary>
	/// <remarks>
	/// Useful for working out what is directly in front of the camera, for example to find what the crosshair of a first-person view is pointing at.
	/// If you want to translate a user click to a ray, see <see cref="Renderer.CreateRayFromRenderSurface"/>.
	/// </remarks>
	public Ray CreateRayFromNearPlane() => new(Position + ViewDirection * NearPlaneDistance, ViewDirection);
	/// <summary>
	/// Returns a ray travelling out from a given point on the near plane of this camera.
	/// </summary>
	/// <remarks>
	/// This API requires normalized co-ordinates, if you want to convert a pixel click to a <see cref="Ray"/>, see <see cref="Renderer.CreateRayFromRenderSurface"/>.
	/// </remarks>
	/// <param name="normalizedNearPlaneCoord">Where on the near plane the ray should start, as a fraction of its size: <c>(0, 0)</c> is the centre of the image,
	/// and each component runs from <c>-1</c> to <c>1</c> at the edges.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray CreateRayFromNearPlane(XYPair<float> normalizedNearPlaneCoord) => Implementation.CreateRayFromNearPlane(_handle, normalizedNearPlaneCoord);

	/// <summary>
	/// Creates a camera controller of the given type, connected to this camera.
	/// </summary>
	/// <remarks>
	/// Controllers are pooled and rented rather than newly allocated, so dispose the returned controller when you are done with it. The new controller starts with
	/// its default parameters; it does not adopt the current placement of this camera.
	/// </remarks>
	/// <typeparam name="TController">The kind of controller to create (i.e. a class that implements <see cref="ICameraController"/>).</typeparam>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TController CreateController<TController>() where TController : ICameraController<TController> => TController.RentAndTetherToCamera(this); 
	
	/// <summary>
	/// Converts a direction expressed relative to this camera in to a direction in the world.
	/// </summary>
	/// <remarks>
	/// For example, <see cref="Orientation.Forward"/> returns the <see cref="ViewDirection"/> of this camera, and <see cref="Orientation.Right"/> returns whichever
	/// world direction is to its right. This is what lets movement be expressed as "forwards" or "sideways" without the caller having to know where the camera
	/// happens to be pointing.
	/// </remarks>
	/// <param name="o">The camera-relative orientation to convert.</param>
	public Direction GetRelativeOrientationDirection(Orientation o) => CameraUtils.CalculateCameraRelativeOrientationDirection(o, ViewDirection, UpDirection);

	#region Disposal
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	/// <inheritdoc />
	public override string ToString() => $"Camera {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	/// <inheritdoc />
	public bool Equals(Camera other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Camera other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	/// <summary>
	/// <see cref="Equals(Camera)"/>
	/// </summary>
	public static bool operator ==(Camera left, Camera right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(Camera)"/>
	/// </summary>
	public static bool operator !=(Camera left, Camera right) => !left.Equals(right);
	#endregion
}