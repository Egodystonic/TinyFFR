using System.Diagnostics;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.Testing.Local.TestSetup;
using Egodystonic.TinyFFR.World;

Console.Clear();
CommonTestSupportFunctions.ResolveNativeAssembliesFromBuildOutputDir();
TestScaffold.Execute();
Console.WriteLine("Test finished with no exceptions.");