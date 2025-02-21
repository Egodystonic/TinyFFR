// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalMeshPolygonGroupTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var vertexList = new List<Location>();

		using var polyGroup = factory.AssetLoader.MeshBuilder.AllocateNewPolygonGroup();
		AssertGroupProps(polyGroup, 0, 0, 0, 0, 0);
		polyGroup.Add(CreatePolygon(vertexList, 5, false, out var p1D), Direction.Forward, Direction.Up, Location.Origin);
		AssertGroupProps(polyGroup, 1, 5, 3, 5, 3);
		polyGroup.Add(CreatePolygon(vertexList, 3, true, out var p2D), Direction.Down, Direction.Backward, (1f, 1f, 1f));
		AssertGroupProps(polyGroup, 2, 8, 4, 5, 3);
		polyGroup.Add(CreatePolygon(vertexList, 11, false, out var p3D), Direction.Right, Direction.Left, (-1f, -1f, -1f));
		AssertGroupProps(polyGroup, 3, 19, 13, 11, 9);

		var p1 = polyGroup.GetPolygonAtIndex(0, out var p1U, out var p1V, out var p1O);
		var p2 = polyGroup.GetPolygonAtIndex(1, out var p2U, out var p2V, out var p2O);
		var p3 = polyGroup.GetPolygonAtIndex(2, out var p3U, out var p3V, out var p3O);
		Assert.Throws<ArgumentOutOfRangeException>(() => polyGroup.GetPolygonAtIndex(-1, out _, out _, out _));
		Assert.Throws<ArgumentOutOfRangeException>(() => polyGroup.GetPolygonAtIndex(3, out _, out _, out _));
		
		Assert.AreEqual(Direction.Forward, p1U);
		Assert.AreEqual(Direction.Up, p1V);
		Assert.AreEqual(new Location(0f, 0f, 0f), p1O);
		Assert.IsTrue(p1.Vertices.SequenceEqual(vertexList.ToArray().AsSpan(0, 5)));
		Assert.AreEqual(p1D, p1.Normal);
		Assert.AreEqual(false, p1.IsWoundClockwise);

		Assert.AreEqual(Direction.Down, p2U);
		Assert.AreEqual(Direction.Backward, p2V);
		Assert.AreEqual(new Location(1f, 1f, 1f), p2O);
		Assert.IsTrue(p2.Vertices.SequenceEqual(vertexList.ToArray().AsSpan(5, 3)));
		Assert.AreEqual(p2D, p2.Normal);
		Assert.AreEqual(true, p2.IsWoundClockwise);

		Assert.AreEqual(Direction.Right, p3U);
		Assert.AreEqual(Direction.Left, p3V);
		Assert.AreEqual(new Location(-1f, -1f, -1f), p3O);
		Assert.IsTrue(p3.Vertices.SequenceEqual(vertexList.ToArray().AsSpan(8, 11)));
		Assert.AreEqual(p3D, p3.Normal);
		Assert.AreEqual(false, p3.IsWoundClockwise);

		polyGroup.Clear();
		AssertGroupProps(polyGroup, 0, 0, 0, 0, 0);
	}

	Polygon CreatePolygon(List<Location> vertexBuffer, int numVertices, bool clockwiseWinding, out Direction normal) {
		var curCount = vertexBuffer.Count;
		var plane = Plane.Random();
		for (var i = 0; i < numVertices; ++i) {
			vertexBuffer.Add(Location.Random().ClosestPointOn(plane));
		}
		normal = plane.Normal;
		return new Polygon(CollectionsMarshal.AsSpan(vertexBuffer)[curCount..], plane.Normal, isWoundClockwise: clockwiseWinding);
	}

	void AssertGroupProps(IMeshPolygonGroup group, int expectedPolygonCount, int expectedVertexCount, int expectedTriangleCount, int expectedHighestIndividualVertexCount, int expectedIndividualTriangleCount) {
		Assert.AreEqual(expectedPolygonCount, group.TotalPolygonCount);
		Assert.AreEqual(expectedVertexCount, group.TotalVertexCount);
		Assert.AreEqual(expectedTriangleCount, group.TotalTriangleCount);
		Assert.AreEqual(expectedHighestIndividualVertexCount, group.HighestIndividualVertexCount);
		Assert.AreEqual(expectedIndividualTriangleCount, group.HighestIndividualTriangleCount);
	}
} 