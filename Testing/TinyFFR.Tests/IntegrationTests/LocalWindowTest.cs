// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalWindowTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory(windowBuilderConfig: new WindowBuilderConfig { MaxWindowTitleLength = 10 });

		var displayDiscoverer = factory.DisplayDiscoverer;
		foreach (var display in displayDiscoverer.All) {
			Console.WriteLine(display);
			Console.WriteLine($"\tHighest resolution mode: {display.HighestSupportedResolutionMode}");
			Console.WriteLine($"\tHighest refresh rate mode: {display.HighestSupportedRefreshRateMode}");
			Console.WriteLine($"\tAll modes:");
			foreach (var refreshRate in display.SupportedDisplayModes) {
				Console.WriteLine($"\t\t{refreshRate}");
			}

			Assert.AreEqual(
				display.SupportedDisplayModes.ToArray().OrderByDescending(mode => mode.Resolution.ToVector2().LengthSquared()).ThenByDescending(mode => mode.RefreshRateHz).First(),
				display.HighestSupportedResolutionMode
			);
			Assert.AreEqual(
				display.SupportedDisplayModes.ToArray().OrderByDescending(mode => mode.RefreshRateHz).ThenByDescending(mode => mode.Resolution.ToVector2().LengthSquared()).First(),
				display.HighestSupportedRefreshRateMode
			);
		}

		var windowBuilder = factory.WindowBuilder;
		using var window = windowBuilder.CreateWindow(new() {
			Display = displayDiscoverer.Primary!.Value,
			Size = (500, 300),
			Position = (100, 100)
		});
		window.SetTitle("Test 456");

		Assert.AreEqual("Test 456", window.GetTitleAsNewStringObject());
		window.SetTitle("Test 123");
		Assert.AreEqual("Test 123", window.GetTitleAsNewStringObject());
		window.SetTitle("1234567890");
		Assert.AreEqual("1234567890", window.GetTitleAsNewStringObject());
		window.SetTitle("12345678901");
		Assert.AreEqual("1234567890", window.GetTitleAsNewStringObject());
		window.SetTitle("1234567890123");
		Assert.AreEqual("1234567890", window.GetTitleAsNewStringObject());
		
		Assert.AreEqual(new XYPair<int>(500, 300), window.Size);
		Assert.AreEqual(new XYPair<int>(100, 100), window.Position);
		 
		window.Size = (100, 800);
		window.Position = (400, 70);
		Assert.Throws<ArgumentOutOfRangeException>(() => window.Size = (-1, -1));
		Assert.Throws<ArgumentOutOfRangeException>(() => window.Size = (-1, 100));
		Assert.Throws<ArgumentOutOfRangeException>(() => window.Size = (100, -1));
		Assert.AreEqual(new XYPair<int>(100, 800), window.Size);
		Assert.AreEqual(new XYPair<int>(400, 70), window.Position);
	}
}