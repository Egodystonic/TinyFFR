---
title: Mocking Resources
description: Information on how to create fake resources for unit testing code that uses TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Every resource is a handle plus an *implementation provider*; by supplying your own implementation, you can create fake resources for unit tests. :material-arrow-right: [Mocking Resources](#mocking-resources)
    * Fake resources are created with `ResourceUtils.CreateCustom()`, using a hand-written fake or a mocking library. :material-arrow-right: [Creating Fakes](#creating-fakes)
    * Fake resources are only for testing your own code; they must never be passed in to TinyFFR itself. :material-arrow-right: [Limitations](#limitations)

</div>

## Mocking Resources

```csharp
static float GetAspectRatio(Texture texture) { // (1)!
	return (float) texture.Dimensions.X / texture.Dimensions.Y;
}

var impl = Substitute.For<ITextureImplProvider>(); // (2)!
impl.GetDimensions(Arg.Any<ResourceHandle<Texture>>()).Returns(new XYPair<int>(1920, 1080));
var texture = ResourceUtils.CreateCustom<Texture, ITextureImplProvider>(1, impl); // (3)!

Assert.That(GetAspectRatio(texture), Is.EqualTo(16f / 9f).Within(0.0001f)); // (4)!
```

1.	Some code to be tested, which uses a `Texture`.

2.	Creates a fake `ITextureImplProvider` with the [NSubstitute](https://nsubstitute.github.io/) mocking library, and configures it to report the texture's dimensions as 1920x1080.

3.	Creates a `Texture` that uses the fake implementation.

4.	The code under test can now be run against the fake texture, with no factory, window, or GPU required.

Every resource in TinyFFR (`Texture`, `Mesh`, `Camera`, `Scene`, etc) is a small struct holding two things:

* A *handle*, which identifies the resource.
* An *implementation provider* (`ITextureImplProvider`, `IMeshImplProvider`, `ICameraImplProvider`, etc), which every property and method on the resource forwards to, passing along the handle.

Normally the implementation provider is part of the factory, and does the real work of managing the resource. But because implementation providers are ordinary public interfaces, you can supply your own; this lets you unit test code that uses TinyFFR resources without creating a factory or any real resources.

## Creating Fakes

`ResourceUtils.CreateCustom<TResource, TImpl>(handle, impl)` creates a resource of type `TResource` that forwards everything to `impl`:

<span class="def-icon">:material-card-bulleted-outline:</span> `handle`

:   Any value you like (it's implicitly convertible from an integer or pointer). It's passed to every method called on `impl`, so a single implementation can tell multiple fake resources apart by their handles.

<span class="def-icon">:material-card-bulleted-outline:</span> `impl`

:   Your implementation of the resource's implementation provider interface (`TImpl`), e.g. `ITextureImplProvider` for a `Texture`. Must not be `null`.

Any mocking library that can mock interfaces will work, as shown at the [top of this page](#mocking-resources). Alternatively, you can write a fake implementation by hand:

```csharp
sealed class FakeTextureImplProvider : ITextureImplProvider {
	public XYPair<int> Dimensions { get; set; } = new(64, 64);
	public bool WasDisposed { get; private set; }

	public XYPair<int> GetDimensions(ResourceHandle<Texture> handle) => Dimensions; // (1)!
	public TexelType GetTexelType(ResourceHandle<Texture> handle) => TexelType.Rgb24;
	public bool GetAllowsDynamicWrites(ResourceHandle<Texture> handle) => false;
	public bool GetContainsMipMaps(ResourceHandle<Texture> handle) => true;
	public TextureRenderingConfig GetRenderingConfig(ResourceHandle<Texture> handle) => default;
	public void OverwriteTexels<TTexel>(ResourceHandle<Texture> handle, ReadOnlySpan<TTexel> newTexels, XYPair<int> dimensions, XYPair<int> offset)
		where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgb24>, IConversionSupplyingTexel<TTexel, TexelRgba32> { }

	public bool IsDisposed(ResourceHandle<Texture> handle) => WasDisposed; // (2)!
	public void Dispose(ResourceHandle<Texture> handle) => WasDisposed = true;

	public string GetNameAsNewStringObject(ResourceHandle<Texture> handle) => "Fake Texture"; // (3)!
	public int GetNameLength(ResourceHandle<Texture> handle) => "Fake Texture".Length;
	public void CopyName(ResourceHandle<Texture> handle, Span<char> destinationBuffer) => "Fake Texture".CopyTo(destinationBuffer);
}

var impl = new FakeTextureImplProvider { Dimensions = new(1920, 1080) };
var texture = ResourceUtils.CreateCustom<Texture, ITextureImplProvider>(1, impl);
```

1.	Invoked whenever `texture.Dimensions` is read.

2.	`IsDisposed()` and `Dispose()` are part of every disposable resource's implementation provider. Here, the fake records whether the texture was disposed, so that a test can check that the code under test disposed it.

3.	The name methods are part of every resource's implementation provider, and are used by e.g. `texture.GetNameAsNewStringObject()` and `texture.ToString()`.

??? info "Disposal Checks"
	Resources forward their properties and methods to the implementation provider without checking whether they've been disposed first; in TinyFFR's own implementations it's the implementation provider that throws an `ObjectDisposedException` when a disposed resource is used.

	So if you'd like your fake resources to throw when used after being disposed, implement that check in your fake implementation.

### Extracting Handles & Implementations

`ResourceUtils` also offers the reverse operations, which work on any resource (fake or real):

<span class="def-icon">:material-card-bulleted-outline:</span> `ResourceUtils.ExtractHandle(resource)`

:   Returns the resource's handle. Throws an `ObjectDisposedException` if the resource has been disposed. Note that the handle is only an identifier; for real resources it may represent a pointer to memory that no longer exists after the resource is disposed.

<span class="def-icon">:material-card-bulleted-outline:</span> `ResourceUtils.ExtractImplementation<TResource, TImpl>(resource)`

:   Returns the resource's implementation provider. For a fake resource, this is the `impl` it was created with.

These are useful in tests for checking which resource some code returned or used; for example, verifying that a method returned the specific fake texture it was given.

Two resources are equal (via `==` or `Equals()`) when they have both the same handle *and* the same implementation provider instance. So two fakes with the same handle but different implementations are not equal.

## Limitations

???+ danger "Never Pass Fakes to TinyFFR"
	Fake resources are only for testing your own code. Passing a fake resource in to the factory or anything created by it (e.g. creating a material from a fake texture, or adding a fake model instance to a real scene) is not supported. TinyFFR's implementations assume every resource they're given is one of their own, and doing so can crash the process outright rather than throwing an exception.

Additionally:

* Fake resources are not tracked by any factory: they don't appear in the [resource directory](resource_directory.md), and have no [dependencies](resource_dependencies.md) tracked.
* A resource's `default` value has no implementation provider; using it throws an `InvalidObjectException`. This applies to fake and real resource types alike.
