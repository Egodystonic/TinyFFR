// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Configuration for creating a new <see cref="Camera"/>.
/// </summary>
public readonly ref struct CameraCreationConfig : IConfigStruct<CameraCreationConfig> {
	/// <summary>
	/// The default value for <see cref="Position"/>: <see cref="Location.Origin"/>.
	/// </summary>
	public static readonly Location DefaultPosition = Location.Origin;
	/// <summary>
	/// The default value for <see cref="ViewDirection"/>: <see cref="Direction.Forward"/>.
	/// </summary>
	public static readonly Direction DefaultViewDirection = Direction.Forward;
	/// <summary>
	/// The default value for <see cref="UpDirection"/>: <see cref="Direction.Up"/>.
	/// </summary>
	public static readonly Direction DefaultUpDirection = Direction.Up;
	/// <summary>
	/// The default value for <see cref="FieldOfView"/>: <c>60°</c>.
	/// </summary>
	public static readonly Angle DefaultFieldOfView = 60f;
	/// <summary>
	/// The default value for <see cref="OrthographicHeight"/>: <c>1f</c> metre.
	/// </summary>
	public static readonly float DefaultOrthographicHeight = 1f;
	/// <summary>
	/// The default value for <see cref="AspectRatio"/>: <c>16:9</c>.
	/// </summary>
	public static readonly float DefaultAspectRatio = 16f / 9f;
	/// <summary>
	/// The default value for <see cref="FieldOfViewIsVertical"/>: <see langword="true"/>.
	/// </summary>
	public static readonly bool DefaultFieldOfViewVerticalFlag = true;
	/// <summary>
	/// The default value for <see cref="NearPlaneDistance"/>: <c>0.03f</c> metres.
	/// </summary>
	public static readonly float DefaultNearPlaneDistance = 0.03f;
	/// <summary>
	/// The default value for <see cref="FarPlaneDistance"/>: <c>1,000f</c> metres.
	/// </summary>
	public static readonly float DefaultFarPlaneDistance = 1_000f;
	/// <summary>
	/// The default value for <see cref="ProjectionType"/>: <see cref="CameraProjectionType.Perspective"/>.
	/// </summary>
	public static readonly CameraProjectionType DefaultProjectionType = CameraProjectionType.Perspective;

	/// <summary>
	/// Where the new camera should be. Defaults to <see cref="DefaultPosition"/>.
	/// </summary>
	public Location Position { get; init; } = DefaultPosition;
	/// <summary>
	/// Which way the new camera should look. Defaults to <see cref="DefaultViewDirection"/>.
	/// </summary>
	public Direction ViewDirection { get; init; } = DefaultViewDirection;
	/// <summary>
	/// Which way should be "up" for the new camera. Defaults to <see cref="DefaultUpDirection"/>.
	/// </summary>
	public Direction UpDirection { get; init; } = DefaultUpDirection;
	/// <summary>
	/// How wide an angle of the scene the new camera should take in. Whether this is measured vertically or horizontally is decided by <see cref="FieldOfViewIsVertical"/>. Defaults to <see cref="DefaultFieldOfView"/>.
	/// </summary>
	public Angle FieldOfView { get; init; } = DefaultFieldOfView;
	/// <summary>
	/// How tall a slice of the world should fill the image when the new camera is orthographic. Only used when <see cref="ProjectionType"/> is <see cref="CameraProjectionType.Orthographic"/>. Defaults to <see cref="DefaultOrthographicHeight"/>.
	/// </summary>
	public float OrthographicHeight { get; init; } = DefaultOrthographicHeight;
	/// <summary>
	/// The width of the image divided by its height. Usually overwritten by the renderer to match its target, so it rarely needs setting. Defaults to <see cref="DefaultAspectRatio"/>.
	/// </summary>
	public float AspectRatio { get; init; } = DefaultAspectRatio;
	/// <summary>
	/// Whether <see cref="FieldOfView"/> is measured from top to bottom rather than from side to side. Defaults to <see cref="DefaultFieldOfViewVerticalFlag"/>.
	/// </summary>
	public bool FieldOfViewIsVertical { get; init; } = DefaultFieldOfViewVerticalFlag;
	/// <summary>
	/// How close an object may come to the new camera and still be drawn, in metres. Defaults to <see cref="DefaultNearPlaneDistance"/>.
	/// </summary>
	public float NearPlaneDistance { get; init; } = DefaultNearPlaneDistance;
	/// <summary>
	/// How far away an object may be and still be drawn, in metres. Defaults to <see cref="DefaultFarPlaneDistance"/>.
	/// </summary>
	public float FarPlaneDistance { get; init; } = DefaultFarPlaneDistance;
	/// <summary>
	/// How the new camera should flatten the scene in to an image. Defaults to <see cref="DefaultProjectionType"/>.
	/// </summary>
	public CameraProjectionType ProjectionType { get; init; } = DefaultProjectionType;

	/// <summary>
	/// Optional name for the new camera.
	/// </summary>
	public ReadOnlySpan<char> Name { get; init; }

	/// <summary>
	/// Constructs a new <see cref="CameraCreationConfig"/> with default values for every setting.
	/// </summary>
	public CameraCreationConfig() { }

	internal void ThrowIfInvalid() {
		static void ThrowArgException(object erroneousArg, string message, [CallerArgumentExpression(nameof(erroneousArg))] string? argName = null) {
			throw new InvalidOperationException($"{nameof(CameraCreationConfig)}.{argName} {message} Value was {erroneousArg}.");
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
		
		if (!OrthographicHeight.IsNonNegativeAndFinite()) {
			ThrowArgException(OrthographicHeight, $"must be non-negative and finite."); 
		}

		if (!AspectRatio.IsPositiveAndFinite()) {
			ThrowArgException(AspectRatio, $"must be a normal, positive floating-point value.");
		}

		if (!Single.IsNormal(NearPlaneDistance) || NearPlaneDistance < Camera.NearPlaneDistanceMin) {
			ThrowArgException(NearPlaneDistance, $"must be a normal floating-point value greater than or equal to {nameof(Camera)}.{nameof(Camera.NearPlaneDistanceMin)} ({Camera.NearPlaneDistanceMin}).");
		}

		if (!Single.IsNormal(FarPlaneDistance) || FarPlaneDistance <= NearPlaneDistance || FarPlaneDistance / NearPlaneDistance > Camera.NearFarPlaneDistanceRatioMax) {
			ThrowArgException(FarPlaneDistance, $"must be a normal floating-point value, larger than {nameof(NearPlaneDistance)}, and no greater than {nameof(Camera)}.{nameof(Camera.NearFarPlaneDistanceRatioMax)} ({Camera.NearFarPlaneDistanceRatioMax}) times the {nameof(NearPlaneDistance)}.");
		}
		
		if (!Enum.IsDefined(ProjectionType)) {
			ThrowArgException(ProjectionType, $"must be one of: {String.Join(", ", Enum.GetValues<CameraProjectionType>())}");
		}
	}

	/// <inheritdoc/>
	public static int GetHeapStorageFormattedLength(in CameraCreationConfig src) {
		return	SerializationSizeOf<Location>() // Position
			+	SerializationSizeOf<Direction>() // ViewDirection
			+	SerializationSizeOf<Direction>() // UpDirection
			+	SerializationSizeOf<Angle>() // FieldOfView
			+	SerializationSizeOfFloat() // OrthographicHeight
			+	SerializationSizeOfFloat() // AspectRatio
			+	SerializationSizeOfBool() // FieldOfViewIsVertical
			+	SerializationSizeOfFloat() // NearPlaneDistance
			+	SerializationSizeOfFloat() // FarPlaneDistance
			+	SerializationSizeOfInt() // ProjectionType
			+	SerializationSizeOfString(src.Name); // Name
	}
	/// <inheritdoc/>
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in CameraCreationConfig src) {
		SerializationWrite(ref dest, src.Position);
		SerializationWrite(ref dest, src.ViewDirection);
		SerializationWrite(ref dest, src.UpDirection);
		SerializationWrite(ref dest, src.FieldOfView);
		SerializationWriteFloat(ref dest, src.OrthographicHeight);
		SerializationWriteFloat(ref dest, src.AspectRatio);
		SerializationWriteBool(ref dest, src.FieldOfViewIsVertical);
		SerializationWriteFloat(ref dest, src.NearPlaneDistance);
		SerializationWriteFloat(ref dest, src.FarPlaneDistance);
		SerializationWriteInt(ref dest, (int) src.ProjectionType);
		SerializationWriteString(ref dest, src.Name);
	}
	/// <inheritdoc/>
	public static CameraCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			Position = SerializationRead<Location>(ref src),
			ViewDirection = SerializationRead<Direction>(ref src),
			UpDirection = SerializationRead<Direction>(ref src),
			FieldOfView = SerializationRead<Angle>(ref src),
			OrthographicHeight = SerializationReadFloat(ref src),
			AspectRatio = SerializationReadFloat(ref src),
			FieldOfViewIsVertical = SerializationReadBool(ref src),
			NearPlaneDistance = SerializationReadFloat(ref src),
			FarPlaneDistance = SerializationReadFloat(ref src),
			ProjectionType = (CameraProjectionType) SerializationReadInt(ref src),
			Name = SerializationReadString(ref src),
		};
	}
	/// <inheritdoc/>
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}