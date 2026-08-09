// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Assets.Text;

public readonly record struct TextLayout(float? Height, Orientation2D PositionAnchor, float? Width, bool DisableAutomaticLineCountBasedHeightScaling) {
	public TextLayout(float height) : this(height, Orientation2D.None, null, false) { }
	public TextLayout(float height, Orientation2D positionAnchor) : this(height, positionAnchor, null, false) { }
}
