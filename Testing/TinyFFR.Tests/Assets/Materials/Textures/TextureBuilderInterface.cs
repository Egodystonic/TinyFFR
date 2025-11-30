// Created on 2025-11-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Assets.Materials;

[TestFixture]
unsafe class TextureBuilderInterfaceTest {
	MockTextureBuilder _mtb;
	ITextureBuilder _tb;

	class MockTextureBuilder : ITextureBuilder {
		readonly HashSet<nuint> _allocatedBufferIds = new();
		public Action<object, TextureGenerationConfig, TextureCreationConfig>? CreateTextureAssertionAction = null;
		public Action<object, XYPair<int>, TextureProcessingConfig>? ProcessTextureAssertionAction = null;

		Texture ITextureBuilder.CreateTextureAndDisposePreallocatedBuffer<TTexel>(ITextureBuilder.PreallocatedBuffer<TTexel> preallocatedBuffer, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) {
			Assert.IsTrue(_allocatedBufferIds.Remove(preallocatedBuffer.BufferId));
			return CreateTexture(preallocatedBuffer.Span, in generationConfig, in config);
		}
		ITextureBuilder.PreallocatedBuffer<TTexel> ITextureBuilder.PreallocateBuffer<TTexel>(int texelCount) {
			var bufferId = (nuint) Random.Shared.Next();
			_allocatedBufferIds.Add(bufferId);
			return new ITextureBuilder.PreallocatedBuffer<TTexel>(
				bufferId,
				new TTexel[texelCount]
			);
		}
		public Texture CreateTexture<TTexel>(ReadOnlySpan<TTexel> texels, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
			Assert.NotNull(CreateTextureAssertionAction);
			CreateTextureAssertionAction(texels.ToArray(), generationConfig, config);
			CreateTextureAssertionAction = null;
			return new Texture(0, null!);
		}
		public void ProcessTexture<TTexel>(Span<TTexel> texels, XYPair<int> dimensions, in TextureProcessingConfig config) where TTexel : unmanaged, ITexel<TTexel> {
			Assert.NotNull(ProcessTextureAssertionAction);
			ProcessTextureAssertionAction(texels.ToArray(), dimensions, config);
			ProcessTextureAssertionAction = null;
		}
	}

	[SetUp]
	public void SetUpTest() {
		_mtb = new();
		_tb = _mtb;
	}

	[TearDown]
	public void TearDownTest() {
		Assert.IsNull(_mtb.CreateTextureAssertionAction, "Expected CreateTexture to be invoked, but it was not.");
		Assert.IsNull(_mtb.ProcessTextureAssertionAction, "Expected ProcessTexture to be invoked, but it was not.");
	}

	void AssertCreateTextureCall<TTexel>(Action<ReadOnlySpan<TTexel>, TextureGenerationConfig, TextureCreationConfig> assertionAction) {
		Assert.IsNull(_mtb.CreateTextureAssertionAction, "Expected CreateTexture to be invoked, but it was not.");
		_mtb.CreateTextureAssertionAction = (o, gc, cc) => {
			Assert.IsTrue(o is TTexel[]);
			assertionAction((TTexel[]) o, gc, cc);
		};
	}

	void AssertProcessTextureCall<TTexel>(Action<Span<TTexel>, XYPair<int>, TextureProcessingConfig> assertionAction) {
		Assert.IsNull(_mtb.ProcessTextureAssertionAction, "Expected ProcessTexture to be invoked, but it was not.");
		_mtb.ProcessTextureAssertionAction = (o, xy, pc) => {
			Assert.IsTrue(o is TTexel[]);
			assertionAction((TTexel[]) o, xy, pc);
		};
	}

	void AssertCreateTextureName<TTexel>(string name) {
		AssertCreateTextureCall<TTexel>((_, _, cc) => {
			Assert.AreEqual(name, cc.Name.ToString());
		});
	}

	[Test]
	public void ShouldCorrectlyPassThroughCreationArgs() {
		AssertCreateTextureCall<TexelRgb24>((texels, gc, cc) => {
			Assert.AreEqual(10, texels.Length);
			Assert.AreEqual(new XYPair<int>(3, 4), gc.Dimensions);
			Assert.AreEqual(false, cc.IsLinearColorspace);
			Assert.AreEqual(true, cc.GenerateMipMaps);
			Assert.AreEqual("abc", cc.Name.ToString());
		});

		_tb.CreateTexture(new TexelRgb24[10], dimensions: (3, 4), isLinearColorspace: false, generateMipMaps: true, name: "abc");
	}

