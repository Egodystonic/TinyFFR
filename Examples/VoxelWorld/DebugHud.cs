using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace VoxelWorld;

sealed class DebugHud : IDisposable {
	const float RefreshIntervalSeconds = 0.25f;
	const float AllocationBaselineDelaySeconds = 10f;

	readonly Font _font;
	readonly FontPen _pen;
	readonly CanvasText _text;
	float _timeSinceRefresh;
	int _framesSinceRefresh;
	float _totalTime;
	long? _allocationBaseline;
	int _gen0CollectionBaseline;

	public DebugHud(ILocalTinyFfrFactory factory, CanvasScene canvas) {
		_font = factory.AssetLoader.LoadFont();
		_pen = _font.CreatePen(BuiltInFontPenStyle.WhiteWithOutline);
		_text = canvas.Add("", _pen);
		_text.SetPlacementPixels(Orientation2D.UpLeft, (16, 16), 26);
	}

	public void Update(float deltaTime, Player player, WorldStreamer world, TerrainGenerator generator) {
		_totalTime += deltaTime;
		_timeSinceRefresh += deltaTime;
		++_framesSinceRefresh;
		if (_allocationBaseline == null && _totalTime >= AllocationBaselineDelaySeconds) {
			_allocationBaseline = GC.GetTotalAllocatedBytes(precise: false);
			_gen0CollectionBaseline = GC.CollectionCount(0);
		}
		if (_timeSinceRefresh < RefreshIntervalSeconds) return;

		var framesPerSecond = _framesSinceRefresh / _timeSinceRefresh;
		_timeSinceRefresh = 0f;
		_framesSinceRefresh = 0;

		var position = player.Position;
		var biomeName = generator.GetBiome(position.X, position.Z) switch {
			Biome.Desert => "Desert",
			Biome.Snowfields => "Snowfields",
			_ => "Grassland"
		};

		Span<char> textBuffer = stackalloc char[320];
		var charsWritten = 0;
		var builtText = _allocationBaseline is { } baseline
			? textBuffer.TryWrite($"FPS: {framesPerSecond:N0}\nPosition: {position.X:N1}, {position.Y:N1}, {position.Z:N1}\nBiome: {biomeName}\nTerrain: {world.VisibleChunkCount} chunks, {world.VisibleMidTileCount} mid tiles, {world.VisibleFarTileCount} far tiles, {world.PendingJobCount} pending\nAllocated since warm-up: {(GC.GetTotalAllocatedBytes(precise: false) - baseline) / 1024:N0} KB, Gen0 GCs: {GC.CollectionCount(0) - _gen0CollectionBaseline}", out charsWritten)
			: textBuffer.TryWrite($"FPS: {framesPerSecond:N0}\nPosition: {position.X:N1}, {position.Y:N1}, {position.Z:N1}\nBiome: {biomeName}\nTerrain: {world.VisibleChunkCount} chunks, {world.VisibleMidTileCount} mid tiles, {world.VisibleFarTileCount} far tiles, {world.PendingJobCount} pending\nWarming up...", out charsWritten);
		if (builtText) _text.SetText(textBuffer[..charsWritten], TextJustification.Left);
	}

	public void Dispose() {
		_text.Dispose();
		_pen.Dispose();
		_font.Dispose();
	}
}
