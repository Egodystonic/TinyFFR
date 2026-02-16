// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public enum MeshAnimationType {
	Skeletal,
	Morphing
}

public readonly record struct MeshAnimationIndex(Mesh Mesh) {
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
	
	public MeshAnimation this[ReadOnlySpan<char> name] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TryGetAnimationByName(name) ?? throw new KeyNotFoundException($"No animation with name '{name}' was found for this mesh ({Mesh}).");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshAnimation? TryGetAnimationByName(ReadOnlySpan<char> name) => Mesh.TryGetAnimationByName(name, null);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshAnimation? TryGetAnimationByName(ReadOnlySpan<char> name, MeshAnimationType animationType) => Mesh.TryGetAnimationByName(name, animationType);
}