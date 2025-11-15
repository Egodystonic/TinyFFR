// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.World;

public readonly ref struct CameraCreationConfig : IConfigStruct<CameraCreationConfig> {
	public static readonly Location DefaultPosition = Location.Origin;
	public static readonly Direction DefaultViewDirection = Direction.Forward;
	public static readonly Direction DefaultUpDirection = Direction.Up;
	public static readonly Angle DefaultFieldOfView = 60f;
	public static readonly float DefaultAspectRatio = 16f / 9f;
	public static readonly bool DefaultFieldOfViewVerticalFlag = true;
	public static readonly float DefaultNearPlaneDistance = 0.15f;
	public static readonly float DefaultFarPlaneDistance = 5_000f;

	public Location Position { get; init; } = DefaultPosition;
	public Direction ViewDirection { get; init; } = DefaultViewDirection;
	public Direction UpDirection { get; init; } = DefaultUpDirection;
	public Angle FieldOfView { get; init; } = DefaultFieldOfView;
	public float AspectRatio { get; init; } = DefaultAspectRatio;
	public bool FieldOfViewIsVertical { get; init; } = DefaultFieldOfViewVerticalFlag;
	public float NearPlaneDistance { get; init; } = DefaultNearPlaneDistance;
	public float FarPlaneDistance { get; init; } = DefaultFarPlaneDistance;

	public ReadOnlySpan<char> Name { get; init; }

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

		if (!AspectRatio.IsPositiveAndFinite()) {
			ThrowArgException(AspectRatio, $"must be a normal, positive floating-point value.");
		}

		if (!Single.IsNormal(NearPlaneDistance) || NearPlaneDistance < Camera.NearPlaneDistanceMin) {
			ThrowArgException(NearPlaneDistance, $"must be a normal floating-point value greater than or equal to {nameof(Camera)}.{nameof(Camera.NearPlaneDistanceMin)} ({Camera.NearPlaneDistanceMin}).");
		}

		if (!Single.IsNormal(FarPlaneDistance) || FarPlaneDistance <= NearPlaneDistance || FarPlaneDistance / NearPlaneDistance > Camera.NearFarPlaneDistanceRatioMax) {
			ThrowArgException(FarPlaneDistance, $"must be a normal floating-point value, larger than {nameof(NearPlaneDistance)}, and no greater than {nameof(Camera)}.{nameof(Camera.NearFarPlaneDistanceRatioMax)} ({Camera.NearFarPlaneDistanceRatioMax}) times the {nameof(NearPlaneDistance)}.");
		}
	}

	public static int GetHeapStorageFormattedLength(in CameraCreationConfig src) {
		return	SerializationSizeOf<Location>() // Position
			+	SerializationSizeOf<Direction>() // ViewDirection
			+	SerializationSizeOf<Direction>() // UpDirection
			+	SerializationSizeOf<Angle>() // FieldOfView
			+	SerializationSizeOfFloat() // AspectRatio
			+	SerializationSizeOfBool() // FieldOfViewIsVertical
			+	SerializationSizeOfFloat() // NearPlaneDistance
			+	SerializationSizeOfFloat() // FarPlaneDistance
			+	SerializationSizeOfString(src.Name); // Name
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in CameraCreationConfig src) {
		SerializationWrite(ref dest, src.Position);
		SerializationWrite(ref dest, src.ViewDirection);
		SerializationWrite(ref dest, src.UpDirection);
		SerializationWrite(ref dest, src.FieldOfView);
		SerializationWriteFloat(ref dest, src.AspectRatio);
		SerializationWriteBool(ref dest, src.FieldOfViewIsVertical);
		SerializationWriteFloat(ref dest, src.NearPlaneDistance);
		SerializationWriteFloat(ref dest, src.FarPlaneDistance);
		SerializationWriteString(ref dest, src.Name);
	}
	public static CameraCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			Position = SerializationRead<Location>(ref src),
			ViewDirection = SerializationRead<Direction>(ref src),
			UpDirection = SerializationRead<Direction>(ref src),
			FieldOfView = SerializationRead<Angle>(ref src),
			AspectRatio = SerializationReadFloat(ref src),
			FieldOfViewIsVertical = SerializationReadBool(ref src),
			NearPlaneDistance = SerializationReadFloat(ref src),
			FarPlaneDistance = SerializationReadFloat(ref src),
			Name = SerializationReadString(ref src),
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}