// Created on 2025-09-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Rendering;
using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.Assets.Materials;

[TestFixture]
unsafe class TextureConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }
	
	static void NoOpPostProcessingRgb24(Span<TexelRgb24> texels, object? argument) { }

	[Test]
	public void ProcessingConfigShouldCorrectlySetRequiresProcessingFlag() {
		Assert.AreEqual(false, new TextureProcessingConfig().FlipX);
		Assert.AreEqual(false, new TextureProcessingConfig().FlipY);
		Assert.AreEqual(false, new TextureProcessingConfig().InvertXRedChannel);
		Assert.AreEqual(false, new TextureProcessingConfig().InvertYGreenChannel);
		Assert.AreEqual(false, new TextureProcessingConfig().InvertZBlueChannel);
		Assert.AreEqual(false, new TextureProcessingConfig().InvertWAlphaChannel);
		Assert.AreEqual(ColorChannel.R, new TextureProcessingConfig().XRedFinalOutputSource);
		Assert.AreEqual(ColorChannel.G, new TextureProcessingConfig().YGreenFinalOutputSource);
		Assert.AreEqual(ColorChannel.B, new TextureProcessingConfig().ZBlueFinalOutputSource);
		Assert.AreEqual(ColorChannel.A, new TextureProcessingConfig().WAlphaFinalOutputSource);
		Assert.AreEqual(false, new TextureProcessingConfig().RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { FlipX = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { FlipX = true }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { FlipY = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { FlipY = true }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { InvertXRedChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertXRedChannel = true }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { InvertYGreenChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertYGreenChannel = true }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { InvertZBlueChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertZBlueChannel = true }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { InvertWAlphaChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertWAlphaChannel = true }.RequiresProcessing);
		
		Assert.AreEqual(false, new TextureProcessingConfig { MultiplyAlpha = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { MultiplyAlpha = true }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { XRedFinalOutputSource = ColorChannel.R }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { XRedFinalOutputSource = ColorChannel.G }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { YGreenFinalOutputSource = ColorChannel.G }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { YGreenFinalOutputSource = ColorChannel.B }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { ZBlueFinalOutputSource = ColorChannel.B }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { ZBlueFinalOutputSource = ColorChannel.A }.RequiresProcessing);

		Assert.AreEqual(false, new TextureProcessingConfig { WAlphaFinalOutputSource = ColorChannel.A }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { WAlphaFinalOutputSource = ColorChannel.R }.RequiresProcessing);

		Assert.AreEqual(
			false, 
			new TextureProcessingConfig {
				FlipX = false,
				FlipY = false,
				InvertXRedChannel = false,
				InvertYGreenChannel = false,
				InvertZBlueChannel = false,
				InvertWAlphaChannel = false,
				MultiplyAlpha = false,
				XRedFinalOutputSource = ColorChannel.R,
				YGreenFinalOutputSource = ColorChannel.G,
				ZBlueFinalOutputSource = ColorChannel.B,
				WAlphaFinalOutputSource = ColorChannel.A,
			}.RequiresProcessing
		);

		Assert.AreEqual(true, new TextureProcessingConfig { FlipX = true, FlipY = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { FlipX = true, FlipY = true }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { FlipY = true, FlipX = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { FlipY = true, FlipX = true }.RequiresProcessing);

		Assert.AreEqual(true, new TextureProcessingConfig { InvertXRedChannel = true, InvertYGreenChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertXRedChannel = true, InvertYGreenChannel = true }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertYGreenChannel = true, InvertXRedChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertYGreenChannel = true, InvertXRedChannel = true }.RequiresProcessing);

		Assert.AreEqual(true, new TextureProcessingConfig { InvertZBlueChannel = true, InvertWAlphaChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertZBlueChannel = true, InvertWAlphaChannel = true }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertWAlphaChannel = true, InvertZBlueChannel = false }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { InvertWAlphaChannel = true, InvertZBlueChannel = true }.RequiresProcessing);

		Assert.AreEqual(true, new TextureProcessingConfig { XRedFinalOutputSource = ColorChannel.G, YGreenFinalOutputSource = ColorChannel.G }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { XRedFinalOutputSource = ColorChannel.G, YGreenFinalOutputSource = ColorChannel.B }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { YGreenFinalOutputSource = ColorChannel.B, XRedFinalOutputSource = ColorChannel.R }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { YGreenFinalOutputSource = ColorChannel.B, XRedFinalOutputSource = ColorChannel.G }.RequiresProcessing);

		Assert.AreEqual(true, new TextureProcessingConfig { ZBlueFinalOutputSource = ColorChannel.A, WAlphaFinalOutputSource = ColorChannel.A }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { ZBlueFinalOutputSource = ColorChannel.A, WAlphaFinalOutputSource = ColorChannel.R }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { WAlphaFinalOutputSource = ColorChannel.R, ZBlueFinalOutputSource = ColorChannel.B }.RequiresProcessing);
		Assert.AreEqual(true, new TextureProcessingConfig { WAlphaFinalOutputSource = ColorChannel.R, ZBlueFinalOutputSource = ColorChannel.A }.RequiresProcessing);
		
		Assert.AreEqual(true, new TextureProcessingConfig { PostProcessingFunction = TexelProcessingFunction.Create<TexelRgb24>(&NoOpPostProcessingRgb24) }.RequiresProcessing);
	}

	[Test]
	public void ShouldCorrectlyConvertProcessingConfigToAndFromHeapStorageFormat() {
		var testConfigA = new TextureProcessingConfig {
			FlipX = false,
			FlipY = true,
			InvertXRedChannel = true,
			InvertYGreenChannel = false,
			InvertZBlueChannel = true,
			InvertWAlphaChannel = false,
			MultiplyAlpha = false,
			XRedFinalOutputSource = ColorChannel.R,
			YGreenFinalOutputSource = ColorChannel.G,
			ZBlueFinalOutputSource = ColorChannel.B,
			WAlphaFinalOutputSource = ColorChannel.A,
		};
		var testConfigB = new TextureProcessingConfig {
			FlipX = true,
			FlipY = false,
			InvertXRedChannel = false,
			InvertYGreenChannel = true,
			InvertZBlueChannel = false,
			InvertWAlphaChannel = true,
			MultiplyAlpha = true,
			XRedFinalOutputSource = ColorChannel.G,
			YGreenFinalOutputSource = ColorChannel.B,
			ZBlueFinalOutputSource = ColorChannel.A,
			WAlphaFinalOutputSource = ColorChannel.R,
		};

		void AssertConfigsMatch(TextureProcessingConfig expected, TextureProcessingConfig actual) {
			Assert.AreEqual(expected.FlipX, actual.FlipX);
			Assert.AreEqual(expected.FlipY, actual.FlipY);
			Assert.AreEqual(expected.InvertXRedChannel, actual.InvertXRedChannel);
			Assert.AreEqual(expected.InvertYGreenChannel, actual.InvertYGreenChannel);
			Assert.AreEqual(expected.InvertZBlueChannel, actual.InvertZBlueChannel);
			Assert.AreEqual(expected.InvertWAlphaChannel, actual.InvertWAlphaChannel);
			Assert.AreEqual(expected.MultiplyAlpha, actual.MultiplyAlpha);
			Assert.AreEqual(expected.XRedFinalOutputSource, actual.XRedFinalOutputSource);
			Assert.AreEqual(expected.YGreenFinalOutputSource, actual.YGreenFinalOutputSource);
			Assert.AreEqual(expected.ZBlueFinalOutputSource, actual.ZBlueFinalOutputSource);
			Assert.AreEqual(expected.WAlphaFinalOutputSource, actual.WAlphaFinalOutputSource);
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);

		AssertHeapSerializationWithObjects<TextureProcessingConfig>()
			.Bool(false)
			.Bool(true)
			.Bool(true)
			.Bool(false)
			.Bool(true)
			.Bool(false)
			.Bool(false)
			.Int((int) ColorChannel.R)
			.Int((int) ColorChannel.G)
			.Int((int) ColorChannel.B)
			.Int((int) ColorChannel.A)
			.Long(0L)
			.Long(0L)
			.Long(0L)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<TextureProcessingConfig>()
			.Bool(true)
			.Bool(false)
			.Bool(false)
			.Bool(true)
			.Bool(false)
			.Bool(true)
			.Bool(true)
			.Int((int) ColorChannel.G)
			.Int((int) ColorChannel.B)
			.Int((int) ColorChannel.A)
			.Int((int) ColorChannel.R)
			.Long(0L)
			.Long(0L)
			.Long(0L)
			.For(testConfigB);

		AssertPropertiesAccountedFor<TextureProcessingConfig>()
			.Including(nameof(TextureProcessingConfig.FlipX))
			.Including(nameof(TextureProcessingConfig.FlipY))
			.Including(nameof(TextureProcessingConfig.InvertXRedChannel))
			.Including(nameof(TextureProcessingConfig.InvertYGreenChannel))
			.Including(nameof(TextureProcessingConfig.InvertZBlueChannel))
			.Including(nameof(TextureProcessingConfig.InvertWAlphaChannel))
			.Including(nameof(TextureProcessingConfig.MultiplyAlpha))
			.Including(nameof(TextureProcessingConfig.XRedFinalOutputSource))
			.Including(nameof(TextureProcessingConfig.YGreenFinalOutputSource))
			.Including(nameof(TextureProcessingConfig.ZBlueFinalOutputSource))
			.Including(nameof(TextureProcessingConfig.WAlphaFinalOutputSource))
			.Including(nameof(TextureProcessingConfig.PostProcessingFunction))
			.Including(nameof(TextureProcessingConfig.PostProcessingArgument))
			.End();
		
		var argument = new object();
		var config = new TextureProcessingConfig {
			FlipY = true,
			PostProcessingFunction = TexelProcessingFunction.Create<TexelRgb24>(&NoOpPostProcessingRgb24),
			PostProcessingArgument = argument
		};

		var buffer = new byte[TextureProcessingConfig.GetHeapStorageFormattedLength(in config)];
		TextureProcessingConfig.AllocateAndConvertToHeapStorage(buffer, in config);
		try {
			var roundTripped = TextureProcessingConfig.ConvertFromAllocatedHeapStorage(buffer);
			Assert.AreEqual(config.PostProcessingFunction, roundTripped.PostProcessingFunction);
			Assert.That(roundTripped.PostProcessingArgument, Is.SameAs(argument));
			Assert.AreEqual(true, roundTripped.FlipY);
			Assert.AreEqual(true, roundTripped.RequiresProcessing);
		}
		finally {
			TextureProcessingConfig.DisposeAllocatedHeapStorage(buffer);
		}
	}

	[Test]
	public void CreationConfigShouldDisposeItsNestedProcessingConfigHeapStorage() {
		static (byte[] Buffer, WeakReference ArgumentRef) AllocateStorage() {
			var argument = new object();
			var config = TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, "nested") with {
				ProcessingToApply = new TextureProcessingConfig {
					PostProcessingFunction = TexelProcessingFunction.Create<TexelRgb24>(&NoOpPostProcessingRgb24),
					PostProcessingArgument = argument
				}
			};
			var buffer = new byte[TextureCreationConfig.GetHeapStorageFormattedLength(in config)];
			TextureCreationConfig.AllocateAndConvertToHeapStorage(buffer, in config);
			return (buffer, new WeakReference(argument));
		}

		var (buffer, argumentRef) = AllocateStorage();
		TextureCreationConfig.DisposeAllocatedHeapStorage(buffer);

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		Assert.AreEqual(false, argumentRef.IsAlive, "TextureCreationConfig did not dispose the heap storage of its nested ProcessingToApply.");
	}

	[Test]
	public void ShouldCorrectlyConvertReadConfigToAndFromHeapStorageFormat() {
		var testConfigA = new TextureReadConfig {
			IncludeWAlphaChannel = true,
			ForceWAlphaChannelPresence = true
		};
		var testConfigB = new TextureReadConfig {
			IncludeWAlphaChannel = false,
			ForceWAlphaChannelPresence = false
		};

		void AssertConfigsMatch(TextureReadConfig expected, TextureReadConfig actual) {
			Assert.AreEqual(expected.IncludeWAlphaChannel, actual.IncludeWAlphaChannel);
			Assert.AreEqual(expected.ForceWAlphaChannelPresence, actual.ForceWAlphaChannelPresence);
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);

		AssertHeapSerializationWithObjects<TextureReadConfig>()
			.Bool(true)
			.Bool(true)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<TextureReadConfig>()
			.Bool(false)
			.Bool(false)
			.For(testConfigB);

		AssertPropertiesAccountedFor<TextureReadConfig>()
			.Including(nameof(TextureReadConfig.IncludeWAlphaChannel))
			.Including(nameof(TextureReadConfig.ForceWAlphaChannelPresence))
			.End();
	}

	[Test]
	public void ReadConfigShouldRejectForcedAlphaWithoutIncludedAlpha() {
		Assert.Throws<InvalidOperationException>(() => new TextureReadConfig { IncludeWAlphaChannel = false, ForceWAlphaChannelPresence = true }.ThrowIfInvalid());

		Assert.DoesNotThrow(() => new TextureReadConfig { IncludeWAlphaChannel = true, ForceWAlphaChannelPresence = true }.ThrowIfInvalid());
		Assert.DoesNotThrow(() => new TextureReadConfig { IncludeWAlphaChannel = true, ForceWAlphaChannelPresence = false }.ThrowIfInvalid());
		Assert.DoesNotThrow(() => new TextureReadConfig { IncludeWAlphaChannel = false, ForceWAlphaChannelPresence = false }.ThrowIfInvalid());
		Assert.DoesNotThrow(() => new TextureReadConfig().ThrowIfInvalid());
	}

	[Test]
	public void ShouldCorrectlyConvertGenerationConfigToAndFromHeapStorageFormat() {
		var testConfigA = new TextureGenerationConfig {
			Dimensions = (100, 200)
		};
		var testConfigB = new TextureGenerationConfig {
			Dimensions = (1000, 2000)
		};

		void AssertConfigsMatch(TextureGenerationConfig expected, TextureGenerationConfig actual) {
			Assert.AreEqual(expected.Dimensions, actual.Dimensions);
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);

		AssertHeapSerializationWithObjects<TextureGenerationConfig>()
			.Obj(new XYPair<int>(100, 200))
			.For(testConfigA);

		AssertHeapSerializationWithObjects<TextureGenerationConfig>()
			.Obj(new XYPair<int>(1000, 2000))
			.For(testConfigB);

		AssertPropertiesAccountedFor<TextureGenerationConfig>()
			.Including(nameof(TextureGenerationConfig.Dimensions))
			.End();
	}

	[Test]
	public void ShouldCorrectlyConvertCreationConfigToAndFromHeapStorageFormat() {
		var testConfigA = new TextureCreationConfig {
			GenerateMipMaps = true,
			AllowsDynamicWrites = false,
			CompressionQuality = Quality.High,
			DataType = TextureDataType.LinearDataUnitVector,
			RenderingConfig = new(true, true, Quality.High),
			Name = "Aa Aa",
			ProcessingToApply = new TextureProcessingConfig {
				FlipX = false,
				FlipY = true,
				InvertXRedChannel = true,
				InvertYGreenChannel = false,
				InvertZBlueChannel = true,
				InvertWAlphaChannel = false,
				XRedFinalOutputSource = ColorChannel.R,
				YGreenFinalOutputSource = ColorChannel.G,
				ZBlueFinalOutputSource = ColorChannel.B,
				WAlphaFinalOutputSource = ColorChannel.A,
			}
		};
		var testConfigB = new TextureCreationConfig {
			GenerateMipMaps = false,
			AllowsDynamicWrites = true,
			CompressionQuality = null,
			DataType = TextureDataType.ColorSrgb,
			RenderingConfig = new(false, false, Quality.Low),
			Name = "BBBbbb",
			ProcessingToApply = new TextureProcessingConfig {
				FlipX = true,
				FlipY = false,
				InvertXRedChannel = false,
				InvertYGreenChannel = true,
				InvertZBlueChannel = false,
				InvertWAlphaChannel = true,
				XRedFinalOutputSource = ColorChannel.G,
				YGreenFinalOutputSource = ColorChannel.B,
				ZBlueFinalOutputSource = ColorChannel.A,
				WAlphaFinalOutputSource = ColorChannel.R,
			}
		};

		void AssertConfigsMatch(TextureCreationConfig expected, TextureCreationConfig actual) {
			Assert.AreEqual(expected.GenerateMipMaps, actual.GenerateMipMaps);
			Assert.AreEqual(expected.AllowsDynamicWrites, actual.AllowsDynamicWrites);
			Assert.AreEqual(expected.CompressionQuality, actual.CompressionQuality);
			Assert.AreEqual(expected.DataType, actual.DataType);
			Assert.AreEqual(expected.RenderingConfig, actual.RenderingConfig);
			Assert.AreEqual(expected.Name.ToString(), actual.Name.ToString());
			Assert.AreEqual(expected.ProcessingToApply, actual.ProcessingToApply);
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);

		AssertHeapSerializationWithObjects<TextureCreationConfig>()
			.Bool(true)
			.Bool(false)
			.Bool(true)
			.Int((int) Quality.High)
			.Int((int) TextureDataType.LinearDataUnitVector)
			.Bool(true)
			.Bool(true)
			.Int(1)
			.Float(testConfigA.RenderingConfig.AnisotropyLevel)
			.String("Aa Aa")
			.SubConfig(testConfigA.ProcessingToApply)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<TextureCreationConfig>()
			.Bool(false)
			.Bool(true)
			.Bool(false)
			.Int(0)
			.Int((int) TextureDataType.ColorSrgb)
			.Bool(false)
			.Bool(false)
			.Int(-1)
			.Float(testConfigB.RenderingConfig.AnisotropyLevel)
			.String("BBBbbb")
			.SubConfig(testConfigB.ProcessingToApply)
			.For(testConfigB);

		AssertPropertiesAccountedFor<TextureCreationConfig>()
			.Including(nameof(TextureCreationConfig.GenerateMipMaps))
			.Including(nameof(TextureCreationConfig.AllowsDynamicWrites))
			.Including(nameof(TextureCreationConfig.CompressionQuality))
			.Including(nameof(TextureCreationConfig.DataType))
			.Including(nameof(TextureCreationConfig.RenderingConfig))
			.Including(nameof(TextureCreationConfig.Name))
			.Including(nameof(TextureCreationConfig.ProcessingToApply))
			.End();
	}

	// Specific test for a specific hole/bug that I found
	[Test]
	public void CanvasTextureConfigShouldPreserveAnisotropyLevelAcrossHeapStorageRoundTrip() {
		var config = TextureCreationConfig.ForCanvasTexture("canvas");
		Assert.AreEqual(0f, config.RenderingConfig.AnisotropyLevel, "Precondition: ForCanvasTexture is expected to explicitly set AnisotropyLevel to 0.");

		AssertRoundTripHeapStorage(config, static (expected, actual) => {
			Assert.AreEqual(expected.RenderingConfig.AnisotropyLevel, actual.RenderingConfig.AnisotropyLevel);
			Assert.AreEqual(expected.RenderingConfig, actual.RenderingConfig);
		});
	}

	[Test]
	public void ShouldCorrectlyConvertCombinationConfigToAndFromHeapStorageFormat() {
		var testConfigA = new TextureCombinationConfig(
			TextureCombinationScalingStrategy.ExtendEdges,
			new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, ColorChannel.G),
			new TextureCombinationSource(TextureCombinationSourceTexture.TextureC, ColorChannel.B),
			new TextureCombinationSource(TextureCombinationSourceTexture.TextureD, ColorChannel.A),
			new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R)
		);
		var testConfigB = new TextureCombinationConfig(
			TextureCombinationScalingStrategy.PixelUpscale,
			new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
			new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.G),
			new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, ColorChannel.B)
		);

		void AssertConfigsMatch(TextureCombinationConfig expected, TextureCombinationConfig actual) {
			Assert.AreEqual(expected.ScalingStrategy, actual.ScalingStrategy);
			Assert.AreEqual(expected.OutputTextureXRedChannelSource, actual.OutputTextureXRedChannelSource);
			Assert.AreEqual(expected.OutputTextureYGreenChannelSource, actual.OutputTextureYGreenChannelSource);
			Assert.AreEqual(expected.OutputTextureZBlueChannelSource, actual.OutputTextureZBlueChannelSource);
			Assert.AreEqual(expected.OutputTextureWAlphaChannelSource, actual.OutputTextureWAlphaChannelSource);
		}

		AssertRoundTripHeapStorage(testConfigA, AssertConfigsMatch);
		AssertRoundTripHeapStorage(testConfigB, AssertConfigsMatch);
		Assert.IsNull(TextureCombinationConfig.ConvertFromAllocatedHeapStorage(SerializeToBytes(testConfigB)).OutputTextureWAlphaChannelSource);

		AssertPropertiesAccountedFor<TextureCombinationConfig>()
			.Including(nameof(TextureCombinationConfig.ScalingStrategy))
			.Including(nameof(TextureCombinationConfig.OutputTextureXRedChannelSource))
			.Including(nameof(TextureCombinationConfig.OutputTextureYGreenChannelSource))
			.Including(nameof(TextureCombinationConfig.OutputTextureZBlueChannelSource))
			.Including(nameof(TextureCombinationConfig.OutputTextureWAlphaChannelSource))
			.End();
	}

	static byte[] SerializeToBytes<T>(scoped in T config) where T : struct, IConfigStruct<T>, allows ref struct {
		var result = new byte[T.GetHeapStorageFormattedLength(in config)];
		T.AllocateAndConvertToHeapStorage(result, in config);
		return result;
	}

	[Test]
	public void ShouldCorrectlyConvertLoadConfigToAndFromHeapStorageFormat() {
		var testConfig = new TextureLoadConfig {
			CreationConfig = TextureCreationConfig.ForCanvasTexture("canvas load"),
			ReadConfig = new TextureReadConfig { IncludeWAlphaChannel = true, ForceWAlphaChannelPresence = true }
		};

		AssertRoundTripHeapStorage(testConfig, static (expected, actual) => {
			Assert.AreEqual(expected.CreationConfig.DataType, actual.CreationConfig.DataType);
			Assert.AreEqual(expected.CreationConfig.GenerateMipMaps, actual.CreationConfig.GenerateMipMaps);
			Assert.AreEqual(expected.CreationConfig.RenderingConfig, actual.CreationConfig.RenderingConfig);
			Assert.AreEqual(expected.CreationConfig.Name.ToString(), actual.CreationConfig.Name.ToString());
			Assert.AreEqual(expected.CreationConfig.ProcessingToApply, actual.CreationConfig.ProcessingToApply);
			Assert.AreEqual(expected.ReadConfig.IncludeWAlphaChannel, actual.ReadConfig.IncludeWAlphaChannel);
			Assert.AreEqual(expected.ReadConfig.ForceWAlphaChannelPresence, actual.ReadConfig.ForceWAlphaChannelPresence);
		});

		AssertPropertiesAccountedFor<TextureLoadConfig>()
			.Including(nameof(TextureLoadConfig.CreationConfig))
			.Including(nameof(TextureLoadConfig.ReadConfig))
			.End();
	}

	[Test]
	public void ShouldCorrectlyConvertCombinedLoadConfigToAndFromHeapStorageFormat() {
		var testConfig = new TextureCombinedLoadConfig {
			CreationConfig = TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, "combined load"),
			CombinationConfig = new TextureCombinationConfig(
				TextureCombinationScalingStrategy.PixelUpscale,
				new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
				new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, ColorChannel.R),
				new TextureCombinationSource(TextureCombinationSourceTexture.TextureC, ColorChannel.R),
				new TextureCombinationSource(TextureCombinationSourceTexture.TextureD, ColorChannel.R)
			),
			ProcessingConfigA = TextureProcessingConfig.Invert(includeAlphaChannel: false),
			ProcessingConfigB = TextureProcessingConfig.Flip(true, false),
			ProcessingConfigC = TextureProcessingConfig.Swizzle(blueSource: ColorChannel.A),
			ProcessingConfigD = TextureProcessingConfig.None,
			SourceCount = 4
		};

		AssertRoundTripHeapStorage(testConfig, static (expected, actual) => {
			Assert.AreEqual(expected.CreationConfig.Name.ToString(), actual.CreationConfig.Name.ToString());
			Assert.AreEqual(expected.CreationConfig.RenderingConfig, actual.CreationConfig.RenderingConfig);
			Assert.AreEqual(expected.CombinationConfig, actual.CombinationConfig);
			Assert.AreEqual(expected.ProcessingConfigA, actual.ProcessingConfigA);
			Assert.AreEqual(expected.ProcessingConfigB, actual.ProcessingConfigB);
			Assert.AreEqual(expected.ProcessingConfigC, actual.ProcessingConfigC);
			Assert.AreEqual(expected.ProcessingConfigD, actual.ProcessingConfigD);
			Assert.AreEqual(expected.SourceCount, actual.SourceCount);
		});

		AssertPropertiesAccountedFor<TextureCombinedLoadConfig>()
			.Including(nameof(TextureCombinedLoadConfig.CreationConfig))
			.Including(nameof(TextureCombinedLoadConfig.CombinationConfig))
			.Including(nameof(TextureCombinedLoadConfig.ProcessingConfigA))
			.Including(nameof(TextureCombinedLoadConfig.ProcessingConfigB))
			.Including(nameof(TextureCombinedLoadConfig.ProcessingConfigC))
			.Including(nameof(TextureCombinedLoadConfig.ProcessingConfigD))
			.Including(nameof(TextureCombinedLoadConfig.SourceCount))
			.End();
	}
}
