// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

public readonly struct GameControllerStickPosition : IEquatable<GameControllerStickPosition> {
	public const float RecommendedDeadzoneSize = ((float) AnalogDisplacementLevel.Slight / Int16.MaxValue) + 0.01f;

	public static readonly GameControllerStickPosition Centered = new(0, 0);
	public static readonly IReadOnlyDictionary<CardinalDirection, GameControllerStickPosition> AllCardinals = new Dictionary<CardinalDirection, GameControllerStickPosition> {
		[CardinalDirection.None] = Centered,
		[CardinalDirection.Right] = new(Int16.MaxValue, 0),
		[CardinalDirection.UpRight] = new(Int16.MaxValue, Int16.MaxValue),
		[CardinalDirection.Up] = new(0, Int16.MaxValue),
		[CardinalDirection.UpLeft] = new(Int16.MinValue, Int16.MaxValue),
		[CardinalDirection.Left] = new(Int16.MinValue, 0),
		[CardinalDirection.DownLeft] = new(Int16.MinValue, Int16.MinValue),
		[CardinalDirection.Down] = new(0, Int16.MinValue),
		[CardinalDirection.DownRight] = new(Int16.MaxValue, Int16.MinValue),
	};

	internal short HorizontalOffsetRaw { get; init; }
	internal short VerticalOffsetRaw { get; init; }

	// TODO document that positive = right, negative = left
	public float HorizontalOffset => HorizontalOffsetRaw / (float) (Int16.MaxValue + ((HorizontalOffsetRaw & 0x8000) >> 15));
	// TODO document that positive = up, negative = down
	public float VerticalOffset => VerticalOffsetRaw / (float) (Int16.MaxValue + ((VerticalOffsetRaw & 0x8000) >> 15));

	// TODO document that this is a 0f to 1f scalar that indicates how far from the centre the stick is being pushed.
	// This calculation is pretty tricky because I've found in practice that pushing a stick to a diagonal doesn't quite give
	// you 32,768 in both directions, rather something a bit smaller. So we can't just take the max of the absolute values.
	// However, it's not (1/sqrt(2) * 32768) in each direction either when pushed to the diagonal, so we're not getting a unit vector.
	// The best I could find is to take the vector length anyway as I'm doing here but force it to never exceed 1f. This probably means we're
	// losing some resolution but as controllers vary anyway it might not be possible to have a one-size-fits-all approach? Not sure.
	public float Displacement => Single.Min(MathF.Sqrt(HorizontalOffset * HorizontalOffset + VerticalOffset * VerticalOffset), 1f);

	public AnalogDisplacementLevel DisplacementLevel {
		get {
			return (int) (Int16.MaxValue * Displacement) switch {
				>= (int) AnalogDisplacementLevel.Full => AnalogDisplacementLevel.Full,
				>= (int) AnalogDisplacementLevel.Moderate => AnalogDisplacementLevel.Moderate,
				>= (int) AnalogDisplacementLevel.Slight => AnalogDisplacementLevel.Slight,
				_ => AnalogDisplacementLevel.None
			};
		}
	}

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
	public Angle? GetPolarAngle(float deadzoneSize = RecommendedDeadzoneSize) => AsXYPair(deadzoneSize).PolarAngle;


	public bool IsHorizontalOffsetOutsideDeadzone(float deadzoneSize = RecommendedDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		return MathF.Abs(HorizontalOffset) > deadzoneSize;
	}
	public bool IsVerticalOffsetOutsideDeadzone(float deadzoneSize = RecommendedDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		return MathF.Abs(VerticalOffset) > deadzoneSize;
	}
	public bool IsOutsideDeadzone(float deadzoneSize = RecommendedDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		return IsHorizontalOffsetOutsideDeadzone(deadzoneSize) || IsVerticalOffsetOutsideDeadzone(deadzoneSize);
	}


	public HorizontalDirection GetHorizontalDirection(float deadzoneSize = RecommendedDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		if (HorizontalOffset > deadzoneSize) return HorizontalDirection.Right;
		else if (HorizontalOffset < -deadzoneSize) return HorizontalDirection.Left;
		else return HorizontalDirection.None;
	}
	public VerticalDirection GetVerticalDirection(float deadzoneSize = RecommendedDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		if (VerticalOffset > deadzoneSize) return VerticalDirection.Up;
		else if (VerticalOffset < -deadzoneSize) return VerticalDirection.Down;
		else return VerticalDirection.None;
	}
	public CardinalDirection GetDirection(float deadzoneSize = RecommendedDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		return (CardinalDirection) ((int) GetHorizontalDirection(deadzoneSize) | (int) GetVerticalDirection(deadzoneSize));
	}

	// TODO explain these are for performance or custom implementation only if necessary and which sign points which way etc
	public void GetRawOffsetValues(out short outRawHorizontalOffset, out short outRawVerticalOffset) {
		outRawHorizontalOffset = HorizontalOffsetRaw;
		outRawVerticalOffset = VerticalOffsetRaw;
	}

	public override string ToString() {
		var dir = GetDirection();
		var dirString = dir == CardinalDirection.None ? "No direction" : $"{DisplacementLevel} {dir}";
		var angle = GetPolarAngle();
		var angleString = angle == null ? "Within deadzone" : $"{GetPolarAngle():N0} with {PercentageUtils.ConvertFractionToPercentageString(Displacement, "N0")} displacement";
		return $"X:{PercentageUtils.ConvertFractionToPercentageString(HorizontalOffset, "N0")} Y:{PercentageUtils.ConvertFractionToPercentageString(VerticalOffset, "N0")} " +
			   $"({angleString}, {dirString})";
	}

	public bool Equals(GameControllerStickPosition other) => HorizontalOffsetRaw == other.HorizontalOffsetRaw && VerticalOffsetRaw == other.VerticalOffsetRaw;
	public override bool Equals(object? obj) => obj is GameControllerStickPosition other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(HorizontalOffsetRaw, VerticalOffsetRaw);
	public static bool operator ==(GameControllerStickPosition left, GameControllerStickPosition right) => left.Equals(right);
	public static bool operator !=(GameControllerStickPosition left, GameControllerStickPosition right) => !left.Equals(right);
}