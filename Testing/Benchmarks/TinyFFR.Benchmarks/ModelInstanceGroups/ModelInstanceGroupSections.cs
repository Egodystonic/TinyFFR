// Created on 2026-09-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Benchmarks.Harness;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Benchmarks.ModelInstanceGroups;

static class ModelInstanceGroupSections {
	static readonly Vect ForwardNudge = Direction.Forward * 0.01f;
	static readonly Vect BackwardNudge = Direction.Backward * 0.01f;
	static readonly Transform TransformA = new(Direction.Right * 1f, 30f % Direction.Up, Vect.One * 0.5f);
	static readonly Transform TransformB = new(Direction.Left * 1f, 60f % Direction.Up, Vect.One * 1.5f);

	static ILocalTinyFfrFactory Factory => BenchmarkEnvironment.Factory;
	static IResourceAllocator Allocator => BenchmarkEnvironment.Allocator;

	public static int RequestedGroupSize { get; set; } = ModelInstanceGroupWorkload.DefaultGroupSize;
	public static int Sink { get; private set; }

	static bool _isBuilt;
	static int _groupSize;
	static Mesh _mesh;
	static Material _materialA;
	static Material _materialB;
	static Memory<ModelInstance> _instances;
	static ModelInstanceGroup _plainGroup;
	static ModelInstanceGroup _mixedGroup;

	public static void Build() {
		if (_isBuilt && _groupSize == RequestedGroupSize) return;
		TearDown();

		_groupSize = RequestedGroupSize;
		_mesh = Factory.MeshBuilder.CreateCuboid(Cuboid.UnitCube, name: "Benchmark Group Mesh");
		_materialA = Factory.MaterialBuilder.CreateTestMaterial();
		_materialB = Factory.MaterialBuilder.CreateTestMaterial(ignoresLighting: true);

		_instances = Allocator.CreatePooledMemoryBuffer<ModelInstance>(_groupSize);
		var instances = _instances.Span[.._groupSize];
		for (var i = 0; i < _groupSize; ++i) {
			instances[i] = Factory.ObjectBuilder.CreateModelInstance(_mesh, _materialA, name: "Benchmark Grouped Instance");
		}
		_plainGroup = Factory.ObjectBuilder.GroupModelInstances(instances, disposingGroupDisposesInstances: false, "Benchmark Plain Group");

		var mixed = Allocator.CreateResourceGroup(disposeContainedResourcesWhenDisposed: true, "Benchmark Mixed Group", _groupSize * 3);
		for (var i = 0; i < _groupSize; ++i) {
			var mesh = Factory.MeshBuilder.CreateCuboid(Cuboid.UnitCube, name: "Benchmark Mixed Group Mesh");
			var material = Factory.MaterialBuilder.CreateTestMaterial();
			mixed.Add(mesh);
			mixed.Add(material);
			mixed.Add(Factory.ObjectBuilder.CreateModelInstance(mesh, material, name: "Benchmark Mixed Group Instance"));
		}
		mixed.Seal();
		_mixedGroup = new ModelInstanceGroup(mixed);

		_isBuilt = true;
	}

	public static void TearDown() {
		if (!_isBuilt) return;
		_isBuilt = false;

		_mixedGroup.Dispose();
		_plainGroup.Dispose();
		var instances = _instances.Span[.._groupSize];
		for (var i = 0; i < instances.Length; ++i) instances[i].Dispose();
		Allocator.ReturnPooledMemoryBuffer(_instances);
		_materialB.Dispose();
		_materialA.Dispose();
		_mesh.Dispose();
	}

	static int Passes(int touches) => Math.Max(1, touches / _groupSize);

	public static void Foreach() {
		Build();
		var group = _plainGroup;
		var count = 0;
		var passes = Passes(ModelInstanceGroupWorkload.EnumerationTouches);
		for (var pass = 0; pass < passes; ++pass) {
			foreach (var instance in group) {
				if (instance != default) ++count;
			}
		}
		Sink = count;
	}

	public static void Indexer() {
		Build();
		var group = _plainGroup;
		var count = 0;
		var passes = Passes(ModelInstanceGroupWorkload.EnumerationTouches);
		for (var pass = 0; pass < passes; ++pass) {
			for (var i = 0; i < group.Count; ++i) {
				if (group[i] != default) ++count;
			}
		}
		Sink = count;
	}

	public static void MoveBy() => MoveGroup(_plainGroup);
	public static void MixedGroupMoveBy() => MoveGroup(_mixedGroup);

	static void MoveGroup(ModelInstanceGroup group) {
		Build();
		var passes = Passes(ModelInstanceGroupWorkload.MutationTouches);
		for (var pass = 0; pass < passes; ++pass) {
			group.MoveBy((pass & 1) == 0 ? ForwardNudge : BackwardNudge);
		}
	}

	public static void SceneObjectMoveBy() {
		Build();
		var sceneObject = new SceneObject(_plainGroup);
		var passes = Passes(ModelInstanceGroupWorkload.MutationTouches);
		for (var pass = 0; pass < passes; ++pass) {
			sceneObject.MoveBy((pass & 1) == 0 ? ForwardNudge : BackwardNudge);
		}
	}

	public static void SceneObjectSetTransform() {
		Build();
		var sceneObject = new SceneObject(_plainGroup);
		var passes = Passes(ModelInstanceGroupWorkload.MutationTouches);
		for (var pass = 0; pass < passes; ++pass) {
			sceneObject.Transform = (pass & 1) == 0 ? TransformA : TransformB;
		}
	}

	public static void SceneObjectGetTransform() {
		Build();
		var sceneObject = new SceneObject(_plainGroup);
		var count = 0;
		for (var query = 0; query < ModelInstanceGroupWorkload.FirstInstanceQueryCount; ++query) {
			if (sceneObject.Transform != Transform.None) ++count;
		}
		Sink = count;
	}

	public static void SetTransform() {
		Build();
		var group = _plainGroup;
		var passes = Passes(ModelInstanceGroupWorkload.MutationTouches);
		for (var pass = 0; pass < passes; ++pass) {
			group.SetTransform((pass & 1) == 0 ? TransformA : TransformB);
		}
	}

	public static void SetMaterial() {
		Build();
		var group = _plainGroup;
		var passes = Passes(ModelInstanceGroupWorkload.MutationTouches);
		for (var pass = 0; pass < passes; ++pass) {
			group.SetMaterial((pass & 1) == 0 ? _materialA : _materialB);
		}
	}

	public static void GetTransform() {
		Build();
		var group = _plainGroup;
		var count = 0;
		for (var query = 0; query < ModelInstanceGroupWorkload.FirstInstanceQueryCount; ++query) {
			if (group.Transform != Transform.None) ++count;
		}
		Sink = count;
	}

	public static void MixedGroupMeshes() {
		Build();
		var resourceGroup = _mixedGroup.UnderlyingResourceGroup;
		var count = 0;
		var passes = Passes(ModelInstanceGroupWorkload.ResourceGroupEnumerationTouches);
		for (var pass = 0; pass < passes; ++pass) {
			foreach (var mesh in resourceGroup.Meshes) {
				if (mesh != default) ++count;
			}
		}
		Sink = count;
	}

	public static void CreateAndDisposeGroup() {
		Build();
		var instances = _instances.Span[.._groupSize];
		var passes = Passes(ModelInstanceGroupWorkload.GroupCreationTouches);
		for (var pass = 0; pass < passes; ++pass) {
			using var group = Factory.ObjectBuilder.GroupModelInstances(instances, disposingGroupDisposesInstances: false, "Benchmark Transient Group");
		}
	}
}
