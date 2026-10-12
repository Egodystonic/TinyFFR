using System.Collections.ObjectModel;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace SceneProgrammer;

sealed class SceneDocument : IDisposable {
	const float ImportedModelTargetSize = 2f;

	readonly ILocalTinyFfrFactory _factory;
	readonly Scene _scene;
	readonly Dictionary<string, ModelBundle> _bundlesByPath = new();
	readonly Dictionary<ModelBundle, int> _bundleUserCounts = new();
	readonly Dictionary<ModelInstance, SceneObject> _ownersByInstance = new();

	public ObservableCollection<SceneObject> Objects { get; } = new();
	public ObservableCollection<SceneLight> Lights { get; } = new();
	int _lightCounter;

	public SceneDocument(ILocalTinyFfrFactory factory, Scene scene) {
		_factory = factory;
		_scene = scene;
	}

	public async Task<SceneObject> ImportAsync(string filePath, Location groundPoint) {
		if (!_bundlesByPath.TryGetValue(filePath, out var bundle)) {
			bundle = await _factory.AssetLoader.LoadBundledAssetAsync(filePath);
			_bundlesByPath[filePath] = bundle;
		}

		var sceneObject = CreateObject(Path.GetFileNameWithoutExtension(filePath), bundle);
		var boundingBox = bundle.CalculateCombinedBoundingBox();
		var largestDimension = MathF.Max(boundingBox.Width, MathF.Max(boundingBox.Height, boundingBox.Depth));
		var scale = largestDimension is > 0.1f and < 25f ? 1f : ImportedModelTargetSize / largestDimension;
		var bottom = boundingBox.Position.Y - boundingBox.HalfHeight;
		sceneObject.Scale = Math.Round((decimal) scale, 4);
		sceneObject.Position = new Location(groundPoint.X, groundPoint.Y - bottom * scale, groundPoint.Z);
		return sceneObject;
	}

	public SceneObject Duplicate(SceneObject source) {
		var copy = CreateObject(source.Name, source.Bundle);
		copy.Scale = source.Scale;
		copy.YawDegrees = source.YawDegrees;
		copy.PitchDegrees = source.PitchDegrees;
		copy.RollDegrees = source.RollDegrees;
		copy.Position = source.Position + new Vect(source.CalculateWorldBoundingBox().Width * 1.1f, 0f, 0f);
		return copy;
	}

	public void Delete(SceneObject sceneObject) {
		Objects.Remove(sceneObject);
		foreach (var instance in sceneObject.Instances.Instances) _ownersByInstance.Remove(instance);
		_scene.Remove(sceneObject.Instances);
		sceneObject.Instances.Dispose();

		var remainingUsers = _bundleUserCounts[sceneObject.Bundle] - 1;
		_bundleUserCounts[sceneObject.Bundle] = remainingUsers;
		if (remainingUsers > 0) return;
		_bundleUserCounts.Remove(sceneObject.Bundle);
		foreach (var (path, bundle) in _bundlesByPath) {
			if (bundle != sceneObject.Bundle) continue;
			_bundlesByPath.Remove(path);
			break;
		}
		sceneObject.Bundle.Dispose();
	}

	public SceneObject? FindOwner(ModelInstance instance) => _ownersByInstance.GetValueOrDefault(instance);

	public SceneLight AddPointLight(Location position) {
		var light = new SceneLight($"Point light {++_lightCounter}", _factory.LightBuilder.CreatePointLight(castsShadows: true), position);
		light.AddTo(_scene);
		Lights.Add(light);
		return light;
	}

	public SceneLight AddSpotLight(Location position, Direction coneDirection) {
		var light = new SceneLight($"Spot light {++_lightCounter}", _factory.LightBuilder.CreateSpotLight(coneDirection: coneDirection, castsShadows: true, highQuality: true), position);
		light.AddTo(_scene);
		Lights.Add(light);
		return light;
	}

	public void DeleteLight(SceneLight light) {
		Lights.Remove(light);
		light.RemoveFrom(_scene);
		light.Dispose();
	}

	SceneObject CreateObject(string baseName, ModelBundle bundle) {
		var name = baseName;
		for (var suffix = 2; Objects.Any(o => o.Name == name); ++suffix) name = $"{baseName} ({suffix})";

		var instances = _factory.ObjectBuilder.CreateModelInstances(bundle, name: name);
		var sceneObject = new SceneObject(name, bundle, instances);
		foreach (var instance in instances.Instances) _ownersByInstance[instance] = sceneObject;
		_bundleUserCounts[bundle] = _bundleUserCounts.GetValueOrDefault(bundle) + 1;
		_scene.Add(instances);
		Objects.Add(sceneObject);
		return sceneObject;
	}

	public void Dispose() {
		while (Objects.Count > 0) Delete(Objects[^1]);
		while (Lights.Count > 0) DeleteLight(Lights[^1]);
	}
}
