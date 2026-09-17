// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.World;

readonly record struct LightShadowFidelityData(uint MapSize, byte CascadeCount);

/// <summary>
/// Represents a light in a scene, of any kind.
/// </summary>
/// <remarks>
/// This is the surface shared by <see cref="PointLight"/>, <see cref="SpotLight"/> and <see cref="DirectionalLight"/>. Where you need to work with a light without
/// caring which kind it is, use this or <see cref="Light"/>; where you need the properties specific to one kind (a spot light's cone, for example), use that kind's
/// own type.
/// </remarks>
public interface ILight : IColoredSceneObject {
	/// <summary>
	/// Which kind of light this is.
	/// </summary>
	LightType Type { get; }
	/// <summary>
	/// The colour of the light this emits.
	/// </summary>
	/// <remarks>
	/// This tints the light itself, not the objects it falls on: a red light leaves a white surface looking red, and a surface that reflects no red at all stays
	/// dark no matter how bright the light is.
	/// </remarks>
	ColorVect Color { get; set; }
	/// <summary>
	/// How much light this emits, where <c>1f</c> is this kind of light's default strength. Negative values are treated as <c>0f</c>.
	/// </summary>
	/// <remarks>
	/// Brightness is a relative measure rather than a physical one, so that lights of different kinds can be adjusted in the same terms. Each kind of light also
	/// offers conversions to and from its real-world unit. Note that the relationship between brightness and that unit differs between the kinds of light, so
	/// consult the <c>Brightness</c> property of the specific type for the exact mapping.
	/// </remarks>
	float Brightness { get; set; }
	/// <summary>
	/// Whether objects lit by this light cast shadows from it.
	/// </summary>
	/// <remarks>
	/// Shadows are calculated separately for each light that casts them, and are one of the more expensive things a scene can ask for. It is therefore common to
	/// enable this only for the one or two lights that most define a scene's look, and leave the rest shadowless.
	/// </remarks>
	bool CastsShadows { get; set; }

	/// <summary>
	/// Adds <paramref name="adjustment"/> to this light's <see cref="Brightness"/>.
	/// </summary>
	/// <param name="adjustment">The amount to add. May be negative, to dim the light; the resulting brightness is never taken below <c>0f</c>.</param>
	void AdjustBrightnessBy(float adjustment);
	/// <summary>
	/// Multiplies this light's <see cref="Brightness"/> by <paramref name="scalar"/>.
	/// </summary>
	/// <param name="scalar">The factor to multiply this light's brightness by. Negative values result in a brightness of <c>0f</c>.</param>
	void ScaleBrightnessBy(float scalar);
	/// <summary>
	/// Returns this light as a kind-agnostic <see cref="Light"/>.
	/// </summary>
	/// <remarks>
	/// This is a reinterpretation, not a conversion: the result refers to the same underlying light, so changes made through it are visible here too, and disposing
	/// either disposes the light itself.
	/// </remarks>
	Light AsBaseLight();

	internal void SetShadowFidelity(LightShadowFidelityData fidelityArgs);
}
/// <summary>
/// An <see cref="ILight"/> that knows its own concrete type, allowing lights to be created and converted generically.
/// </summary>
/// <typeparam name="TSelf">The implementing type itself.</typeparam>
public interface ILight<TSelf> : ILight, IDisposableResource<TSelf, ILightImplProvider> where TSelf : ILight<TSelf> {
	internal static abstract TSelf FromBaseLight(Light l);
	internal static abstract LightType SelfType { get; }
}