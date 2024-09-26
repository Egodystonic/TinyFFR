// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;
using Egodystonic.TinyFFR.Resources.Memory;
using static Egodystonic.TinyFFR.Resources.IHandleImplPairResource;

namespace Egodystonic.TinyFFR.Resources;

static class ResourceDependencyTracker {
	static readonly ArrayPool<ResourceIdent> _dependentsArrayPool = ArrayPool<ResourceIdent>.Shared;
	static readonly ArrayPoolBackedMap<ResourceIdent, ResourceIdent[]> _dependencyMap = new();

	public static void RegisterDependency<TDependent, TTarget>(TDependent dependent, TTarget target) where TDependent : IHandleImplPairResource where TTarget : IHandleImplPairResource {
		var dependentIdent = dependent.Ident;
		var targetIdent = target.Ident;
		ResourceIdent[] dependentsArray;
		_dependencyMap.
	}

	public static void DeregisterDependency<TDependent, TTarget>(TDependent dependent, TTarget target) where TDependent : IHandleImplPairResource where TTarget : IHandleImplPairResource {

	}

	public static void ThrowIfHasDependents<TTarget>(TTarget target) where TTarget : IHandleImplPairResource {

	}
}