// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.World;
using static Egodystonic.TinyFFR.Resources.IResourceGroupImplProvider;

namespace Egodystonic.TinyFFR.Resources;

public readonly struct ResourceGroup : IDisposableResource<ResourceGroup, IResourceGroupImplProvider> {
	readonly ResourceHandle<ResourceGroup> _handle;
	readonly IResourceGroupImplProvider _impl;

	internal ResourceHandle<ResourceGroup> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(ResourceGroup)) : _handle;
	internal IResourceGroupImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<ResourceGroup>();

	IResourceGroupImplProvider IResource<ResourceGroup, IResourceGroupImplProvider>.Implementation => Implementation;
	ResourceHandle<ResourceGroup> IResource<ResourceGroup>.Handle => Handle;

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

	#region Specific Resource Enumeration Properties
	// Maintainer's note: We don't provide one of these for every resource type, only those that we feel are likely to be grouped.
	// Users can still use GetAllResourcesOfType for any resource type. These are just a convenience shortcut.
	public TypedReferentIterator<EnumerationInput, Material> Materials => GetAllResourcesOfType<Material>();
	public TypedReferentIterator<EnumerationInput, Texture> Textures => GetAllResourcesOfType<Texture>();
	public TypedReferentIterator<EnumerationInput, Mesh> Meshes => GetAllResourcesOfType<Mesh>();
	public TypedReferentIterator<EnumerationInput, ModelInstance> ModelInstances => GetAllResourcesOfType<ModelInstance>();
	#endregion

	internal ReadOnlySpan<ResourceStub> Resources {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetResources(Handle);
	}

	internal ResourceGroup(ResourceHandle<ResourceGroup> handle, IResourceGroupImplProvider impl) {
		ArgumentNullException.ThrowIfNull(impl);
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static ResourceGroup IResource<ResourceGroup>.CreateFromHandleAndImpl(ResourceHandle<ResourceGroup> handle, IResourceImplProvider impl) {
		return new(handle, impl as IResourceGroupImplProvider ?? throw new ArgumentException($"Impl was '{impl}'.", nameof(impl)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add<TResource>(TResource resource) where TResource : IResource => Implementation.AddResource(Handle, resource);

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

	public override string ToString() => IsDisposed ? $"Resource Group (Disposed)" : $"Resource Group \"{GetNameAsNewStringObject()}\"";

	#region Equality
	public bool Equals(ResourceGroup other) => _handle == other._handle && _impl == other._impl;
	public override bool Equals(object? obj) => obj is ResourceGroup other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(ResourceGroup left, ResourceGroup right) => left.Equals(right);
	public static bool operator !=(ResourceGroup left, ResourceGroup right) => !left.Equals(right);
	#endregion
}