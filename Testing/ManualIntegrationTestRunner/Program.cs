using System.Globalization;
using System.Reflection;
using Egodystonic.TinyFFR;
using NUnit.Framework;

var tests = typeof(GlobalSetup).Assembly.GetTypes()
	.Where(t => t.GetCustomAttribute<ExplicitAttribute>() != null)
	.ToArray();

Console.WriteLine("Select test to run (or 'q' to quit):");

for (var i = 0; i < tests.Length; ++i) {
	Console.WriteLine($"\t{i,7} {tests[i].Name}");
}

if (!Int32.TryParse(Console.ReadKey().KeyChar.ToString(), NumberStyles.Integer, CultureInfo.InstalledUICulture, out var index) || index < 0 || index >= tests.Length) return;

var testType = tests[index];

Console.WriteLine();
Console.WriteLine("=================================");
Console.WriteLine(testType.Name);
Console.WriteLine("=================================");
Console.WriteLine();

var testInstance = Activator.CreateInstance(testType);

Console.WriteLine($"> {nameof(GlobalSetup)}.{nameof(GlobalSetup.TestSetup)}()");
new GlobalSetup().TestSetup();
Console.WriteLine();

var setupMethod = testType.GetMethods().SingleOrDefault(m => m.GetCustomAttribute<SetUpAttribute>() != null);
if (setupMethod != null) {
	Console.WriteLine($"> {setupMethod.Name}()");
	setupMethod.Invoke(testInstance, Array.Empty<object>());
	Console.WriteLine();
}

var testMethods = testType.GetMethods().Where(m => m.GetCustomAttribute<TestAttribute>() != null).ToArray();
foreach (var testMethod in testMethods) {
	Console.WriteLine($"> {testMethod.Name}()");
	testMethod.Invoke(testInstance, Array.Empty<object>());
	Console.WriteLine();
}

var teardownMethod = testType.GetMethods().SingleOrDefault(m => m.GetCustomAttribute<TearDownAttribute>() != null);
if (teardownMethod != null) {
	Console.WriteLine($"> {teardownMethod.Name}()");
	teardownMethod.Invoke(testInstance, Array.Empty<object>());
	Console.WriteLine();
}

Console.WriteLine("Done!");
