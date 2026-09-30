// Created on 2026-09-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Assets.Meshes;

#pragma warning disable CA1710 // "Must be called Collection because it implements IROCollection<>" -- I disagree in this case
public interface IMeshNodeIndex : IReadOnlyCollection<MeshNode> {
#pragma warning restore CA1710
#pragma warning disable CA1043 // Telling me to use a string or int arg for indexers -- we are though, just a more GC-friendly one
	MeshNode this[ReadOnlySpan<char> name] {
#pragma warning restore CA1043
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
	}
	MeshNode this[int index] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
	}
	MeshNode? TryGetNodeByName(ReadOnlySpan<char> name);
}

public interface IMeshNodeIndex<TEnumerationArg> : IMeshNodeIndex {
	IndirectEnumerable<TEnumerationArg, MeshNode> All {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
	}
}
