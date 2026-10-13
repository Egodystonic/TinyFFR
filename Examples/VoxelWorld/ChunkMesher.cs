using System.Numerics;

namespace VoxelWorld;

static class ChunkMesher {
	public static void BuildMesh(ReadOnlySpan<BlockType> borderedBlocks, ReadOnlySpan<byte> borderedColumnHeights, TerrainMeshData output) {
		output.Clear();
		var faces = FaceDefinition.All;

		for (var x = 0; x < WorldSettings.ChunkSize; ++x) {
			for (var z = 0; z < WorldSettings.ChunkSize; ++z) {
				var columnTop = borderedColumnHeights[(x + 1) * TerrainGenerator.BorderedChunkSize + (z + 1)];
				for (var y = 0; y <= columnTop; ++y) {
					var block = borderedBlocks[TerrainGenerator.GetBlockIndex(x + 1, y, z + 1)];
					if (block == BlockType.Air) continue;

					for (var f = 0; f < faces.Length; ++f) {
						ref readonly var face = ref faces[f];
						var neighbourY = y + face.NormalY;
						if (neighbourY < 0) continue;
						if (neighbourY < WorldSettings.ChunkHeight) {
							var neighbour = borderedBlocks[TerrainGenerator.GetBlockIndex(x + 1 + face.NormalX, neighbourY, z + 1 + face.NormalZ)];
							if (neighbour != BlockType.Air) continue;
						}

						var faceCentre = new Vector3(x + 0.5f + face.NormalX * 0.5f, y + 0.5f + face.NormalY * 0.5f, z + 0.5f + face.NormalZ * 0.5f);
						if (!TerrainMeshWriter.TryAddQuad(output, faceCentre, in face, 1f, 1f, BlockTextures.GetTile(block, face.Face))) return;
					}
				}
			}
		}
	}
}
