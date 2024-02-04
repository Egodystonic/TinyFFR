// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Loop;

public readonly record struct DeltaTime(float ElapsedSeconds) {
	public DeltaTime(TimeSpan timeSpan) : this((float) timeSpan.TotalSeconds) { }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TimeSpan ToTimeSpan() => TimeSpan.FromSeconds(ElapsedSeconds);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TimeSpan(DeltaTime operand) => operand.ToTimeSpan();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator DeltaTime(TimeSpan operand) => new(operand);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator float(DeltaTime operand) => operand.ElapsedSeconds;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator DeltaTime(float operand) => new(operand);
} 