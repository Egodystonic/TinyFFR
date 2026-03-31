// Created on 2026-03-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;
using NSubstitute;
using NSubstitute.Core.Arguments;

namespace Egodystonic.TinyFFR.Testing;

[TestFixture]
class MockResourceFactoryTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyProvideMockResources() {
		IMockAlphaResourceImplProvider mockImpl = new MockAlphaResourceImplProvider {
			OnGetNameAsNewStringObject = handle => handle.AsInteger.ToString()  
		};
		var mockResource = MockResourceFactory.Create(new ResourceHandle<MockResourceAlpha>((nuint) 1234UL), mockImpl);
		Assert.AreEqual(mockImpl, mockResource.Implementation);
		
		var windowImplProvider = Substitute.For<IWindowImplProvider>();
		var window = MockResourceFactory.Create(new ResourceHandle<Window>((nuint) 123UL), windowImplProvider);
		window.Size = new XYPair<int>(100, 200);
		windowImplProvider.Received(1).SetSize(new ResourceHandle<Window>((nuint) 123UL), new XYPair<int>(100, 200));
	}
}