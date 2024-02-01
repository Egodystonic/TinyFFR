// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(short) + sizeof(short))]
public readonly struct GameControllerStickPosition : IEquatable<GameControllerStickPosition> {
	public const float RecommendedDeadzoneSize = 0.1f;
	readonly short _horizontalOffsetRaw;
	readonly short _verticalOffsetRawInverted;

	public short HorizontalOffsetRaw { // TODO explain these are for performance only if necessary
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _horizontalOffsetRaw;
	}
	public short VerticalOffsetRaw {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		// We invert this in order to make the PolarAngle property work correctly
		get => (short) -_verticalOffsetRawInverted;
	}

	// TODO document that positive = right, negative = left
	public float HorizontalOffset => _horizontalOffsetRaw / (float) (Int16.MaxValue + ((_horizontalOffsetRaw & 0x8000) >> 15));
	// TODO document that positive = up, negative = down
	// We invert this in order to make the PolarAngle property work correctly
	public float VerticalOffset => _verticalOffsetRawInverted / (float) -(Int16.MaxValue + ((_verticalOffsetRawInverted & 0x8000) >> 15)); 

	public XYPair AsXYPair(float deadzoneSize = RecommendedDeadzoneSize) {
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

	public bool Equals(GameControllerStickPosition other) => _horizontalOffsetRaw == other._horizontalOffsetRaw && _verticalOffsetRawInverted == other._verticalOffsetRawInverted;
	public override bool Equals(object? obj) => obj is GameControllerStickPosition other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_horizontalOffsetRaw, _verticalOffsetRawInverted);
	public static bool operator ==(GameControllerStickPosition left, GameControllerStickPosition right) => left.Equals(right);
	public static bool operator !=(GameControllerStickPosition left, GameControllerStickPosition right) => !left.Equals(right);
}