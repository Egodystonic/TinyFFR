// Created on 2024-02-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public interface IRandomizable<out TSelf> where TSelf : IRandomizable<TSelf> {
	static abstract TSelf Random();
}
public interface IBoundedRandomizable<TSelf> : IRandomizable<TSelf> where TSelf : IBoundedRandomizable<TSelf>, IRandomizable<TSelf> {
	static abstract TSelf Random(TSelf minInclusive, TSelf maxExclusive);
}

static class RandomUtils {
	public static readonly Random GlobalRng = new();
	const double IntegerScalar = 1d / (Int32.MaxValue - 1);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float NextSingle() => GlobalRng.NextSingle();
	public static float NextSingle(float minInclusive, float maxExclusive) => (maxExclusive - minInclusive) * GlobalRng.NextSingle() + minInclusive;
	
	// Change to this algorithm requires change to RandomUtilsTest.NextSingleInclusiveAlgorithmShouldWorkAsExpected()
	public static float NextSingleZeroToOneInclusive() => (float) (GlobalRng.Next() * IntegerScalar);
	
	// Change to this algorithm requires change to RandomUtilsTest.NextSingleInclusiveAlgorithmShouldWorkAsExpected()
	public static float NextSingleNegOneToOneInclusive() => (float) (GlobalRng.Next() * IntegerScalar * 2d - 1d);
}