// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Benchmarks.Smoke;

static partial class SmokeSections {
	const string NamedMeshName = "Benchmark Named Mesh";

	public static void ResourceGroups() {
		using var group = Allocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true, "Benchmark Resource Group", SmokeWorkload.ResourceGroupResourceCount);

		var material = Factory.MaterialBuilder.CreateTestMaterial();
		group.Add(material);

		for (var i = 0; i < SmokeWorkload.ResourceGroupResourceCount; ++i) {
			var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Benchmark Grouped Mesh");
			var instance = Factory.ObjectBuilder.CreateModelInstance(mesh, material, name: "Benchmark Grouped Instance");
			group.Add(mesh);
			group.Add(instance);
		}

		for (var pass = 0; pass < SmokeWorkload.ResourceGroupPassCount; ++pass) {
			_ = group.ResourceCount;
			var containedMeshCount = 0;
			foreach (var containedMesh in group.Meshes) {
				if (containedMesh != default) ++containedMeshCount;
			}
			var containedInstanceCount = 0;
			foreach (var containedInstance in group.ModelInstances) {
				if (containedInstance != default) ++containedInstanceCount;
			}
		}
	}

	public static void ResourceNamingAndDirectory() {
		var meshes = Allocator.GetSharedScratchList<Mesh>();
		Span<char> nameBuffer = stackalloc char[NamedMeshName.Length];

		for (var i = 0; i < SmokeWorkload.NamedResourceCount; ++i) {
			meshes.Add(Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: NamedMeshName));
		}

		try {
			for (var i = 0; i < meshes.Count; ++i) {
				_ = meshes[i].GetNameLength();
				meshes[i].CopyName(nameBuffer);
			}

			for (var repeat = 0; repeat < SmokeWorkload.NameLookupRepeatCount; ++repeat) {
				_ = Factory.ResourceDirectory.FindByName<Mesh>(NamedMeshName);

				var activeCount = 0;
				foreach (var activeMesh in Factory.ResourceDirectory.GetAllActiveInstances<Mesh>()) {
					if (activeMesh != default) ++activeCount;
				}
			}
		}
		finally {
			for (var i = 0; i < meshes.Count; ++i) meshes[i].Dispose();
		}
	}

	public static void AllocatorCollections() {
		for (var repeat = 0; repeat < SmokeWorkload.ScratchCollectionRepeatCount; ++repeat) {
			var list = Allocator.GetSharedScratchList<int>();
			var secondList = Allocator.GetSharedScratchList<int>(1);
			var map = Allocator.GetSharedScratchDictionary<int, int>();
			var set = Allocator.GetSharedScratchSet<int>();

			for (var i = 0; i < SmokeWorkload.ScratchCollectionElementCount; ++i) {
				list.Add(i);
				secondList.Add(i * 2);
				map.Add(i, i * 3);
				set.Add(i);
			}

			for (var i = 0; i < SmokeWorkload.ScratchCollectionElementCount; ++i) {
				_ = list[i];
				_ = map[i];
				_ = set.Contains(i);
			}

			var pooledBuffer = Allocator.CreatePooledMemoryBuffer<Location>(SmokeWorkload.ScratchCollectionElementCount);
			try {
				var span = pooledBuffer.Span;
				for (var i = 0; i < span.Length; ++i) span[i] = new Location(i, i, i);
			}
			finally {
				Allocator.ReturnPooledMemoryBuffer(pooledBuffer);
			}
		}
	}

	public static void DisplayDiscovery() {
		var discoverer = Factory.DisplayDiscoverer;

		for (var repeat = 0; repeat < SmokeWorkload.DisplayQueryRepeatCount; ++repeat) {
			_ = discoverer.AtLeastOneDisplayConnected;
			_ = discoverer.Primary;
			_ = discoverer.HighestResolution;
			_ = discoverer.HighestRefreshRate;

			var displays = discoverer.All;
			for (var i = 0; i < displays.Length; ++i) {
				_ = displays[i].CurrentResolution;
				_ = displays[i].HighestSupportedRefreshRateMode;
				_ = displays[i].HighestSupportedResolutionMode;
				_ = displays[i].IsPrimary;
			}
		}
	}
}
