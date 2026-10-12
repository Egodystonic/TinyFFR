using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.World;

namespace SceneProgrammer;

enum SceneLightKind {
	Point,
	Spot
}

sealed partial class SceneLight : ObservableObject {
	readonly PointLight? _pointLight;
	readonly SpotLight? _spotLight;

	public string Name { get; }
	public SceneLightKind Kind { get; }
	public bool IsSpotLight => Kind == SceneLightKind.Spot;

	[ObservableProperty] public partial decimal PositionX { get; set; }
	[ObservableProperty] public partial decimal PositionY { get; set; }
	[ObservableProperty] public partial decimal PositionZ { get; set; }
	[ObservableProperty] public partial Color Color { get; set; } = Colors.White;
	[ObservableProperty] public partial PointLightBrightnessPreset PointBrightnessPreset { get; set; } = PointLightBrightnessPreset.BulbTypical;
	[ObservableProperty] public partial SpotLightBrightnessPreset SpotBrightnessPreset { get; set; } = SpotLightBrightnessPreset.DeskLamp;
	[ObservableProperty] public partial double BrightnessMultiplier { get; set; } = 1d;
	[ObservableProperty] public partial bool CastsShadows { get; set; } = true;
	[ObservableProperty] public partial decimal Range { get; set; } = 15m;
	[ObservableProperty] public partial decimal ConeAngleDegrees { get; set; } = 40m;
	[ObservableProperty] public partial decimal IntenseBeamAngleDegrees { get; set; } = 25m;

	public Location Position {
		get => new((float) PositionX, (float) PositionY, (float) PositionZ);
		set {
			PositionX = Math.Round((decimal) value.X, 3);
			PositionY = Math.Round((decimal) value.Y, 3);
			PositionZ = Math.Round((decimal) value.Z, 3);
		}
	}

	public Direction ConeDirection => _spotLight?.ConeDirection ?? Direction.Down;

	public SceneLight(string name, PointLight light, Location position) {
		Name = name;
		Kind = SceneLightKind.Point;
		_pointLight = light;
		Position = position;
		Apply();
	}

	public SceneLight(string name, SpotLight light, Location position) {
		Name = name;
		Kind = SceneLightKind.Spot;
		_spotLight = light;
		Position = position;
		Apply();
	}

	public void AimAlong(Direction direction) {
		if (_spotLight is not { } spotLight) return;
		spotLight.ConeDirection = direction;
		OnPropertyChanged(nameof(ConeDirection));
	}

	public void AddTo(Scene scene) {
		if (_pointLight is { } pointLight) scene.Add(pointLight);
		if (_spotLight is { } spotLight) scene.Add(spotLight);
	}

	public void RemoveFrom(Scene scene) {
		if (_pointLight is { } pointLight) scene.Remove(pointLight);
		if (_spotLight is { } spotLight) scene.Remove(spotLight);
	}

	public void Dispose() {
		_pointLight?.Dispose();
		_spotLight?.Dispose();
	}

	public ColorVect DisplayColor => new(Color.R / 255f, Color.G / 255f, Color.B / 255f);
	public ColorVect LinearColor => ColorVect.SrgbToLinear(DisplayColor);

	partial void OnPositionXChanged(decimal value) => Apply();
	partial void OnPositionYChanged(decimal value) => Apply();
	partial void OnPositionZChanged(decimal value) => Apply();
	partial void OnColorChanged(Color value) => Apply();
	partial void OnPointBrightnessPresetChanged(PointLightBrightnessPreset value) => Apply();
	partial void OnSpotBrightnessPresetChanged(SpotLightBrightnessPreset value) => Apply();
	partial void OnBrightnessMultiplierChanged(double value) => Apply();
	partial void OnCastsShadowsChanged(bool value) => Apply();
	partial void OnRangeChanged(decimal value) => Apply();
	partial void OnConeAngleDegreesChanged(decimal value) => Apply();
	partial void OnIntenseBeamAngleDegreesChanged(decimal value) => Apply();

	void Apply() {
		var multiplier = (float) Math.Max(BrightnessMultiplier, 0d);
		var range = Math.Max((float) Range, 0.1f);
		if (_pointLight is { } pointLight) {
			pointLight.Position = Position;
			pointLight.Color = LinearColor;
			pointLight.Brightness = PointBrightnessPreset.ToBrightnessValue() * multiplier;
			pointLight.CastsShadows = CastsShadows;
			pointLight.MaxIlluminationRadius = range;
		}
		if (_spotLight is { } spotLight) {
			spotLight.Position = Position;
			spotLight.Color = LinearColor;
			spotLight.Brightness = SpotBrightnessPreset.ToBrightnessValue() * multiplier;
			spotLight.CastsShadows = CastsShadows;
			spotLight.MaxIlluminationDistance = range;
			spotLight.ConeAngle = Angle.FromDegrees((float) ConeAngleDegrees);
			spotLight.IntenseBeamAngle = Angle.FromDegrees((float) Math.Min(IntenseBeamAngleDegrees, ConeAngleDegrees));
		}
	}

	public override string ToString() => Name;
}
