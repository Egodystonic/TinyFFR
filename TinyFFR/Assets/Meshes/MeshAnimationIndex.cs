// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public enum MeshAnimationType {
	Skeletal,
	Morphing
}

public readonly record struct MeshAnimationIndex(Mesh Mesh) : IEnumerable<MeshAnimation> {
	public bool Any {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Mesh.GetHasAnyAnimations();
	}
	
	public IndirectEnumerable<Mesh, MeshAnimation> Skeletal {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Mesh.GetAnimations(MeshAnimationType.Skeletal);
	}
	public IndirectEnumerable<Mesh, MeshAnimation> Morphing {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Mesh.GetAnimations(MeshAnimationType.Morphing);
	}
	public IndirectEnumerable<Mesh, MeshAnimation> All {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Mesh.GetAnimations(null);
	}
	
#pragma warning disable CA1043 // Telling me to use a string or int arg for indexers -- we are though, just a more GC-friendly one
	public MeshAnimation this[ReadOnlySpan<char> name] {
#pragma warning restore CA1043
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TryGetAnimationByName(name) ?? throw new KeyNotFoundException($"No animation with name '{name}' was found for this mesh ({Mesh}).");
	}
	
	public MeshAnimation this[int index] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => All[index];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshAnimation? TryGetAnimationByName(ReadOnlySpan<char> name) => Mesh.TryGetAnimationByName(name, null);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshAnimation? TryGetAnimationByName(ReadOnlySpan<char> name, MeshAnimationType animationType) => Mesh.TryGetAnimationByName(name, animationType);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<MeshAnimation> IEnumerable<MeshAnimation>.GetEnumerator() => GetEnumerator();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IndirectEnumerable<Mesh, MeshAnimation>.Enumerator GetEnumerator() => All.GetEnumerator();
}