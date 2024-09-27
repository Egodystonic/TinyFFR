// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text.RegularExpressions;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Resources.Memory;
using static Egodystonic.TinyFFR.Resources.ICombinedResourceGroupImplProvider;

namespace Egodystonic.TinyFFR.Resources;

public readonly struct CombinedResourceGroup : IHandleImplPairResource<CombinedResourceGroup, CombinedResourceGroupHandle, ICombinedResourceGroupImplProvider> {
	readonly CombinedResourceGroupHandle _handle;
	readonly ICombinedResourceGroupImplProvider _impl;

	public CombinedResourceGroupHandle Handle => _handle;
	public ICombinedResourceGroupImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<CombinedResourceGroup>();
	IHandleImplPairResource.ResourceIdent IHandleImplPairResource.Ident => new(typeof(CombinedResourceGroup).TypeHandle.Value, Handle);

	static CombinedResourceGroup IHandleImplPairResource<CombinedResourceGroup>.RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl) {
		return new(rawHandle, impl as ICombinedResourceGroupImplProvider ?? throw new ArgumentException($"Impl was '{impl}'.", nameof(impl)));
	}

	public int ResourceCount {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetResourceCount(Handle);
	}

	public int ResourceCapacity {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetResourceCapacity(Handle);
	}

	public string Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(Handle);
	}

	internal CombinedResourceGroup(CombinedResourceGroupHandle handle, ICombinedResourceGroupImplProvider impl) {
		ArgumentNullException.ThrowIfNull(impl);
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AddResource<TResource>(TResource resource) where TResource : IHandleImplPairResource => Implementation.AddResource(Handle, resource);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public OneToManyEnumerator<EnumerationArg, TResource> GetAllResourcesOfType<TResource>() where TResource : IHandleImplPairResource<TResource> {
		return Implementation.GetAllResourcesOfType<TResource>(Handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameUsingSpan(Span<char> dest) => Implementation.GetNameUsingSpan(_handle, dest);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameSpanLength() => Implementation.GetNameSpanLength(_handle);

	public override string ToString() => IsDisposed ? $"{nameof(CombinedResourceGroup)} (Disposed)" : $"{nameof(CombinedResourceGroup)} \"{Name}\"";

	#region Disposal
	bool IsDisposed => Implementation.IsDisposed(_handle);
	public void Dispose() => Implementation.Dispose(_handle);

	internal void ThrowIfInvalid() => InvalidObjectException.ThrowIfDefault(this);
	#endregion

	#region Equality
	public bool Equals(CombinedResourceGroup other) => _handle == other._handle && _impl == other._impl;
	public override bool Equals(object? obj) => obj is CombinedResourceGroup other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(CombinedResourceGroup left, CombinedResourceGroup right) => left.Equals(right);
	public static bool operator !=(CombinedResourceGroup left, CombinedResourceGroup right) => !left.Equals(right);
	#endregion
}