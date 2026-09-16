// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment.Local;

/// <summary>
/// Discovery interface that lets you enumerate all of the <see cref="Display"/>s connected to this device.
/// </summary>
public interface IDisplayDiscoverer {
	/// <summary>
	/// Every display currently connected to this device.
	/// </summary>
	ReadOnlySpan<Display> All { get; }
	/// <summary>
	/// The machine's primary display (i.e. the one the operating system treats as the user's main screen), or <see langword="null"/> if no display is connected.
	/// </summary>
	/// <remarks>
	/// This is usually the right display to open your first window on, as it is where the user expects new windows to appear.
	/// </remarks>
	Display? Primary { get; }

	/// <summary>
	/// Whether this device has at least one display connected. Where this is <see langword="false"/>, no window can be created.
	/// </summary>
	bool AtLeastOneDisplayConnected => All.Length > 0;

	/// <summary>
	/// The connected display whose maximum resolution has the most pixels, or <see langword="null"/> if no display is connected.
	/// </summary>
	/// <remarks>
	/// Ties are broken by refresh rate; and after that <see cref="Primary"/> is preferred over non-primary.
	/// Note that displays are compared by the highest resolution each one <i>supports</i>, not by the resolution each is currently set to.
	/// </remarks>
	Display? HighestResolution {
		get {
			if (Primary == null) return null;

			var result = Primary.Value;
			var resultMode = result.HighestSupportedResolutionMode;

			for (var i = 0; i < All.Length; ++i) {
				var thisMode = All[i].HighestSupportedResolutionMode;
				if (thisMode.Resolution.Area > resultMode.Resolution.Area || (thisMode.Resolution.Area == resultMode.Resolution.Area && thisMode.RefreshRateHz > resultMode.RefreshRateHz)) {
					result = All[i];
					resultMode = thisMode;
				}
			}

			return result;
		}
	}
	/// <summary>
	/// The connected display whose maximum refresh rate is fastest, or <see langword="null"/> if no display is connected.
	/// </summary>
	/// <remarks>
	/// Ties are broken by pixel count; and after that <see cref="Primary"/> is preferred over non-primary.
	/// Note that a display commonly only reaches its fastest refresh rate at a reduced resolution, so this is not necessarily the same display as
	/// <see cref="HighestResolution"/>, nor will it necessarily run at its full resolution whilst doing so.
	/// </remarks>
	Display? HighestRefreshRate {
		get {
			if (Primary == null) return null;

			var result = Primary.Value;
			var resultMode = result.HighestSupportedRefreshRateMode;

			for (var i = 0; i < All.Length; ++i) {
				var thisMode = All[i].HighestSupportedRefreshRateMode;
				if (thisMode.RefreshRateHz > resultMode.RefreshRateHz || (thisMode.RefreshRateHz == resultMode.RefreshRateHz && thisMode.Resolution.Area > resultMode.Resolution.Area)) {
					result = All[i];
					resultMode = thisMode;
				}
			}

			return result;
		}
	}
}