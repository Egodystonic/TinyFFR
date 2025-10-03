// Created on 2024-01-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Reflection;
using Egodystonic.TinyFFR;

[assembly: InternalsVisibleTo("TinyFFR.Tests")]
[assembly: InternalsVisibleTo("TinyFFR.Integrations.Common")]
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
[assembly: AssemblyVersion(TffrAssemblyMetadata.TffrVersion + ".0")]
[assembly: AssemblyFileVersion(TffrAssemblyMetadata.TffrVersion)]
[assembly: AssemblyInformationalVersion(TffrAssemblyMetadata.TffrVersion)]

namespace Egodystonic.TinyFFR;
static class TffrAssemblyMetadata {
	public const string TffrVersion = "0.4.0";
}