// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024
using static Egodystonic.TinyFFR.Bits;

namespace Egodystonic.TinyFFR;

file static class Bits {
	public const int RightBit = 0b1;
	public const int UpBit = 0b10;
	public const int LeftBit = 0b100;
	public const int DownBit = 0b1000;

	public const int HorizontalBits = RightBit | LeftBit;
	public const int VerticalBits = UpBit | DownBit;
}

/// <summary>
/// Identifies one of the two 2D axes (X or Y), independent of sign/direction.
/// </summary>
#pragma warning disable CA1027 //"Mark flags enums with Flags attribute" ... This isn't a bitfield enum
public enum Axis2D {
	/// <summary>
	/// No axis.
	/// </summary>
	None = Axis.None,
	/// <summary>
	/// The X axis (<see cref="HorizontalOrientation2D.Left"/>/<see cref="HorizontalOrientation2D.Right"/>).
	/// </summary>
	X = Axis.X,
	/// <summary>
	/// The Y axis (<see cref="VerticalOrientation2D.Up"/>/<see cref="VerticalOrientation2D.Down"/>).
	/// </summary>
	Y = Axis.Y
}
#pragma warning restore CA1027


/// <summary>
/// Identifies a horizontal 2D direction: <see cref="Right"/>, <see cref="Left"/>, or <see cref="None"/>.
/// </summary>
/// <remarks>
/// Any value of this enum can be safely cast to <see cref="Orientation2D"/>, but it is not safe to cast back to this type from <see cref="Orientation2D" />.
/// </remarks>
[Flags]
public enum HorizontalOrientation2D {
	/// <summary>No horizontal orientation.</summary>
	None = 0,
	/// <summary>Points right.</summary>
	Right = RightBit,
	/// <summary>Points left.</summary>
	Left = LeftBit,
}

/// <summary>
/// Identifies a vertical 2D direction: <see cref="Up"/>, <see cref="Down"/>, or <see cref="None"/>.
/// </summary>
/// <remarks>
/// Any value of this enum can be safely cast to <see cref="Orientation2D"/>, but it is not safe to cast back to this type from <see cref="Orientation2D" />.
/// </remarks>
[Flags]
public enum VerticalOrientation2D {
	/// <summary>No vertical orientation.</summary>
	None = 0,
	/// <summary>Points up.</summary>
	Up = UpBit,
	/// <summary>Points down.</summary>
	Down = DownBit,
}

/// <summary>
/// Identifies one of the four 2D diagonal directions, or <see cref="None"/>.
/// </summary>
/// <remarks>
/// Any value of this enum can be safely cast to <see cref="Orientation2D"/>, but it is not safe to cast back to this type from <see cref="Orientation2D" />.
/// </remarks>
public enum DiagonalOrientation2D {
	/// <summary>No orientation.</summary>
	None = 0,
	/// <summary>Combines <see cref="VerticalOrientation2D.Up"/> and <see cref="HorizontalOrientation2D.Right"/>.</summary>
	UpRight = UpBit | RightBit,
	/// <summary>Combines <see cref="VerticalOrientation2D.Up"/> and <see cref="HorizontalOrientation2D.Left"/>.</summary>
	UpLeft = UpBit | LeftBit,
	/// <summary>Combines <see cref="VerticalOrientation2D.Down"/> and <see cref="HorizontalOrientation2D.Left"/>.</summary>
	DownLeft = DownBit | LeftBit,
	/// <summary>Combines <see cref="VerticalOrientation2D.Down"/> and <see cref="HorizontalOrientation2D.Right"/>.</summary>
	DownRight = DownBit | RightBit
}

/// <summary>
/// Identifies any of the eight horizontal, vertical, or diagonal 2D directions, or <see cref="None"/>.
/// </summary>
[Flags]
public enum Orientation2D {
	/// <summary>No orientation.</summary>
	None = 0,
	/// <summary>Points right.</summary>
	Right = RightBit,
	/// <summary>Combines <see cref="Up"/> and <see cref="Right"/>.</summary>
	UpRight = UpBit | RightBit,
	/// <summary>Points up.</summary>
	Up = UpBit,
	/// <summary>Combines <see cref="Up"/> and <see cref="Left"/>.</summary>
	UpLeft = UpBit | LeftBit,
	/// <summary>Points left.</summary>
	Left = LeftBit,
	/// <summary>Combines <see cref="Down"/> and <see cref="Left"/>.</summary>
	DownLeft = DownBit | LeftBit,
	/// <summary>Points down.</summary>
	Down = DownBit,
	/// <summary>Combines <see cref="Down"/> and <see cref="Right"/>.</summary>
	DownRight = DownBit | RightBit
}

