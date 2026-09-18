// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// Identifies which of the two ways of animating a mesh's geometry an animation uses.
/// </summary>
public enum MeshAnimationType {
	/// <summary>
	/// The mesh is deformed by moving a small tree of joints, with each vertex following the joints it is weighted against.
	/// </summary>
	/// <remarks>
	/// This is how a character's limbs are usually animated: a handful of joints drive thousands of vertices.
	/// </remarks>
	Skeletal,
	/// <summary>
	/// The mesh is deformed by interpolating its vertices directly between stored shapes.
	/// </summary>
	/// <remarks>
	/// This suits deformations that no arrangement of joints would produce cleanly, such as facial expressions.
	/// </remarks>
	Morphing
}

#pragma warning disable CA1710 // "Must be called Collection because it implements IROCollection<>" -- I disagree in this case
/// <summary>
/// An index that allows you to look up the animations belonging to one mesh, addressable by name, by position or by kind.
/// Usually you'll pass the returned <see cref="MeshAnimation"/>s to a <see cref="MeshAnimationPlayer"/> or <see cref="MeshBlendedAnimationPlayer"/>.
/// </summary>
/// <remarks>
/// Animations are named in the file they were authored in, so looking one up by name is the usual way to find it.
/// </remarks>
/// <param name="Mesh">The mesh whose animations these are.</param>
public readonly record struct MeshAnimationIndex(Mesh Mesh) : IReadOnlyCollection<MeshAnimation> {
#pragma warning restore CA1710
	/// <summary>
	/// How many animations the mesh has in total.
	/// </summary>
	public int Count {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => All.Count;
	}

	/// <summary>
	/// The mesh's skeletal animations, which deform it by moving a tree of joints.
	/// </summary>
	public IndirectEnumerable<Mesh, MeshAnimation> Skeletal {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Mesh.GetAnimations(MeshAnimationType.Skeletal);
	}
	/// <summary>
	/// The mesh's morphing animations, which deform it by interpolating its vertices directly.
	/// </summary>
	public IndirectEnumerable<Mesh, MeshAnimation> Morphing {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Mesh.GetAnimations(MeshAnimationType.Morphing);
	}
	/// <summary>
	/// Every animation the mesh has, of either kind.
	/// </summary>
	public IndirectEnumerable<Mesh, MeshAnimation> All {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Mesh.GetAnimations(null);
	}
	
#pragma warning disable CA1043 // Telling me to use a string or int arg for indexers -- we are though, just a more GC-friendly one
	/// <summary>
	/// Returns the animation with the given name.
	/// </summary>
	/// <param name="name">The name of the animation to find.</param>
	/// <exception cref="KeyNotFoundException">Thrown when the mesh has no animation of that name.</exception>
	public MeshAnimation this[ReadOnlySpan<char> name] {
#pragma warning restore CA1043
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TryGetAnimationByName(name) ?? throw new KeyNotFoundException($"No animation with name '{name}' was found for this mesh ({Mesh}).");
	}
	
	/// <summary>
	/// Returns the animation at the given position in the mesh's animation list.
	/// </summary>
	/// <param name="index">Which animation to return, in the range <c>0 &lt;= index &lt; Count</c>.</param>
	public MeshAnimation this[int index] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => All[index];
	}

	/// <summary>
	/// Returns the animation with the given name, or <see langword="null"/> if the mesh has none of that name.
	/// </summary>
	/// <param name="name">The name of the animation to find.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshAnimation? TryGetAnimationByName(ReadOnlySpan<char> name) => Mesh.TryGetAnimationByName(name, null);
	
	/// <summary>
	/// Returns the animation of the given kind with the given name, or <see langword="null"/> if the mesh has none matching.
	/// </summary>
	/// <param name="name">The name of the animation to find.</param>
	/// <param name="animationType">Which kind of animation to look for.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshAnimation? TryGetAnimationByName(ReadOnlySpan<char> name, MeshAnimationType animationType) => Mesh.TryGetAnimationByName(name, animationType);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<MeshAnimation> IEnumerable<MeshAnimation>.GetEnumerator() => GetEnumerator();
	/// <summary>
	/// Returns an enumerator over every animation the mesh has.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IndirectEnumerable<Mesh, MeshAnimation>.Enumerator GetEnumerator() => All.GetEnumerator();
}