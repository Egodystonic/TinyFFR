using System.Reflection;
using Egodystonic.TinyFFR.Factory.Local;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Testing.Local.TestSetup;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Testing.Local;

// This is a local development testing ground.
//	Configure the test in ConfigureTest().
//	Initialize any testing setup in StartTest().
//	Tick() will then be called every frame, unless you set options.UseDefaultLoop to false (in which case you can call it yourself manually or ignore it).
//	There are some default objects accessible via properties (see TestMain.Scaffold.cs).
//	Call ExitTest() to finish the test.
//	This file is ignored in git, so you can edit it freely without creating merge conflicts

static partial class TestMain {
	public static void ConfigureTest(TestOptions options) {
		
	}
}

static partial class TestMain {
	public static void StartTest() {
		
	}

	public static void Tick(float deltaTime, ILatestInputRetriever input) {

	}
}