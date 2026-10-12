using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace VoxelWorld;

sealed class ChunkManager : ITerrainLevel, IDisposable {
	const int SlotGridSize = (WorldSettings.NearRegionRadius * 2 + 3) * WorldSettings.RegionSizeChunks;

	readonly Chunk[] _slots = new Chunk[SlotGridSize * SlotGridSize];
	readonly ChunkCoord[] _loadOrderOffsets;
	RegionCoord _centre;

	public ITerrainLevel? Coarser { get; set; }
	public int SlotCount => _slots.Length;
	public int LiveChunkCount { get; private set; }
	public int VisibleChunkCount { get; private set; }
	public int ResizedMeshCount { get; private set; }

	public ChunkManager(ILocalTinyFfrFactory factory, Scene scene, Material material) {
		for (var i = 0; i < _slots.Length; ++i) {
			_slots[i] = new Chunk {
				Coord = new ChunkCoord(Int32.MinValue, Int32.MinValue),
				MeshSlot = new GpuMeshSlot(factory, scene, material, WorldSettings.InitialFaceCapacityPerChunk)
			};
		}
		_loadOrderOffsets = CreateLoadOrderOffsets();
	}

	public bool IsChunkLiveAt(float worldX, float worldZ) {
		var coord = ChunkCoord.FromWorldPosition(worldX, worldZ);
		var chunk = GetSlot(coord);
		return chunk.Coord == coord && chunk.State == MeshState.Live;
	}

	public bool IsSolid(int worldX, int worldY, int worldZ) {
		if (worldY < 0) return true;
		if (worldY >= WorldSettings.ChunkHeight) return false;
		var coord = ChunkCoord.FromWorldPosition(worldX, worldZ);
		var chunk = GetSlot(coord);
		if (chunk.Coord != coord || chunk.State != MeshState.Live) return true;
		var localX = worldX - coord.X * WorldSettings.ChunkSize;
		var localZ = worldZ - coord.Z * WorldSettings.ChunkSize;
		return chunk.GetLocalBlock(localX, worldY, localZ) != BlockType.Air;
	}

	public void SetCentre(Location playerPosition) {
		_centre = RegionCoord.FromWorldPosition(playerPosition.X, playerPosition.Z);
	}

	public bool IsLiveAt(int worldX, int worldZ) => IsChunkLiveAt(worldX, worldZ);

	public bool IsAreaFullyLive(int worldMinX, int worldMinZ, int size) {
		for (var x = worldMinX; x < worldMinX + size; x += WorldSettings.RegionSize) {
			for (var z = worldMinZ; z < worldMinZ + size; z += WorldSettings.RegionSize) {
				var region = RegionCoord.FromWorldPosition(x, z);
				if (region.ChebyshevDistanceTo(_centre) > WorldSettings.NearRegionRadius || !IsRegionFullyLive(region)) return false;
			}
		}
		return true;
	}

	bool IsRegionFullyLive(RegionCoord region) {
		var firstChunk = region.FirstChunk;
		for (var x = 0; x < WorldSettings.RegionSizeChunks; ++x) {
			for (var z = 0; z < WorldSettings.RegionSizeChunks; ++z) {
				var coord = new ChunkCoord(firstChunk.X + x, firstChunk.Z + z);
				var chunk = GetSlot(coord);
				if (chunk.Coord != coord || chunk.State != MeshState.Live) return false;
			}
		}
		return true;
	}

	public bool AcceptResult(in TerrainJobResult result) {
		var chunk = result.Chunk!;
		if (result.Generation != chunk.Generation || chunk.Coord.Region.ChebyshevDistanceTo(_centre) > WorldSettings.NearRegionRadius + 1) {
			chunk.State = MeshState.Free;
			return false;
		}

		var position = new Location(chunk.Coord.X * WorldSettings.ChunkSize, 0f, chunk.Coord.Z * WorldSettings.ChunkSize);
		if (chunk.MeshSlot.Upload(result.MeshData, position, WorldSettings.ChunkSize)) ++ResizedMeshCount;
		chunk.State = MeshState.Live;
		++LiveChunkCount;
		return true;
	}

