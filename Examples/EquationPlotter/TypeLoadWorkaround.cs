using System.Runtime.CompilerServices;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

// Note: The reason for this file is as a workaround to a known .NET 9/10 bug on Linux. See https://tinyffr.dev/tutorials/adding_a_2d_overlay/ for more information.

#pragma warning disable CS0169
sealed class TypeLoadWorkaroundStage1 {
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

sealed class TypeLoadWorkaroundStage2 {
	CanvasScene _canvasScene;
	ModelBundle _modelBundle;
	QuadInstance _quadInstance;
	QuadMesh _quadMesh;
	TextInstance _textInstance;
}

sealed class TypeLoadWorkaroundStage3 {
	CameraLockedQuadInstance _cameraLockedQuadInstance;
	CameraLockedTextInstance _cameraLockedTextInstance;
	CanvasImage _canvasImage;
	CanvasText _canvasText;
}
#pragma warning restore CS0169

static class TypeLoadWorkaround {
	[ModuleInitializer]
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void ForceSafeTypeLoadOrder() {
		_ = new TypeLoadWorkaroundStage1();
		_ = new TypeLoadWorkaroundStage2();
		_ = new TypeLoadWorkaroundStage3();
	}
}
