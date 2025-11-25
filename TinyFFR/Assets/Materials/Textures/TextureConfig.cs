// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.World;
using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly ref struct TextureReadConfig : IConfigStruct<TextureReadConfig> {
	public required ReadOnlySpan<char> FilePath { get; init; }
	public bool IncludeWAlphaChannel { get; init; } = true;

	public TextureReadConfig() { }

	internal void ThrowIfInvalid() {
		if (FilePath.IsEmpty) {
			throw new ArgumentException($"{nameof(TextureReadConfig)}.{nameof(FilePath)} can not be empty.", nameof(FilePath));
		}
	}

	public static int GetHeapStorageFormattedLength(in TextureReadConfig src) {
		return SerializationSizeOfString(src.FilePath) // FilePath
			+ SerializationSizeOfBool(); // IncludeWAlphaChannel
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
	public required bool IsLinearColorspace { get; init; }
	public ReadOnlySpan<char> Name { get; init; }
	public TextureProcessingConfig ProcessingToApply { get; init; } = TextureProcessingConfig.None;

	public TextureCreationConfig() { }

	internal void ThrowIfInvalid() {
		/* no-op */
	}

	public static int GetHeapStorageFormattedLength(in TextureCreationConfig src) {
		return	SerializationSizeOfBool() // GenerateMipMaps
			+	SerializationSizeOfBool() // IsLinearColorspace
			+	SerializationSizeOfString(src.Name) // Name
			+	SerializationSizeOfSubConfig(src.ProcessingToApply);
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureCreationConfig src) {
		SerializationWriteBool(ref dest, src.GenerateMipMaps);
		SerializationWriteBool(ref dest, src.IsLinearColorspace);
		SerializationWriteString(ref dest, src.Name);
		SerializationWriteSubConfig(ref dest, src.ProcessingToApply);
	}
	public static TextureCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new TextureCreationConfig {
			GenerateMipMaps = SerializationReadBool(ref src),
			IsLinearColorspace = SerializationReadBool(ref src),
			Name = SerializationReadString(ref src),
			ProcessingToApply = SerializationReadSubConfig<TextureProcessingConfig>(ref src),
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}

public readonly record struct TextureProcessingConfig : IConfigStruct<TextureProcessingConfig> {
	public static readonly TextureProcessingConfig None = new();

	public bool FlipX {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value;
		}
	} = false;
	public bool FlipY {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value;
		}
	} = false;
	public bool InvertXRedChannel {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value;
		}
	} = false;
	public bool InvertYGreenChannel {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value;
		}
	} = false;
	public bool InvertZBlueChannel {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value;
		}
	} = false;
	public bool InvertWAlphaChannel {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value;
		}
	} = false;
	// TODO xmldoc these four properties set which channels will make up the final output channels
	// TODO e.g. if XRedFinalOutputSource is 'G', the YGreen channel will be copied to XRed as the last step
	public ColorChannel XRedFinalOutputSource {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value != ColorChannel.R;
		}
	} = ColorChannel.R;
	public ColorChannel YGreenFinalOutputSource {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value != ColorChannel.G;
		}
	} = ColorChannel.G;
	public ColorChannel ZBlueFinalOutputSource {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value != ColorChannel.B;
		}
	} = ColorChannel.B;
	public ColorChannel WAlphaFinalOutputSource {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value != ColorChannel.A;
		}
	} = ColorChannel.A;
	
	internal bool RequiresProcessing { get; private init; } = false;

	public TextureProcessingConfig() { }

	public static TextureProcessingConfig Flip(bool aroundVerticalCentre, bool aroundHorizontalCentre) {
		return new() {
			FlipX = aroundVerticalCentre, 
			FlipY = aroundHorizontalCentre
		};
	}
	public static TextureProcessingConfig Negate(bool includeRedChannel = true, bool includeGreenChannel = true, bool includeBlueChannel = true, bool includeAlphaChannel = true) {
		return new() {
			InvertXRedChannel = includeRedChannel, 
			InvertYGreenChannel = includeGreenChannel, 
			InvertZBlueChannel = includeBlueChannel, 
			InvertWAlphaChannel = includeAlphaChannel
		};
	}
	public static TextureProcessingConfig Swizzle(ColorChannel redSource = ColorChannel.R, ColorChannel greenSource = ColorChannel.G, ColorChannel blueSource = ColorChannel.B, ColorChannel alphaSource = ColorChannel.A) {
		return new() {
			XRedFinalOutputSource = redSource, 
			YGreenFinalOutputSource = greenSource,
			ZBlueFinalOutputSource = blueSource,
			WAlphaFinalOutputSource = alphaSource
		};
	}

	internal void ThrowIfInvalid() { /* no-op */ }

	public static int GetHeapStorageFormattedLength(in TextureProcessingConfig src) {
		return SerializationSizeOfBool() // FlipX
			+ SerializationSizeOfBool() // FlipY
			+ SerializationSizeOfBool() // InvertXRedChannel
			+ SerializationSizeOfBool() // InvertYGreenChannel
			+ SerializationSizeOfBool() // InvertZBlueChannel
			+ SerializationSizeOfBool() // InvertWAlphaChannel
			+ SerializationSizeOfInt() // XRedFinalOutputSource
			+ SerializationSizeOfInt() // YGreenFinalOutputSource
			+ SerializationSizeOfInt() // ZBlueFinalOutputSource
			+ SerializationSizeOfInt(); // WAlphaFinalOutputSource
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureProcessingConfig src) {
		SerializationWriteBool(ref dest, src.FlipX);
		SerializationWriteBool(ref dest, src.FlipY);
		SerializationWriteBool(ref dest, src.InvertXRedChannel);
		SerializationWriteBool(ref dest, src.InvertYGreenChannel);
		SerializationWriteBool(ref dest, src.InvertZBlueChannel);
		SerializationWriteBool(ref dest, src.InvertWAlphaChannel);
		SerializationWriteInt(ref dest, (int) src.XRedFinalOutputSource);
		SerializationWriteInt(ref dest, (int) src.YGreenFinalOutputSource);
		SerializationWriteInt(ref dest, (int) src.ZBlueFinalOutputSource);
		SerializationWriteInt(ref dest, (int) src.WAlphaFinalOutputSource);
	}
	public static TextureProcessingConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new TextureProcessingConfig {
			FlipX = SerializationReadBool(ref src),
			FlipY = SerializationReadBool(ref src),
			InvertXRedChannel = SerializationReadBool(ref src),
			InvertYGreenChannel = SerializationReadBool(ref src),
			InvertZBlueChannel = SerializationReadBool(ref src),
			InvertWAlphaChannel = SerializationReadBool(ref src),
			XRedFinalOutputSource = (ColorChannel) SerializationReadInt(ref src),
			YGreenFinalOutputSource = (ColorChannel) SerializationReadInt(ref src),
			ZBlueFinalOutputSource = (ColorChannel) SerializationReadInt(ref src),
			WAlphaFinalOutputSource = (ColorChannel) SerializationReadInt(ref src),
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}

public readonly ref struct TextureGenerationConfig : IConfigStruct<TextureGenerationConfig> {
	public required XYPair<int> Dimensions { get; init; }

	public TextureGenerationConfig() { }

	internal void ThrowIfInvalid() {
		static void ThrowArgException(object erroneousArg, string message, [CallerArgumentExpression(nameof(erroneousArg))] string? argName = null) {
			throw new InvalidOperationException($"{nameof(TextureCreationConfig)}.{argName} {message} Value was {erroneousArg}.");
		}

		if (Dimensions.X < 1) {
			ThrowArgException(Dimensions.X, "must be positive.");
		}
		if (Dimensions.Y < 1) {
			ThrowArgException(Dimensions.Y, "must be positive.");
		}
	}

	public static int GetHeapStorageFormattedLength(in TextureGenerationConfig src) {
		return	SerializationSizeOf<XYPair<int>>(); // Dimensions
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureGenerationConfig src) {
		SerializationWrite(ref dest, src.Dimensions);
	}
	public static TextureGenerationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new TextureGenerationConfig {
			Dimensions = SerializationRead<XYPair<int>>(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}