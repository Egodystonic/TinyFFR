// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

#pragma warning disable CA1710 // "Must be called Collection because it implements IROCollection<>" -- I disagree in this case
/// <summary>
/// An index that allows you to look up the animations shared by a group of meshes, addressable by name, by position or by kind.
/// Usually you'll pass the returned <see cref="MeshAnimation"/>s to a <see cref="MeshAnimationPlayer"/> or <see cref="MeshBlendedAnimationPlayer"/> targeting a
/// <see cref="ModelInstanceGroup"/>.
/// </summary>
/// <remarks>
/// <para>
/// Animations are named in the file they were authored in, so looking one up by name is the usual way to find it.
/// </para>
/// <para>
/// This is the group equivalent of <see cref="MeshAnimationIndex"/>. You'll normally reach it via <see cref="ModelBundle.Animations"/> or
/// <see cref="ModelInstanceGroup.Animations"/>, rather than constructing one yourself.
/// </para>
/// </remarks>
/// <param name="Table">The animation table whose animations these are.</param>
public readonly record struct MeshGroupAnimationIndex(MeshGroupAnimationTable Table) : IMeshAnimationIndex<MeshGroupAnimationTable> {
#pragma warning restore CA1710
	/// <summary>
	/// How many animations the table has in total.
	/// </summary>
	public int Count {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => All.Count;
	}

	/// <summary>
	/// The table's skeletal animations, which deform the group's meshes by moving their shared tree of joints.
	/// </summary>
	public IndirectEnumerable<MeshGroupAnimationTable, MeshAnimation> Skeletal {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Table.GetAnimations(MeshAnimationType.Skeletal);
	}
	/// <summary>
	/// The table's morphing animations, which deform the group's meshes by interpolating their vertices directly.
	/// </summary>
	public IndirectEnumerable<MeshGroupAnimationTable, MeshAnimation> Morphing {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Table.GetAnimations(MeshAnimationType.Morphing);
	}
	/// <summary>
	/// Every animation the table has, of either kind.
	/// </summary>
	public IndirectEnumerable<MeshGroupAnimationTable, MeshAnimation> All {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Table.GetAnimations(null);
	}
	
#pragma warning disable CA1043 // Telling me to use a string or int arg for indexers -- we are though, just a more GC-friendly one
	/// <summary>
	/// Returns the animation with the given name.
	/// </summary>
	/// <param name="name">The name of the animation to find.</param>
	/// <exception cref="KeyNotFoundException">Thrown when the table has no animation of that name.</exception>
	public MeshAnimation this[ReadOnlySpan<char> name] {
#pragma warning restore CA1043
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TryGetAnimationByName(name) ?? throw new KeyNotFoundException($"No animation with name '{name}' was found for this mesh group ({Table}).");
	}
	
	/// <summary>
	/// Returns the animation at the given position in the table's animation list.
	/// </summary>
	/// <param name="index">Which animation to return, in the range <c>0 &lt;= index &lt; Count</c>.</param>
	public MeshAnimation this[int index] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => All[index];
	}

	/// <summary>
	/// Returns the animation with the given name, or <see langword="null"/> if the table has none of that name.
	/// </summary>
	/// <param name="name">The name of the animation to find.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshAnimation? TryGetAnimationByName(ReadOnlySpan<char> name) => Table.TryGetAnimationByName(name, null);
	
	/// <summary>
	/// Returns the animation of the given kind with the given name, or <see langword="null"/> if the table has none matching.
	/// </summary>
	/// <param name="name">The name of the animation to find.</param>
	/// <param name="animationType">Which kind of animation to look for.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshAnimation? TryGetAnimationByName(ReadOnlySpan<char> name, MeshAnimationType animationType) => Table.TryGetAnimationByName(name, animationType);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<MeshAnimation> IEnumerable<MeshAnimation>.GetEnumerator() => GetEnumerator();
	/// <summary>
	/// Returns an enumerator over every animation the table has.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IndirectEnumerable<MeshGroupAnimationTable, MeshAnimation>.Enumerator GetEnumerator() => All.GetEnumerator();
}