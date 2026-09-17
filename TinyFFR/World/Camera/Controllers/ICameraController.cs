// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Drives a <see cref="World.Camera"/> according to a particular style of camera movement, such as first-person or orbital.
/// </summary>
/// <remarks>
/// <para>
/// A camera controller saves you from positioning and aiming a camera by hand. Instead of setting the camera's position and view direction yourself each frame, you
/// set higher-level values on the controller — how far it should be from its target, which way it is facing, and so on — and it works out the camera parameters that
/// follow from them.
/// </para>
/// <para>
/// Every controller follows the same rhythm: set or adjust its properties (usually in response to user input), then call <see cref="Progress(float)"/> exactly once per
/// frame. Nothing reaches the camera until <see cref="Progress(float)"/> is called, because most controllers ease towards their target values over several frames rather
/// than snapping to them (see <see cref="SmoothingStrength"/>).
/// </para>
/// </remarks>
public interface ICameraController : IDisposable {
	/// <summary>
	/// The camera this controller is attached to and moves.
	/// </summary>
	Camera Camera { get; }

	/// <summary>
	/// Resets every parameter of this controller back to its default value.
	/// </summary>
	/// <remarks>
	/// This also resets any smoothing in progress, so the camera jumps straight to the default arrangement on the next <see cref="Progress(float)"/> rather than easing
	/// towards it.
	/// </remarks>
	void ResetParametersToDefault();
	/// <summary>
	/// Advances this controller by <paramref name="deltaTime"/> and applies the result to <see cref="Camera"/>.
	/// </summary>
	/// <remarks>
	/// This must be called once per frame; it is the only thing that actually moves the camera. Setting properties or calling any of the <c>Adjust</c> methods only
	/// records where the camera should be heading — this is what takes it there, easing towards the target values according to the controller's smoothing settings.
	/// </remarks>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	void Progress(float deltaTime);
	/// <summary>
	/// Sets every one of this controller's smoothing settings at once.
	/// </summary>
	/// <param name="newSmoothingStrength">How heavily this controller should smooth its movement.</param>
	void SetGlobalSmoothing(SmoothingStrength newSmoothingStrength);
	/// <summary>
	/// Adjusts every one of this controller's parameters according to this controller's default keyboard and mouse control scheme.
	/// </summary>
	/// <remarks>
	/// This is a convenience for getting a controller usable in one line; each controller documents the scheme it applies. Where you want a different mapping, call
	/// the individual <c>Adjust...Via...</c> methods yourself instead of this.
	/// </remarks>
	/// <param name="input">The latest keyboard and mouse state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime);
	/// <summary>
	/// Adjusts every one of this controller's parameters according to this controller's default game controller scheme.
	/// </summary>
	/// <remarks>
	/// This is a convenience for getting a controller usable in one line; each controller documents the scheme it applies. Where you want a different mapping, call
	/// the individual <c>Adjust...Via...</c> methods yourself instead of this.
	/// </remarks>
	/// <param name="input">The latest game controller state to read. Must not be <see langword="null"/>.</param>
	/// <param name="deltaTime">The time elapsed since the previous frame, in seconds.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
	void AdjustAllViaDefaultControls(ILatestGameControllerInputRetriever input, float deltaTime);
}
/// <summary>
/// An <see cref="ICameraController"/> that knows its own concrete type, which is what lets a camera create one of a given type via
/// <c>CreateController&lt;TController&gt;()</c>.
/// </summary>
/// <typeparam name="TSelf">The implementing type itself.</typeparam>
public interface ICameraController<out TSelf> : ICameraController where TSelf : ICameraController<TSelf> {
	internal static abstract TSelf RentAndTetherToCamera(Camera camera);
}