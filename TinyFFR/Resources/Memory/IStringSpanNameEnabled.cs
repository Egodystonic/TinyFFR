// Created on 2024-09-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources.Memory;

/// <summary>
/// Represents a value that has a name, exposing it both as a regular <see cref="string"/> and as an allocation-free span of characters.
/// </summary>
public interface IStringSpanNameEnabled {
	/// <summary>
	/// Returns this value's name as a newly-allocated <see cref="string"/>.
	/// </summary>
	/// <remarks>
	/// Prefer <see cref="GetNameLength"/> and <see cref="CopyName"/> instead if you want to avoid allocating, for example to copy the name into a pooled or stack-allocated buffer.
	/// </remarks>
	string GetNameAsNewStringObject();
	/// <summary>
	/// Returns the length, in characters, of this value's name.
	/// </summary>
	int GetNameLength();
	/// <summary>
	/// Copies this value's name into <paramref name="destinationBuffer"/>, without allocating.
	/// </summary>
	/// <param name="destinationBuffer">The buffer to copy the name into. Must be at least <see cref="GetNameLength"/> characters long.</param>
	void CopyName(Span<char> destinationBuffer);
}