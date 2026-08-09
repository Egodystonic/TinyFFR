// Created on 2026-07-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

public readonly struct TextInstance : ITextInstance, IEquatable<TextInstance>, ITransformedSceneObject {
	public ModelInstance UnderlyingModelInstance { get; }
	
	public Font Font => String.Font;

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
	
	public Transform Transform {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Transform;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetTransform(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTransform(Transform transform) => Transform = transform;
	
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Position;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetPosition(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	public Rotation Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Rotation;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetRotation(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotation(Rotation rotation) => Rotation = rotation;

	public Quaternion RotationQuaternion {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.RotationQuaternion;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetRotationQuaternion(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotationQuaternion(Quaternion rotationQuaternion) => RotationQuaternion = rotationQuaternion;

	Vect IScaledSceneObject.Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Scaling;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetScaling(value);
	}
	public XYPair<float> Scaling {
		get {
			var scalingVect = UnderlyingModelInstance.Scaling;
			return (scalingVect.X, scalingVect.Y);
		}
		set {
			UnderlyingModelInstance.SetScaling(new Vect(value.X, value.Y, 1f));
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetScaling(XYPair<float> scaling) => Scaling = scaling;

	internal TextInstance(ModelInstance underlyingModelInstance, FontPen pen, FontString @string, TextLayout layout) {
		UnderlyingModelInstance = underlyingModelInstance;
		UnderlyingModelInstance.Implementation.SetTextInstanceInitialPenAndString(UnderlyingModelInstance.GetHandleWithoutDisposeCheck(), pen, @string, layout);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextInstance FromPreviouslyAllocatedUnderlyingModelInstance(ModelInstance underlyingModelInstance, FontPen pen, FontString @string, TextLayout layout) => new(underlyingModelInstance, pen, @string, layout);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingModelInstance.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingModelInstance.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingModelInstance.CopyName(destinationBuffer);
	
	public void SetTransform(Location position, Direction facingDirection, Direction? uprightDirection, TextLayout layout) {
		UnderlyingModelInstance.Implementation.SetTextInstanceLayout(UnderlyingModelInstance.GetHandleWithoutDisposeCheck(), layout);
		
		var @string = String;
		var transform = @string.Font.GetTextInstanceTransform(@string.Size, position, facingDirection, uprightDirection, layout);
		UnderlyingModelInstance.SetTransform(transform);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveBy(Vect translation) => UnderlyingModelInstance.MoveBy(translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Rotation rotation) => UnderlyingModelInstance.RotateBy(rotation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Rotation rotation, Location pivotPoint) => UnderlyingModelInstance.RotateBy(rotation, pivotPoint);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Quaternion rotationQuaternion) => UnderlyingModelInstance.RotateBy(rotationQuaternion);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Quaternion rotationQuaternion, Location pivotPoint) => UnderlyingModelInstance.RotateBy(rotationQuaternion, pivotPoint);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(float scalar) => UnderlyingModelInstance.ScaleBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(Vect vect) => UnderlyingModelInstance.ScaleBy(vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(float scalar) => UnderlyingModelInstance.AdjustScaleBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(Vect vect) => UnderlyingModelInstance.AdjustScaleBy(vect);

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