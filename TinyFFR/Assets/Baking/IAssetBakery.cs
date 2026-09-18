// Created on 2026-08-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Baking;

/// <summary>
/// Bakes loaded resources in to a bespoke format TinyFFR can load much faster.
/// </summary>
/// <remarks>
/// <para>
/// Loading an ordinary asset file means decoding an image or parsing a model, then processing and compressing the result. Baking
/// performs all of that once, ahead of time, and writes out what came of it; the corresponding <c>LoadBaked...</c> methods then
/// skip straight to handing the data to the GPU.
/// </para>
/// <para>
/// This is an authoring-time/packaging-time or first-run step rather than something to do while an application is running, and the bakery must be
/// enabled in the factory's configuration before it can be used.
/// </para>
/// </remarks>
public interface IAssetBakery {
	/// <summary>
	/// Whether the asset bakery is available for use.
	/// </summary>
	/// <remarks>
	/// Resources you wish to bake must be loaded <i>after</i> this is set to <c>true</c>.
	/// Setting this to <c>false</c> removes all data from previously-loaded assets, meaning you
	/// should commit any resources via a <c>Bake(...)</c> overload before disabling the bakery.
	/// </remarks>
	bool Enabled { get; set; }
	
	/// <summary>
	/// Writes the given backdrop texture to a baked asset file.
	/// </summary>
	/// <param name="resource">The resource to bake.</param>
	/// <param name="filePath">The path of the baked asset file to write. Any existing file at that path is overwritten.</param>
	void Bake(BackdropTexture resource, ReadOnlySpan<char> filePath);
	/// <summary>
	/// Writes the given font to a baked asset file.
	/// </summary>
	/// <param name="resource">The resource to bake.</param>
	/// <param name="filePath">The path of the baked asset file to write. Any existing file at that path is overwritten.</param>
	void Bake(Font resource, ReadOnlySpan<char> filePath);
	/// <summary>
	/// Writes the given material and the textures it uses to a baked asset file.
	/// </summary>
	/// <param name="resource">The resource to bake.</param>
	/// <param name="filePath">The path of the baked asset file to write. Any existing file at that path is overwritten.</param>
	void Bake(Material resource, ReadOnlySpan<char> filePath);
	/// <summary>
	/// Writes the given mesh to a baked asset file.
	/// </summary>
	/// <param name="resource">The resource to bake.</param>
	/// <param name="filePath">The path of the baked asset file to write. Any existing file at that path is overwritten.</param>
	void Bake(Mesh resource, ReadOnlySpan<char> filePath);
	/// <summary>
	/// Writes the given model, and the mesh, material and textures it uses to a baked asset file.
	/// </summary>
	/// <param name="resource">The resource to bake.</param>
	/// <param name="filePath">The path of the baked asset file to write. Any existing file at that path is overwritten.</param>
	void Bake(Model resource, ReadOnlySpan<char> filePath);
	/// <summary>
	/// Writes the given group of resources to a baked asset file.
	/// </summary>
	/// <param name="resource">The resource to bake.</param>
	/// <param name="filePath">The path of the baked asset file to write. Any existing file at that path is overwritten.</param>
	void Bake(ResourceGroup resource, ReadOnlySpan<char> filePath);
	/// <summary>
	/// Writes the given texture to a baked asset file.
	/// </summary>
	/// <param name="resource">The resource to bake.</param>
	/// <param name="filePath">The path of the baked asset file to write. Any existing file at that path is overwritten.</param>
	void Bake(Texture resource, ReadOnlySpan<char> filePath);
	/// <summary>
	/// Releases everything the bakery is holding in memory.
	/// </summary>
	/// <remarks>
	/// The bakery keeps recently-baked resources around so that a resource referenced by several assets is only written once.
	/// Clearing that is worth doing between unrelated batches of baking.
	/// </remarks>
	void ClearBakeryMemory();
}