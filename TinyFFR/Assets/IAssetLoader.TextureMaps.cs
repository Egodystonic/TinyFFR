// Created on 2026-08-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.IO;
using System.Threading;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;
using static Egodystonic.TinyFFR.Assets.Materials.TextureCombinationSourceTexture;
using static Egodystonic.TinyFFR.ColorChannel;

namespace Egodystonic.TinyFFR.Assets;

public partial interface IAssetLoader {
	#region Color & Canvas
	Texture LoadColorMap(ReadOnlySpan<char> filePath) => LoadTexture(filePath, TextureCreationConfig.ForColorTexture(Path.GetFileName(filePath)));

	TinyFfrAsyncOperation<Texture> LoadColorMapAsync(ReadOnlySpan<char> filePath) => LoadTextureAsync(filePath, TextureCreationConfig.ForColorTexture(Path.GetFileName(filePath)));

	Texture LoadCanvasTexture(ReadOnlySpan<char> filePath) => LoadTexture(filePath, TextureCreationConfig.ForCanvasTexture(Path.GetFileName(filePath)));

	TinyFfrAsyncOperation<Texture> LoadCanvasTextureAsync(ReadOnlySpan<char> filePath) => LoadTextureAsync(filePath, TextureCreationConfig.ForCanvasTexture(Path.GetFileName(filePath)));
	#endregion

