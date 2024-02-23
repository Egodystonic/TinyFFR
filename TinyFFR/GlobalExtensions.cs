// Created on 2024-02-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

static class GlobalExtensions {
	public static string ToStringMs(this TimeSpan @this) => $"{@this.TotalMilliseconds:N2}ms";
}