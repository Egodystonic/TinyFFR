using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using static NupkgTesting.AssetTestConstants;

using var factory = new LocalTinyFfrFactory();

if (new TextureReadMetadata(1024, 1024) != factory.AssetLoader.ReadTextureMetadata(AlbedoFile)) throw new InvalidOperationException("Test fail");
if (new TextureReadMetadata(1024, 1024) != factory.AssetLoader.ReadTextureMetadata(NormalFile)) throw new InvalidOperationException("Test fail");
if (new TextureReadMetadata(1024, 1024) != factory.AssetLoader.ReadTextureMetadata(SpecularFile)) throw new InvalidOperationException("Test fail");

var texBuffer = (new TexelRgb24[1024 * 1024]).AsSpan();
factory.AssetLoader.ReadTexture(AlbedoFile, texBuffer);
AssertTexelSamples(texBuffer, _expectedSampledAlbedoPixelValues);
factory.AssetLoader.ReadTexture(NormalFile, texBuffer);
AssertTexelSamples(texBuffer, _expectedSampledNormalPixelValues);
factory.AssetLoader.ReadTexture(SpecularFile, texBuffer);
AssertTexelSamples(texBuffer, _expectedSampledSpecularPixelValues);

if (new MeshReadMetadata(662, 480) != factory.AssetLoader.ReadMeshMetadata(MeshFile)) throw new InvalidOperationException("Test fail");
var meshVertexBuffer = new MeshVertex[662];
var meshTriangleBuffer = new VertexTriangle[480];
factory.AssetLoader.ReadMesh(MeshFile, meshVertexBuffer, meshTriangleBuffer);
var minLoc = new Location(Single.MaxValue, Single.MaxValue, Single.MaxValue);
var maxLoc = new Location(Single.MinValue, Single.MinValue, Single.MinValue);
foreach (var v in meshVertexBuffer) {
	var loc = v.Location;
	minLoc = new(Single.Min(minLoc.X, loc.X), Single.Min(minLoc.Y, loc.Y), Single.Min(minLoc.Z, loc.Z));
	maxLoc = new(Single.Max(maxLoc.X, loc.X), Single.Max(maxLoc.Y, loc.Y), Single.Max(maxLoc.Z, loc.Z));
}
var calculatedOrigin = ((minLoc.AsVect() + maxLoc.AsVect()) * 0.5f).AsLocation();
if (new Location(0f, 11.344924f, 1.9073486E-06f) != calculatedOrigin) throw new InvalidOperationException("Test fail");

var display = factory.DisplayDiscoverer.Recommended!.Value;
using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Asset Import Test");
window.SetIcon(LogoFile);
using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
using var albedo = factory.AssetLoader.LoadTexture(AlbedoFile);
using var normal = factory.AssetLoader.LoadTexture(NormalFile);
using var orm = factory.AssetLoader.LoadAndCombineOrmTextures(roughnessMapFilePath: SpecularFile, metallicMapFilePath: SpecularFile, config: new() { InvertYGreenChannel = true });
using var mat = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(albedo, normal, orm);
using var mesh = factory.AssetLoader.LoadMesh(MeshFile, new MeshCreationConfig { LinearRescalingFactor = 0.03f, OriginTranslation = calculatedOrigin.AsVect() });
using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, initialPosition: camera.Position + Direction.Forward * 1.3f);
using var cubemap = factory.AssetLoader.LoadEnvironmentCubemap(SkyboxFile);
using var scene = factory.SceneBuilder.CreateScene();
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

scene.Add(instance);
scene.SetBackdrop(cubemap, 0f);

var instanceToCameraVect = instance.Position >> camera.Position;

using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < TimeSpan.FromSeconds(8.9d)) {
	_ = loop.IterateOnce();
	renderer.Render();

	instanceToCameraVect = instanceToCameraVect.RotatedBy(2.3f % Direction.Up);
	camera.Position = instance.Position + instanceToCameraVect + (Direction.Up * MathF.Sin((float) loop.TotalIteratedTime.TotalSeconds * 1.67f));
	camera.ViewDirection = (camera.Position >> instance.Position).Direction;
	var cbBrightness = (float) Math.Floor(loop.TotalIteratedTime.TotalSeconds) * 0.25f;
	scene.SetBackdrop(cubemap, cbBrightness);
	window.SetTitle("Backdrop brightness level " + PercentageUtils.ConvertFractionToPercentageString(cbBrightness));
}