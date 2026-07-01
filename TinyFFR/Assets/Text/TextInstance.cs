// Created on 2026-07-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

public readonly struct TextInstance : IDisposable, IStringSpanNameEnabled, IEquatable<TextInstance> {
	public Font Font { get; }
	public ModelInstance UnderlyingModelInstance { get; }

	public TextInstance(Font font, ModelInstance underlyingModelInstance) {
		Font = font;
		UnderlyingModelInstance = underlyingModelInstance;
	}
	
	public void SetText(ReadOnlySpan<char> text, FontPen pen) {
		UnderlyingModelInstance.SetMaterial(pen.GetPenMaterial());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingModelInstance.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingModelInstance.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingModelInstance.CopyName(destinationBuffer);
	
	public void SetTransform(Location position, XYPair<float> size, Direction facingDirection, Direction? uprightDirection = null, Orientation2D positionAnchor = Orientation2D.None) {
		UnderlyingModelInstance.SetTransform(); // TODO maybe this needs to happen as part of SetText
	}

	public void Dispose() {
		// TODO dispose text model?
		// TODO any maybe let's stop exposing the underlying model instance directly here and on the other types and just expose the properties we want to expose safely
		// TODO including the implicit conversion -- we want to update scene.Add anyway. We can add a static Coerce or something for users that really really need it
	}

	public override string ToString() => $"Text {UnderlyingModelInstance}";
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator ModelInstance(TextInstance operand) => operand.UnderlyingModelInstance;

	#region Equality
	public bool Equals(TextInstance other) => UnderlyingModelInstance.Equals(other.UnderlyingModelInstance);
	public override bool Equals(object? obj) => obj is TextInstance other && Equals(other);
	public override int GetHashCode() => UnderlyingModelInstance.GetHashCode();
	public static bool operator ==(TextInstance left, TextInstance right) => left.Equals(right);
	public static bool operator !=(TextInstance left, TextInstance right) => !left.Equals(right);
	#endregion
}