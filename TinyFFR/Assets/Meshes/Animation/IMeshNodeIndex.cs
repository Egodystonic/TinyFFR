// Created on 2026-09-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Assets.Meshes;

#pragma warning disable CA1710 // "Must be called Collection because it implements IROCollection<>" -- I disagree in this case
/// <summary>
/// An index that allows you to look up the joints of a skeleton, addressable by name or by position.
/// </summary>
/// <remarks>
/// <see cref="MeshNodeIndex"/> is the implementation for a single <see cref="Mesh"/>'s skeleton; <see cref="MeshGroupNodeIndex"/> is the implementation for a
/// skeleton shared by a <see cref="MeshGroupAnimationTable"/>.
/// </remarks>
public interface IMeshNodeIndex : IReadOnlyCollection<MeshNode> {
#pragma warning restore CA1710
#pragma warning disable CA1043 // Telling me to use a string or int arg for indexers -- we are though, just a more GC-friendly one
	/// <summary>
	/// Returns the joint with the given name.
	/// </summary>
	/// <param name="name">The name of the joint to find.</param>
	/// <exception cref="KeyNotFoundException">Thrown when the skeleton has no joint of that name.</exception>
	MeshNode this[ReadOnlySpan<char> name] {
#pragma warning restore CA1043
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
	}
	/// <summary>
	/// Returns the joint at the given position in the skeleton's node list.
	/// </summary>
	/// <param name="index">Which joint to return, in the range <c>0 &lt;= index &lt; Count</c>.</param>
	MeshNode this[int index] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
	}
	/// <summary>
	/// Returns the joint with the given name, or <see langword="null"/> if the skeleton has none of that name.
	/// </summary>
	/// <param name="name">The name of the joint to find.</param>
	MeshNode? TryGetNodeByName(ReadOnlySpan<char> name);
}

/// <summary>
/// An <see cref="IMeshNodeIndex"/> that can also enumerate its joints without generating garbage.
/// </summary>
/// <typeparam name="TEnumerationArg">The type that owns the skeleton and backs the enumerable (a <see cref="Mesh"/> or a <see cref="MeshGroupAnimationTable"/>).</typeparam>
public interface IMeshNodeIndex<TEnumerationArg> : IMeshNodeIndex {
	/// <summary>
	/// Every joint in the skeleton.
	/// </summary>
	IndirectEnumerable<TEnumerationArg, MeshNode> All {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
	}
}
