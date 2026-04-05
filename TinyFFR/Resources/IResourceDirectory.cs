// Created on 2026-03-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Resources;

public interface IResourceDirectory {
	protected internal const bool DefaultAllowPartialMatch = false;
	protected internal const StringComparison DefaultComparisonType = StringComparison.OrdinalIgnoreCase;
	
	IndirectEnumerable<object, TResource> GetAllActiveInstances<TResource>() where TResource : struct, IResource => GetDirectoryForType<TResource>().AllActiveInstances;
	
	TResource? FindByName<TResource>(ReadOnlySpan<char> name, bool allowPartialMatch = DefaultAllowPartialMatch, StringComparison comparisonType = DefaultComparisonType) where TResource : struct, IResource {
		return GetDirectoryForType<TResource>().FindByName(name, allowPartialMatch, comparisonType);
	}
	// TODO xmldoc that dest can be smaller than the result set, that's allowed - this method returns the number of matches total
	int FindByName<TResource>(Span<TResource> dest, ReadOnlySpan<char> name, bool allowPartialMatch = DefaultAllowPartialMatch, StringComparison comparisonType = DefaultComparisonType) where TResource : struct, IResource {
		return GetDirectoryForType<TResource>().FindByName(dest, name, allowPartialMatch, comparisonType);
	}
	IResourceDirectory<TResource> GetDirectoryForType<TResource>() where TResource : struct, IResource;
}
public interface IResourceDirectory<TResource> where TResource : struct, IResource {
	protected const bool DefaultAllowPartialMatch = IResourceDirectory.DefaultAllowPartialMatch;
	protected const StringComparison DefaultComparisonType = IResourceDirectory.DefaultComparisonType;
	
	IndirectEnumerable<object, TResource> AllActiveInstances { get; }
	protected bool ResourceNameMatchIsMatching(TResource resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType);
	
	TResource? FindByName(ReadOnlySpan<char> name, bool allowPartialMatch = DefaultAllowPartialMatch, StringComparison comparisonType = DefaultComparisonType) {
		Unsafe.SkipInit(out TResource result);
		if (FindByName(new Span<TResource>(ref result), name, allowPartialMatch, comparisonType) == 1) return result;
		else return null;
	}
	// TODO xmldoc that dest can be smaller than the result set, that's allowed - this method returns the number of matches total
	int FindByName(Span<TResource> dest, ReadOnlySpan<char> name, bool allowPartialMatch = DefaultAllowPartialMatch, StringComparison comparisonType = DefaultComparisonType) {
		var result = 0;
		foreach (var instance in AllActiveInstances) {
			if (!ResourceNameMatchIsMatching(instance, name, allowPartialMatch, comparisonType)) continue;
			
			if (result < dest.Length) dest[result] = instance;
			++result;
		}
		return result;
	}
}