// Created on 2026-09-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Numerics;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Meshes;

[TestFixture]
class ArrowMeshTest {
	const float StemLength = 0.7f;
	const float StemRadius = 0.05f;
	const float HeadLength = 0.3f;
	const float HeadRadius = 0.12f;
	const float TotalLength = StemLength + HeadLength;
	const float Tolerance = 1E-4f;

	sealed class CapturingMeshBuilder : IMeshBuilder, IDisposable {
		readonly HeapPool _pool = new();
		public MeshVertex[] Vertices { get; private set; } = Array.Empty<MeshVertex>();
		public VertexTriangle[] Triangles { get; private set; } = Array.Empty<VertexTriangle>();
		public Vect OriginTranslation { get; private set; }

		public IMeshPolygonGroup AllocateNewPolygonGroup() => throw new NotSupportedException();
		ScopedSpanLease<MeshVertex> IMeshBuilder.GetPooledVertexBuffer(int vertexCount) => _pool.CreateSpanLease<MeshVertex>(vertexCount);
		ScopedSpanLease<VertexTriangle> IMeshBuilder.GetPooledTriangleBuffer(int triangleCount) => _pool.CreateSpanLease<VertexTriangle>(triangleCount);
		public DynamicVertexBuffer CreateDynamicVertexBuffer(int initialVertexCapacity, int initialIndexCapacity, ReadOnlySpan<char> name = default) => throw new NotSupportedException();
		public Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, in MeshCreationConfig config) {
			Vertices = vertices.ToArray();
			Triangles = triangles.ToArray();
			OriginTranslation = config.OriginTranslation;
			return default;
		}
		public Mesh CreateMesh(ReadOnlySpan<MeshVertexSkeletal> vertices, ReadOnlySpan<VertexTriangle> triangles, ReadOnlySpan<SkeletalAnimationNode> skeletalNodes, in MeshCreationConfig config) => throw new NotSupportedException();
		public void SetSkeletonNodeName(Mesh mesh, int nodeIndex, ReadOnlySpan<char> name) => throw new NotSupportedException();
		public MeshAnimation AttachAnimation(Mesh mesh, ReadOnlySpan<SkeletalAnimationScalingKeyframe> scalingKeyframes, ReadOnlySpan<SkeletalAnimationRotationKeyframe> rotationKeyframes, ReadOnlySpan<SkeletalAnimationTranslationKeyframe> translationKeyframes, ReadOnlySpan<SkeletalAnimationNodeMutationDescriptor> boneMutations, float defaultCompletionTimeSeconds, ReadOnlySpan<char> name) => throw new NotSupportedException();
		public void Dispose() => _pool.Dispose();
	}

	CapturingMeshBuilder _builder = null!;
	IMeshBuilder Builder => _builder;

	[SetUp]
	public void SetUpTest() => _builder = new CapturingMeshBuilder();

	[TearDown]
	public void TearDownTest() => _builder.Dispose();

	static Vector3 FaceNormal(MeshVertex a, MeshVertex b, MeshVertex c) {
		return Vector3.Cross(b.Location.ToVector3() - a.Location.ToVector3(), c.Location.ToVector3() - a.Location.ToVector3());
	}

	[Test]
	public void WindingConventionShouldMatchQuadMesh() {
		_ = Builder.CreateQuadMesh(twoSided: false);
		var t = _builder.Triangles[0];
		var faceNormal = Vector3.Normalize(FaceNormal(_builder.Vertices[t.IndexA], _builder.Vertices[t.IndexB], _builder.Vertices[t.IndexC]));
		Assert.AreEqual(-1f, faceNormal.Z, Tolerance);
	}

	[Test]
	[TestCase(3)]
	[TestCase(8)]
	[TestCase(32)]
	public void ShouldGenerateExpectedVertexAndTriangleCounts(int segmentCount) {
		_ = Builder.CreateMesh(StemLength, StemRadius, HeadLength, HeadRadius, segmentCount: segmentCount);
		Assert.AreEqual(segmentCount * 8 + 6, _builder.Vertices.Length);
		Assert.AreEqual(segmentCount * 6, _builder.Triangles.Length);
		foreach (var t in _builder.Triangles) {
			Assert.That(t.IndexA, Is.InRange(0, _builder.Vertices.Length - 1));
			Assert.That(t.IndexB, Is.InRange(0, _builder.Vertices.Length - 1));
			Assert.That(t.IndexC, Is.InRange(0, _builder.Vertices.Length - 1));
		}
	}

	[Test]
	public void ShouldBeBoundedByGivenDimensions() {
		_ = Builder.CreateMesh(StemLength, StemRadius, HeadLength, HeadRadius);
		var tipCount = 0;
		foreach (var v in _builder.Vertices) {
			var l = v.Location;
			var radius = MathF.Sqrt(l.X * l.X + l.Y * l.Y);
			Assert.That(l.Z, Is.InRange(-Tolerance, TotalLength + Tolerance));
			Assert.That(radius, Is.LessThanOrEqualTo(HeadRadius + Tolerance));
			if (l.Z < StemLength - Tolerance) Assert.That(radius, Is.LessThanOrEqualTo(StemRadius + Tolerance));
			if (MathF.Abs(l.Z - TotalLength) < Tolerance) {
				Assert.AreEqual(0f, radius, Tolerance);
				++tipCount;
			}
		}
		Assert.AreEqual(IMeshBuilder.DefaultArrowSegmentCount, tipCount);
	}

	[Test]
	public void TextureCoordsShouldWrapAroundAndRunTailToHead() {
		_ = Builder.CreateMesh(StemLength, StemRadius, HeadLength, HeadRadius);
		var sawUZero = false;
		var sawUOne = false;
		foreach (var v in _builder.Vertices) {
			Assert.AreEqual(v.Location.Z / TotalLength, v.TextureCoords.Y, Tolerance);
			Assert.That(v.TextureCoords.X, Is.InRange(0f, 1f));
			if (v.TextureCoords.X == 0f) sawUZero = true;
			if (v.TextureCoords.X == 1f) sawUOne = true;
		}
		Assert.IsTrue(sawUZero);
		Assert.IsTrue(sawUOne);
	}

	[Test]
	public void ShouldApplyTextureTransform() {
		_ = Builder.CreateMesh(StemLength, StemRadius, HeadLength, HeadRadius, textureTransform: Transform2D.FromScalingOnly(2f));
		foreach (var v in _builder.Vertices) {
			Assert.AreEqual(v.Location.Z / TotalLength * 2f, v.TextureCoords.Y, Tolerance);
		}
	}

	[Test]
	public void AllTrianglesShouldFaceOutwards() {
		_ = Builder.CreateMesh(StemLength, StemRadius, HeadLength, HeadRadius);
		foreach (var t in _builder.Triangles) {
			var a = _builder.Vertices[t.IndexA];
			var b = _builder.Vertices[t.IndexB];
			var c = _builder.Vertices[t.IndexC];
			var faceNormal = FaceNormal(a, b, c);
			Assert.That(faceNormal.LengthSquared(), Is.GreaterThan(0f));

			var isFlatDisc = MathF.Abs(a.Location.Z - b.Location.Z) < Tolerance && MathF.Abs(b.Location.Z - c.Location.Z) < Tolerance;
			if (isFlatDisc) {
				Assert.That(faceNormal.Z, Is.LessThan(0f));
			}
			else {
				var centroid = (a.Location.ToVector3() + b.Location.ToVector3() + c.Location.ToVector3()) / 3f;
				Assert.That(Vector3.Dot(faceNormal, centroid with { Z = 0f }), Is.GreaterThan(0f));
			}
		}
	}

	[Test]
	public void ShouldTranslateOriginAccordingToEnum() {
		_ = Builder.CreateMesh(StemLength, StemRadius, HeadLength, HeadRadius);
		Assert.AreEqual(Vect.Zero, _builder.OriginTranslation);

		_ = Builder.CreateMesh(StemLength, StemRadius, HeadLength, HeadRadius, ArrowMeshOrigin.HeadTip);
		Assert.AreEqual(TotalLength, _builder.OriginTranslation.Z, Tolerance);
		Assert.AreEqual(0f, _builder.OriginTranslation.X);
		Assert.AreEqual(0f, _builder.OriginTranslation.Y);

		_ = Builder.CreateMesh(StemLength, StemRadius, HeadLength, HeadRadius, ArrowMeshOrigin.Centre);
		Assert.AreEqual(TotalLength * 0.5f, _builder.OriginTranslation.Z, Tolerance);

		_ = Builder.CreateMesh(StemLength, StemRadius, HeadLength, HeadRadius, ArrowMeshOrigin.HeadTip, IMeshBuilder.DefaultArrowSegmentCount, new MeshGenerationConfig(), new MeshCreationConfig { OriginTranslation = new Vect(1f, 2f, 3f) });
		Assert.AreEqual(new Vect(1f, 2f, 3f + TotalLength), _builder.OriginTranslation);
	}

	[Test]
	public void ShouldThrowOnInvalidArguments() {
		Assert.Throws<ArgumentOutOfRangeException>(() => Builder.CreateMesh(0f, StemRadius, HeadLength, HeadRadius));
		Assert.Throws<ArgumentOutOfRangeException>(() => Builder.CreateMesh(StemLength, -1f, HeadLength, HeadRadius));
		Assert.Throws<ArgumentOutOfRangeException>(() => Builder.CreateMesh(StemLength, StemRadius, 0f, HeadRadius));
		Assert.Throws<ArgumentOutOfRangeException>(() => Builder.CreateMesh(StemLength, StemRadius, HeadLength, 0f));
		Assert.Throws<ArgumentOutOfRangeException>(() => Builder.CreateMesh(StemLength, StemRadius, HeadLength, float.NaN));
		Assert.Throws<ArgumentOutOfRangeException>(() => Builder.CreateMesh(StemLength, StemRadius, HeadLength, HeadRadius, segmentCount: 2));
		Assert.Throws<ArgumentException>(() => Builder.CreateMesh(StemLength, HeadRadius, HeadLength, StemRadius));
		Assert.DoesNotThrow(() => Builder.CreateMesh(StemLength, StemRadius, HeadLength, StemRadius));
	}
}
