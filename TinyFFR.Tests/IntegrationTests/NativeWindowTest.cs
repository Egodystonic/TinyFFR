// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Factory;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class NativeWindowTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new TffrFactory();
		using var wh = factory.GetWindowBuilder(new() {
			MaxWindowTitleLength = 10
		}).Build(new() {
			ScreenDimensions = (500, 500), 
			ScreenLocation = (100, 100),
			Title = "Test 456"
		});
		Assert.AreEqual("Test 456", wh.Title);
		wh.Title = "Test 123";
		Assert.AreEqual("Test 123", wh.Title);
		wh.Title = "1234567890";
		Assert.AreEqual("1234567890", wh.Title);
		wh.Title = "12345678901";
		Assert.AreEqual("1234567890", wh.Title);
		wh.Title = "1234567890123";
		Assert.AreEqual("1234567890", wh.Title);
		Span<char> tooSmallSpan = stackalloc char[5];
		Assert.AreEqual(5, wh.GetTitleUsingSpan(tooSmallSpan));
		Assert.AreEqual("12345", new String(tooSmallSpan));
		Span<char> oversizeSpan = stackalloc char[15];
		Assert.AreEqual(10, wh.GetTitleUsingSpan(oversizeSpan));
		Assert.AreEqual("1234567890", new String(oversizeSpan[..10]));

		Thread.Sleep(1000);
	}
}