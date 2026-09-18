using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// The values each kind of texture pattern falls back to when a corresponding argument is not supplied.
/// </summary>
public sealed class TexturePatternDefaultValues {
	/// <summary>
	/// The default number of times a chequerboard pattern repeats across each axis: <c>(8, 8)</c>.
	/// </summary>
	public static readonly XYPair<int> ChequerboardDefaultRepetitionCount = (8, 8);
	/// <summary>
	/// The default width and height of one chequerboard cell, in texels: <c>64</c>.
	/// </summary>
	public const int ChequerboardDefaultCellResolution = 64;

	/// <summary>
	/// The default radius of the interior of a circle, in texels: <c>256</c>.
	/// </summary>
	public const int CirclesDefaultInteriorRadius = 256;
	/// <summary>
	/// The default thickness of a circle's border, in texels: <c>24</c>.
	/// </summary>
	public const int CirclesDefaultBorderSize = 24;
	/// <summary>
	/// The default space left around each circle, in texels: <c>96</c> on both axes.
	/// </summary>
	public static readonly XYPair<int> CirclesDefaultPaddingSize = new(96);
	/// <summary>
	/// The default number of times a circle pattern repeats across each axis: <c>3</c> on both axes.
	/// </summary>
	public static readonly XYPair<int> CirclesDefaultRepetitions = new(3);

	/// <summary>
	/// The default width and height of a gradient pattern, in texels: <c>(512, 512)</c>.
	/// </summary>
	public static readonly XYPair<int> GradientDefaultResolution = (512, 512);

	/// <summary>
	/// The default number of times a line pattern repeats: <c>4</c>.
	/// </summary>
	public const int LineDefaultRepeatCount = 4;
	/// <summary>
	/// The default width and height of a line pattern, in texels: <c>1024</c>.
	/// </summary>
	public const int LineDefaultTextureSize = 1024;
	/// <summary>
	/// The default amount by which a line pattern's lines wander from straight: <c>0f</c>, i.e. perfectly straight.
	/// </summary>
	public const float LineDefaultPerturbationMagnitude = 0f;
	/// <summary>
	/// The default rate at which a line pattern's lines wander back and forth along their length: <c>1f</c>.
	/// </summary>
	/// <remarks>
	/// This has no visible effect whilst the amount of wandering is <c>0f</c>.
	/// </remarks>
	public const float LineDefaultPerturbationFrequency = 1f;

	/// <summary>
	/// The default width and height of the interior of a rectangle, in texels: <c>(512, 256)</c>.
	/// </summary>
	public static readonly XYPair<int> RectanglesDefaultInteriorSize = (512, 256);
	/// <summary>
	/// The default thickness of a rectangle's border, in texels: <c>(64, 32)</c>.
	/// </summary>
	public static readonly XYPair<int> RectanglesDefaultBorderSize = (64, 32);
	/// <summary>
	/// The default space left around each rectangle, in texels: <c>(128, 64)</c>.
	/// </summary>
	public static readonly XYPair<int> RectanglesDefaultPaddingSize = (128, 64);
	/// <summary>
	/// The default number of times a rectangle pattern repeats across each axis: <c>(4, 8)</c>.
	/// </summary>
	public static readonly XYPair<int> RectanglesDefaultRepetitions = (4, 8);
}