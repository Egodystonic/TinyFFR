// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;
using static Egodystonic.TinyFFR.Resources.IResourceGroupImplProvider;

namespace Egodystonic.TinyFFR.Resources;

/// <summary>
/// Represents a group of tightly-related resources.
/// </summary>
/// <remarks>
/// <para>
/// Resource groups are meant for when you wish to group/relate small bundles of strongly-associated resources (e.g. a mesh and material that make up a model).
/// They are not designed for storing large lists of resources and you may suffer performance penalties when using them this way.
/// If you need broader "collection-like" functionality you could instead consider array-pool-backed collections exposed via the <see cref="IResourceAllocator">resource allocator</see>.
/// </para>
/// <para>
/// Also: Resource groups create dependencies on the resources added to them, meaning you can not dispose a resource that's part of a group before firstly disposing the group.
/// This is by design and makes sense when using groups for their intended purpose to "collate" or "tightly-group" related assets.
/// </para>
/// <para>
/// The ResourceGroup is itself a resource (and can even be added to another resource group). Like all other resources it is just a handle + implementation reference and is cheap to copy/pass around.
/// </para>
/// </remarks>
public readonly struct ResourceGroup : IDisposableResource<ResourceGroup, IResourceGroupImplProvider> {
	readonly ResourceHandle<ResourceGroup> _handle;
	readonly IResourceGroupImplProvider _impl;

	internal ResourceHandle<ResourceGroup> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(ResourceGroup)) : _handle;
	internal IResourceGroupImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<ResourceGroup>();

	IResourceGroupImplProvider IResource<ResourceGroup, IResourceGroupImplProvider>.Implementation => Implementation;
	ResourceHandle<ResourceGroup> IResource<ResourceGroup>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// The number of resources currently added to this group.
	/// </summary>
	public int ResourceCount {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetResourceCount(Handle);
	}

	/// <summary>
	/// Whether this group has been sealed (see <see cref="Seal"/>), after which no further resources may be added to it.
	/// </summary>
	public bool IsSealed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsSealed(Handle);
	}

	/// <summary>
	/// Whether calling the parameterless <see cref="Dispose()"/> on this group will also dispose every resource currently contained within it.
	/// </summary>
	/// <remarks>
	/// This is fixed at the point the group was created (see <see cref="IResourceAllocator.CreateResourceGroup(bool)"/>); use the explicit <see cref="Dispose(bool)"/> overload if you want to override this behaviour for a single disposal.
	/// </remarks>
	public bool DisposesContainedResourcesByDefaultWhenDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetDisposesContainedResourcesByDefaultWhenDisposed(Handle);
	}

	#region Specific Resource Enumeration Properties
	/// <summary>
	/// All currently-live <see cref="ApplicationLoop"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;ApplicationLoop&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, ApplicationLoop> ApplicationLoops => GetAllResourcesOfType<ApplicationLoop>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, BackdropTexture> BackdropTextures => GetAllResourcesOfType<BackdropTexture>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, Camera> Cameras => GetAllResourcesOfType<Camera>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, DirectionalLight> DirectionalLights => GetAllResourcesOfType<DirectionalLight>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, Display> Displays => GetAllResourcesOfType<Display>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, Font> Fonts => GetAllResourcesOfType<Font>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, Material> Materials => GetAllResourcesOfType<Material>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, Mesh> Meshes => GetAllResourcesOfType<Mesh>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, MeshAnimation> MeshAnimations => GetAllResourcesOfType<MeshAnimation>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, MeshNode> MeshNodes => GetAllResourcesOfType<MeshNode>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, Model> Models => GetAllResourcesOfType<Model>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, ModelInstance> ModelInstances => GetAllResourcesOfType<ModelInstance>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, PointLight> PointLights => GetAllResourcesOfType<PointLight>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, Renderer> Renderers => GetAllResourcesOfType<Renderer>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, RendererCompositor> RendererCompositors => GetAllResourcesOfType<RendererCompositor>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, RenderOutputBuffer> RenderOutputBuffers => GetAllResourcesOfType<RenderOutputBuffer>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, ResourceGroup> ResourceGroups => GetAllResourcesOfType<ResourceGroup>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, Scene> Scenes => GetAllResourcesOfType<Scene>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, SpotLight> SpotLights => GetAllResourcesOfType<SpotLight>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, Texture> Textures => GetAllResourcesOfType<Texture>();
	/// <inheritdoc cref="ApplicationLoops"/>
	public IndirectEnumerable<EnumerationInput, Window> Windows => GetAllResourcesOfType<Window>();

	/// <summary>
	/// All currently-live <see cref="QuadMesh"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;QuadMesh, Mesh&gt;()</c>.
	/// </summary>
	/// <remarks>
	/// <see cref="QuadMesh"/> is a specialization of <see cref="Mesh"/> (see <see cref="IResourceSpecialization{TSelf,TBase}"/>); this property only returns <see cref="Mesh"/>es that were added to the group specifically as a <see cref="QuadMesh"/> (via <see cref="Add(QuadMesh)"/>), not every <see cref="Mesh"/> in <see cref="Meshes"/>.
	/// </remarks>
	public IndirectEnumerable<EnumerationInput, QuadMesh> QuadMeshes => GetAllResourcesOfType<QuadMesh, Mesh>();
	/// <inheritdoc cref="QuadMeshes"/>
	public IndirectEnumerable<EnumerationInput, QuadInstance> QuadInstances => GetAllResourcesOfType<QuadInstance, ModelInstance>();
	/// <inheritdoc cref="QuadMeshes"/>
	public IndirectEnumerable<EnumerationInput, CameraLockedQuadInstance> CameraLockedQuadInstances => GetAllResourcesOfType<CameraLockedQuadInstance, ModelInstance>();
	/// <inheritdoc cref="QuadMeshes"/>
	public IndirectEnumerable<EnumerationInput, TextInstance> TextInstances => GetAllResourcesOfType<TextInstance, ModelInstance>();
	/// <inheritdoc cref="QuadMeshes"/>
	public IndirectEnumerable<EnumerationInput, CameraLockedTextInstance> CameraLockedTextInstances => GetAllResourcesOfType<CameraLockedTextInstance, ModelInstance>();
	/// <inheritdoc cref="QuadMeshes"/>
	public IndirectEnumerable<EnumerationInput, FontString> FontStrings => GetAllResourcesOfType<FontString, Font>();
	/// <inheritdoc cref="QuadMeshes"/>
	public IndirectEnumerable<EnumerationInput, FontPen> FontPens => GetAllResourcesOfType<FontPen, Font>();
	/// <inheritdoc cref="QuadMeshes"/>
	public IndirectEnumerable<EnumerationInput, CanvasScene> CanvasScenes => GetAllResourcesOfType<CanvasScene, Scene>();
	/// <inheritdoc cref="QuadMeshes"/>
	public IndirectEnumerable<EnumerationInput, CanvasText> CanvasTexts => GetAllResourcesOfType<CanvasText, ModelInstance>();
	/// <inheritdoc cref="QuadMeshes"/>
	public IndirectEnumerable<EnumerationInput, CanvasTexture> CanvasTextures => GetAllResourcesOfType<CanvasTexture, ModelInstance>();
	#endregion

	internal ResourceGroup(ResourceHandle<ResourceGroup> handle, IResourceGroupImplProvider impl) {
		ArgumentNullException.ThrowIfNull(impl);
		_handle = handle;
		_impl = impl;
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static ResourceGroup IResource<ResourceGroup>.CreateFromHandleAndImpl(ResourceHandle<ResourceGroup> handle, IResourceImplProvider impl) {
		return new(handle, impl as IResourceGroupImplProvider ?? throw new ArgumentException($"Impl was '{impl}'.", nameof(impl)));
	}
	/// <summary>
	/// Returns this group's handle without first checking whether it has already been disposed.
	/// </summary>
	/// <remarks>
	/// This is primarily intended for internal library use in situations where validity has already been established some other way; prefer using this <see cref="ResourceGroup"/> value directly in most cases, as its other members all check for disposal first.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ResourceHandle<ResourceGroup> GetHandleWithoutDisposeCheck() => _handle;

	/// <summary>
	/// Adds <paramref name="resource"/> to this group.
	/// </summary>
	/// <remarks>
	/// This creates a dependency from this group on <paramref name="resource"/>: you can not dispose <paramref name="resource"/> while it remains part of this (non-disposed) group.
	/// </remarks>
	/// <param name="resource">The resource to add.</param>
	/// <exception cref="ResourceGroupSealedException">Thrown if this group has already been sealed (see <see cref="Seal"/>).</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add<TResource>(TResource resource) where TResource : IResource => Implementation.AddResource(Handle, resource);

	/// <summary>
	/// Adds <paramref name="resource"/> (a specialization of <typeparamref name="TBase"/>, see <see cref="IResourceSpecialization{TSelf,TBase}"/>) to this group.
	/// </summary>
	/// <remarks>
	/// This creates a dependency from this group on the underlying <typeparamref name="TBase"/> resource that <paramref name="resource"/> specializes: you can not dispose that underlying resource while <paramref name="resource"/> remains part of this (non-disposed) group.
	/// </remarks>
	/// <param name="resource">The resource to add.</param>
	/// <exception cref="ResourceGroupSealedException">Thrown if this group has already been sealed (see <see cref="Seal"/>).</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add<TResource, TBase>(TResource resource) where TResource : struct, IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase> => Implementation.AddResource<TResource, TBase>(Handle, resource);

	/// <inheritdoc cref="Add{TResource,TBase}"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(QuadMesh resource) => Add<QuadMesh, Mesh>(resource);

	/// <inheritdoc cref="Add{TResource,TBase}"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(QuadInstance resource) => Add<QuadInstance, ModelInstance>(resource);

	/// <inheritdoc cref="Add{TResource,TBase}"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CameraLockedQuadInstance resource) => Add<CameraLockedQuadInstance, ModelInstance>(resource);

	/// <inheritdoc cref="Add{TResource,TBase}"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(TextInstance resource) => Add<TextInstance, ModelInstance>(resource);

	/// <inheritdoc cref="Add{TResource,TBase}"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CameraLockedTextInstance resource) => Add<CameraLockedTextInstance, ModelInstance>(resource);

	/// <inheritdoc cref="Add{TResource,TBase}"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(FontString resource) => Add<FontString, Font>(resource);

	/// <inheritdoc cref="Add{TResource,TBase}"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(FontPen resource) => Add<FontPen, Font>(resource);

	/// <inheritdoc cref="Add{TResource,TBase}"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CanvasScene resource) => Add<CanvasScene, Scene>(resource);

	/// <inheritdoc cref="Add{TResource,TBase}"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CanvasText resource) => Add<CanvasText, ModelInstance>(resource);

	/// <inheritdoc cref="Add{TResource,TBase}"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CanvasTexture resource) => Add<CanvasTexture, ModelInstance>(resource);

	/// <summary>
	/// Prevents any further resources from being added to this group.
	/// </summary>
	/// <remarks>
	/// This can not be undone. Attempting to <see cref="Add{TResource}"/> a resource to a sealed group throws <see cref="ResourceGroupSealedException"/>.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Seal() => Implementation.Seal(Handle);

	/// <summary>
	/// Returns every resource currently added to this group, boxed as <see cref="object"/>.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="GetAllResourcesOfType{TResource}"/> and the typed enumeration properties (e.g. <see cref="Meshes"/>), this returns resources of every type in the group at once, at the cost of boxing each one.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IReadOnlyCollection<object> GetAllResourcesBoxed() => Implementation.GetAllResourcesBoxed(Handle);

	/// <summary>
	/// Returns every currently-live <typeparamref name="TResource"/> in this group.
	/// </summary>
	/// <remarks>
	/// This is the general-purpose form of the typed enumeration properties above (e.g. <see cref="Meshes"/> is equivalent to <c>GetAllResourcesOfType&lt;Mesh&gt;()</c>).
	/// </remarks>
	/// <typeparam name="TResource">The type of resource to enumerate.</typeparam>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IndirectEnumerable<EnumerationInput, TResource> GetAllResourcesOfType<TResource>() where TResource : IResource<TResource> {
		return Implementation.GetAllResourcesOfType<TResource>(Handle);
	}

	/// <summary>
	/// Returns every currently-live <typeparamref name="TResource"/> (a specialization of <typeparamref name="TBase"/>) in this group.
	/// </summary>
	/// <remarks>
	/// This is the general-purpose form of the specialization-typed enumeration properties above (e.g. <see cref="QuadMeshes"/> is equivalent to <c>GetAllResourcesOfType&lt;QuadMesh, Mesh&gt;()</c>).
	/// </remarks>
	/// <typeparam name="TResource">The specialized type of resource to enumerate.</typeparam>
	/// <typeparam name="TBase">The underlying resource type that <typeparamref name="TResource"/> specializes.</typeparam>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IndirectEnumerable<EnumerationInput, TResource> GetAllResourcesOfType<TResource, TBase>() where TResource : struct, IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase> {
		return Implementation.GetAllResourcesOfType<TResource, TBase>(Handle);
	}

	/// <summary>
	/// Returns the <typeparamref name="TResource"/> at <paramref name="index"/> in this group, using the same ordering as <see cref="GetAllResourcesOfType{TResource}"/>.
	/// </summary>
	/// <param name="index">The index of the resource to retrieve, among only the <typeparamref name="TResource"/>s in this group.</param>
	/// <typeparam name="TResource">The type of resource to retrieve.</typeparam>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative, or greater than or equal to the number of <typeparamref name="TResource"/>s in this group.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TResource GetNthResourceOfType<TResource>(int index) where TResource : IResource<TResource> {
		return Implementation.GetNthResourceOfType<TResource>(Handle, index);
	}

	/// <summary>
	/// Returns the <typeparamref name="TResource"/> (a specialization of <typeparamref name="TBase"/>) at <paramref name="index"/> in this group, using the same ordering as <see cref="GetAllResourcesOfType{TResource,TBase}"/>.
	/// </summary>
	/// <param name="index">The index of the resource to retrieve, among only the <typeparamref name="TResource"/>s in this group.</param>
	/// <typeparam name="TResource">The specialized type of resource to retrieve.</typeparam>
	/// <typeparam name="TBase">The underlying resource type that <typeparamref name="TResource"/> specializes.</typeparam>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative, or greater than or equal to the number of <typeparamref name="TResource"/>s in this group.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TResource GetNthResourceOfType<TResource, TBase>(int index) where TResource : struct, IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase> {
		return Implementation.GetNthResourceOfType<TResource, TBase>(Handle, index);
	}

	#region Disposal
	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}

	/// <summary>
	/// Disposes this group, using <see cref="DisposesContainedResourcesByDefaultWhenDisposed"/> to determine whether resources currently in the group are also disposed.
	/// </summary>
	/// <remarks>
	/// Use <see cref="Dispose(bool)"/> instead if you want to explicitly choose whether contained resources are disposed, regardless of <see cref="DisposesContainedResourcesByDefaultWhenDisposed"/>.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	/// <summary>
	/// Disposes this group, explicitly specifying whether resources currently in the group should also be disposed.
	/// </summary>
	/// <param name="disposeContainedResources">If <see langword="true"/>, every resource currently in the group is also disposed. If <see langword="false"/>, contained resources are left untouched (and this group's dependency on them is released).</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose(bool disposeContainedResources) => Implementation.Dispose(_handle, disposeContainedResources);
	#endregion

	/// <inheritdoc/>
	public override string ToString() => IsDisposed ? $"Resource Group (Disposed)" : $"Resource Group \"{GetNameAsNewStringObject()}\"";

	#region Equality
	/// <inheritdoc/>
	public bool Equals(ResourceGroup other) => _handle == other._handle && _impl == other._impl;
	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj is ResourceGroup other && Equals(other);
	/// <inheritdoc/>
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <inheritdoc/>
	public static bool operator ==(ResourceGroup left, ResourceGroup right) => left.Equals(right);
	/// <inheritdoc/>
	public static bool operator !=(ResourceGroup left, ResourceGroup right) => !left.Equals(right);
	#endregion
}