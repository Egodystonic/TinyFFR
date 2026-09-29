// Created on 2026-09-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets;

public readonly struct ModelBundle : IDisposable, IStringSpanNameEnabled, IEquatable<ModelBundle> {
	public ResourceGroup UnderlyingResourceGroup { get; }
	
	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Texture> Textures { get; }
	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Material> Materials { get; }
	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Mesh> Meshes { get; }
	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, Model> Models { get; }
}