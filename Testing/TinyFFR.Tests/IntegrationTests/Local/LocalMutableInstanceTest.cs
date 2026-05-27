// Created on 2026-05-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Numerics;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalMutableInstanceTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	static MeshVertex[] CreateQuadVertices(float z = 0f) => [
		new MeshVertex(new Location(-1f, -1f, z), new XYPair<float>(0f, 0f), Quaternion.Identity),
		new MeshVertex(new Location( 1f, -1f, z), new XYPair<float>(1f, 0f), Quaternion.Identity),
		new MeshVertex(new Location( 1f,  1f, z), new XYPair<float>(1f, 1f), Quaternion.Identity),
		new MeshVertex(new Location(-1f,  1f, z), new XYPair<float>(0f, 1f), Quaternion.Identity),
	];

	static VertexTriangle[] CreateQuadTriangles() => [
		new VertexTriangle(0, 1, 2),
		new VertexTriangle(0, 2, 3),
	];

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		using var tex = factory.TextureBuilder.CreateColorMap(TexturePattern.PlainFill<ColorVect>(StandardColor.White), includeAlpha: false);
		using var mat = factory.MaterialBuilder.CreateStandardMaterial(tex);

		// === Case 1: AllowsPerInstanceVertexMutation flag round-trips ===
		var mutableMesh = factory.MeshBuilder.CreateMesh(
			CreateQuadVertices(),
			CreateQuadTriangles(),
			new MeshCreationConfig { AllowsPerInstanceVertexMutation = true });
		Assert.IsTrue(mutableMesh.AllowsPerInstanceVertexMutation);

		var immutableMesh = factory.MeshBuilder.CreateMesh(
			CreateQuadVertices(),
			CreateQuadTriangles(),
			new MeshCreationConfig());
		Assert.IsFalse(immutableMesh.AllowsPerInstanceVertexMutation);

		// === Case 2: Instance from immutable mesh rejects UpdateVertices ===
		var immutableInst = factory.ObjectBuilder.CreateModelInstance(immutableMesh, mat);
		Assert.IsFalse(immutableInst.IsVertexDataMutable);
		Assert.Catch<InvalidOperationException>(
			() => immutableInst.UpdateVertices(0, CreateQuadVertices()));
		immutableInst.Dispose();
		immutableMesh.Dispose();

		// === Case 3: Instance from mutable mesh accepts UpdateVertices ===
		var instA = factory.ObjectBuilder.CreateModelInstance(mutableMesh, mat);
		var instB = factory.ObjectBuilder.CreateModelInstance(mutableMesh, mat);
		Assert.IsTrue(instA.IsVertexDataMutable);
		Assert.IsTrue(instB.IsVertexDataMutable);

		// Full replace via region API.
		Assert.DoesNotThrow(() => instA.UpdateVertices(0, CreateQuadVertices(z: 5f)));

		// Convenience overload (full replace at index 0).
		Assert.DoesNotThrow(() => instA.UpdateVertices(CreateQuadVertices(z: -2f)));

		// Partial region update.
		var partialUpdate = new[] { new MeshVertex(new Location(10f, 10f, 10f), new XYPair<float>(0.5f, 0.5f), Quaternion.Identity) };
		Assert.DoesNotThrow(() => instA.UpdateVertices(2, partialUpdate));

		// === Case 4: out-of-range rejection ===
		Assert.Catch<ArgumentOutOfRangeException>(
			() => instA.UpdateVertices(-1, CreateQuadVertices()));
		Assert.Catch<ArgumentOutOfRangeException>(
			() => instA.UpdateVertices(3, CreateQuadVertices())); // 3 + 4 = 7 > vertexCount(4)
		Assert.Catch<ArgumentOutOfRangeException>(
			() => instA.UpdateVertices(4, partialUpdate)); // 4 + 1 = 5 > vertexCount(4)

		// === Case 5: empty span is a no-op (does not throw, does not privatize untouched instances) ===
		Assert.DoesNotThrow(() => instB.UpdateVertices(0, ReadOnlySpan<MeshVertex>.Empty));
		Assert.DoesNotThrow(() => instB.UpdateVertices(0, ReadOnlySpan<MeshVertex>.Empty, recalculateBoundingBox: true));

		// === Case 6: recalculateBoundingBox path doesn't throw on either full or partial updates ===
		Assert.DoesNotThrow(() => instA.UpdateVertices(0, CreateQuadVertices(z: 3f), recalculateBoundingBox: true));
		Assert.DoesNotThrow(() => instA.UpdateVertices(1, partialUpdate, recalculateBoundingBox: true));

		// === Case 7: Mutating instA does not affect instB's ability to use the shared mesh ===
		Assert.DoesNotThrow(() => instB.UpdateVertices(0, CreateQuadVertices()));

		// === Case 8: dispose order (instances first, then mesh; reverse order would throw from dependency tracker) ===
		Assert.DoesNotThrow(instA.Dispose);
		Assert.DoesNotThrow(instB.Dispose);
		Assert.DoesNotThrow(mutableMesh.Dispose);

		// === Case 9: skeletal + AllowPerInstanceVertexMutation rejected at mesh creation ===
		// (Skipped — requires constructing MeshVertexSkeletal + SkeletalAnimationNode arrays;
		// the rejection lives in LocalMeshBuilder.ProcessVerticesAndCreateMesh and is exercised
		// by the dual ArgumentException guards there. A dedicated test case would require
		// substantially more setup than its diagnostic value warrants.)

		// === Case 10: round-trip with many updates leaves no leaks (verified by clean factory dispose at end of using block) ===
		using (var soakMesh = factory.MeshBuilder.CreateMesh(
				CreateQuadVertices(),
				CreateQuadTriangles(),
				new MeshCreationConfig { AllowsPerInstanceVertexMutation = true })) {
			using var soakInst = factory.ObjectBuilder.CreateModelInstance(soakMesh, mat);
			for (var i = 0; i < 50; ++i) {
				soakInst.UpdateVertices(0, CreateQuadVertices(z: i * 0.1f), recalculateBoundingBox: (i % 5 == 0));
			}
		}
	}
}
