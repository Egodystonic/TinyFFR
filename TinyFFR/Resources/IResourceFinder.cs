// Created on 2026-03-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Resources;

public interface IResourceFinder {
	protected internal const bool DefaultAllowPartialMatch = false;
	protected internal const StringComparison DefaultComparisonType = StringComparison.OrdinalIgnoreCase;
	TResource? FindResourceByName<TResource>(ReadOnlySpan<char> name, bool allowPartialMatch = DefaultAllowPartialMatch, StringComparison comparisonType = DefaultComparisonType) where TResource : struct, IResource;
	IndirectEnumerable<object, TResource> GetAllResources<TResource>() where TResource : struct, IResource;
}
public interface IResourceFinder<TResource> where TResource : struct, IResource {
	protected const bool DefaultAllowPartialMatch = IResourceFinder.DefaultAllowPartialMatch;
	protected const StringComparison DefaultComparisonType = IResourceFinder.DefaultComparisonType;
	TResource? FindResourceByName(ReadOnlySpan<char> name, bool allowPartialMatch = DefaultAllowPartialMatch, StringComparison comparisonType = DefaultComparisonType);
	IndirectEnumerable<object, TResource> GetAllResources();
}