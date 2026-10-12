---
title: AI Assistance
description: What the TinyFFR AI assistance package is, what it contains, and how to use it with AI coding assistants such as Claude Code, Cursor, or GitHub Copilot.
icon: octicons/sparkles-fill-16
hide:
  - toc
  - footer
---

## The AI Assistance Package

The AI assistance package gives your assistant the same information you have: Everything on this site, the full API reference, and the source code of every example application, all for one specific version of TinyFFR. It's distributed as a single `.zip` file which you extract in to a folder your assistant can read. Once it's in place, you can just ask your assistant to write TinyFFR code as normal; it will look up the relevant pages and API members as it goes.

The package is just a folder of text files (markdown, XML, and C# source code). It contains no binaries and doesn't need installing.

## Downloading

[:material-download: Download the AI Assistance Package](https://github.com/Egodystonic/TinyFFR/releases)

A new package is published with every release of TinyFFR, and each package only describes the version it was published with (the version is in the file name, e.g. `TinyFFR-AI-Assistance-1.0.0.zip`, and stated at the top of `SKILL.md`). Use the package that matches the version of the `Egodystonic.TinyFFR` NuGet package your project references; otherwise your assistant may suggest APIs that have since changed.

When you upgrade TinyFFR, replace the whole `tinyffr` folder with the new version's (delete the old folder first rather than extracting over it, so that pages and API files that no longer exist don't linger).

### Contents

The `.zip` contains a single folder named `tinyffr`, which contains:

* **`SKILL.md`**: An overview of TinyFFR written for an AI assistant: What TinyFFR is, how the rest of the package is organised, and a short list of important rules (e.g. how resources must be disposed, and TinyFFR's coordinate conventions).
* **`llms.txt`**: An index of the whole package, with a one-line summary of every documentation page, API reference file, and example application. It follows the [llms.txt](https://llmstxt.org/) convention.
* **`AGENTS.md`**: A short pointer to `SKILL.md` and `llms.txt`, for tools that look for a file with this name.
* **`docs/`**: Every page of this documentation site, as markdown. Images aren't included, but are replaced with their descriptions.
* **`api/`**: The API reference, as one markdown file per namespace (e.g. `Egodystonic.TinyFFR.Rendering.md`). Each type and member is listed with its exact C# signature and its XML documentation. The original XML documentation files are included in `api/xml/`.
* **`examples/`**: The source code of every [example application](https://github.com/Egodystonic/TinyFFR/tree/main/Examples), without their assets (models, textures, etc).

The package is too large for an assistant to read all at once (roughly 10 MB when extracted), and isn't meant to be: `SKILL.md` and `llms.txt` tell the assistant how to find only the parts relevant to what it's doing.

## Using the Package

=== "Claude Code"

	The package is a ready-made Claude Code [skill](https://code.claude.com/docs/en/skills). Extract the `.zip` in to one of Claude Code's skills folders, so that the `SKILL.md` file ends up at one of the following paths:

	* `.claude/skills/tinyffr/SKILL.md` inside your project's folder, to make it available only in that project (and to anyone else working on the project, if you commit it to source control);
	* `~/.claude/skills/tinyffr/SKILL.md` inside your home folder, to make it available in all your projects.

	Claude Code will then automatically use the package whenever you ask it to do something involving TinyFFR. You can also invoke it explicitly by typing `/tinyffr` in Claude Code.

	Claude Code notices new skills straight away, even mid-session. The one exception is when the `skills` folder itself didn't exist when the session started; in that case run `/reload-skills`.

=== "Other Coding Assistants"

	For coding assistants that work inside your project folder (e.g. Cursor, GitHub Copilot, Codex, Windsurf), extract the `.zip` in to your project's folder (or anywhere else the assistant can read files from), and then tell the assistant where to find it by adding a line to its project instructions file. For example:

	```markdown
	When writing or reviewing code that uses TinyFFR, first read `tinyffr/SKILL.md`,
	then use `tinyffr/llms.txt` to find the relevant documentation, API reference, and examples.
	```

	The name of the instructions file depends on the tool; common ones include `AGENTS.md` in the root of your repository, `.github/copilot-instructions.md` for GitHub Copilot, and the rules in `.cursor/rules/` for Cursor. Check your tool's documentation for details.

	You can also just mention the folder in your prompt when you need it (e.g. "using the docs in `tinyffr/`, add a spot light to the scene").

=== "Chat Assistants"

	For chat-based assistants that you upload files to (e.g. Claude, ChatGPT, or Gemini in a web browser), the whole package is too large to attach to a conversation in one go. Instead:

	1. Attach `SKILL.md` and `llms.txt` at the start of the conversation. These give the assistant an overview and tell it what else is available.
	2. Attach the documentation pages and API reference files relevant to your question (use `llms.txt` to find them, or ask the assistant which files it would like to see).

	Some chat assistants also let you add files to a "project" so they're available in every conversation within it; if so, `SKILL.md` and `llms.txt` are good candidates to add permanently.

??? warning "Don't Extract the Package Inside a C# Project Folder"
	The `examples/` folder contains `.cs` and `.csproj` files. If you extract the package in to a folder that contains (or is inside) one of your C# projects, the .NET SDK will try to compile the example code as part of your project, causing build errors.

	Folders whose names start with a `.` are excluded automatically, so `.claude/skills/tinyffr/` is fine. Otherwise, either extract the package somewhere outside your project's folder (e.g. at the root of your repository, next to your solution file), or exclude it in your `.csproj`:

	```xml
	<ItemGroup>
		<Compile Remove="tinyffr/**" />
		<None Remove="tinyffr/**" />
	</ItemGroup>
	```

??? question "Should I Commit the Package to Source Control?"
	That's up to you. Committing it means everyone working on the project (and any AI assistant running in your CI or code review) gets the same version automatically; but it adds roughly 10 MB of files that change with every TinyFFR upgrade. If you'd rather not commit it, add the folder to your `.gitignore` and have each developer download it themselves.

## Limitations

* The package helps an assistant use TinyFFR's API correctly, but it can still make mistakes, just as with any other code it writes. Build and run what it produces.
* The API reference doesn't show nullable reference type annotations (e.g. it shows `string` where the actual parameter is `string?`). Your IDE and compiler will show you the exact signatures.
* Images and diagrams from this site are not included, only their descriptions.
