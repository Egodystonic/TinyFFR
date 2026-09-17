// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// A preset for a new camera's near and far plane distances; i.e. how close and how far away an object may be and still be drawn.
/// </summary>
/// <remarks>
/// A camera draws nothing nearer than its near plane or further than its far plane. Widening that range sounds harmless but costs precision: the further apart the
/// two planes are, the less accurately the renderer can tell which of two nearly-coincident surfaces is in front, which shows up as flickering where surfaces meet.
/// These presets pick sensible pairings for common situations; use <see cref="CameraCreationConfig"/> directly to set exact distances.
/// </remarks>
public enum CameraPlaneConfiguration {
	/// <summary>
	/// A general-purpose range, suitable for most scenes.
	/// </summary>
	Standard,
	/// <summary>
	/// Allows objects very close to the camera to be drawn, at the cost of the maximum view distance.
	/// </summary>
	/// <remarks>
	/// Near plane <c>0.01m</c>, far plane <c>333m</c>. Appropriate where the camera gets right up against objects, such as a first-person view or an inspector that
	/// zooms in closely.
	/// </remarks>
	CloseRange,
	/// <summary>
	/// Allows very distant objects to be drawn, at the cost of how close an object may come to the camera.
	/// </summary>
	/// <remarks>
	/// Near plane <c>0.15m</c>, far plane <c>5,000m</c>. Appropriate for outdoor scenes with a distant horizon, where objects never approach the camera closely.
	/// </remarks>
	LongRange
}

/// <summary>
/// Builder interface that allows you to create <see cref="Camera"/>s.
/// </summary>
public interface ICameraBuilder {
	/// <summary>
	/// Creates a new <see cref="Camera"/>.
	/// </summary>
	/// <param name="initialPosition">Where the new camera should be. If <see langword="null"/>, the default is used.</param>
	/// <param name="initialViewDirection">Which way the new camera should look. If <see langword="null"/>, the default is used.</param>
	/// <param name="cameraRange">A preset for how close and how far away an object may be and still be drawn. Defaults to <see cref="CameraPlaneConfiguration.Standard"/>.</param>
	/// <param name="name">Optional name for the new camera.</param>
	Camera CreateCamera(Location? initialPosition = null, Direction? initialViewDirection = null, CameraPlaneConfiguration cameraRange = CameraPlaneConfiguration.Standard, ReadOnlySpan<char> name = default) {
		return CreateCamera(new CameraCreationConfig {
			Position = initialPosition ?? CameraCreationConfig.DefaultPosition,
			ViewDirection = initialViewDirection ?? CameraCreationConfig.DefaultViewDirection,
			NearPlaneDistance = cameraRange switch {
				CameraPlaneConfiguration.CloseRange => 0.01f,
				CameraPlaneConfiguration.LongRange => 0.15f,
				_ => CameraCreationConfig.DefaultNearPlaneDistance,
			},
			FarPlaneDistance = cameraRange switch {
				CameraPlaneConfiguration.CloseRange => 333f,
				CameraPlaneConfiguration.LongRange => 5_000f,
				_ => CameraCreationConfig.DefaultFarPlaneDistance,
			},
			Name = name
		});
	}
	/// <summary>
	/// Creates a new <see cref="Camera"/> according to the given <paramref name="config"/>.
	/// </summary>
	/// <param name="config">Configuration for the new camera, including its placement, field of view and projection type.</param>
	Camera CreateCamera(in CameraCreationConfig config);
}