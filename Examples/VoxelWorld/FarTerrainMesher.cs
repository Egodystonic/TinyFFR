using System.Numerics;

namespace VoxelWorld;

static class FarTerrainMesher {
	public const int CellsPerTileSide = WorldSettings.CellsPerTerrainTileSide;
	public const int BorderedCellsPerSide = CellsPerTileSide + 2;

	public static void BuildMesh(TerrainTile tile, TerrainGenerator generator, Span<int> borderedCellHeights, Span<BlockType> borderedCellSurfaces, TerrainMeshData output) {
		output.Clear();
		var cellSize = tile.CellSize;
		var tileOriginX = tile.WorldMinX;
		var tileOriginZ = tile.WorldMinZ;

		for (var bx = 0; bx < BorderedCellsPerSide; ++bx) {
			for (var bz = 0; bz < BorderedCellsPerSide; ++bz) {
				var cellCentreX = tileOriginX + (bx - 1) * cellSize + cellSize * 0.5f;
				var cellCentreZ = tileOriginZ + (bz - 1) * cellSize + cellSize * 0.5f;
				borderedCellHeights[bx * BorderedCellsPerSide + bz] = generator.GetSurface(cellCentreX, cellCentreZ, out var surface);
				borderedCellSurfaces[bx * BorderedCellsPerSide + bz] = surface;
			}
		}

		var faces = FaceDefinition.All;
		for (var cx = 0; cx < CellsPerTileSide; ++cx) {
			for (var cz = 0; cz < CellsPerTileSide; ++cz) {
				var index = (cx + 1) * BorderedCellsPerSide + (cz + 1);
				var height = borderedCellHeights[index];
				var surface = borderedCellSurfaces[index];
				var top = height + 1f;
				var centreX = cx * cellSize + cellSize * 0.5f;
				var centreZ = cz * cellSize + cellSize * 0.5f;

				ref readonly var topFace = ref faces[0];
				if (!TerrainMeshWriter.TryAddQuad(output, new Vector3(centreX, top, centreZ), in topFace, cellSize, cellSize, BlockTextures.GetTile(surface, BlockFace.Top))) return;

				for (var f = 2; f < faces.Length; ++f) {
					ref readonly var face = ref faces[f];
					var neighbourHeight = borderedCellHeights[(cx + 1 + face.NormalX) * BorderedCellsPerSide + (cz + 1 + face.NormalZ)];
					if (neighbourHeight >= height) continue;

					var drop = height - neighbourHeight;
					var sideCentre = new Vector3(centreX + face.NormalX * cellSize * 0.5f, top - drop * 0.5f, centreZ + face.NormalZ * cellSize * 0.5f);
					if (!TerrainMeshWriter.TryAddQuad(output, sideCentre, in face, cellSize, drop, BlockTextures.GetTile(surface, BlockFace.Side))) return;
				}
			}
		}
	}
}
