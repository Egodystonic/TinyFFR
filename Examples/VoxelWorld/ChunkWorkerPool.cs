namespace VoxelWorld;

readonly record struct TerrainJobResult(Chunk? Chunk, TerrainTile? Tile, int Generation, TerrainMeshData MeshData);

sealed class ChunkWorkerPool : IDisposable {
	readonly record struct TerrainJob(Chunk? Chunk, TerrainTile? Tile, int Generation);

	readonly object _lock = new();
	readonly TerrainGenerator _generator;
	readonly Thread[] _workers;
	readonly TerrainJob[] _jobs;
	readonly TerrainJobResult[] _results;
	readonly TerrainMeshData[] _freeMeshData;
	int _jobsHead;
	int _jobsCount;
	int _resultsHead;
	int _resultsCount;
	int _freeMeshDataCount;
	int _jobsInProgress;
	bool _isStopping;

	public ChunkWorkerPool(TerrainGenerator generator, int maxQueuedJobs) {
		_generator = generator;
		_jobs = new TerrainJob[maxQueuedJobs];

		var workerCount = WorldSettings.WorkerThreadCount;
		var meshDataCount = workerCount + WorldSettings.MaxMeshUploadsPerFrame;
		_results = new TerrainJobResult[meshDataCount];
		_freeMeshData = new TerrainMeshData[meshDataCount];
		for (var i = 0; i < meshDataCount; ++i) _freeMeshData[i] = new TerrainMeshData();
		_freeMeshDataCount = meshDataCount;

		_workers = new Thread[workerCount];
		for (var i = 0; i < workerCount; ++i) {
			_workers[i] = new Thread(WorkerLoop) { IsBackground = true, Name = "Terrain Worker " + i };
			_workers[i].Start();
		}
	}

	public int PendingJobCount {
		get {
			lock (_lock) return _jobsCount + _jobsInProgress + _resultsCount;
		}
	}

	public bool TryQueueJob(Chunk chunk) => TryQueueJob(new TerrainJob(chunk, null, chunk.Generation));

	public bool TryQueueJob(TerrainTile tile) => TryQueueJob(new TerrainJob(null, tile, tile.Generation));

	bool TryQueueJob(TerrainJob job) {
		lock (_lock) {
			if (_jobsCount == _jobs.Length) return false;
			_jobs[(_jobsHead + _jobsCount) % _jobs.Length] = job;
			++_jobsCount;
			Monitor.PulseAll(_lock);
			return true;
		}
	}

	public bool TryTakeResult(out TerrainJobResult result) {
		lock (_lock) {
			if (_resultsCount == 0) {
				result = default;
				return false;
			}
			result = _results[_resultsHead];
			_results[_resultsHead] = default;
			_resultsHead = (_resultsHead + 1) % _results.Length;
			--_resultsCount;
			return true;
		}
	}

	public void ReturnMeshData(TerrainMeshData meshData) {
		lock (_lock) {
			_freeMeshData[_freeMeshDataCount++] = meshData;
			Monitor.PulseAll(_lock);
		}
	}

	void WorkerLoop() {
		var cellHeights = new int[FarTerrainMesher.BorderedCellsPerSide * FarTerrainMesher.BorderedCellsPerSide];
		var cellSurfaces = new BlockType[FarTerrainMesher.BorderedCellsPerSide * FarTerrainMesher.BorderedCellsPerSide];

		while (true) {
			TerrainJob job;
			TerrainMeshData meshData;
			lock (_lock) {
				while (!_isStopping && (_jobsCount == 0 || _freeMeshDataCount == 0)) Monitor.Wait(_lock);
				if (_isStopping) return;
				job = _jobs[_jobsHead];
				_jobs[_jobsHead] = default;
				_jobsHead = (_jobsHead + 1) % _jobs.Length;
				--_jobsCount;
				meshData = _freeMeshData[--_freeMeshDataCount];
				_freeMeshData[_freeMeshDataCount] = null!;
				++_jobsInProgress;
			}

			if (job.Chunk is { } chunk) {
				_generator.GenerateChunk(chunk.Coord, chunk.Blocks, chunk.ColumnHeights);
				ChunkMesher.BuildMesh(chunk.Blocks, chunk.ColumnHeights, meshData);
			}
			else if (job.Tile is { } tile) {
				FarTerrainMesher.BuildMesh(tile, _generator, cellHeights, cellSurfaces, meshData);
			}

			lock (_lock) {
				_results[(_resultsHead + _resultsCount) % _results.Length] = new TerrainJobResult(job.Chunk, job.Tile, job.Generation, meshData);
				++_resultsCount;
				--_jobsInProgress;
			}
		}
	}

	public void Dispose() {
		lock (_lock) {
			_isStopping = true;
			Monitor.PulseAll(_lock);
		}
		foreach (var worker in _workers) worker.Join();
	}
}
