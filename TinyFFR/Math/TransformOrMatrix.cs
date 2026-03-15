// Created on 2026-03-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Diagnostics;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Explicit)]
public readonly struct TransformOrMatrix : IEquatable<TransformOrMatrix> {
	static readonly Vector4 TransformDifferentiationVectorValue = new(10101.0101f, -222222f, 0.333333f, 4.0404440f);
	
	[FieldOffset(0)]
	readonly Matrix4x4 _matrix;
	[FieldOffset(0)]
	readonly Transform _transform;	
	[FieldOffset(3 * 4 * sizeof(float))]
	readonly Vector4 _differentiationVector;
	
	public bool IsMatrix => _differentiationVector == TransformDifferentiationVectorValue;
	public bool IsTransform => _differentiationVector != TransformDifferentiationVectorValue;
	
	public Matrix4x4? AsMatrix => IsMatrix ? _matrix : null;
	public Transform? AsTransform => IsTransform ? _transform : null;
	
	internal Matrix4x4 AsMatrixFast {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _matrix;
	}
	internal Transform AsTransformFast {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _transform;
	}

	public TransformOrMatrix(Matrix4x4 matrix) => _matrix = matrix;
	public TransformOrMatrix(Transform transform) {
		_transform = transform;
		_differentiationVector = TransformDifferentiationVectorValue;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TransformOrMatrix(Matrix4x4 matrix) => new(matrix);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TransformOrMatrix(Transform transform) => new(transform);
	
	public Matrix4x4 ToMatrix() => AsMatrix ?? _transform.ToMatrix();
	public void ToMatrix(out Matrix4x4 dest) {
		if (IsTransform) _transform.ToMatrix(out dest);
		else dest = _matrix;
	}

	public bool Equals(TransformOrMatrix other) => _matrix.Equals(other._matrix);
	public override bool Equals(object? obj) => obj is TransformOrMatrix other && Equals(other);
	public override int GetHashCode() => _matrix.GetHashCode();
	public static bool operator ==(TransformOrMatrix left, TransformOrMatrix right) => left.Equals(right);
	public static bool operator !=(TransformOrMatrix left, TransformOrMatrix right) => !left.Equals(right);
}