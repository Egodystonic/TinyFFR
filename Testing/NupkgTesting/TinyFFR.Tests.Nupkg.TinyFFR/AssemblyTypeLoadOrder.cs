// Created on 2026-10-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Runtime.CompilerServices;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Testing.Nupkg;

#pragma warning disable CS0169
sealed class AssemblyTypeLoadOrderAnchor {
	ApplicationLoop _applicationLoop;
	BackdropTexture _backdropTexture;
	Camera _camera;
	DirectionalLight _directionalLight;
	DynamicVertexBuffer _dynamicVertexBuffer;
	Display _display;
	Font _font;
	FontPen _fontPen;
	FontString _fontString;
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

sealed class AssemblyTypeLoadOrderAnchorStage2 {
	CanvasScene _canvasScene;
	QuadInstance _quadInstance;
	QuadMesh _quadMesh;
	TextInstance _textInstance;
}

sealed class AssemblyTypeLoadOrderAnchorStage3 {
	CameraLockedQuadInstance _cameraLockedQuadInstance;
	CameraLockedTextInstance _cameraLockedTextInstance;
	CanvasImage _canvasImage;
	CanvasText _canvasText;
}
#pragma warning restore CS0169

static class NupkgTypeLoadOrder {
	[ModuleInitializer]
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void ForceSafeTypeLoadOrder() {
		_ = new AssemblyTypeLoadOrderAnchor();
		_ = new AssemblyTypeLoadOrderAnchorStage2();
		_ = new AssemblyTypeLoadOrderAnchorStage3();
	}
}
