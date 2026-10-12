namespace VoxelWorld;

enum Biome : byte {
	Grassland,
	Desert,
	Snowfields
}

sealed class TerrainGenerator {
	public const int BorderedChunkSize = WorldSettings.ChunkSize + 2;
	const int BaseHeight = 44;
	const int SnowLine = 150;
	const int ExposedStoneHeight = 100;
	const int SoilDepth = 3;
	const int DeepStoneDepth = 14;
	const float DesertTemperature = 0.3f;
	const float SnowfieldsTemperature = -0.3f;

	readonly Noise _continent;
	readonly Noise _hills;
	readonly Noise _mountainMask;
	readonly Noise _ridges;
	readonly Noise _temperature;
	readonly Noise _rangeMask;
	readonly Noise _rangeRidges;

	public TerrainGenerator(int seed) {
		_continent = new Noise(seed);
		_hills = new Noise(seed + 1);
		_mountainMask = new Noise(seed + 2);
		_ridges = new Noise(seed + 3);
		_temperature = new Noise(seed + 4);
		_rangeMask = new Noise(seed + 5);
		_rangeRidges = new Noise(seed + 6);
	}

	public static int GetBlockIndex(int borderedX, int y, int borderedZ) => (borderedX * BorderedChunkSize + borderedZ) * WorldSettings.ChunkHeight + y;

	public Biome GetBiome(float worldX, float worldZ) {
		var temperature = GetTemperature(worldX, worldZ);
		if (temperature > DesertTemperature) return Biome.Desert;
		if (temperature < SnowfieldsTemperature) return Biome.Snowfields;
		return Biome.Grassland;
	}

	public int GetSurfaceHeight(float worldX, float worldZ) => ComputeSurfaceHeight(worldX, worldZ, GetTemperature(worldX, worldZ), out _);

	public int GetSurface(float worldX, float worldZ, out BlockType surfaceBlock) {
		var temperature = GetTemperature(worldX, worldZ);
		var surfaceHeight = ComputeSurfaceHeight(worldX, worldZ, temperature, out var mountainHeight);
		surfaceBlock = ChooseSurfaceBlock(surfaceHeight, mountainHeight, temperature);
		return surfaceHeight;
	}

	public void GenerateChunk(ChunkCoord coord, Span<BlockType> borderedBlocks, Span<byte> borderedColumnHeights) {
		for (var bx = 0; bx < BorderedChunkSize; ++bx) {
			for (var bz = 0; bz < BorderedChunkSize; ++bz) {
				var worldX = coord.X * WorldSettings.ChunkSize + bx - 1;
				var worldZ = coord.Z * WorldSettings.ChunkSize + bz - 1;
				var temperature = GetTemperature(worldX, worldZ);
				var surfaceHeight = ComputeSurfaceHeight(worldX, worldZ, temperature, out var mountainHeight);
				var surfaceBlock = ChooseSurfaceBlock(surfaceHeight, mountainHeight, temperature);
				var soilBlock = surfaceBlock switch {
					BlockType.Grass or BlockType.SnowyDirt => BlockType.Dirt,
					BlockType.Sand => BlockType.Sand,
					_ => BlockType.Stone
				};

				borderedColumnHeights[bx * BorderedChunkSize + bz] = (byte) surfaceHeight;
				var column = borderedBlocks.Slice(GetBlockIndex(bx, 0, bz), WorldSettings.ChunkHeight);
				for (var y = 0; y < column.Length; ++y) {
					if (y > surfaceHeight) column[y] = BlockType.Air;
					else if (y == surfaceHeight) column[y] = surfaceBlock;
					else if (y >= surfaceHeight - SoilDepth) column[y] = soilBlock;
					else if (y >= surfaceHeight - DeepStoneDepth) column[y] = BlockType.Stone;
					else column[y] = BlockType.DeepStone;
				}
			}
		}
	}

	float GetTemperature(float worldX, float worldZ) => _temperature.SampleFractal(worldX * 0.0012f, worldZ * 0.0012f, 3);

	int ComputeSurfaceHeight(float worldX, float worldZ, float temperature, out float mountainHeight) {
		var desertFactor = SmoothStep(0.1f, DesertTemperature + 0.15f, temperature);
		var coldFactor = SmoothStep(-0.1f, -(DesertTemperature + 0.15f), temperature);

		var continent = _continent.SampleFractal(worldX * 0.0025f, worldZ * 0.0025f, 3) * 14f;
		var hills = _hills.SampleFractal(worldX * 0.012f, worldZ * 0.012f, 4) * 9f * (1f - 0.6f * desertFactor);

		var mountainMask = SmoothStep(0.05f, 0.55f, _mountainMask.SampleFractal(worldX * 0.0035f, worldZ * 0.0035f, 2) + coldFactor * 0.3f);
		var ridge = 1f - MathF.Abs(_ridges.SampleFractal(worldX * 0.007f, worldZ * 0.007f, 4));
		mountainHeight = ridge * ridge * 62f * mountainMask * (1f - 0.75f * desertFactor);

		var rangeMask = SmoothStep(0.05f, 0.45f, _rangeMask.SampleFractal(worldX * 0.0007f, worldZ * 0.0007f, 2));
		var rangeRidge = 1f - MathF.Abs(_rangeRidges.SampleFractal(worldX * 0.0028f, worldZ * 0.0028f, 5));
		mountainHeight += rangeRidge * rangeRidge * rangeRidge * 130f * rangeMask * (1f - 0.5f * desertFactor);

		var height = BaseHeight + continent + hills + mountainHeight;
		return Math.Clamp((int) height, 1, WorldSettings.ChunkHeight - 2);
	}

	static BlockType ChooseSurfaceBlock(int surfaceHeight, float mountainHeight, float temperature) {
		if (surfaceHeight >= SnowLine) return BlockType.SnowyStone;
		if (temperature > DesertTemperature) return BlockType.Sand;
		if (temperature < SnowfieldsTemperature) return surfaceHeight >= ExposedStoneHeight ? BlockType.SnowyStone : BlockType.SnowyDirt;
		if (surfaceHeight >= ExposedStoneHeight && mountainHeight > 18f) return BlockType.Stone;
		return BlockType.Grass;
	}

	static float SmoothStep(float edge0, float edge1, float value) {
		var t = Math.Clamp((value - edge0) / (edge1 - edge0), 0f, 1f);
		return t * t * (3f - 2f * t);
	}
}
