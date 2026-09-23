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
/// You can create a ResourceGroup via the factory's <see cref="IResourceAllocator"/>.
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
	/// <summary>
	/// All currently-live <see cref="BackdropTexture"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;BackdropTexture&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, BackdropTexture> BackdropTextures => GetAllResourcesOfType<BackdropTexture>();
	/// <summary>
	/// All currently-live <see cref="Camera"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;Camera&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, Camera> Cameras => GetAllResourcesOfType<Camera>();
	/// <summary>
	/// All currently-live <see cref="DirectionalLight"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;DirectionalLight&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, DirectionalLight> DirectionalLights => GetAllResourcesOfType<DirectionalLight>();
	/// <summary>
	/// All currently-live <see cref="Display"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;Display&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, Display> Displays => GetAllResourcesOfType<Display>();
	/// <summary>
	/// All currently-live <see cref="Font"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;Font&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, Font> Fonts => GetAllResourcesOfType<Font>();
	/// <summary>
	/// All currently-live <see cref="Material"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;Material&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, Material> Materials => GetAllResourcesOfType<Material>();
	/// <summary>
	/// All currently-live <see cref="Mesh"/>es in this group; equivalent to <c>GetAllResourcesOfType&lt;Mesh&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, Mesh> Meshes => GetAllResourcesOfType<Mesh>();
	/// <summary>
	/// All currently-live <see cref="MeshAnimation"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;MeshAnimation&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, MeshAnimation> MeshAnimations => GetAllResourcesOfType<MeshAnimation>();
	/// <summary>
	/// All currently-live <see cref="MeshNode"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;MeshNode&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, MeshNode> MeshNodes => GetAllResourcesOfType<MeshNode>();
	/// <summary>
	/// All currently-live <see cref="Model"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;Model&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, Model> Models => GetAllResourcesOfType<Model>();
	/// <summary>
	/// All currently-live <see cref="ModelInstance"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;ModelInstance&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, ModelInstance> ModelInstances => GetAllResourcesOfType<ModelInstance>();
	/// <summary>
	/// All currently-live <see cref="PointLight"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;PointLight&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, PointLight> PointLights => GetAllResourcesOfType<PointLight>();
	/// <summary>
	/// All currently-live <see cref="Renderer"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;Renderer&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, Renderer> Renderers => GetAllResourcesOfType<Renderer>();
	/// <summary>
	/// All currently-live <see cref="RendererCompositor"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;RendererCompositor&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, RendererCompositor> RendererCompositors => GetAllResourcesOfType<RendererCompositor>();
	/// <summary>
	/// All currently-live <see cref="RenderOutputBuffer"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;RenderOutputBuffer&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, RenderOutputBuffer> RenderOutputBuffers => GetAllResourcesOfType<RenderOutputBuffer>();
	/// <summary>
	/// All currently-live <see cref="ResourceGroup"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;ResourceGroup&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, ResourceGroup> ResourceGroups => GetAllResourcesOfType<ResourceGroup>();
	/// <summary>
	/// All currently-live <see cref="Scene"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;Scene&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, Scene> Scenes => GetAllResourcesOfType<Scene>();
	/// <summary>
	/// All currently-live <see cref="SpotLight"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;SpotLight&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, SpotLight> SpotLights => GetAllResourcesOfType<SpotLight>();
	/// <summary>
	/// All currently-live <see cref="Texture"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;Texture&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, Texture> Textures => GetAllResourcesOfType<Texture>();
	/// <summary>
	/// All currently-live <see cref="Window"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;Window&gt;()</c>.
	/// </summary>
	public IndirectEnumerable<EnumerationInput, Window> Windows => GetAllResourcesOfType<Window>();

	/// <summary>
	/// All currently-live <see cref="QuadMesh"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;QuadMesh, Mesh&gt;()</c>.
	/// </summary>
	/// <remarks>
	/// <see cref="QuadMesh"/> is a specialization of <see cref="Mesh"/> (see <see cref="IResourceSpecialization{TSelf,TBase}"/>); this property only returns <see cref="Mesh"/>es that were added to the group specifically as a <see cref="QuadMesh"/> (via <see cref="Add(QuadMesh)"/>), not every <see cref="Mesh"/> in <see cref="Meshes"/>.
	/// </remarks>
	public IndirectEnumerable<EnumerationInput, QuadMesh> QuadMeshes => GetAllResourcesOfType<QuadMesh, Mesh>();
	/// <summary>
	/// All currently-live <see cref="QuadInstance"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;QuadInstance, ModelInstance&gt;()</c>.
	/// </summary>
	/// <remarks>
	/// <see cref="QuadInstance"/> is a specialization of <see cref="ModelInstance"/> (see <see cref="IResourceSpecialization{TSelf,TBase}"/>); this property only returns <see cref="ModelInstance"/>s that were added to the group specifically as a <see cref="QuadInstance"/> (via <see cref="Add(QuadInstance)"/>), not every <see cref="ModelInstance"/> in <see cref="ModelInstances"/>.
	/// </remarks>
	public IndirectEnumerable<EnumerationInput, QuadInstance> QuadInstances => GetAllResourcesOfType<QuadInstance, ModelInstance>();
	/// <summary>
	/// All currently-live <see cref="CameraLockedQuadInstance"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;CameraLockedQuadInstance, ModelInstance&gt;()</c>.
	/// </summary>
	/// <remarks>
	/// <see cref="CameraLockedQuadInstance"/> is a specialization of <see cref="ModelInstance"/> (see <see cref="IResourceSpecialization{TSelf,TBase}"/>); this property only returns <see cref="ModelInstance"/>s that were added to the group specifically as a <see cref="CameraLockedQuadInstance"/> (via <see cref="Add(CameraLockedQuadInstance)"/>), not every <see cref="ModelInstance"/> in <see cref="ModelInstances"/>.
	/// </remarks>
	public IndirectEnumerable<EnumerationInput, CameraLockedQuadInstance> CameraLockedQuadInstances => GetAllResourcesOfType<CameraLockedQuadInstance, ModelInstance>();
	/// <summary>
	/// All currently-live <see cref="TextInstance"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;TextInstance, ModelInstance&gt;()</c>.
	/// </summary>
	/// <remarks>
	/// <see cref="TextInstance"/> is a specialization of <see cref="ModelInstance"/> (see <see cref="IResourceSpecialization{TSelf,TBase}"/>); this property only returns <see cref="ModelInstance"/>s that were added to the group specifically as a <see cref="TextInstance"/> (via <see cref="Add(TextInstance)"/>), not every <see cref="ModelInstance"/> in <see cref="ModelInstances"/>.
	/// </remarks>
	public IndirectEnumerable<EnumerationInput, TextInstance> TextInstances => GetAllResourcesOfType<TextInstance, ModelInstance>();
	/// <summary>
	/// All currently-live <see cref="CameraLockedTextInstance"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;CameraLockedTextInstance, ModelInstance&gt;()</c>.
	/// </summary>
	/// <remarks>
	/// <see cref="CameraLockedTextInstance"/> is a specialization of <see cref="ModelInstance"/> (see <see cref="IResourceSpecialization{TSelf,TBase}"/>); this property only returns <see cref="ModelInstance"/>s that were added to the group specifically as a <see cref="CameraLockedTextInstance"/> (via <see cref="Add(CameraLockedTextInstance)"/>), not every <see cref="ModelInstance"/> in <see cref="ModelInstances"/>.
	/// </remarks>
	public IndirectEnumerable<EnumerationInput, CameraLockedTextInstance> CameraLockedTextInstances => GetAllResourcesOfType<CameraLockedTextInstance, ModelInstance>();
	/// <summary>
	/// All currently-live <see cref="FontString"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;FontString, Font&gt;()</c>.
	/// </summary>
	/// <remarks>
	/// <see cref="FontString"/> is a specialization of <see cref="Font"/> (see <see cref="IResourceSpecialization{TSelf,TBase}"/>); this property only returns <see cref="Font"/>s that were added to the group specifically as a <see cref="FontString"/> (via <see cref="Add(FontString)"/>), not every <see cref="Font"/> in <see cref="Fonts"/>.
	/// </remarks>
	public IndirectEnumerable<EnumerationInput, FontString> FontStrings => GetAllResourcesOfType<FontString, Font>();
	/// <summary>
	/// All currently-live <see cref="FontPen"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;FontPen, Font&gt;()</c>.
	/// </summary>
	/// <remarks>
	/// <see cref="FontPen"/> is a specialization of <see cref="Font"/> (see <see cref="IResourceSpecialization{TSelf,TBase}"/>); this property only returns <see cref="Font"/>s that were added to the group specifically as a <see cref="FontPen"/> (via <see cref="Add(FontPen)"/>), not every <see cref="Font"/> in <see cref="Fonts"/>.
	/// </remarks>
	public IndirectEnumerable<EnumerationInput, FontPen> FontPens => GetAllResourcesOfType<FontPen, Font>();
	/// <summary>
	/// All currently-live <see cref="CanvasScene"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;CanvasScene, Scene&gt;()</c>.
	/// </summary>
	/// <remarks>
	/// <see cref="CanvasScene"/> is a specialization of <see cref="Scene"/> (see <see cref="IResourceSpecialization{TSelf,TBase}"/>); this property only returns <see cref="Scene"/>s that were added to the group specifically as a <see cref="CanvasScene"/> (via <see cref="Add(CanvasScene)"/>), not every <see cref="Scene"/> in <see cref="Scenes"/>.
	/// </remarks>
	public IndirectEnumerable<EnumerationInput, CanvasScene> CanvasScenes => GetAllResourcesOfType<CanvasScene, Scene>();
	/// <summary>
	/// All currently-live <see cref="CanvasText"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;CanvasText, ModelInstance&gt;()</c>.
	/// </summary>
	/// <remarks>
	/// <see cref="CanvasText"/> is a specialization of <see cref="ModelInstance"/> (see <see cref="IResourceSpecialization{TSelf,TBase}"/>); this property only returns <see cref="ModelInstance"/>s that were added to the group specifically as a <see cref="CanvasText"/> (via <see cref="Add(CanvasText)"/>), not every <see cref="ModelInstance"/> in <see cref="ModelInstances"/>.
	/// </remarks>
	public IndirectEnumerable<EnumerationInput, CanvasText> CanvasTexts => GetAllResourcesOfType<CanvasText, ModelInstance>();
	/// <summary>
	/// All currently-live <see cref="CanvasTexture"/>s in this group; equivalent to <c>GetAllResourcesOfType&lt;CanvasTexture, ModelInstance&gt;()</c>.
	/// </summary>
	/// <remarks>
	/// <see cref="CanvasTexture"/> is a specialization of <see cref="ModelInstance"/> (see <see cref="IResourceSpecialization{TSelf,TBase}"/>); this property only returns <see cref="ModelInstance"/>s that were added to the group specifically as a <see cref="CanvasTexture"/> (via <see cref="Add(CanvasTexture)"/>), not every <see cref="ModelInstance"/> in <see cref="ModelInstances"/>.
	/// </remarks>
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<ResourceGroup> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<ResourceGroup> IResource<ResourceGroup>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

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

	/// <summary>
	/// Adds <paramref name="resource"/> (a specialization of <see cref="Mesh"/>, see <see cref="IResourceSpecialization{TSelf,TBase}"/>) to this group.
	/// </summary>
	/// <remarks>
	/// This creates a dependency from this group on the underlying <see cref="Mesh"/> that <paramref name="resource"/> specializes: you can not dispose that underlying resource while <paramref name="resource"/> remains part of this (non-disposed) group.
	/// </remarks>
	/// <param name="resource">The resource to add.</param>
	/// <exception cref="ResourceGroupSealedException">Thrown if this group has already been sealed (see <see cref="Seal"/>).</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(QuadMesh resource) => Add<QuadMesh, Mesh>(resource);

	/// <summary>
	/// Adds <paramref name="resource"/> (a specialization of <see cref="ModelInstance"/>, see <see cref="IResourceSpecialization{TSelf,TBase}"/>) to this group.
	/// </summary>
	/// <remarks>
	/// This creates a dependency from this group on the underlying <see cref="ModelInstance"/> that <paramref name="resource"/> specializes: you can not dispose that underlying resource while <paramref name="resource"/> remains part of this (non-disposed) group.
	/// </remarks>
	/// <param name="resource">The resource to add.</param>
	/// <exception cref="ResourceGroupSealedException">Thrown if this group has already been sealed (see <see cref="Seal"/>).</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(QuadInstance resource) => Add<QuadInstance, ModelInstance>(resource);

	/// <summary>
	/// Adds <paramref name="resource"/> (a specialization of <see cref="ModelInstance"/>, see <see cref="IResourceSpecialization{TSelf,TBase}"/>) to this group.
	/// </summary>
	/// <remarks>
	/// This creates a dependency from this group on the underlying <see cref="ModelInstance"/> that <paramref name="resource"/> specializes: you can not dispose that underlying resource while <paramref name="resource"/> remains part of this (non-disposed) group.
	/// </remarks>
	/// <param name="resource">The resource to add.</param>
	/// <exception cref="ResourceGroupSealedException">Thrown if this group has already been sealed (see <see cref="Seal"/>).</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CameraLockedQuadInstance resource) => Add<CameraLockedQuadInstance, ModelInstance>(resource);

	/// <summary>
	/// Adds <paramref name="resource"/> (a specialization of <see cref="ModelInstance"/>, see <see cref="IResourceSpecialization{TSelf,TBase}"/>) to this group.
	/// </summary>
	/// <remarks>
	/// This creates a dependency from this group on the underlying <see cref="ModelInstance"/> that <paramref name="resource"/> specializes: you can not dispose that underlying resource while <paramref name="resource"/> remains part of this (non-disposed) group.
	/// </remarks>
	/// <param name="resource">The resource to add.</param>
	/// <exception cref="ResourceGroupSealedException">Thrown if this group has already been sealed (see <see cref="Seal"/>).</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(TextInstance resource) => Add<TextInstance, ModelInstance>(resource);

	/// <summary>
	/// Adds <paramref name="resource"/> (a specialization of <see cref="ModelInstance"/>, see <see cref="IResourceSpecialization{TSelf,TBase}"/>) to this group.
	/// </summary>
	/// <remarks>
	/// This creates a dependency from this group on the underlying <see cref="ModelInstance"/> that <paramref name="resource"/> specializes: you can not dispose that underlying resource while <paramref name="resource"/> remains part of this (non-disposed) group.
	/// </remarks>
	/// <param name="resource">The resource to add.</param>
	/// <exception cref="ResourceGroupSealedException">Thrown if this group has already been sealed (see <see cref="Seal"/>).</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CameraLockedTextInstance resource) => Add<CameraLockedTextInstance, ModelInstance>(resource);

	/// <summary>
	/// Adds <paramref name="resource"/> (a specialization of <see cref="Font"/>, see <see cref="IResourceSpecialization{TSelf,TBase}"/>) to this group.
	/// </summary>
	/// <remarks>
	/// This creates a dependency from this group on the underlying <see cref="Font"/> that <paramref name="resource"/> specializes: you can not dispose that underlying resource while <paramref name="resource"/> remains part of this (non-disposed) group.
	/// </remarks>
	/// <param name="resource">The resource to add.</param>
	/// <exception cref="ResourceGroupSealedException">Thrown if this group has already been sealed (see <see cref="Seal"/>).</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(FontString resource) => Add<FontString, Font>(resource);

	/// <summary>
	/// Adds <paramref name="resource"/> (a specialization of <see cref="Font"/>, see <see cref="IResourceSpecialization{TSelf,TBase}"/>) to this group.
	/// </summary>
	/// <remarks>
	/// This creates a dependency from this group on the underlying <see cref="Font"/> that <paramref name="resource"/> specializes: you can not dispose that underlying resource while <paramref name="resource"/> remains part of this (non-disposed) group.
	/// </remarks>
	/// <param name="resource">The resource to add.</param>
	/// <exception cref="ResourceGroupSealedException">Thrown if this group has already been sealed (see <see cref="Seal"/>).</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(FontPen resource) => Add<FontPen, Font>(resource);

	/// <summary>
	/// Adds <paramref name="resource"/> (a specialization of <see cref="Scene"/>, see <see cref="IResourceSpecialization{TSelf,TBase}"/>) to this group.
	/// </summary>
	/// <remarks>
	/// This creates a dependency from this group on the underlying <see cref="Scene"/> that <paramref name="resource"/> specializes: you can not dispose that underlying resource while <paramref name="resource"/> remains part of this (non-disposed) group.
	/// </remarks>
	/// <param name="resource">The resource to add.</param>
	/// <exception cref="ResourceGroupSealedException">Thrown if this group has already been sealed (see <see cref="Seal"/>).</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CanvasScene resource) => Add<CanvasScene, Scene>(resource);

	/// <summary>
	/// Adds <paramref name="resource"/> (a specialization of <see cref="ModelInstance"/>, see <see cref="IResourceSpecialization{TSelf,TBase}"/>) to this group.
	/// </summary>
	/// <remarks>
	/// This creates a dependency from this group on the underlying <see cref="ModelInstance"/> that <paramref name="resource"/> specializes: you can not dispose that underlying resource while <paramref name="resource"/> remains part of this (non-disposed) group.
	/// </remarks>
	/// <param name="resource">The resource to add.</param>
	/// <exception cref="ResourceGroupSealedException">Thrown if this group has already been sealed (see <see cref="Seal"/>).</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CanvasText resource) => Add<CanvasText, ModelInstance>(resource);

	/// <summary>
	/// Adds <paramref name="resource"/> (a specialization of <see cref="ModelInstance"/>, see <see cref="IResourceSpecialization{TSelf,TBase}"/>) to this group.
	/// </summary>
	/// <remarks>
	/// This creates a dependency from this group on the underlying <see cref="ModelInstance"/> that <paramref name="resource"/> specializes: you can not dispose that underlying resource while <paramref name="resource"/> remains part of this (non-disposed) group.
	/// </remarks>
	/// <param name="resource">The resource to add.</param>
	/// <exception cref="ResourceGroupSealedException">Thrown if this group has already been sealed (see <see cref="Seal"/>).</exception>
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
	/// <summary>
	/// <see cref="Equals(ResourceGroup)"/>
	/// </summary>
	public static bool operator ==(ResourceGroup left, ResourceGroup right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(ResourceGroup)"/>
	/// </summary>
	public static bool operator !=(ResourceGroup left, ResourceGroup right) => !left.Equals(right);
	#endregion
}