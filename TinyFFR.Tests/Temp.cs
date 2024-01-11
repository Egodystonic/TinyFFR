// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment;

namespace Egodystonic.TinyFFR;

[TestFixture]
class TempTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Should() {
		TffrInitializer.Init(); // TODO This global static state is gonna make this lib a nightmare to debug - we need a way to inject it etc. No global state. Let's rid ourselves of singletons/statics
		var w = Window.Create();
		Thread.Sleep(4000);
	}
}