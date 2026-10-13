using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace VoxelWorld;

sealed class TerrainLevel : ITerrainLevel, IDisposable {
	readonly int _tileSize;
	readonly int _parentTileSize;
	readonly int _coverageSize;
	readonly int _coverageRadius;
	readonly int _tilesPerCoverageUnit;
	readonly int _slotGridSize;
	readonly TerrainTile[] _slots;
	readonly TileCoord[] _loadOrderOffsets;
	TileCoord _centreCoverageUnit;

	public ITerrainLevel? Finer { get; set; }
	public ITerrainLevel? Coarser { get; set; }
	public int SlotCount => _slots.Length;
	public int LiveTileCount { get; private set; }
	public int VisibleTileCount { get; private set; }
	public int ResizedMeshCount { get; private set; }

	public TerrainLevel(int levelIndex, int tileSize, int parentTileSize, int coverageSize, int coverageRadius, ILocalTinyFfrFactory factory, Scene scene, Material material) {
		_tileSize = tileSize;
		_parentTileSize = parentTileSize;
		_coverageSize = coverageSize;
		_coverageRadius = coverageRadius;
		_tilesPerCoverageUnit = coverageSize / tileSize;
		_slotGridSize = (coverageRadius * 2 + 3) * _tilesPerCoverageUnit;
		_slots = new TerrainTile[_slotGridSize * _slotGridSize];
		for (var i = 0; i < _slots.Length; ++i) {
			_slots[i] = new TerrainTile {
				Coord = new TileCoord(Int32.MinValue, Int32.MinValue),
				LevelIndex = levelIndex,
				TileSize = tileSize,
				CellSize = tileSize / WorldSettings.CellsPerTerrainTileSide,
				MeshSlot = new GpuMeshSlot(factory, scene, material, WorldSettings.InitialFaceCapacityPerTerrainTile)
			};
		}
		_loadOrderOffsets = CreateLoadOrderOffsets();
	}

	public void SetCentre(Location playerPosition) {
		_centreCoverageUnit = TileCoord.FromWorldPosition(playerPosition.X, playerPosition.Z, _coverageSize);
	}

	public bool IsLiveAt(int worldX, int worldZ) {
		var coord = TileCoord.FromWorldPosition(worldX, worldZ, _tileSize);
		var tile = GetSlot(coord);
		return tile.Coord == coord && tile.State == MeshState.Live;
	}

	public bool IsAreaFullyLive(int worldMinX, int worldMinZ, int size) {
		for (var x = worldMinX; x < worldMinX + size; x += _tileSize) {
			for (var z = worldMinZ; z < worldMinZ + size; z += _tileSize) {
				var coord = TileCoord.FromWorldPosition(x, z, _tileSize);
				if (CoverageDistance(coord) > _coverageRadius || !IsLiveAt(x, z)) return false;
			}
		}
		return true;
	}

	public bool AcceptResult(in TerrainJobResult result) {
		var tile = result.Tile!;
		if (result.Generation != tile.Generation || CoverageDistance(tile.Coord) > _coverageRadius + 1) {
			tile.State = MeshState.Free;
			return false;
		}

		var position = new Location(tile.WorldMinX, 0f, tile.WorldMinZ);
		if (tile.MeshSlot.Upload(result.MeshData, position, _tileSize)) ++ResizedMeshCount;
		tile.State = MeshState.Live;
		++LiveTileCount;
		return true;
	}

	public void UpdateVisibility() {
		var visibleCount = 0;
		foreach (var tile in _slots) {
			if (tile.State != MeshState.Live) continue;
			var distance = CoverageDistance(tile.Coord);
			var coarserCoversTile = Coarser == null || Coarser.IsLiveAt(tile.WorldMinX + _tileSize / 2, tile.WorldMinZ + _tileSize / 2);
			if (distance > _coverageRadius + 1 || (distance > _coverageRadius && coarserCoversTile)) {
				tile.MeshSlot.Hide();
				tile.State = MeshState.Free;
				--LiveTileCount;
				continue;
			}

			var finerCoversTile = distance <= _coverageRadius && Finer != null && Finer.IsAreaFullyLive(tile.WorldMinX, tile.WorldMinZ, _tileSize);
			var canReplaceCoarser = !coarserCoversTile || AreSiblingsLive(tile);
			if (finerCoversTile || !canReplaceCoarser) tile.MeshSlot.Hide();
			else tile.MeshSlot.Show();
			if (tile.MeshSlot.IsVisible) ++visibleCount;
		}
		VisibleTileCount = visibleCount;
	}

	public void QueueMissingTiles(TerrainJobScheduler workerPool) {
		var firstTileX = _centreCoverageUnit.X * _tilesPerCoverageUnit;
		var firstTileZ = _centreCoverageUnit.Z * _tilesPerCoverageUnit;
		foreach (var offset in _loadOrderOffsets) {
			var coord = new TileCoord(firstTileX + offset.X, firstTileZ + offset.Z);
			var tile = GetSlot(coord);
			if (tile.State == MeshState.Pending) continue;
			if (tile.Coord == coord && tile.State == MeshState.Live) continue;

			if (tile.State == MeshState.Live) {
				tile.MeshSlot.Hide();
				--LiveTileCount;
			}
			tile.Coord = coord;
			tile.State = MeshState.Pending;
			++tile.Generation;
			if (workerPool.TryQueueJob(tile)) continue;

			tile.State = MeshState.Free;
			return;
		}
	}

	bool AreSiblingsLive(TerrainTile tile) {
		var parentMinX = TileCoord.FloorDiv(tile.WorldMinX, _parentTileSize) * _parentTileSize;
		var parentMinZ = TileCoord.FloorDiv(tile.WorldMinZ, _parentTileSize) * _parentTileSize;
		for (var x = parentMinX; x < parentMinX + _parentTileSize; x += _tileSize) {
			for (var z = parentMinZ; z < parentMinZ + _parentTileSize; z += _tileSize) {
				if (!IsLiveAt(x, z)) return false;
			}
		}
		return true;
	}

	int CoverageDistance(TileCoord tile) {
		var coverageUnit = new TileCoord(TileCoord.FloorDiv(tile.X, _tilesPerCoverageUnit), TileCoord.FloorDiv(tile.Z, _tilesPerCoverageUnit));
		return coverageUnit.ChebyshevDistanceTo(_centreCoverageUnit);
	}

	TerrainTile GetSlot(TileCoord coord) {
		var slotX = ((coord.X % _slotGridSize) + _slotGridSize) % _slotGridSize;
		var slotZ = ((coord.Z % _slotGridSize) + _slotGridSize) % _slotGridSize;
		return _slots[slotX * _slotGridSize + slotZ];
	}

	TileCoord[] CreateLoadOrderOffsets() {
		var centreOffset = _tilesPerCoverageUnit * 0.5f - 0.5f;
		var radius = _coverageRadius * _tilesPerCoverageUnit;
		var offsets = new List<TileCoord>();
		for (var x = -radius; x < radius + _tilesPerCoverageUnit; ++x) {
			for (var z = -radius; z < radius + _tilesPerCoverageUnit; ++z) offsets.Add(new TileCoord(x, z));
		}
		offsets.Sort((a, b) => DistanceSquared(a).CompareTo(DistanceSquared(b)));
		return offsets.ToArray();

		float DistanceSquared(TileCoord offset) => (offset.X - centreOffset) * (offset.X - centreOffset) + (offset.Z - centreOffset) * (offset.Z - centreOffset);
	}

	public void Dispose() {
		foreach (var tile in _slots) tile.MeshSlot.Dispose();
	}
}
