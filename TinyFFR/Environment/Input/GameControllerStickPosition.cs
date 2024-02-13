// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

public readonly struct GameControllerStickPosition : IEquatable<GameControllerStickPosition> {
	public const float RecommendedDeadzoneSize = 0.15f;
	public const float RecommendedDirectionalDeadzoneSize = 0.3f;

	public short HorizontalOffsetRaw { get; init; }// TODO explain these are for performance only if necessary
	public short VerticalOffsetRaw { get; init; }

	// TODO document that positive = right, negative = left
	public float HorizontalOffset => HorizontalOffsetRaw / (float) (Int16.MaxValue + ((HorizontalOffsetRaw & 0x8000) >> 15));
	// TODO document that positive = up, negative = down
	public float VerticalOffset => VerticalOffsetRaw / (float) (Int16.MaxValue + ((VerticalOffsetRaw & 0x8000) >> 15));

	public GameControllerStickPosition(short horizontalOffsetRaw, short verticalOffsetRaw) {
		HorizontalOffsetRaw = horizontalOffsetRaw;
		VerticalOffsetRaw = verticalOffsetRaw;
	}

	public XYPair<float> AsXYPair(float deadzoneSize = RecommendedDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		var x = HorizontalOffset;
		var y = VerticalOffset;
		return new(
			MathF.Abs(x) <= deadzoneSize ? 0f : x,
			MathF.Abs(y) <= deadzoneSize ? 0f : y
		);
	}

	// TODO clarify this is the four-quadrant inverse tangent
	public Angle GetPolarAngle(float deadzoneSize = RecommendedDeadzoneSize) => AsXYPair(deadzoneSize).PolarAngle;


	public bool IsHorizontalOffsetWithinDeadzone(float deadzoneSize = RecommendedDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		return MathF.Abs(HorizontalOffset) <= deadzoneSize;
	}
	public bool IsVerticalOffsetWithinDeadzone(float deadzoneSize = RecommendedDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		return MathF.Abs(VerticalOffset) <= deadzoneSize;
	}
	public bool IsWithinDeadzone(float deadzoneSize = RecommendedDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		return IsHorizontalOffsetWithinDeadzone(deadzoneSize) && IsVerticalOffsetWithinDeadzone(deadzoneSize);
	}


	public HorizontalDirection GetHorizontalDirection(float deadzoneSize = RecommendedDirectionalDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		if (HorizontalOffset > deadzoneSize) return HorizontalDirection.Right;
		else if (HorizontalOffset < -deadzoneSize) return HorizontalDirection.Left;
		else return HorizontalDirection.None;
	}
	public VerticalDirection GetVerticalDirection(float deadzoneSize = RecommendedDirectionalDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		if (VerticalOffset > deadzoneSize) return VerticalDirection.Up;
		else if (VerticalOffset < -deadzoneSize) return VerticalDirection.Down;
		else return VerticalDirection.None;
	}
	public CardinalDirection GetDirection(float deadzoneSize = RecommendedDirectionalDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		return (CardinalDirection) ((int) GetHorizontalDirection(deadzoneSize) | (int) GetVerticalDirection(deadzoneSize));
	}

	public override string ToString() {
		return $"X:{PercentageUtils.ConvertFractionToPercentageString(HorizontalOffset, "N0")} Y:{PercentageUtils.ConvertFractionToPercentageString(VerticalOffset, "N0")} " +
			   $"({GetPolarAngle():N0}, {GetDirection()})";
	}

	public bool Equals(GameControllerStickPosition other) => HorizontalOffsetRaw == other.HorizontalOffsetRaw && VerticalOffsetRaw == other.VerticalOffsetRaw;
	public override bool Equals(object? obj) => obj is GameControllerStickPosition other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(HorizontalOffsetRaw, VerticalOffsetRaw);
	public static bool operator ==(GameControllerStickPosition left, GameControllerStickPosition right) => left.Equals(right);
	public static bool operator !=(GameControllerStickPosition left, GameControllerStickPosition right) => !left.Equals(right);
}