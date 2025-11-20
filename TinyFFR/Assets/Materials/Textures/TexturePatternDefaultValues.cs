using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public sealed class TexturePatternDefaultValues {
	public static readonly XYPair<int> ChequerboardDefaultRepetitionCount = (8, 8);
	public const int ChequerboardDefaultCellResolution = 64;

	public const int CirclesDefaultInteriorRadius = 256;
	public const int CirclesDefaultBorderSize = 24;
	public static readonly XYPair<int> CirclesDefaultPaddingSize = new(96);
	public static readonly XYPair<int> CirclesDefaultRepetitions = new(3);

	public static readonly XYPair<int> GradientDefaultResolution = (512, 512);

	public const int LineDefaultRepeatCount = 4;
	public const int LineDefaultTextureSize = 1024;
	public const float LineDefaultPerturbationMagnitude = 0f;
	public const float LineDefaultPerturbationFrequency = 1f;

	public static readonly XYPair<int> RectanglesDefaultInteriorSize = (512, 256);
	public static readonly XYPair<int> RectanglesDefaultBorderSize = (64, 32);
	public static readonly XYPair<int> RectanglesDefaultPaddingSize = (128, 64);
	public static readonly XYPair<int> RectanglesDefaultRepetitions = (4, 8);
}