	#region Normal
	Texture LoadNormalMap(ReadOnlySpan<char> filePath, bool isDirectXFormat = false) {
		if (!isDirectXFormat) return LoadTexture(filePath, TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath)));
		return LoadTexture(
			filePath,
			TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath)) with {
				ProcessingToApply = new TextureProcessingConfig { InvertYGreenChannel = true }
			}
		);
	}

	TinyFfrAsyncOperation<Texture> LoadNormalMapAsync(ReadOnlySpan<char> filePath, bool isDirectXFormat = false) {
		if (!isDirectXFormat) return LoadTextureAsync(filePath, TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath)));
		return LoadTextureAsync(
			filePath,
			TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath)) with {
				ProcessingToApply = new TextureProcessingConfig { InvertYGreenChannel = true }
			}
		);
	}
	#endregion

	#region ORM & ORMR
	Texture LoadOcclusionRoughnessMetallicMap(ReadOnlySpan<char> filePath) => LoadTexture(filePath, TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath)));
	Texture LoadOcclusionRoughnessMetallicMap(ReadOnlySpan<char> occlusionFilePath, ReadOnlySpan<char> roughnessFilePath, ReadOnlySpan<char> metallicFilePath) {
		var a = Path.GetFileName(occlusionFilePath);
		var b = Path.GetFileName(roughnessFilePath);
		var c = Path.GetFileName(metallicFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b, "+", c)];
		SpanUtils.Concatenate(name, a, "+", b, "+", c);

		return LoadCombinedTexture(
			occlusionFilePath,
			roughnessFilePath,
			metallicFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureB, R),
				OutputTextureZBlueChannelSource = new(TextureC, R)
			},
			TextureCreationConfig.ForDataTexture(name)
		);
	}

	TinyFfrAsyncOperation<Texture> LoadOcclusionRoughnessMetallicMapAsync(ReadOnlySpan<char> filePath) => LoadTextureAsync(filePath, TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath)));
	TinyFfrAsyncOperation<Texture> LoadOcclusionRoughnessMetallicMapAsync(ReadOnlySpan<char> occlusionFilePath, ReadOnlySpan<char> roughnessFilePath, ReadOnlySpan<char> metallicFilePath) {
		var a = Path.GetFileName(occlusionFilePath);
		var b = Path.GetFileName(roughnessFilePath);
		var c = Path.GetFileName(metallicFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b, "+", c)];
		SpanUtils.Concatenate(name, a, "+", b, "+", c);

		return LoadCombinedTextureAsync(
			occlusionFilePath,
			roughnessFilePath,
			metallicFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureB, R),
				OutputTextureZBlueChannelSource = new(TextureC, R)
			},
			TextureCreationConfig.ForDataTexture(name)
		);
	}

	Texture LoadOcclusionRoughnessMetallicReflectanceMap(ReadOnlySpan<char> filePath) {
		if (ReadTextureMetadata(filePath).IncludesAlphaChannel) return LoadTexture(filePath, TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath)));
		else return LoadOcclusionRoughnessMetallicReflectanceMap(filePath, BuiltInTexturePaths.DefaultReflectanceMap);
	}
	Texture LoadOcclusionRoughnessMetallicReflectanceMap(ReadOnlySpan<char> occlusionRoughnessMetallicFilePath, ReadOnlySpan<char> reflectanceFilePath) {
		var a = Path.GetFileName(occlusionRoughnessMetallicFilePath);
		var b = Path.GetFileName(reflectanceFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTexture(
			occlusionRoughnessMetallicFilePath,
			reflectanceFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureA, B),
				OutputTextureWAlphaChannelSource = new(TextureB, R),
			},
			TextureCreationConfig.ForDataTexture(name)
		);
	}
	Texture LoadOcclusionRoughnessMetallicReflectanceMap(ReadOnlySpan<char> occlusionFilePath, ReadOnlySpan<char> roughnessFilePath, ReadOnlySpan<char> metallicFilePath, ReadOnlySpan<char> reflectanceFilePath) {
		var a = Path.GetFileName(occlusionFilePath);
		var b = Path.GetFileName(roughnessFilePath);
		var c = Path.GetFileName(metallicFilePath);
		var d = Path.GetFileName(reflectanceFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b, "+", c, "+", d)];
		SpanUtils.Concatenate(name, a, "+", b, "+", c, "+", d);

		return LoadCombinedTexture(
			occlusionFilePath,
			roughnessFilePath,
			metallicFilePath,
			reflectanceFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureB, R),
				OutputTextureZBlueChannelSource = new(TextureC, R),
				OutputTextureWAlphaChannelSource = new(TextureD, R),
			},
			TextureCreationConfig.ForDataTexture(name)
		);
	}

	TinyFfrAsyncOperation<Texture> LoadOcclusionRoughnessMetallicReflectanceMapAsync(ReadOnlySpan<char> filePath) {
		if (ReadTextureMetadata(filePath).IncludesAlphaChannel) return LoadTextureAsync(filePath, TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath)));
		else return LoadOcclusionRoughnessMetallicReflectanceMapAsync(filePath, BuiltInTexturePaths.DefaultReflectanceMap);
	}
	TinyFfrAsyncOperation<Texture> LoadOcclusionRoughnessMetallicReflectanceMapAsync(ReadOnlySpan<char> occlusionRoughnessMetallicFilePath, ReadOnlySpan<char> reflectanceFilePath) {
		var a = Path.GetFileName(occlusionRoughnessMetallicFilePath);
		var b = Path.GetFileName(reflectanceFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTextureAsync(
			occlusionRoughnessMetallicFilePath,
			reflectanceFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureA, B),
				OutputTextureWAlphaChannelSource = new(TextureB, R),
			},
			TextureCreationConfig.ForDataTexture(name)
		);
	}
	TinyFfrAsyncOperation<Texture> LoadOcclusionRoughnessMetallicReflectanceMapAsync(ReadOnlySpan<char> occlusionFilePath, ReadOnlySpan<char> roughnessFilePath, ReadOnlySpan<char> metallicFilePath, ReadOnlySpan<char> reflectanceFilePath) {
		var a = Path.GetFileName(occlusionFilePath);
		var b = Path.GetFileName(roughnessFilePath);
		var c = Path.GetFileName(metallicFilePath);
		var d = Path.GetFileName(reflectanceFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b, "+", c, "+", d)];
		SpanUtils.Concatenate(name, a, "+", b, "+", c, "+", d);

		return LoadCombinedTextureAsync(
			occlusionFilePath,
			roughnessFilePath,
			metallicFilePath,
			reflectanceFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureB, R),
				OutputTextureZBlueChannelSource = new(TextureC, R),
				OutputTextureWAlphaChannelSource = new(TextureD, R),
			},
			TextureCreationConfig.ForDataTexture(name)
		);
	}
	#endregion

	#region AT
	Texture LoadAbsorptionTransmissionMap(ReadOnlySpan<char> filePath, bool invertAbsorption = false) {
		var includesTransmission = ReadTextureMetadata(filePath).IncludesAlphaChannel;
		if (!includesTransmission) return LoadAbsorptionTransmissionMap(filePath, BuiltInTexturePaths.DefaultTransmissionMap, invertAbsorption);
		if (!invertAbsorption) return LoadTexture(filePath, TextureCreationConfig.ForColorTexture(Path.GetFileName(filePath)));

		return LoadTexture(
			filePath,
			TextureCreationConfig.ForColorTexture(Path.GetFileName(filePath)) with {
				ProcessingToApply = TextureProcessingConfig.Invert(includeAlphaChannel: false)
			}
		);
	}
	Texture LoadAbsorptionTransmissionMap(ReadOnlySpan<char> absorptionFilePath, ReadOnlySpan<char> transmissionFilePath, bool invertAbsorption = false) {
		var a = Path.GetFileName(absorptionFilePath);
		var b = Path.GetFileName(transmissionFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTexture(
			absorptionFilePath, invertAbsorption ? TextureProcessingConfig.Invert(includeAlphaChannel: false) : TextureProcessingConfig.None,
			transmissionFilePath, TextureProcessingConfig.None,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureA, B),
				OutputTextureWAlphaChannelSource = new(TextureB, R)
			},
			TextureCreationConfig.ForColorTexture(name)
		);
	}

	TinyFfrAsyncOperation<Texture> LoadAbsorptionTransmissionMapAsync(ReadOnlySpan<char> filePath, bool invertAbsorption = false) {
		var includesTransmission = ReadTextureMetadata(filePath).IncludesAlphaChannel;
		if (!includesTransmission) return LoadAbsorptionTransmissionMapAsync(filePath, BuiltInTexturePaths.DefaultTransmissionMap, invertAbsorption);
		if (!invertAbsorption) return LoadTextureAsync(filePath, TextureCreationConfig.ForColorTexture(Path.GetFileName(filePath)));

		return LoadTextureAsync(
			filePath,
			TextureCreationConfig.ForColorTexture(Path.GetFileName(filePath)) with {
				ProcessingToApply = TextureProcessingConfig.Invert(includeAlphaChannel: false)
			}
		);
	}
	TinyFfrAsyncOperation<Texture> LoadAbsorptionTransmissionMapAsync(ReadOnlySpan<char> absorptionFilePath, ReadOnlySpan<char> transmissionFilePath, bool invertAbsorption = false) {
		var a = Path.GetFileName(absorptionFilePath);
		var b = Path.GetFileName(transmissionFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTextureAsync(
			absorptionFilePath, invertAbsorption ? TextureProcessingConfig.Invert(includeAlphaChannel: false) : TextureProcessingConfig.None,
			transmissionFilePath, TextureProcessingConfig.None,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureA, B),
				OutputTextureWAlphaChannelSource = new(TextureB, R)
			},
			TextureCreationConfig.ForColorTexture(name)
		);
	}
	#endregion

	#region Emissive
	Texture LoadEmissiveMap(ReadOnlySpan<char> filePath) {
		if (ReadTextureMetadata(filePath).IncludesAlphaChannel) return LoadTexture(filePath, TextureCreationConfig.ForColorTexture(Path.GetFileName(filePath)));
		else return LoadEmissiveMap(filePath, BuiltInTexturePaths.DefaultEmissiveIntensityMap);
	}
	Texture LoadEmissiveMap(ReadOnlySpan<char> emissiveColorFilePath, ReadOnlySpan<char> emissiveIntensityFilePath) {
		var a = Path.GetFileName(emissiveColorFilePath);
		var b = Path.GetFileName(emissiveIntensityFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTexture(
			emissiveColorFilePath,
			emissiveIntensityFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureA, B),
				OutputTextureWAlphaChannelSource = new(TextureB, R),
			},
			TextureCreationConfig.ForColorTexture(name)
		);
	}

	TinyFfrAsyncOperation<Texture> LoadEmissiveMapAsync(ReadOnlySpan<char> filePath) {
		if (ReadTextureMetadata(filePath).IncludesAlphaChannel) return LoadTextureAsync(filePath, TextureCreationConfig.ForColorTexture(Path.GetFileName(filePath)));
		else return LoadEmissiveMapAsync(filePath, BuiltInTexturePaths.DefaultEmissiveIntensityMap);
	}
	TinyFfrAsyncOperation<Texture> LoadEmissiveMapAsync(ReadOnlySpan<char> emissiveColorFilePath, ReadOnlySpan<char> emissiveIntensityFilePath) {
		var a = Path.GetFileName(emissiveColorFilePath);
		var b = Path.GetFileName(emissiveIntensityFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTextureAsync(
			emissiveColorFilePath,
			emissiveIntensityFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureA, B),
				OutputTextureWAlphaChannelSource = new(TextureB, R),
			},
			TextureCreationConfig.ForColorTexture(name)
		);
	}
	#endregion

	#region Anisotropy
	private sealed class RadialAngleAnisotropyArguments {
		public Orientation2D ZeroDirection { get; set; }
		public AnisotropyRadialAngleRange EncodedRange { get; set; }
		public bool EncodedAnticlockwise { get; set; }
		public ColorChannel? StrengthChannel { get; set; }
	}

	private static readonly Lock _staticMutationLock = new();
	private static readonly unsafe ArrayPoolBackedObjectPool<RadialAngleAnisotropyArguments> _radialAngleAnisotropyArgumentPool = new(&CreateRadialAngleAnisotropyArguments);

	private static RadialAngleAnisotropyArguments CreateRadialAngleAnisotropyArguments() => new();

	private static RadialAngleAnisotropyArguments RentRadialAngleAnisotropyArguments(Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, ColorChannel? strengthChannel) {
		RadialAngleAnisotropyArguments result;
		lock (_staticMutationLock) result = _radialAngleAnisotropyArgumentPool.Rent();
		result.ZeroDirection = zeroDirection;
		result.EncodedRange = encodedRange;
		result.EncodedAnticlockwise = encodedAnticlockwise;
		result.StrengthChannel = strengthChannel;
		return result;
	}

	private static void ReturnRadialAngleAnisotropyArguments(RadialAngleAnisotropyArguments arguments) {
		lock (_staticMutationLock) _radialAngleAnisotropyArgumentPool.Return(arguments);
	}

	private static void ApplyRadialAngleAnisotropyConversionRgb24(Span<TexelRgb24> texels, object? argument) {
		var arguments = (RadialAngleAnisotropyArguments) argument!;
		try {
			ConvertRadialAngleToVectorFormatAnisotropy(texels, arguments.ZeroDirection, arguments.EncodedRange, arguments.EncodedAnticlockwise, arguments.StrengthChannel);
		}
		finally {
			ReturnRadialAngleAnisotropyArguments(arguments);
		}
	}

	private static void ApplyRadialAngleAnisotropyConversionRgba32(Span<TexelRgba32> texels, object? argument) {
		var arguments = (RadialAngleAnisotropyArguments) argument!;
		try {
			ConvertRadialAngleToVectorFormatAnisotropy(texels, arguments.ZeroDirection, arguments.EncodedRange, arguments.EncodedAnticlockwise, arguments.StrengthChannel);
		}
		finally {
			ReturnRadialAngleAnisotropyArguments(arguments);
		}
	}

	private static unsafe TextureProcessingConfig CreateRadialAngleAnisotropyProcessingConfig(bool includesAlphaChannel, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, ColorChannel? strengthChannel) {
		return new TextureProcessingConfig {
			PostProcessingFunction = includesAlphaChannel
				? TexelProcessingFunction.Create<TexelRgba32>(&ApplyRadialAngleAnisotropyConversionRgba32)
				: TexelProcessingFunction.Create<TexelRgb24>(&ApplyRadialAngleAnisotropyConversionRgb24),
			PostProcessingArgument = RentRadialAngleAnisotropyArguments(zeroDirection, encodedRange, encodedAnticlockwise, strengthChannel)
		};
	}

	private static TextureCombinationConfig RadialAngleAnisotropyCombinationConfig => new(
		TextureCombinationScalingStrategy.PixelUpscale,
		new TextureCombinationSource(TextureA, R),
		new TextureCombinationSource(TextureA, G),
		new TextureCombinationSource(TextureB, R)
	);
	
	Texture LoadAnisotropyMapVectorFormatted(ReadOnlySpan<char> filePath, ColorChannel? strengthChannel) {
		return strengthChannel switch {
			B => LoadTexture(filePath, TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath))),
			A => LoadTexture(filePath, TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath)) with { ProcessingToApply = TextureProcessingConfig.Swizzle(blueSource: A) }),
			_ => LoadAnisotropyMapVectorFormatted(filePath, BuiltInTexturePaths.DefaultAnisotropyStrengthMap)
		};
	}
	Texture LoadAnisotropyMapVectorFormatted(ReadOnlySpan<char> vectorFilePath, ReadOnlySpan<char> strengthFilePath) {
		var a = Path.GetFileName(vectorFilePath);
		var b = Path.GetFileName(strengthFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTexture(
			vectorFilePath,
			strengthFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureB, R),
			},
			TextureCreationConfig.ForDataTexture(name)
		);
	}

	TinyFfrAsyncOperation<Texture> LoadAnisotropyMapVectorFormattedAsync(ReadOnlySpan<char> filePath, ColorChannel? strengthChannel) {
		return strengthChannel switch {
			B => LoadTextureAsync(filePath, TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath))),
			A => LoadTextureAsync(filePath, TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath)) with { ProcessingToApply = TextureProcessingConfig.Swizzle(blueSource: A) }),
			_ => LoadAnisotropyMapVectorFormattedAsync(filePath, BuiltInTexturePaths.DefaultAnisotropyStrengthMap)
		};
	}
	TinyFfrAsyncOperation<Texture> LoadAnisotropyMapVectorFormattedAsync(ReadOnlySpan<char> vectorFilePath, ReadOnlySpan<char> strengthFilePath) {
		var a = Path.GetFileName(vectorFilePath);
		var b = Path.GetFileName(strengthFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTextureAsync(
			vectorFilePath,
			strengthFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureB, R),
			},
			TextureCreationConfig.ForDataTexture(name)
		);
	}

	static void ConvertRadialAngleToVectorFormatAnisotropy(Span<TexelRgb24> texels, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, ColorChannel? strengthChannel) {
		const float StrengthCoefficient = 1f / Byte.MaxValue;
		const float AngleCoefficientZeroTo180 = 0.5f / Byte.MaxValue;
		const float AngleCoefficientZeroTo360 = 1f / Byte.MaxValue;
		var angleAddition = Angle.From2DPolarAngle(zeroDirection) ?? Angle.Zero;
		var angleCoefficient = encodedRange == AnisotropyRadialAngleRange.ZeroTo180 ? AngleCoefficientZeroTo180 : AngleCoefficientZeroTo360;
		if (!encodedAnticlockwise) angleCoefficient *= -1f;

		if (strengthChannel is G or B) {
			for (var i = 0; i < texels.Length; ++i) {
				texels[i] = ITextureBuilder.CreateAnisotropyTexel(Angle.FromFullCircleFraction(texels[i].R * angleCoefficient) + angleAddition, texels[i][strengthChannel.Value] * StrengthCoefficient);
			}
		}
		else {
			for (var i = 0; i < texels.Length; ++i) {
				texels[i] = ITextureBuilder.CreateAnisotropyTexel(Angle.FromFullCircleFraction(texels[i].R * angleCoefficient) + angleAddition, ITextureBuilder.DefaultAnisotropyStrength);
			}
		}
	}
	static void ConvertRadialAngleToVectorFormatAnisotropy(Span<TexelRgba32> texels, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, ColorChannel? strengthChannel) {
		const float StrengthCoefficient = 1f / Byte.MaxValue;
		const float AngleCoefficientZeroTo180 = 0.5f / Byte.MaxValue;
		const float AngleCoefficientZeroTo360 = 1f / Byte.MaxValue;
		var angleAddition = Angle.From2DPolarAngle(zeroDirection) ?? Angle.Zero;
		var angleCoefficient = encodedRange == AnisotropyRadialAngleRange.ZeroTo180 ? AngleCoefficientZeroTo180 : AngleCoefficientZeroTo360;
		if (!encodedAnticlockwise) angleCoefficient *= -1f;

		if (strengthChannel is G or B or A) {
			for (var i = 0; i < texels.Length; ++i) {
				texels[i] = ITextureBuilder.CreateAnisotropyTexel(Angle.FromFullCircleFraction(texels[i].R * angleCoefficient) + angleAddition, texels[i][strengthChannel.Value] * StrengthCoefficient).ToRgba32();
			}
		}
		else {
			for (var i = 0; i < texels.Length; ++i) {
				texels[i] = ITextureBuilder.CreateAnisotropyTexel(Angle.FromFullCircleFraction(texels[i].R * angleCoefficient) + angleAddition, ITextureBuilder.DefaultAnisotropyStrength).ToRgba32();
			}
		}
	}
	Texture LoadAnisotropyMapRadialAngleFormatted(ReadOnlySpan<char> filePath, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, ColorChannel? strengthChannel) {
		var includesAlphaChannel = ReadTextureMetadata(filePath).IncludesAlphaChannel;
		return LoadTexture(
			filePath,
			TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath)) with {
				ProcessingToApply = CreateRadialAngleAnisotropyProcessingConfig(includesAlphaChannel, zeroDirection, encodedRange, encodedAnticlockwise, strengthChannel)
			},
			new TextureReadConfig { IncludeWAlphaChannel = includesAlphaChannel, ForceWAlphaChannelPresence = includesAlphaChannel }
		);
	}
	Texture LoadAnisotropyMapRadialAngleFormatted(ReadOnlySpan<char> radialAngleFilePath, ReadOnlySpan<char> strengthFilePath, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise) {
		var a = Path.GetFileName(radialAngleFilePath);
		var b = Path.GetFileName(strengthFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTexture(
			radialAngleFilePath,
			strengthFilePath,
			RadialAngleAnisotropyCombinationConfig,
			TextureCreationConfig.ForDataTexture(name) with {
				ProcessingToApply = CreateRadialAngleAnisotropyProcessingConfig(false, zeroDirection, encodedRange, encodedAnticlockwise, B)
			}
		);
	}

	TinyFfrAsyncOperation<Texture> LoadAnisotropyMapRadialAngleFormattedAsync(ReadOnlySpan<char> filePath, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, ColorChannel? strengthChannel) {
		var includesAlphaChannel = ReadTextureMetadata(filePath).IncludesAlphaChannel;
		return LoadTextureAsync(
			filePath,
			TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath)) with {
				ProcessingToApply = CreateRadialAngleAnisotropyProcessingConfig(includesAlphaChannel, zeroDirection, encodedRange, encodedAnticlockwise, strengthChannel)
			},
			new TextureReadConfig { IncludeWAlphaChannel = includesAlphaChannel, ForceWAlphaChannelPresence = includesAlphaChannel }
		);
	}
	TinyFfrAsyncOperation<Texture> LoadAnisotropyMapRadialAngleFormattedAsync(ReadOnlySpan<char> radialAngleFilePath, ReadOnlySpan<char> strengthFilePath, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise) {
		var a = Path.GetFileName(radialAngleFilePath);
		var b = Path.GetFileName(strengthFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTextureAsync(
			radialAngleFilePath,
			strengthFilePath,
			RadialAngleAnisotropyCombinationConfig,
			TextureCreationConfig.ForDataTexture(name) with {
				ProcessingToApply = CreateRadialAngleAnisotropyProcessingConfig(false, zeroDirection, encodedRange, encodedAnticlockwise, B)
			}
		);
	}
	#endregion

	#region Clearcoat
	Texture LoadClearCoatMap(ReadOnlySpan<char> filePath) => LoadTexture(filePath, TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath)));
	Texture LoadClearCoatMap(ReadOnlySpan<char> thicknessFilePath, ReadOnlySpan<char> roughnessFilePath) {
		var a = Path.GetFileName(thicknessFilePath);
		var b = Path.GetFileName(roughnessFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTexture(
			thicknessFilePath,
			roughnessFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureB, R),
				OutputTextureZBlueChannelSource = new(TextureA, B)
			},
			TextureCreationConfig.ForDataTexture(name)
		);
	}

	TinyFfrAsyncOperation<Texture> LoadClearCoatMapAsync(ReadOnlySpan<char> filePath) => LoadTextureAsync(filePath, TextureCreationConfig.ForDataTexture(Path.GetFileName(filePath)));
	TinyFfrAsyncOperation<Texture> LoadClearCoatMapAsync(ReadOnlySpan<char> thicknessFilePath, ReadOnlySpan<char> roughnessFilePath) {
		var a = Path.GetFileName(thicknessFilePath);
		var b = Path.GetFileName(roughnessFilePath);
		Span<char> name = stackalloc char[SpanUtils.GetConcatenatedLength(a, "+", b)];
		SpanUtils.Concatenate(name, a, "+", b);

		return LoadCombinedTextureAsync(
			thicknessFilePath,
			roughnessFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureB, R),
				OutputTextureZBlueChannelSource = new(TextureA, B)
			},
			TextureCreationConfig.ForDataTexture(name)
		);
	}
	#endregion
}
