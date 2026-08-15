// Created on 2026-07-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

public readonly struct CameraLockedTextInstance : ITextInstance, IResourceSpecialization<CameraLockedTextInstance, ModelInstance>, IEquatable<CameraLockedTextInstance>, IScaledSceneObject, IPositionedSceneObject {
	public TextInstance UnderlyingTextInstance { get; }
	public Direction LockedUprightDirection { get; } // Can be None
	public Orientation2D PositionAnchor { get; }
	public CameraLockedScalingMode ScalingMode { get; }
	public CameraLockStyle LockStyle { get; }

	internal CameraLockedTextInstance(TextInstance underlyingTextInstance, Direction lockedUprightDirection, Orientation2D positionAnchor, CameraLockedScalingMode scalingMode, CameraLockStyle lockStyle) {
		UnderlyingTextInstance = underlyingTextInstance;
		LockedUprightDirection = lockedUprightDirection;
		PositionAnchor = positionAnchor;
		ScalingMode = scalingMode;
		LockStyle = lockStyle;
	}
	
	#region Specialization
	static IntPtr IResourceSpecialization<CameraLockedTextInstance, ModelInstance>.SpecializationTypeIdentifier => typeof(CameraLockedTextInstance).TypeHandle.Value;
	int IResourceSpecialization<CameraLockedTextInstance, ModelInstance>.SpecializationDataLength => Direction.SerializationByteSpanLength + sizeof(int) + sizeof(int) + sizeof(int);
	static void IResourceSpecialization<CameraLockedTextInstance, ModelInstance>.Smuggle(CameraLockedTextInstance resource, Span<byte> specializationDataBuffer, out ModelInstance outBaseResource, out ResourceStub? additionalResourceRef) {
		additionalResourceRef = null;
		Direction.SerializeToBytes(specializationDataBuffer, resource.LockedUprightDirection);
		BinaryPrimitives.WriteInt32LittleEndian(specializationDataBuffer[Direction.SerializationByteSpanLength..], (int) resource.PositionAnchor);
		BinaryPrimitives.WriteInt32LittleEndian(specializationDataBuffer[(Direction.SerializationByteSpanLength + sizeof(int) * 1)..], (int) resource.ScalingMode);
		BinaryPrimitives.WriteInt32LittleEndian(specializationDataBuffer[(Direction.SerializationByteSpanLength + sizeof(int) * 2)..], (int) resource.LockStyle);
		outBaseResource = resource.UnderlyingTextInstance.UnderlyingModelInstance;
	}
	static CameraLockedTextInstance IResourceSpecialization<CameraLockedTextInstance, ModelInstance>.DeSmuggle(ModelInstance baseResource, ReadOnlySpan<byte> specializationDataBuffer, ResourceStub? additionalResourceRef) {
		return new(
			new TextInstance(baseResource),
			Direction.DeserializeFromBytes(specializationDataBuffer),
			(Orientation2D) BinaryPrimitives.ReadInt32LittleEndian(specializationDataBuffer[Direction.SerializationByteSpanLength..]),
			(CameraLockedScalingMode) BinaryPrimitives.ReadInt32LittleEndian(specializationDataBuffer[(Direction.SerializationByteSpanLength + sizeof(int) * 1)..]),
			(CameraLockStyle) BinaryPrimitives.ReadInt32LittleEndian(specializationDataBuffer[(Direction.SerializationByteSpanLength + sizeof(int) * 2)..])
		);	
	}
	#endregion
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CameraLockedTextInstance FromPreviouslyAllocatedUnderlyingTextInstance(TextInstance underlyingTextInstance, Direction lockedUprightDirection, Orientation2D positionAnchor, CameraLockedScalingMode scalingMode, CameraLockStyle lockStyle) {
		return new(underlyingTextInstance, lockedUprightDirection, positionAnchor, scalingMode, lockStyle);
	}

	public Font Font => UnderlyingTextInstance.Font;

	public FontPen Pen {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingTextInstance.Pen;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingTextInstance.SetPen(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPen(FontPen pen) => Pen = pen;

	public FontString String {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingTextInstance.String;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingTextInstance.SetString(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetString(FontString @string) => String = @string;

	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingTextInstance.Position;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingTextInstance.SetPosition(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	Vect IScaledSceneObject.Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingTextInstance.UnderlyingModelInstance.Scaling;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingTextInstance.UnderlyingModelInstance.SetScaling(value);
	}
	public XYPair<float> Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingTextInstance.Scaling;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingTextInstance.SetScaling(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetScaling(XYPair<float> scaling) => Scaling = scaling;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingTextInstance.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingTextInstance.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingTextInstance.CopyName(destinationBuffer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveBy(Vect translation) => UnderlyingTextInstance.MoveBy(translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(float scalar) => UnderlyingTextInstance.ScaleBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(Vect vect) => UnderlyingTextInstance.ScaleBy(vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(float scalar) => UnderlyingTextInstance.AdjustScaleBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(Vect vect) => UnderlyingTextInstance.AdjustScaleBy(vect);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingTextInstance.Dispose();

	public override string ToString() => UnderlyingTextInstance.ToString();

	#region Equality
	public bool Equals(CameraLockedTextInstance other) => UnderlyingTextInstance.Equals(other.UnderlyingTextInstance);
	public override bool Equals(object? obj) => obj is CameraLockedTextInstance other && Equals(other);
	public override int GetHashCode() => UnderlyingTextInstance.GetHashCode();
	public static bool operator ==(CameraLockedTextInstance left, CameraLockedTextInstance right) => left.Equals(right);
	public static bool operator !=(CameraLockedTextInstance left, CameraLockedTextInstance right) => !left.Equals(right);
	#endregion
}
