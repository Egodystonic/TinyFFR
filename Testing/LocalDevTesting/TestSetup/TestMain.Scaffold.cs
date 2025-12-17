using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Testing.Local.TestSetup;
using Egodystonic.TinyFFR.World;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

#pragma warning disable IDE0160 // ReSharper disable once CheckNamespace
namespace Egodystonic.TinyFFR.Testing.Local;
#pragma warning restore IDE0160

static partial class TestMain {
	static void BeginDefaultLoop(Func<float, bool> loopAction, ApplicationLoop loop, Camera autoCameraControlTarget) {
		TestScaffold.BeginDefaultLoop(loopAction, loop, autoCameraControlTarget);
	}
	static void BeginDefaultLoop(Func<float, bool> loopAction, ApplicationLoop loop) {
		TestScaffold.BeginDefaultLoop(loopAction, loop, null);
	}
}