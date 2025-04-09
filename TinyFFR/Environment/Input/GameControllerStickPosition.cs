// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR.Environment.Input;

public readonly struct GameControllerStickPosition : IEquatable<GameControllerStickPosition> {
	public const float RecommendedDeadzoneSize = ((float) AnalogDisplacementLevel.Slight / Int16.MaxValue) + 1E-5f; // Just slightly over the raw trigger level for 'slight'

	public static readonly GameControllerStickPosition Centered = new(0, 0);
#pragma warning disable CA1859 // "Read-only dictionary is slower than just Dictionary" -- True, but not a big performance hit and the intent implied by making this readonly is more important
	static readonly IReadOnlyDictionary<Orientation2D, GameControllerStickPosition> _orientationMap = new Dictionary<Orientation2D, GameControllerStickPosition> {
		[Orientation2D.None] = Centered,
		[Orientation2D.Right] = new(Int16.MaxValue, 0),
		[Orientation2D.UpRight] = new(Int16.MaxValue, Int16.MaxValue),
		[Orientation2D.Up] = new(0, Int16.MaxValue),
		[Orientation2D.UpLeft] = new(Int16.MinValue, Int16.MaxValue),
		[Orientation2D.Left] = new(Int16.MinValue, 0),
		[Orientation2D.DownLeft] = new(Int16.MinValue, Int16.MinValue),
		[Orientation2D.Down] = new(0, Int16.MinValue),
		[Orientation2D.DownRight] = new(Int16.MaxValue, Int16.MinValue),
	};
#pragma warning restore CA1859

	internal short RawDisplacementHorizontal { get; init; }
	internal short RawDisplacementVertical { get; init; }

	// This approach treats a positive and negative value the same (i.e. -10000 and +10000 give the same value with the sign flipped),
	// but essentially means that Int16.MinValue is the same as Int16.MinValue + 1. This is a deliberate choice/tradeoff.
	// TODO document that positive = right, negative = left
	public float DisplacementHorizontal => Single.Clamp(RawDisplacementHorizontal / (float) Int16.MaxValue, -1f, 1f);
	// TODO document that positive = up, negative = down
	public float DisplacementVertical => Single.Clamp(RawDisplacementVertical / (float) Int16.MaxValue, -1f, 1f);

	// TODO document that this is a 0f to 1f scalar that indicates how far from the centre the stick is being pushed.
	// This calculation is pretty tricky because I've found in practice that pushing a stick to a diagonal doesn't quite give
	// you 32,768 in both directions, rather something a bit smaller. So we can't just take the max of the absolute values.
	// However, it's not (1/sqrt(2) * 32768) in each direction either when pushed to the diagonal, so we're not getting a unit vector.
	// The best I could find is to take the vector length anyway as I'm doing here but force it to never exceed 1f. This probably means we're
	// losing some resolution but as controllers vary anyway it might not be possible to have a one-size-fits-all approach? Not sure.
	static float GetNormalizedDisplacement(float normalizedHorizontal, float normalizedVertical) => Single.Min(MathF.Sqrt(normalizedHorizontal * normalizedHorizontal + normalizedVertical * normalizedVertical), 1f);
	public float Displacement => GetNormalizedDisplacement(DisplacementHorizontal, DisplacementVertical);

	public float DisplacementHorizontalWithDeadzone {
		get {
			var d = DisplacementHorizontal;
			return MathF.Abs(d) > RecommendedDeadzoneSize ? d : 0f;
		}
	}
	public float DisplacementVerticalWithDeadzone {
		get {
			var d = DisplacementVertical;
			return MathF.Abs(d) > RecommendedDeadzoneSize ? d : 0f;
		}
	}
	public float DisplacementWithDeadzone {
		get {
			var h = DisplacementHorizontal;
			var v = DisplacementVertical;
			return MathF.Abs(h) > RecommendedDeadzoneSize || MathF.Abs(v) > RecommendedDeadzoneSize ? GetNormalizedDisplacement(h, v) : 0f;
		}
	}

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

	public GameControllerStickPosition(short rawDisplacementHorizontal, short rawDisplacementVertical) {
		RawDisplacementHorizontal = rawDisplacementHorizontal;
		RawDisplacementVertical = rawDisplacementVertical;
	}

	public static GameControllerStickPosition FromMaxOrientation(Orientation2D orientation) => _orientationMap[orientation];

	public XYPair<float> AsXYPair(float deadzoneSize = RecommendedDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		var x = DisplacementHorizontal;
		var y = DisplacementVertical;
		return new(
			MathF.Abs(x) <= deadzoneSize ? 0f : x,
			MathF.Abs(y) <= deadzoneSize ? 0f : y
		);
	}

	// TODO clarify this is the four-quadrant inverse tangent
	public Angle? GetPolarAngle(float deadzoneSize = RecommendedDeadzoneSize) => Angle.From2DPolarAngle(AsXYPair(deadzoneSize));


	public bool IsHorizontalOffsetOutsideDeadzone(float deadzoneSize = RecommendedDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		return MathF.Abs(DisplacementHorizontal) > deadzoneSize;
	}
	public bool IsVerticalOffsetOutsideDeadzone(float deadzoneSize = RecommendedDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		return MathF.Abs(DisplacementVertical) > deadzoneSize;
	}
	public bool IsOutsideDeadzone(float deadzoneSize = RecommendedDeadzoneSize) {
		if (deadzoneSize is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(deadzoneSize), deadzoneSize, $"Deadzone must be between 0 and 1 (inclusive).");
		return IsHorizontalOffsetOutsideDeadzone(deadzoneSize) || IsVerticalOffsetOutsideDeadzone(deadzoneSize);
	}


	public HorizontalOrientation2D GetHorizontalOrientation(float deadzoneSize = RecommendedDeadzoneSize) => GetOrientation(deadzoneSize).GetHorizontalComponent();
	public VerticalOrientation2D GetVerticalOrientation(float deadzoneSize = RecommendedDeadzoneSize) => GetOrientation(deadzoneSize).GetVerticalComponent();
	public Orientation2D GetOrientation(float deadzoneSize = RecommendedDeadzoneSize) => GetPolarAngle(deadzoneSize)?.PolarOrientation ?? Orientation2D.None;

	// TODO explain these are for performance or custom implementation only if necessary and which sign points which way etc
	public void GetRawDisplacementValues(out short outRawHorizontalOffset, out short outRawVerticalOffset) {
		outRawHorizontalOffset = RawDisplacementHorizontal;
		outRawVerticalOffset = RawDisplacementVertical;
	}

	public override string ToString() {
		var orientation = GetOrientation();
		var orString = orientation == Orientation2D.None ? "No orientation" : $"{DisplacementLevel} {orientation}";
		var angle = GetPolarAngle();
		var angleString = angle == null ? "Within deadzone" : $"{GetPolarAngle():N0} with {PercentageUtils.ConvertFractionToPercentageString(Displacement, "N0", CultureInfo.CurrentCulture)} displacement";
		return $"X:{PercentageUtils.ConvertFractionToPercentageString(DisplacementHorizontal, "N0", CultureInfo.CurrentCulture)} Y:{PercentageUtils.ConvertFractionToPercentageString(DisplacementVertical, "N0", CultureInfo.CurrentCulture)} " +
			   $"({angleString}, {orString})";
	}

	public bool Equals(GameControllerStickPosition other) => RawDisplacementHorizontal == other.RawDisplacementHorizontal && RawDisplacementVertical == other.RawDisplacementVertical;
	public override bool Equals(object? obj) => obj is GameControllerStickPosition other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(RawDisplacementHorizontal, RawDisplacementVertical);
	public static bool operator ==(GameControllerStickPosition left, GameControllerStickPosition right) => left.Equals(right);
	public static bool operator !=(GameControllerStickPosition left, GameControllerStickPosition right) => !left.Equals(right);
}