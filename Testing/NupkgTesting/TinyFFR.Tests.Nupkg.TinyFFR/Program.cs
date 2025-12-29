using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Testing;
using static Egodystonic.TinyFFR.Testing.Nupkg.AssetTestConstants;

using var factory = new LocalTinyFfrFactory();

if (new TextureReadMetadata((1024, 1024), false) != factory.AssetLoader.ReadTextureMetadata(CommonTestAssets.FindAsset(KnownTestAsset.CrateAlbedoTex))) throw new InvalidOperationException("Test fail");
if (new TextureReadMetadata((1024, 1024), false) != factory.AssetLoader.ReadTextureMetadata(CommonTestAssets.FindAsset(KnownTestAsset.CrateNormalTex))) throw new InvalidOperationException("Test fail");
if (new TextureReadMetadata((1024, 1024), false) != factory.AssetLoader.ReadTextureMetadata(CommonTestAssets.FindAsset(KnownTestAsset.CrateSpecularTex))) throw new InvalidOperationException("Test fail");

var texBuffer = (new TexelRgb24[1024 * 1024]).AsSpan();
factory.AssetLoader.ReadTexture(CommonTestAssets.FindAsset(KnownTestAsset.CrateAlbedoTex), texBuffer);
AssertTexelSamples(texBuffer, _expectedSampledAlbedoPixelValues);
factory.AssetLoader.ReadTexture(CommonTestAssets.FindAsset(KnownTestAsset.CrateNormalTex), texBuffer);
AssertTexelSamples(texBuffer, _expectedSampledNormalPixelValues);
factory.AssetLoader.ReadTexture(CommonTestAssets.FindAsset(KnownTestAsset.CrateSpecularTex), texBuffer);
AssertTexelSamples(texBuffer, _expectedSampledSpecularPixelValues);

if (new MeshReadMetadata(662, 480) != factory.AssetLoader.ReadMeshMetadata(CommonTestAssets.FindAsset(KnownTestAsset.CrateMesh))) throw new InvalidOperationException("Test fail");
var meshVertexBuffer = new MeshVertex[662];
var meshTriangleBuffer = new VertexTriangle[480];
factory.AssetLoader.ReadMesh(CommonTestAssets.FindAsset(KnownTestAsset.CrateMesh), meshVertexBuffer, meshTriangleBuffer);
var minLoc = new Location(Single.MaxValue, Single.MaxValue, Single.MaxValue);
var maxLoc = new Location(Single.MinValue, Single.MinValue, Single.MinValue);
foreach (var v in meshVertexBuffer) {
	var loc = v.Location;
	minLoc = new(Single.Min(minLoc.X, loc.X), Single.Min(minLoc.Y, loc.Y), Single.Min(minLoc.Z, loc.Z));
	maxLoc = new(Single.Max(maxLoc.X, loc.X), Single.Max(maxLoc.Y, loc.Y), Single.Max(maxLoc.Z, loc.Z));
}
var calculatedOrigin = ((minLoc.AsVect() + maxLoc.AsVect()) * 0.5f).AsLocation();
if (new Location(0f, 11.344924f, 1.9073486E-06f) != calculatedOrigin) throw new InvalidOperationException("Test fail");

var display = factory.DisplayDiscoverer.Primary!.Value;
using var window = factory.WindowBuilder.CreateWindow(display, title: "Nupkg Test");
window.SetIcon(CommonTestAssets.FindAsset(KnownTestAsset.EgodystonicLogo));
using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
using var albedo = factory.AssetLoader.LoadColorMap(CommonTestAssets.FindAsset(KnownTestAsset.CrateAlbedoTex));
using var normal = factory.AssetLoader.LoadNormalMap(CommonTestAssets.FindAsset(KnownTestAsset.CrateNormalTex));
using var orm = factory.AssetLoader.LoadCombinedTexture(
	aFilePath: CommonTestAssets.FindAsset(KnownTestAsset.WhiteTex),
	aProcessingConfig: TextureProcessingConfig.None,
	bFilePath: CommonTestAssets.FindAsset(KnownTestAsset.CrateSpecularTex),
	bProcessingConfig: TextureProcessingConfig.None,
	cFilePath: CommonTestAssets.FindAsset(KnownTestAsset.CrateSpecularTex),
	cProcessingConfig: TextureProcessingConfig.None,
	combinationConfig: new(
		new(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
		new(TextureCombinationSourceTexture.TextureB, ColorChannel.R),
		new(TextureCombinationSourceTexture.TextureC, ColorChannel.R)
	),
	finalOutputConfig: new TextureCreationConfig { IsLinearColorspace = true, ProcessingToApply = new() { InvertYGreenChannel = true, InvertZBlueChannel = true } }
);
using var mat = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(albedo, normal, orm);
using var mesh = factory.AssetLoader.LoadMesh(CommonTestAssets.FindAsset(KnownTestAsset.CrateMesh), new MeshCreationConfig { LinearRescalingFactor = 0.03f, OriginTranslation = calculatedOrigin.AsVect() });
using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialPosition: camera.Position + Direction.Forward * 1.3f);
using var cubemap = factory.AssetLoader.LoadBackdropTexture(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));
using var scene = factory.SceneBuilder.CreateScene();
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

scene.Add(instance);
scene.SetBackdrop(cubemap, 0f);

var instanceToCameraVect = instance.Position >> camera.Position;

using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(8.9d)) {
	var deltaTime = (float) loop.IterateOnce().TotalSeconds;
	renderer.Render();

	instanceToCameraVect = instanceToCameraVect.RotatedBy(2.3f % Direction.Up);
	camera.Position = instance.Position + instanceToCameraVect + (Direction.Up * MathF.Sin((float) loop.TotalIteratedTime.TotalSeconds * 1.67f));
	camera.ViewDirection = (camera.Position >> instance.Position).Direction;
	var cbBrightness = (float) Math.Floor(loop.TotalIteratedTime.TotalSeconds) * 0.25f;
	scene.SetBackdrop(cubemap, cbBrightness);
	window.SetTitle("Backdrop brightness level " + PercentageUtils.ConvertFractionToPercentageString(cbBrightness));

	if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) instance.AdjustScaleBy(-0.1f);
}

Console.WriteLine("Test completed fine (assuming you saw the crate and the backdrop).");