/// <summary>
/// A static class housing extension methods for the 2D orientation enums (<see cref="Orientation2D"/>, <see cref="HorizontalOrientation2D"/>, <see cref="VerticalOrientation2D"/>, <see cref="DiagonalOrientation2D"/>).
/// </summary>
public static class Orientation2DExtensions {
	/// <summary>
	/// Converts this orientation to the general-purpose <see cref="Orientation2D"/> type; equivalent to a direct cast.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation2D AsGeneralOrientation(this HorizontalOrientation2D @this) => (Orientation2D) @this;

	/// <summary>
	/// Converts this orientation to the general-purpose <see cref="Orientation2D"/> type; equivalent to a direct cast.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation2D AsGeneralOrientation(this VerticalOrientation2D @this) => (Orientation2D) @this;

	/// <summary>
	/// Converts this orientation to the general-purpose <see cref="Orientation2D"/> type; equivalent to a direct cast.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation2D AsGeneralOrientation(this DiagonalOrientation2D @this) => (Orientation2D) @this;

	/// <summary>
	/// Combines this orientation with <paramref name="verticalComponent"/> in to a single <see cref="Orientation2D"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="verticalComponent">The vertical orientation to combine with.</param>
	public static Orientation2D Plus(this HorizontalOrientation2D @this, VerticalOrientation2D verticalComponent) => (Orientation2D) ((int) @this | (int) verticalComponent);

	/// <summary>
	/// Combines this orientation with <paramref name="horizontalComponent"/> in to a single <see cref="Orientation2D"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="horizontalComponent">The horizontal orientation to combine with.</param>
	public static Orientation2D Plus(this VerticalOrientation2D @this, HorizontalOrientation2D horizontalComponent) => (Orientation2D) ((int) @this | (int) horizontalComponent);

	/// <summary>
	/// Returns this orientation's horizontal component.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	public static HorizontalOrientation2D GetHorizontalComponent(this Orientation2D @this) => (HorizontalOrientation2D) ((int) @this & HorizontalBits);
	/// <summary>
	/// Returns this orientation's vertical component.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	public static VerticalOrientation2D GetVerticalComponent(this Orientation2D @this) => (VerticalOrientation2D) ((int) @this & VerticalBits);
	/// <summary>
	/// Returns this orientation's horizontal component.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	public static HorizontalOrientation2D GetHorizontalComponent(this DiagonalOrientation2D @this) => (HorizontalOrientation2D) ((int) @this & HorizontalBits);
	/// <summary>
	/// Returns this orientation's vertical component.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	public static VerticalOrientation2D GetVerticalComponent(this DiagonalOrientation2D @this) => (VerticalOrientation2D) ((int) @this & VerticalBits);

	/// <summary>
	/// Converts this orientation to the angle around a circle it represents.
	/// </summary>
	/// <remarks>
	/// This follows the same convention as <see cref="Angle.From2DPolarAngle(Orientation2D)"/>: the angle "starts" at 0° for <see cref="Orientation2D.Right"/> and increases anticlockwise.
	/// </remarks>
	/// <param name="this">The extended orientation.</param>
	/// <returns><see langword="null"/> if this orientation is <see cref="Orientation2D.None"/>; the corresponding angle otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle? ToPolarAngle(this Orientation2D @this) => Angle.From2DPolarAngle(@this);

	/// <summary>
	/// Converts this orientation to the angle around a circle it represents.
	/// </summary>
	/// <remarks>
	/// This follows the same convention as <see cref="Angle.From2DPolarAngle(Orientation2D)"/>: the angle "starts" at 0° for a rightward orientation and increases anticlockwise.
	/// </remarks>
	/// <param name="this">The extended orientation.</param>
	/// <returns><see langword="null"/> if this orientation is <see cref="DiagonalOrientation2D.None"/>; the corresponding angle otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle? ToPolarAngle(this DiagonalOrientation2D @this) => Angle.From2DPolarAngle(@this.AsGeneralOrientation());
}