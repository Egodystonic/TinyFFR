// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

#pragma warning disable CA1710 // "Must be called Collection because it implements IROCollection<>" -- I disagree in this case
/// <summary>
/// An index that allows you to look up the the joints of one mesh's skeleton, addressable by name or by position.
/// </summary>
/// <param name="Skeleton">The skeleton whose joints these are.</param>
public readonly record struct MeshNodeIndex(MeshSkeleton Skeleton) : IReadOnlyCollection<MeshNode> {
#pragma warning restore CA1710
	/// <summary>
	/// The mesh whose skeleton these nodes belong to.
	/// </summary>
	public Mesh Mesh {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Skeleton.Mesh;
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
	public IndirectEnumerable<Mesh, MeshNode> All {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Mesh.GetNodes();
	}
	
#pragma warning disable CA1043 // Telling me to use a string or int arg for indexers -- we are though, just a more GC-friendly one
	/// <summary>
	/// Returns the joint with the given name.
	/// </summary>
	/// <remarks>
	/// Joints are named either in the file the mesh came from or through the mesh builder.
	/// </remarks>
	/// <param name="name">The name of the joint to find.</param>
	/// <exception cref="KeyNotFoundException">Thrown when the skeleton has no joint of that name.</exception>
	public MeshNode this[ReadOnlySpan<char> name] {
#pragma warning restore CA1043
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TryGetNodeByName(name) ?? throw new KeyNotFoundException($"No node with name '{name}' was found for this mesh ({Mesh}).");
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
	public MeshNode? TryGetNodeByName(ReadOnlySpan<char> name) => Mesh.TryGetNodeByName(name);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<MeshNode> IEnumerable<MeshNode>.GetEnumerator() => GetEnumerator();
	/// <summary>
	/// Returns an enumerator over every joint in the skeleton.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IndirectEnumerable<Mesh, MeshNode>.Enumerator GetEnumerator() => All.GetEnumerator();
}