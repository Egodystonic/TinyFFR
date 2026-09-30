// Created on 2026-09-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Assets.Meshes;

#pragma warning disable CA1710 // "Must be called Collection because it implements IROCollection<>" -- I disagree in this case
public readonly record struct MeshGroupNodeIndex(MeshGroupSkeleton Skeleton) : IMeshNodeIndex<MeshGroupAnimationTable> {
#pragma warning restore CA1710
	public MeshGroupAnimationTable Table {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Skeleton.Table;
	}

	public int Count {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => All.Count;
	}

	public IndirectEnumerable<MeshGroupAnimationTable, MeshNode> All {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Table.GetNodes();
	}

#pragma warning disable CA1043 // Telling me to use a string or int arg for indexers -- we are though, just a more GC-friendly one
	public MeshNode this[ReadOnlySpan<char> name] {
#pragma warning restore CA1043
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TryGetNodeByName(name) ?? throw new KeyNotFoundException($"No node with name '{name}' was found for this mesh group ({Table}).");
	}

	public MeshNode this[int index] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => All[index];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshNode? TryGetNodeByName(ReadOnlySpan<char> name) => Table.TryGetNodeByName(name);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<MeshNode> IEnumerable<MeshNode>.GetEnumerator() => GetEnumerator();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IndirectEnumerable<MeshGroupAnimationTable, MeshNode>.Enumerator GetEnumerator() => All.GetEnumerator();
}
