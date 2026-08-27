using System.Diagnostics;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.Testing.Local.TestSetup;
using Egodystonic.TinyFFR.World;

Console.Clear();
CommonTestSupportFunctions.ResolveNativeAssembliesFromBuildOutputDir();
Egodystonic.TinyFFR.Testing.Local.DevTestingTypeLoadOrder.ForceSafeTypeLoadOrder();
if (args is ["vramprobe", ..]) {
	Egodystonic.TinyFFR.Testing.Local.VramProbe.Execute(args[1..]);
	return;
}
if (args is ["compositorprobe", ..]) {
	Egodystonic.TinyFFR.Testing.Local.CompositorProbe.Execute(args[1..]);
	return;
}
TestScaffold.Execute();
Console.WriteLine("Test finished with no exceptions.");