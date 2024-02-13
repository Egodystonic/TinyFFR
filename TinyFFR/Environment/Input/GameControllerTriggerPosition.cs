// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

public enum GameControllerTriggerDisplacementLevel {
	None = 0,
	Slight = 6_000,
	Moderate = 16_000,
	Full = 30_000
}

public readonly struct GameControllerTriggerPosition : IEquatable<GameControllerTriggerPosition> {
	public short DisplacementRaw { get; init; } // TODO explain this is for performance only if necessary

	// Make sure displacement can never be negative,
	public float Displacement => Int16.Max(DisplacementRaw, 0) / (float) Int16.MaxValue;

	public GameControllerTriggerDisplacementLevel DisplacementLevel {
		get {
			return DisplacementRaw switch {
				>= (int) GameControllerTriggerDisplacementLevel.Full => GameControllerTriggerDisplacementLevel.Full,
				>= (int) GameControllerTriggerDisplacementLevel.Moderate => GameControllerTriggerDisplacementLevel.Moderate,
				>= (int) GameControllerTriggerDisplacementLevel.Slight => GameControllerTriggerDisplacementLevel.Slight,
				_ => GameControllerTriggerDisplacementLevel.None
			};
		}
	}

	public GameControllerTriggerPosition(short displacementRaw) => DisplacementRaw = displacementRaw;

	public override string ToString() {
		return $"{(DisplacementLevel == GameControllerTriggerDisplacementLevel.None ? "No" : DisplacementLevel.ToString())} displacement ({PercentageUtils.ConvertFractionToPercentageString(Displacement, "N0")})";
	}

	public bool Equals(GameControllerTriggerPosition other) => DisplacementRaw == other.DisplacementRaw;
	public override bool Equals(object? obj) => obj is GameControllerTriggerPosition other && Equals(other);
	public override int GetHashCode() => DisplacementRaw.GetHashCode();
	public static bool operator ==(GameControllerTriggerPosition left, GameControllerTriggerPosition right) => left.Equals(right);
	public static bool operator !=(GameControllerTriggerPosition left, GameControllerTriggerPosition right) => !left.Equals(right);
}