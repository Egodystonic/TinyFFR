// Created on 2026-09-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Numerics;

namespace Egodystonic.TinyFFR.Assets.Meshes;

[TestFixture]
class MeshGroupAnimationTableTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void EmptyTableShouldHaveNoAnimationsOrNodes() {
		var empty = MeshGroupAnimationTable.Empty;

		Assert.AreEqual(0, empty.Animations.Count);
		Assert.AreEqual(0, empty.Animations.Skeletal.Count);
		Assert.AreEqual(0, empty.Animations.Morphing.Count);
		Assert.IsNull(empty.Animations.TryGetAnimationByName("Walk"));
		Assert.Throws<KeyNotFoundException>(() => _ = empty.Animations["Walk"]);
		Assert.AreEqual(0, empty.Skeleton.Nodes.Count);
		Assert.IsNull(empty.Skeleton.Nodes.TryGetNodeByName("Root"));
	}

	[Test]
	public void EmptyTableShouldNeverBeDisposed() {
		var empty = MeshGroupAnimationTable.Empty;

		Assert.DoesNotThrow(empty.Dispose);
		Assert.IsFalse(empty.IsDisposed);
		Assert.AreEqual(empty.GetNameLength(), empty.GetNameAsNewStringObject().Length);
		Assert.AreEqual(MeshGroupAnimationTable.Empty, empty);
	}

	[Test]
	public void EmptyTableBindPoseQueriesShouldOnlyRejectNonEmptyRequests() {
		var empty = MeshGroupAnimationTable.Empty;

		Assert.DoesNotThrow(() => empty.Skeleton.GetBindPoseNodeTransforms(ReadOnlySpan<int>.Empty, Span<Matrix4x4>.Empty));
		Assert.Throws<ArgumentException>(() => empty.Skeleton.GetBindPoseNodeTransforms(new[] { 0 }, new Matrix4x4[1]));
	}
}
