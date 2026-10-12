namespace VoxelWorld;

readonly record struct TileCoord(int X, int Z) {
	public static int FloorDiv(int value, int divisor) => (int) MathF.Floor((float) value / divisor);

	public static TileCoord FromWorldPosition(float worldX, float worldZ, int tileSize) {
		return new TileCoord((int) MathF.Floor(worldX / tileSize), (int) MathF.Floor(worldZ / tileSize));
	}

	public int ChebyshevDistanceTo(TileCoord other) => Math.Max(Math.Abs(X - other.X), Math.Abs(Z - other.Z));
}

sealed class TerrainTile {
	public TileCoord Coord;
	public MeshState State;
	public int Generation;
	public int LevelIndex;
	public int TileSize;
	public int CellSize;
	public GpuMeshSlot MeshSlot = null!;

	public int WorldMinX => Coord.X * TileSize;
	public int WorldMinZ => Coord.Z * TileSize;
}

interface ITerrainLevel {
	bool IsLiveAt(int worldX, int worldZ);
	bool IsAreaFullyLive(int worldMinX, int worldMinZ, int size);
}
