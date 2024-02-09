// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment;

public readonly record struct DeltaTime(float ElapsedSeconds) : 
	IComparable<DeltaTime>, 
	IComparisonOperators<DeltaTime, DeltaTime, bool>, 
	IAdditionOperators<DeltaTime, DeltaTime, DeltaTime>, 
	ISubtractionOperators<DeltaTime, DeltaTime, DeltaTime>,
	IMultiplyOperators<DeltaTime, float, DeltaTime>,
	IDivisionOperators<DeltaTime, float, DeltaTime> {
	public DeltaTime(TimeSpan timeSpan) : this((float) timeSpan.TotalSeconds) { }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TimeSpan ToTimeSpan() => TimeSpan.FromSeconds(ElapsedSeconds);

	public override string ToString() {
		return $"{ToTimeSpan().TotalMilliseconds:N2}ms";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TimeSpan(DeltaTime operand) => operand.ToTimeSpan();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator DeltaTime(TimeSpan operand) => new(operand);

	public int CompareTo(DeltaTime other) => ElapsedSeconds.CompareTo(other.ElapsedSeconds);

	public static bool operator >(DeltaTime left, DeltaTime right) => left.ElapsedSeconds > right.ElapsedSeconds;
	public static bool operator >=(DeltaTime left, DeltaTime right) => left.ElapsedSeconds >= right.ElapsedSeconds;
	public static bool operator <(DeltaTime left, DeltaTime right) => left.ElapsedSeconds < right.ElapsedSeconds;
	public static bool operator <=(DeltaTime left, DeltaTime right) => left.ElapsedSeconds <= right.ElapsedSeconds;
	public static DeltaTime operator +(DeltaTime left, DeltaTime right) => new(left.ElapsedSeconds + right.ElapsedSeconds);
	public static DeltaTime operator -(DeltaTime left, DeltaTime right) => new(left.ElapsedSeconds - right.ElapsedSeconds);
	public static DeltaTime operator *(DeltaTime left, float right) => new(left.ElapsedSeconds * right);
	public static DeltaTime operator *(float left, DeltaTime right) => new(left * right.ElapsedSeconds);
	public static DeltaTime operator /(DeltaTime left, float right) => new(left.ElapsedSeconds / right);
} 