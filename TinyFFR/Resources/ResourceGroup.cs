// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using static Egodystonic.TinyFFR.Resources.IResourceGroupImplProvider;

namespace Egodystonic.TinyFFR.Resources;

public readonly struct ResourceGroup : IDisposableResource<ResourceGroup, ResourceGroupHandle, IResourceGroupImplProvider> {
	readonly ResourceGroupHandle _handle;
	readonly IResourceGroupImplProvider _impl;

	internal ResourceGroupHandle Handle => IsDisposed ? throw new ObjectDisposedException(nameof(ResourceGroup)) : _handle;
	internal IResourceGroupImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<ResourceGroupHandle>();

	IResourceGroupImplProvider IResource<ResourceGroupHandle, IResourceGroupImplProvider>.Implementation => Implementation;
	ResourceGroupHandle IResource<ResourceGroupHandle, IResourceGroupImplProvider>.Handle => Handle;

	public int ResourceCount {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetResourceCount(Handle);
	}

	public bool IsSealed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsSealed(Handle);
	}

	public bool DisposesContainedResourcesByDefaultWhenDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetDisposesContainedResourcesByDefaultWhenDisposed(Handle);
	}

	public ReadOnlySpan<char> Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(Handle);
	}

	#region Specific Resource Enumeration Properties
	public TypedReferentIterator<EnumerationInput, Material> Materials => GetAllResourcesOfType<Material>();
	public TypedReferentIterator<EnumerationInput, Texture> Textures => GetAllResourcesOfType<Texture>();
	public TypedReferentIterator<EnumerationInput, Mesh> Meshes => GetAllResourcesOfType<Mesh>();
	#endregion

	internal ReadOnlySpan<ResourceStub> Resources {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetResources(Handle);
	}

	internal ResourceGroup(ResourceGroupHandle handle, IResourceGroupImplProvider impl) {
		ArgumentNullException.ThrowIfNull(impl);
		_handle = handle;
		_impl = impl;
	}

	static ResourceGroup IResource<ResourceGroup>.RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl) {
		return new(rawHandle, impl as IResourceGroupImplProvider ?? throw new ArgumentException($"Impl was '{impl}'.", nameof(impl)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AddResource<TResource>(TResource resource) where TResource : IResource => Implementation.AddResource(Handle, resource);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Seal() => Implementation.Seal(Handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TypedReferentIterator<EnumerationInput, TResource> GetAllResourcesOfType<TResource>() where TResource : IResource<TResource> {
		return Implementation.GetAllResourcesOfType<TResource>(Handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TResource GetNthResourceOfType<TResource>(int index) where TResource : IResource<TResource> {
		return Implementation.GetNthResourceOfType<TResource>(Handle, index);
	}

	#region Disposal
	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose(bool disposeContainedResources) => Implementation.Dispose(_handle, disposeContainedResources);
	#endregion

	public override string ToString() => IsDisposed ? $"{nameof(ResourceGroup)} (Disposed)" : $"{nameof(ResourceGroup)} \"{Name}\"";

	#region Equality
	public bool Equals(ResourceGroup other) => _handle == other._handle && _impl == other._impl;
	public override bool Equals(object? obj) => obj is ResourceGroup other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(ResourceGroup left, ResourceGroup right) => left.Equals(right);
	public static bool operator !=(ResourceGroup left, ResourceGroup right) => !left.Equals(right);
	#endregion
}