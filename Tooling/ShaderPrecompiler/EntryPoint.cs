using Egodystonic.TinyFFR;

Console.Clear();

var solutionDir = Directory.GetCurrentDirectory();
while (!Path.GetFileName(solutionDir).Equals("Tooling")) {
	solutionDir	= Directory.GetParent(solutionDir)?.FullName ?? throw new ApplicationException("Can't find tooling dir");

}
solutionDir = Directory.GetParent(solutionDir)?.FullName ?? throw new ApplicationException("Can't find solution dir");
var matcLocation = Path.Combine(
	solutionDir, 
	"ThirdParty", "build_output", "filament", "Release", "tools", "matc", "Release", "matc.exe"
);
var shaderSourcesLocation = Path.Combine(
	solutionDir,
	"TinyFFR", "Assets", "Materials", "Local", "Shaders"
);
var compiledShadersLocation = Path.Combine(
	solutionDir,
	"TinyFFR", "Assets", "Materials", "Local", "Shaders", "CompiledObjects"
);

Console.WriteLine("Key directories:");
Console.WriteLine();
Console.WriteLine("\tmatc.exe");
Console.WriteLine("\t\t" + matcLocation);
Console.WriteLine("\t\tExists: " + File.Exists(matcLocation));
Console.WriteLine();
Console.WriteLine("\tTarget folder");
Console.WriteLine("\t\t" + compiledShadersLocation);
Console.WriteLine("\t\tExists: " + Directory.Exists(compiledShadersLocation));
Console.WriteLine();
Console.WriteLine("\tShaders");
Console.WriteLine("\t\t" + shaderSourcesLocation);
Console.WriteLine("\t\tExists: " + Directory.Exists(shaderSourcesLocation));

Console.WriteLine();
Console.WriteLine();
Console.WriteLine();
Console.WriteLine("Continue? (Y/n)");
var userSelectedContinue = false;
while (!userSelectedContinue) {
	switch (Console.ReadLine()?.ToLowerInvariant()) {
		case "y": userSelectedContinue = true; break;
		case "n": return;
	}
}
Console.WriteLine();
Console.WriteLine();

Console.WriteLine();
var shaderSourceFiles = Directory.GetFiles(shaderSourcesLocation, "shader_*.txt");
Console.WriteLine("Shader source files found:");
foreach (var sourceFile in shaderSourceFiles) {
	Console.WriteLine("\t" + Path.GetFileName(sourceFile));
}

Console.WriteLine();
var compiledObjectsToDelete = Directory.GetFiles(compiledShadersLocation, "*.zip");
Console.WriteLine("Previous compiled objects to be deleted:");
foreach (var objFile in compiledObjectsToDelete) {
	Console.WriteLine("\t" + Path.GetFileName(objFile));
}

Console.WriteLine();
Console.WriteLine();
Console.WriteLine();
Console.WriteLine("Continue? (Y/n)");
userSelectedContinue = false;
while (!userSelectedContinue) {
	switch (Console.ReadLine()?.ToLowerInvariant()) {
		case "y": userSelectedContinue = true; break;
		case "n": return;
	}
}
Console.WriteLine();

CompilerRunner.Execute(matcLocation, compiledShadersLocation, shaderSourceFiles, compiledObjectsToDelete);