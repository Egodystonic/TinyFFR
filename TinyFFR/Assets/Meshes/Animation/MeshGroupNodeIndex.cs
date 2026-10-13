// Created on 2026-09-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Assets.Meshes;

#pragma warning disable CA1710 // "Must be called Collection because it implements IROCollection<>" -- I disagree in this case
/// <summary>
/// An index that allows you to look up the joints of a skeleton shared by a group of meshes, addressable by name or by position.
/// </summary>
/// <remarks>
/// This is the group equivalent of <see cref="MeshNodeIndex"/>. The skeleton contains every joint used by any mesh in the group, so a given joint may drive
/// only some of the group's meshes.
/// </remarks>
/// <param name="Skeleton">The skeleton whose joints these are.</param>
public readonly record struct MeshGroupNodeIndex(MeshGroupSkeleton Skeleton) : IMeshNodeIndex<MeshGroupAnimationTable> {
#pragma warning restore CA1710
	/// <summary>
	/// The animation table whose skeleton these nodes belong to.
	/// </summary>
	public MeshGroupAnimationTable Table {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Skeleton.Table;
	}

	/// <summary>
	/// How many joints the skeleton has.
	/// </summary>
	public int Count {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => All.Count;
	}

	/// <summary>
	/// Every joint in the skeleton.
	/// </summary>
	public IndirectEnumerable<MeshGroupAnimationTable, MeshNode> All {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Table.GetNodes();
	}

#pragma warning disable CA1043 // Telling me to use a string or int arg for indexers -- we are though, just a more GC-friendly one
	/// <summary>
	/// Returns the joint with the given name.
	/// </summary>
	/// <remarks>
	/// Joints are named in the file the meshes came from.
	/// </remarks>
	/// <param name="name">The name of the joint to find.</param>
	/// <exception cref="KeyNotFoundException">Thrown when the skeleton has no joint of that name.</exception>
	public MeshNode this[ReadOnlySpan<char> name] {
#pragma warning restore CA1043
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TryGetNodeByName(name) ?? throw new KeyNotFoundException($"No node with name '{name}' was found for this mesh group ({Table}).");
	}

	/// <summary>
	/// Returns the joint at the given position in the skeleton's node list.
	/// </summary>
	/// <param name="index">Which joint to return, in the range <c>0 &lt;= index &lt; Count</c>.</param>
	public MeshNode this[int index] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => All[index];
	}

	/// <summary>
	/// Returns the joint with the given name, or <see langword="null"/> if the skeleton has none of that name.
	/// </summary>
	/// <param name="name">The name of the joint to find.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshNode? TryGetNodeByName(ReadOnlySpan<char> name) => Table.TryGetNodeByName(name);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<MeshNode> IEnumerable<MeshNode>.GetEnumerator() => GetEnumerator();
	/// <summary>
	/// Returns an enumerator over every joint in the skeleton.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IndirectEnumerable<MeshGroupAnimationTable, MeshNode>.Enumerator GetEnumerator() => All.GetEnumerator();
}
