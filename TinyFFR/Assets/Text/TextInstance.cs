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
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPen(FontPen pen) => Pen = pen;
	
	public FontString String {
		get => UnderlyingModelInstance.Implementation.GetTextInstanceString(UnderlyingModelInstance.GetHandleWithoutDisposeCheck());
		set {
			UnderlyingModelInstance.SetMesh(value.GetStringMesh());
			UnderlyingModelInstance.Implementation.UpdateTextInstanceString(UnderlyingModelInstance.GetHandleWithoutDisposeCheck(), value);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetString(FontString @string) => String = @string;

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
	
	public void SetTransform(Location position, Direction facingDirection, float? textInstanceHeight, float? textInstanceWidth = null, Direction? uprightDirection = null, Orientation2D positionAnchor = Orientation2D.None) {
		var @string = String;
		var stringSize = @string.Size;
		var transform = @string.Font.GetTextInstanceTransform(stringSize, position, facingDirection, textInstanceHeight, textInstanceWidth, uprightDirection, positionAnchor);
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