namespace EquationPlotter;

sealed class PlotClock {
	public float Time { get; set; }
	public float Speed { get; set; } = 1f;
	public bool IsPlaying { get; set; } = true;
	public bool IsLooping { get; set; }
	public float LoopStart { get; set; }
	public float LoopEnd { get; set; } = MathF.Tau;

	public void Advance(float deltaTime) {
		if (!IsPlaying) return;
		Time += deltaTime * Speed;
		if (!IsLooping || LoopEnd <= LoopStart) return;
		var length = LoopEnd - LoopStart;
		if (Time > LoopEnd || Time < LoopStart) Time = LoopStart + (((Time - LoopStart) % length) + length) % length;
	}
}
