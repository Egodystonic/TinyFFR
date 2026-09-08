// Created on 2026-09-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Benchmarks.Soak;

static class SoakWorkload {
	public const double DefaultDurationMinutes = 10d;
	public const double SampleIntervalSeconds = 15d;

	public const int InstanceCount = 64;
	public const int LightCount = 8;
	public const int QueriesPerFrame = 8;
	public const int QueryResultCapacity = 16;
	public const int TransientChurnFrameInterval = 30;
	public const int TransientChurnCount = 4;
	public const int SphereSubdivisionLevel = 3;
}
