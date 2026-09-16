// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR.Environment.Input;

/// <summary>
/// Representation of the current position of one of the analog triggers on a gamepad.
/// </summary>
public readonly struct GameControllerTriggerPosition : IEquatable<GameControllerTriggerPosition> {
	/// <summary>
	/// The default "deadzone" size used by <see cref="GetDisplacementWithDeadzone"/>: a displacement at or below this value is treated as the trigger not being pulled at all.
	/// </summary>
	/// <remarks>
	/// Triggers do not always report exactly zero when fully released, and that resting value drifts as a controller ages. A deadzone is the threshold below which
	/// such readings are discarded. This value sits just above <see cref="AnalogDisplacementLevel.Slight"/>, which suits most controllers.
	/// </remarks>
	public const float RecommendedDeadzoneSize = ((float) AnalogDisplacementLevel.Slight / Int16.MaxValue) + 1E-5f; // Just slightly over the raw trigger level for 'slight'
	/// <summary>
	/// A trigger position representing the trigger being fully released.
	/// </summary>
	public static readonly GameControllerTriggerPosition Zero = new(0);
	/// <summary>
	/// A trigger position representing the trigger being pulled as far as it will go.
	/// </summary>
	public static readonly GameControllerTriggerPosition Max = new(Int16.MaxValue);

	internal short DisplacementRaw { get; init; }

	// Make sure displacement can never be negative
	/// <summary>
	/// How far the trigger is currently pulled, in the range <c>0f &lt;= n &lt;= 1f</c>, where <c>1f</c> indicates it is pulled as far as it will go.
	/// </summary>
	/// <remarks>
	/// This value does not have any deadzone applied to it; use <see cref="GetDisplacementWithDeadzone"/> if you want a value that ignores the small residual
	/// displacement some triggers report when released (see <see cref="RecommendedDeadzoneSize"/>).
	/// </remarks>
	public float Displacement => Int16.Max(DisplacementRaw, 0) / (float) Int16.MaxValue;

	/// <summary>
	/// A coarse stratification of <see cref="Displacement"/>, useful when you only care roughly how hard the trigger is being pulled.
	/// </summary>
	public AnalogDisplacementLevel DisplacementLevel => AnalogDisplacementLevelExtensions.FromRawDisplacementMagnitude(Int16.Max(DisplacementRaw, 0));

	/// <summary>
	/// Returns <see cref="Displacement"/> with <paramref name="deadzoneSize"/> applied.
	/// </summary>
	/// <remarks>
	/// The result is re-scaled after the deadzone is subtracted, so pulling the trigger fully still reports <c>1f</c> rather than <c>1f</c> minus the deadzone
	/// and displacement just outside the deadzone reports just above <c>0f</c>.
	/// Displacement within the deadzone reports <c>0f</c>.
	/// </remarks>
	/// <param name="deadzoneSize">The deadzone to apply. Defaults to <see cref="RecommendedDeadzoneSize"/>.</param>
	public float GetDisplacementWithDeadzone(float deadzoneSize = RecommendedDeadzoneSize) {
		var displacement = Displacement;
		var displacementLessDeadzone = displacement - deadzoneSize;
		if (Math.Sign(displacement) != Math.Sign(displacementLessDeadzone)) return 0f;

		return displacementLessDeadzone / (1f - deadzoneSize);
	}

	/// <summary>
	/// Constructs a new <see cref="GameControllerTriggerPosition"/> from a raw hardware displacement value.
	/// </summary>
	/// <remarks>
	/// You will not usually need to construct one of these yourself; obtain the current position of a real trigger from
	/// <see cref="ILatestGameControllerInputRetriever.LeftTriggerPosition"/>/<see cref="ILatestGameControllerInputRetriever.RightTriggerPosition"/> instead.
	/// </remarks>
	/// <param name="displacementRaw">The raw displacement, where <c>0</c> is fully released and <see cref="Int16.MaxValue"/> is fully pulled. Negative values are treated as fully released.</param>
	public GameControllerTriggerPosition(short displacementRaw) => DisplacementRaw = displacementRaw;

#pragma warning disable CA1024 // "Use properties" -  Why is this a method and we don't just make DisplacementRaw public? Because
	//	a) It makes it slightly less discoverable than "Displacement" (which is good, we want people to use that property instead) and
	//	b) It's also the choice we made for raw values in the stick position struct
	/// <summary>
	/// Returns the raw displacement value as reported by the hardware, without any deadzone or normalization applied.
	/// </summary>
	/// <remarks>
	/// Most code should prefer <see cref="Displacement"/>; this raw value is exposed for callers who need to avoid the cost of normalization or who want to
	/// implement their own handling of the trigger's input from scratch. <c>0</c> indicates fully released and <see cref="Int16.MaxValue"/> indicates fully pulled.
	/// </remarks>
	public short GetRawDisplacementValue() => DisplacementRaw;
#pragma warning restore CA1024

	/// <inheritdoc/>
	public override string ToString() {
		return $"{(DisplacementLevel == AnalogDisplacementLevel.None ? "No" : DisplacementLevel.ToString())} displacement ({PercentageUtils.ConvertFractionToPercentageString(Displacement, "N0", CultureInfo.CurrentCulture)})";
	}

	/// <inheritdoc/>
	public bool Equals(GameControllerTriggerPosition other) => DisplacementRaw == other.DisplacementRaw;
	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj is GameControllerTriggerPosition other && Equals(other);
	/// <inheritdoc/>
	public override int GetHashCode() => DisplacementRaw.GetHashCode();
	/// <summary>
	/// <see cref="Equals(GameControllerTriggerPosition)"/>
	/// </summary>
	public static bool operator ==(GameControllerTriggerPosition left, GameControllerTriggerPosition right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(GameControllerTriggerPosition)"/>
	/// </summary>
	public static bool operator !=(GameControllerTriggerPosition left, GameControllerTriggerPosition right) => !left.Equals(right);
}