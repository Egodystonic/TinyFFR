namespace VoxelWorld;

readonly record struct RegionCoord(int X, int Z) {
	public static RegionCoord FromWorldPosition(float worldX, float worldZ) {
		return new RegionCoord((int) MathF.Floor(worldX / WorldSettings.RegionSize), (int) MathF.Floor(worldZ / WorldSettings.RegionSize));
	}

	public static RegionCoord FromChunk(ChunkCoord chunk) {
		const int ChunksPerRegion = WorldSettings.RegionSize / WorldSettings.ChunkSize;
		return new RegionCoord(TileCoord.FloorDiv(chunk.X, ChunksPerRegion), TileCoord.FloorDiv(chunk.Z, ChunksPerRegion));
	}

	public ChunkCoord FirstChunk => new(X * (WorldSettings.RegionSize / WorldSettings.ChunkSize), Z * (WorldSettings.RegionSize / WorldSettings.ChunkSize));

	public int ChebyshevDistanceTo(RegionCoord other) => Math.Max(Math.Abs(X - other.X), Math.Abs(Z - other.Z));
}
