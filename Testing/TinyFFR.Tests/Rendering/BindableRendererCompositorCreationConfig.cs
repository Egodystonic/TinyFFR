// Created on 2026-07-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.Rendering;

[TestFixture]
class BindableRendererCompositorCreationConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldUseExpectedDefaults() {
		var config = new BindableRendererCompositorCreationConfig();
		Assert.AreEqual(BindableRendererCreationConfig.DefaultDefaultBufferSize, config.DefaultBufferSize);
		Assert.AreEqual(true, config.Name.IsEmpty);
	}

	[Test]
	public void ShouldThrowForInvalidDefaultBufferSize() {
		Assert.Throws<ArgumentOutOfRangeException>(() => new BindableRendererCompositorCreationConfig { DefaultBufferSize = (0, 100) }.ThrowIfInvalid());
		Assert.Throws<ArgumentOutOfRangeException>(() => new BindableRendererCompositorCreationConfig { DefaultBufferSize = (100, 0) }.ThrowIfInvalid());
		Assert.Throws<ArgumentOutOfRangeException>(() => new BindableRendererCompositorCreationConfig { DefaultBufferSize = (-100, 100) }.ThrowIfInvalid());
		Assert.DoesNotThrow(() => new BindableRendererCompositorCreationConfig { DefaultBufferSize = (100, 100) }.ThrowIfInvalid());
		Assert.DoesNotThrow(() => new BindableRendererCompositorCreationConfig().ThrowIfInvalid());
	}
}
