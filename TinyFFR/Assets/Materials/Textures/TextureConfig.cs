// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.World;
using System;
using Egodystonic.TinyFFR.Rendering;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly record struct TextureRenderingConfig {
	public bool DisableTextureRepeat { get; init; }
	public bool DisableTexelBlending {
		get; 
		init {
			field = value;
			AnisotropyLevel = CalcAnisotropy(value, AnisotropicFilteringQuality);
		}
	}
	public Quality AnisotropicFilteringQuality {
		get;
		init {
			field = value;
			AnisotropyLevel = CalcAnisotropy(DisableTexelBlending, value);
		}
	}
	internal float AnisotropyLevel { get; init; } = CalcAnisotropy(false, Quality.Standard);

	public TextureRenderingConfig() { }

	public TextureRenderingConfig(bool disableTextureRepeat, bool disableTexelBlending, Quality anisotropicFilteringQuality) {
		DisableTextureRepeat = disableTextureRepeat;
		DisableTexelBlending = disableTexelBlending;
		AnisotropicFilteringQuality = anisotropicFilteringQuality;
	}
	
	static float CalcAnisotropy(bool disableBlending, Quality filteringQuality) {
		if (disableBlending || !Enum.IsDefined(filteringQuality)) return 1f;
		else return 1 << (((int) filteringQuality) - ((int) Quality.VeryLow));
	}
}

