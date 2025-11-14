// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.World;
using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

// Read Config for just how to read the file in (e.g. any preprocessing and the file path)
// Generation Config for live generation of new ones
// Creation Config for general processing in the local builder when creating the resource

public readonly ref struct TextureReadConfig : IConfigStruct<TextureReadConfig> {
	public required ReadOnlySpan<char> FilePath { get; init; }
	public bool IncludeWAlphaChannel { get; init; } = false;

	public TextureReadConfig() { }

	internal void ThrowIfInvalid() {
		if (FilePath.IsEmpty) {
			throw new ArgumentException($"{nameof(TextureReadConfig)}.{nameof(FilePath)} can not be empty.", nameof(FilePath));
		}
	}

	public static int GetHeapStorageFormattedLength(in TextureReadConfig src) {
		return	SerializationSizeOfString(src.FilePath) // FilePath
			+	SerializationSizeOfBool(); // IncludeWAlphaChannel
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureReadConfig src) {
		SerializationWriteString(ref dest, src.FilePath);
		SerializationWriteBool(ref dest, src.IncludeWAlphaChannel);
	}
	public static TextureReadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new TextureReadConfig {
			FilePath = SerializationReadString(ref src),
			IncludeWAlphaChannel = SerializationReadBool(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}

public readonly ref struct TextureCreationConfig : IConfigStruct<TextureCreationConfig> {
	public bool GenerateMipMaps { get; init; } = true;
	public bool IsLinearColorspace { get; init; } = true;
	public bool FlipX { get; init; } = false;
	public bool FlipY { get; init; } = false;
	public bool InvertXRedChannel { get; init; } = false;
	public bool InvertYGreenChannel { get; init; } = false;
	public bool InvertZBlueChannel { get; init; } = false;
	public bool InvertWAlphaChannel { get; init; } = false;
	// TODO xmldoc these four properties set which channels will make up the final output channels
	// TODO e.g. if XRedFinalOutputSource is 'G', the YGreen channel will be copied to XRed as the last step
	public ColorChannel XRedFinalOutputSource { get; init; } = ColorChannel.R;
	public ColorChannel YGreenFinalOutputSource { get; init; } = ColorChannel.G;
	public ColorChannel ZBlueFinalOutputSource { get; init; } = ColorChannel.B;
	public ColorChannel WAlphaFinalOutputSource { get; init; } = ColorChannel.A;
	public ReadOnlySpan<char> Name { get; init; }

	public TextureCreationConfig() { }

	internal void ThrowIfInvalid() { /* no-op */ }

	public static int GetHeapStorageFormattedLength(in TextureCreationConfig src) {
		return	SerializationSizeOfBool() // GenerateMipMaps
			+	SerializationSizeOfBool() // FlipX
			+	SerializationSizeOfBool() // FlipY
			+	SerializationSizeOfBool() // InvertXRedChannel
			+	SerializationSizeOfBool() // InvertYGreenChannel
			+	SerializationSizeOfBool() // InvertZBlueChannel
			+	SerializationSizeOfBool() // InvertWAlphaChannel
			+	SerializationSizeOfBool() // IsLinearColorspace
			+	SerializationSizeOfInt() // XRedFinalOutputSource
			+	SerializationSizeOfInt() // YGreenFinalOutputSource
			+	SerializationSizeOfInt() // ZBlueFinalOutputSource
			+	SerializationSizeOfInt() // WAlphaFinalOutputSource
			+	SerializationSizeOfString(src.Name); // Name
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureCreationConfig src) {
		SerializationWriteBool(ref dest, src.GenerateMipMaps);
		SerializationWriteBool(ref dest, src.FlipX);
		SerializationWriteBool(ref dest, src.FlipY);
		SerializationWriteBool(ref dest, src.InvertXRedChannel);
		SerializationWriteBool(ref dest, src.InvertYGreenChannel);
		SerializationWriteBool(ref dest, src.InvertZBlueChannel);
		SerializationWriteBool(ref dest, src.InvertWAlphaChannel);
		SerializationWriteBool(ref dest, src.IsLinearColorspace);
		SerializationWriteInt(ref dest, (int) src.XRedFinalOutputSource);
		SerializationWriteInt(ref dest, (int) src.YGreenFinalOutputSource);
		SerializationWriteInt(ref dest, (int) src.ZBlueFinalOutputSource);
		SerializationWriteInt(ref dest, (int) src.WAlphaFinalOutputSource);
		SerializationWriteString(ref dest, src.Name);
	}
	public static TextureCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new TextureCreationConfig {
			GenerateMipMaps = SerializationReadBool(ref src),
			FlipX = SerializationReadBool(ref src),
			FlipY = SerializationReadBool(ref src),
			InvertXRedChannel = SerializationReadBool(ref src),
			InvertYGreenChannel = SerializationReadBool(ref src),
			InvertZBlueChannel = SerializationReadBool(ref src),
			InvertWAlphaChannel = SerializationReadBool(ref src),
			IsLinearColorspace = SerializationReadBool(ref src),
			XRedFinalOutputSource = (ColorChannel) SerializationReadInt(ref src),
			YGreenFinalOutputSource = (ColorChannel) SerializationReadInt(ref src),
			ZBlueFinalOutputSource = (ColorChannel) SerializationReadInt(ref src),
			WAlphaFinalOutputSource = (ColorChannel) SerializationReadInt(ref src),
			Name = SerializationReadString(ref src),
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}

public readonly ref struct TextureGenerationConfig : IConfigStruct<TextureGenerationConfig> {
	public required int Width { get; init; }
	public required int Height { get; init; }

	public TextureGenerationConfig() { }

	internal void ThrowIfInvalid() {
		static void ThrowArgException(object erroneousArg, string message, [CallerArgumentExpression(nameof(erroneousArg))] string? argName = null) {
			throw new ArgumentException($"{nameof(TextureCreationConfig)}.{argName} {message} Value was {erroneousArg}.", argName);
		}

		if (Width < 1) {
			ThrowArgException(Width, "must be positive.");
		}
		if (Height < 1) {
			ThrowArgException(Height, "must be positive.");
		}
	}

	public static int GetHeapStorageFormattedLength(in TextureGenerationConfig src) {
		return	SerializationSizeOfInt() // Width
			+	SerializationSizeOfInt(); // Height
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureGenerationConfig src) {
		SerializationWriteInt(ref dest, src.Width);
		SerializationWriteInt(ref dest, src.Height);
	}
	public static TextureGenerationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new TextureGenerationConfig {
			Width = SerializationReadInt(ref src),
			Height = SerializationReadInt(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}