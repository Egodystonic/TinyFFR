using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.World;

namespace SceneProgrammer;

enum BackdropStyle {
	Clouds,
	Starfield,
	SkyColor,
	CustomHdr,
	None
}

enum LightingPreset {
	Midday,
	Overcast,
	SunriseSunset,
	Twilight,
	Indoors
}

sealed class EnvironmentSettings {
	static readonly ColorVect SkyColor = new(0.3f, 0.55f, 0.95f);

	public BackdropStyle Backdrop { get; set; } = BackdropStyle.Clouds;
	public LightingPreset Lighting { get; set; } = LightingPreset.Midday;
	public float SunAzimuthDegrees { get; set; } = 35f;
	public float SunElevationDegrees { get; set; } = 50f;
	public bool SunCastsShadows { get; set; } = true;
	public bool ShowGround { get; set; } = true;
	public float SunBrightnessMultiplier { get; set; } = 1f;
	public ColorVect SunColor { get; set; } = ColorVect.WhiteOpaque;
	public BackdropTexture? CustomBackdrop { get; set; }
	public SceneBackdropBrightnessPreset BackdropBrightness { get; set; } = SceneBackdropBrightnessPreset.Midday;
	public bool UseBackdropBrightnessSlider { get; set; }
	public float BackdropBrightnessPercent { get; set; } = 100f;
	public float BackdropHorizontalRotationDegrees { get; set; }
	public float BackdropVerticalRotationDegrees { get; set; }
	public FogDescriptor? Fog { get; set; }

	public CameraExposurePreset Exposure => Lighting switch {
		LightingPreset.Overcast => CameraExposurePreset.OutsideOvercast,
		LightingPreset.SunriseSunset => CameraExposurePreset.OutsideSunriseSunset,
		LightingPreset.Twilight => CameraExposurePreset.OutsideTwilight,
		LightingPreset.Indoors => CameraExposurePreset.InsideBrightLighting,
		_ => CameraExposurePreset.OutsideMidday
	};

	public static SceneBackdropBrightnessPreset GetMatchingBackdropBrightness(LightingPreset lighting) => lighting switch {
		LightingPreset.Overcast => SceneBackdropBrightnessPreset.Overcast,
		LightingPreset.SunriseSunset => SceneBackdropBrightnessPreset.SunriseSunset,
		LightingPreset.Twilight => SceneBackdropBrightnessPreset.Twilight,
		LightingPreset.Indoors => SceneBackdropBrightnessPreset.InsideBrightLighting,
		_ => SceneBackdropBrightnessPreset.Midday
	};

	DirectionalLightBrightnessPreset SunBrightness => Lighting switch {
		LightingPreset.Overcast => DirectionalLightBrightnessPreset.Overcast,
		LightingPreset.SunriseSunset => DirectionalLightBrightnessPreset.SunriseSunset,
		LightingPreset.Twilight => DirectionalLightBrightnessPreset.Twilight,
		LightingPreset.Indoors => DirectionalLightBrightnessPreset.InsideBrightLighting,
		_ => DirectionalLightBrightnessPreset.Midday
	};

	public Direction SunDirection {
		get {
			var azimuth = SunAzimuthDegrees * MathF.PI / 180f;
			var elevation = SunElevationDegrees * MathF.PI / 180f;
			return new Direction(-MathF.Cos(elevation) * MathF.Sin(azimuth), -MathF.Sin(elevation), -MathF.Cos(elevation) * MathF.Cos(azimuth));
		}
	}

	Rotation BackdropRotation => (Angle.FromDegrees(BackdropHorizontalRotationDegrees) % Direction.Up) + (Angle.FromDegrees(BackdropVerticalRotationDegrees) % Direction.Left);

	public void ApplyTo(Scene scene, DirectionalLight sun) {
		switch (Backdrop) {
			case BackdropStyle.Clouds:
				scene.SetBackdrop(BuiltInSceneBackdrop.Clouds, BackdropBrightness, BackdropRotation);
				break;
			case BackdropStyle.Starfield:
				scene.SetBackdrop(BuiltInSceneBackdrop.Starfield, BackdropBrightness, BackdropRotation);
				break;
			case BackdropStyle.SkyColor:
				scene.SetBackdrop(SkyColor, BackdropBrightness);
				break;
			case BackdropStyle.CustomHdr when CustomBackdrop is { } custom:
				if (UseBackdropBrightnessSlider) {
					var lux = GetMatchingBackdropBrightness(Lighting).ToLux() * BackdropBrightnessPercent / 100f;
					scene.SetBackdrop(custom, custom.LuxToIntensity(lux), BackdropRotation);
				}
				else {
					scene.SetBackdrop(custom, BackdropBrightness, BackdropRotation);
				}
				break;
			default:
				scene.RemoveBackdrop();
				break;
		}

		if (Fog is { } fog) scene.AddFog(fog);
		else scene.RemoveFog();

		sun.SetBrightness(SunBrightness.ToBrightnessValue() * SunBrightnessMultiplier);
		sun.Color = SunColor;
		sun.Direction = SunDirection;
		sun.CastsShadows = SunCastsShadows;
	}
}