	[Test]
	public void ShouldCorrectlyManageGeneralPatterns() {
		void AssertInvocation(ReadOnlySpan<TexelRgb24> texels, TextureGenerationConfig gc, TextureCreationConfig cc) {
			Assert.AreEqual(1, texels.Length);
			Assert.AreEqual(new TexelRgb24(1, 2, 3), texels[0]);
			Assert.AreEqual(new XYPair<int>(1, 1), gc.Dimensions);
			Assert.AreEqual(true, cc.IsLinearColorspace);
			Assert.AreEqual(false, cc.GenerateMipMaps);
			Assert.AreEqual(TextureProcessingConfig.None, cc.ProcessingToApply);
			Assert.IsTrue(cc.Name.IsEmpty);
		}

		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateTexture(TexturePattern.PlainFill(new TexelRgb24(1, 2, 3)), isLinearColorspace: true);
		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateTexture(TexturePattern.PlainFill(new TexelRgb24(1, 2, 3)), new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = true });
		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateTexture(new TexelRgb24(1, 2, 3), isLinearColorspace: true);
		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateTexture(new TexelRgb24(1, 2, 3), new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = true });

		AssertCreateTextureCall<TexelRgb24>((_, _, cc) => {
			Assert.AreEqual("abc", cc.Name.ToString());
			Assert.AreEqual(true, cc.IsLinearColorspace);
		});
		_tb.CreateTexture(TexturePattern.PlainFill(new TexelRgb24(1, 2, 3)), isLinearColorspace: true, name: "abc");
		AssertCreateTextureCall<TexelRgb24>((_, _, cc) => {
			Assert.AreEqual("abc", cc.Name.ToString());
			Assert.AreEqual(true, cc.IsLinearColorspace);
		});
		_tb.CreateTexture(new TexelRgb24(1, 2, 3), isLinearColorspace: true, name: "abc");

		AssertCreateTextureCall<TexelRgb24>((_, _, cc) => {
			Assert.AreEqual("abc", cc.Name.ToString());
			Assert.AreEqual(false, cc.IsLinearColorspace);
		});
		_tb.CreateTexture(TexturePattern.PlainFill(new TexelRgb24(1, 2, 3)), isLinearColorspace: false, name: "abc");
		AssertCreateTextureCall<TexelRgb24>((_, _, cc) => {
			Assert.AreEqual("abc", cc.Name.ToString());
			Assert.AreEqual(false, cc.IsLinearColorspace);
		});
		_tb.CreateTexture(new TexelRgb24(1, 2, 3), isLinearColorspace: false, name: "abc");
	}

	[Test]
	public void ShouldCorrectlyManageColorPatterns() {
		void AssertRgbInvocation(ReadOnlySpan<TexelRgb24> texels, TextureGenerationConfig gc, TextureCreationConfig cc) {
			Assert.AreEqual(1, texels.Length);
			Assert.AreEqual(TexelRgb24.ConvertFrom(new ColorVect(0.1f, 0.2f, 0.3f)), texels[0]);
			Assert.AreEqual(new XYPair<int>(1, 1), gc.Dimensions);
			Assert.AreEqual(false, cc.IsLinearColorspace);
			Assert.AreEqual(false, cc.GenerateMipMaps);
			Assert.AreEqual(TextureProcessingConfig.None, cc.ProcessingToApply);
			Assert.IsTrue(cc.Name.IsEmpty);
		}

		void AssertRgbaInvocation(ReadOnlySpan<TexelRgba32> texels, TextureGenerationConfig gc, TextureCreationConfig cc) {
			Assert.AreEqual(1, texels.Length);
			Assert.AreEqual(TexelRgba32.ConvertFrom(new ColorVect(0.1f, 0.2f, 0.3f, 0.4f)), texels[0]);
			Assert.AreEqual(new XYPair<int>(1, 1), gc.Dimensions);
			Assert.AreEqual(false, cc.IsLinearColorspace);
			Assert.AreEqual(false, cc.GenerateMipMaps);
			Assert.AreEqual(TextureProcessingConfig.None, cc.ProcessingToApply);
			Assert.IsTrue(cc.Name.IsEmpty);
		}

		AssertCreateTextureCall<TexelRgb24>(AssertRgbInvocation);
		_tb.CreateColorMap(TexturePattern.PlainFill(new ColorVect(0.1f, 0.2f, 0.3f)), includeAlpha: false);
		AssertCreateTextureCall<TexelRgb24>(AssertRgbInvocation);
		_tb.CreateColorMap(TexturePattern.PlainFill(new ColorVect(0.1f, 0.2f, 0.3f)), includeAlpha: false, new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = false });
		AssertCreateTextureCall<TexelRgb24>(AssertRgbInvocation);
		_tb.CreateColorMap(new ColorVect(0.1f, 0.2f, 0.3f), includeAlpha: false);
		AssertCreateTextureCall<TexelRgb24>(AssertRgbInvocation);
		_tb.CreateColorMap(new ColorVect(0.1f, 0.2f, 0.3f), includeAlpha: false, new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = false });

		AssertCreateTextureName<TexelRgb24>("abc");
		_tb.CreateColorMap(TexturePattern.PlainFill(new ColorVect(0.1f, 0.2f, 0.3f)), includeAlpha: false, name: "abc");
		AssertCreateTextureName<TexelRgb24>("abc");
		_tb.CreateColorMap(new ColorVect(0.1f, 0.2f, 0.3f), includeAlpha: false, name: "abc");

		AssertCreateTextureCall<TexelRgba32>(AssertRgbaInvocation);
		_tb.CreateColorMap(TexturePattern.PlainFill(new ColorVect(0.1f, 0.2f, 0.3f, 0.4f)), includeAlpha: true);
		AssertCreateTextureCall<TexelRgba32>(AssertRgbaInvocation);
		_tb.CreateColorMap(TexturePattern.PlainFill(new ColorVect(0.1f, 0.2f, 0.3f, 0.4f)), includeAlpha: true, new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = false });
		AssertCreateTextureCall<TexelRgba32>(AssertRgbaInvocation);
		_tb.CreateColorMap(new ColorVect(0.1f, 0.2f, 0.3f, 0.4f), includeAlpha: true);
		AssertCreateTextureCall<TexelRgba32>(AssertRgbaInvocation);
		_tb.CreateColorMap(new ColorVect(0.1f, 0.2f, 0.3f, 0.4f), includeAlpha: true, new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = false });

		AssertCreateTextureName<TexelRgba32>("abc");
		_tb.CreateColorMap(TexturePattern.PlainFill(new ColorVect(0.1f, 0.2f, 0.3f)), includeAlpha: true, name: "abc");
		AssertCreateTextureName<TexelRgba32>("abc");
		_tb.CreateColorMap(new ColorVect(0.1f, 0.2f, 0.3f), includeAlpha: true, name: "abc");
	}

	[Test]
	public void ShouldCorrectlyManageNormalPatterns() {
		void AssertInvocation(ReadOnlySpan<TexelRgb24> texels, TextureGenerationConfig gc, TextureCreationConfig cc) {
			Assert.AreEqual(1, texels.Length);
			Assert.AreEqual(new TexelRgb24(127, 255, 127), texels[0]);
			Assert.AreEqual(new XYPair<int>(1, 1), gc.Dimensions);
			Assert.AreEqual(true, cc.IsLinearColorspace);
			Assert.AreEqual(false, cc.GenerateMipMaps);
			Assert.AreEqual(TextureProcessingConfig.None, cc.ProcessingToApply);
			Assert.IsTrue(cc.Name.IsEmpty);
		}

		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateNormalMap(TexturePattern.PlainFill(new UnitSphericalCoordinate(90f, 90f)));
		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateNormalMap(TexturePattern.PlainFill(new UnitSphericalCoordinate(90f, 90f)), new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = true });
		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateNormalMap(new UnitSphericalCoordinate(90f, 90f));
		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateNormalMap(new UnitSphericalCoordinate(90f, 90f), new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = true });

		AssertCreateTextureName<TexelRgb24>("abc");
		_tb.CreateNormalMap(TexturePattern.PlainFill(new UnitSphericalCoordinate(90f, 90f)), name: "abc");
		AssertCreateTextureName<TexelRgb24>("abc");
		_tb.CreateNormalMap(new UnitSphericalCoordinate(90f, 90f), name: "abc");
	}

	[Test]
	public void ShouldCorrectlyManageOrmPatterns() {
		void AssertInvocation(ReadOnlySpan<TexelRgb24> texels, TextureGenerationConfig gc, TextureCreationConfig cc) {
			Assert.AreEqual(1, texels.Length);
			Assert.AreEqual(TexelRgb24.FromNormalizedFloats(0.2f, 0.4f, 0.6f), texels[0]);
			Assert.AreEqual(new XYPair<int>(1, 1), gc.Dimensions);
			Assert.AreEqual(true, cc.IsLinearColorspace);
			Assert.AreEqual(false, cc.GenerateMipMaps);
			Assert.AreEqual(TextureProcessingConfig.None, cc.ProcessingToApply);
			Assert.IsTrue(cc.Name.IsEmpty);
		}

		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateOcclusionRoughnessMetallicMap(TexturePattern.PlainFill<Real>(0.2f), TexturePattern.PlainFill<Real>(0.4f), TexturePattern.PlainFill<Real>(0.6f));
		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateOcclusionRoughnessMetallicMap(TexturePattern.PlainFill<Real>(0.2f), TexturePattern.PlainFill<Real>(0.4f), TexturePattern.PlainFill<Real>(0.6f), new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = true });
		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateOcclusionRoughnessMetallicMap(0.2f, 0.4f, 0.6f);
		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateOcclusionRoughnessMetallicMap(0.2f, 0.4f, 0.6f, new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = true });

		AssertCreateTextureName<TexelRgb24>("abc");
		_tb.CreateOcclusionRoughnessMetallicMap(TexturePattern.PlainFill<Real>(0.2f), TexturePattern.PlainFill<Real>(0.4f), TexturePattern.PlainFill<Real>(0.6f), name: "abc");
		AssertCreateTextureName<TexelRgb24>("abc");
		_tb.CreateOcclusionRoughnessMetallicMap(0.2f, 0.4f, 0.6f, name: "abc");
	}

	[Test]
	public void ShouldCorrectlyManageOrmrPatterns() {
		void AssertInvocation(ReadOnlySpan<TexelRgba32> texels, TextureGenerationConfig gc, TextureCreationConfig cc) {
			Assert.AreEqual(1, texels.Length);
			Assert.AreEqual(TexelRgba32.FromNormalizedFloats(0.2f, 0.4f, 0.6f, 0.8f), texels[0]);
			Assert.AreEqual(new XYPair<int>(1, 1), gc.Dimensions);
			Assert.AreEqual(true, cc.IsLinearColorspace);
			Assert.AreEqual(false, cc.GenerateMipMaps);
			Assert.AreEqual(TextureProcessingConfig.None, cc.ProcessingToApply);
			Assert.IsTrue(cc.Name.IsEmpty);
		}

		AssertCreateTextureCall<TexelRgba32>(AssertInvocation);
		_tb.CreateOcclusionRoughnessMetallicReflectanceMap(TexturePattern.PlainFill<Real>(0.2f), TexturePattern.PlainFill<Real>(0.4f), TexturePattern.PlainFill<Real>(0.6f), TexturePattern.PlainFill<Real>(0.8f));
		AssertCreateTextureCall<TexelRgba32>(AssertInvocation);
		_tb.CreateOcclusionRoughnessMetallicReflectanceMap(TexturePattern.PlainFill<Real>(0.2f), TexturePattern.PlainFill<Real>(0.4f), TexturePattern.PlainFill<Real>(0.6f), TexturePattern.PlainFill<Real>(0.8f), new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = true });
		AssertCreateTextureCall<TexelRgba32>(AssertInvocation);
		_tb.CreateOcclusionRoughnessMetallicReflectanceMap(0.2f, 0.4f, 0.6f, 0.8f);
		AssertCreateTextureCall<TexelRgba32>(AssertInvocation);
		_tb.CreateOcclusionRoughnessMetallicReflectanceMap(0.2f, 0.4f, 0.6f, 0.8f, new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = true });

		AssertCreateTextureName<TexelRgba32>("abc");
		_tb.CreateOcclusionRoughnessMetallicReflectanceMap(TexturePattern.PlainFill<Real>(0.2f), TexturePattern.PlainFill<Real>(0.4f), TexturePattern.PlainFill<Real>(0.6f), TexturePattern.PlainFill<Real>(0.8f), name: "abc");
		AssertCreateTextureName<TexelRgba32>("abc");
		_tb.CreateOcclusionRoughnessMetallicReflectanceMap(0.2f, 0.4f, 0.6f, 0.8f, name: "abc");
	}

	[Test]
	public void ShouldCorrectlyManageAbsorptionTransmissionPatterns() {
		void AssertInvocation(ReadOnlySpan<TexelRgba32> texels, TextureGenerationConfig gc, TextureCreationConfig cc) {
			Assert.AreEqual(1, texels.Length);
			Assert.AreEqual(new TexelRgba32(TexelRgb24.ConvertFrom(new ColorVect(0.2f, 0.4f, 0.6f)), 127), texels[0]);
			Assert.AreEqual(new XYPair<int>(1, 1), gc.Dimensions);
			Assert.AreEqual(false, cc.IsLinearColorspace);
			Assert.AreEqual(false, cc.GenerateMipMaps);
			Assert.AreEqual(TextureProcessingConfig.None, cc.ProcessingToApply);
			Assert.IsTrue(cc.Name.IsEmpty);
		}

		AssertCreateTextureCall<TexelRgba32>(AssertInvocation);
		_tb.CreateAbsorptionTransmissionMap(TexturePattern.PlainFill<ColorVect>(new(0.2f, 0.4f, 0.6f)), TexturePattern.PlainFill<Real>(0.5f));
		AssertCreateTextureCall<TexelRgba32>(AssertInvocation);
		_tb.CreateAbsorptionTransmissionMap(TexturePattern.PlainFill<ColorVect>(new(0.2f, 0.4f, 0.6f)), TexturePattern.PlainFill<Real>(0.5f), new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = false });
		AssertCreateTextureCall<TexelRgba32>(AssertInvocation);
		_tb.CreateAbsorptionTransmissionMap(new ColorVect(0.2f, 0.4f, 0.6f), 0.5f);
		AssertCreateTextureCall<TexelRgba32>(AssertInvocation);
		_tb.CreateAbsorptionTransmissionMap(new ColorVect(0.2f, 0.4f, 0.6f), 0.5f, new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = false });

		AssertCreateTextureName<TexelRgba32>("abc");
		_tb.CreateAbsorptionTransmissionMap(TexturePattern.PlainFill<ColorVect>(new(0.2f, 0.4f, 0.6f)), TexturePattern.PlainFill<Real>(0.5f), name: "abc");
		AssertCreateTextureName<TexelRgba32>("abc");
		_tb.CreateAbsorptionTransmissionMap(new ColorVect(0.2f, 0.4f, 0.6f), 0.5f, name: "abc");
	}

	[Test]
	public void ShouldCorrectlyManageEmissivePatterns() {
		void AssertInvocation(ReadOnlySpan<TexelRgba32> texels, TextureGenerationConfig gc, TextureCreationConfig cc) {
			Assert.AreEqual(1, texels.Length);
			Assert.AreEqual(new TexelRgba32(TexelRgb24.ConvertFrom(new ColorVect(0.2f, 0.4f, 0.6f)), 127), texels[0]);
			Assert.AreEqual(new XYPair<int>(1, 1), gc.Dimensions);
			Assert.AreEqual(false, cc.IsLinearColorspace);
			Assert.AreEqual(false, cc.GenerateMipMaps);
			Assert.AreEqual(TextureProcessingConfig.None, cc.ProcessingToApply);
			Assert.IsTrue(cc.Name.IsEmpty);
		}

		AssertCreateTextureCall<TexelRgba32>(AssertInvocation);
		_tb.CreateEmissiveMap(TexturePattern.PlainFill<ColorVect>(new(0.2f, 0.4f, 0.6f)), TexturePattern.PlainFill<Real>(0.5f));
		AssertCreateTextureCall<TexelRgba32>(AssertInvocation);
		_tb.CreateEmissiveMap(TexturePattern.PlainFill<ColorVect>(new(0.2f, 0.4f, 0.6f)), TexturePattern.PlainFill<Real>(0.5f), new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = false });
		AssertCreateTextureCall<TexelRgba32>(AssertInvocation);
		_tb.CreateEmissiveMap(new ColorVect(0.2f, 0.4f, 0.6f), 0.5f);
		AssertCreateTextureCall<TexelRgba32>(AssertInvocation);
		_tb.CreateEmissiveMap(new ColorVect(0.2f, 0.4f, 0.6f), 0.5f, new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = false });

		AssertCreateTextureName<TexelRgba32>("abc");
		_tb.CreateEmissiveMap(TexturePattern.PlainFill<ColorVect>(new(0.2f, 0.4f, 0.6f)), TexturePattern.PlainFill<Real>(0.5f), name: "abc");
		AssertCreateTextureName<TexelRgba32>("abc");
		_tb.CreateEmissiveMap(new ColorVect(0.2f, 0.4f, 0.6f), 0.5f, name: "abc");
	}

	[Test]
	public void ShouldCorrectlyManageAnisotropyPatterns() {
		void AssertInvocation(ReadOnlySpan<TexelRgb24> texels, TextureGenerationConfig gc, TextureCreationConfig cc) {
			Assert.AreEqual(1, texels.Length);
			Assert.AreEqual(new TexelRgb24(0, 127, 25), texels[0]);
			Assert.AreEqual(new XYPair<int>(1, 1), gc.Dimensions);
			Assert.AreEqual(true, cc.IsLinearColorspace);
			Assert.AreEqual(false, cc.GenerateMipMaps);
			Assert.AreEqual(TextureProcessingConfig.None, cc.ProcessingToApply);
			Assert.IsTrue(cc.Name.IsEmpty);
		}

		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateAnisotropyMap(TexturePattern.PlainFill<Angle>(180f), TexturePattern.PlainFill<Real>(0.1f));
		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateAnisotropyMap(TexturePattern.PlainFill<Angle>(180f), TexturePattern.PlainFill<Real>(0.1f), new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = true });
		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateAnisotropyMap(new Angle(180f), 0.1f);
		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateAnisotropyMap(new Angle(180f), 0.1f, new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = true });

		AssertCreateTextureName<TexelRgb24>("abc");
		_tb.CreateAnisotropyMap(TexturePattern.PlainFill<Angle>(180f), TexturePattern.PlainFill<Real>(0.1f), name: "abc");
		AssertCreateTextureName<TexelRgb24>("abc");
		_tb.CreateAnisotropyMap(new Angle(180f), 0.1f, name: "abc");
	}

	[Test]
	public void ShouldCorrectlyManageClearCoatPatterns() {
		void AssertInvocation(ReadOnlySpan<TexelRgb24> texels, TextureGenerationConfig gc, TextureCreationConfig cc) {
			Assert.AreEqual(1, texels.Length);
			Assert.AreEqual(new TexelRgb24(25, 127, 0), texels[0]);
			Assert.AreEqual(new XYPair<int>(1, 1), gc.Dimensions);
			Assert.AreEqual(true, cc.IsLinearColorspace);
			Assert.AreEqual(false, cc.GenerateMipMaps);
			Assert.AreEqual(TextureProcessingConfig.None, cc.ProcessingToApply);
			Assert.IsTrue(cc.Name.IsEmpty);
		}

		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateClearCoatMap(TexturePattern.PlainFill<Real>(0.1f), TexturePattern.PlainFill<Real>(0.5f));
		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateClearCoatMap(TexturePattern.PlainFill<Real>(0.1f), TexturePattern.PlainFill<Real>(0.5f), new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = true });
		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateClearCoatMap(0.1f, 0.5f);
		AssertCreateTextureCall<TexelRgb24>(AssertInvocation);
		_tb.CreateClearCoatMap(0.1f, 0.5f, new TextureCreationConfig { GenerateMipMaps = false, IsLinearColorspace = true });

		AssertCreateTextureName<TexelRgb24>("abc");
		_tb.CreateClearCoatMap(TexturePattern.PlainFill<Real>(0.1f), TexturePattern.PlainFill<Real>(0.5f), name: "abc");
		AssertCreateTextureName<TexelRgb24>("abc");
		_tb.CreateClearCoatMap(0.1f, 0.5f, name: "abc");
	}
}