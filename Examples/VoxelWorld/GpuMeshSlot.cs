using System.Numerics;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace VoxelWorld;

sealed class GpuMeshSlot : IDisposable {
	readonly ILocalTinyFfrFactory _factory;
	readonly Scene _scene;
	readonly Material _material;
	DynamicVertexBuffer _vertexBuffer;
	Mesh _mesh;
	ModelInstance _instance;
	int _vertexCapacity;
	int _triangleCapacity;
	int _uploadedTriangleCount;

	public bool IsVisible { get; private set; }

	public GpuMeshSlot(ILocalTinyFfrFactory factory, Scene scene, Material material, int initialFaceCapacity) {
		_factory = factory;
		_scene = scene;
		_material = material;
		CreateResources(initialFaceCapacity * 4, initialFaceCapacity * 2);
	}

	public bool Upload(TerrainMeshData meshData, Location position, float footprintSize) {
		var resized = EnsureCapacity(meshData.VertexCount, meshData.TriangleCount);

		if (meshData.VertexCount > 0) {
			using var vertices = _vertexBuffer.BorrowVerticesSpan(recalculateBoundingBoxOnLeaseDispose: false, overwriteChildMeshBoundingBoxes: false, range: ..meshData.VertexCount);
			meshData.Vertices.AsSpan(0, meshData.VertexCount).CopyTo(vertices.Span);
		}

		var trianglesToWrite = Math.Max(meshData.TriangleCount, _uploadedTriangleCount);
		if (trianglesToWrite > 0) {
			using var triangles = _vertexBuffer.BorrowTrianglesSpan(recalculateBoundingBoxOnLeaseDispose: false, overwriteChildMeshBoundingBoxes: false, range: ..trianglesToWrite);
			meshData.Triangles.AsSpan(0, meshData.TriangleCount).CopyTo(triangles.Span);
			triangles.Span[meshData.TriangleCount..].Clear();
		}
		_uploadedTriangleCount = meshData.TriangleCount;

		var lowestY = Math.Min(meshData.LowestY, meshData.HighestY);
		var height = Math.Max(meshData.HighestY - lowestY, 1);
		var boundingBox = new PositionedCuboid(
			footprintSize, 
			height, 
			footprintSize, 
			new Location(footprintSize * 0.5f, lowestY + height * 0.5f, footprintSize * 0.5f)
		);
		_vertexBuffer.SetBoundingBox(boundingBox, overwriteChildMeshBoundingBoxes: true);
		_instance.Position = position;
		return resized;
	}

	public void Show() {
		if (IsVisible) return;
		_scene.Add(_instance);
		IsVisible = true;
	}

	public void Hide() {
		if (!IsVisible) return;
		_scene.Remove(_instance);
		IsVisible = false;
	}

	bool EnsureCapacity(int vertexCount, int triangleCount) {
		if (vertexCount <= _vertexCapacity && triangleCount <= _triangleCapacity) return false;

		var wasVisible = IsVisible;
		Hide();
		DisposeResources();
		CreateResources(
			Math.Max(_vertexCapacity * 2, (int) BitOperations.RoundUpToPowerOf2((uint) vertexCount)),
			Math.Max(_triangleCapacity * 2, (int) BitOperations.RoundUpToPowerOf2((uint) triangleCount))
		);
		if (wasVisible) Show();
		return true;
	}

	void CreateResources(int vertexCapacity, int triangleCapacity) {
		_vertexCapacity = vertexCapacity;
		_triangleCapacity = triangleCapacity;
		_vertexBuffer = _factory.MeshBuilder.CreateDynamicVertexBuffer(vertexCapacity, triangleCapacity);
		_mesh = _vertexBuffer.CreateMesh();
		_instance = _factory.ObjectBuilder.CreateModelInstance(_mesh, _material);
		_uploadedTriangleCount = 0;
	}

	void DisposeResources() {
		_instance.Dispose();
		_mesh.Dispose();
		_vertexBuffer.Dispose();
	}

	public void Dispose() {
		Hide();
		DisposeResources();
	}
}
