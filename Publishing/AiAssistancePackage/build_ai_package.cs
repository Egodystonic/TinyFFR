using System.Collections.Immutable;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

const string PackageDirName = "tinyffr";
string[] assemblyNames = ["TinyFFR", "TinyFFR.Integrations.Common", "TinyFFR.Avalonia", "TinyFFR.ImGui", "TinyFFR.Wpf", "TinyFFR.WinForms"];
var exampleFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".cs", ".csproj", ".axaml", ".md" };

var repoRoot = Path.GetFullPath(args.Length > 0 ? args[0] : "../..");
var outputRoot = Path.GetFullPath(args.Length > 1 ? args[1] : "build_output");
var templateDir = Path.Combine(repoRoot, "Publishing", "AiAssistancePackage");
var docsSourceDir = Path.Combine(repoRoot, "Documentation", "source");
var warnings = new List<string>();

var version = ReadVersion(Path.Combine(repoRoot, "Directory.Build.props"));
Console.WriteLine($"Building AI assistance package for TinyFFR {version}");
Console.WriteLine($"Repository root: {repoRoot}");

var packageDir = Path.Combine(outputRoot, PackageDirName);
if (Directory.Exists(packageDir)) Directory.Delete(packageDir, true);
Directory.CreateDirectory(packageDir);

var docPages = WriteDocs();
var apiFiles = WriteApi();
var examples = WriteExamples();

File.WriteAllText(Path.Combine(packageDir, "SKILL.md"), ReadTemplate("SKILL.md.template"));
File.WriteAllText(Path.Combine(packageDir, "AGENTS.md"), ReadTemplate("AGENTS.md.template"));
File.WriteAllText(Path.Combine(packageDir, "llms.txt"), ReadTemplate("llms.txt.template").Replace("{{INDEX}}", BuildLlmsIndex()));

var zipPath = Path.Combine(outputRoot, $"TinyFFR-AI-Assistance-{version}.zip");
if (File.Exists(zipPath)) File.Delete(zipPath);
ZipFile.CreateFromDirectory(packageDir, zipPath, CompressionLevel.Optimal, includeBaseDirectory: true);

Console.WriteLine();
Console.WriteLine($"Documentation pages: {docPages.Count}");
Console.WriteLine($"API namespaces: {apiFiles.Count}, types: {apiFiles.Sum(f => f.TypeCount)}, members: {apiFiles.Sum(f => f.MemberCount)}");
Console.WriteLine($"Example applications: {examples.Count}, files: {examples.Sum(e => e.FileCount)}");
Console.WriteLine($"Package: {zipPath} ({new FileInfo(zipPath).Length / 1024} KiB)");
Console.WriteLine(warnings.Count == 0 ? "No warnings." : $"{warnings.Count} warning(s); see above.");
return 0;

