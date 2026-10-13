namespace VoxelWorld;

readonly record struct ChunkCoord(int X, int Z) {
	public static ChunkCoord FromWorldPosition(float worldX, float worldZ) {
		return new ChunkCoord((int) MathF.Floor(worldX / WorldSettings.ChunkSize), (int) MathF.Floor(worldZ / WorldSettings.ChunkSize));
	}

	public RegionCoord Region => RegionCoord.FromChunk(this);
}

enum MeshState : byte {
	Free,
	Pending,
	Live
}

sealed class Chunk {
	public readonly BlockType[] Blocks = new BlockType[TerrainGenerator.BorderedChunkSize * TerrainGenerator.BorderedChunkSize * WorldSettings.ChunkHeight];
	public readonly byte[] ColumnHeights = new byte[TerrainGenerator.BorderedChunkSize * TerrainGenerator.BorderedChunkSize];
	public ChunkCoord Coord;
	public MeshState State;
	public int Generation;
	public GpuMeshSlot MeshSlot = null!;

	public BlockType GetLocalBlock(int localX, int y, int localZ) => Blocks[TerrainGenerator.GetBlockIndex(localX + 1, y, localZ + 1)];
}
