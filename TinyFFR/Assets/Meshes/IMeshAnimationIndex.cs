namespace Egodystonic.TinyFFR.Assets.Meshes;

#pragma warning disable CA1710 // "Must be called Collection because it implements IROCollection<>" -- I disagree in this case
/// <summary>
/// An index that allows you to look up the animations belonging to one mesh, addressable by name, by position or by kind.
/// Usually you'll pass the returned <see cref="MeshAnimation"/>s to a <see cref="MeshAnimationPlayer"/> or <see cref="MeshBlendedAnimationPlayer"/>.
/// </summary>
/// <remarks>
/// Animations are named in the file they were authored in, so looking one up by name is the usual way to find it.
/// </remarks>
public interface IMeshAnimationIndex : IReadOnlyCollection<MeshAnimation> {
#pragma warning restore CA1710
	/// <summary>
	/// Returns the animation with the given name.
	/// </summary>
	/// <param name="name">The name of the animation to find.</param>
	/// <exception cref="KeyNotFoundException">Thrown when the mesh has no animation of that name.</exception>
#pragma warning disable CA1043 // Telling me to use a string or int arg for indexers -- we are though, just a more GC-friendly one
	MeshAnimation this[ReadOnlySpan<char> name] {
#pragma warning restore CA1043
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
	}
	/// <summary>
	/// Returns the animation at the given position in the mesh's animation list.
	/// </summary>
	/// <param name="index">Which animation to return, in the range <c>0 &lt;= index &lt; Count</c>.</param>
	MeshAnimation this[int index] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
	}
	/// <summary>
	/// Returns the animation with the given name, or <see langword="null"/> if the mesh has none of that name.
	/// </summary>
	/// <param name="name">The name of the animation to find.</param>
	MeshAnimation? TryGetAnimationByName(ReadOnlySpan<char> name);
	/// <summary>
	/// Returns the animation of the given kind with the given name, or <see langword="null"/> if the mesh has none matching.
	/// </summary>
	/// <param name="name">The name of the animation to find.</param>
	/// <param name="animationType">Which kind of animation to look for.</param>
	MeshAnimation? TryGetAnimationByName(ReadOnlySpan<char> name, MeshAnimationType animationType);
}

public interface IMeshAnimationIndex<TEnumerationArg> : IMeshAnimationIndex {
	/// <summary>
	/// The mesh's skeletal animations, which deform it by moving a tree of joints.
	/// </summary>
	IndirectEnumerable<TEnumerationArg, MeshAnimation> Skeletal {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
	}
	/// <summary>
	/// The mesh's morphing animations, which deform it by interpolating its vertices directly.
	/// </summary>
	IndirectEnumerable<TEnumerationArg, MeshAnimation> Morphing {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
	}
	/// <summary>
	/// Every animation the mesh has, of either kind.
	/// </summary>
	IndirectEnumerable<TEnumerationArg, MeshAnimation> All {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
	}
}