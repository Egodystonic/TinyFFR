// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using static Egodystonic.TinyFFR.Resources.ICombinedResourceGroupImplProvider;

namespace Egodystonic.TinyFFR.Resources;

public readonly struct CombinedResourceGroup : IDisposableResource<CombinedResourceGroup, CombinedResourceGroupHandle, ICombinedResourceGroupImplProvider> {
	readonly CombinedResourceGroupHandle _handle;
	readonly ICombinedResourceGroupImplProvider _impl;

	internal CombinedResourceGroupHandle Handle => IsDisposed ? throw new ObjectDisposedException(nameof(CombinedResourceGroup)) : _handle;
	internal ICombinedResourceGroupImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<CombinedResourceGroupHandle>();

	ICombinedResourceGroupImplProvider IResource<CombinedResourceGroupHandle, ICombinedResourceGroupImplProvider>.Implementation => Implementation;
	CombinedResourceGroupHandle IResource<CombinedResourceGroupHandle, ICombinedResourceGroupImplProvider>.Handle => Handle;

	public int ResourceCount {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetResourceCount(Handle);
	}

	public bool IsSealed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsSealed(Handle);
	}

	public bool DisposedContainedResourcesByDefaultWhenDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.DisposedContainedResourcesByDefaultWhenDisposed(Handle);
	}

	public ReadOnlySpan<char> Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(Handle);
	}

	internal ReadOnlySpan<ResourceStub> Resources {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetResources(Handle);
	}

	internal CombinedResourceGroup(CombinedResourceGroupHandle handle, ICombinedResourceGroupImplProvider impl) {
		ArgumentNullException.ThrowIfNull(impl);
		_handle = handle;
		_impl = impl;
	}

	static CombinedResourceGroup IResource<CombinedResourceGroup>.RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl) {
		return new(rawHandle, impl as ICombinedResourceGroupImplProvider ?? throw new ArgumentException($"Impl was '{impl}'.", nameof(impl)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AddResource<TResource>(TResource resource) where TResource : IResource => Implementation.AddResource(Handle, resource);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Seal() => Implementation.Seal(Handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public OneToManyEnumerator<EnumerationArg, TResource> GetAllResourcesOfType<TResource>() where TResource : IResource<TResource> {
		return Implementation.GetAllResourcesOfType<TResource>(Handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TResource GetNthResourceOfType<TResource>(int index) where TResource : IResource<TResource> {
		return Implementation.GetNthResourceOfType<TResource>(Handle, index);
	}

	#region Disposal
	public bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose(bool disposeContainedResources) => Implementation.Dispose(_handle, disposeContainedResources);
	#endregion

	public override string ToString() => IsDisposed ? $"{nameof(CombinedResourceGroup)} (Disposed)" : $"{nameof(CombinedResourceGroup)} \"{Name}\"";

	#region Equality
	public bool Equals(CombinedResourceGroup other) => _handle == other._handle && _impl == other._impl;
	public override bool Equals(object? obj) => obj is CombinedResourceGroup other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(CombinedResourceGroup left, CombinedResourceGroup right) => left.Equals(right);
	public static bool operator !=(CombinedResourceGroup left, CombinedResourceGroup right) => !left.Equals(right);
	#endregion
}