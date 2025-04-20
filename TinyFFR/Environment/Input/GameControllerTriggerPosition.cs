// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR.Environment.Input;

public readonly struct GameControllerTriggerPosition : IEquatable<GameControllerTriggerPosition> {
	public const float RecommendedDeadzoneSize = ((float) AnalogDisplacementLevel.Slight / Int16.MaxValue) + 1E-5f; // Just slightly over the raw trigger level for 'slight'
	public static readonly GameControllerTriggerPosition Zero = new(0);
	public static readonly GameControllerTriggerPosition Max = new(Int16.MaxValue);

	internal short DisplacementRaw { get; init; }

	// Make sure displacement can never be negative
	public float Displacement => Int16.Max(DisplacementRaw, 0) / (float) Int16.MaxValue;

	public AnalogDisplacementLevel DisplacementLevel => AnalogDisplacementLevelExtensions.FromRawDisplacementMagnitude(Int16.Max(DisplacementRaw, 0));

	public float GetDisplacementWithDeadzone(float deadzoneSize = RecommendedDeadzoneSize) {
		var displacement = Displacement;
		var displacementLessDeadzone = displacement - deadzoneSize;
		if (Math.Sign(displacement) != Math.Sign(displacementLessDeadzone)) return 0f;

		return displacementLessDeadzone / (1f - deadzoneSize);
	}

	public GameControllerTriggerPosition(short displacementRaw) => DisplacementRaw = displacementRaw;

#pragma warning disable CA1024 // "Use properties" -  Why is this a method and we don't just make DisplacementRaw public? Because
	//	a) It makes it slightly less discoverable than "Displacement" (which is good, we want people to use that property instead) and
	//	b) It's also the choice we made for raw values in the stick position struct
	// TODO explain this is for performance or custom implementations only if necessary
	public short GetRawDisplacementValue() => DisplacementRaw;
#pragma warning restore CA1024

	public override string ToString() {
		return $"{(DisplacementLevel == AnalogDisplacementLevel.None ? "No" : DisplacementLevel.ToString())} displacement ({PercentageUtils.ConvertFractionToPercentageString(Displacement, "N0", CultureInfo.CurrentCulture)})";
	}

	public bool Equals(GameControllerTriggerPosition other) => DisplacementRaw == other.DisplacementRaw;
	public override bool Equals(object? obj) => obj is GameControllerTriggerPosition other && Equals(other);
	public override int GetHashCode() => DisplacementRaw.GetHashCode();
	public static bool operator ==(GameControllerTriggerPosition left, GameControllerTriggerPosition right) => left.Equals(right);
	public static bool operator !=(GameControllerTriggerPosition left, GameControllerTriggerPosition right) => !left.Equals(right);
}