// Created on 2026-03-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Resources;

/// <summary>
/// Provides a directory of all active, non-disposed resources.
/// </summary>
public interface IResourceDirectory {
	/// <summary>
	/// Default value passed to <c>FindByName</c> for <c>allowPartialMatch</c>. 
	/// </summary>
	protected internal const bool DefaultAllowPartialMatch = false;
	/// <summary>
	/// Default value passed to <c>FindByName</c> for <c>comparisonType</c>. 
	/// </summary>
	protected internal const StringComparison DefaultComparisonType = StringComparison.OrdinalIgnoreCase;
	
	/// <summary>
	/// Returns an <see cref="IndirectEnumerable{TIn,TOut}"/> of all currently-live resources of type <typeparamref name="TResource"/>.
	/// </summary>
	/// <typeparam name="TResource">The type of resource you'd like to enumerate.</typeparam>
	IndirectEnumerable<object, TResource> GetAllActiveInstances<TResource>() where TResource : struct, IResource => ForType<TResource>().AllActiveInstances;
	
	/// <summary>
	/// Attempts to find any <typeparamref name="TResource"/> with the given <paramref name="name"/>.
	/// </summary>
	/// <param name="name">The string to search for.</param>
	/// <param name="allowPartialMatch">If <c>true</c>, the search string only needs to match part of a resource's name for it to be returned;
	/// if <c>false</c> the entire string must match exactly. Defaults to <c>false</c>.</param>
	/// <param name="comparisonType">The type of string comparison to use. Defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
	/// <typeparam name="TResource">The type of resource to search for.</typeparam>
	/// <returns>The first matching <typeparamref name="TResource"/>, or <c>null</c> if no matches found.</returns>
	TResource? FindByName<TResource>(ReadOnlySpan<char> name, bool allowPartialMatch = DefaultAllowPartialMatch, StringComparison comparisonType = DefaultComparisonType) where TResource : struct, IResource {
		return ForType<TResource>().FindByName(name, allowPartialMatch, comparisonType);
	}
	// TODO xmldoc that dest can be smaller than the result set, that's allowed - this method returns the number of matches total
	int FindByName<TResource>(Span<TResource> dest, ReadOnlySpan<char> name, bool allowPartialMatch = DefaultAllowPartialMatch, StringComparison comparisonType = DefaultComparisonType) where TResource : struct, IResource {
		return ForType<TResource>().FindByName(dest, name, allowPartialMatch, comparisonType);
	}
	IResourceDirectory<TResource> ForType<TResource>() where TResource : struct, IResource;
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