// Created on 2026-09-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using BenchmarkDotNet.Attributes;
using Egodystonic.TinyFFR.Benchmarks.Harness;

namespace Egodystonic.TinyFFR.Benchmarks.ModelInstanceGroups;

public unsafe class ModelInstanceGroupBenchmark : TinyFfrBenchmark {
	[Params(1, 5, 10, 25, 50, 100, 200)]
	public int GroupSize { get; set; } = ModelInstanceGroupWorkload.DefaultGroupSize;

	protected override void OnGlobalSetup() => Invoke(&ModelInstanceGroupSections.Build);
	protected override void OnGlobalCleanup() => TinyFfrThread.Invoke(&ModelInstanceGroupSections.TearDown);

	[Benchmark] public void Foreach() => Invoke(&ModelInstanceGroupSections.Foreach);
	[Benchmark] public void Indexer() => Invoke(&ModelInstanceGroupSections.Indexer);
	[Benchmark] public void MoveBy() => Invoke(&ModelInstanceGroupSections.MoveBy);
	[Benchmark] public void SetTransform() => Invoke(&ModelInstanceGroupSections.SetTransform);
	[Benchmark] public void SetMaterial() => Invoke(&ModelInstanceGroupSections.SetMaterial);
	[Benchmark] public void GetTransform() => Invoke(&ModelInstanceGroupSections.GetTransform);
	[Benchmark] public void SceneObjectMoveBy() => Invoke(&ModelInstanceGroupSections.SceneObjectMoveBy);
	[Benchmark] public void SceneObjectSetTransform() => Invoke(&ModelInstanceGroupSections.SceneObjectSetTransform);
	[Benchmark] public void SceneObjectGetTransform() => Invoke(&ModelInstanceGroupSections.SceneObjectGetTransform);
	[Benchmark] public void MixedGroupMoveBy() => Invoke(&ModelInstanceGroupSections.MixedGroupMoveBy);
	[Benchmark] public void MixedGroupMeshes() => Invoke(&ModelInstanceGroupSections.MixedGroupMeshes);
	[Benchmark] public void CreateAndDisposeGroup() => Invoke(&ModelInstanceGroupSections.CreateAndDisposeGroup);

	void Invoke(delegate*<void> section) {
		ModelInstanceGroupSections.RequestedGroupSize = GroupSize;
		TinyFfrThread.Invoke(section);
	}
}
