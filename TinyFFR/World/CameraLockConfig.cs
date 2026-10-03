// Created on 2026-10-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Describes how a camera-locked object (such as a <see cref="Assets.Meshes.CameraLockedQuadInstance"/> or <see cref="Assets.Text.CameraLockedTextInstance"/>) turns and sizes itself to follow the camera.
/// </summary>
/// <param name="LockedUprightDirection">Which way is "up" across the object as it turns to follow the camera (or unconstrained if <see cref="Direction.None"/>).</param>
/// <param name="PositionAnchor">Which point of the object is placed at its position (or the centre if <see cref="Orientation2D.None"/>).</param>
/// <param name="ScalingMode">How the object's size responds to its distance from the camera.</param>
/// <param name="LockStyle">Which axes the object is free to turn about as it follows the camera.</param>
public readonly record struct CameraLockConfig(Direction LockedUprightDirection, Orientation2D PositionAnchor, CameraLockedScalingMode ScalingMode, CameraLockStyle LockStyle) {
	internal static CameraLockConfig Default { get; } = new(Direction.None, Orientation2D.None, CameraLockedScalingMode.Standard, CameraLockStyle.FaceCameraPosition);
}
