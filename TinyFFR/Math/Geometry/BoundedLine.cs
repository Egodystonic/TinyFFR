// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(float) * 4 * 2)]
public readonly partial struct BoundedLine : ILine<BoundedLine> {
	readonly Location _startPoint;
	readonly Vect _vect;

	public Location StartPoint {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _startPoint;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _startPoint = value;
	}
	public Direction Direction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _vect.Direction;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _vect = Vect.FromDirectionAndDistance(value, Length);
	}
	public float Length {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _vect.Length;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _vect = _vect.WithLength(value);
	}
	public float LengthSquared {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _vect.LengthSquared;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _vect = _vect.WithLength(MathF.Sqrt(value));
	}
	public Vect StartToEndVect {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _vect;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _vect = value;
	}
	public Location EndPoint {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _startPoint + _vect;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _vect = value - _startPoint;
	}
	bool ILine.IsUnboundedInBothDirections => false;
	float? ILine.Length => Length;
	float? ILine.LengthSquared => LengthSquared;
	Vect? ILine.StartToEndVect => StartToEndVect;
	Location? ILine.EndPoint => EndPoint;

	public BoundedLine(Location startPoint, Location endPoint) {
		_startPoint = startPoint;
		_vect = endPoint - startPoint;
	}
	public BoundedLine(Location startPoint, Vect startToEndVect) {
		_startPoint = startPoint;
		_vect = startToEndVect;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<float> ConvertToSpan(in BoundedLine src) => MemoryMarshal.Cast<BoundedLine, float>(new ReadOnlySpan<BoundedLine>(in src));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine ConvertFromSpan(ReadOnlySpan<float> src) => new(Location.ConvertFromSpan(src), Vect.ConvertFromSpan(src[3..]));

	public override string ToString() => ToString(null, null);
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(BoundedLine), (nameof(StartPoint), _startPoint), (nameof(EndPoint), EndPoint));
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(BoundedLine), (nameof(StartPoint), _startPoint), (nameof(EndPoint), EndPoint));

	public static BoundedLine Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out BoundedLine result) => TryParse(s.AsSpan(), provider, out result);

	public static BoundedLine Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out Location startPoint, out Location endPoint);
		return new(startPoint, endPoint);
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out BoundedLine result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out Location startPoint, out Location endPoint)) return false;
		result = new(startPoint, endPoint);
		return true;
	}

	#region Equality
	public bool Equals(BoundedLine other) => _startPoint.Equals(other._startPoint) && _vect.Equals(other._vect);
	public bool Equals(BoundedLine other, float tolerance) => _startPoint.Equals(other.StartPoint, tolerance) && _vect.Equals(other.StartToEndVect, tolerance);
	public override bool Equals(object? obj) => obj is BoundedLine other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_startPoint, _vect);
	public static bool operator ==(BoundedLine left, BoundedLine right) => left.Equals(right);
	public static bool operator !=(BoundedLine left, BoundedLine right) => !left.Equals(right);
	#endregion
}