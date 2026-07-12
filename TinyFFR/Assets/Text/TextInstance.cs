// Created on 2026-07-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

public readonly struct TextInstance : IDisposable, IStringSpanNameEnabled, IEquatable<TextInstance> {
	public ModelInstance UnderlyingModelInstance { get; }
	public FontPen Pen {
		get => UnderlyingModelInstance.Implementation.GetTextInstancePen(UnderlyingModelInstance.GetHandleWithoutDisposeCheck());
		set {
			UnderlyingModelInstance.SetMaterial(value.GetPenMaterial());
			UnderlyingModelInstance.Implementation.UpdateTextInstancePen(UnderlyingModelInstance.GetHandleWithoutDisposeCheck(), value);
		}
	}
	public FontString String {
		get => UnderlyingModelInstance.Implementation.GetTextInstanceString(UnderlyingModelInstance.GetHandleWithoutDisposeCheck());
		set {
			UnderlyingModelInstance.SetMesh(value.GetStringMesh());
			UnderlyingModelInstance.Implementation.UpdateTextInstanceString(UnderlyingModelInstance.GetHandleWithoutDisposeCheck(), value);
		}
	}

	public TextInstance(ModelInstance underlyingModelInstance, FontPen pen, FontString @string) {
		UnderlyingModelInstance = underlyingModelInstance;
		UnderlyingModelInstance.Implementation.SetTextInstanceInitialPenAndString(UnderlyingModelInstance.GetHandleWithoutDisposeCheck(), pen, @string);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingModelInstance.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingModelInstance.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingModelInstance.CopyName(destinationBuffer);
	
	public void SetTransformUsingFixedWidth(Location position, float width, Direction facingDirection, Direction? uprightDirection = null, Orientation2D positionAnchor = Orientation2D.None) {
		var @string = String;
		var stringSize = @string.Size;
		var transform = @string.Font.GetStringTransformUsingFixedWidth(stringSize, position, width, facingDirection, uprightDirection, positionAnchor);
		UnderlyingModelInstance.SetTransform(transform);
	}
	public void SetTransformUsingFixedHeight(Location position, float height, Direction facingDirection, Direction? uprightDirection = null, Orientation2D positionAnchor = Orientation2D.None) {
		var @string = String;
		var stringSize = @string.Size;
		var transform = @string.Font.GetStringTransformUsingFixedHeight(stringSize, position, height, facingDirection, uprightDirection, positionAnchor);
		UnderlyingModelInstance.SetTransform(transform);
	}
	public void SetTransformUsingFixedWidthAndHeight(Location position, XYPair<float> widthAndHeight, Direction facingDirection, Direction? uprightDirection = null, Orientation2D positionAnchor = Orientation2D.None) {
		var @string = String;
		var stringSize = @string.Size;
		var transform = @string.Font.GetStringTransformUsingFixedWidthAndHeight(stringSize, position, widthAndHeight, facingDirection, uprightDirection, positionAnchor);
		UnderlyingModelInstance.SetTransform(transform);
	}

	public void Dispose() => UnderlyingModelInstance.Dispose();

	public override string ToString() => $"Text {UnderlyingModelInstance}";

	#region Equality
	public bool Equals(TextInstance other) => UnderlyingModelInstance.Equals(other.UnderlyingModelInstance);
	public override bool Equals(object? obj) => obj is TextInstance other && Equals(other);
	public override int GetHashCode() => UnderlyingModelInstance.GetHashCode();
	public static bool operator ==(TextInstance left, TextInstance right) => left.Equals(right);
	public static bool operator !=(TextInstance left, TextInstance right) => !left.Equals(right);
	#endregion
}