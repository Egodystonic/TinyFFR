// Created on 2026-08-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Runtime.Serialization;

namespace Egodystonic.TinyFFR.Assets.Baking;

/// <summary>
/// Thrown when a resource can not be written to a baked asset file, or when a baked asset file can not be read back.
/// </summary>
public class AssetBakeException : Exception {
	/// <inheritdoc />
	public AssetBakeException() { }
	/// <inheritdoc />
	public AssetBakeException(string? message) : base(message) { }
	/// <inheritdoc />
	public AssetBakeException(string? message, Exception? innerException) : base(message, innerException) { }
}