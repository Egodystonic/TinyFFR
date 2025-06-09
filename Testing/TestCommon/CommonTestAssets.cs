using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Egodystonic.TinyFFR.Testing;

public static class CommonTestAssets {
	public const string AssetFolderName = "test_assets";
	static readonly Lock _staticMutationLock = new();
	static Dictionary<string, string>? _preMappedAssetDict = null;
	static string? _preFoundAssetFolder = null;

	[MemberNotNull(nameof(_preFoundAssetFolder), nameof(_preMappedAssetDict))]
	public static string FindAssetFolder() {
		lock (_staticMutationLock) {
			if (_preFoundAssetFolder != null && _preMappedAssetDict != null) return _preFoundAssetFolder;

			var curDir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
			while (curDir != null) {
				var containedDirectories = Directory.GetDirectories(curDir);
				if (containedDirectories.Any(d => Path.GetFileName(d)?.Equals(AssetFolderName, StringComparison.OrdinalIgnoreCase) ?? false)) {
					_preFoundAssetFolder = Path.Combine(curDir, AssetFolderName);
					break;
				}

				curDir = Directory.GetParent(curDir)?.FullName;
			}

			if (_preFoundAssetFolder == null) throw new InvalidOperationException($"Could not find {AssetFolderName}.");

			_preMappedAssetDict = new();
			foreach (var file in Directory.GetFiles(_preFoundAssetFolder)) {
				_preMappedAssetDict.Add(Path.GetFileName(file), file);
			}

			return _preFoundAssetFolder;
		} 
	}

	public static string FindAsset(string filename) {
		lock (_staticMutationLock) {
			if (_preMappedAssetDict == null) _ = FindAssetFolder();

			if (_preMappedAssetDict.TryGetValue(filename, out var fullFilePath)) return fullFilePath;

			throw new InvalidOperationException($"Asset '{filename}' does not exist in {AssetFolderName}.");
		}
	}

	public static string FindAsset(KnownTestAsset knownAsset) => FindAsset(knownAsset.GetFilename());
}