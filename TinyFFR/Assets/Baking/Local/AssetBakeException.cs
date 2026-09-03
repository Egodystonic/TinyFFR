// Created on 2026-08-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Runtime.Serialization;

namespace Egodystonic.TinyFFR.Assets.Baking;

public class AssetBakeException : Exception {
	public AssetBakeException() { }
	public AssetBakeException(string? message) : base(message) { }
	public AssetBakeException(string? message, Exception? innerException) : base(message, innerException) { }
}