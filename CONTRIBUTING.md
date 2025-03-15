# Contributing to TinyFFR

Please follow the following steps to submit code to this repository:

1. If no issue describing the change you wish to make already exists, make it.
2. On the issue pertaining to the change you wish to make, add a comment indicating you wish to begin work on this change. The design of the implementation should be discussed with the project owner(s).
3. Once the design is agreed-upon and you are given the go-ahead, fork the repository and develop the changeset in your fork. Use the guidelines below to guide your implementation.
4. When ready, submit a PR (Pull Request).

# What to Contribute

All contributions are welcome, even if you're fixing an issue that only applies to your usage.

If you don't have a specific issue you wish to address, and instead are just looking to help out, the issues labeled as "[good first issue](https://github.com/Egodystonic/TinyFFR/labels/%E2%9D%87%EF%B8%8F%20good%20first%20issue%20%E2%9D%87%EF%B8%8F)" are a great place to start. 

Feel free to comment on any issue asking for more direction and help if you need!

# Code & Design Guidelines

The following guidelines are not meant to dissuade anyone from contributing. If you wish, simply scan through and read the "Important" bubbles for a quick starting point.

## Design Philosophy, Target Users

> [!IMPORTANT]
> Make your public API as braindead-simple to use as possible.

> [!IMPORTANT]
> Convention over configuration.

> [!IMPORTANT]
> Plain-English over mathematical or rendering jargon.

TinyFFR stands for "Tiny Fixed Function Renderer". It is not designed as a full-fat graphics API. It is not attempting to replace game engines or facilitate complex novel shading techniques.

TinyFFR is designed as a lightweight, opinionated library that makes it really easy to render things in an industry-standard way (e.g. PBR). It is designed for users that do not need a highly flexible rendering pipeline or design environment.

We try not to assume any 3D math or linear algebra experience on the user's behalf. Language around math and geometry operations should be, to the fullest extent possible, plain-English descriptive. Attempt to abstract away difficult math behind simpler-looking operations (even at the cost of performance, within reason).

## Performance and Garbage

> [!IMPORTANT]
> Performance is important, but can be secondary to API design. That being said, write code so that the "default API path" will not pressure the GC.

We try to minimize garbage as it helps keep "jitter" or "frame stuttering" very low, which is important for many realtime graphics applications. In general, we try to avoid generating garbage (GC pressure) at all in all APIs. The only exceptions are:

* When no viable zero-allocating API exists in .NET
* When offering an API or function "off the expected hot path" that offers other benefits (e.g. a debug command, ToString, etc)

On the other hand, users requiring the absolute highest performance from their graphics application should probably be using C++ and a raw graphics API (OpenGL, DirectX, Vulkan, Metal, etc.). Our target user is one who values and desires steady and reliable performance, but does not mind sacrificing the final 10% for a much friendlier developer experience. We care more about reducing GC pauses than pure throughput.

Therefore we consider it acceptable to sacrifice some performance for the sake of providing a more user-friendly API. This is more of an art than a science, and may need to be profiler-driven when hard decisions arise, but we generally do not value sacrificing a friendly API in the name of chasing every last nanosecond of performance.

## Testing

> [!IMPORTANT]
> Please make sure your code is thoroughly tested.

Most code in TinyFFR is covered either by a unit test or an "integration test" (functionally a larger unit test, marked as `[Explicit]`). Tests help prove new algorithms and implementations' correctness, but perhaps more importantly allow us to prevent regressions when editing existing code.

* Unit tests should be reasonably quick, parallelizable, and automatable.
* Integration tests are, for now, designed for human/manual run and checking.

## Mutability

> [!IMPORTANT]
> Make everything immutable by default. Only create mutable objects when provably worthwhile. 

TinyFFR uses an "immutable by default" design philosophy. Mutable objects are generally considered more error-prone and harder to reason about. Immutable types also lend themselves more readily to performance optimisations. 

Of course, eventually, there *must* be mutation in any real application or library; the current desire is to try and "quarantine" all the mutation in internal and private classes where we can control it/protect it from the "outside world".

## Vernacular

### Modifier functions

> [!IMPORTANT]
> "Modifier" functions on immutable objects should use past-participle.

```csharp
var v = vect
    .RotatedBy(rot)
    .WithLengthIncreasedBy(3f)
    .ProjectedOnTo(direction)
    .ReflectedBy(plane);
```

The vernacular used for functions that return "modified" instances of their immutable dispatch target is past tense. This has two benefits:

* It makes these methods "chainable";
* It makes it clear at-a-glance that these methods return a new instance of the target object, rather than mutating it in-place.

### With...() vs with { ... }

> [!IMPORTANT]
> Use `With...()` methods when ordering of mutations matters. Use `init` property setters when it does not.

```csharp
var a = cuboid.WithVolume(2f).WithSurfaceArea(4f);
var b = cuboid with { Width = 2f, Depth = 3f };
```

We make properties on immutable types "init-able" when mutating those properties with a `with` statement is ordering-independent; e.g. the order they're set in makes no difference to the outcome object.

When this is not the case, we prefer `With()` methods that force the developer to explicitly specify the ordering of their mutations.

### To vs As

> [!IMPORTANT]
> Use a "ToXyz()" function/property when any sort of transformation occurs or is implied. Use an "AsXyz()" function/property when the conversion is more of a simple reinterpretation.

