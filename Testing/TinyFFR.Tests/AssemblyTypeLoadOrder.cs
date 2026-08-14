// Created on 2026-07-13 by Ben Bowen
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

namespace Egodystonic.TinyFFR;

// This type must remain first in this assembly's TypeDef table, meaning this file must sort alphabetically
// before every other .cs file in this project, and it must remain a class (loading a CLASS with resource
// struct fields safely forces those structs' types to load; a struct anchor would itself hit the bug below).
// Structs embedding a resource struct as a field (including compiler-generated closure classes capturing a
// local of resource type) trigger a CLR type-loader bug (segfault, reproduced on .NET 9.0.0 through 10.0.9)
// when loaded BEFORE the embedded resource struct's own type. NUnit test discovery reflects over all types
// in TypeDef order, so without this anchor the test host crashes or survives depending on nothing more than
// the alphabetical ordering of test file names.
// This issue is currently being tracked here: https://github.com/dotnet/runtime/issues/130661
#pragma warning disable CS0169 // Fields are never used: they exist purely to force type loads in a safe order
sealed class AssemblyTypeLoadOrderAnchor {
	ApplicationLoop _applicationLoop;
	BackdropTexture _backdropTexture;
	Camera _camera;
	DirectionalLight _directionalLight;
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

// Second stage: types that themselves embed a resource struct (the fragile shape described above) load
// safely here because the first anchor has already forced their embedded resource types to load.
sealed class AssemblyTypeLoadOrderAnchorStage2 {
	CanvasScene _canvasScene;
	FontPen _fontPen;
	FontString _fontString;
	QuadInstance _quadInstance;
	QuadMesh _quadMesh;
	TextInstance _textInstance;
}

// Third stage: types embedding the stage-2 types (one nesting level deeper).
sealed class AssemblyTypeLoadOrderAnchorStage3 {
	CameraLockedQuadInstance _cameraLockedQuadInstance;
	CameraLockedTextInstance _cameraLockedTextInstance;
	CanvasTexture _canvasTexture;
	CanvasText _canvasText;
}
#pragma warning restore CS0169
