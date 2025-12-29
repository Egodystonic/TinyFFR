---
title: IndirectEnumerable&lt;TIn, TOut&gt;
---

## Summary

### Plain-English

An instance of a `IndirectEnumerable<TIn, TOut>` lets you enumerate, count, and copy all of the `TOut`s in a `TIn` according to some property/function.

In most cases you can use this type like a pseudo-collection; it implements `IEnumerable<TOut>`.

### Further Detail

This type is designed to take an input type `TIn` and allow garbage-free iteration over some property or facet of that type, yielding a sequence of `TOut` values, without exposing the memory or mechanism of generation for those values (which in some cases may be unmanaged or ad-hoc). In other words, it is something that allows you to iterate over values of a certain type belonging to a referent (i.e. the owning object being referred to).

## Iteration

The simplest way to use this type is via a `foreach` loop. The `TOut` type is the element type in the enumeration:

```csharp
// CurrentlyPressedKeys returns a IndirectEnumerable<ILatestKeyboardAndMouseInputRetriever, KeyboardOrMouseKey>
foreach (var key in loop.Input.KeyboardAndMouse.CurrentlyPressedKeys) {
	// 'key' will be a TOut (KeyboardOrMouseKey)
}
```

## Count & Indexing

You can also index directly and/or count the number of elements in the referent:

```csharp
var firstKey = loop.Input.KeyboardAndMouse.CurrentlyPressedKeys[0];
var numKeys = loop.Input.KeyboardAndMouse.CurrentlyPressedKeys.Count;
```

Supplying an index below 0 or `>= Count` will result in `ArgumentOutOfRangeException` being thrown (just like with any collection type).

## Copying

You can also copy the entire set of values to a `Span<TOut>`:

```csharp
// Copies all values to `mySpan`
loop.Input.KeyboardAndMouse.CurrentlyPressedKeys.CopyTo(mySpan);

// Checks whether `mySpan` is large enough to hold all values first
// Returns false if no copy was made (i.e. `mySpan` was too small)
// Returns true if all values were copied
var success = loop.Input.KeyboardAndMouse.CurrentlyPressedKeys.TryCopyTo(mySpan);
```

## Invalidation

Note that, like how iterators/enumerators maintain a "collection version", a `IndirectEnumerable<TIn, TOut>` maintains an internal "referent version". If you modify the owning `TIn` reference your iterator will be invalidated, and attempting to access any of its functions will result in an exception being thrown:

```csharp
var currentKeys = loop.Input.KeyboardAndMouse.CurrentlyPressedKeys;

// Iterating the loop invalidates "currentKeys", because the
// TIn referent (the ILatestKeyboardAndMouseInputRetriever) has been modified
_ = loop.IterateOnce();

// ==== Every line below this will throw an exception unless we get a new currentKeys ====
foreach (var key in currentKeys) { }
_ = currentKeys.GetEnumerator();
_ = currentKeys.Count;
_ = currentKeys[0];
_ = currentKeys.ElementAt(0);
currentKeys.CopyTo(someSpan);
currentKeys.TryCopyTo(someSpan);
```
