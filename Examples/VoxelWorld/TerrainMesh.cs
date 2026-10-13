using System.Numerics;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace VoxelWorld;

sealed class TerrainMeshData {
	public readonly MeshVertex[] Vertices = new MeshVertex[WorldSettings.MaxFacesPerMesh * 4];
	public readonly VertexTriangle[] Triangles = new VertexTriangle[WorldSettings.MaxFacesPerMesh * 2];
	public int VertexCount;
	public int TriangleCount;
	public int LowestY;
	public int HighestY;
	public bool Truncated;

	public void Clear() {
		VertexCount = 0;
		TriangleCount = 0;
		LowestY = WorldSettings.ChunkHeight;
		HighestY = 0;
		Truncated = false;
	}
}

readonly record struct FaceDefinition(int NormalX, int NormalY, int NormalZ, Vector3 Tangent, Vector3 Bitangent, BlockFace Face, Quaternion TangentRotation) {
	public FaceDefinition(int normalX, int normalY, int normalZ, Direction tangent, Direction bitangent, BlockFace face)
		: this(normalX, normalY, normalZ, tangent.ToVector3(), bitangent.ToVector3(), face, MeshVertex.CalculateTangentRotation(tangent, bitangent, new Direction(normalX, normalY, normalZ))) { }

	public static readonly FaceDefinition[] All = {
		new(0, 1, 0, Direction.Left, Direction.Backward, BlockFace.Top),
		new(0, -1, 0, Direction.Left, Direction.Forward, BlockFace.Bottom),
		new(1, 0, 0, Direction.Backward, Direction.Up, BlockFace.Side),
		new(-1, 0, 0, Direction.Forward, Direction.Up, BlockFace.Side),
		new(0, 0, 1, Direction.Left, Direction.Up, BlockFace.Side),
		new(0, 0, -1, Direction.Right, Direction.Up, BlockFace.Side)
	};
}

static class TerrainMeshWriter {
	public static bool TryAddQuad(TerrainMeshData output, Vector3 faceCentre, in FaceDefinition face, float width, float height, int tile) {
		if (output.VertexCount + 4 > output.Vertices.Length) {
			output.Truncated = true;
			return false;
		}

		BlockTextures.GetTileUvs(tile, out var uMin, out var vMin, out var uMax, out var vMax);
		var halfTangent = face.Tangent * (width * 0.5f);
		var halfBitangent = face.Bitangent * (height * 0.5f);
		var corner0 = faceCentre - halfTangent - halfBitangent;
		var corner1 = faceCentre + halfTangent - halfBitangent;
		var corner2 = faceCentre + halfTangent + halfBitangent;
		var corner3 = faceCentre - halfTangent + halfBitangent;

		var firstVertex = output.VertexCount;
		output.Vertices[firstVertex] = new MeshVertex(Location.FromVector3(corner0), new XYPair<float>(uMin, vMin), face.TangentRotation);
		output.Vertices[firstVertex + 1] = new MeshVertex(Location.FromVector3(corner1), new XYPair<float>(uMax, vMin), face.TangentRotation);
		output.Vertices[firstVertex + 2] = new MeshVertex(Location.FromVector3(corner2), new XYPair<float>(uMax, vMax), face.TangentRotation);
		output.Vertices[firstVertex + 3] = new MeshVertex(Location.FromVector3(corner3), new XYPair<float>(uMin, vMax), face.TangentRotation);
		output.VertexCount += 4;

		output.Triangles[output.TriangleCount] = new VertexTriangle(firstVertex, firstVertex + 1, firstVertex + 2);
		output.Triangles[output.TriangleCount + 1] = new VertexTriangle(firstVertex, firstVertex + 2, firstVertex + 3);
		output.TriangleCount += 2;

		var lowest = (int) MathF.Floor(MathF.Min(corner0.Y, corner2.Y));
		var highest = (int) MathF.Ceiling(MathF.Max(corner0.Y, corner2.Y));
		output.LowestY = Math.Min(output.LowestY, lowest);
		output.HighestY = Math.Max(output.HighestY, highest);
		return true;
	}
}
