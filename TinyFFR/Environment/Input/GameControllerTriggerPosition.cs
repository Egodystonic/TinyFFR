// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

public enum GameControllerTriggerDisplacementLevel {
	None = 0,
	Slight = 5_500,
	Moderate = 11_000,
	Full = 22_000
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(short))]
public readonly struct GameControllerTriggerPosition : IEquatable<GameControllerTriggerPosition> {
	readonly short _displacementRaw;

	public short DisplacementRaw { // TODO explain this is for performance only if necessary
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _displacementRaw;
	}
	// Make sure displacement can never be negative,
	public float Displacement => Int16.Max(_displacementRaw, 0) / (float) Int16.MaxValue;

	public GameControllerTriggerDisplacementLevel DisplacementLevel {
		get {
			return _displacementRaw switch {
				>= (int) GameControllerTriggerDisplacementLevel.Full => GameControllerTriggerDisplacementLevel.Full,
				>= (int) GameControllerTriggerDisplacementLevel.Moderate => GameControllerTriggerDisplacementLevel.Moderate,
				>= (int) GameControllerTriggerDisplacementLevel.Slight => GameControllerTriggerDisplacementLevel.Slight,
				_ => GameControllerTriggerDisplacementLevel.None
			};
		}
	}

	public bool Equals(GameControllerTriggerPosition other) => _displacementRaw == other._displacementRaw;
	public override bool Equals(object? obj) => obj is GameControllerTriggerPosition other && Equals(other);
	public override int GetHashCode() => _displacementRaw.GetHashCode();
	public static bool operator ==(GameControllerTriggerPosition left, GameControllerTriggerPosition right) => left.Equals(right);
	public static bool operator !=(GameControllerTriggerPosition left, GameControllerTriggerPosition right) => !left.Equals(right);
}