// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Loop;

public readonly record struct LoopTimeInterval(float ElapsedSeconds) {
	public TimeSpan TimeSpan {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TimeSpan.FromSeconds(ElapsedSeconds);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator float(LoopTimeInterval operand) => operand.ElapsedSeconds;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator LoopTimeInterval(float operand) => new(operand);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TimeSpan(LoopTimeInterval operand) => operand.TimeSpan;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator LoopTimeInterval(TimeSpan operand) => new((float) operand.TotalSeconds);
}