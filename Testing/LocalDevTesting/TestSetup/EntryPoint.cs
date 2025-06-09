using System.Diagnostics;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.Testing.Local.TestSetup;
using Egodystonic.TinyFFR.World;

CommonTestSupportFunctions.ResolveNativeAssembliesFromBuildOutputDir();
TestScaffold.SetUpStandardTestObjects();
TestScaffold.RunTestLoop();
Console.WriteLine("Test finished with no exceptions.");