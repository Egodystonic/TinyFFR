// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR.Environment.Input;

/// <summary>
/// Representation of the current position of one of the analog sticks on a gamepad.
/// </summary>
public readonly struct GameControllerStickPosition : IEquatable<GameControllerStickPosition> {
	/// <summary>
	/// The default "deadzone" size used by the methods on this type that accept one: a displacement at or below this value is treated as no input at all by default.
	/// </summary>
	/// <remarks>
	/// Analog sticks rarely rest at exactly centre — most report a small non-zero displacement even when untouched, and that resting value drifts as a controller
	/// ages. A deadzone is the threshold below which such readings are discarded, preventing a stick the user isn't touching from slowly nudging your camera.
	/// This value sits just above <see cref="AnalogDisplacementLevel.Slight"/>, which suits most controllers; pass a larger value to the relevant methods
	/// if you find a particular controller still drifts or a lower value for more sensitivity.
	/// </remarks>
	public const float RecommendedDeadzoneSize = ((float) AnalogDisplacementLevel.Slight / Int16.MaxValue) + 1E-5f; // Just slightly over the raw trigger level for 'slight'

	/// <summary>
	/// A stick position representing the stick resting at exactly its centre point (i.e. no displacement in any direction).
	/// </summary>
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
	/// <summary>
	/// How far the stick is currently pushed left or right, in the range <c>-1f &lt;= n &lt;= 1f</c>. Positive values indicate the stick is pushed right, negative values indicate left.
	/// </summary>
	/// <remarks>
	/// This value does not have any deadzone applied to it; use <see cref="GetDisplacementHorizontalWithDeadzone"/> if you want a value that ignores the small
	/// residual displacement most sticks report when untouched (see <see cref="RecommendedDeadzoneSize"/>).
	/// </remarks>
	public float DisplacementHorizontal => Single.Clamp(RawDisplacementHorizontal / (float) Int16.MaxValue, -1f, 1f);
	/// <summary>
	/// How far the stick is currently pushed up or down, in the range <c>-1f &lt;= n &lt;= 1f</c>. Positive values indicate the stick is pushed up, negative values indicate down.
	/// </summary>
	/// <remarks>
	/// This value does not have any deadzone applied to it; use <see cref="GetDisplacementVerticalWithDeadzone"/> if you want a value that ignores the small
	/// residual displacement most sticks report when untouched (see <see cref="RecommendedDeadzoneSize"/>).
	/// </remarks>
	public float DisplacementVertical => Single.Clamp(RawDisplacementVertical / (float) Int16.MaxValue, -1f, 1f);

	// This calculation is pretty tricky because I've found in practice that pushing a stick to a diagonal doesn't quite give
	// you 32,768 in both directions, rather something a bit smaller. So we can't just take the max of the absolute values.
	// However, it's not (1/sqrt(2) * 32768) in each direction either when pushed to the diagonal, so we're not getting a unit vector.
	// The best I could find is to take the vector length anyway as I'm doing here but force it to never exceed 1f. This probably means we're
	// losing some resolution but as controllers vary anyway it might not be possible to have a one-size-fits-all approach? Not sure.
	static float GetNormalizedDisplacement(float normalizedHorizontal, float normalizedVertical) => Single.Min(MathF.Sqrt(normalizedHorizontal * normalizedHorizontal + normalizedVertical * normalizedVertical), 1f);
	/// <summary>
	/// How far the stick is currently pushed away from its centre point, regardless of direction, in the range <c>0f &lt;= n &lt;= 1f</c>.
	/// </summary>
	/// <remarks>
	/// <c>1f</c> indicates the stick is pushed as far as it will go. Note that real controllers vary in how far they physically permit the stick to travel diagonally,
	/// so pushing a stick fully in to a corner may report slightly less than <c>1f</c> on some hardware; equally, this value is capped at <c>1f</c> so that
	/// hardware which over-reports on the diagonal doesn't exceed the documented range.
	/// This value does not have any deadzone applied to it; use <see cref="GetDisplacementWithDeadzone"/> if you want a value that ignores the small
	/// residual displacement most sticks report when untouched (see <see cref="RecommendedDeadzoneSize"/>).
	/// </remarks>
	public float Displacement => GetNormalizedDisplacement(DisplacementHorizontal, DisplacementVertical);

	/// <summary>
	/// A coarse stratification of <see cref="Displacement"/>, useful when you only care roughly how hard the stick is being pushed.
	/// </summary>
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
	/// <summary>
	/// A coarse stratification of how far the stick is pushed left or right, ignoring its vertical displacement.
	/// </summary>
	public AnalogDisplacementLevel DisplacementLevelHorizontal => AnalogDisplacementLevelExtensions.FromRawDisplacementMagnitude(MathUtils.SafeAbs(RawDisplacementHorizontal));
	/// <summary>
	/// A coarse stratification of how far the stick is pushed up or down, ignoring its horizontal displacement.
	/// </summary>
	public AnalogDisplacementLevel DisplacementLevelVertical => AnalogDisplacementLevelExtensions.FromRawDisplacementMagnitude(MathUtils.SafeAbs(RawDisplacementVertical));

	/// <summary>
	/// Constructs a new <see cref="GameControllerStickPosition"/> from raw hardware displacement values.
	/// </summary>
	/// <remarks>
	/// You will not usually need to construct one of these yourself; obtain the current position of a real stick from
	/// <see cref="ILatestGameControllerInputRetriever.LeftStickPosition"/>/<see cref="ILatestGameControllerInputRetriever.RightStickPosition"/> instead.
	/// </remarks>
	/// <param name="rawDisplacementHorizontal">The raw horizontal displacement, where <see cref="Int16.MaxValue"/> is fully right and <see cref="Int16.MinValue"/> is fully left.</param>
	/// <param name="rawDisplacementVertical">The raw vertical displacement, where <see cref="Int16.MaxValue"/> is fully up and <see cref="Int16.MinValue"/> is fully down.</param>
	public GameControllerStickPosition(short rawDisplacementHorizontal, short rawDisplacementVertical) {
		RawDisplacementHorizontal = rawDisplacementHorizontal;
		RawDisplacementVertical = rawDisplacementVertical;
	}

	/// <summary>
	/// Returns the stick position representing the stick being pushed as far as it will go in the given <paramref name="orientation"/>.
	/// </summary>
	/// <param name="orientation">The direction to push the stick in, or <see cref="Orientation2D.None"/> for <see cref="Centered"/>.</param>
	public static GameControllerStickPosition FromMaxOrientation(Orientation2D orientation) => _orientationMap[orientation];

	/// <summary>
	/// Returns this stick position as an <see cref="XYPair{T}"/>, with <paramref name="deadzoneSize"/> applied to both axes.
	/// </summary>
	/// <remarks>
	/// <c>X</c> is the horizontal displacement (positive is right) and <c>Y</c> is the vertical displacement (positive is up).
	/// </remarks>
	/// <param name="deadzoneSize">The deadzone to apply; displacement at or below this value on an axis is reported as <c>0f</c> for that axis. Defaults to <see cref="RecommendedDeadzoneSize"/>.</param>
	public XYPair<float> AsXYPair(float deadzoneSize = RecommendedDeadzoneSize) => new(GetDisplacementHorizontalWithDeadzone(deadzoneSize), GetDisplacementVerticalWithDeadzone(deadzoneSize));

	/// <summary>
	/// Returns the direction the stick is currently being pushed in, as an angle around the circle the stick can travel in.
	/// </summary>
	/// <remarks>
	/// The angle starts at 0° for a stick pushed fully right and increases anticlockwise (i.e. 90° is up, 180° is left, 270° is down).
	/// </remarks>
	/// <param name="deadzoneSize">The deadzone to apply. Defaults to <see cref="RecommendedDeadzoneSize"/>.</param>
	/// <returns><see langword="null"/> if the stick's displacement is inside the deadzone (i.e. there is no meaningful direction to report); the angle otherwise.</returns>
	public Angle? GetPolarAngle(float deadzoneSize = RecommendedDeadzoneSize) => Angle.From2DPolarAngle(AsXYPair(deadzoneSize));

	/// <summary>
	/// Returns whether the stick is pushed further left or right than <paramref name="deadzoneSize"/>, ignoring its vertical displacement.
	/// </summary>
	/// <param name="deadzoneSize">The deadzone to test against. Defaults to <see cref="RecommendedDeadzoneSize"/>.</param>
	public bool IsOutsideDeadzoneHorizontal(float deadzoneSize = RecommendedDeadzoneSize) {
		return MathF.Abs(DisplacementHorizontal) > deadzoneSize;
	}
	/// <summary>
	/// Returns whether the stick is pushed further up or down than <paramref name="deadzoneSize"/>, ignoring its horizontal displacement.
	/// </summary>
	/// <param name="deadzoneSize">The deadzone to test against. Defaults to <see cref="RecommendedDeadzoneSize"/>.</param>
	public bool IsOutsideDeadzoneVertical(float deadzoneSize = RecommendedDeadzoneSize) {
		return MathF.Abs(DisplacementVertical) > deadzoneSize;
	}
	/// <summary>
	/// Returns whether the stick is pushed further from its centre point than <paramref name="deadzoneSize"/>, in any direction.
	/// </summary>
	/// <param name="deadzoneSize">The deadzone to test against. Defaults to <see cref="RecommendedDeadzoneSize"/>.</param>
	public bool IsOutsideDeadzone(float deadzoneSize = RecommendedDeadzoneSize) {
		return MathF.Abs(Displacement) > deadzoneSize;
	}

	static float AccountForAndRenormalizeDisplacementWithDeadzone(float displacement, float deadzone) {
		var displacementLessDeadzone = displacement - Single.CopySign(deadzone, displacement);
		if (Math.Sign(displacement) != Math.Sign(displacementLessDeadzone)) return 0f;

		return displacementLessDeadzone / (1f - deadzone);
	}
	/// <summary>
	/// Returns <see cref="DisplacementHorizontal"/> with <paramref name="deadzoneSize"/> applied.
	/// </summary>
	/// <remarks>
	/// The result is re-scaled after the deadzone is subtracted, so pushing the stick fully still reports <c>1f</c> (or <c>-1f</c>) rather than
	/// <c>1f</c> minus the deadzone and displacement just outside the deadzone reports just above <c>0f</c>. Displacement within the deadzone reports <c>0f</c>.
	/// </remarks>
	/// <param name="deadzoneSize">The deadzone to apply. Defaults to <see cref="RecommendedDeadzoneSize"/>.</param>
	public float GetDisplacementHorizontalWithDeadzone(float deadzoneSize = RecommendedDeadzoneSize) {
		return AccountForAndRenormalizeDisplacementWithDeadzone(DisplacementHorizontal, deadzoneSize);
	}
	/// <summary>
	/// Returns <see cref="DisplacementVertical"/> with <paramref name="deadzoneSize"/> applied.
	/// </summary>
	/// <remarks>
	/// The result is re-scaled after the deadzone is subtracted, so pushing the stick fully still reports <c>1f</c> (or <c>-1f</c>) rather than
	/// <c>1f</c> minus the deadzone and displacement just outside the deadzone reports just above <c>0f</c>. Displacement within the deadzone reports <c>0f</c>.
	/// </remarks>
	/// <param name="deadzoneSize">The deadzone to apply. Defaults to <see cref="RecommendedDeadzoneSize"/>.</param>
	public float GetDisplacementVerticalWithDeadzone(float deadzoneSize = RecommendedDeadzoneSize) {
		return AccountForAndRenormalizeDisplacementWithDeadzone(DisplacementVertical, deadzoneSize);
	}
	/// <summary>
	/// Returns <see cref="Displacement"/> with <paramref name="deadzoneSize"/> applied.
	/// </summary>
	/// <remarks>
	/// The result is re-scaled after the deadzone is subtracted, so pushing the stick fully still reports <c>1f</c> rather than <c>1f</c> minus the
	/// deadzone and displacement just outside the deadzone reports just above <c>0f</c>. Displacement within the deadzone reports <c>0f</c>.
	/// </remarks>
	/// <param name="deadzoneSize">The deadzone to apply. Defaults to <see cref="RecommendedDeadzoneSize"/>.</param>
	public float GetDisplacementWithDeadzone(float deadzoneSize = RecommendedDeadzoneSize) {
		return AccountForAndRenormalizeDisplacementWithDeadzone(Displacement, deadzoneSize);
	}

	/// <summary>
	/// Returns whether the stick is being pushed left, right, or neither, reducing its analog position to a simple three-way direction.
	/// </summary>
	/// <param name="deadzoneSize">The deadzone to apply; if the stick's displacement is within it, <see cref="HorizontalOrientation2D.None"/> is returned. Defaults to <see cref="RecommendedDeadzoneSize"/>.</param>
	public HorizontalOrientation2D GetHorizontalOrientation(float deadzoneSize = RecommendedDeadzoneSize) => GetOrientation(deadzoneSize).GetHorizontalComponent();
	/// <summary>
	/// Returns whether the stick is being pushed up, down, or neither, reducing its analog position to a simple three-way direction.
	/// </summary>
	/// <param name="deadzoneSize">The deadzone to apply; if the stick's displacement is within it, <see cref="VerticalOrientation2D.None"/> is returned. Defaults to <see cref="RecommendedDeadzoneSize"/>.</param>
	public VerticalOrientation2D GetVerticalOrientation(float deadzoneSize = RecommendedDeadzoneSize) => GetOrientation(deadzoneSize).GetVerticalComponent();
	/// <summary>
	/// Returns which of the eight compass-style directions the stick is being pushed in, reducing its analog position to a simple digital direction.
	/// </summary>
	/// <remarks>
	/// This is useful for treating a stick like a directional pad (e.g. for menu navigation). Each of the eight directions covers an equal 45° slice of the circle the stick travels in.
	/// </remarks>
	/// <param name="deadzoneSize">The deadzone to apply; if the stick's displacement is within it, <see cref="Orientation2D.None"/> is returned. Defaults to <see cref="RecommendedDeadzoneSize"/>.</param>
	public Orientation2D GetOrientation(float deadzoneSize = RecommendedDeadzoneSize) => GetPolarAngle(deadzoneSize)?.PolarOrientation ?? Orientation2D.None;

	/// <summary>
	/// Outputs the raw displacement values as reported by the hardware, without any deadzone or normalization applied.
	/// </summary>
	/// <remarks>
	/// Most code should prefer the normalized properties on this type (e.g. <see cref="DisplacementHorizontal"/>); these raw values are exposed for callers
	/// who need to avoid the cost of normalization or who want to implement their own handling of the stick's input from scratch.
	/// </remarks>
	/// <param name="outRawHorizontalOffset">Set to the raw horizontal displacement, where <see cref="Int16.MaxValue"/> is fully right and <see cref="Int16.MinValue"/> is fully left.</param>
	/// <param name="outRawVerticalOffset">Set to the raw vertical displacement, where <see cref="Int16.MaxValue"/> is fully up and <see cref="Int16.MinValue"/> is fully down.</param>
	public void GetRawDisplacementValues(out short outRawHorizontalOffset, out short outRawVerticalOffset) {
		outRawHorizontalOffset = RawDisplacementHorizontal;
		outRawVerticalOffset = RawDisplacementVertical;
	}

	/// <inheritdoc/>
	public override string ToString() {
		var orientation = GetOrientation();
		var orString = orientation == Orientation2D.None ? "No orientation" : $"{DisplacementLevel} {orientation}";
		var angle = GetPolarAngle();
		var angleString = angle == null ? "Within deadzone" : $"{GetPolarAngle():N0} with {PercentageUtils.ConvertFractionToPercentageString(Displacement, "N0", CultureInfo.CurrentCulture)} displacement";
		return $"X:{PercentageUtils.ConvertFractionToPercentageString(DisplacementHorizontal, "N0", CultureInfo.CurrentCulture)} Y:{PercentageUtils.ConvertFractionToPercentageString(DisplacementVertical, "N0", CultureInfo.CurrentCulture)} " +
			   $"({angleString}, {orString})";
	}

	/// <inheritdoc/>
	public bool Equals(GameControllerStickPosition other) => RawDisplacementHorizontal == other.RawDisplacementHorizontal && RawDisplacementVertical == other.RawDisplacementVertical;
	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj is GameControllerStickPosition other && Equals(other);
	/// <inheritdoc/>
	public override int GetHashCode() => HashCode.Combine(RawDisplacementHorizontal, RawDisplacementVertical);
	/// <summary>
	/// <see cref="Equals(GameControllerStickPosition)"/>
	/// </summary>
	public static bool operator ==(GameControllerStickPosition left, GameControllerStickPosition right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(GameControllerStickPosition)"/>
	/// </summary>
	public static bool operator !=(GameControllerStickPosition left, GameControllerStickPosition right) => !left.Equals(right);
}