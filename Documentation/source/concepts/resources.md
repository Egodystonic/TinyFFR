---
title: Resources
description: This page explains how TinyFFR handles resources and dependencies.
---

## What is a Resource?

Most things you'll work with in TinyFFR are resources. All resource types implement the `IResource` interface.(1)
{ .annotate }

1. 	There are not really any useful public methods on this interface; it is used internally to define some things all resources need to do.

	However, types tagged with `IResource` can be used in other APIs in TinyFFR (such as `ResourceGroup`).

Every resource type in TinyFFR is an [opaque handle](https://en.wikipedia.org/wiki/Opaque_pointer); i.e. an immutable struct that *represents* but does not actually *contain* the resource data. For example, a `Camera` instance does not actually contain any mutable state or camera data, it only ultimately wraps a __pointer__ to the camera data and a reference to the interface that provides the __implementation__ for that pointer.

In other words, resource types contain just two fields internally:

1. A pointer/handle;
2. A reference to the implementation for operations using that pointer/handle.

??? tip "Why are resources designed as opaque handles?"
	Although there are many valid possibilities for the design of resource management in a library like TinyFFR, this design was chosen for the following reasons:
	
	* Resource types remain small, and are quick to pass around between methods or hold in data structures. They can be packed tightly in collections, and iterated over quickly.

	* Cache locality for the *actual* data is improved. We can store the pointed-to data in whatever structure we want behind the scenes, exposing only pointers or handles in to those data structures.

	* Because resource types are C# structs they do not generate pressure on the GC. Simultaneously, because they are immutable you can not accidentally create copies of them and mutate those copies. You can not accidentally mutate an rvalue(1).
	{ .annotate }

		1. "Rvalues" are values that may not have an actual memory location; i.e. they're transient copies of data. 
		
			For example, when doing something like `#!csharp structInstance.ValueProperty.SomeProperty = someValue;` in C# the write to `SomeProperty` is often lost because the struct returned via `ValueProperty` is an rvalue and only exists for the lifetime of the expression.

??? tip "But resources behave like mutable objects?"
	Resource types give the *illusion* of being mutable reference-type instances by providing you with properties that actually defer to the enclosed implementation reference. For example, when writing `myCamera.Position = newPosition`, you're actually invoking the following property:

	```csharp
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPosition(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPosition(_handle, value);
	}
	```

	`Implementation` is the wrapped implementation reference; and it is passed the camera `_handle` along with the new `value` you wish to set. That implementation then does whatever is necessary to affect the change on the camera data.

	Neither `Implementation` nor `_handle` are mutable; therefore the resource instance itself is readonly. But it ostensibly *behaves* like a mutable reference type.

??? warning "C# compiler inconsistency"
	Unfortunately, when working with resource types in TinyFFR you may encounter a "CS1612" error when attempting to set properties via secondary structs.

	For example, the following code will not compile:

	```csharp
	readonly struct MyStruct {
		public Camera MainCamera { get; }
	}

	var s = new MyStruct();
	s.MainCamera.Position = Location.Origin; // CS1612 error here
	```

	You can fix this by using the `Set...()` methods supplied on each resource type; paired with each settable property:

	```csharp
	readonly struct MyStruct {
		public Camera MainCamera { get; }
	}

	var s = new MyStruct();
	s.MainCamera.SetPosition(Location.Origin); // No more error, works as intended
	```

	There is an open proposal to fix this inconsistency: [https://github.com/dotnet/csharplang/issues/9174](https://github.com/dotnet/csharplang/issues/9174)

	There are also various discussions on github: [https://github.com/dotnet/roslyn/issues/45284](https://github.com/dotnet/roslyn/issues/45284), [https://github.com/dotnet/csharplang/discussions/2068](https://github.com/dotnet/csharplang/discussions/2068), [https://github.com/dotnet/csharplang/discussions/8364](https://github.com/dotnet/csharplang/discussions/8364).

??? question "What is *not* a resource?"
	The short answer is: Anything that doesn't implement `IResource`.

	More generally, things that aren't resources are most the math/geometry types, but not always. These types can be used freely at any time, even before the factory has been created (or after it has been disposed).

	Builders (e.g. `ICameraBuilder`) are also technically not resources, but these can not be used without their parent factory still being 'valid'. The factory itself is also not a resource.

## Lifetimes

You generally can not create resources directly (i.e. you can not/should not write something like `new Material()`(1)). Resources should be created via the factory and its builders.
{ .annotate }

1. If you *do* try to create resources this way, you will encounter exceptions being thrown the moment you try to do anything with them or pass them anywhere in to the library.

Because most resources represent either native memory or data on the GPU, they implement `IDisposable`, and it is up to you to dispose them correctly when they're no longer in use. 

Letting a resource 'leak' by losing the reference to it before calling `Dispose()` will cause your application to slowly increase its memory size (both RAM and VRAM) until it crashes or becomes unusably slow. You can not rely on the garbage collector to do this for you: Resource data is not tracked in managed memory and resource types do not (and can not) implement [finalizers](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/finalizers).

### Dependency Tracking

Dependencies between any resources you create are automatically tracked behind-the-scenes. If you attempt to dispose a resource that is itself in use by another resource, TinyFFR will stop you by throwing an exception:

```csharp
var mesh = meshBuilder.CreateMesh(new Cuboid(1f), name: "My Cube");
var material = materialBuilder.CreateOpaqueMaterial(redColorMap);
var modelInstance = objectBuilder.CreateModelInstance(mesh, material, name: "Red Cube");

mesh.Dispose(); // Exception thrown here (1)
```

1. 	*"Unhandled exception. Egodystonic.TinyFFR.Resources.ResourceDependencyException: Can not dispose Mesh 'My Cube' because it is still in use by 1 other resource(s) ('Red Cube'). Dispose those resources first before disposing 'My Cube'."*

In the example above, attempting to dispose the `mesh` before disposing the `modelInstance` throws an exception. This is because the model instance was created using the `mesh`, and is therefore still using the data it represents loaded on the GPU memory. The `modelInstance` must be disposed first, at which point disposing the `mesh` is permitted.

In general, resource dependencies should be fairly obvious: If you're passing one resource to a builder to create another, or setting it as a property on another, that implies a dependency.

That being said, the dependency type graph is enumerated for convenience below:

* A __Material__ depends on:
	* Every *Texture* it was created with
* A __ModelInstance__ depends on:
	* The *Mesh* it is using
	* The *Material* it is using
* A __Renderer__ depends on:
	* The *Scene* it was created with
	* The *Camera* it was created with
	* The *Window* it was created with
* A __ResourceGroup__ depends on:
	* Every resource added to it
* A __Scene__ depends on:
	* Any *ModelInstance* added to it (until removed)
	* Any *Light* added to it (until removed)
	* Any *EnvironmentCubemap* added to it (until removed)

## Groups

The factory allows you to create a lightweight handle called a `ResourceGroup` that represents a grouped collection of arbitrary resources; without allocating any garbage-collected data:

```csharp
var resourceGroup = factory.ResourceAllocator.CreateResourceGroup( // (1)!
	disposeContainedResourcesWhenDisposed: false
);
resourceGroup.Add(mesh); // (2)!
resourceGroup.Add(scene);
resourceGroup.Add(materialOne);
resourceGroup.Add(materialTwo);
resourceGroup.Seal(); // (3)!
foreach (var material in resourceGroup.GetAllResourcesOfType<Material>()) { // (4)!
	// ...
}
var s = resourceGroup.GetNthResourceOfType<Scene>(0); // (5)!
resourceGroup.Dispose(disposeContainedResources: true); // (6)!
```

1. 	`CreateResourceGroup()` takes at least one argument, `disposeContainedResourcesWhenDisposed`, which indicates whether all the resources added to the group should be disposed when the group itself is disposed.

	This behaviour is a default though, and can optionally be overridden when calling `Dispose()`.

2.	Adding resources to the group is done with the `Add()` method. There is no `Remove()`.

3.	When sealed, no more resources can be added to a group.

	This helps maintain immutability. You can seal a group before exposing it to other parts of your application and be certain that nothing else will be added to it.

	Attempting to `Add()` more resources to a sealed group will cause an exception to be thrown. You can check whether a group is sealed before calling `Add()` by using the `IsSealed` property.

4.	It's possible to iterate over all resources of a given type with `GetAllResourcesOfType<T>()`. `T` must implement `IResource`.

	If the group contains no resources of type `T`, the loop will not iterate; this is valid/permitted.

5.	You can also get the "Nth" resource of type `T` with `GetNthResourceOfType<T>()`. 

	You must supply 'n' (i.e. the index of the resource). Indexing starts at 0 per-type (i.e. the example in this line returns the first `Scene` in the group).

	You can determine how many of a particular resource type are present in the group with `GetAllResourcesOfType<T>().Count`.

6.	When disposing a `ResourceGroup` you can override whether or not you wish to dispose all contained resources.

	If you just call `Dispose()` (with no arguments), the default behaviour supplied at construction will be used.

The `ResourceGroup` is *itself* a resource and can be added to another resource group. Like all other resources it is just a handle + implementation reference and is cheap to copy/pass around.

Resource groups are meant for when you wish to group/relate small bundles of strongly-associated resources (e.g. a mesh and material that make up a model). They are not designed for storing large lists of resources and you may suffer performance penalties when using them this way. 

Also, remember: Resource groups create dependencies on the resources added to them, meaning you can not dispose a resource that's part of a group before firstly disposing the group. This is by design and makes sense when using groups for their intended purpose to "collate" or "tightly-group" related assets.

If you need broader "collection-like" functionality you could instead consider *array-pool-backed collections*:

## Array-Pool-Backed Collections

The factory's `ResourceAllocator` offers two methods for creating a list or dictionary that is backed by memory-pooled arrays:

```csharp
using var factory = new LocalTinyFfrFactory();

var materials = factory.ResourceAllocator.CreateNewArrayPoolBackedList<Material>(); // (1)!
var ints = factory.ResourceAllocator.CreateNewArrayPoolBackedList<int>();  // (2)!
var dict = factory.ResourceAllocator.CreateNewArrayPoolBackedDictionary<int, Material>();  // (3)!
```

1. This creates a list of `Material`s. The returned list implements `IList<T>` and can therefore do most things any regular list can do.
2. Array-pool-backed collections do not need to contain resource types only, here we create a list of `int`s. There is no restriction in the collection type.
3. This creates a dictionary whose keys are `int`s and whose values are `Material`s. The returned type implements `IDictionary<TKey, TValue>`.

Array-pool-backed collections rent and return internal storage buffers from a shared memory pool. This means that as the array/dictionary grows over time the internal memory storage will not become GC-rootless, meaning there is no pressure on the garbage collector.

The disadvantage is that these collections are less well-optimised in some cases when compared to built-in .NET collections.

These collections must also be `Disposed()` when you are done with them.

If you can, pre-allocate collections at initialization time and dispose them after your application finishes, to reduce the garbage pressure even more (i.e. the list/dictionary itself will still be garbage collected ultimately). However this is not a hard requirement and using array-pool-backed collections will still offer great improvements to GC pressure even if you create/dispose them dynamically.

## Pooled Memory Buffers

You can also access pooled memory directly using the `ResourceAllocator`'s `CreatePooledMemoryBuffer()` method:

```csharp
var texelData = factory.ResourceAllocator
	.CreatePooledMemoryBuffer<TexelRgb24>(1024 * 1024); // (1)!
// Do stuff with texelData
factory.ResourceAllocator.ReturnPooledMemoryBuffer(texelData); // (2)!
```

1. This returns a `Memory<TexelRgb24>` of length `1024 * 1024` that will be reserved for your use until returned. The memory is reserved from an internal pool but is guaranteed to be zeroed when rented.
2. The rented memory is returned to the pool to be used again. The buffer is cleared/zeroed on return.

You should consider renting buffers like this when you need a 'space' to temporarily work with large amounts of data. Allocating standard collections or arrays results in high GC pressure if and when they are no longer in use; but using rented memory buffers avoids this problem.

You must remember to always return any rented memory or else you will cause a memory leak.
