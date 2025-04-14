---
title: The Factory
description: This page explains the concept of the factory object in TinyFFR.
---

The factory object is the "root" entry point for using the library. It must be created before all other resources, and should be disposed as the last step when you're done using TinyFFR.

The factory object has no API or methods, it only exposes a set of *builders* via properties(1). Each builder presents a specific interface that is the only way to create or load resources for its resource type. For example, the `ILightBuilder` is the only way to create `Light`s. 
{ .annotate }

1. 	Actually, while most types exposed via the factory are indeed *builders*, there are also *loaders* and *discoverers*. The general rubric is as follows:

	* __Builder__ interfaces help create/construct/build resources (e.g. the `ILightBuilder`). 
	* __Loader__ interfaces help load resources (e.g. the `IAssetLoader`).
	* __Discoverer__ interfaces help find resources already attached or present in the environment (e.g. the `IDisplayDiscoverer`).

	There is also an `IResourceAllocator` accessible via the factory object that helps with allocating memory or generic resource groups.

	---

	Although these technically have different naming, for the intents of this document they can all still be thought of as "builders". 
	
	The nomenclature differences are only there to help make the API easier to understand; everything else discussed regarding "builders" from here on in applies equally to these types.

### Local Factory

The only concrete factory type available today is the `LocalTinyFfrFactory`, created simply with `new LocalTinyFfrFactory()`.(1)
{ .annotate }

1. The "local" here refers to the factory and its builders working on the local machine's desktop. In future there may be other factories that help render 3D scenes across networks, on distributed rendering clusters, or on web clients.

The constructor for `new LocalTinyFfrFactory()` takes some optional configuration parameters that can mostly be used to set the size of some internal buffers. You shouldn't need to specify anything here unless you're working with exceptionally large assets.

## Lifetime

Any builder object accessed via a factory property will continue to be valid for the lifetime of the factory. That is to say, as long as you don't invoke `factory.Dispose()`, all your builder instances will be valid. Builders themselves can not be disposed.

The same builder object instance is always returned from a given property on the factory (e.g. `factory.LightBuilder` always returns the same `ILightBuilder` instance). It is safe to store and/or pass around a reference to a specific builder rather than passing around the entire factory.

Although you should have disposed all other resources before calling `Dispose()` on the factory, when the factory is disposed it will attempt to safely/correctly dispose all other live resources. This means *every* other resource in your application will cease to be valid as soon as the factory is disposed.

Only one factory instance may be "live" at any given time. Trying to create a second factory before disposing the first will result in an exception being thrown.

???+ warning "Do not rely on the factory to dispose live resources"
	You should not rely on the factory disposing your live resources when you invoke `factory.Dispose()`.

	The factory *must* dispose live resources when it itself is disposed because it is the owner of various buffers and memory pools in use by those resources; but relying on this behaviour is error-prone.
	
	When "nuking" resources this way, the dependency graph between them may be broken temporarily during the teardown. In the worst case, this may lead to exceptions being thrown in order to protect memory safety.

	Always dispose resources properly when no longer in use. By the time you call `Dispose()` on the factory, every other resource should already have been disposed.