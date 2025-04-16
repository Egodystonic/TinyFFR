---
title: Input
description: This page explains the concept of application loops and input handling in TinyFFR.
---

## Application Loop

All input data is accessed via an `ApplicationLoop` (built via the factory's `ApplicationLoopBuilder`).

Every time the application loop is successfully iterated, the state of every input device (keyboard, mouse, gamepads) is updated.

??? tip "Multiple loops and IterationShouldRefreshGlobalInputStates"
	When creating an `ApplicationLoop` with the builder you can pass a config object and set `IterationShouldRefreshGlobalInputStates` to `false`.

	By default this property is `true`, but if set to `false` iterating the application loop will __not__ update input states.

	The purpose of this is to allow using multiple loops for setting different 'tick rates' for different parts/functions in your application whilst having only one loop globally update the input state of the system.

	Because input state is system/environment-wide, whenever any one loop iterates and updates the global input state that state will be updated/changed for *every* loop's `Input` view.

## Input

The `Input` property on the `loop` returns an `ILatestInputRetriever`. As its name implies, this object provides an API for accessing the __latest__ user input events & state since the last application loop iteration. 

The instance returned by `loop.Input` is the same one every time, which means you can hold on to the same `ILatestInputRetriever` reference indefinitely and as long as the `ApplicationLoop` it came from is not disposed, the instance will remain valid and can be used to always access input data for the current frame.

The `ILatestInputRetriever` has the following properties:



The same lifetime and usage pattern applies to all members of the `ILatestInputRetriever`, including the `ILatestKeyboardAndMouseInputRetriever` returned via the `KeyboardAndMouse` property and the `ILatestGameControllerInputStateRetriever` returned by the `GameControllers`/`GameControllersCombined` properties.

That being said, simply accessing `loop.Input` every time like we're doing above is absolutely fine too.