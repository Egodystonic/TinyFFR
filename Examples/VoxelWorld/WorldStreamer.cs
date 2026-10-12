using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace VoxelWorld;

sealed class WorldStreamer : IDisposable {
	readonly ChunkManager _nearChunks;
	readonly TerrainLevel _midTerrain;
	readonly TerrainLevel _farTerrain;
	readonly TerrainJobScheduler _workerPool;

	public int VisibleChunkCount => _nearChunks.VisibleChunkCount;
	public int VisibleMidTileCount => _midTerrain.VisibleTileCount;
	public int VisibleFarTileCount => _farTerrain.VisibleTileCount;
	public int PendingJobCount { get; private set; }
	public int ResizedMeshCount => _nearChunks.ResizedMeshCount + _midTerrain.ResizedMeshCount + _farTerrain.ResizedMeshCount;
	public int TruncatedMeshCount { get; private set; }

	public WorldStreamer(ILocalTinyFfrFactory factory, Scene scene, Material material, TerrainGenerator generator) {
		_nearChunks = new ChunkManager(factory, scene, material);
		_midTerrain = new TerrainLevel(1, WorldSettings.RegionSize, WorldSettings.SuperRegionSize, WorldSettings.SuperRegionSize, WorldSettings.MidSuperRegionRadius, factory, scene, material);
		_farTerrain = new TerrainLevel(2, WorldSettings.SuperRegionSize, WorldSettings.SuperRegionSize, WorldSettings.SuperRegionSize, WorldSettings.FarSuperRegionRadius, factory, scene, material);

		_nearChunks.Coarser = _midTerrain;
		_midTerrain.Finer = _nearChunks;
		_midTerrain.Coarser = _farTerrain;
		_farTerrain.Finer = _midTerrain;

		_workerPool = new TerrainJobScheduler(factory.ResourceAllocator, generator, WorldSettings.MaxJobsInFlight);
	}

	public bool IsChunkLiveAt(float worldX, float worldZ) => _nearChunks.IsChunkLiveAt(worldX, worldZ);

	public bool IsSolid(int worldX, int worldY, int worldZ) => _nearChunks.IsSolid(worldX, worldY, worldZ);

	public void Update(Location playerPosition) {
		_nearChunks.SetCentre(playerPosition);
		_midTerrain.SetCentre(playerPosition);
		_farTerrain.SetCentre(playerPosition);

		var uploads = 0;
		while (uploads < WorldSettings.MaxMeshUploadsPerFrame && _workerPool.TryTakeResult(out var result)) {
			bool uploaded;
			if (result.Chunk != null) uploaded = _nearChunks.AcceptResult(in result);
			else if (result.Tile!.LevelIndex == 1) uploaded = _midTerrain.AcceptResult(in result);
			else uploaded = _farTerrain.AcceptResult(in result);
			if (uploaded) ++uploads;
			if (result.MeshData.Truncated) ++TruncatedMeshCount;
			_workerPool.ReturnJob(result.Job);
		}

		_farTerrain.UpdateVisibility();
		_midTerrain.UpdateVisibility();
		_nearChunks.UpdateVisibility();

		_nearChunks.QueueMissingChunks(_workerPool);
		_midTerrain.QueueMissingTiles(_workerPool);
		_farTerrain.QueueMissingTiles(_workerPool);
		PendingJobCount = _workerPool.PendingJobCount;
	}

	public void Dispose() {
		_workerPool.Dispose();
		_nearChunks.Dispose();
		_midTerrain.Dispose();
		_farTerrain.Dispose();
	}
}
