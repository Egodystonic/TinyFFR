// Created on 2026-02-20 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents an item that is positioned along a timeline, such as a single keyframe in an animation or camera movement.
/// </summary>
/// <remarks>
/// The precise meaning of <see cref="TimeKeySeconds"/> (for example, whether it is an absolute timestamp or a duration relative to the previous item) is defined by the implementing type.
/// </remarks>
public interface ITimeKeyedItem {
	/// <summary>
	/// The time, in seconds, associated with this item.
	/// </summary>
	public float TimeKeySeconds { get; }
}