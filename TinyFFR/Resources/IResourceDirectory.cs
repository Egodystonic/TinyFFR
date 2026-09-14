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
	/// <summary>
	/// Finds every currently-live <typeparamref name="TResource"/> matching <paramref name="name"/>, copying up to <c>dest.Length</c> of them into <paramref name="dest"/>.
	/// </summary>
	/// <remarks>
	/// <paramref name="dest"/> is allowed to be smaller than the total number of matches: this method always returns the total number of matches found, even if that is greater than <c>dest.Length</c> (in which case only the first <c>dest.Length</c> matches are actually copied into <paramref name="dest"/>).
	/// </remarks>
	/// <param name="dest">The buffer to copy matching resources into.</param>
	/// <param name="name">The string to search for.</param>
	/// <param name="allowPartialMatch">If <c>true</c>, the search string only needs to match part of a resource's name for it to be returned;
	/// if <c>false</c> the entire string must match exactly. Defaults to <c>false</c>.</param>
	/// <param name="comparisonType">The type of string comparison to use. Defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</param>
	/// <typeparam name="TResource">The type of resource to search for.</typeparam>
	/// <returns>The total number of matches found (which may be greater than the number actually copied into <paramref name="dest"/>).</returns>
	int FindByName<TResource>(Span<TResource> dest, ReadOnlySpan<char> name, bool allowPartialMatch = DefaultAllowPartialMatch, StringComparison comparisonType = DefaultComparisonType) where TResource : struct, IResource {
		return ForType<TResource>().FindByName(dest, name, allowPartialMatch, comparisonType);
	}
	/// <summary>
	/// Returns the <see cref="IResourceDirectory{TResource}"/> specifically for resources of type <typeparamref name="TResource"/>.
	/// </summary>
	/// <typeparam name="TResource">The type of resource to get a directory for.</typeparam>
	IResourceDirectory<TResource> ForType<TResource>() where TResource : struct, IResource;
}
/// <summary>
/// The strongly-typed counterpart to <see cref="IResourceDirectory"/>, providing a directory of all active, non-disposed resources of type <typeparamref name="TResource"/> specifically.
/// </summary>
/// <typeparam name="TResource">The type of resource this directory tracks.</typeparam>
public interface IResourceDirectory<TResource> where TResource : struct, IResource {
	protected const bool DefaultAllowPartialMatch = IResourceDirectory.DefaultAllowPartialMatch;
	protected const StringComparison DefaultComparisonType = IResourceDirectory.DefaultComparisonType;
	
	/// <summary>
	/// An <see cref="IndirectEnumerable{TIn,TOut}"/> of all currently-live resources of type <typeparamref name="TResource"/>.
	/// </summary>
	IndirectEnumerable<object, TResource> AllActiveInstances { get; }
	protected bool ResourceNameMatchIsMatching(TResource resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType);

	/// <inheritdoc cref="IResourceDirectory.FindByName{TResource}(ReadOnlySpan{char},bool,StringComparison)"/>
	TResource? FindByName(ReadOnlySpan<char> name, bool allowPartialMatch = DefaultAllowPartialMatch, StringComparison comparisonType = DefaultComparisonType) {
		Unsafe.SkipInit(out TResource result);
		if (FindByName(new Span<TResource>(ref result), name, allowPartialMatch, comparisonType) == 1) return result;
		else return null;
	}
	/// <inheritdoc cref="IResourceDirectory.FindByName{TResource}(Span{TResource},ReadOnlySpan{char},bool,StringComparison)"/>
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