using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Testing.Local.TestSetup;
using Egodystonic.TinyFFR.World;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Rendering;

namespace Egodystonic.TinyFFR.Testing.Local;

// This is a local development testing ground.
//	Configure the test in ConfigureTest().
//	The test then begins in StartTest().

// Anti-merge-issues:
//	Because this is a test ground for each developer, ideally before editing this file
//	you'd execute the following command in git to never push any changes up:
//
//	git update-index --skip-worktree Testing/LocalDevTesting/TestMain.cs
//	
//	After that you can modify this file as you wish.

static partial class TestMain {
	public static void ConfigureTest(TestBuilder builder) {
		// Set test configuration here by adjusting properties on the passed-in builder.
		// The values you set on builder.Context will be passed to StartTest() as the 'context' parameter.
		//	Every property on the context is optional.
		//		If you don't set any value for a property, a default resource will be created and passed to StartTest().
		//		If you explicitly set a property to null, no resource will be created.
		//			Some values depend on others; for example if you set "builder.Context.Factory = null;" no other resources will be created by default.
		//		You can use context properties to create others.
		//			For example: "builder.Context.Loop = builder.Context.Factory!.ApplicationLoopBuilder.CreateLoop();" is completely fine.
	}

	public static void StartTest(TestContext context) {
		// Write your test here.
		//	Once this method returns, the test will end and the application will quit.
		//	Calling BeginDefaultLoop starts a tick loop with additional FPS timing measurements printed to console.
		//		You can remove BeginDefaultLoop and Tick if you prefer to write your own loop.
		//		The Tick function passed to BeginDefaultLoop should return `true` to exit the loop.
		//		If you pass a CameraController to BeginDefaultLoop, it will be possible to control the camera with keyboard/mouse or gamepad using the default controller input mapping.
		
		BeginDefaultLoop(Tick, context.Loop, context.CameraController);
		bool Tick(float deltaTime) {
			// Write anything you like here to be executed once per frame.
			
			context.Renderer.Render();
			return context.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape);
		}
	}
}
