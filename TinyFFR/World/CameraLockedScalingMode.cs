// Created on 2026-07-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.World;

public enum CameraLockedScalingMode {
	Standard,
	ViewportFractionalFixedWidth,
	ViewportFractionalFixedHeight,
	ViewportFractionalFixedWidthAndHeight,
	ViewportFractionalFixedWidthPlusPreservedAspectRatio,
	ViewportFractionalFixedHeightPlusPreservedAspectRatio
}
