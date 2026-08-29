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

public enum TextureDataType {
	LinearData = 0,
	LinearDataUnitVector = 1,
	LinearDataTwoChannelMax = 2,
	ColorSrgb = 3,
}

public static class TextureDataTypeExtensions {
	public static bool UsesLinearColorspace(this TextureDataType @this) => @this != TextureDataType.ColorSrgb;
}

public readonly ref struct TextureCreationConfig : IConfigStruct<TextureCreationConfig> {
	public static readonly Quality? DefaultCompressionQuality = null;
	
	public static TextureCreationConfig ForColorTexture(ReadOnlySpan<char> name = default) => ForColorTexture(DefaultCompressionQuality, name);
	public static TextureCreationConfig ForColorTexture(Quality? compressionQuality, ReadOnlySpan<char> name = default) => new() {
		GenerateMipMaps = true,
		DataType = TextureDataType.ColorSrgb,
		RenderingConfig = new(),
		ProcessingToApply = TextureProcessingConfig.None,
		CompressionQuality = compressionQuality,
		Name = name
	};
	public static TextureCreationConfig ForCanvasTexture(ReadOnlySpan<char> name = default) => new() {
		GenerateMipMaps = true,
		DataType = TextureDataType.LinearData, // Because canvas scenes have postprocessing disabled which includes the srgb/linear conversion pipeline
		RenderingConfig = new() {
			AnisotropicFilteringQuality	= Quality.VeryLow,
			AnisotropyLevel = 0f,
			DisableTextureRepeat = true
		},
		ProcessingToApply = TextureProcessingConfig.None,
		CompressionQuality = null,
		Name = name
	};
	public static TextureCreationConfig ForDataTexture(TextureDataType dataType, ReadOnlySpan<char> name = default) => ForDataTexture(dataType, DefaultCompressionQuality, name);
	public static TextureCreationConfig ForDataTexture(TextureDataType dataType, Quality? compressionQuality, ReadOnlySpan<char> name = default) => new() {
		GenerateMipMaps = true,
		DataType = dataType,
		RenderingConfig = new(),
		ProcessingToApply = TextureProcessingConfig.None,
		CompressionQuality = compressionQuality,
		Name = name
	};

	public bool GenerateMipMaps { get; init; } = true;
	public Quality? CompressionQuality { get; init; } = DefaultCompressionQuality;
	public required TextureDataType DataType { get; init; }
	public bool AllowsDynamicWrites {
		get; 
		init {
			if (value) {
				GenerateMipMaps = false;
				CompressionQuality = null;
			}
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
		if (AllowsDynamicWrites && CompressionQuality != null) {
			throw new InvalidOperationException(
				$"It is not permitted for {nameof(CompressionQuality)} to be non-null and {nameof(AllowsDynamicWrites)} to be true simultaneously."
			);
		}
	}

	public static int GetHeapStorageFormattedLength(in TextureCreationConfig src) {
		return	SerializationSizeOfBool() // GenerateMipMaps
			+	SerializationSizeOfBool() // AllowsDynamicWrites
			+	SerializationSizeOfBool() // CompressionQuality.HasValue
			+	SerializationSizeOfInt() // CompressionQuality
			+	SerializationSizeOfInt() // DataType
			+	SerializationSizeOfBool() // SamplingConfig.DisableTextureRepeat
			+	SerializationSizeOfBool() // SamplingConfig.DisableTexelBlending
			+	SerializationSizeOfInt() // SamplingConfig.AnisotropicFilteringQuality
			+	SerializationSizeOfFloat() // SamplingConfig.AnisotropyLevel
			+	SerializationSizeOfString(src.Name) // Name
			+	SerializationSizeOfSubConfig(src.ProcessingToApply);
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureCreationConfig src) {
		SerializationWriteBool(ref dest, src.GenerateMipMaps);
		SerializationWriteBool(ref dest, src.AllowsDynamicWrites);
		SerializationWriteBool(ref dest, src.CompressionQuality != null);
		SerializationWriteInt(ref dest, (int) (src.CompressionQuality ?? default));
		SerializationWriteInt(ref dest, (int) src.DataType);
		SerializationWriteBool(ref dest, src.RenderingConfig.DisableTextureRepeat);
		SerializationWriteBool(ref dest, src.RenderingConfig.DisableTexelBlending);
		SerializationWriteInt(ref dest, (int) src.RenderingConfig.AnisotropicFilteringQuality);
		SerializationWriteFloat(ref dest, src.RenderingConfig.AnisotropyLevel);
		SerializationWriteString(ref dest, src.Name);
		SerializationWriteSubConfig(ref dest, src.ProcessingToApply);
	}
	public static TextureCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var generateMipMaps = SerializationReadBool(ref src);
		var allowsDynamicWrites = SerializationReadBool(ref src);
		var hasCompressionQuality = SerializationReadBool(ref src);
		var compressionQualityValue = (Quality) SerializationReadInt(ref src);
		var dataType = (TextureDataType) SerializationReadInt(ref src);
		var disableTextureRepeat = SerializationReadBool(ref src);
		var disableTexelBlending = SerializationReadBool(ref src);
		var anisotropicFilteringQuality = (Quality) SerializationReadInt(ref src);
		var anisotropyLevel = SerializationReadFloat(ref src);

		return new TextureCreationConfig {
			GenerateMipMaps = generateMipMaps,
			AllowsDynamicWrites = allowsDynamicWrites,
			CompressionQuality = hasCompressionQuality ? compressionQualityValue : null,
			DataType = dataType,
			RenderingConfig = new TextureRenderingConfig(disableTextureRepeat, disableTexelBlending, anisotropicFilteringQuality) {
				AnisotropyLevel = anisotropyLevel
			},
			Name = SerializationReadString(ref src),
			ProcessingToApply = SerializationReadSubConfig<TextureProcessingConfig>(ref src),
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var converted = ConvertFromAllocatedHeapStorage(src);
		var processingConfigSlice = src[(GetHeapStorageFormattedLength(in converted) - SerializationSizeOfSubConfig(converted.ProcessingToApply))..];
		SerializationDisposeSubConfig<TextureProcessingConfig>(ref processingConfigSlice);
	}
}

public readonly struct TexelProcessingFunction : IEquatable<TexelProcessingFunction> {
	internal nint FunctionPtr { get; private init; }
	internal nint TexelTypeHandle { get; private init; }

	public static unsafe TexelProcessingFunction Create<TTexel>(delegate*<Span<TTexel>, object?, void> function) where TTexel : unmanaged, ITexel<TTexel> {
		if (function == null) throw new ArgumentNullException(nameof(function));
		return new TexelProcessingFunction {
			FunctionPtr = (nint) function,
			TexelTypeHandle = typeof(TTexel).TypeHandle.Value
		};
	}
	
	public bool ExpectsTexelType<TTexel>() => TexelTypeHandle == typeof(TTexel).TypeHandle.Value;

	internal static TexelProcessingFunction FromSerializedValues(nint functionPtr, nint texelTypeHandle) {
		return new TexelProcessingFunction {
			FunctionPtr = functionPtr,
			TexelTypeHandle = texelTypeHandle
		};
	}

	internal unsafe void Invoke<TTexel>(Span<TTexel> texels, object? argument) where TTexel : unmanaged, ITexel<TTexel> {
		if (!ExpectsTexelType<TTexel>()) {
			throw new InvalidOperationException(
				$"This {nameof(TexelProcessingFunction)} was created for a different texel type than {typeof(TTexel).Name}. " +
				$"A function created via {nameof(TexelProcessingFunction)}.{nameof(Create)}<T>() may only be applied to textures whose texel type is exactly T."
			);
		}
		((delegate*<Span<TTexel>, object?, void>) FunctionPtr)(texels, argument);
	}

	public bool Equals(TexelProcessingFunction other) => FunctionPtr == other.FunctionPtr && TexelTypeHandle == other.TexelTypeHandle;
	public override bool Equals(object? obj) => obj is TexelProcessingFunction other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(FunctionPtr, TexelTypeHandle);
	public static bool operator ==(TexelProcessingFunction left, TexelProcessingFunction right) => left.Equals(right);
	public static bool operator !=(TexelProcessingFunction left, TexelProcessingFunction right) => !left.Equals(right);
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
	
	public TexelProcessingFunction? PostProcessingFunction {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value != null;
		}
	} = null;
	public object? PostProcessingArgument { get; init; } = null;

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

	internal void ThrowIfInvalid() {
		/* no-op */
	}

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
			+ SerializationSizeOfInt() // WAlphaFinalOutputSource
			+ SerializationSizeOfLong()
			+ SerializationSizeOfLong()
			+ SerializationSizeOfLong();
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
		SerializationWriteLong(ref dest, src.PostProcessingFunction is { } function ? function.FunctionPtr : 0L);
		SerializationWriteLong(ref dest, src.PostProcessingFunction is { } fn ? fn.TexelTypeHandle : 0L);
		SerializationWriteLong(ref dest, src.PostProcessingArgument is { } argument ? GCHandle.ToIntPtr(GCHandle.Alloc(argument)) : 0L);
	}
	public static TextureProcessingConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var flipX = SerializationReadBool(ref src);
		var flipY = SerializationReadBool(ref src);
		var invertXRedChannel = SerializationReadBool(ref src);
		var invertYGreenChannel = SerializationReadBool(ref src);
		var invertZBlueChannel = SerializationReadBool(ref src);
		var invertWAlphaChannel = SerializationReadBool(ref src);
		var multiplyAlpha = SerializationReadBool(ref src);
		var xRedFinalOutputSource = (ColorChannel) SerializationReadInt(ref src);
		var yGreenFinalOutputSource = (ColorChannel) SerializationReadInt(ref src);
		var zBlueFinalOutputSource = (ColorChannel) SerializationReadInt(ref src);
		var wAlphaFinalOutputSource = (ColorChannel) SerializationReadInt(ref src);
		var functionPtr = (nint) SerializationReadLong(ref src);
		var texelTypeHandle = (nint) SerializationReadLong(ref src);
		var argumentHandle = (nint) SerializationReadLong(ref src);

		return new TextureProcessingConfig {
			FlipX = flipX,
			FlipY = flipY,
			InvertXRedChannel = invertXRedChannel,
			InvertYGreenChannel = invertYGreenChannel,
			InvertZBlueChannel = invertZBlueChannel,
			InvertWAlphaChannel = invertWAlphaChannel,
			MultiplyAlpha = multiplyAlpha,
			XRedFinalOutputSource = xRedFinalOutputSource,
			YGreenFinalOutputSource = yGreenFinalOutputSource,
			ZBlueFinalOutputSource = zBlueFinalOutputSource,
			WAlphaFinalOutputSource = wAlphaFinalOutputSource,
			PostProcessingFunction = functionPtr != 0 ? TexelProcessingFunction.FromSerializedValues(functionPtr, texelTypeHandle) : null,
			PostProcessingArgument = argumentHandle != 0 ? GCHandle.FromIntPtr(argumentHandle).Target : null
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var argumentSlice = src[^SerializationSizeOfLong()..];
		var argumentHandle = (nint) SerializationReadLong(ref argumentSlice);
		if (argumentHandle != 0) GCHandle.FromIntPtr(argumentHandle).Free();
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
		SerializationDisposeSubConfig<TextureCreationConfig>(ref src);
		SerializationDisposeSubConfig<TextureReadConfig>(ref src);
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
		SerializationDisposeSubConfig<TextureCreationConfig>(ref src);
		SerializationDisposeSubConfig<TextureCombinationConfig>(ref src);
		SerializationDisposeSubConfig<TextureProcessingConfig>(ref src);
		SerializationDisposeSubConfig<TextureProcessingConfig>(ref src);
		SerializationDisposeSubConfig<TextureProcessingConfig>(ref src);
		SerializationDisposeSubConfig<TextureProcessingConfig>(ref src);
	}
}
