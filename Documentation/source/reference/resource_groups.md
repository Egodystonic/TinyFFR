---
title: Resource Groups
description: Information on how to bundle related resources together with ResourceGroups.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * A `ResourceGroup` bundles a small number of closely-related resources together so they can be passed around and disposed as one. :material-arrow-right: [Resource Groups](#resource-groups)
    * Resources are added to a group, which can then be *sealed* to prevent further additions. :material-arrow-right: [Adding & Sealing](#adding-sealing)
    * Disposing a group can also dispose everything in it. :material-arrow-right: [Disposing Groups](#disposing-groups)

</div>

## Resource Groups

```csharp
var group = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true, name: "Bricks"); // (1)!

var colorMap = factory.TextureBuilder.CreateColorMap(StandardColor.Red, includeAlpha: false);
var material = factory.MaterialBuilder.CreateStandardMaterial(colorMap);
group.Add(colorMap); // (2)!
group.Add(material);
group.Seal(); // (3)!

var firstMaterial = group.Materials[0]; // (4)!

group.Dispose(); // (5)!
```

1.	Creates a new, empty group. Because `disposeContainedResourcesWhenDisposed` is `true`, disposing the group will also dispose everything in it.

2.	Adds the texture and material to the group.

3.	Seals the group, so that no further resources can be added to it.

4.	Retrieves the first (and only) material in the group.

5.	Disposes the group, which also disposes the material and the texture.

A `ResourceGroup` is a resource that holds a collection of other resources. Groups are intended for small bundles of closely-related resources that belong together, such as a material and the textures it uses, or every mesh, material, and texture that makes up a single model.

TinyFFR uses groups itself wherever a single operation produces several resources: for example, the [asset bakery](asset_bakery.md)'s `LoadBakedMaterial()`, `LoadBakedModel()`, and `LoadBakedResourceGroup()` methods each return a `ResourceGroup` holding everything they loaded, and a [`ModelBundle`](bundled_assets.md) is a thin wrapper around a group (exposed via its `UnderlyingResourceGroup` property).

### Creating Groups

Groups are created via `factory.ResourceAllocator.CreateResourceGroup()`, which accepts the following parameters:

<span class="def-icon">:material-card-bulleted-outline:</span> `disposeContainedResourcesWhenDisposed`

:   Whether disposing the group should also dispose every resource in it by default (see [Disposing Groups](#disposing-groups) below). This is fixed for the lifetime of the group, and can be read back via `group.DisposesContainedResourcesByDefaultWhenDisposed`.

<span class="def-icon">:material-card-bulleted-outline:</span> `name` *(optional)*

:   A name for the group. Like every other resource, groups can be found by name via the [resource directory](resource_directory.md).

<span class="def-icon">:material-card-bulleted-outline:</span> `initialCapacity` *(optional)*

:   How many resources to make room for up front. The group grows automatically as resources are added, so this is only an optimization when you know how many resources you'll be adding.

## Adding & Sealing

Any resource can be added to a group with `group.Add()`, including another `ResourceGroup`.

Adding a resource to a group creates a [dependency](resource_dependencies.md) from the group on that resource. This means that a resource can't be disposed while it's still in a (non-disposed) group; attempting to do so throws a `ResourceDependencyException`.

Specialized resource types (such as `QuadMesh`, `TextInstance`, `FontString`, or `CanvasScene`) can also be added to groups, and are retrieved again as their specialized type via the group's specialized enumeration properties (e.g. `group.QuadMeshes`).

Once you've finished adding resources, the group can be *sealed* with `group.Seal()`. Sealing can't be undone: attempting to add resources to a sealed group throws a `ResourceGroupSealedException`. `group.IsSealed` indicates whether a group has been sealed.

???+ tip "Seal Your Groups"
	Sealed groups build an index of their contents by type, which makes retrieving resources of a given type from them (e.g. `group.Meshes`, or `group.GetNthResourceOfType<Mesh>(3)`) faster, especially in groups containing many types of resource.

	It's also a useful safeguard against accidentally adding resources to a group after it's been handed off to other code. Every group returned by TinyFFR itself is already sealed.

## Retrieving Resources

Each resource type has a corresponding enumeration property on `ResourceGroup` that returns every resource of that type in the group, in the order they were added:

```csharp
foreach (var mesh in group.Meshes) { // (1)!
	Console.WriteLine(mesh);
}

var numTextures = group.Textures.Count; // (2)!
var secondMaterial = group.Materials[1]; // (3)!
```

1.	Iterates every `Mesh` in the group.

2.	Each enumeration has a `Count` of how many resources of that type are in the group.

3.	Enumerations can also be indexed directly.

The enumeration properties cover every resource type, including:

<span class="def-icon">:material-card-bulleted-outline:</span> Assets

:   `Textures`, `Materials`, `Meshes`, `Models`, `Fonts`, `BackdropTextures`, `AnimationTables`, `MeshAnimations`, `MeshNodes`

<span class="def-icon">:material-card-bulleted-outline:</span> World

:   `Scenes`, `ModelInstances`, `Cameras`, `PointLights`, `SpotLights`, `DirectionalLights`

<span class="def-icon">:material-card-bulleted-outline:</span> Rendering & Environment

:   `Renderers`, `RenderOutputBuffers`, `RendererCompositors`, `Windows`, `Displays`, `ApplicationLoops`

<span class="def-icon">:material-card-bulleted-outline:</span> Specialized Types

:   `QuadMeshes`, `QuadInstances`, `CameraLockedQuadInstances`, `TextInstances`, `CameraLockedTextInstances`, `FontStrings`, `FontPens`, `CanvasScenes`, `CanvasTexts`, `CanvasTextures`

<span class="def-icon">:material-card-bulleted-outline:</span> Other

:   `ResourceGroups`

The following methods offer the same functionality generically:

* `group.GetAllResourcesOfType<T>()` returns every resource of type `T` in the group (e.g. `group.GetAllResourcesOfType<Mesh>()` is equivalent to `group.Meshes`).
* `group.GetNthResourceOfType<T>(index)` returns the resource of type `T` at the given index, using the same ordering.
* `group.GetAllResourcesBoxed()` returns every resource in the group (of all types) in the order they were added, each boxed as an `object`. This is convenient for debugging or inspecting a group's contents generically (e.g. `if (resource is Mesh mesh) { ... }`), but allocates; prefer the typed enumerations elsewhere.
* `group.ResourceCount` returns the total number of resources in the group, of all types.

For specialized types, use the two-type-parameter overloads, specifying the specialized type and the type it specializes (e.g. `group.GetAllResourcesOfType<QuadMesh, Mesh>()`).

## Disposing Groups

Disposing a group with `group.Dispose()` removes the group's dependency on all of its resources; and, if the group was created with `disposeContainedResourcesWhenDisposed: true`, disposes every resource in it.

Alternatively, `group.Dispose(disposeContainedResources)` lets you choose whether the contained resources are disposed, regardless of the setting the group was created with.

```csharp
var group = factory.ResourceAllocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true);
group.Add(colorMap);
group.Add(material);
group.Add(sharedMesh);
group.ExcludeFromDisposal(sharedMesh); // (1)!
group.Dispose(); // (2)!
```

1.	Marks `sharedMesh` so that it isn't disposed along with the group. The group must not yet be sealed.

2.	Disposes `material` and `colorMap`; `sharedMesh` is left alive (and no longer has a dependency from the group).

???+ warning "Add Dependencies Before Dependents"
	When a group disposes its contents, it disposes them in the *reverse* order to the order they were added. This means that if you add each resource to a group *after* the resources it uses (e.g. adding a texture before the material that uses it), the group will always dispose dependents before the resources they depend on.

	If you add a resource *before* the resources it depends on (e.g. adding the material before its texture), disposing the group throws a `ResourceDependencyException` part-way through, because the group attempts to dispose the texture while the material is still using it.

### Nesting Groups

Groups can be added to other groups. The usual [dependency](resource_dependencies.md) rules apply: the inner group can't be disposed while it's still in the outer group, and disposing the outer group (with its contained resources) also disposes the inner group (and, depending on its own settings, the inner group's resources).

Like all resources, a `ResourceGroup` is just a small handle, so it's cheap to copy and pass around; every copy refers to the same group.

## Large Collections of Resources

Resource groups are designed for small bundles of tightly-related resources; they're not designed to act as general-purpose collections, and may perform poorly when used to hold large numbers of resources. They also create dependencies on everything in them, which can make managing the lifetimes of individual resources awkward.

If you need to keep track of large or frequently-changing collections of resources (e.g. every enemy in a level), use an ordinary collection instead. If you'd like to avoid allocating garbage while doing so, `factory.ResourceAllocator` also provides pooled, zero-garbage collections and memory:

<span class="def-icon">:material-card-bulleted-outline:</span> `CreateNewArrayPoolBackedList<T>()`, `CreateNewArrayPoolBackedDictionary<TKey, TValue>()`, `CreateNewArrayPoolBackedSet<T>()`

:   Creates a new collection backed by pooled memory, suitable for long-lived storage (e.g. a field). Dispose the collection when it's no longer needed, to return its memory to the pool.

<span class="def-icon">:material-card-bulleted-outline:</span> `GetSharedScratchList<T>()`, `GetSharedScratchDictionary<TKey, TValue>()`, `GetSharedScratchSet<T>()`

:   Returns a shared, reusable collection for short-lived scratch work (e.g. within a single method). The same instance is returned (and cleared) on every call, so never hold on to one across a call that may itself use it.

<span class="def-icon">:material-card-bulleted-outline:</span> `BorrowSpan<T>(numElements)`

:   Borrows a pooled buffer for a short-lived operation, returned as a lease that should be disposed (usually via `using`) when you've finished with it.
