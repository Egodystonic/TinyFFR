using System;

namespace Egodystonic.TinyFFR.Resources;

/// <summary>
/// Base interface for all resource implementation providers.
/// </summary>
/// <remarks>
/// Implementation providers are the actual "meat" of what makes any given resource 'work'.
/// </remarks>
public interface IResourceImplProvider {
	/// <summary>
	/// Invoked via every <see cref="IResource"/>'s own <see cref="IStringSpanNameEnabled.GetNameAsNewStringObject"/> implementation.
	/// </summary>
	string GetNameAsNewStringObject(ResourceHandle handle);
	/// <summary>
	/// Invoked via every <see cref="IResource"/>'s own <see cref="IStringSpanNameEnabled.GetNameLength"/> implementation.
	/// </summary>
	int GetNameLength(ResourceHandle handle);
	/// <summary>
	/// Invoked via every <see cref="IResource"/>'s own <see cref="IStringSpanNameEnabled.CopyName"/> implementation.
	/// </summary>
	void CopyName(ResourceHandle handle, Span<char> destinationBuffer);
}
/// <summary>
/// The strongly-typed counterpart to <see cref="IResourceImplProvider"/>, for implementation providers backing a specific resource type <typeparamref name="TResource"/>.
/// </summary>
/// <typeparam name="TResource">The resource type this implementation provider backs.</typeparam>
public interface IResourceImplProvider<TResource> : IResourceImplProvider where TResource : IResource<TResource> {
	/// <inheritdoc cref="IResourceImplProvider.GetNameAsNewStringObject"/>
	string GetNameAsNewStringObject(ResourceHandle<TResource> handle);
	/// <inheritdoc cref="IResourceImplProvider.GetNameLength"/>
	int GetNameLength(ResourceHandle<TResource> handle);
	/// <inheritdoc cref="IResourceImplProvider.CopyName"/>
	void CopyName(ResourceHandle<TResource> handle, Span<char> destinationBuffer);

	string IResourceImplProvider.GetNameAsNewStringObject(ResourceHandle handle) => GetNameAsNewStringObject((ResourceHandle<TResource>) handle);
	int IResourceImplProvider.GetNameLength(ResourceHandle handle) => GetNameLength((ResourceHandle<TResource>) handle);
	void IResourceImplProvider.CopyName(ResourceHandle handle, Span<char> destinationBuffer) => CopyName((ResourceHandle<TResource>) handle, destinationBuffer);
}



/// <inheritdoc cref="IResourceImplProvider"/>
/// <remarks>
/// This type additionally provides members backing <see cref="IDisposableResource"/>'s disposal-related members.
/// </remarks>
public interface IDisposableResourceImplProvider : IResourceImplProvider {
	/// <summary>
	/// Invoked internally to determine whether a given <see cref="IDisposableResource"/> has already been disposed.
	/// </summary>
	bool IsDisposed(ResourceHandle handle);
	/// <summary>
	/// Invoked via every <see cref="IDisposableResource"/>'s own <see cref="IDisposable.Dispose"/> implementation.
	/// </summary>
	void Dispose(ResourceHandle handle);
}
/// <inheritdoc cref="IResourceImplProvider{TResource}"/>
/// <remarks>
/// This type additionally provides members backing <see cref="IDisposableResource"/>'s disposal-related members.
/// </remarks>
/// <typeparam name="TResource">The resource type this implementation provider backs.</typeparam>
public interface IDisposableResourceImplProvider<TResource> : IDisposableResourceImplProvider, IResourceImplProvider<TResource> where TResource : IResource<TResource> {
	/// <inheritdoc cref="IDisposableResourceImplProvider.IsDisposed"/>
	bool IsDisposed(ResourceHandle<TResource> handle);
	/// <inheritdoc cref="IDisposableResourceImplProvider.Dispose"/>
	void Dispose(ResourceHandle<TResource> handle);

	bool IDisposableResourceImplProvider.IsDisposed(ResourceHandle handle) => IsDisposed((ResourceHandle<TResource>) handle);
	void IDisposableResourceImplProvider.Dispose(ResourceHandle handle) => Dispose((ResourceHandle<TResource>) handle);
}