using System.IO;
using System.Linq;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Threading;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Testing;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalMeshAsyncLoadingTest {
	static readonly TimeSpan AsyncTimeout = TimeSpan.FromSeconds(60d);

	static string CrateFile => CommonTestAssets.FindAsset("ELCrate.obj");
	static string RiggedFile => CommonTestAssets.FindAsset("models/RiggedSimple.glb");
	static string CesiumManFile => CommonTestAssets.FindAsset("models/CesiumMan.glb");

	static Mesh AwaitLoad(TinyFfrAsyncOperation<Mesh> op) {
		Assert.IsTrue(op.WaitForCompletion(AsyncTimeout), "Async mesh load timed out.");
		return op.GetResultAndDisposeOperation();
	}

	static void DescribeMesh(string label, IAssetLoader loader, Mesh mesh) {
		Console.WriteLine($"  {label}: name='{mesh.GetNameAsNewStringObject()}' bounds={mesh.BoundingBox} animations={mesh.Animations.Count()} nodes={mesh.Skeleton.Nodes.Count()}");
	}

	[Test]
	public void SyncAndAsyncShouldProduceIdenticalNonSkeletalMeshes() {
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

	[Test]
	public void SyncAndAsyncShouldApplyCreationConfigTransformsIdentically() {
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

	[Test]
	public void SyncAndAsyncShouldProduceIdenticalSkeletalMeshes() {
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

	[Test]
	public void ConcurrentAsyncLoadsShouldAllSucceed() {
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

	[Test]
	public void RepeatedLoadsShouldNotLeakOrCorrupt() {
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

	[Test]
	public void LoadAllShouldStillWorkAfterModelsRework() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		foreach (var file in new[] { CesiumManFile, CommonTestAssets.FindAsset("models/DamagedHelmet.glb") }) {
			using var group = loader.LoadAll(file, Path.GetFileNameWithoutExtension(file));
			Console.WriteLine($"  {Path.GetFileName(file)}: {group.Meshes.Count} meshes, {group.Materials.Count} materials, {group.Textures.Count} textures");
			Assert.Greater(group.Meshes.Count, 0, $"{file}: LoadAll produced no meshes.");
			Assert.Greater(group.Materials.Count, 0, $"{file}: LoadAll produced no materials.");
		}
	}

	[Test]
	public void ReadMeshShouldStillPopulateCallerBuffers() {
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
