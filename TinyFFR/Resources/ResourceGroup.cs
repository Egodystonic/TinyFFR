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
	public IndirectEnumerable<EnumerationInput, ApplicationLoop> ApplicationLoops => GetAllResourcesOfType<ApplicationLoop>();
	public IndirectEnumerable<EnumerationInput, BackdropTexture> BackdropTextures => GetAllResourcesOfType<BackdropTexture>();
	public IndirectEnumerable<EnumerationInput, Camera> Cameras => GetAllResourcesOfType<Camera>();
	public IndirectEnumerable<EnumerationInput, DirectionalLight> DirectionalLights => GetAllResourcesOfType<DirectionalLight>();
	public IndirectEnumerable<EnumerationInput, Display> Displays => GetAllResourcesOfType<Display>();
	public IndirectEnumerable<EnumerationInput, Font> Fonts => GetAllResourcesOfType<Font>();
	public IndirectEnumerable<EnumerationInput, Material> Materials => GetAllResourcesOfType<Material>();
	public IndirectEnumerable<EnumerationInput, Mesh> Meshes => GetAllResourcesOfType<Mesh>();
	public IndirectEnumerable<EnumerationInput, MeshAnimation> MeshAnimations => GetAllResourcesOfType<MeshAnimation>();
	public IndirectEnumerable<EnumerationInput, MeshNode> MeshNodes => GetAllResourcesOfType<MeshNode>();
	public IndirectEnumerable<EnumerationInput, Model> Models => GetAllResourcesOfType<Model>();
	public IndirectEnumerable<EnumerationInput, ModelInstance> ModelInstances => GetAllResourcesOfType<ModelInstance>();
	public IndirectEnumerable<EnumerationInput, PointLight> PointLights => GetAllResourcesOfType<PointLight>();
	public IndirectEnumerable<EnumerationInput, Renderer> Renderers => GetAllResourcesOfType<Renderer>();
	public IndirectEnumerable<EnumerationInput, RendererCompositor> RendererCompositors => GetAllResourcesOfType<RendererCompositor>();
	public IndirectEnumerable<EnumerationInput, RenderOutputBuffer> RenderOutputBuffers => GetAllResourcesOfType<RenderOutputBuffer>();
	public IndirectEnumerable<EnumerationInput, ResourceGroup> ResourceGroups => GetAllResourcesOfType<ResourceGroup>();
	public IndirectEnumerable<EnumerationInput, Scene> Scenes => GetAllResourcesOfType<Scene>();
	public IndirectEnumerable<EnumerationInput, SpotLight> SpotLights => GetAllResourcesOfType<SpotLight>();
	public IndirectEnumerable<EnumerationInput, Texture> Textures => GetAllResourcesOfType<Texture>();
	public IndirectEnumerable<EnumerationInput, Window> Windows => GetAllResourcesOfType<Window>();
	
	public IndirectEnumerable<EnumerationInput, QuadMesh> QuadMeshes => GetAllResourcesOfType<QuadMesh, Mesh>();
	public IndirectEnumerable<EnumerationInput, QuadInstance> QuadInstances => GetAllResourcesOfType<QuadInstance, ModelInstance>();
	public IndirectEnumerable<EnumerationInput, CameraLockedQuadInstance> CameraLockedQuadInstances => GetAllResourcesOfType<CameraLockedQuadInstance, ModelInstance>();
	public IndirectEnumerable<EnumerationInput, TextInstance> TextInstances => GetAllResourcesOfType<TextInstance, ModelInstance>();
	public IndirectEnumerable<EnumerationInput, CameraLockedTextInstance> CameraLockedTextInstances => GetAllResourcesOfType<CameraLockedTextInstance, ModelInstance>();
	public IndirectEnumerable<EnumerationInput, FontString> FontStrings => GetAllResourcesOfType<FontString, Font>();
	public IndirectEnumerable<EnumerationInput, FontPen> FontPens => GetAllResourcesOfType<FontPen, Font>();
	public IndirectEnumerable<EnumerationInput, CanvasScene> CanvasScenes => GetAllResourcesOfType<CanvasScene, Scene>();
	public IndirectEnumerable<EnumerationInput, CanvasText> CanvasTexts => GetAllResourcesOfType<CanvasText, ModelInstance>();
	public IndirectEnumerable<EnumerationInput, CanvasTexture> CanvasTextures => GetAllResourcesOfType<CanvasTexture, ModelInstance>();
	#endregion

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
	public ResourceHandle<ResourceGroup> GetHandleWithoutDisposeCheck() => _handle;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add<TResource>(TResource resource) where TResource : IResource => Implementation.AddResource(Handle, resource);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add<TResource, TBase>(TResource resource) where TResource : struct, IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase> => Implementation.AddResource<TResource, TBase>(Handle, resource);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(QuadMesh resource) => Add<QuadMesh, Mesh>(resource);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(QuadInstance resource) => Add<QuadInstance, ModelInstance>(resource);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CameraLockedQuadInstance resource) => Add<CameraLockedQuadInstance, ModelInstance>(resource);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(TextInstance resource) => Add<TextInstance, ModelInstance>(resource);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CameraLockedTextInstance resource) => Add<CameraLockedTextInstance, ModelInstance>(resource);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(FontString resource) => Add<FontString, Font>(resource);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(FontPen resource) => Add<FontPen, Font>(resource);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CanvasScene resource) => Add<CanvasScene, Scene>(resource);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CanvasText resource) => Add<CanvasText, ModelInstance>(resource);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(CanvasTexture resource) => Add<CanvasTexture, ModelInstance>(resource);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Seal() => Implementation.Seal(Handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IReadOnlyCollection<object> GetAllResourcesBoxed() => Implementation.GetAllResourcesBoxed(Handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IndirectEnumerable<EnumerationInput, TResource> GetAllResourcesOfType<TResource>() where TResource : IResource<TResource> {
		return Implementation.GetAllResourcesOfType<TResource>(Handle);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IndirectEnumerable<EnumerationInput, TResource> GetAllResourcesOfType<TResource, TBase>() where TResource : struct, IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase> {
		return Implementation.GetAllResourcesOfType<TResource, TBase>(Handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TResource GetNthResourceOfType<TResource>(int index) where TResource : IResource<TResource> {
		return Implementation.GetNthResourceOfType<TResource>(Handle, index);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TResource GetNthResourceOfType<TResource, TBase>(int index) where TResource : struct, IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase> {
		return Implementation.GetNthResourceOfType<TResource, TBase>(Handle, index);
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