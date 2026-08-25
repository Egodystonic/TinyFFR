using System.IO;
using System.Linq;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.Threading;
using static Egodystonic.TinyFFR.Assets.Materials.TextureCombinationSourceTexture;
using static Egodystonic.TinyFFR.ColorChannel;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalAsyncLoadingTest {
	static readonly TimeSpan AsyncTimeout = TimeSpan.FromSeconds(60d);

	static string CrateColorFile => CommonTestAssets.FindAsset("ELCrate.png");
	static string CrateNormalFile => CommonTestAssets.FindAsset("ELCrate_Normal.png");
	static string CrateOrmFile => CommonTestAssets.FindAsset("ELCrate_Specular.png");
	static string AlphaLogoFile => CommonTestAssets.FindAsset("egdLogo.png");
	static string CrateFile => CommonTestAssets.FindAsset("ELCrate.obj");
	static string RiggedFile => CommonTestAssets.FindAsset("models/RiggedSimple.glb");
	static string CesiumManFile => CommonTestAssets.FindAsset("models/CesiumMan.glb");

	static T AwaitLoad<T>(TinyFfrAsyncOperation<T> op) {
		Assert.IsTrue(op.WaitForCompletion(AsyncTimeout), "Async load timed out.");
		return op.GetResultAndDisposeOperation();
	}

	static void AssertTexturesEquivalent(Texture expected, Texture actual, string context) {
		Assert.AreEqual(expected.Dimensions, actual.Dimensions, $"{context}: dimensions differ.");
		Assert.AreEqual(expected.TexelType, actual.TexelType, $"{context}: texel type differs.");
		Assert.AreEqual(expected.ContainsMipMaps, actual.ContainsMipMaps, $"{context}: mip-map presence differs.");
		Assert.AreEqual(expected.AllowsDynamicWrites, actual.AllowsDynamicWrites, $"{context}: dynamic-write flag differs.");
		Assert.AreEqual(expected.RenderingConfig, actual.RenderingConfig, $"{context}: rendering config differs.");
		Console.WriteLine($"  {context}: {actual.Dimensions} {actual.TexelType} mips={actual.ContainsMipMaps} aniso={actual.RenderingConfig.AnisotropyLevel}");
	}
	
	[Test]
	public void Execute() {
		RunAllAutomatedTests();
		// TODO
	}
	
	void RunAllAutomatedTests() {
		SyncAndAsyncShouldProduceEquivalentTextures();
		SyncAndAsyncShouldApplyProcessingIdentically();
		SyncAndAsyncShouldProduceEquivalentCombinedTextures();
		CanvasTextureShouldKeepZeroAnisotropyThroughBothPaths();
		BuiltInTexturesShouldLoadThroughBothPaths();
		MapLoadersShouldWorkThroughBothPaths();
		ConcurrentAsyncTextureLoadsShouldAllSucceed();
		RepeatedAlternatingLoadsShouldRemainConsistent();
		SyncAndAsyncShouldProduceIdenticalNonSkeletalMeshes();
		SyncAndAsyncShouldApplyCreationConfigTransformsIdentically();
		SyncAndAsyncShouldProduceIdenticalSkeletalMeshes();
		ConcurrentAsyncMeshLoadsShouldAllSucceed();
		RepeatedLoadsShouldNotLeakOrCorrupt();
		LoadAllShouldStillWorkAfterModelsRework();
		ReadMeshShouldStillPopulateCallerBuffers();
	}

	void SyncAndAsyncShouldProduceEquivalentTextures() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var metadata = loader.ReadTextureMetadata(CrateColorFile);
		Console.WriteLine($"ELCrate.png: {metadata.Dimensions}, alpha={metadata.IncludesAlphaChannel}");

		using var syncTex = loader.LoadTexture(CrateColorFile, isLinearColorspace: false, "tex-sync");
		using var asyncTex = AwaitLoad(loader.LoadTextureAsync(CrateColorFile, isLinearColorspace: false, "tex-async"));

		AssertTexturesEquivalent(syncTex, asyncTex, "plain load");
		Assert.AreEqual(metadata.Dimensions, syncTex.Dimensions);
		Assert.AreEqual("tex-sync", syncTex.GetNameAsNewStringObject());
		Assert.AreEqual("tex-async", asyncTex.GetNameAsNewStringObject());
	}

	void SyncAndAsyncShouldApplyProcessingIdentically() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var config = new TextureCreationConfig {
			IsLinearColorspace = true,
			Name = "processed",
			ProcessingToApply = new TextureProcessingConfig {
				FlipX = true,
				FlipY = true,
				InvertYGreenChannel = true,
				ZBlueFinalOutputSource = A
			}
		};

		using var syncTex = loader.LoadTexture(CrateColorFile, in config);
		using var asyncTex = AwaitLoad(loader.LoadTextureAsync(CrateColorFile, in config));
		AssertTexturesEquivalent(syncTex, asyncTex, "processed load");
	}

	void SyncAndAsyncShouldProduceEquivalentCombinedTextures() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var twoSource = new TextureCombinationConfig {
			OutputTextureXRedChannelSource = new(TextureA, R),
			OutputTextureYGreenChannelSource = new(TextureA, G),
			OutputTextureZBlueChannelSource = new(TextureB, R)
		};
		using var sync2 = loader.LoadCombinedTexture(CrateColorFile, CrateNormalFile, twoSource, TextureCreationConfig.ForDataTexture("combine2"));
		using var async2 = AwaitLoad(loader.LoadCombinedTextureAsync(CrateColorFile, CrateNormalFile, twoSource, TextureCreationConfig.ForDataTexture("combine2")));
		AssertTexturesEquivalent(sync2, async2, "2-source combine");

		var threeSource = new TextureCombinationConfig {
			OutputTextureXRedChannelSource = new(TextureA, R),
			OutputTextureYGreenChannelSource = new(TextureB, R),
			OutputTextureZBlueChannelSource = new(TextureC, R)
		};
		using var sync3 = loader.LoadCombinedTexture(CrateColorFile, CrateNormalFile, CrateOrmFile, threeSource, TextureCreationConfig.ForDataTexture("combine3"));
		using var async3 = AwaitLoad(loader.LoadCombinedTextureAsync(CrateColorFile, CrateNormalFile, CrateOrmFile, threeSource, TextureCreationConfig.ForDataTexture("combine3")));
		AssertTexturesEquivalent(sync3, async3, "3-source combine");

		var fourSource = new TextureCombinationConfig {
			OutputTextureXRedChannelSource = new(TextureA, R),
			OutputTextureYGreenChannelSource = new(TextureB, R),
			OutputTextureZBlueChannelSource = new(TextureC, R),
			OutputTextureWAlphaChannelSource = new(TextureD, R)
		};
		using var sync4 = loader.LoadCombinedTexture(CrateColorFile, CrateNormalFile, CrateOrmFile, CrateColorFile, fourSource, TextureCreationConfig.ForDataTexture("combine4"));
		using var async4 = AwaitLoad(loader.LoadCombinedTextureAsync(CrateColorFile, CrateNormalFile, CrateOrmFile, CrateColorFile, fourSource, TextureCreationConfig.ForDataTexture("combine4")));
		AssertTexturesEquivalent(sync4, async4, "4-source combine");
		Assert.AreEqual(TexelType.Rgba32, sync4.TexelType, "A combine with an alpha source should produce an Rgba32 texture.");
		Assert.AreEqual(TexelType.Rgb24, sync3.TexelType, "A combine without an alpha source should produce an Rgb24 texture.");
	}

	void CanvasTextureShouldKeepZeroAnisotropyThroughBothPaths() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		using var syncTex = loader.LoadCanvasTexture(CrateColorFile);
		using var asyncTex = AwaitLoad(loader.LoadCanvasTextureAsync(CrateColorFile));

		Console.WriteLine($"  sync aniso={syncTex.RenderingConfig.AnisotropyLevel} async aniso={asyncTex.RenderingConfig.AnisotropyLevel}");
		Assert.AreEqual(0f, syncTex.RenderingConfig.AnisotropyLevel, "Sync canvas texture lost its explicit zero anisotropy.");
		Assert.AreEqual(0f, asyncTex.RenderingConfig.AnisotropyLevel, "Async canvas texture lost its explicit zero anisotropy (the config round-trip dropped AnisotropyLevel).");
		AssertTexturesEquivalent(syncTex, asyncTex, "canvas texture");
	}

	void BuiltInTexturesShouldLoadThroughBothPaths() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		using var syncTex = loader.LoadTexture(loader.BuiltInTexturePaths.DefaultReflectanceMap, isLinearColorspace: true, "builtin-sync");
		using var asyncTex = AwaitLoad(loader.LoadTextureAsync(loader.BuiltInTexturePaths.DefaultReflectanceMap, isLinearColorspace: true, "builtin-async"));
		AssertTexturesEquivalent(syncTex, asyncTex, "built-in texel");

		using var syncEmbedded = loader.LoadTexture(loader.BuiltInTexturePaths.UvTestingTexture, isLinearColorspace: false, "embedded-sync");
		using var asyncEmbedded = AwaitLoad(loader.LoadTextureAsync(loader.BuiltInTexturePaths.UvTestingTexture, isLinearColorspace: false, "embedded-async"));
		AssertTexturesEquivalent(syncEmbedded, asyncEmbedded, "built-in embedded resource");
	}

	void MapLoadersShouldWorkThroughBothPaths() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		using var syncNormal = loader.LoadNormalMap(CrateNormalFile);
		using var asyncNormal = AwaitLoad(loader.LoadNormalMapAsync(CrateNormalFile));
		AssertTexturesEquivalent(syncNormal, asyncNormal, "normal map");

		using var syncOrm = loader.LoadOcclusionRoughnessMetallicMap(CrateOrmFile);
		using var asyncOrm = AwaitLoad(loader.LoadOcclusionRoughnessMetallicMapAsync(CrateOrmFile));
		AssertTexturesEquivalent(syncOrm, asyncOrm, "ORM map");

		using var syncOrmr = loader.LoadOcclusionRoughnessMetallicReflectanceMap(CrateOrmFile);
		using var asyncOrmr = AwaitLoad(loader.LoadOcclusionRoughnessMetallicReflectanceMapAsync(CrateOrmFile));
		AssertTexturesEquivalent(syncOrmr, asyncOrmr, "ORMR map (metadata-branching)");

		using var syncAniso = loader.LoadAnisotropyMapRadialAngleFormatted(CrateOrmFile, Orientation2D.Right, AnisotropyRadialAngleRange.ZeroTo360, true, R);
		using var asyncAniso = AwaitLoad(loader.LoadAnisotropyMapRadialAngleFormattedAsync(CrateOrmFile, Orientation2D.Right, AnisotropyRadialAngleRange.ZeroTo360, true, R));
		AssertTexturesEquivalent(syncAniso, asyncAniso, "radial-angle anisotropy map");
		AssertRadialAngleAnisotropyTexelType(loader, CrateOrmFile, syncAniso);

		Assert.IsTrue(loader.ReadTextureMetadata(AlphaLogoFile).IncludesAlphaChannel, "Expected an alpha-bearing asset; the Rgba32 anisotropy branch would otherwise go untested.");
		using var syncAnisoAlpha = loader.LoadAnisotropyMapRadialAngleFormatted(AlphaLogoFile, Orientation2D.Up, AnisotropyRadialAngleRange.ZeroTo180, false, A);
		using var asyncAnisoAlpha = AwaitLoad(loader.LoadAnisotropyMapRadialAngleFormattedAsync(AlphaLogoFile, Orientation2D.Up, AnisotropyRadialAngleRange.ZeroTo180, false, A));
		AssertTexturesEquivalent(syncAnisoAlpha, asyncAnisoAlpha, "radial-angle anisotropy map (alpha strength channel)");
		AssertRadialAngleAnisotropyTexelType(loader, AlphaLogoFile, syncAnisoAlpha);

		using var syncAniso2 = loader.LoadAnisotropyMapRadialAngleFormatted(CrateOrmFile, CrateColorFile, Orientation2D.Right, AnisotropyRadialAngleRange.ZeroTo180, false);
		using var asyncAniso2 = AwaitLoad(loader.LoadAnisotropyMapRadialAngleFormattedAsync(CrateOrmFile, CrateColorFile, Orientation2D.Right, AnisotropyRadialAngleRange.ZeroTo180, false));
		AssertTexturesEquivalent(syncAniso2, asyncAniso2, "radial-angle anisotropy map (two-source)");
		Assert.AreEqual(TexelType.Rgb24, syncAniso2.TexelType, "A two-source radial-angle anisotropy map has no alpha source and must always be Rgb24.");
	}

	static void AssertRadialAngleAnisotropyTexelType(IAssetLoader loader, string filePath, Texture texture) {
		var expected = loader.ReadTextureMetadata(filePath).IncludesAlphaChannel ? TexelType.Rgba32 : TexelType.Rgb24;
		Assert.AreEqual(
			expected,
			texture.TexelType,
			$"Radial-angle anisotropy load of '{Path.GetFileName(filePath)}' produced the wrong texel type; " +
			"the metadata probe and the pinned read config must agree with the texel type the conversion callback was created for."
		);
	}

	void ConcurrentAsyncTextureLoadsShouldAllSucceed() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var ops = new[] {
			loader.LoadTextureAsync(CrateColorFile, false, "c0"),
			loader.LoadTextureAsync(CrateNormalFile, true, "c1"),
			loader.LoadTextureAsync(CrateOrmFile, true, "c2"),
			loader.LoadTextureAsync(CrateColorFile, false, "c3")
		};

		var textures = ops.Select(AwaitLoad).ToList();
		for (var i = 0; i < textures.Count; ++i) {
			Console.WriteLine($"  concurrent[{i}] '{textures[i].GetNameAsNewStringObject()}' {textures[i].Dimensions}");
			Assert.AreEqual($"c{i}", textures[i].GetNameAsNewStringObject());
		}
		foreach (var texture in textures) texture.Dispose();
	}

	void RepeatedAlternatingLoadsShouldRemainConsistent() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		XYPair<int>? expectedDimensions = null;
		for (var i = 0; i < 12; ++i) {
			using var texture = (i % 2 == 0)
				? loader.LoadTexture(CrateColorFile, isLinearColorspace: false, "repeat")
				: AwaitLoad(loader.LoadTextureAsync(CrateColorFile, isLinearColorspace: false, "repeat"));
			expectedDimensions ??= texture.Dimensions;
			Assert.AreEqual(expectedDimensions.Value, texture.Dimensions, $"Iteration {i} produced different dimensions.");
		}
		Console.WriteLine($"  12 alternating sync/async loads all produced {expectedDimensions}");
	}
	
	static void DescribeMesh(string label, IAssetLoader loader, Mesh mesh) {
		Console.WriteLine($"  {label}: name='{mesh.GetNameAsNewStringObject()}' bounds={mesh.BoundingBox} animations={mesh.Animations.Count()} nodes={mesh.Skeleton.Nodes.Count()}");
	}

	void SyncAndAsyncShouldProduceIdenticalNonSkeletalMeshes() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var metadata = loader.ReadMeshMetadata(CrateFile);
		Console.WriteLine($"ELCrate.obj metadata: {metadata.TotalVertexCount} verts, {metadata.TotalTriangleCount} tris, {metadata.SubMeshCount} sub-meshes");
		Assert.Greater(metadata.TotalVertexCount, 0);
		Assert.Greater(metadata.TotalTriangleCount, 0);

		using var syncMesh = loader.LoadMesh(CrateFile, "crate-sync");
		using var asyncMesh = AwaitLoad(loader.LoadMeshAsync(CrateFile, "crate-async"));

		DescribeMesh("sync ", loader, syncMesh);
		DescribeMesh("async", loader, asyncMesh);

		Assert.AreEqual(syncMesh.BoundingBox, asyncMesh.BoundingBox, "Bounding boxes differ between sync and async load.");
		Assert.AreEqual("crate-sync", syncMesh.GetNameAsNewStringObject());
		Assert.AreEqual("crate-async", asyncMesh.GetNameAsNewStringObject());
	}

	void SyncAndAsyncShouldApplyCreationConfigTransformsIdentically() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var config = new MeshCreationConfig {
			Name = "transformed",
			OriginTranslation = new Vect(1.5f, -2f, 0.25f),
			LinearRescalingFactor = 3f,
			InvertTextureU = true,
			InvertTextureV = true,
			FlipTriangles = true,
			BoundingBoxAdditionalMargin = 0.05f
		};

		using var plainMesh = loader.LoadMesh(CrateFile, "plain");
		using var syncMesh = loader.LoadMesh(CrateFile, config);
		using var asyncMesh = AwaitLoad(loader.LoadMeshAsync(CrateFile, config));

		Console.WriteLine($"  plain bounds:       {plainMesh.BoundingBox}");
		Console.WriteLine($"  sync transformed:   {syncMesh.BoundingBox}");
		Console.WriteLine($"  async transformed:  {asyncMesh.BoundingBox}");

		Assert.AreEqual(syncMesh.BoundingBox, asyncMesh.BoundingBox, "Transformed bounding boxes differ between sync and async load.");
		Assert.AreNotEqual(plainMesh.BoundingBox, syncMesh.BoundingBox, "Transform config had no effect at all; test is vacuous.");
	}

	void SyncAndAsyncShouldProduceIdenticalSkeletalMeshes() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		foreach (var file in new[] { RiggedFile, CesiumManFile }) {
			Console.WriteLine($"--- {Path.GetFileName(file)} ---");
			using var syncMesh = loader.LoadMesh(file, "skel-sync");
			using var asyncMesh = AwaitLoad(loader.LoadMeshAsync(file, "skel-async"));

			DescribeMesh("sync ", loader, syncMesh);
			DescribeMesh("async", loader, asyncMesh);

			Assert.AreEqual(syncMesh.BoundingBox, asyncMesh.BoundingBox, $"{file}: bounding boxes differ.");

			var syncAnims = syncMesh.Animations.Select(a => a.GetNameAsNewStringObject()).ToList();
			var asyncAnims = asyncMesh.Animations.Select(a => a.GetNameAsNewStringObject()).ToList();
			Console.WriteLine($"  animations sync={String.Join(",", syncAnims)} async={String.Join(",", asyncAnims)}");
			CollectionAssert.AreEqual(syncAnims, asyncAnims, $"{file}: animation names differ.");

			var syncNodes = syncMesh.Skeleton.Nodes.Select(n => n.GetNameAsNewStringObject()).ToList();
			var asyncNodes = asyncMesh.Skeleton.Nodes.Select(n => n.GetNameAsNewStringObject()).ToList();
			Console.WriteLine($"  node names sync=[{String.Join(",", syncNodes)}]");
			Console.WriteLine($"  node names async=[{String.Join(",", asyncNodes)}]");
			CollectionAssert.AreEqual(syncNodes, asyncNodes, $"{file}: skeleton node names differ.");
			Assert.Greater(syncNodes.Count, 0, $"{file}: expected skeletal nodes but found none; test is vacuous.");
		}
	}

	void ConcurrentAsyncMeshLoadsShouldAllSucceed() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var ops = new[] {
			loader.LoadMeshAsync(CrateFile, "c0"),
			loader.LoadMeshAsync(RiggedFile, "c1"),
			loader.LoadMeshAsync(CesiumManFile, "c2"),
			loader.LoadMeshAsync(CrateFile, "c3")
		};

		var meshes = ops.Select(AwaitLoad).ToList();
		for (var i = 0; i < meshes.Count; ++i) {
			Console.WriteLine($"  concurrent[{i}] name='{meshes[i].GetNameAsNewStringObject()}' bounds={meshes[i].BoundingBox}");
			Assert.AreEqual($"c{i}", meshes[i].GetNameAsNewStringObject());
		}
		foreach (var mesh in meshes) mesh.Dispose();
	}

	void RepeatedLoadsShouldNotLeakOrCorrupt() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		PositionedCuboid? expectedBounds = null;
		for (var i = 0; i < 12; ++i) {
			using var mesh = (i % 2 == 0)
				? loader.LoadMesh(CrateFile, "repeat")
				: AwaitLoad(loader.LoadMeshAsync(CrateFile, "repeat"));
			expectedBounds ??= mesh.BoundingBox;
			Assert.AreEqual(expectedBounds.Value, mesh.BoundingBox, $"Iteration {i} produced different geometry.");
		}
		Console.WriteLine($"  12 alternating sync/async loads all produced bounds {expectedBounds}");
	}

	void LoadAllShouldStillWorkAfterModelsRework() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		foreach (var file in new[] { CesiumManFile, CommonTestAssets.FindAsset("models/DamagedHelmet.glb") }) {
			using var group = loader.LoadAll(file, Path.GetFileNameWithoutExtension(file));
			Console.WriteLine($"  {Path.GetFileName(file)}: {group.Meshes.Count} meshes, {group.Materials.Count} materials, {group.Textures.Count} textures");
			Assert.Greater(group.Meshes.Count, 0, $"{file}: LoadAll produced no meshes.");
			Assert.Greater(group.Materials.Count, 0, $"{file}: LoadAll produced no materials.");
		}
	}

	void ReadMeshShouldStillPopulateCallerBuffers() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var metadata = loader.ReadMeshMetadata(CrateFile);
		var vertices = new MeshVertex[metadata.TotalVertexCount];
		var triangles = new VertexTriangle[metadata.TotalTriangleCount];
		var counts = loader.ReadMesh(CrateFile, vertices, triangles);

		Console.WriteLine($"  ReadMesh wrote {counts.NumVerticesWritten} verts / {counts.NumTrianglesWritten} tris");
		Assert.AreEqual(metadata.TotalVertexCount, counts.NumVerticesWritten);
		Assert.AreEqual(metadata.TotalTriangleCount, counts.NumTrianglesWritten);
		Assert.IsTrue(vertices.Any(v => v.Location != Location.Origin), "ReadMesh left the vertex buffer empty.");
		Assert.IsTrue(triangles.Any(t => t.IndexA != 0 || t.IndexB != 0 || t.IndexC != 0), "ReadMesh left the triangle buffer empty.");
	}
}
