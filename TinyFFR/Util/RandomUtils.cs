using System;

namespace Egodystonic.TinyFFR;

static class RandomUtils {
	public static readonly Random GlobalRng = new();
	const double IntegerScalar = 1d / (Int32.MaxValue - 1);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float NextSingle() => GlobalRng.NextSingle();
	public static float NextSingle(float minInclusive, float maxExclusive) => (maxExclusive - minInclusive) * GlobalRng.NextSingle() + minInclusive;
	public static float NextSingleInclusive(float minInclusive, float maxInclusive) => (float) (NextSingle(minInclusive, maxInclusive) * IntegerScalar);
	
	// Change to this algorithm requires change to RandomUtilsTest.NextSingleInclusiveAlgorithmShouldWorkAsExpected()
	public static float NextSingleZeroToOneInclusive() => (float) (GlobalRng.Next() * IntegerScalar);
	
	// Change to this algorithm requires change to RandomUtilsTest.NextSingleInclusiveAlgorithmShouldWorkAsExpected()
	public static float NextSingleNegOneToOneInclusive() => (float) (GlobalRng.Next() * IntegerScalar * 2d - 1d);
}