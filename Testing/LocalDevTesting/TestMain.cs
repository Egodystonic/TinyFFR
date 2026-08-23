using System.Diagnostics;
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
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Threading;

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
		builder.Context.Factory = new LocalTinyFfrFactory(assetLoaderConfig: new LocalAssetLoaderConfig() { MaxCachedTextMeshesPerFont = 64 }, rendererBuilderConfig: new RendererBuilderConfig { EnableVSync = false });
		builder.DefaultLoopSlowFrameReportingEnable = false;
	}

	public static void StartTest(TestContext context) {
		// Write your test here.
		//	Once this method returns, the test will end and the application will quit.
		//	Calling BeginDefaultLoop starts a tick loop with additional FPS timing measurements printed to console.
		//		You can remove BeginDefaultLoop and Tick if you prefer to write your own loop.
		//		The Tick function passed to BeginDefaultLoop should return `true` to exit the loop.
		//		If you pass a CameraController to BeginDefaultLoop, it will be possible to control the camera with keyboard/mouse or gamepad using the default controller input mapping.

		static string RandomStr() => Guid.NewGuid().ToString();
		
		var startTime = Stopwatch.GetTimestamp();
		var font = context.Factory.AssetLoader.LoadFont(BuiltInFont.Default);
		Console.WriteLine(Stopwatch.GetElapsedTime(startTime));
		var pen = font.CreatePen(BuiltInFontPenStyle.WhiteWithOutline);
		const int NumStrings = 30;
		
		List<string> storedStrings = new List<string>();
		List<CameraLockedTextInstance> texts = new();
		for (var i = 0; i < NumStrings; ++i) {
			var s = RandomStr();
			storedStrings.Add(s);
			texts.Add(context.Factory.ObjectBuilder.CreateCameraLockedTextInstance(pen, font.CreateString(s), Location.Random(Sphere.UnitSphere)));
			context.Scene.Add(texts[i]);
		}
		
		var fontOp = context.Factory.AssetLoader.LoadFontAsync(BuiltInFont.Monospace, "myfontname");
		var skyboxOp = context.Factory.AssetLoader.LoadBackdropTextureAsync(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr), BackdropTextureResolution.VeryHigh);
		
		BeginDefaultLoop(Tick, context.Loop, context.CameraController);
		bool Tick(float deltaTime) {
			// Write anything you like here to be executed once per frame.
			
			for (var i = 0; i < NumStrings; ++i) {
				var prevStr = texts[i].String;
				var newStr = font.CreateString(RandomStr());
				texts[i].SetString(newStr);
				texts[i].SetPen(pen);
				prevStr.Dispose();
			}
			
			if (skyboxOp.IsResultAvailable) {
				context.Scene.SetBackdrop(skyboxOp.GetResultAndDisposeOperation());
			}
			if (fontOp.IsResultAvailable) {
				font = fontOp.GetResultAndDisposeOperation();
				pen = font.CreatePen(BuiltInFontPenStyle.WhiteWithOutline);
			}
			// if (fontOp != null) {
			// 	Console.WriteLine(TinyFfrAsyncOperation.GetCompletionStats(fontOp.Value, fontOp2, fontOp3, fontOp4, fontOp5, fontOp6));
			// }
			// if (fontOp?.IsCompleted ?? false) {
			// 	font = fontOp.Value.GetResultAndDisposeOperation();
			// 	fontOp = null;
			// 	Console.WriteLine(Stopwatch.GetElapsedTime(startTime));
			// }
		
			context.Renderer.Render();
			return context.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape);
		}
	}
}
