namespace VoxelWorld;

sealed class Noise {
	const float Skew = 0.36602540378f;
	const float Unskew = 0.2113248654f;
	static readonly float[] GradientX = { 1f, -1f, 1f, -1f, 1f, -1f, 0f, 0f };
	static readonly float[] GradientY = { 1f, 1f, -1f, -1f, 0f, 0f, 1f, -1f };
	readonly byte[] _permutation = new byte[512];

	public Noise(int seed) {
		var source = new byte[256];
		for (var i = 0; i < source.Length; ++i) source[i] = (byte) i;
		var random = new Random(seed);
		for (var i = source.Length - 1; i > 0; --i) {
			var j = random.Next(i + 1);
			(source[i], source[j]) = (source[j], source[i]);
		}
		for (var i = 0; i < _permutation.Length; ++i) _permutation[i] = source[i & 255];
	}

	public float Sample(float x, float y) {
		var skewOffset = (x + y) * Skew;
		var cellX = (int) MathF.Floor(x + skewOffset);
		var cellY = (int) MathF.Floor(y + skewOffset);
		var unskewOffset = (cellX + cellY) * Unskew;
		var x0 = x - (cellX - unskewOffset);
		var y0 = y - (cellY - unskewOffset);

		var stepX = x0 > y0 ? 1 : 0;
		var stepY = 1 - stepX;
		var x1 = x0 - stepX + Unskew;
		var y1 = y0 - stepY + Unskew;
		var x2 = x0 - 1f + 2f * Unskew;
		var y2 = y0 - 1f + 2f * Unskew;

		var hashX = cellX & 255;
		var hashY = cellY & 255;
		var corner0 = Corner(x0, y0, _permutation[hashX + _permutation[hashY]]);
		var corner1 = Corner(x1, y1, _permutation[hashX + stepX + _permutation[hashY + stepY]]);
		var corner2 = Corner(x2, y2, _permutation[hashX + 1 + _permutation[hashY + 1]]);
		return 70f * (corner0 + corner1 + corner2);
	}

	public float SampleFractal(float x, float y, int octaves) {
		var total = 0f;
		var amplitude = 1f;
		var frequency = 1f;
		var amplitudeSum = 0f;
		for (var i = 0; i < octaves; ++i) {
			total += Sample(x * frequency, y * frequency) * amplitude;
			amplitudeSum += amplitude;
			amplitude *= 0.5f;
			frequency *= 2f;
		}
		return total / amplitudeSum;
	}

	static float Corner(float x, float y, int hash) {
		var falloff = 0.5f - x * x - y * y;
		if (falloff <= 0f) return 0f;
		falloff *= falloff;
		var gradientIndex = hash & 7;
		return falloff * falloff * (GradientX[gradientIndex] * x + GradientY[gradientIndex] * y);
	}
}
