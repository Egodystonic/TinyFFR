using System;
using System.Reflection.Metadata;

namespace Egodystonic.TinyFFR;

public static class MiscExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float AsDeltaTime(this TimeSpan @this) => (float) @this.TotalSeconds; 
}