// Created on 2026-08-04 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.World;

public readonly ref struct CanvasSceneCreationConfig : IConfigStruct<CanvasSceneCreationConfig> {
	public const DiagonalOrientation2D DefaultOrigin = DiagonalOrientation2D.DownLeft;

	public ReadOnlySpan<char> Name { get; init; }
	public ColorVect? InitialBackdropColor { get; init; } = null;

	public CanvasSceneCreationConfig() { }

	internal void ThrowIfInvalid() {
		if (!Enum.IsDefined(Origin)) throw new ArgumentOutOfRangeException(nameof(Origin), Origin, $"Must be a defined {nameof(DiagonalOrientation2D)} value.");
	}

	internal SceneCreationConfig ToSceneCreationConfig() => new() {
		Name = Name,
		InitialBackdropColor = InitialBackdropColor
	};

	public static int GetHeapStorageFormattedLength(in CanvasSceneCreationConfig src) {
		return	SerializationSizeOfString(src.Name)
			+	SerializationSizeOfNullable<ColorVect>()
			+	SerializationSizeOfInt();
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in CanvasSceneCreationConfig src) {
		SerializationWriteString(ref dest, src.Name);
		SerializationWriteNullable(ref dest, src.InitialBackdropColor);
		SerializationWriteInt(ref dest, (int) src.Origin);
	}
	public static CanvasSceneCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new CanvasSceneCreationConfig {
			Name = SerializationReadString(ref src),
			InitialBackdropColor = SerializationReadNullable<ColorVect>(ref src),
			Origin = (DiagonalOrientation2D) SerializationReadInt(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}
