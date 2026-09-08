// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Benchmarks.Smoke;

static partial class SmokeSections {
	public static void ProceduralMeshes() {
		var polygonVertices = Allocator.CreatePooledMemoryBuffer<Location>(SmokeWorkload.PolygonVertexCount);

		try {
			var vertexSpan = polygonVertices.Span;
			for (var i = 0; i < vertexSpan.Length; ++i) {
				var angle = new Angle(360f * (i / (float) vertexSpan.Length));
				vertexSpan[i] = new Location(MathF.Cos(angle.Radians), 0f, -MathF.Sin(angle.Radians));
			}

			for (var repeat = 0; repeat < SmokeWorkload.MeshBuildRepeatCount; ++repeat) {
				using var cuboid = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Benchmark Cuboid Mesh");
				using var sphere = Factory.MeshBuilder.CreateMesh(new Sphere(0.5f), subdivisionLevel: SmokeWorkload.SphereSubdivisionLevel, name: "Benchmark Sphere Mesh");
				using var configuredCuboid = Factory.MeshBuilder.CreateMesh(
					new Cuboid(0.8f),
					centreTextureOrigin: false,
					new MeshGenerationConfig { TextureTransform = Transform2D.None },
					new MeshCreationConfig { Name = "Benchmark Configured Cuboid Mesh", GenerateWireframeData = true }
				);

				using var polygonGroup = Factory.MeshBuilder.AllocateNewPolygonGroup();
				polygonGroup.Add(new Polygon(vertexSpan, Direction.Up), Direction.Right, Direction.Forward, Location.Origin);
				using var polygonMesh = Factory.MeshBuilder.CreateMesh(polygonGroup, name: "Benchmark Polygon Mesh");

				using var quadMesh = Factory.MeshBuilder.CreateQuadMesh(name: "Benchmark Quad Mesh");
			}
		}
		finally {
			Allocator.ReturnPooledMemoryBuffer(polygonVertices);
		}
	}

	public static void MeshVertexMutation() {
		using var mesh = Factory.MeshBuilder.CreateMesh(
			new Sphere(0.5f),
			SmokeWorkload.SphereSubdivisionLevel,
			new MeshGenerationConfig(),
			new MeshCreationConfig { Name = "Benchmark Mutable Mesh", AllowsPerInstanceVertexMutation = true }
		);
		using var material = Factory.MaterialBuilder.CreateTestMaterial();
		using var instance = Factory.ObjectBuilder.CreateModelInstance(mesh, material, name: "Benchmark Mutable Instance");

		for (var pass = 0; pass < SmokeWorkload.MutableMeshVertexPassCount; ++pass) {
			using (var lease = instance.BorrowVerticesSpan(recalculateBoundingBoxOnLeaseDispose: true)) {
				var span = lease.Span;
				for (var i = 0; i < span.Length; ++i) span[i] = span[i] with { Location = span[i].Location + Direction.Up * 0.0001f };
			}

			using (var readLease = instance.BorrowVerticesSpanReadOnly()) {
				var span = readLease.Span;
				var accumulator = 0f;
				for (var i = 0; i < span.Length; ++i) accumulator += span[i].Location.X;
			}

			instance.TriggerManualBoundingBoxRecalculation();
			_ = instance.GetModelSpaceBoundingBox();
		}
	}

	public static void DynamicVertexBuffers() {
		for (var repeat = 0; repeat < SmokeWorkload.DynamicBufferRepeatCount; ++repeat) {
			using var buffer = Factory.MeshBuilder.CreateDynamicVertexBuffer(SmokeWorkload.DynamicBufferVertexCount, SmokeWorkload.DynamicBufferIndexCount, "Benchmark Dynamic Vertex Buffer");

			using (var vertexLease = buffer.BorrowVerticesSpan(recalculateBoundingBoxOnLeaseDispose: false, overwriteChildMeshBoundingBoxes: false)) {
				var span = vertexLease.Span;
				for (var i = 0; i < span.Length; ++i) {
					var fraction = i / (float) span.Length;
					span[i] = new MeshVertex(new Location(fraction, 0f, 1f - fraction), new XYPair<float>(fraction, fraction), Direction.Right, Direction.Forward, Direction.Up);
				}
			}

			using (var indexLease = buffer.BorrowIndicesSpan(recalculateBoundingBoxOnLeaseDispose: true, overwriteChildMeshBoundingBoxes: true)) {
				var span = indexLease.Span;
				for (var i = 0; i < span.Length; ++i) span[i] = (ushort) (i % SmokeWorkload.DynamicBufferVertexCount);
			}

			using var view = buffer.CreateMesh();
			_ = buffer.VertexBufferSize;
			_ = buffer.IndexBufferSize;
		}
	}

	public static void MutableGridMeshes() {
		using var material = Factory.MaterialBuilder.CreateTestMaterial();

		for (var repeat = 0; repeat < SmokeWorkload.GridRepeatCount; ++repeat) {
			using var gridMesh = Factory.MeshBuilder.CreateMutableGridMesh((SmokeWorkload.GridDimension, SmokeWorkload.GridDimension), name: "Benchmark Grid Mesh");
			using var gridInstance = Factory.ObjectBuilder.CreateMutableGridInstance(gridMesh, material, Location.Origin, (2f, 2f), "Benchmark Grid Instance");

			gridInstance.SetPosition(Location.Origin + Direction.Forward * 2f);
			for (var i = 0; i < SmokeWorkload.GridDimension; ++i) {
				_ = gridMesh.GetVertexIndex((i, i));
				_ = gridMesh.GetVertexCoordinateNormalized(i);
			}
		}
	}
}
