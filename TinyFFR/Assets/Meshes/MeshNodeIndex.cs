// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

#pragma warning disable CA1710 // "Must be called Collection because it implements IROCollection<>" -- I disagree in this case
public readonly record struct MeshNodeIndex(MeshSkeleton Skeleton) : IReadOnlyCollection<MeshNode> {
#pragma warning restore CA1710
	public Mesh Mesh {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Skeleton.Mesh;
	}
	
	public int Count {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => All.Count;
	}
	
	public IndirectEnumerable<Mesh, MeshNode> All {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Mesh.GetNodes();
	}
	
#pragma warning disable CA1043 // Telling me to use a string or int arg for indexers -- we are though, just a more GC-friendly one
	public MeshNode this[ReadOnlySpan<char> name] {
#pragma warning restore CA1043
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TryGetNodeByName(name) ?? throw new KeyNotFoundException($"No node with name '{name}' was found for this mesh ({Mesh}).");
	}
	
	public MeshNode this[int index] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => All[index];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshNode? TryGetNodeByName(ReadOnlySpan<char> name) => Mesh.TryGetNodeByName(name);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<MeshNode> IEnumerable<MeshNode>.GetEnumerator() => GetEnumerator();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IndirectEnumerable<Mesh, MeshNode>.Enumerator GetEnumerator() => All.GetEnumerator();
}