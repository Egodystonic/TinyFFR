namespace VoxelWorld;

static class WorldSettings {
	public const int DefaultSeed = 20261011;
	public const int ChunkSize = 16;
	public const int ChunkHeight = 256;
	public const int RegionSizeChunks = 4;
	public const int RegionSize = ChunkSize * RegionSizeChunks;
	public const int SuperRegionSize = RegionSize * 4;
	public const int CellsPerTerrainTileSide = 16;

	public const int NearRegionRadius = 2;
	public const int MidSuperRegionRadius = 2;
	public const int FarSuperRegionRadius = 10;

	public const int MaxMeshUploadsPerFrame = 4;
	public const int InitialFaceCapacityPerChunk = 3584;
	public const int InitialFaceCapacityPerTerrainTile = 1024;
	public const int MaxFacesPerMesh = 8192;
	public const float MaxDeltaTime = 0.1f;

	public static int WorkerThreadCount => Math.Clamp(Environment.ProcessorCount - 1, 1, 8);
	public static float NearDetailDistance => NearRegionRadius * RegionSize;
	public static float RenderDistance => FarSuperRegionRadius * SuperRegionSize;
}
