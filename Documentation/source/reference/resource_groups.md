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
var group = factory.ResourceAllocator.CreateResourceGroup( // (1)!
	disposeContainedResourcesWhenDisposed: true, 
	name: "Bricks"
); 

var colorMap = factory.TextureBuilder.CreateColorMap(StandardColor.Red, includeAlpha: false);
group.Add(colorMap); // (2)!

var material = factory.MaterialBuilder.CreateStandardMaterial(colorMap);
group.Add(material); // (6)!

group.Seal(); // (3)!

var firstMaterial = group.Materials[0]; // (4)!

group.Dispose(); // (5)!
```

1.	Creates a new, empty group. Because `disposeContainedResourcesWhenDisposed` is `true`, disposing the group will also dispose everything we're about to add to it.

2.	Adds the texture to the group.

3.	Seals the group, so that no further resources can be added to it.

4.	Retrieves the first (and only) material in the group.

5.	Disposes the group, which also disposes the material and the texture.

6.	Adds the material to the group. The fact that we add the material *second*, *after* the texture, is important (see [Adding & Sealing](#adding-sealing)).

A `ResourceGroup` is a resource(1) that holds a collection of other resources. Groups are intended for small bundles of closely-related resources that belong together, such as a material and the textures it uses, or every mesh, material, and texture that makes up a single model.
{ .annotate }

1.	In fact, you can add a `ResourceGroup` to another `ResourceGroup`.

TinyFFR uses groups itself wherever a single operation produces several resources; e.g. the [asset bakery](asset_bakery.md)'s `LoadBakedMaterial()`, `LoadBakedModel()`, and `LoadBakedResourceGroup()` methods each return a `ResourceGroup` holding everything they loaded, and a [`ModelBundle`](bundled_assets.md) is a thin wrapper around a group (exposed via its `UnderlyingResourceGroup` property).

### Creating Groups

Groups are created via `factory.ResourceAllocator.CreateResourceGroup()`, which accepts the following parameters:

<span class="def-icon">:material-code-json:</span> `disposeContainedResourcesWhenDisposed`

:   Whether disposing the group should also dispose every resource in it by default (see [Disposing Groups](#disposing-groups) below). This is fixed for the lifetime of the group, and can be read back via `group.DisposesContainedResourcesByDefaultWhenDisposed`.

<span class="def-icon">:material-code-json:</span> `name` *(optional)*

:   A name for the group. Like every other resource, groups can be found by name via the [resource directory](resource_directory.md).

<span class="def-icon">:material-code-json:</span> `initialCapacity` *(optional)*

:   How many resources to make room for up front. The group grows automatically as resources are added, so this is only an optimization when you know how many resources you'll be adding.

## Adding & Sealing

Any resource can be added to a group with `group.Add()`, including another `ResourceGroup`.

Adding a resource to a group creates a [dependency](resource_dependencies.md) from the group on that resource. This means that a resource can't be disposed while it's still in a (non-disposed) group; attempting to do so throws a `ResourceDependencyException`.

Once you've finished adding resources, the group can be *sealed* with `group.Seal()`. You don't have to seal a group; but doing so makes it "immutable" so you can safely pass it around. Sealing can't be undone; attempting to add resources to a sealed group throws a `ResourceGroupSealedException`. The `group.IsSealed` property indicates whether a group has been sealed.

???+ success "Add Dependencies Before Dependents"
	When a group disposes its contents, it disposes them in *reverse* order (relative to the order they were added). This means that it's recommended to add any resource to a group *after* the resources it depends upon are added (e.g. in the example at the top of the page, we add a texture and *then* the material that depends-upon/uses that texture).

	If you add a resource *before* the resources it depends on (e.g. adding the material before its texture), disposing the group may throw a `ResourceDependencyException`. The library attempts to disentangle the dependency graph before disposing the target group, but particularly complex dependency chains (especially those involving sub-groups) can still slip through, causing an exception to be thrown. Therefore, adding resources in dependency order is generally best practice.

??? tip "Sealing Groups can Improve Performance"
	When sealing a group, the library builds an internal set of flat-index maps for its constituent resources.
	
	This makes retrieving resources of a given type from them (e.g. `group.Meshes`, or `group.GetNthResourceOfType<Mesh>(3)`) faster, especially in groups containing many types of resource.
	
	This can yield a performance gain in some scenarios (i.e. where you're iterating over a large number of `ResourceGroup`s each frame). It has a neglible effect for sporadic usage patterns, however.

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

The enumeration properties cover every resource type, including: `Textures`, `Materials`, `Meshes`, `Models`, `Fonts`, `FontPens`, `FontStrings`, `BackdropTextures`, `AnimationTables`, `MeshAnimations`, `MeshNodes`, `Scenes`, `ModelInstances`, `Cameras`, `PointLights`, `SpotLights`, `DirectionalLights`, `QuadMeshes`, `QuadInstances`, `CameraLockedQuadInstances`, `TextInstances`, `CameraLockedTextInstances`, `CanvasScenes`, `CanvasTexts`, `CanvasTextures`, `Renderers`, `RenderOutputBuffers`, `RendererCompositors`, `Windows`, `Displays`, `ApplicationLoops`, and `ResourceGroups`.

The following methods offer the same functionality generically:

* `group.GetAllResourcesOfType<T>()` returns every resource of type `T` in the group (e.g. `group.GetAllResourcesOfType<Mesh>()` is equivalent to `group.Meshes`, and `group.GetAllResourcesOfType<QuadMesh>()` is equivalent to `group.QuadMeshes`).

* `group.GetNthResourceOfType<T>(index)` returns the resource of type `T` at the given index, using the same ordering.

* `group.GetAllResourcesBoxed()` returns every resource in the group (of all types) in the order they were added, each boxed as an `object` of the type it was added as. This is convenient for debugging or inspecting a group's contents generically (e.g. `if (resource is Mesh mesh) { ... }`), but allocates; prefer the typed enumerations elsewhere.

* `group.ResourceCount` returns the total number of resources in the group, of all types.

## Disposing Groups

Disposing a group with `group.Dispose()` removes the group's dependency on all of its resources; and, if the group was created with `disposeContainedResourcesWhenDisposed: true`, disposes every resource contained within.

Alternatively, `group.Dispose(disposeContainedResources)` lets you choose whether the contained resources are disposed, regardless of the setting the group was created with(1).
{ .annotate }

1.	Beware: Overriding this value means you are now responsible for the lifetime of the contained resources.

### Excluding Specific Resources from Disposal

It is possible to exclude specific dependencies from disposal; this can be useful when you want to bundle a shared resource alongside created resources:

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

## Nesting Groups

Groups can be added to other groups. The usual [dependency](resource_dependencies.md) rules apply: The inner group can't be disposed while it's still in the outer group, and disposing the outer group (with its contained resources) also disposes the inner group (and, depending on its own settings, the inner group's resources).

## Large Collections of Resources

Like all resources, a `ResourceGroup` instance is just an opaque handle, so it's cheap to copy and pass around. The copy cost of passing around a `ResourceGroup` is the same no matter if it's empty or has thousands of resources contained.

However, resource groups are ultimately designed to collate small bundles of tightly-related resources; they're not meant to act as general-purpose collections, and are not as performant as collection types for general iteration etc. Resource groups also create dependencies on everything inside them.

While it is permitted (and sometimes desirable) to create larger `ResourceGroup`s, generally you should aim to keep the average `ResourceCount` in the order of 1-100 items.

If you need to keep track of large or frequently-changing collections of resources, you should still use an ordinary collection instead. If you'd like to avoid allocating garbage while doing so, `factory.ResourceAllocator` also provides pooled, zero-garbage collections and memory. See [Avoiding GC Stutter](avoiding_gc_stutter.md) for more information.
