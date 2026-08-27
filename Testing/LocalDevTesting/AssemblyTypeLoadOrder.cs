// Created on 2026-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Testing.Local;

#pragma warning disable CS0169 // Fields are never used: they exist purely to force type loads in a safe order
sealed class DevTestingTypeLoadAnchor {
	ApplicationLoop _applicationLoop;
	BackdropTexture _backdropTexture;
	Camera _camera;
	DirectionalLight _directionalLight;
	DynamicVertexBuffer _dynamicVertexBuffer;
	Display _display;
	Font _font;
	Light _light;
	Material _material;
	Mesh _mesh;
	MeshAnimation _meshAnimation;
	MeshNode _meshNode;
	Model _model;
	ModelInstance _modelInstance;
	ModelInstanceGroup _modelInstanceGroup;
	PointLight _pointLight;
	Renderer _renderer;
	RendererCompositor _rendererCompositor;
	RenderOutputBuffer _renderOutputBuffer;
	ResourceGroup _resourceGroup;
	Scene _scene;
	SpotLight _spotLight;
	Texture _texture;
	Window _window;
}

sealed class DevTestingTypeLoadAnchorStage2 {
	CanvasScene _canvasScene;
	FontPen _fontPen;
	FontString _fontString;
	QuadInstance _quadInstance;
	QuadMesh _quadMesh;
	TextInstance _textInstance;
}

sealed class DevTestingTypeLoadAnchorStage3 {
	CameraLockedQuadInstance _cameraLockedQuadInstance;
	CameraLockedTextInstance _cameraLockedTextInstance;
	CanvasTexture _canvasTexture;
	CanvasText _canvasText;
}
#pragma warning restore CS0169

static class DevTestingTypeLoadOrder {
	public static void ForceSafeTypeLoadOrder() {
		_ = new DevTestingTypeLoadAnchor();
		_ = new DevTestingTypeLoadAnchorStage2();
		_ = new DevTestingTypeLoadAnchorStage3();
	}
}