	public void UpdateVisibility() {
		var visibleCount = 0;
		foreach (var chunk in _slots) {
			if (chunk.State != MeshState.Live) continue;
			var region = chunk.Coord.Region;
			var distance = region.ChebyshevDistanceTo(_centre);
			if (distance > WorldSettings.NearRegionRadius) {
				var coarserCoversChunk = Coarser == null || Coarser.IsLiveAt(chunk.Coord.X * WorldSettings.ChunkSize + WorldSettings.ChunkSize / 2, chunk.Coord.Z * WorldSettings.ChunkSize + WorldSettings.ChunkSize / 2);
				if (distance > WorldSettings.NearRegionRadius + 1 || coarserCoversChunk) {
					chunk.MeshSlot.Hide();
					chunk.State = MeshState.Free;
					--LiveChunkCount;
					continue;
				}
			}
			else {
				var coarserCoversChunk = Coarser != null && Coarser.IsLiveAt(chunk.Coord.X * WorldSettings.ChunkSize + WorldSettings.ChunkSize / 2, chunk.Coord.Z * WorldSettings.ChunkSize + WorldSettings.ChunkSize / 2);
				if (!coarserCoversChunk || IsRegionFullyLive(region)) chunk.MeshSlot.Show();
				else chunk.MeshSlot.Hide();
			}
			if (chunk.MeshSlot.IsVisible) ++visibleCount;
		}
		VisibleChunkCount = visibleCount;
	}

	public void QueueMissingChunks(ChunkWorkerPool workerPool) {
		var firstChunk = _centre.FirstChunk;
		foreach (var offset in _loadOrderOffsets) {
			var coord = new ChunkCoord(firstChunk.X + offset.X, firstChunk.Z + offset.Z);
			var chunk = GetSlot(coord);
			if (chunk.State == MeshState.Pending) continue;
			if (chunk.Coord == coord && chunk.State == MeshState.Live) continue;

			if (chunk.State == MeshState.Live) {
				chunk.MeshSlot.Hide();
				--LiveChunkCount;
			}
			chunk.Coord = coord;
			chunk.State = MeshState.Pending;
			++chunk.Generation;
			if (workerPool.TryQueueJob(chunk)) continue;

			chunk.State = MeshState.Free;
			return;
		}
	}

	Chunk GetSlot(ChunkCoord coord) {
		var slotX = ((coord.X % SlotGridSize) + SlotGridSize) % SlotGridSize;
		var slotZ = ((coord.Z % SlotGridSize) + SlotGridSize) % SlotGridSize;
		return _slots[slotX * SlotGridSize + slotZ];
	}

	static ChunkCoord[] CreateLoadOrderOffsets() {
		const float CentreOffset = WorldSettings.RegionSizeChunks * 0.5f - 0.5f;
		var radius = WorldSettings.NearRegionRadius * WorldSettings.RegionSizeChunks;
		var offsets = new List<ChunkCoord>();
		for (var x = -radius; x < radius + WorldSettings.RegionSizeChunks; ++x) {
			for (var z = -radius; z < radius + WorldSettings.RegionSizeChunks; ++z) offsets.Add(new ChunkCoord(x, z));
		}
		offsets.Sort(static (a, b) => DistanceSquared(a).CompareTo(DistanceSquared(b)));
		return offsets.ToArray();

		static float DistanceSquared(ChunkCoord offset) => (offset.X - CentreOffset) * (offset.X - CentreOffset) + (offset.Z - CentreOffset) * (offset.Z - CentreOffset);
	}

	public void Dispose() {
		foreach (var chunk in _slots) chunk.MeshSlot.Dispose();
	}
}
