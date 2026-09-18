// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Builder interface that allows you to create <see cref="PointLight"/>s, <see cref="SpotLight"/>s and <see cref="DirectionalLight"/>s.
/// </summary>
public interface ILightBuilder {
	/// <summary>
	/// Creates a new <see cref="PointLight"/>: a light radiating outward in every direction from a single point, like a bare bulb.
	/// </summary>
	/// <param name="position">Where the new light should be. If <see langword="null"/>, the default (<see cref="PointLightCreationConfig.DefaultInitialPosition"/>) is used.</param>
	/// <param name="color">The colour of light the new light should emit. If <see langword="null"/>, the default (<see cref="PointLightCreationConfig.DefaultInitialColor"/>) is used.</param>
	/// <param name="brightness">How much light the new light should emit, where <c>1f</c> is its default strength. If <see langword="null"/>, the default (<see cref="PointLightCreationConfig.DefaultInitialBrightness"/>) is used.</param>
	/// <param name="maxIlluminationRadius">How far the new light should reach, in metres. If <see langword="null"/>, the default (<see cref="PointLightCreationConfig.DefaultInitialMaxIlluminationRadius"/>) is used.</param>
	/// <param name="castsShadows">Whether objects lit by the new light should cast shadows from it. If <see langword="null"/>, the default ((<see cref="PointLightCreationConfig.DefaultCastsShadows"/>)) is used.</param>
	/// <param name="name">Optional name for the new light.</param>
	PointLight CreatePointLight(Location? position = null, ColorVect? color = null, float? brightness = null, float? maxIlluminationRadius = null, bool? castsShadows = null, ReadOnlySpan<char> name = default) {
		return CreatePointLight(new PointLightCreationConfig {
			InitialPosition = position ?? PointLightCreationConfig.DefaultInitialPosition, 
			InitialColor = color ?? PointLightCreationConfig.DefaultInitialColor,
			InitialBrightness = brightness ?? PointLightCreationConfig.DefaultInitialBrightness,
			InitialMaxIlluminationRadius = maxIlluminationRadius ?? PointLightCreationConfig.DefaultInitialMaxIlluminationRadius,
			CastsShadows = castsShadows ?? PointLightCreationConfig.DefaultCastsShadows,
			Name = name
		});
	}
	/// <summary>
	/// Creates a new <see cref="PointLight"/> according to the given <paramref name="config"/>.
	/// </summary>
	/// <param name="config">Configuration for the new light.</param>
	PointLight CreatePointLight(in PointLightCreationConfig config);

