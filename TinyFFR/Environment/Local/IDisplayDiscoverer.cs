// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment.Local;

/// <summary>
/// Discovery interface that lets you enumerate all of the <see cref="Display"/>s connected to this device.
/// </summary>
public interface IDisplayDiscoverer {
	ReadOnlySpan<Display> All { get; }
	Display? Primary { get; }

	bool AtLeastOneDisplayConnected => All.Length > 0;

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