public readonly ref struct TextureReadConfig : IConfigStruct<TextureReadConfig> {
	public bool IncludeWAlphaChannel { get; init; } = true;
	public bool ForceWAlphaChannelPresence { get; init; } = false;

	public TextureReadConfig() { }

	internal void ThrowIfInvalid() {
		if (ForceWAlphaChannelPresence && !IncludeWAlphaChannel) {
			throw new InvalidOperationException(
				$"It is not permitted for {nameof(ForceWAlphaChannelPresence)} to be true while {nameof(IncludeWAlphaChannel)} is false."
			);
		}
	}

	public static int GetHeapStorageFormattedLength(in TextureReadConfig src) {
		return	SerializationSizeOfBool() // IncludeWAlphaChannel
			+	SerializationSizeOfBool(); // ForceWAlphaChannelPresence
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureReadConfig src) {
		SerializationWriteBool(ref dest, src.IncludeWAlphaChannel);
		SerializationWriteBool(ref dest, src.ForceWAlphaChannelPresence);
	}
	public static TextureReadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new TextureReadConfig {
			IncludeWAlphaChannel = SerializationReadBool(ref src),
			ForceWAlphaChannelPresence = SerializationReadBool(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}

public readonly ref struct TextureCreationConfig : IConfigStruct<TextureCreationConfig> {
	public static TextureCreationConfig ForColorTexture(ReadOnlySpan<char> name = default) => new() {
		GenerateMipMaps = true,
		IsLinearColorspace = false,
		RenderingConfig = new(),
		ProcessingToApply = TextureProcessingConfig.None,
		Name = name
	};
	public static TextureCreationConfig ForCanvasTexture(ReadOnlySpan<char> name = default) => new() {
		GenerateMipMaps = true,
		IsLinearColorspace = true,
		RenderingConfig = new() {
			AnisotropicFilteringQuality	= Quality.VeryLow,
			AnisotropyLevel = 0f,
			DisableTextureRepeat = true
		},
		ProcessingToApply = TextureProcessingConfig.None,
		Name = name
	};
	public static TextureCreationConfig ForDataTexture(ReadOnlySpan<char> name = default) => new() {
		GenerateMipMaps = true,
		IsLinearColorspace = true,
		RenderingConfig = new(),
		ProcessingToApply = TextureProcessingConfig.None,
		Name = name
	};
	
	public bool GenerateMipMaps { get; init; } = true;
	public required bool IsLinearColorspace { get; init; }
	public bool AllowsDynamicWrites {
		get; 
		init {
			if (value) GenerateMipMaps = false;
			field = value;
		}
	} = false;
	public TextureRenderingConfig RenderingConfig { get; init; } = new();
	public ReadOnlySpan<char> Name { get; init; }
	public TextureProcessingConfig ProcessingToApply { get; init; } = TextureProcessingConfig.None;

	public TextureCreationConfig() { }

	internal void ThrowIfInvalid() {
		if (AllowsDynamicWrites && GenerateMipMaps) {
			throw new InvalidOperationException(
				$"It is not permitted for both {nameof(GenerateMipMaps)} and {nameof(AllowsDynamicWrites)} to be true simultaneously."
			);
		}
	}

	public static int GetHeapStorageFormattedLength(in TextureCreationConfig src) {
		return	SerializationSizeOfBool() // GenerateMipMaps
			+	SerializationSizeOfBool() // IsLinearColorspace
			+	SerializationSizeOfBool() // AllowsDynamicWrites
			+	SerializationSizeOfBool() // SamplingConfig.DisableTextureRepeat
			+	SerializationSizeOfBool() // SamplingConfig.DisableTexelBlending
			+	SerializationSizeOfInt() // SamplingConfig.AnisotropicFilteringQuality
			+	SerializationSizeOfFloat() // SamplingConfig.AnisotropyLevel
			+	SerializationSizeOfString(src.Name) // Name
			+	SerializationSizeOfSubConfig(src.ProcessingToApply);
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureCreationConfig src) {
		SerializationWriteBool(ref dest, src.GenerateMipMaps);
		SerializationWriteBool(ref dest, src.IsLinearColorspace);
		SerializationWriteBool(ref dest, src.AllowsDynamicWrites);
		SerializationWriteBool(ref dest, src.RenderingConfig.DisableTextureRepeat);
		SerializationWriteBool(ref dest, src.RenderingConfig.DisableTexelBlending);
		SerializationWriteInt(ref dest, (int) src.RenderingConfig.AnisotropicFilteringQuality);
		SerializationWriteFloat(ref dest, src.RenderingConfig.AnisotropyLevel);
		SerializationWriteString(ref dest, src.Name);
		SerializationWriteSubConfig(ref dest, src.ProcessingToApply);
	}
	public static TextureCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var generateMipMaps = SerializationReadBool(ref src);
		var isLinearColorspace = SerializationReadBool(ref src);
		var allowsDynamicWrites = SerializationReadBool(ref src);
		var disableTextureRepeat = SerializationReadBool(ref src);
		var disableTexelBlending = SerializationReadBool(ref src);
		var anisotropicFilteringQuality = (Quality) SerializationReadInt(ref src);
		var anisotropyLevel = SerializationReadFloat(ref src);

		return new TextureCreationConfig {
			GenerateMipMaps = generateMipMaps,
			IsLinearColorspace = isLinearColorspace,
			AllowsDynamicWrites = allowsDynamicWrites,
			RenderingConfig = new TextureRenderingConfig(disableTextureRepeat, disableTexelBlending, anisotropicFilteringQuality) {
				AnisotropyLevel = anisotropyLevel
			},
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
	public bool MultiplyAlpha {
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
	public static TextureProcessingConfig Invert(bool includeRedChannel = true, bool includeGreenChannel = true, bool includeBlueChannel = true, bool includeAlphaChannel = true) {
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
	public static TextureProcessingConfig PremultiplyAlpha() {
		return new() {
			MultiplyAlpha = true
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
			+ SerializationSizeOfBool() // MultiplyAlpha
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
		SerializationWriteBool(ref dest, src.MultiplyAlpha);
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
			MultiplyAlpha = SerializationReadBool(ref src),
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
readonly ref struct TextureLoadConfig : IConfigStruct<TextureLoadConfig> {
	public TextureCreationConfig CreationConfig { get; init; }
	public TextureReadConfig ReadConfig { get; init; }

	public TextureLoadConfig() { }

	internal void ThrowIfInvalid() {
		CreationConfig.ThrowIfInvalid();
		ReadConfig.ThrowIfInvalid();
	}

	public static int GetHeapStorageFormattedLength(in TextureLoadConfig src) {
		return	SerializationSizeOfSubConfig(src.CreationConfig) // CreationConfig
			+	SerializationSizeOfSubConfig(src.ReadConfig); // ReadConfig
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureLoadConfig src) {
		SerializationWriteSubConfig(ref dest, src.CreationConfig);
		SerializationWriteSubConfig(ref dest, src.ReadConfig);
	}
	public static TextureLoadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new TextureLoadConfig {
			CreationConfig = SerializationReadSubConfig<TextureCreationConfig>(ref src),
			ReadConfig = SerializationReadSubConfig<TextureReadConfig>(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var converted = ConvertFromAllocatedHeapStorage(src);
		var creationConfigLength = SerializationSizeOfSubConfig(converted.CreationConfig);
		var readConfigLength = SerializationSizeOfSubConfig(converted.ReadConfig);
		TextureCreationConfig.DisposeAllocatedHeapStorage(src[..creationConfigLength]);
		TextureReadConfig.DisposeAllocatedHeapStorage(src[creationConfigLength..][..readConfigLength]);
	}
}

readonly ref struct TextureCombinedLoadConfig : IConfigStruct<TextureCombinedLoadConfig> {
	public TextureCreationConfig CreationConfig { get; init; }
	public TextureCombinationConfig CombinationConfig { get; init; }
	public TextureProcessingConfig ProcessingConfigA { get; init; }
	public TextureProcessingConfig ProcessingConfigB { get; init; }
	public TextureProcessingConfig ProcessingConfigC { get; init; }
	public TextureProcessingConfig ProcessingConfigD { get; init; }
	public int SourceCount { get; init; }

	public TextureCombinedLoadConfig() { }

	internal void ThrowIfInvalid() {
		CreationConfig.ThrowIfInvalid();
		CombinationConfig.ThrowIfInvalid(SourceCount);
		ProcessingConfigA.ThrowIfInvalid();
		ProcessingConfigB.ThrowIfInvalid();
		ProcessingConfigC.ThrowIfInvalid();
		ProcessingConfigD.ThrowIfInvalid();
	}

	public static int GetHeapStorageFormattedLength(in TextureCombinedLoadConfig src) {
		return	SerializationSizeOfSubConfig(src.CreationConfig) // CreationConfig
			+	SerializationSizeOfSubConfig(src.CombinationConfig) // CombinationConfig
			+	SerializationSizeOfSubConfig(src.ProcessingConfigA) // ProcessingConfigA
			+	SerializationSizeOfSubConfig(src.ProcessingConfigB) // ProcessingConfigB
			+	SerializationSizeOfSubConfig(src.ProcessingConfigC) // ProcessingConfigC
			+	SerializationSizeOfSubConfig(src.ProcessingConfigD) // ProcessingConfigD
			+	SerializationSizeOfInt(); // SourceCount
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureCombinedLoadConfig src) {
		SerializationWriteSubConfig(ref dest, src.CreationConfig);
		SerializationWriteSubConfig(ref dest, src.CombinationConfig);
		SerializationWriteSubConfig(ref dest, src.ProcessingConfigA);
		SerializationWriteSubConfig(ref dest, src.ProcessingConfigB);
		SerializationWriteSubConfig(ref dest, src.ProcessingConfigC);
		SerializationWriteSubConfig(ref dest, src.ProcessingConfigD);
		SerializationWriteInt(ref dest, src.SourceCount);
	}
	public static TextureCombinedLoadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new TextureCombinedLoadConfig {
			CreationConfig = SerializationReadSubConfig<TextureCreationConfig>(ref src),
			CombinationConfig = SerializationReadSubConfig<TextureCombinationConfig>(ref src),
			ProcessingConfigA = SerializationReadSubConfig<TextureProcessingConfig>(ref src),
			ProcessingConfigB = SerializationReadSubConfig<TextureProcessingConfig>(ref src),
			ProcessingConfigC = SerializationReadSubConfig<TextureProcessingConfig>(ref src),
			ProcessingConfigD = SerializationReadSubConfig<TextureProcessingConfig>(ref src),
			SourceCount = SerializationReadInt(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var converted = ConvertFromAllocatedHeapStorage(src);
		TextureCreationConfig.DisposeAllocatedHeapStorage(src[..SerializationSizeOfSubConfig(converted.CreationConfig)]);
	}
}