void Warn(string message) {
	warnings.Add(message);
	Console.WriteLine((Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" ? "::warning::" : "WARNING: ") + message);
}

string ReadTemplate(string fileName) => File.ReadAllText(Path.Combine(templateDir, fileName)).Replace("\r\n", "\n").Replace("{{VERSION}}", version);

static string ReadVersion(string propsPath) {
	var doc = XDocument.Load(propsPath);
	string Get(string name) => doc.Descendants().FirstOrDefault(e => e.Name.LocalName == name)?.Value.Trim() ?? "";
	return Get("TffrVersion") + Get("TffrVersionSuffix");
}

List<DocPage> WriteDocs() {
	var targetDir = Path.Combine(packageDir, "docs");
	var pages = new List<DocPage>();
	var sourceFiles = Directory.EnumerateFiles(docsSourceDir, "*.md", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToList();
	foreach (var file in sourceFiles) {
		var relativePath = Path.GetRelativePath(docsSourceDir, file).Replace('\\', '/');
		var text = File.ReadAllText(file).Replace("\r\n", "\n");
		var (title, description) = ReadFrontMatter(text);
		var targetPath = Path.Combine(targetDir, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
		File.WriteAllText(targetPath, MarkdownCleaner.Clean(text));
		pages.Add(new DocPage(relativePath, title ?? Path.GetFileNameWithoutExtension(file), description));
	}

	var glossarySource = Path.Combine(repoRoot, "Documentation", "includes", "acronym_definitions.md");
	if (File.Exists(glossarySource)) {
		var glossary = new StringBuilder("# Glossary\n\nAbbreviations used throughout the documentation.\n\n");
		foreach (var line in File.ReadAllLines(glossarySource)) {
			var match = Regex.Match(line, @"^\*\[(.+?)\]:\s*(.+)$");
			if (match.Success) glossary.Append($"* **{match.Groups[1].Value}**: {match.Groups[2].Value.Trim()}\n");
		}
		File.WriteAllText(Path.Combine(targetDir, "glossary.md"), glossary.ToString());
	}
	else Warn($"Glossary source not found: {glossarySource}");

	foreach (var page in pages) {
		var pagePath = Path.Combine(targetDir, page.RelativePath);
		foreach (var issue in MarkdownCleaner.Validate(File.ReadAllText(pagePath), Path.GetDirectoryName(pagePath)!)) Warn($"docs/{page.RelativePath}:{issue}");
	}
	return pages;
}

static (string? Title, string? Description) ReadFrontMatter(string text) {
	if (!text.StartsWith("---\n")) return (null, null);
	var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
	if (end < 0) return (null, null);
	string? title = null, description = null;
	foreach (var line in text[4..end].Split('\n')) {
		var colon = line.IndexOf(':');
		if (colon < 0) continue;
		var key = line[..colon].Trim();
		var value = line[(colon + 1)..].Trim().Trim('"', '\'');
		if (key == "title") title = value;
		else if (key == "description") description = value;
	}
	return (title, description);
}

List<ExampleInfo> WriteExamples() {
	var examplesRoot = Path.Combine(repoRoot, "Examples");
	var targetRoot = Path.Combine(packageDir, "examples");
	var result = new List<ExampleInfo>();
	Directory.CreateDirectory(targetRoot);
	var rootReadme = Path.Combine(examplesRoot, "README.md");
	if (File.Exists(rootReadme)) File.Copy(rootReadme, Path.Combine(targetRoot, "README.md"));
	foreach (var exampleDir in Directory.GetDirectories(examplesRoot).Order(StringComparer.Ordinal)) {
		var name = Path.GetFileName(exampleDir);
		var fileCount = 0;
		foreach (var file in Directory.EnumerateFiles(exampleDir, "*", SearchOption.AllDirectories)) {
			var relativePath = Path.GetRelativePath(exampleDir, file);
			var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (segments.Any(s => s.Equals("bin", StringComparison.OrdinalIgnoreCase) || s.Equals("obj", StringComparison.OrdinalIgnoreCase))) continue;
			if (!exampleFileExtensions.Contains(Path.GetExtension(file))) continue;
			var targetPath = Path.Combine(targetRoot, name, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
			File.Copy(file, targetPath);
			fileCount++;
		}
		var readmePath = Path.Combine(exampleDir, "README.md");
		string? description = null;
		if (File.Exists(readmePath)) description = File.ReadAllLines(readmePath).Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0 && !l.StartsWith('#'));
		else Warn($"Example '{name}' has no README.md");
		result.Add(new ExampleInfo(name, File.Exists(readmePath), description, fileCount));
	}
	return result;
}

List<ApiFileInfo> WriteApi() {
	var apiDir = Path.Combine(packageDir, "api");
	var xmlTargetDir = Path.Combine(apiDir, "xml");
	Directory.CreateDirectory(xmlTargetDir);
	var xmlMembers = new List<XmlMember>();
	var metadata = new Dictionary<string, MemberMetadata>(StringComparer.Ordinal);

	foreach (var assemblyName in assemblyNames) {
		var dir = assemblyName == "TinyFFR" ? Path.Combine(repoRoot, "build_output", "Release") : Path.Combine(repoRoot, "Integrations", assemblyName, "bin", "Release");
		var xmlPath = Path.Combine(dir, assemblyName + ".xml");
		var dllPath = Path.Combine(dir, assemblyName + ".dll");
		if (!File.Exists(xmlPath)) {
			Warn($"XML documentation not found, skipping {assemblyName}: {xmlPath}");
			continue;
		}
		File.Copy(xmlPath, Path.Combine(xmlTargetDir, assemblyName + ".xml"));
		var membersElement = XDocument.Load(xmlPath).Root?.Element("members");
		foreach (var member in membersElement?.Elements("member") ?? Enumerable.Empty<XElement>()) {
			var id = member.Attribute("name")?.Value;
			if (id is { Length: > 2 } && id[1] == ':') xmlMembers.Add(new XmlMember(assemblyName, id[0], id[2..], id, member));
		}
		if (File.Exists(dllPath)) MetadataScanner.Scan(dllPath, metadata);
		else Warn($"Assembly not found, signatures for {assemblyName} will be derived from documentation IDs only: {dllPath}");
	}

	var renderer = new ApiRenderer(metadata, xmlMembers.Where(m => m.Kind == 'T').Select(m => m.Body).ToHashSet(StringComparer.Ordinal));
	var namespaces = new SortedDictionary<string, SortedDictionary<string, ApiType>>(StringComparer.Ordinal);
	var hiddenCount = 0;
	var unmatchedIds = new List<string>();

	foreach (var member in xmlMembers) {
		if (member.Kind is not ('T' or 'M' or 'P' or 'F' or 'E')) continue;
		metadata.TryGetValue(member.Id, out var meta);
		var ownerTypeId = member.Kind == 'T' ? member.Body : meta?.OwnerTypeId ?? ApiRenderer.FallbackOwnerTypeId(member.Body);
		var ownerVisible = !metadata.TryGetValue("T:" + ownerTypeId, out var ownerMeta) || ownerMeta.IsVisible;
		var visible = meta?.IsVisible ?? (ownerVisible && !ApiRenderer.FallbackMemberName(member.Body).Contains('#'));
		if (!visible || !ownerVisible) {
			hiddenCount++;
			continue;
		}
		if (meta == null) unmatchedIds.Add(member.Id);
		var ns = renderer.NamespaceOf(ownerTypeId);
		if (!namespaces.TryGetValue(ns, out var types)) namespaces[ns] = types = new SortedDictionary<string, ApiType>(StringComparer.Ordinal);
		if (!types.TryGetValue(ownerTypeId, out var type)) types[ownerTypeId] = type = new ApiType(ownerTypeId);
		type.Assemblies.Add(member.Assembly);
		if (member.Kind == 'T') type.Doc = member;
		else type.Members.Add(member);
	}

	var result = new List<ApiFileInfo>();
	foreach (var (ns, types) in namespaces) {
		var fileName = (ns.Length == 0 ? "global" : ns) + ".md";
		var assemblies = types.Values.SelectMany(t => t.Assemblies).Distinct().Order(StringComparer.Ordinal).ToList();
		var content = renderer.RenderNamespace(ns, types.Values.OrderBy(t => renderer.TypeDisplayName(t.TypeId), StringComparer.Ordinal).ToList(), version, assemblies);
		File.WriteAllText(Path.Combine(apiDir, fileName), content);
		result.Add(new ApiFileInfo(ns, fileName, types.Count, types.Values.Sum(t => t.Members.Count)));
	}

	Console.WriteLine($"API members: {xmlMembers.Count} in XML, {hiddenCount} skipped as non-public, {unmatchedIds.Count} without assembly metadata (signature derived from documentation ID)");
	foreach (var id in unmatchedIds) Warn($"Documented member could not be matched to assembly metadata, its signature is approximate: {id}");
	return result;
}

string BuildLlmsIndex() {
	var sb = new StringBuilder();
	var pagesByPath = docPages.ToDictionary(p => p.RelativePath, StringComparer.Ordinal);
	var listed = new HashSet<string>(StringComparer.Ordinal);
	var sections = new List<(string Name, List<DocPage> Pages)>();
	var mkdocsPath = Path.Combine(repoRoot, "Documentation", "mkdocs.yml");

	foreach (var (section, path) in ReadNav(mkdocsPath)) {
		if (!pagesByPath.TryGetValue(path, out var page)) {
			Warn($"mkdocs.yml nav entry not found in documentation source: {path}");
			continue;
		}
		var name = section.Length == 0 ? "Overview" : section;
		var group = sections.FirstOrDefault(s => s.Name == name);
		if (group.Pages == null) sections.Add(group = (name, []));
		group.Pages.Add(page);
		listed.Add(path);
	}
	var unlisted = docPages.Where(p => !listed.Contains(p.RelativePath)).ToList();
	if (unlisted.Count > 0) sections.Add(("Other documentation pages", unlisted));

	foreach (var (name, pages) in sections) {
		sb.Append($"## {name}\n\n");
		if (name == "Overview") sb.Append("- [SKILL.md](SKILL.md): Overview of TinyFFR, how this package is organised, and the most important rules.\n");
		foreach (var page in pages) sb.Append($"- [{page.Title}](docs/{page.RelativePath})" + (page.Description is { Length: > 0 } d ? $": {d}" : "") + "\n");
		sb.Append('\n');
	}

	sb.Append("## API reference\n\n");
	foreach (var file in apiFiles) sb.Append($"- [{(file.Namespace.Length == 0 ? "(global namespace)" : file.Namespace)}](api/{file.FileName}): {file.TypeCount} types, {file.MemberCount} members\n");
	sb.Append('\n');

	sb.Append("## Example applications\n\n");
	foreach (var example in examples) {
		var link = example.HasReadme ? $"examples/{example.Name}/README.md" : $"examples/{example.Name}/";
		sb.Append($"- [{example.Name}]({link})" + (example.Description is { Length: > 0 } d ? $": {d}" : "") + "\n");
	}
	sb.Append('\n');

	sb.Append("## Optional\n\n");
	sb.Append("- [Glossary](docs/glossary.md): Abbreviations used throughout the documentation.\n");
	sb.Append("- [Raw XML documentation](api/xml/): The original XML documentation files the `api/` markdown files were generated from.\n");
	return sb.ToString().TrimEnd() + "\n";
}

static List<(string Section, string Path)> ReadNav(string mkdocsPath) {
	var result = new List<(string, string)>();
	if (!File.Exists(mkdocsPath)) return result;
	var lines = File.ReadAllLines(mkdocsPath);
	var start = Array.FindIndex(lines, l => l.TrimEnd() == "nav:");
	if (start < 0) return result;
	var stack = new List<(int Indent, string Name)>();
	for (var i = start + 1; i < lines.Length; i++) {
		var line = lines[i];
		if (line.Trim().Length == 0) continue;
		if (!char.IsWhiteSpace(line[0])) break;
		var match = Regex.Match(line, @"^(\s*)- (.+?)\s*$");
		if (!match.Success) continue;
		var indent = match.Groups[1].Value.Length;
		var value = match.Groups[2].Value;
		while (stack.Count > 0 && stack[^1].Indent >= indent) stack.RemoveAt(stack.Count - 1);
		if (value.EndsWith(':')) {
			stack.Add((indent, value[..^1].Trim()));
			continue;
		}
		var separator = value.IndexOf(": ", StringComparison.Ordinal);
		var path = separator >= 0 ? value[(separator + 2)..] : value;
		result.Add((string.Join(" / ", stack.Select(s => s.Name)), path.Trim().Trim('"', '\'')));
	}
	return result;
}

sealed record DocPage(string RelativePath, string Title, string? Description);
sealed record ExampleInfo(string Name, bool HasReadme, string? Description, int FileCount);
sealed record ApiFileInfo(string Namespace, string FileName, int TypeCount, int MemberCount);
sealed record XmlMember(string Assembly, char Kind, string Body, string Id, XElement Element);
sealed record MemberMetadata(string OwnerTypeId, string Namespace, bool IsVisible, string Signature, string DisplayName);

sealed class ApiType(string typeId) {
	public string TypeId { get; } = typeId;
	public XmlMember? Doc { get; set; }
	public List<XmlMember> Members { get; } = [];
	public HashSet<string> Assemblies { get; } = new(StringComparer.Ordinal);
}

static class MarkdownCleaner {
	static readonly Regex LinkedImage = new(@"\[!\[([^\]]*)\]\([^)]*\)(?:\{[^}]*\})?\]\([^)]*\)", RegexOptions.Compiled);
	static readonly Regex Image = new(@"!\[([^\]]*)\]\([^)]*\)(?:\{[^}]*\})?", RegexOptions.Compiled);
	static readonly Regex ImageLink = new(@"\[([^\]]+)\]\([^)\s]+\.(?:jpe?g|png|svg|gif|webp)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
	static readonly Regex DefIcon = new(@"<span class=""def-icon"">[^<]*</span>\s*", RegexOptions.Compiled);
	static readonly Regex Affirmative = new(@"<span class=""tffr-affirmative"">[^<]*</span>", RegexOptions.Compiled);
	static readonly Regex Negative = new(@"<span class=""tffr-negative"">[^<]*</span>", RegexOptions.Compiled);
	static readonly Regex ArrowIcon = new(@":(?:material-arrow-right|octicons-arrow-right-24):", RegexOptions.Compiled);
	static readonly Regex IconWithAttributes = new(@":[a-z0-9_]+(?:-[a-z0-9]+)*:\{[^}]*\}\s*", RegexOptions.Compiled);
	static readonly Regex Icon = new(@":(?:material|octicons|fontawesome|simple)-[a-z0-9-]+:\s?", RegexOptions.Compiled);
	static readonly Regex AttributeList = new(@"\s?\{ ?[:.][^}]*\}", RegexOptions.Compiled);
	static readonly Regex EmojiShortcode = new(@"(?<![\w:]):([a-z_]+):(?![\w{])", RegexOptions.Compiled);
	static readonly Regex InlineCode = new(@"`[^`]*`", RegexOptions.Compiled);
	static readonly Dictionary<string, string> Emoji = new(StringComparer.Ordinal) {
		["warning"] = "⚠", ["information_source"] = "ℹ", ["bulb"] = "💡", ["white_check_mark"] = "✓", ["x"] = "✗", ["chestnut"] = "",
	};
	static readonly Regex DivLine = new(@"^</?div\b[^>]*>$", RegexOptions.Compiled);

	static readonly Regex LeftoverImage = new(@"\]\([^)\s]+\.(?:jpe?g|png|svg|gif|webp)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
	static readonly Regex LeftoverHtml = new(@"<(?:span|div|img)\b", RegexOptions.Compiled);
	static readonly Regex RelativeMdLink = new(@"\]\(([^)\s#:]+\.md)(?:#[^)\s]*)?\)", RegexOptions.Compiled);

	public static string Clean(string text) {
		var result = new StringBuilder();
		var fence = new FenceTracker();
		var captionState = 0;
		var lastWasBlank = false;
		foreach (var rawLine in text.Split('\n')) {
			var line = rawLine;
			var trimmed = line.Trim();
			if (fence.Process(trimmed)) {
				Append(line);
				continue;
			}
			if (DivLine.IsMatch(trimmed)) continue;
			if (trimmed == "/// caption") {
				captionState = 1;
				continue;
			}
			if (trimmed == "///" && captionState > 0) {
				captionState = 0;
				continue;
			}
			line = CleanInline(line);
			if (captionState == 1 && line.Trim().Length > 0) {
				var indent = line[..(line.Length - line.TrimStart().Length)];
				line = indent + "*Caption:* " + line.Trim();
				captionState = 2;
			}
			Append(line);
		}
		return result.ToString().TrimEnd() + "\n";

		void Append(string line) {
			var isBlank = line.Trim().Length == 0;
			if (isBlank && lastWasBlank && !fence.InFence) return;
			result.Append(line.TrimEnd()).Append('\n');
			lastWasBlank = isBlank;
		}
	}

	static string CleanInline(string line) {
		line = LinkedImage.Replace(line, m => $"*(Image: {m.Groups[1].Value.Trim()})*");
		line = Image.Replace(line, m => $"*(Image: {m.Groups[1].Value.Trim()})*");
		line = ImageLink.Replace(line, "$1");
		line = DefIcon.Replace(line, "");
		line = Affirmative.Replace(line, "✓");
		line = Negative.Replace(line, "✗");
		line = ArrowIcon.Replace(line, "→");
		line = IconWithAttributes.Replace(line, "");
		line = Icon.Replace(line, "");
		line = AttributeList.Replace(line, "");
		line = EmojiShortcode.Replace(line, m => Emoji.TryGetValue(m.Groups[1].Value, out var replacement) ? replacement : m.Value);
		return line;
	}

	public static IEnumerable<string> Validate(string text, string pageDir) {
		var fence = new FenceTracker();
		var lines = text.Split('\n');
		for (var i = 0; i < lines.Length; i++) {
			var line = lines[i];
			if (fence.Process(line.Trim())) continue;
			var lineNumber = i + 1;
			if (LeftoverImage.IsMatch(line)) yield return $"{lineNumber}: leftover image reference: {Excerpt(line)}";
			if (Icon.IsMatch(line)) yield return $"{lineNumber}: leftover icon shortcode: {Excerpt(line)}";
			if (AttributeList.IsMatch(line)) yield return $"{lineNumber}: leftover attribute list: {Excerpt(line)}";
			if (LeftoverHtml.IsMatch(line)) yield return $"{lineNumber}: leftover HTML element: {Excerpt(line)}";
			if (EmojiShortcode.IsMatch(InlineCode.Replace(line, ""))) yield return $"{lineNumber}: leftover emoji shortcode: {Excerpt(line)}";
			foreach (Match link in RelativeMdLink.Matches(line)) {
				var target = Path.GetFullPath(Path.Combine(pageDir, link.Groups[1].Value));
				if (!File.Exists(target)) yield return $"{lineNumber}: broken link to '{link.Groups[1].Value}'";
			}
		}
	}

	static string Excerpt(string line) {
		var trimmed = line.Trim();
		return trimmed.Length <= 100 ? trimmed : trimmed[..100] + "...";
	}

	sealed class FenceTracker {
		string? _marker;
		public bool InFence => _marker != null;

		public bool Process(string trimmedLine) {
			if (_marker == null) {
				var match = Regex.Match(trimmedLine, @"^(`{3,}|~{3,})");
				if (!match.Success) return false;
				_marker = match.Groups[1].Value;
				return true;
			}
			if (trimmedLine.StartsWith(_marker, StringComparison.Ordinal) && trimmedLine.TrimEnd(_marker[0]).Length == 0) _marker = null;
			return true;
		}
	}
}

sealed class ApiRenderer(Dictionary<string, MemberMetadata> metadata, HashSet<string> xmlTypeIds) {
	static Dictionary<string, string> OperatorSymbols => MetadataScanner.OperatorSymbols;

	static readonly Dictionary<string, string> DocPrimitiveNames = new(StringComparer.Ordinal) {
		["System.Boolean"] = "bool", ["System.Char"] = "char", ["System.SByte"] = "sbyte", ["System.Byte"] = "byte",
		["System.Int16"] = "short", ["System.UInt16"] = "ushort", ["System.Int32"] = "int", ["System.UInt32"] = "uint",
		["System.Int64"] = "long", ["System.UInt64"] = "ulong", ["System.Single"] = "float", ["System.Double"] = "double",
		["System.Decimal"] = "decimal", ["System.String"] = "string", ["System.Object"] = "object", ["System.Void"] = "void",
		["System.IntPtr"] = "nint", ["System.UIntPtr"] = "nuint",
	};

	public string NamespaceOf(string typeId) {
		if (metadata.TryGetValue("T:" + typeId, out var meta)) return meta.Namespace;
		var candidate = typeId;
		do {
			var lastDot = candidate.LastIndexOf('.');
			if (lastDot < 0) return "";
			candidate = candidate[..lastDot];
		} while (xmlTypeIds.Contains(candidate) || metadata.ContainsKey("T:" + candidate));
		return candidate;
	}

	public string TypeDisplayName(string typeId) {
		if (metadata.TryGetValue("T:" + typeId, out var meta)) return meta.DisplayName;
		var ns = NamespaceOf(typeId);
		var relative = ns.Length > 0 && typeId.StartsWith(ns + ".", StringComparison.Ordinal) ? typeId[(ns.Length + 1)..] : typeId;
		return Regex.Replace(relative, @"`(\d+)", m => "<" + string.Join(", ", Enumerable.Range(1, int.Parse(m.Groups[1].Value)).Select(i => "T" + i)) + ">");
	}

	public static string FallbackOwnerTypeId(string body) {
		var withoutParams = StripParams(body);
		var lastDot = withoutParams.LastIndexOf('.');
		return lastDot < 0 ? withoutParams : withoutParams[..lastDot];
	}

	public static string FallbackMemberName(string body) {
		var withoutParams = StripParams(body);
		var lastDot = withoutParams.LastIndexOf('.');
		return lastDot < 0 ? withoutParams : withoutParams[(lastDot + 1)..];
	}

	static string StripParams(string body) {
		var end = body.IndexOfAny(['(', '~']);
		return end < 0 ? body : body[..end];
	}

	public string RenderNamespace(string ns, List<ApiType> types, string version, List<string> assemblies) {
		var sb = new StringBuilder();
		sb.Append($"# Namespace `{(ns.Length == 0 ? "(global)" : ns)}`\n\n");
		sb.Append($"API reference for TinyFFR {version}, generated from the XML documentation and compiled metadata of {string.Join(", ", assemblies)}. ");
		sb.Append("Types are listed alphabetically; each type lists its constructors, fields, properties, methods, operators and events. ");
		sb.Append("Where a signature could not be read from the compiled assembly it is derived from the documentation ID instead, and a `&` marks a by-reference (`in`, `ref` or `out`) parameter.\n\n");

		foreach (var type in types) {
			sb.Append($"## `{TypeDisplayName(type.TypeId)}`\n\n");
			var typeMeta = metadata.GetValueOrDefault("T:" + type.TypeId);
			var typeParams = TypeParamNames(type);
			sb.Append("```csharp\n").Append(typeMeta?.Signature ?? "type " + TypeDisplayName(type.TypeId)).Append("\n```\n\n");
			if (type.Doc != null) AppendDocBody(sb, type.Doc.Element);
			else sb.Append("*No type-level documentation.*\n\n");

			foreach (var member in type.Members.OrderBy(KindOrder).ThenBy(m => MemberHeading(m), StringComparer.Ordinal).ThenBy(m => m.Id, StringComparer.Ordinal)) {
				var memberMeta = metadata.GetValueOrDefault(member.Id);
				sb.Append($"### `{MemberHeading(member)}`\n\n");
				sb.Append("```csharp\n").Append(memberMeta?.Signature ?? FallbackSignature(member, typeParams)).Append("\n```\n\n");
				AppendDocBody(sb, member.Element);
			}
		}
		return TidyMarkdown(sb.ToString());
	}

	string[] TypeParamNames(ApiType type) => type.Doc?.Element.Elements("typeparam").Select(e => e.Attribute("name")?.Value ?? "T").ToArray() ?? [];

	static int KindOrder(XmlMember member) {
		var name = FallbackMemberName(member.Body);
		return member.Kind switch {
			'M' when name is "#ctor" or "#cctor" => 0,
			'F' => 1,
			'P' => 2,
			'M' when name.StartsWith("op_", StringComparison.Ordinal) => 4,
			'M' => 3,
			'E' => 5,
			_ => 6
		};
	}

	string MemberHeading(XmlMember member) {
		var name = FallbackMemberName(member.Body);
		var genericArity = Regex.Match(name, @"``(\d+)$");
		if (genericArity.Success) name = name[..genericArity.Index];
		if (name == "#ctor") return "Constructor";
		if (name == "#cctor") return "Static constructor";
		if (name is "op_Implicit" or "op_Explicit") {
			var tilde = member.Body.IndexOf('~');
			var target = tilde >= 0 ? FormatDocType(member.Body[(tilde + 1)..], [], []) : "?";
			return (name == "op_Implicit" ? "implicit operator " : "explicit operator ") + target;
		}
		if (OperatorSymbols.TryGetValue(name, out var symbol)) return "operator " + symbol;
		if (member.Kind == 'P' && member.Body.Contains('(')) return "this[]";
		return name.Replace('#', '.').Replace('{', '<').Replace('}', '>');
	}

	string FallbackSignature(XmlMember member, string[] typeParams) {
		var methodTypeParams = member.Element.Elements("typeparam").Select(e => e.Attribute("name")?.Value ?? "T").ToArray();
		var paramNames = member.Element.Elements("param").Select(e => e.Attribute("name")?.Value ?? "").ToArray();
		var heading = MemberHeading(member);
		var open = member.Body.IndexOf('(');
		if (open < 0) return member.Kind == 'E' ? "event " + heading : heading;
		var close = member.Body.LastIndexOf(')');
		var paramTypes = SplitTopLevel(member.Body[(open + 1)..close]).Select(t => FormatDocType(t, typeParams, methodTypeParams)).ToList();
		var parameters = paramTypes.Select((t, i) => paramNames.Length == paramTypes.Count ? $"{t} {paramNames[i]}" : t);
		var name = heading == "Constructor" ? TypeDisplayName(FallbackOwnerTypeId(member.Body)).Split('<')[0].Split('.')[^1] : heading;
		if (methodTypeParams.Length > 0) name += "<" + string.Join(", ", methodTypeParams) + ">";
		return heading == "this[]" ? $"this[{string.Join(", ", parameters)}]" : $"{name}({string.Join(", ", parameters)})";
	}

	static List<string> SplitTopLevel(string text) {
		var result = new List<string>();
		var depth = 0;
		var start = 0;
		for (var i = 0; i < text.Length; i++) {
			if (text[i] is '{' or '[') depth++;
			else if (text[i] is '}' or ']') depth--;
			else if (text[i] == ',' && depth == 0) {
				result.Add(text[start..i]);
				start = i + 1;
			}
		}
		if (text.Length > 0) result.Add(text[start..]);
		return result;
	}

	string FormatDocType(string docType, string[] typeParams, string[] methodParams) {
		if (docType.EndsWith('@')) return FormatDocType(docType[..^1], typeParams, methodParams) + "&";
		if (docType.EndsWith('*')) return FormatDocType(docType[..^1], typeParams, methodParams) + "*";
		if (docType.EndsWith(']')) {
			var open = docType.LastIndexOf('[');
			return FormatDocType(docType[..open], typeParams, methodParams) + "[" + new string(',', docType[open..].Count(c => c == ',')) + "]";
		}
		if (docType.StartsWith("``", StringComparison.Ordinal) && int.TryParse(docType[2..], out var methodIndex)) return methodIndex < methodParams.Length ? methodParams[methodIndex] : "TM" + methodIndex;
		if (docType.StartsWith('`') && int.TryParse(docType[1..], out var typeIndex)) return typeIndex < typeParams.Length ? typeParams[typeIndex] : "T" + typeIndex;
		var brace = docType.IndexOf('{');
		if (brace >= 0 && docType.EndsWith('}')) {
			var name = docType[..brace];
			var typeArgs = SplitTopLevel(docType[(brace + 1)..^1]).Select(a => FormatDocType(a, typeParams, methodParams)).ToList();
			if (name == "System.Nullable" && typeArgs.Count == 1) return typeArgs[0] + "?";
			return ShortDocTypeName(name) + "<" + string.Join(", ", typeArgs) + ">";
		}
		return ShortDocTypeName(docType);
	}

	string ShortDocTypeName(string name) {
		if (DocPrimitiveNames.TryGetValue(name, out var keyword)) return keyword;
		if (metadata.ContainsKey("T:" + name) || xmlTypeIds.Contains(name)) return TypeDisplayName(name);
		var lastDot = name.LastIndexOf('.');
		return Regex.Replace(lastDot < 0 ? name : name[(lastDot + 1)..], @"`\d+$", "");
	}

	string ShortCref(string cref) {
		if (cref.Length < 3 || cref[1] != ':') return cref;
		var kind = cref[0];
		var body = cref[2..];
		if (kind == '!') return body;
		if (kind is 'T' or 'N') return kind == 'N' ? body : TypeDisplayName(body);
		var owner = FallbackOwnerTypeId(body);
		var ownerName = TypeDisplayName(owner);
		var name = FallbackMemberName(body);
		name = Regex.Replace(name, @"``\d+$", "");
		if (name == "#ctor") return ownerName;
		if (OperatorSymbols.TryGetValue(name, out var symbol)) return $"{ownerName}.operator {symbol}";
		if (name is "op_Implicit" or "op_Explicit") return $"{ownerName}.{(name == "op_Implicit" ? "implicit" : "explicit")} operator";
		return $"{ownerName}.{name.Replace('#', '.')}" + (kind == 'M' ? "()" : "");
	}

	void AppendDocBody(StringBuilder sb, XElement element) {
		foreach (var summary in element.Elements("summary")) sb.Append(RenderInline(summary).Trim()).Append("\n\n");
		AppendNamedList(sb, element, "typeparam", "Type parameters");
		AppendNamedList(sb, element, "param", "Parameters");
		foreach (var value in element.Elements("value")) AppendLabelled(sb, "Value", RenderInline(value).Trim());
		foreach (var returns in element.Elements("returns")) AppendLabelled(sb, "Returns", RenderInline(returns).Trim());
		var exceptions = element.Elements("exception").ToList();
		if (exceptions.Count > 0) {
			sb.Append("**Exceptions:**\n\n");
			foreach (var exception in exceptions) sb.Append($"- `{ShortCref(exception.Attribute("cref")?.Value ?? "")}`: {RenderInline(exception).Trim()}\n");
			sb.Append('\n');
		}
		foreach (var remarks in element.Elements("remarks")) AppendLabelled(sb, "Remarks", RenderInline(remarks).Trim());
		foreach (var example in element.Elements("example")) AppendLabelled(sb, "Example", RenderInline(example).Trim());
		var seeAlso = element.Elements("seealso").Select(s => s.Attribute("cref") is { } cref ? $"`{ShortCref(cref.Value)}`" : RenderInline(s).Trim()).Where(s => s.Length > 0).ToList();
		if (seeAlso.Count > 0) sb.Append("**See also:** ").Append(string.Join(", ", seeAlso)).Append("\n\n");
	}

	void AppendNamedList(StringBuilder sb, XElement element, string tag, string label) {
		var entries = element.Elements(tag).ToList();
		if (entries.Count == 0) return;
		sb.Append($"**{label}:**\n\n");
		foreach (var entry in entries) sb.Append($"- `{entry.Attribute("name")?.Value}`: {RenderInline(entry).Trim()}\n");
		sb.Append('\n');
	}

	static void AppendLabelled(StringBuilder sb, string label, string text) {
		if (text.Length == 0) return;
		sb.Append(text.Contains('\n') ? $"**{label}:**\n\n{text}\n\n" : $"**{label}:** {text}\n\n");
	}

	string RenderInline(XElement element) {
		var sb = new StringBuilder();
		foreach (var node in element.Nodes()) RenderNode(sb, node);
		return sb.ToString();
	}

	void RenderNode(StringBuilder sb, XNode node) {
		if (node is XText text) {
			sb.Append(Regex.Replace(text.Value, @"\s+", " "));
			return;
		}
		if (node is not XElement e) return;
		switch (e.Name.LocalName) {
			case "see":
			case "seealso":
				var inner = RenderInline(e).Trim();
				if (e.Attribute("langword") is { } langword) sb.Append($"`{langword.Value}`");
				else if (e.Attribute("href") is { } href) sb.Append(inner.Length > 0 ? $"[{inner}]({href.Value})" : $"<{href.Value}>");
				else if (e.Attribute("cref") is { } cref) sb.Append(inner.Length > 0 ? inner : $"`{ShortCref(cref.Value)}`");
				else sb.Append(inner);
				break;
			case "a":
				sb.Append($"[{RenderInline(e).Trim()}]({e.Attribute("href")?.Value})");
				break;
			case "paramref":
			case "typeparamref":
				sb.Append($"`{e.Attribute("name")?.Value}`");
				break;
			case "c":
				sb.Append($"`{e.Value}`");
				break;
			case "code":
				sb.Append("\n\n```csharp\n").Append(Dedent(e.Value)).Append("\n```\n\n");
				break;
			case "para":
				sb.Append("\n\n").Append(RenderInline(e).Trim()).Append("\n\n");
				break;
			case "br":
				sb.Append('\n');
				break;
			case "b":
			case "strong":
				sb.Append($"**{RenderInline(e).Trim()}**");
				break;
			case "i":
			case "em":
				sb.Append($"*{RenderInline(e).Trim()}*");
				break;
			case "list":
				var numbered = e.Attribute("type")?.Value == "number";
				sb.Append("\n\n");
				var index = 1;
				foreach (var item in e.Elements("item")) {
					var term = item.Element("term");
					var description = item.Element("description");
					var body = term != null && description != null ? $"**{RenderInline(term).Trim()}**: {RenderInline(description).Trim()}"
						: description != null ? RenderInline(description).Trim()
						: RenderInline(item).Trim();
					sb.Append(numbered ? $"{index++}. " : "- ").Append(body).Append('\n');
				}
				sb.Append('\n');
				break;
			case "inheritdoc":
				break;
			default:
				sb.Append(RenderInline(e));
				break;
		}
	}

	static string Dedent(string code) {
		var lines = code.Replace("\r\n", "\n").Split('\n').ToList();
		while (lines.Count > 0 && lines[0].Trim().Length == 0) lines.RemoveAt(0);
		while (lines.Count > 0 && lines[^1].Trim().Length == 0) lines.RemoveAt(lines.Count - 1);
		var indent = lines.Where(l => l.Trim().Length > 0).Select(l => l.Length - l.TrimStart().Length).DefaultIfEmpty(0).Min();
		return string.Join("\n", lines.Select(l => l.Length >= indent ? l[indent..] : l.TrimStart()));
	}

	static string TidyMarkdown(string markdown) {
		var result = new StringBuilder();
		var inFence = false;
		var lastWasBlank = false;
		foreach (var rawLine in markdown.Split('\n')) {
			var isFenceLine = rawLine.TrimStart().StartsWith("```", StringComparison.Ordinal);
			var line = inFence && !isFenceLine ? rawLine.TrimEnd() : rawLine.Trim();
			if (isFenceLine) inFence = !inFence;
			var isBlank = line.Length == 0;
			if (isBlank && lastWasBlank && !inFence) continue;
			result.Append(line).Append('\n');
			lastWasBlank = isBlank;
		}
		return result.ToString().TrimEnd() + "\n";
	}
}

abstract record Sig;
sealed record PrimSig(PrimitiveTypeCode Code) : Sig;
sealed record NamedSig(string Namespace, string[] Names, ImmutableArray<Sig> Args, bool IsLocalDefinition, bool IsLocalVisible) : Sig {
	public string FullName => (Namespace.Length > 0 ? Namespace + "." : "") + string.Join(".", Names);
}
sealed record ArraySig(Sig Element, int Rank) : Sig;
sealed record ByRefSig(Sig Element) : Sig;
sealed record PointerSig(Sig Element) : Sig;
sealed record GenParamSig(bool IsMethod, int Index) : Sig;
sealed record ModifiedSig(Sig Unmodified, Sig Modifier, bool IsRequired) : Sig;
sealed record FnPtrSig(Sig Return, ImmutableArray<Sig> Parameters) : Sig;
sealed record DisplayContext(string[] TypeParams, string[] MethodParams);
sealed record ParamInfo(string Name, ParameterAttributes Flags, HashSet<string> Attributes, ConstantHandle DefaultValue);

sealed class SigProvider : ISignatureTypeProvider<Sig, object?> {
	public static readonly SigProvider Instance = new();

	public Sig GetPrimitiveType(PrimitiveTypeCode typeCode) => new PrimSig(typeCode);
	public Sig GetSZArrayType(Sig elementType) => new ArraySig(elementType, 0);
	public Sig GetArrayType(Sig elementType, ArrayShape shape) => new ArraySig(elementType, shape.Rank);
	public Sig GetByReferenceType(Sig elementType) => new ByRefSig(elementType);
	public Sig GetPointerType(Sig elementType) => new PointerSig(elementType);
	public Sig GetPinnedType(Sig elementType) => elementType;
	public Sig GetGenericMethodParameter(object? genericContext, int index) => new GenParamSig(true, index);
	public Sig GetGenericTypeParameter(object? genericContext, int index) => new GenParamSig(false, index);
	public Sig GetModifiedType(Sig modifier, Sig unmodifiedType, bool isRequired) => new ModifiedSig(unmodifiedType, modifier, isRequired);
	public Sig GetFunctionPointerType(MethodSignature<Sig> signature) => new FnPtrSig(signature.ReturnType, signature.ParameterTypes);
	public Sig GetGenericInstantiation(Sig genericType, ImmutableArray<Sig> typeArguments) => genericType is NamedSig named ? named with { Args = typeArguments } : genericType;
	public Sig GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

	public Sig GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) {
		var chain = MetadataScanner.DefinitionChain(reader, handle);
		return new NamedSig(reader.GetString(chain[0].Namespace), chain.Select(t => reader.GetString(t.Name)).ToArray(), [], true, chain.All(MetadataScanner.IsTypeVisible));
	}

	public Sig GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) {
		var names = new List<string>();
		var current = reader.GetTypeReference(handle);
		names.Add(reader.GetString(current.Name));
		while (current.ResolutionScope.Kind == HandleKind.TypeReference) {
			current = reader.GetTypeReference((TypeReferenceHandle)current.ResolutionScope);
			names.Insert(0, reader.GetString(current.Name));
		}
		return new NamedSig(reader.GetString(current.Namespace), names.ToArray(), [], false, true);
	}
}

static class MetadataScanner {
	static readonly Dictionary<PrimitiveTypeCode, (string Doc, string Keyword)> Primitives = new() {
		[PrimitiveTypeCode.Boolean] = ("System.Boolean", "bool"), [PrimitiveTypeCode.Char] = ("System.Char", "char"),
		[PrimitiveTypeCode.SByte] = ("System.SByte", "sbyte"), [PrimitiveTypeCode.Byte] = ("System.Byte", "byte"),
		[PrimitiveTypeCode.Int16] = ("System.Int16", "short"), [PrimitiveTypeCode.UInt16] = ("System.UInt16", "ushort"),
		[PrimitiveTypeCode.Int32] = ("System.Int32", "int"), [PrimitiveTypeCode.UInt32] = ("System.UInt32", "uint"),
		[PrimitiveTypeCode.Int64] = ("System.Int64", "long"), [PrimitiveTypeCode.UInt64] = ("System.UInt64", "ulong"),
		[PrimitiveTypeCode.Single] = ("System.Single", "float"), [PrimitiveTypeCode.Double] = ("System.Double", "double"),
		[PrimitiveTypeCode.String] = ("System.String", "string"), [PrimitiveTypeCode.Object] = ("System.Object", "object"),
		[PrimitiveTypeCode.Void] = ("System.Void", "void"), [PrimitiveTypeCode.IntPtr] = ("System.IntPtr", "nint"),
		[PrimitiveTypeCode.UIntPtr] = ("System.UIntPtr", "nuint"), [PrimitiveTypeCode.TypedReference] = ("System.TypedReference", "TypedReference"),
	};

	public static readonly Dictionary<string, string> OperatorSymbols = new(StringComparer.Ordinal) {
		["op_Addition"] = "+", ["op_Subtraction"] = "-", ["op_Multiply"] = "*", ["op_Division"] = "/", ["op_Modulus"] = "%",
		["op_UnaryNegation"] = "-", ["op_UnaryPlus"] = "+", ["op_Equality"] = "==", ["op_Inequality"] = "!=",
		["op_LessThan"] = "<", ["op_GreaterThan"] = ">", ["op_LessThanOrEqual"] = "<=", ["op_GreaterThanOrEqual"] = ">=",
		["op_RightShift"] = ">>", ["op_LeftShift"] = "<<", ["op_UnsignedRightShift"] = ">>>", ["op_ExclusiveOr"] = "^",
		["op_BitwiseAnd"] = "&", ["op_BitwiseOr"] = "|", ["op_OnesComplement"] = "~", ["op_LogicalNot"] = "!",
		["op_Increment"] = "++", ["op_Decrement"] = "--", ["op_True"] = "true", ["op_False"] = "false",
		["op_CheckedAddition"] = "checked +", ["op_CheckedSubtraction"] = "checked -", ["op_CheckedMultiply"] = "checked *",
		["op_CheckedDivision"] = "checked /", ["op_CheckedUnaryNegation"] = "checked -",
	};

	static readonly Dictionary<string, Dictionary<string, string>> EnumMemberNames = new(StringComparer.Ordinal);
	static readonly Dictionary<string, List<Sig>> InterfaceBases = new(StringComparer.Ordinal);

	public static void Scan(string dllPath, Dictionary<string, MemberMetadata> result) {
		using var stream = File.OpenRead(dllPath);
		using var peReader = new PEReader(stream);
		var reader = peReader.GetMetadataReader();
		CollectEnumsAndInterfaces(reader);
		foreach (var typeHandle in reader.TypeDefinitions) {
			var type = reader.GetTypeDefinition(typeHandle);
			var chain = DefinitionChain(reader, typeHandle);
			var names = chain.Select(t => reader.GetString(t.Name)).ToArray();
			if (names[0] == "<Module>") continue;
			var ns = reader.GetString(chain[0].Namespace);
			var typeDocId = (ns.Length > 0 ? ns + "." : "") + string.Join(".", names);
			var typeParams = type.GetGenericParameters().Select(h => reader.GetString(reader.GetGenericParameter(h).Name)).ToArray();
			var typeVisible = chain.All(IsTypeVisible);
			var displayName = TypeDisplayName(names, typeParams);
			var context = new DisplayContext(typeParams, []);
			var (typeSignature, isEnum) = TypeSignature(reader, type, displayName, typeVisible, context);
			result["T:" + typeDocId] = new MemberMetadata(typeDocId, ns, typeVisible, typeSignature, displayName);
			var simpleName = Regex.Replace(names[^1], @"`\d+$", "");

			foreach (var methodHandle in type.GetMethods()) {
				var method = reader.GetMethodDefinition(methodHandle);
				var name = reader.GetString(method.Name);
				var isSpecial = (method.Attributes & MethodAttributes.SpecialName) != 0;
				if (isSpecial && name is not (".ctor" or ".cctor") && !name.StartsWith("op_", StringComparison.Ordinal)) continue;
				var methodTypeParams = method.GetGenericParameters().Select(h => reader.GetString(reader.GetGenericParameter(h).Name)).ToArray();
				var methodContext = context with { MethodParams = methodTypeParams };
				var signature = method.DecodeSignature(SigProvider.Instance, null);
				var docName = name switch {
					".ctor" => "#ctor",
					".cctor" => "#cctor",
					_ => name.Replace('.', '#').Replace('<', '{').Replace('>', '}').Replace(',', '@')
				};
				if (signature.GenericParameterCount > 0) docName += "``" + signature.GenericParameterCount;
				var id = $"M:{typeDocId}.{docName}" + (signature.ParameterTypes.Length > 0 ? "(" + string.Join(",", signature.ParameterTypes.Select(DocId)) + ")" : "");
				if (name is "op_Implicit" or "op_Explicit") id += "~" + DocId(signature.ReturnType);
				var parameters = ReadParameters(reader, method.GetParameters(), signature.ParameterTypes.Length);
				var visible = typeVisible && IsMemberVisible(method.Attributes);
				var text = MethodSignature(reader, method, name, simpleName, signature, parameters, methodContext);
				result[id] = new MemberMetadata(typeDocId, ns, visible, text, name);
			}

			foreach (var propertyHandle in type.GetProperties()) {
				var property = reader.GetPropertyDefinition(propertyHandle);
				var name = reader.GetString(property.Name);
				var signature = property.DecodeSignature(SigProvider.Instance, null);
				var accessors = property.GetAccessors();
				var getter = accessors.Getter.IsNil ? (MethodDefinition?)null : reader.GetMethodDefinition(accessors.Getter);
				var setter = accessors.Setter.IsNil ? (MethodDefinition?)null : reader.GetMethodDefinition(accessors.Setter);
				var getterVisible = getter is { } g && IsMemberVisible(g.Attributes);
				var setterVisible = setter is { } s && IsMemberVisible(s.Attributes);
				var id = $"P:{typeDocId}.{name.Replace('.', '#').Replace('<', '{').Replace('>', '}').Replace(',', '@')}" + (signature.ParameterTypes.Length > 0 ? "(" + string.Join(",", signature.ParameterTypes.Select(DocId)) + ")" : "");
				var primary = getterVisible ? getter : setterVisible ? setter : getter ?? setter;
				var propertyAccess = primary is { } pa ? AccessWord(pa.Attributes) : "public";
				var isStatic = primary is { } p && (p.Attributes & MethodAttributes.Static) != 0;
				var isInit = setter is { } st && st.DecodeSignature(SigProvider.Instance, null).ReturnType is ModifiedSig { Modifier: NamedSig { Names: [.., "IsExternalInit"] } };
				var accessorText = new List<string>();
				if (getterVisible) accessorText.Add("get;");
				if (setterVisible) {
					var setterAccess = AccessWord(setter!.Value.Attributes);
					accessorText.Add((setterAccess != propertyAccess ? setterAccess + " " : "") + (isInit ? "init;" : "set;"));
				}
				var sb = new StringBuilder();
				sb.Append(propertyAccess).Append(' ');
				if (isStatic) sb.Append("static ");
				sb.Append(ReturnTypeText(signature.ReturnType, getter is { } gp ? ReadParameters(reader, gp.GetParameters(), 0)[0] : null, context)).Append(' ');
				if (signature.ParameterTypes.Length > 0) {
					var indexParams = getter is { } gi ? ReadParameters(reader, gi.GetParameters(), signature.ParameterTypes.Length) : setter is { } si ? ReadParameters(reader, si.GetParameters(), signature.ParameterTypes.Length + 1) : Array.Empty<ParamInfo?>();
					sb.Append("this[").Append(string.Join(", ", signature.ParameterTypes.Select((t, i) => ParameterText(reader, t, i + 1 < indexParams.Length ? indexParams[i + 1] : null, context, false)))).Append(']');
				}
				else sb.Append(name);
				sb.Append(" { ").Append(string.Join(" ", accessorText)).Append(" }");
				result[id] = new MemberMetadata(typeDocId, ns, typeVisible && (getterVisible || setterVisible), sb.ToString(), name);
			}

			foreach (var fieldHandle in type.GetFields()) {
				var field = reader.GetFieldDefinition(fieldHandle);
				if ((field.Attributes & FieldAttributes.SpecialName) != 0) continue;
				var name = reader.GetString(field.Name);
				var fieldType = field.DecodeSignature(SigProvider.Instance, null);
				var constant = field.GetDefaultValue();
				string text;
				if (isEnum) text = constant.IsNil ? name : $"{name} = {FormatConstant(reader, constant, null, context)}";
				else {
					var sb = new StringBuilder(AccessWord(field.Attributes)).Append(' ');
					if ((field.Attributes & FieldAttributes.Literal) != 0) sb.Append("const ");
					else {
						if ((field.Attributes & FieldAttributes.Static) != 0) sb.Append("static ");
						if ((field.Attributes & FieldAttributes.InitOnly) != 0) sb.Append("readonly ");
					}
					sb.Append(Display(fieldType, context)).Append(' ').Append(name);
					if (!constant.IsNil) sb.Append(" = ").Append(FormatConstant(reader, constant, fieldType, context));
					text = sb.Append(';').ToString();
				}
				result[$"F:{typeDocId}.{name}"] = new MemberMetadata(typeDocId, ns, typeVisible && IsFieldVisible(field.Attributes), text, name);
			}

			foreach (var eventHandle in type.GetEvents()) {
				var ev = reader.GetEventDefinition(eventHandle);
				var name = reader.GetString(ev.Name);
				var adder = ev.GetAccessors().Adder;
				var adderDef = adder.IsNil ? (MethodDefinition?)null : reader.GetMethodDefinition(adder);
				var visible = typeVisible && adderDef is { } a && IsMemberVisible(a.Attributes);
				var isStatic = adderDef is { } sa && (sa.Attributes & MethodAttributes.Static) != 0;
				var text = $"{(adderDef is { } ad ? AccessWord(ad.Attributes) : "public")} {(isStatic ? "static " : "")}event {Display(DecodeHandle(reader, ev.Type), context)} {name};";
				result[$"E:{typeDocId}.{name}"] = new MemberMetadata(typeDocId, ns, visible, text, name);
			}
		}
	}

	static void CollectEnumsAndInterfaces(MetadataReader reader) {
		foreach (var typeHandle in reader.TypeDefinitions) {
			var type = reader.GetTypeDefinition(typeHandle);
			if (SigProvider.Instance.GetTypeFromDefinition(reader, typeHandle, 0) is not NamedSig self) continue;
			if ((type.Attributes & TypeAttributes.Interface) != 0) {
				InterfaceBases[self.FullName] = type.GetInterfaceImplementations().Select(h => DecodeHandle(reader, reader.GetInterfaceImplementation(h).Interface)).ToList();
				continue;
			}
			if (type.BaseType.IsNil || DecodeHandle(reader, type.BaseType) is not NamedSig { FullName: "System.Enum" }) continue;
			var members = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var fieldHandle in type.GetFields()) {
				var field = reader.GetFieldDefinition(fieldHandle);
				var constant = field.GetDefaultValue();
				if (constant.IsNil) continue;
				members.TryAdd(ConstantValueText(reader, constant), reader.GetString(field.Name));
			}
			EnumMemberNames[self.FullName] = members;
		}
	}

	static string ConstantValueText(MetadataReader reader, ConstantHandle handle) {
		var constant = reader.GetConstant(handle);
		var blob = reader.GetBlobReader(constant.Value);
		return constant.TypeCode switch {
			ConstantTypeCode.SByte => blob.ReadSByte().ToString(CultureInfo.InvariantCulture),
			ConstantTypeCode.Byte => blob.ReadByte().ToString(CultureInfo.InvariantCulture),
			ConstantTypeCode.Int16 => blob.ReadInt16().ToString(CultureInfo.InvariantCulture),
			ConstantTypeCode.UInt16 => blob.ReadUInt16().ToString(CultureInfo.InvariantCulture),
			ConstantTypeCode.Int32 => blob.ReadInt32().ToString(CultureInfo.InvariantCulture),
			ConstantTypeCode.UInt32 => blob.ReadUInt32().ToString(CultureInfo.InvariantCulture),
			ConstantTypeCode.Int64 => blob.ReadInt64().ToString(CultureInfo.InvariantCulture),
			ConstantTypeCode.UInt64 => blob.ReadUInt64().ToString(CultureInfo.InvariantCulture),
			_ => ""
		};
	}

	static Sig Substitute(Sig sig, ImmutableArray<Sig> typeArgs) => sig switch {
		GenParamSig { IsMethod: false } generic when generic.Index < typeArgs.Length => typeArgs[generic.Index],
		NamedSig named => named with { Args = named.Args.Select(a => Substitute(a, typeArgs)).ToImmutableArray() },
		ArraySig array => array with { Element = Substitute(array.Element, typeArgs) },
		ByRefSig byRef => byRef with { Element = Substitute(byRef.Element, typeArgs) },
		PointerSig pointer => pointer with { Element = Substitute(pointer.Element, typeArgs) },
		ModifiedSig modified => modified with { Unmodified = Substitute(modified.Unmodified, typeArgs) },
		_ => sig
	};

	static List<string> PrunedInterfaceList(List<Sig> interfaces, DisplayContext context) {
		var displayed = interfaces.Select(i => Display(i, context)).ToList();
		var implied = new HashSet<string>(StringComparer.Ordinal);
		foreach (var iface in interfaces.OfType<NamedSig>()) {
			if (!InterfaceBases.TryGetValue(iface.FullName, out var bases)) continue;
			foreach (var baseInterface in bases) implied.Add(Display(Substitute(baseInterface, iface.Args), context));
		}
		foreach (var name in displayed) {
			if (name == "ISpanFormattable") implied.Add("IFormattable");
			else if (name.StartsWith("ISpanParsable<", StringComparison.Ordinal)) implied.Add("IParsable<" + name["ISpanParsable<".Length..]);
		}
		return displayed.Where(d => !implied.Contains(d)).Distinct().ToList();
	}

	public static List<TypeDefinition> DefinitionChain(MetadataReader reader, TypeDefinitionHandle handle) {
		var chain = new List<TypeDefinition>();
		var current = reader.GetTypeDefinition(handle);
		chain.Add(current);
		while (!current.GetDeclaringType().IsNil) {
			current = reader.GetTypeDefinition(current.GetDeclaringType());
			chain.Insert(0, current);
		}
		return chain;
	}

	public static bool IsTypeVisible(TypeDefinition type) => (type.Attributes & TypeAttributes.VisibilityMask) is TypeAttributes.Public or TypeAttributes.NestedPublic or TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem;

	static bool IsMemberVisible(MethodAttributes attributes) => (attributes & MethodAttributes.MemberAccessMask) is MethodAttributes.Public or MethodAttributes.Family or MethodAttributes.FamORAssem;

	static bool IsFieldVisible(FieldAttributes attributes) => (attributes & FieldAttributes.FieldAccessMask) is FieldAttributes.Public or FieldAttributes.Family or FieldAttributes.FamORAssem;

	static string AccessWord(MethodAttributes attributes) => (attributes & MethodAttributes.MemberAccessMask) switch {
		MethodAttributes.Public => "public",
		MethodAttributes.Family or MethodAttributes.FamORAssem => "protected",
		_ => "internal"
	};

	static string AccessWord(FieldAttributes attributes) => (attributes & FieldAttributes.FieldAccessMask) switch {
		FieldAttributes.Public => "public",
		FieldAttributes.Family or FieldAttributes.FamORAssem => "protected",
		_ => "internal"
	};

	static string TypeDisplayName(string[] names, string[] typeParams) {
		var used = 0;
		var segments = new List<string>();
		foreach (var name in names) {
			var match = Regex.Match(name, @"`(\d+)$");
			if (!match.Success) {
				segments.Add(name);
				continue;
			}
			var arity = int.Parse(match.Groups[1].Value);
			var args = typeParams.Skip(used).Take(arity).ToList();
			used += arity;
			segments.Add(name[..match.Index] + "<" + string.Join(", ", args) + ">");
		}
		return string.Join(".", segments);
	}

	static (string Signature, bool IsEnum) TypeSignature(MetadataReader reader, TypeDefinition type, string displayName, bool visible, DisplayContext context) {
		var attributes = type.Attributes;
		var customAttributes = AttributeNames(reader, type.GetCustomAttributes());
		var baseType = type.BaseType.IsNil ? null : DecodeHandle(reader, type.BaseType) as NamedSig;
		var baseName = baseType?.FullName;
		var access = visible ? ((attributes & TypeAttributes.VisibilityMask) is TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem ? "protected" : "public") : "internal";
		var interfaces = PrunedInterfaceList(type.GetInterfaceImplementations()
			.Select(h => DecodeHandle(reader, reader.GetInterfaceImplementation(h).Interface))
			.Where(IsSigVisible)
			.ToList(), context);

		if ((attributes & TypeAttributes.Interface) != 0) return ($"{access} interface {displayName}{BaseList(interfaces)}", false);
		if (baseName == "System.Enum") {
			var underlying = type.GetFields().Select(reader.GetFieldDefinition).Where(f => (f.Attributes & FieldAttributes.SpecialName) != 0).Select(f => Display(f.DecodeSignature(SigProvider.Instance, null), context)).FirstOrDefault();
			var flags = customAttributes.Contains("FlagsAttribute") ? "[Flags]\n" : "";
			return ($"{flags}{access} enum {displayName}" + (underlying is null or "int" ? "" : " : " + underlying), true);
		}
		if (baseName == "System.MulticastDelegate") {
			var invokeHandle = type.GetMethods().FirstOrDefault(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "Invoke");
			if (invokeHandle.IsNil) return ($"{access} delegate {displayName}", false);
			var invoke = reader.GetMethodDefinition(invokeHandle);
			var signature = invoke.DecodeSignature(SigProvider.Instance, null);
			var parameters = ReadParameters(reader, invoke.GetParameters(), signature.ParameterTypes.Length);
			var paramText = string.Join(", ", signature.ParameterTypes.Select((t, i) => ParameterText(reader, t, parameters[i + 1], context, false)));
			return ($"{access} delegate {ReturnTypeText(signature.ReturnType, parameters[0], context)} {displayName}({paramText});", false);
		}
		var modifiers = new StringBuilder(access).Append(' ');
		string kind;
		var bases = new List<string>();
		if (baseName == "System.ValueType") {
			if (customAttributes.Contains("IsReadOnlyAttribute")) modifiers.Append("readonly ");
			if (customAttributes.Contains("IsByRefLikeAttribute")) modifiers.Append("ref ");
			kind = "struct";
		}
		else {
			var isAbstract = (attributes & TypeAttributes.Abstract) != 0;
			var isSealed = (attributes & TypeAttributes.Sealed) != 0;
			if (isAbstract && isSealed) modifiers.Append("static ");
			else if (isAbstract) modifiers.Append("abstract ");
			else if (isSealed) modifiers.Append("sealed ");
			kind = type.GetMethods().Any(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "<Clone>$") ? "record" : "class";
			if (baseType != null && baseName != "System.Object" && IsSigVisible(baseType)) bases.Add(Display(baseType, context));
		}
		bases.AddRange(interfaces);
		return ($"{modifiers}{kind} {displayName}{BaseList(bases)}", false);
	}

	static string BaseList(List<string> bases) => bases.Count == 0 ? "" : " : " + string.Join(", ", bases);

	static bool IsSigVisible(Sig sig) => sig switch {
		NamedSig named => (!named.IsLocalDefinition || named.IsLocalVisible) && named.Args.All(IsSigVisible),
		ArraySig array => IsSigVisible(array.Element),
		ModifiedSig modified => IsSigVisible(modified.Unmodified),
		_ => true
	};

	static string MethodSignature(MetadataReader reader, MethodDefinition method, string name, string ownerSimpleName, MethodSignature<Sig> signature, ParamInfo?[] parameters, DisplayContext context) {
		var attributes = AttributeNames(reader, method.GetCustomAttributes());
		var isExtension = attributes.Contains("ExtensionAttribute");
		var sb = new StringBuilder(AccessWord(method.Attributes)).Append(' ');
		if ((method.Attributes & MethodAttributes.Static) != 0) sb.Append("static ");
		else if ((method.Attributes & MethodAttributes.Abstract) != 0 && (method.Attributes & MethodAttributes.Virtual) != 0 && !IsInterfaceOwner(reader, method)) sb.Append("abstract ");
		var paramText = string.Join(", ", signature.ParameterTypes.Select((t, i) => ParameterText(reader, t, parameters[i + 1], context, isExtension && i == 0)));
		var genericText = context.MethodParams.Length > 0 ? "<" + string.Join(", ", context.MethodParams) + ">" : "";
		var returnText = ReturnTypeText(signature.ReturnType, parameters[0], context);
		if (name == ".ctor") sb.Append(ownerSimpleName).Append('(').Append(paramText).Append(')');
		else if (name == ".cctor") return $"static {ownerSimpleName}()";
		else if (name is "op_Implicit" or "op_Explicit") sb.Append(name == "op_Implicit" ? "implicit" : "explicit").Append(" operator ").Append(returnText).Append('(').Append(paramText).Append(')');
		else if (OperatorSymbols.TryGetValue(name, out var symbol)) sb.Append(returnText).Append(" operator ").Append(symbol).Append('(').Append(paramText).Append(')');
		else sb.Append(returnText).Append(' ').Append(name).Append(genericText).Append('(').Append(paramText).Append(')');
		return sb.Append(';').ToString();
	}

	static bool IsInterfaceOwner(MetadataReader reader, MethodDefinition method) => (reader.GetTypeDefinition(method.GetDeclaringType()).Attributes & TypeAttributes.Interface) != 0;

	static ParamInfo?[] ReadParameters(MetadataReader reader, ParameterHandleCollection handles, int count) {
		var result = new ParamInfo?[count + 1];
		foreach (var handle in handles) {
			var parameter = reader.GetParameter(handle);
			if (parameter.SequenceNumber > count) continue;
			result[parameter.SequenceNumber] = new ParamInfo(reader.GetString(parameter.Name), parameter.Attributes, AttributeNames(reader, parameter.GetCustomAttributes()), parameter.GetDefaultValue());
		}
		return result;
	}

	static string ReturnTypeText(Sig returnType, ParamInfo? returnParam, DisplayContext context) {
		var (unwrapped, modifiers) = Unwrap(returnType);
		if (unwrapped is ByRefSig byRef) {
			var isReadOnly = modifiers.Contains("InAttribute") || (returnParam?.Attributes.Contains("IsReadOnlyAttribute") ?? false);
			return (isReadOnly ? "ref readonly " : "ref ") + Display(byRef.Element, context);
		}
		return Display(unwrapped, context);
	}

	static string ParameterText(MetadataReader reader, Sig type, ParamInfo? parameter, DisplayContext context, bool isExtensionTarget) {
		var sb = new StringBuilder();
		var attributes = parameter?.Attributes ?? new HashSet<string>();
		if (isExtensionTarget) sb.Append("this ");
		if (attributes.Contains("ScopedRefAttribute")) sb.Append("scoped ");
		if (attributes.Contains("ParamArrayAttribute") || attributes.Contains("ParamCollectionAttribute")) sb.Append("params ");
		var (unwrapped, modifiers) = Unwrap(type);
		var displayType = unwrapped;
		if (unwrapped is ByRefSig byRef) {
			var flags = parameter?.Flags ?? ParameterAttributes.None;
			if (attributes.Contains("RequiresLocationAttribute")) sb.Append("ref readonly ");
			else if (modifiers.Contains("InAttribute") || attributes.Contains("IsReadOnlyAttribute")) sb.Append("in ");
			else if ((flags & ParameterAttributes.Out) != 0 && (flags & ParameterAttributes.In) == 0) sb.Append("out ");
			else sb.Append("ref ");
			displayType = Unwrap(byRef.Element).Unwrapped;
		}
		sb.Append(Display(displayType, context));
		if (parameter != null) {
			sb.Append(' ').Append(parameter.Name);
			if ((parameter.Flags & ParameterAttributes.HasDefault) != 0 && !parameter.DefaultValue.IsNil) sb.Append(" = ").Append(FormatConstant(reader, parameter.DefaultValue, displayType, context));
		}
		return sb.ToString();
	}

	static (Sig Unwrapped, HashSet<string> Modifiers) Unwrap(Sig sig) {
		var modifiers = new HashSet<string>(StringComparer.Ordinal);
		while (sig is ModifiedSig modified) {
			if (modified.Modifier is NamedSig { Names: [.., var modifierName] }) modifiers.Add(modifierName);
			sig = modified.Unmodified;
		}
		if (sig is ByRefSig { Element: ModifiedSig } byRef) {
			var (inner, innerModifiers) = Unwrap(byRef.Element);
			modifiers.UnionWith(innerModifiers);
			sig = new ByRefSig(inner);
		}
		return (sig, modifiers);
	}

	static string FormatConstant(MetadataReader reader, ConstantHandle handle, Sig? type, DisplayContext context) {
		var constant = reader.GetConstant(handle);
		var blob = reader.GetBlobReader(constant.Value);
		var isPrimitiveTarget = type is null or PrimSig;
		string Numeric(IFormattable value, bool isZero) {
			var text = value.ToString(null, CultureInfo.InvariantCulture);
			if (isPrimitiveTarget) return text;
			var typeName = Display(type!, context);
			if (type is NamedSig named && EnumMemberNames.TryGetValue(named.FullName, out var members) && members.TryGetValue(text, out var memberName)) return $"{typeName}.{memberName}";
			return isZero ? "default" : $"({typeName}){text}";
		}
		switch (constant.TypeCode) {
			case ConstantTypeCode.Boolean: return blob.ReadBoolean() ? "true" : "false";
			case ConstantTypeCode.Char: return $"'{EscapeChar(blob.ReadChar())}'";
			case ConstantTypeCode.SByte: { var v = blob.ReadSByte(); return Numeric(v, v == 0); }
			case ConstantTypeCode.Byte: { var v = blob.ReadByte(); return Numeric(v, v == 0); }
			case ConstantTypeCode.Int16: { var v = blob.ReadInt16(); return Numeric(v, v == 0); }
			case ConstantTypeCode.UInt16: { var v = blob.ReadUInt16(); return Numeric(v, v == 0); }
			case ConstantTypeCode.Int32: { var v = blob.ReadInt32(); return Numeric(v, v == 0); }
			case ConstantTypeCode.UInt32: { var v = blob.ReadUInt32(); return Numeric(v, v == 0); }
			case ConstantTypeCode.Int64: { var v = blob.ReadInt64(); return Numeric(v, v == 0); }
			case ConstantTypeCode.UInt64: { var v = blob.ReadUInt64(); return Numeric(v, v == 0); }
			case ConstantTypeCode.Single: { var v = blob.ReadSingle(); return (float.IsFinite(v) ? v.ToString("R", CultureInfo.InvariantCulture) : FormatFloat(v)) + "f"; }
			case ConstantTypeCode.Double: { var v = blob.ReadDouble(); return FormatFloat(v); }
			case ConstantTypeCode.String: return blob.Length == 0 ? "\"\"" : "\"" + blob.ReadUTF16(blob.Length).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
			case ConstantTypeCode.NullReference:
				return type is PrimSig { Code: PrimitiveTypeCode.String or PrimitiveTypeCode.Object } || type is NamedSig { Namespace: "System", Names: ["Nullable`1"] } ? "null" : "default";
			default: return "default";
		}
	}

	static string FormatFloat(double value) {
		if (double.IsPositiveInfinity(value)) return "PositiveInfinity";
		if (double.IsNegativeInfinity(value)) return "NegativeInfinity";
		if (double.IsNaN(value)) return "NaN";
		return value.ToString("R", CultureInfo.InvariantCulture);
	}

	static string EscapeChar(char c) => c switch { '\'' => "\\'", '\\' => "\\\\", '\0' => "\\0", '\n' => "\\n", '\r' => "\\r", '\t' => "\\t", _ => c.ToString() };

	static HashSet<string> AttributeNames(MetadataReader reader, CustomAttributeHandleCollection handles) {
		var result = new HashSet<string>(StringComparer.Ordinal);
		foreach (var handle in handles) {
			var attribute = reader.GetCustomAttribute(handle);
			var parent = attribute.Constructor.Kind switch {
				HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent,
				HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
				_ => default(EntityHandle)
			};
			if (parent.Kind == HandleKind.TypeReference) result.Add(reader.GetString(reader.GetTypeReference((TypeReferenceHandle)parent).Name));
			else if (parent.Kind == HandleKind.TypeDefinition) result.Add(reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)parent).Name));
		}
		return result;
	}

	static Sig DecodeHandle(MetadataReader reader, EntityHandle handle) => handle.Kind switch {
		HandleKind.TypeDefinition => SigProvider.Instance.GetTypeFromDefinition(reader, (TypeDefinitionHandle)handle, 0),
		HandleKind.TypeReference => SigProvider.Instance.GetTypeFromReference(reader, (TypeReferenceHandle)handle, 0),
		HandleKind.TypeSpecification => reader.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(SigProvider.Instance, null),
		_ => new PrimSig(PrimitiveTypeCode.Object)
	};

	static string DocId(Sig sig) => sig switch {
		PrimSig prim => Primitives.TryGetValue(prim.Code, out var p) ? p.Doc : prim.Code.ToString(),
		NamedSig named => NamedDocId(named),
		ArraySig array => DocId(array.Element) + (array.Rank == 0 ? "[]" : "[" + string.Join(",", Enumerable.Repeat("0:", array.Rank)) + "]"),
		ByRefSig byRef => DocId(byRef.Element) + "@",
		PointerSig pointer => DocId(pointer.Element) + "*",
		GenParamSig generic => (generic.IsMethod ? "``" : "`") + generic.Index,
		ModifiedSig modified => DocId(modified.Unmodified),
		FnPtrSig => "",
		_ => "?"
	};

	static string NamedDocId(NamedSig named) {
		var sb = new StringBuilder();
		if (named.Namespace.Length > 0) sb.Append(named.Namespace).Append('.');
		var used = 0;
		for (var i = 0; i < named.Names.Length; i++) {
			if (i > 0) sb.Append('.');
			var name = named.Names[i];
			var match = Regex.Match(name, @"`(\d+)$");
			if (!match.Success || named.Args.Length == 0) {
				sb.Append(name);
				continue;
			}
			var arity = int.Parse(match.Groups[1].Value);
			sb.Append(name[..match.Index]).Append('{').Append(string.Join(",", named.Args.Skip(used).Take(arity).Select(DocId))).Append('}');
			used += arity;
		}
		return sb.ToString();
	}

	static string Display(Sig sig, DisplayContext context) => sig switch {
		PrimSig prim => Primitives.TryGetValue(prim.Code, out var p) ? p.Keyword : prim.Code.ToString(),
		NamedSig named => NamedDisplay(named, context),
		ArraySig array => Display(array.Element, context) + "[" + new string(',', Math.Max(0, array.Rank - 1)) + "]",
		ByRefSig byRef => "ref " + Display(byRef.Element, context),
		PointerSig pointer => Display(pointer.Element, context) + "*",
		GenParamSig generic => (generic.IsMethod ? context.MethodParams : context.TypeParams) is var names && generic.Index < names.Length ? names[generic.Index] : (generic.IsMethod ? "TM" : "T") + generic.Index,
		ModifiedSig modified => Display(modified.Unmodified, context),
		FnPtrSig fnPtr => "delegate*<" + string.Join(", ", fnPtr.Parameters.Append(fnPtr.Return).Select(s => Display(s, context))) + ">",
		_ => "?"
	};

	static string NamedDisplay(NamedSig named, DisplayContext context) {
		if (named.Namespace == "System" && named.Names is ["Nullable`1"] && named.Args.Length == 1) return Display(named.Args[0], context) + "?";
		if (named.Namespace == "System" && named.Names.Length == 1 && named.Names[0].StartsWith("ValueTuple`", StringComparison.Ordinal) && named.Args.Length is > 1 and < 8) return "(" + string.Join(", ", named.Args.Select(a => Display(a, context))) + ")";
		var used = 0;
		var segments = new List<string>();
		foreach (var name in named.Names) {
			var match = Regex.Match(name, @"`(\d+)$");
			if (!match.Success) {
				segments.Add(name);
				continue;
			}
			var arity = int.Parse(match.Groups[1].Value);
			var args = named.Args.Length == 0 ? Enumerable.Repeat("", arity).ToList() : named.Args.Skip(used).Take(arity).Select(a => Display(a, context)).ToList();
			used += arity;
			segments.Add(name[..match.Index] + "<" + string.Join(named.Args.Length == 0 ? "," : ", ", args) + ">");
		}
		return string.Join(".", segments);
	}
}
