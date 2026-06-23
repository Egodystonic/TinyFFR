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
if (!File.Exists(matcLocation)) {
	matcLocation = Path.Combine(
		solutionDir, 
		"ThirdParty", "build_output", "filament", "Release", "tools", "matc", "matc.exe"
	);
}
if (!File.Exists(matcLocation)) {
	matcLocation = Path.Combine(
		solutionDir, 
		"ThirdParty", "build_output", "filament", "Release", "tools", "matc", "Release", "matc"
	);
}
if (!File.Exists(matcLocation)) {
	matcLocation = Path.Combine(
		solutionDir, 
		"ThirdParty", "build_output", "filament", "Release", "tools", "matc", "matc"
	);
}

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
Console.WriteLine("\tmatc");
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
var allShaderSourceFiles = Directory.GetFiles(shaderSourcesLocation, "shader_*.txt");
Console.WriteLine("Shader source files found:");
foreach (var sourceFile in allShaderSourceFiles) {
	Console.WriteLine("\t" + Path.GetFileName(sourceFile));
}

Console.WriteLine();
Console.WriteLine("Enter a filter string to only (re)compile matching shader file(s), or leave empty to compile all:");
var filter = Console.ReadLine()?.Trim();
var filterIsActive = !String.IsNullOrEmpty(filter);

var shaderSourceFiles = filterIsActive
	? allShaderSourceFiles.Where(f => Path.GetFileName(f).Contains(filter!, StringComparison.OrdinalIgnoreCase)).ToArray()
	: allShaderSourceFiles;

if (shaderSourceFiles.Length == 0) {
	Console.WriteLine();
	Console.WriteLine("No shader source files matched filter '" + filter + "'. Nothing to do.");
	return;
}

if (filterIsActive) {
	Console.WriteLine();
	Console.WriteLine("Shader source files matching filter '" + filter + "':");
	foreach (var sourceFile in shaderSourceFiles) {
		Console.WriteLine("\t" + Path.GetFileName(sourceFile));
	}
}

// Map each compiled object to the source file it was produced from (its longest matching name-start owner),
// so that when a filter is active we only delete objects that this run will also recreate.
static string DestinationNameStart(string sourceFile) => Path.GetFileNameWithoutExtension(sourceFile)["shader_".Length..];
var allSourceNameStarts = allShaderSourceFiles.Select(DestinationNameStart).ToArray();
var selectedSourceNameStarts = shaderSourceFiles.Select(DestinationNameStart).ToHashSet(StringComparer.OrdinalIgnoreCase);

bool ObjectIsOwnedBySelectedSource(string objFilePath) {
	var objName = Path.GetFileName(objFilePath);
	string? owner = null;
	foreach (var nameStart in allSourceNameStarts) {
		var isOwnedByNameStart = objName.StartsWith(nameStart + "_", StringComparison.OrdinalIgnoreCase)
								|| objName.StartsWith(nameStart + ".", StringComparison.OrdinalIgnoreCase);
		if (isOwnedByNameStart && (owner == null || nameStart.Length > owner.Length)) owner = nameStart;
	}
	return owner != null && selectedSourceNameStarts.Contains(owner);
}

Console.WriteLine();
var allCompiledObjects = Directory.GetFiles(compiledShadersLocation, "*.zip");
var compiledObjectsToDelete = filterIsActive
	? allCompiledObjects.Where(ObjectIsOwnedBySelectedSource).ToArray()
	: allCompiledObjects;
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