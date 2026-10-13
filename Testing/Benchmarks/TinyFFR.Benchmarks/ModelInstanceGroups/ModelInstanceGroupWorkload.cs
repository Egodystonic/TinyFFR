// Created on 2026-09-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Benchmarks.ModelInstanceGroups;

static class ModelInstanceGroupWorkload {
	public const int DefaultGroupSize = 200;

	public const int EnumerationTouches = 160_000_000;
	public const int ResourceGroupEnumerationTouches = 2_400_000;
	public const int MutationTouches = 750_000;
	public const int FirstInstanceQueryCount = 1_800_000;
	public const int GroupCreationTouches = 400_000;
}
