// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.World;

public readonly ref struct CameraCreationConfig {
	public static readonly Location DefaultPosition = Location.Origin;
	public static readonly Direction DefaultViewDirection = Direction.Forward;
	public static readonly Direction DefaultUpDirection = Direction.Up;
	public static readonly Angle DefaultFieldOfView = 60f;
	public static readonly bool DefaultFieldOfViewVerticalFlag = true;
	public static readonly float DefaultNearPlaneDistance = 0.3f;
	public static readonly float DefaultFarPlaneDistance = 30_000f;

	public Location Position { get; init; } = DefaultPosition;
	public Direction ViewDirection { get; init; } = DefaultViewDirection;
	public Direction UpDirection { get; init; } = DefaultUpDirection;
	public Angle FieldOfView { get; init; } = DefaultFieldOfView;
	public bool FieldOfViewIsVertical { get; init; } = DefaultFieldOfViewVerticalFlag;
	public float NearPlaneDistance { get; init; } = DefaultNearPlaneDistance;
	public float FarPlaneDistance { get; init; } = DefaultFarPlaneDistance;

	public ReadOnlySpan<char> Name { get; init; }

	public CameraCreationConfig() { }

	internal void ThrowIfInvalid() {
		static void ThrowArgException(object erroneousArg, string message, [CallerArgumentExpression(nameof(erroneousArg))] string? argName = null) {
			throw new ArgumentException($"{nameof(CameraCreationConfig)}.{argName} {message} Value was {erroneousArg}.", argName);
		}

		if (ViewDirection == Direction.None) {
			ThrowArgException(ViewDirection, $"must not be {Direction.None}.");
		}

		if (UpDirection == Direction.None) {
			ThrowArgException(UpDirection, $"must not be {Direction.None}.");
		}

		if (FieldOfView < Camera.FieldOfViewMin || FieldOfView > Camera.FieldOfViewMax) {
			ThrowArgException(FieldOfView, $"must be between {nameof(Camera)}.{nameof(Camera.FieldOfViewMin)} ({Camera.FieldOfViewMin}) and {nameof(Camera)}.{nameof(Camera.FieldOfViewMax)} ({Camera.FieldOfViewMax}).");
		}

		if (!Single.IsNormal(NearPlaneDistance) || NearPlaneDistance < Camera.NearPlaneDistanceMin) {
			ThrowArgException(NearPlaneDistance, $"must be a normal floating-point value greater than or equal to {nameof(Camera)}.{nameof(Camera.NearPlaneDistanceMin)} ({Camera.NearPlaneDistanceMin}).");
		}

		if (!Single.IsNormal(FarPlaneDistance) || FarPlaneDistance <= NearPlaneDistance || FarPlaneDistance / NearPlaneDistance > Camera.NearFarPlaneDistanceRatioMax) {
			ThrowArgException(FarPlaneDistance, $"must be a normal floating-point value, larger than {nameof(NearPlaneDistance)}, and no greater than {nameof(Camera)}.{nameof(Camera.NearFarPlaneDistanceRatioMax)} ({Camera.NearFarPlaneDistanceRatioMax}) times the {nameof(NearPlaneDistance)}.");
		}
	}
}