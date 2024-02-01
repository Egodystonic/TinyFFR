// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(int))]
public readonly struct GameControllerId : IEquatable<GameControllerId> {
	readonly int _id;

	public GameControllerId(int id) => _id = id;

	public bool Equals(GameControllerId other) => _id == other._id;
	public override bool Equals(object? obj) => obj is GameControllerId other && Equals(other);
	public override int GetHashCode() => _id;
	public static bool operator ==(GameControllerId left, GameControllerId right) => left.Equals(right);
	public static bool operator !=(GameControllerId left, GameControllerId right) => !left.Equals(right);
}