	/// <summary>
	/// Creates a new <see cref="SpotLight"/>: a light radiating from a single point but confined to a cone, like a torch or a stage spotlight.
	/// </summary>
	/// <param name="position">Where the new light should be. If <see langword="null"/>, the default (<see cref="SpotLightCreationConfig.DefaultInitialPosition"/>) is used.</param>
	/// <param name="coneDirection">Which way the new light should point. If <see langword="null"/>, the default (<see cref="SpotLightCreationConfig.DefaultInitialConeDirection"/>) is used.</param>
	/// <param name="coneAngle">How wide the new light's cone should be, measured as its full width. If <see langword="null"/>, the default (<see cref="SpotLightCreationConfig.DefaultInitialConeAngle"/>) is used.</param>
	/// <param name="intenseBeamAngle">How wide the fully-lit centre of the cone should be, measured as its full width; between this and <paramref name="coneAngle"/> the light fades out. If <see langword="null"/>, the default (<see cref="SpotLightCreationConfig.DefaultInitialIntenseBeamAngle"/>) is used.</param>
	/// <param name="color">The colour of light the new light should emit. If <see langword="null"/>, the default (<see cref="SpotLightCreationConfig.DefaultInitialColor"/>) is used.</param>
	/// <param name="brightness">How much light the new light should emit, where <c>1f</c> is its default strength. If <see langword="null"/>, the default (<see cref="SpotLightCreationConfig.DefaultInitialBrightness"/>) is used.</param>
	/// <param name="maxDistance">How far down its cone the new light should reach, in metres. If <see langword="null"/>, the default (<see cref="SpotLightCreationConfig.DefaultInitialMaxIlluminationDistance"/>) is used.</param>
	/// <param name="castsShadows">Whether objects lit by the new light should cast shadows from it. If <see langword="null"/>, the default (<see cref="SpotLightCreationConfig.DefaultCastsShadows"/>) is used.</param>
	/// <param name="highQuality">Whether the new light should be rendered at higher quality, at the cost of performance. If <see langword="null"/>, the default (<see cref="SpotLightCreationConfig.DefaultIsHighQuality"/>) is used.</param>
	/// <param name="name">Optional name for the new light.</param>
	SpotLight CreateSpotLight(Location? position = null, Direction? coneDirection = null, Angle? coneAngle = null, Angle? intenseBeamAngle = null, ColorVect? color = null, float? brightness = null, float? maxDistance = null, bool? castsShadows = null, bool? highQuality = null, ReadOnlySpan<char> name = default) {
		return CreateSpotLight(new SpotLightCreationConfig {
			InitialPosition = position ?? SpotLightCreationConfig.DefaultInitialPosition,
			InitialConeDirection = coneDirection ?? SpotLightCreationConfig.DefaultInitialConeDirection,
			InitialConeAngle = coneAngle ?? SpotLightCreationConfig.DefaultInitialConeAngle,
			InitialIntenseBeamAngle = intenseBeamAngle ?? SpotLightCreationConfig.DefaultInitialIntenseBeamAngle,
			InitialColor = color ?? SpotLightCreationConfig.DefaultInitialColor,
			InitialBrightness = brightness ?? SpotLightCreationConfig.DefaultInitialBrightness,
			InitialMaxIlluminationDistance = maxDistance ?? SpotLightCreationConfig.DefaultInitialMaxIlluminationDistance,
			IsHighQuality = highQuality ?? SpotLightCreationConfig.DefaultIsHighQuality,
			CastsShadows = castsShadows ?? SpotLightCreationConfig.DefaultCastsShadows,
			Name = name
		});
	}
	/// <summary>
	/// Creates a new <see cref="SpotLight"/> according to the given <paramref name="config"/>.
	/// </summary>
	/// <param name="config">Configuration for the new light.</param>
	SpotLight CreateSpotLight(in SpotLightCreationConfig config);

	/// <summary>
	/// Creates a new <see cref="DirectionalLight"/>: a light arriving from one direction across the whole scene, like sunlight.
	/// </summary>
	/// <param name="direction">The direction the new light's rays should travel in; for a sun overhead this points downward. If <see langword="null"/>, the default (<see cref="DirectionalLightCreationConfig.DefaultInitialDirection"/>) is used.</param>
	/// <param name="color">The colour of light the new light should emit. If <see langword="null"/>, the default (<see cref="DirectionalLightCreationConfig.DefaultInitialColor"/>) is used.</param>
	/// <param name="brightness">How much light the new light should emit, where <c>1f</c> is its default strength. If <see langword="null"/>, the default (<see cref="DirectionalLightCreationConfig.DefaultInitialBrightness"/>) is used.</param>
	/// <param name="castsShadows">Whether objects lit by the new light should cast shadows from it. If <see langword="null"/>, the default (<see cref="DirectionalLightCreationConfig.DefaultCastsShadows"/>) is used.</param>
	/// <param name="showSunDisc">Whether the new light should draw a visible disc in the sky where it comes from. If <see langword="null"/>, the default (<see cref="DirectionalLightCreationConfig.DefaultShowSunDisc"/>) is used.</param>
	/// <param name="name">Optional name for the new light.</param>
	DirectionalLight CreateDirectionalLight(Direction? direction = null, ColorVect? color = null, float? brightness = null, bool? castsShadows = null, bool? showSunDisc = null, ReadOnlySpan<char> name = default) {
		return CreateDirectionalLight(new DirectionalLightCreationConfig {
			InitialDirection = direction ?? DirectionalLightCreationConfig.DefaultInitialDirection,
			InitialColor = color ?? DirectionalLightCreationConfig.DefaultInitialColor,
			InitialBrightness = brightness ?? DirectionalLightCreationConfig.DefaultInitialBrightness,
			ShowSunDisc = showSunDisc ?? DirectionalLightCreationConfig.DefaultShowSunDisc,
			CastsShadows = castsShadows ?? DirectionalLightCreationConfig.DefaultCastsShadows,
			Name = name
		});
	}
	/// <summary>
	/// Creates a new <see cref="DirectionalLight"/> according to the given <paramref name="config"/>.
	/// </summary>
	/// <param name="config">Configuration for the new light.</param>
	DirectionalLight CreateDirectionalLight(in DirectionalLightCreationConfig config);
}