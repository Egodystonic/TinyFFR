namespace VoxelWorld;

enum BlockType : byte {
	Air,
	Grass,
	Dirt,
	Stone,
	DeepStone,
	Sand,
	SnowyDirt,
	SnowyStone
}

enum BlockFace : byte {
	Top,
	Bottom,
	Side
}

static class BlockTextures {
	const int AtlasCellsPerRow = 4;
	const float AtlasSizePixels = 1024f;
	const float CellSizePixels = 256f;
	const float TilePaddingPixels = 64f;
	const float TileSizePixels = 128f;
	const float TileInsetPixels = 0.5f;

	const int GrassTopTile = 0;
	const int GrassSideTile = 1;
	const int DirtTile = 2;
	const int StoneTile = 3;
	const int SandTile = 4;
	const int SnowTile = 5;
	const int SnowyDirtSideTile = 6;
	const int SnowyStoneSideTile = 7;
	const int DeepStoneTile = 8;

	public static int GetTile(BlockType block, BlockFace face) {
		return block switch {
			BlockType.Grass => face switch { BlockFace.Top => GrassTopTile, BlockFace.Side => GrassSideTile, _ => DirtTile },
			BlockType.Dirt => DirtTile,
			BlockType.Stone => StoneTile,
			BlockType.DeepStone => DeepStoneTile,
			BlockType.Sand => SandTile,
			BlockType.SnowyDirt => face switch { BlockFace.Top => SnowTile, BlockFace.Side => SnowyDirtSideTile, _ => DirtTile },
			BlockType.SnowyStone => face switch { BlockFace.Top => SnowTile, BlockFace.Side => SnowyStoneSideTile, _ => StoneTile },
			_ => StoneTile
		};
	}

	public static void GetTileUvs(int tile, out float uMin, out float vMin, out float uMax, out float vMax) {
		var left = (tile % AtlasCellsPerRow) * CellSizePixels + TilePaddingPixels + TileInsetPixels;
		var top = (tile / AtlasCellsPerRow) * CellSizePixels + TilePaddingPixels + TileInsetPixels;
		var size = TileSizePixels - TileInsetPixels * 2f;
		uMin = left / AtlasSizePixels;
		uMax = (left + size) / AtlasSizePixels;
		vMax = 1f - top / AtlasSizePixels;
		vMin = 1f - (top + size) / AtlasSizePixels;
	}
}
