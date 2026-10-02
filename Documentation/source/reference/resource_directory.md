---
title: Resource Directory
description: Information on how to enumerate and find live resources via the IResourceDirectory.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * The resource directory lets you enumerate every live resource of a given type, or find resources by name. :material-arrow-right: [The Resource Directory](#the-resource-directory)
    * It's mostly useful for debugging, diagnostics, and tooling, rather than as a way to keep track of resources in your application. :material-arrow-right: [Usage Notes](#usage-notes)

</div>

## The Resource Directory

```csharp
var directory = factory.ResourceDirectory;

foreach (var mesh in directory.GetAllActiveInstances<Mesh>()) { // (1)!
	Console.WriteLine(mesh);
}

var bricks = directory.FindByName<Material>("Bricks"); // (2)!
if (bricks is { } material) {
	Console.WriteLine($"Found {material}.");
}
```

1.	Enumerates every `Mesh` that currently exists (i.e. has been created and not yet disposed).

2.	Looks up a `Material` named "Bricks". The result is `null` if no such material is found.

`factory.ResourceDirectory` (an `IResourceDirectory`) keeps track of every live resource created by the factory. It can be used to:

* Enumerate every live resource of a given type, via `GetAllActiveInstances<T>()`.
* Find resources of a given type by their name, via `FindByName<T>()`.

Resources appear in the directory as soon as they're created, and are removed when they're disposed; no registration is required.

### Finding Resources By Name

`FindByName<T>()` searches the live resources of type `T` for one whose name matches the given string:

```csharp
var wallColor = directory.FindByName<Texture>("wall color"); // (1)!
var anyWall = directory.FindByName<Texture>("wall", allowPartialMatch: true); // (2)!
var exactWall = directory.FindByName<Texture>("Wall Color", comparisonType: StringComparison.Ordinal); // (3)!
```

1.	By default, the whole name must match, but the comparison ignores case (`StringComparison.OrdinalIgnoreCase`). So this finds a texture named "Wall Color".

2.	With `allowPartialMatch: true`, the search string only needs to be found somewhere in the resource's name. This would find "Wall Color", "Wall Normals", "Stone Wall", etc.

3.	The `comparisonType` parameter sets how names are compared. Here the match is case-sensitive.

This version of `FindByName()` returns the first matching resource it finds as a nullable `T?`, or `null` if there is no match. If more than one resource matches, there's no guarantee as to which of them is returned.

Resource names are not required to be unique, so a search can match more than one resource. To get every match, pass in a span to be filled with the results:

```csharp
using var resultsLease = factory.ResourceAllocator.BorrowSpan<Texture>(16); // (1)!
var numMatches = directory.FindByName(resultsLease.Span, "wall", allowPartialMatch: true); // (2)!

foreach (var texture in resultsLease.Span[..Math.Min(numMatches, resultsLease.Span.Length)]) { // (3)!
	Console.WriteLine(texture);
}
```

1.	Borrows a buffer with room for up to 16 results (see [memory and collections](resource_groups.md#large-collections-of-resources)).

2.	Fills the span with matching textures. The return value is the *total* number of matches found, which can be greater than the length of the span (in which case only the first `resultsLease.Span.Length` matches are written).

3.	Only the matches that were actually written to the span are iterated.

### Supported Types

The directory supports the following resource types:

<span class="def-icon">:material-card-bulleted-outline:</span> Assets

:   `Texture`, `Material`, `Mesh`, `Model`, `Font`, `BackdropTexture`, `MeshGroupAnimationTable`, `MeshAnimation`, `MeshNode`, `DynamicVertexBuffer`

<span class="def-icon">:material-card-bulleted-outline:</span> World

:   `Scene`, `ModelInstance`, `Camera`, `PointLight`, `SpotLight`, `DirectionalLight`

<span class="def-icon">:material-card-bulleted-outline:</span> Rendering

:   `Renderer`, `RenderOutputBuffer`, `RendererCompositor`

<span class="def-icon">:material-card-bulleted-outline:</span> Environment

:   `Display`, `Window`, `ApplicationLoop`

<span class="def-icon">:material-card-bulleted-outline:</span> Other

:   `ResourceGroup`

Specialized resource types (such as `QuadMesh`, `TextInstance`, or `FontString`) can't be looked up directly. Instead, look up the underlying resource type they specialize (e.g. `Mesh` for a `QuadMesh`, or `ModelInstance` for a `TextInstance`).

### Per-Type Directories

`directory.ForType<T>()` returns an `IResourceDirectory<T>`, a directory scoped to a single resource type. It offers the same functionality (`AllActiveInstances` and `FindByName()`) without needing to specify the type on every call:

```csharp
var textureDirectory = factory.ResourceDirectory.ForType<Texture>();
var wallColor = textureDirectory.FindByName("Wall Color");
var numTextures = textureDirectory.AllActiveInstances.Count;
```

## Usage Notes

The resource directory is designed primarily for debugging, diagnostics, and tooling (e.g. listing every live texture in a debug UI, or checking for resources that weren't disposed before shutdown).

???+ warning "Not a Replacement for References"
	Finding resources by name requires checking the name of every live resource of that type; and resource names are not guaranteed to be unique. Prefer keeping hold of the resources you create (in fields, or in [resource groups](resource_groups.md)) over looking them up by name, especially in code that runs every frame.

??? warning "Modifying Resources While Enumerating"
	Creating or disposing a resource of type `T` while enumerating `GetAllActiveInstances<T>()` invalidates the enumeration; continuing to iterate it throws an `InvalidOperationException`.

	If you need to dispose resources as you find them (e.g. cleaning up everything that matches some condition), copy them in to a separate list or span first, then dispose them after enumeration has finished.
