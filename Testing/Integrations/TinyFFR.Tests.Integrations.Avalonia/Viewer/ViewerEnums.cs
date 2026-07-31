// Created on 2026-07-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace TinyFFR.Tests.Integrations.Avalonia.Viewer;

// DefaultMaterialShadingStyle has no 'textured' member because restoring the loaded materials is not a shading style
public enum ViewerShadingStyle {
	Textured,
	Plain3D,
	Wireframe
}

public enum ViewerBackdropMode {
	Texture,
	Color,
	None
}