```csharp
var vect = direction.AsVect();
var v3 = direction.ToVector3();
```

The lines here get very blurry, but in general:

* We try to use the "As" vernacular for conversions that are *ostensibly* "reinterpret casts" from the user's POV (even if they're actually not); 
* We try to use the "To" vernacular for things that are *ostensibly* conversions to another related but different type/object.

### Language

> [!IMPORTANT]
> US English as default.

> [!IMPORTANT]
> "Orthogonal" preferred over "Perpendicular".

US English is the lingua-Franca of the programming world; so that is what we use. 

Note: I (Ben) am Welsh/British, there may be some places British English has slipped in, feel free to correct these if you see them.

Note 2: I had a mix of "perpendicular" and "orthogonal" for a while in the API, but in the end it's just confusing and irritating to try and guess which one you need each time, so even if there is some theoretical 'right' answer for each usage I think it's better to just use one everywhere (and I arbitrarily chose "Orthogonal").

## Object and API Design

### Object Construction

> [!IMPORTANT]
> Use constructors for the most "intrinsic" construction of an object. Use static factory methods for anything that helps build those intrinsic parameters.

```csharp
var a = new Rotation(angle, axis);
var b = Rotation.FromStartAndEndDirection(dir1, dir2);
```

Generally we try to make the constructor for a type (where one is provided at all) take the "intrinsic" or "core" parameters/properties needed to construct that object. This applies to what is *presented as* the intrinsic properties of the object, not what may actually *be* the underlying state implementation.

For all other constructions, we try to provide static factory methods. Users should then implicitly understand that factory methods are there to help build the intrinsic state of their object, and may (but may not) incur performance penalties.

These are only rough guidelines and do not universally apply.

### Handling Degenerate Inputs

> [!IMPORTANT]
> Handle degenerate/invalid inputs as gracefully as possible. Use exceptions only when the inputs can only come about from invalid API usage.

```csharp
var canBeNull = direction.ParallelizedWith(plane);
var canBeInvalid = direction.FastParallelizedWith(plane);
var canThrow = direction[(Axis) randomInt];
```

When an input to a method can result in an invalid or non-continuable outcome, we use the following guidelines:

**Can the method return a nullable value?** 

This option is preferred when the inputs being nonviable is not necessarily the result of programmer error (e.g. the math/geometry API uses this approach a lot). 

Doing checks and returning null (which then must be checked again by the caller) can introduce a lot of branching and register spilling; so we tend to offer a "Fast" variant alongside null-returning methods that assume all inputs are valid and coherent (and are allowed to return nonsense or undefined results if those assumptions are violated).

**Should the method throw an exception?**

In the case that an input should *never* be passed to a method it's acceptable to throw an exception. We try to follow the mantra "exceptions are for exceptional circumstances", i.e. they should only be thrown when the developer has made a mistake in the usage of the API, not when the inputs may have naturally become invalid via normal usage of the API.

### Handling Floating Point

> [!IMPORTANT]
> Check for and correct floating point inaccuracies as standard. Try to shield users from FP-inaccuracy-related issues in the API design and implementation.

```csharp
var linesMatch = line1.Equals(line2, Epsilon);
var areOrtho = v1.IsApproximatelyOrthogonalTo(v2, Epsilon);
```

Correctly working with floating point values can be a minefield. A lot of classes in TinyFFR implement an `IToleranceEquatable<T>` interface that allows FP-inaccuracy-aware equality checks to be made. Consider implementing this interface in any new types you author.

Additionally, validate FP-based inputs and outputs where appropriate (even if it incurs additional performance costs), especially if the resultant values could be meaningless or outside a valid range. 

* One example is the implementation of `Direction.Dot(Direction)` that clamps the returned result to the range `-1f to 1f`: Passing anything outside that range to some trigonometric functions results in `NaN` which can be really hard to track down and tends to have a 'viral' effect.
* Another example is in various `AngleTo(...)` methods that automatically clamp small enough margins to 0° or 180° as users tend to check for these values directly after certain operations and unwittingly introduce FP-inaccuracy bugs.

## C# Code Conventions

> [!IMPORTANT]
> Generally, we ask that contributors copy the convention/style of the repository as it exists today. 

Please browse the source code to get an understanding of the style we're using. We do not expect 100% accuracy as every developer has their own idiosyncrasies, however please kindly be prepared to make any convention corrections in the PR process.

## C++ Code Conventions

> [!IMPORTANT]
> Any convention or style is technically permitted as long as it can be justified. The current "style" is mostly "raw C++", or even something akin to "raw C". 

Migration to a more modern C++ style may be desirable, but RAII/smart pointers probably won't mix well with the lifetime of most objects actually being controlled on the managed side.

## Misc.

### Resource Types

Resource types are any type that implements `IResource`/`IDisposableResource<T>`/etc.

* When creating a new resource type, please update any relevant integration tests that test the disposal and dependency protections of all resource types.

* Resource types do not expose `IsDisposed` publicly because they ultimately wrap a pointer or native-sized integer. A resource *could* be disposed and then a new one created in its original memory address, meaning the first would appear to be "revived" from the user's POV. Users should track their own disposal on top of the resource objects API.

### "Entity"

We deliberately avoid using the word "Entity" anywhere in the public API as we:

1. Want people to be able to write their own ECS on top of TinyFFR and therefore don't want to "muddy" the namespace,
2. Aren't really offering any kind of actual entity system.