// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

/// <summary>
/// The untyped counterpart to <see cref="ResourceHandle{TResource}"/>: the basic, core identifier of a resource, without reference to which resource type it belongs to.
/// </summary>
/// <remarks>
/// This handle only has meaning to the <see cref="IResourceImplProvider"/> that created it, and most user code should not need to use it.
/// </remarks>
public readonly unsafe struct ResourceHandle : IEquatable<ResourceHandle> {
	/// <summary>
	/// This handle's raw value, as an unsigned integer.
	/// </summary>
	public nuint AsInteger { get; }
	/// <summary>
	/// This handle's raw value, as an untyped pointer.
	/// </summary>
	public void* AsPointer => (void*) AsInteger;

	/// <summary>
	/// Constructs a new <see cref="ResourceHandle"/> from a raw pointer.
	/// </summary>
	/// <param name="val">The value for <see cref="AsPointer"/> (and, equivalently, <see cref="AsInteger"/>).</param>
	public ResourceHandle(void* val) : this((nuint) val) { }
	/// <summary>
	/// Constructs a new <see cref="ResourceHandle"/> from a raw unsigned integer.
	/// </summary>
	/// <param name="val">The value for <see cref="AsInteger"/> (and, equivalently, <see cref="AsPointer"/>).</param>
	public ResourceHandle(nuint val) => AsInteger = val;

	/// <summary>
	/// Converts <paramref name="handle"/> to its raw <see cref="AsInteger"/> value.
	/// </summary>
	/// <param name="handle">The handle to convert.</param>
	public static implicit operator nuint(ResourceHandle handle) => handle.AsInteger;
	/// <summary>
	/// Converts a raw unsigned integer to a <see cref="ResourceHandle"/>.
	/// </summary>
	/// <param name="val">The value to convert.</param>
	public static implicit operator ResourceHandle(nuint val) => new(val);
	/// <summary>
	/// Converts <paramref name="handle"/> to its raw <see cref="AsPointer"/> value.
	/// </summary>
	/// <param name="handle">The handle to convert.</param>
	public static implicit operator void*(ResourceHandle handle) => handle.AsPointer;
	/// <summary>
	/// Converts a raw pointer to a <see cref="ResourceHandle"/>.
	/// </summary>
	/// <param name="val">The value to convert.</param>
	public static implicit operator ResourceHandle(void* val) => new(val);

	/// <inheritdoc/>
	public bool Equals(ResourceHandle other) => AsInteger == other.AsInteger;
	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj is ResourceHandle other && Equals(other);
	/// <inheritdoc/>
	public override int GetHashCode() => AsInteger.GetHashCode();
	/// <summary>
	/// <see cref="Equals(ResourceHandle"/>
	/// </summary>
	public static bool operator ==(ResourceHandle left, ResourceHandle right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(ResourceHandle"/>
	/// </summary>
	public static bool operator !=(ResourceHandle left, ResourceHandle right) => !left.Equals(right);
	/// <inheritdoc/>
	public override string ToString() => $"Untyped Handle 0x{AsInteger:X16}";
}

/// <summary>
/// The basic, core identifier of a resource of type <typeparamref name="TResource"/>.
/// </summary>
/// <remarks>
/// This handle only has meaning to the <see cref="IResourceImplProvider{TResource}"/> that created it, and most user
/// code should not need to use it.
/// </remarks>
/// <typeparam name="TResource">The type of resource this handle pertains to.</typeparam>
public readonly unsafe struct ResourceHandle<TResource> : IEquatable<ResourceHandle<TResource>> where TResource : IResource<TResource> {
	/// <summary>
	/// This handle's raw value, as an unsigned integer.
	/// </summary>
	public nuint AsInteger { get; }
	/// <summary>
	/// This handle's raw value, as an untyped pointer.
	/// </summary>
	public void* AsPointer => (void*) AsInteger;
	internal ResourceIdent Ident => new(TypeHandle, AsInteger);
	/// <summary>
	/// A process-wide identifier for the resource type <typeparamref name="TResource"/> itself (as opposed to <see cref="AsInteger"/>/<see cref="AsPointer"/>, which identify a specific handle instance).
	/// </summary>
	/// <remarks>
	/// This is used internally to check that an untyped <see cref="ResourceHandle"/> actually refers to a resource of type <typeparamref name="TResource"/> before it is cast/converted back to a <see cref="ResourceHandle{TResource}"/>.
	/// </remarks>
	public static nint TypeHandle => typeof(TResource).TypeHandle.Value;

	/// <summary>
	/// Constructs a new <see cref="ResourceHandle{TResource}"/> from a raw pointer.
	/// </summary>
	/// <param name="val">The value for <see cref="AsPointer"/> (and, equivalently, <see cref="AsInteger"/>).</param>
	public ResourceHandle(void* val) : this((nuint) val) { }
	/// <summary>
	/// Constructs a new <see cref="ResourceHandle{TResource}"/> from a raw unsigned integer.
	/// </summary>
	/// <param name="val">The value for <see cref="AsInteger"/> (and, equivalently, <see cref="AsPointer"/>).</param>
	public ResourceHandle(nuint val) => AsInteger = val;

	/// <summary>
	/// Converts <paramref name="handle"/> to its raw <see cref="AsInteger"/> value.
	/// </summary>
	/// <param name="handle">The handle to convert.</param>
	public static implicit operator nuint(ResourceHandle<TResource> handle) => handle.AsInteger;
	/// <summary>
	/// Converts a raw unsigned integer to a <see cref="ResourceHandle{TResource}"/>.
	/// </summary>
	/// <param name="val">The value to convert.</param>
	public static implicit operator ResourceHandle<TResource>(nuint val) => new(val);
	/// <summary>
	/// Converts <paramref name="handle"/> to its raw <see cref="AsPointer"/> value.
	/// </summary>
	/// <param name="handle">The handle to convert.</param>
	public static implicit operator void*(ResourceHandle<TResource> handle) => handle.AsPointer;
	/// <summary>
	/// Converts a raw pointer to a <see cref="ResourceHandle{TResource}"/>.
	/// </summary>
	/// <param name="val">The value to convert.</param>
	public static implicit operator ResourceHandle<TResource>(void* val) => new(val);
	/// <summary>
	/// Converts <paramref name="typedHandle"/> to an untyped <see cref="ResourceHandle"/> referring to the same underlying resource.
	/// </summary>
	/// <param name="typedHandle">The handle to convert.</param>
	public static implicit operator ResourceHandle(ResourceHandle<TResource> typedHandle) => new(typedHandle.AsInteger);
	/// <summary>
	/// Converts <paramref name="untypedHandle"/> back to a <see cref="ResourceHandle{TResource}"/>.
	/// </summary>
	/// <remarks>
	/// This does not verify that <paramref name="untypedHandle"/> actually refers to a resource of type <typeparamref name="TResource"/> (see <see cref="TypeHandle"/>); it is your responsibility to ensure that is the case before converting.
	/// </remarks>
	/// <param name="untypedHandle">The handle to convert.</param>
	public static explicit operator ResourceHandle<TResource>(ResourceHandle untypedHandle) => new(untypedHandle.AsInteger);

	/// <inheritdoc/>
	public bool Equals(ResourceHandle<TResource> other) => AsInteger == other.AsInteger;
	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj is ResourceHandle<TResource> other && Equals(other);
	/// <inheritdoc/>
	public override int GetHashCode() => AsInteger.GetHashCode();
	/// <summary>
	/// <see cref="Equals(ResourceHandle{TResource}"/>
	/// </summary>
	public static bool operator ==(ResourceHandle<TResource> left, ResourceHandle<TResource> right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(ResourceHandle{TResource}"/>
	/// </summary>
	public static bool operator !=(ResourceHandle<TResource> left, ResourceHandle<TResource> right) => !left.Equals(right);

	/// <inheritdoc/>
	public override string ToString() => $"{typeof(TResource).Name} Handle 0x{AsInteger:X16}";
}