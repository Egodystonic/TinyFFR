---
title: Creating Mock Resources
description: Snippet demonstrating how to create resource mocks for testing or stubbing
---

## Code

```csharp
var windowImplProvider = Substitute.For<IWindowImplProvider>(); // (1)!
var window = MockResourceFactory.Create(new ResourceHandle<Window>((nuint) 123UL), windowImplProvider);

window.Size = new XYPair<int>(100, 200); // (2)!
windowImplProvider.Received(1).SetSize(new ResourceHandle<Window>((nuint) 123UL), new XYPair<int>(100, 200));
```

1. 	This example demonstrates creating a mock implementation for `Window`s using [NSubstitute](https://nsubstitute.github.io/), but you can use your preferred mocking library of choice.
2.	This line sets the size of the mock Window to 100x200. The line below uses NSubstitute to check that the call was received on the mock `IWindowImplProvider`.

## Explanation

You can create mock resources with `MockResourceFactory.Create()`.

Every resource type in TinyFFR is constructed of a `ResourceHandle<T>` (where `T` is the resource type, e.g. `Window`) and an `[...]ImplProvider` interface (e.g. `IWindowImplProvider`).

The example above shows a simple demonstration of constructing a `Window` with a mock `IWindowImplProvider`; you can then use the mocked-out implementation to override the behaviour of the `Window`.

The `ResourceHandle<T>` can have any value set; note that resources with the same implementation reference and same handle integer value are considered equal.
