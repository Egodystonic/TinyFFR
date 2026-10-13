using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Threading;
using Egodystonic.TinyFFR.Resources;

namespace VoxelWorld;

readonly record struct TerrainJobResult(Chunk? Chunk, TerrainTile? Tile, int Generation, TerrainMeshData MeshData, TerrainJob Job);

sealed class TerrainJob {
	public readonly TerrainMeshData MeshData = new();
	public readonly int[] CellHeights = new int[FarTerrainMesher.BorderedCellsPerSide * FarTerrainMesher.BorderedCellsPerSide];
	public readonly BlockType[] CellSurfaces = new BlockType[FarTerrainMesher.BorderedCellsPerSide * FarTerrainMesher.BorderedCellsPerSide];
	public readonly TerrainGenerator Generator;
	public Chunk? Chunk;
	public TerrainTile? Tile;
	public int Generation;

	public TerrainJob(TerrainGenerator generator) => Generator = generator;
}

sealed unsafe class TerrainJobScheduler : IDisposable {
	readonly IResourceAllocator _allocator;
	readonly TerrainJob[] _freeJobs;
	readonly TinyFfrAsyncOperation<TerrainJob?>[] _inFlight;
	int _freeJobCount;
	int _inFlightCount;

	public int PendingJobCount => _inFlightCount;

	public TerrainJobScheduler(IResourceAllocator allocator, TerrainGenerator generator, int maxJobsInFlight) {
		_allocator = allocator;
		_freeJobs = new TerrainJob[maxJobsInFlight];
		_inFlight = new TinyFfrAsyncOperation<TerrainJob?>[maxJobsInFlight];
		for (var i = 0; i < maxJobsInFlight; ++i) _freeJobs[i] = new TerrainJob(generator);
		_freeJobCount = maxJobsInFlight;
	}

	public bool TryQueueJob(Chunk chunk) => TryDispatch(chunk, null, chunk.Generation);

	public bool TryQueueJob(TerrainTile tile) => TryDispatch(null, tile, tile.Generation);

	bool TryDispatch(Chunk? chunk, TerrainTile? tile, int generation) {
		if (_freeJobCount == 0) return false;
		var job = _freeJobs[--_freeJobCount];
		job.Chunk = chunk;
		job.Tile = tile;
		job.Generation = generation;
		_inFlight[_inFlightCount++] = _allocator.DispatchWorkerThreadJob<TerrainJob, TerrainJob>(job, &Run);
		return true;
	}

	static TerrainJob? Run(TerrainJob? job) {
		if (job!.Chunk is { } chunk) {
			job.Generator.GenerateChunk(chunk.Coord, chunk.Blocks, chunk.ColumnHeights);
			ChunkMesher.BuildMesh(chunk.Blocks, chunk.ColumnHeights, job.MeshData);
		}
		else if (job.Tile is { } tile) {
			FarTerrainMesher.BuildMesh(tile, job.Generator, job.CellHeights, job.CellSurfaces, job.MeshData);
		}
		return job;
	}

	public bool TryTakeResult(out TerrainJobResult result) {
		for (var i = 0; i < _inFlightCount; ++i) {
			if (!_inFlight[i].IsResultAvailable) continue;
			var job = _inFlight[i].GetResultAndDisposeOperation()!;
			_inFlight[i] = _inFlight[--_inFlightCount];
			_inFlight[_inFlightCount] = default;
			result = new TerrainJobResult(job.Chunk, job.Tile, job.Generation, job.MeshData, job);
			return true;
		}
		result = default;
		return false;
	}

	public void ReturnJob(TerrainJob job) {
		job.Chunk = null;
		job.Tile = null;
		_freeJobs[_freeJobCount++] = job;
	}

	public void Dispose() {
		for (var i = 0; i < _inFlightCount; ++i) ReturnJob(_inFlight[i].GetResultAndDisposeOperation()!);
		_inFlightCount = 0;
	}
}
