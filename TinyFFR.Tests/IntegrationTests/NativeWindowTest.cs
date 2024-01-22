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
		using var window = factory.GetWindowBuilder(new() {
			MaxWindowTitleLength = 10
		}).Build(new() {
			Size = (500, 300), 
			Position = (100, 100),
			Title = "Test 456"
		});
		Assert.AreEqual("Test 456", window.Title);
		window.Title = "Test 123";
		Assert.AreEqual("Test 123", window.Title);
		window.Title = "1234567890";
		Assert.AreEqual("1234567890", window.Title);
		window.Title = "12345678901";
		Assert.AreEqual("1234567890", window.Title);
		window.Title = "1234567890123";
		Assert.AreEqual("1234567890", window.Title);
		Span<char> tooSmallSpan = stackalloc char[5];
		Assert.AreEqual(5, window.GetTitleUsingSpan(tooSmallSpan));
		Assert.AreEqual("12345", new String(tooSmallSpan));
		Span<char> oversizeSpan = stackalloc char[15];
		Assert.AreEqual(10, window.GetTitleUsingSpan(oversizeSpan));
		Assert.AreEqual("1234567890", new String(oversizeSpan[..10]));

		Assert.AreEqual(new XYPair(500, 300), window.Size);
		Assert.AreEqual(new XYPair(100, 100), window.Position);
		
		Thread.Sleep(500);
		window.Size = (100, 800);
		window.Position = (400, -50);
		Thread.Sleep(500);
		Assert.Throws<ArgumentOutOfRangeException>(() => window.Size = (-1, -1));
		Assert.Throws<ArgumentOutOfRangeException>(() => window.Size = (-1, 100));
		Assert.Throws<ArgumentOutOfRangeException>(() => window.Size = (100, -1));
		Assert.AreEqual(new XYPair(100, 800), window.Size);
		Assert.AreEqual(new XYPair(400, -50), window.Position);
	}
}