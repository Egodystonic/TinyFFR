using System;
using System.Reflection.Metadata;

namespace Egodystonic.TinyFFR;

public static class MiscExtensions {
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