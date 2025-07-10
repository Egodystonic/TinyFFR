// Created on 2024-01-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Testing;
using NUnit.Framework.Constraints;
using NUnit.Framework.Internal;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("ManualIntegrationTestRunner")]

namespace Egodystonic.TinyFFR;

[SetUpFixture]
sealed class GlobalSetup {
	[OneTimeSetUp]
	public void TestSetup() {
		TestExecutionContext.CurrentContext.AddFormatter(_ => obj => (obj as IDescriptiveStringProvider)?.ToStringDescriptive() ?? obj.ToString()!);
		CommonTestSupportFunctions.ResolveNativeAssembliesFromBuildOutputDir();
	}
}