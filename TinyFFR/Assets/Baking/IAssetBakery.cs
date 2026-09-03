// Created on 2026-08-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Baking;

public interface IAssetBakery {
	bool Enabled { get; set; }
	
	void Bake(BackdropTexture resource, ReadOnlySpan<char> filePath);
	void Bake(Font resource, ReadOnlySpan<char> filePath);
	void Bake(Material resource, ReadOnlySpan<char> filePath);
	void Bake(Mesh resource, ReadOnlySpan<char> filePath);
	void Bake(Model resource, ReadOnlySpan<char> filePath);
	void Bake(ResourceGroup resource, ReadOnlySpan<char> filePath);
	void Bake(Texture resource, ReadOnlySpan<char> filePath);
	void ClearBakeryMemory();
}