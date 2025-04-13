---
title: The Factory
description: This page explains the concept of the factory object in TinyFFR.
---

## Overview

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

### Lifetime

Any builder object accessed via a factory property will continue to be valid for the lifetime of the factory. That is to say, as long as you don't invoke `factory.Dispose()`, your `ILightBuilder` will be valid. Builders themselves can not be disposed.

### Local Factory

Currently there are two factory interfaces:

1. `ITinyFfrFactory` :material-arrow-right: Contains 