// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Benchmarks.Smoke;

static class SmokeWorkload {

	public const int PolygonVertexCount = 1_024;
	public const int SphereSubdivisionLevel = 5;
	public const int MeshBuildRepeatCount = 2;
	public const int MutableMeshVertexPassCount = 260;
	public const int DynamicBufferVertexCount = 65_536;
	public const int DynamicBufferIndexCount = 98_304;
	public const int DynamicBufferRepeatCount = 30;
	public const int GridDimension = 192;
	public const int GridRepeatCount = 16;

	public const int BuiltInTextureRepeatCount = 1_200;
	public const int BuiltInTextureMetadataRepeatCount = 400_000;
	public const int CombinedTextureRepeatCount = 4_200;
	public const int TextureFileLoadRepeatCount = 3;
	public const int TextureVariantRepeatCount = 2;

	public const int SpecialMaterialRepeatCount = 6;
	public const int MaterialEffectRepeatCount = 8;

	public const int LightCount = 700;
	public const int LightPassCount = 800;
	public const int ShadowLightCount = 12;
	public const int ShadowFrameCount = 72;
	public const int BackdropRepeatCount = 5;
	public const int ModelInstanceCount = 512;
	public const int ModelInstancePassCount = 260;
	public const int QueryInstanceCount = 384;
	public const int QueryRepeatCount = 400;
	public const int QueryResultCapacity = 16;
	public const int PrimitiveRepeatCount = 1;
	public const int CameraCount = 256;
	public const int CameraStepCount = 170_000;

	public const int RenderFrameCount = 260;
	public const int ReadbackFrameCount = 18;
	public const int DynamicTextureFrameCount = 68;
	public const int QualityCycleCount = 80;
	public const int ViewportConfigCount = 120;
	public const int PickCount = 68;
	public const int CompositingFrameCount = 80;
	public const int LoopIterationCount = 34_000;

	public const int QuadCount = 96;
	public const int QuadPassCount = 120;

	public const int MeshFileLoadCount = 50;
	public const int ModelFileLoadCount = 380;
	public const int AnimationSampleCount = 300_000;
	public const int AsyncLoadRepeatCount = 8;
	public const int BakeRepeatCount = 60;
	public const int BakedLoadRepeatCount = 1_200;

	public const int ResourceGroupResourceCount = 768;
	public const int ResourceGroupPassCount = 110;
	public const int NamedResourceCount = 512;
	public const int NameLookupRepeatCount = 5_200;
	public const int ScratchCollectionElementCount = 24_576;
	public const int ScratchCollectionRepeatCount = 105;
	public const int DisplayQueryRepeatCount = 220_000;
}
