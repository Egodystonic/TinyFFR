using System;
using System.Reflection.Metadata;

namespace Egodystonic.TinyFFR;

/// <summary>
/// A static class holding miscellaneous extension methods that don't have a more specific home elsewhere.
/// </summary>
public static class MiscExtensions {
	/// <summary>
	/// Converts this <see cref="TimeSpan"/> to a typical game-engine/render-loop "deltaTime"/"dT" value;
	/// that is a <see cref="float"/> representing the number of seconds elapsed.
	/// </summary>
	/// <param name="this">The <see cref="TimeSpan"/> representing a frame timing.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float AsDeltaTime(this TimeSpan @this) => (float) @this.TotalSeconds;
	
	internal static string GetAllMessages(this Exception @this) {
		var result = @this.Message;
		var inner = @this.InnerException;
		while (inner != null) {
			result += " | " + inner.Message;
			inner = inner.InnerException;
		}
		return result;
	}
}