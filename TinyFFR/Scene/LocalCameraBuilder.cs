// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Scene;

sealed unsafe class LocalCameraBuilder : ICameraBuilder, ICameraAssetImplProvider, IDisposable {
	readonly record struct CameraParameters(Location Position, Direction ViewDirection, Direction UpDirection, float VerticalFovRadians, float AspectRatio, float NearPlaneDistance, float FarPlaneDistance);

	const float DefaultAspectRatio = 16f / 9f;
	readonly ArrayPoolBackedMap<UIntPtr, CameraParameters> _activeCameras = new();
	bool _isDisposed = false;

	public LocalCameraBuilder() {
		
	}

	public Camera CreateCamera() => CreateCamera(new CameraCreationConfig());
	public Camera CreateCamera(Location initialPosition, Direction initialViewDirection) {
		return CreateCamera(new CameraCreationConfig() {
			Position = initialPosition,
			ViewDirection = initialViewDirection
		});
	}
	public Camera CreateCamera(in CameraCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		AllocateCamera(out var newCameraHandle).ThrowIfFailure();

		var parameters = new CameraParameters(
			config.Position,
			config.ViewDirection,
			GetReorthogonalizedUpOrViewDirection(config.UpDirection, config.ViewDirection),
			config.FieldOfViewIsVertical ? config.FieldOfView.AsRadians : ConvertHorizontalFovToVertical(config.FieldOfView.AsRadians, DefaultAspectRatio),
			DefaultAspectRatio,
			config.NearPlaneDistance,
			config.FarPlaneDistance
		);

		_activeCameras.Add((UIntPtr) newCameraHandle, parameters);
		return new(newCameraHandle, this);
	}

	static Direction GetReorthogonalizedUpOrViewDirection(Direction currentSecondaryDirection, Direction newPrimaryDirection) {
		return currentSecondaryDirection.OrthogonalizedAgainst(newPrimaryDirection) ?? newPrimaryDirection.AnyOrthogonal();
	}
	static float ConvertHorizontalFovToVertical(float horizontalFovRadians, float aspectRatio) {
		return 2f * MathF.Atan(MathF.Tan(horizontalFovRadians * 0.5f) / aspectRatio);
	}
	static float ConvertVerticalFovToHorizontal(float verticalFovRadians, float aspectRatio) {
		return 2f * MathF.Atan(aspectRatio * MathF.Tan(verticalFovRadians * 0.5f));
	}

	public Location GetPosition(CameraHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeCameras[(UIntPtr) handle].Position;
	}
	public void SetPosition(CameraHandle handle, Location newPosition) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_activeCameras[(UIntPtr) handle] = _activeCameras[(UIntPtr) handle] with { Position = newPosition };
		UpdateViewMatrixFromParameters(handle);
	}

	public Direction GetViewDirection(CameraHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeCameras[(UIntPtr) handle].ViewDirection;
	}
	public void SetViewDirection(CameraHandle handle, Direction newDirection) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (newDirection == Direction.None) throw new ArgumentException($"View direction can not be '{nameof(Direction.None)}'.", nameof(newDirection));
		_activeCameras[(UIntPtr) handle] = _activeCameras[(UIntPtr) handle] with {
			ViewDirection = newDirection, 
			UpDirection = GetReorthogonalizedUpOrViewDirection(_activeCameras[(UIntPtr) handle].UpDirection, newDirection)
		};
		UpdateViewMatrixFromParameters(handle);
	}

	public Direction GetUpDirection(CameraHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeCameras[(UIntPtr) handle].UpDirection;
	}
	public void SetUpDirection(CameraHandle handle, Direction newDirection) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (newDirection == Direction.None) throw new ArgumentException($"Up direction can not be '{nameof(Direction.None)}'.", nameof(newDirection));
		_activeCameras[(UIntPtr) handle] = _activeCameras[(UIntPtr) handle] with {
			UpDirection = newDirection,
			ViewDirection = GetReorthogonalizedUpOrViewDirection(_activeCameras[(UIntPtr) handle].ViewDirection, newDirection)
		};
		UpdateViewMatrixFromParameters(handle);
	}

	public Angle GetVerticalFieldOfView(CameraHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return Angle.FromRadians(_activeCameras[(UIntPtr) handle].VerticalFovRadians);
	}
	public void SetVerticalFieldOfView(CameraHandle handle, Angle newFov) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (newFov < Camera.FieldOfViewMin || newFov > Camera.FieldOfViewMax) {
			throw new ArgumentException(
				$"Field of view must be between {nameof(Camera)}.{nameof(Camera.FieldOfViewMin)} ({Camera.FieldOfViewMin}) and {nameof(Camera)}.{nameof(Camera.FieldOfViewMax)} ({Camera.FieldOfViewMax}).",
				nameof(newFov)
			);
		}
		_activeCameras[(UIntPtr) handle] = _activeCameras[(UIntPtr) handle] with { VerticalFovRadians = newFov.AsRadians };
		UpdateProjectionMatrixFromParameters(handle);
	}

	public Angle GetHorizontalFieldOfView(CameraHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return Angle.FromRadians(ConvertVerticalFovToHorizontal(_activeCameras[(UIntPtr) handle].VerticalFovRadians, _activeCameras[(UIntPtr) handle].AspectRatio));
	}
	public void SetHorizontalFieldOfView(CameraHandle handle, Angle newFov) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetVerticalFieldOfView(handle, Angle.FromRadians(ConvertHorizontalFovToVertical(newFov.AsRadians, _activeCameras[(UIntPtr) handle].AspectRatio)));
	}

	public float GetNearPlaneDistance(CameraHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeCameras[(UIntPtr) handle].NearPlaneDistance;
	}
	public void SetNearPlaneDistance(CameraHandle handle, float newDistance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!Single.IsNormal(newDistance) || newDistance < Camera.NearPlaneDistanceMin) {
			throw new ArgumentException(
				$"Near-plane distance must be a normal floating-point value greater than or equal to {nameof(Camera)}.{nameof(Camera.NearPlaneDistanceMin)} ({Camera.NearPlaneDistanceMin}).", 
				nameof(newDistance)
			);
		}

		var farDist = _activeCameras[(UIntPtr) handle].FarPlaneDistance;
		if (newDistance >= farDist) {
			throw new ArgumentException(
				$"Near-plane distance must be less than far-plane distance (currently {farDist}).",
				nameof(newDistance)
			);
		}

		_activeCameras[(UIntPtr) handle] = _activeCameras[(UIntPtr) handle] with {
			NearPlaneDistance = newDistance,
			FarPlaneDistance = MathF.Min(farDist, newDistance * Camera.NearFarPlaneDistanceRatioMax)
		};

		UpdateProjectionMatrixFromParameters(handle);
	}
	public float GetFarPlaneDistance(CameraHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeCameras[(UIntPtr) handle].FarPlaneDistance;
	}
	public void SetFarPlaneDistance(CameraHandle handle, float newDistance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!Single.IsNormal(newDistance)) {
			throw new ArgumentException(
				$"Far-plane distance must be a normal floating-point value.",
				nameof(newDistance)
			);
		}

		var nearDist = _activeCameras[(UIntPtr) handle].NearPlaneDistance;
		if (newDistance <= nearDist) {
			throw new ArgumentException(
				$"Far-plane distance must be greater than near-plane distance (currently {nearDist}).",
				nameof(newDistance)
			);
		}

		_activeCameras[(UIntPtr) handle] = _activeCameras[(UIntPtr) handle] with {
			FarPlaneDistance = newDistance,
			NearPlaneDistance = MathF.Max(nearDist, newDistance / Camera.NearFarPlaneDistanceRatioMax)
		};

		UpdateProjectionMatrixFromParameters(handle);
	}

	public void GetProjectionMatrix(CameraHandle handle, out Matrix4x4 outMatrix) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetCameraProjectionMatrix(handle, out outMatrix, out _, out _).ThrowIfFailure();
	}

	public void SetProjectionMatrix(CameraHandle handle, in Matrix4x4 newMatrix) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetCameraProjectionMatrix(handle, in newMatrix, _activeCameras[(UIntPtr) handle].NearPlaneDistance, _activeCameras[(UIntPtr) handle].FarPlaneDistance)
			.ThrowIfFailure();
	}

	public void GetViewMatrix(CameraHandle handle, out Matrix4x4 outMatrix) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetCameraViewMatrix(handle, out outMatrix).ThrowIfFailure();
	}

	public void SetViewMatrix(CameraHandle handle, in Matrix4x4 newMatrix) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetCameraViewMatrix(handle, in newMatrix)
			.ThrowIfFailure();
	}

	void UpdateProjectionMatrixFromParameters(CameraHandle handle) {
		var parameters = _activeCameras[(UIntPtr) handle];

		var a = parameters.AspectRatio;
		var v = parameters.VerticalFovRadians;
		var t = MathF.Tan(v * 0.5f);
		var f = parameters.FarPlaneDistance;
		var n = parameters.NearPlaneDistance;

		SetCameraProjectionMatrix(
			handle,
			new Matrix4x4(
				m11: 1f / (a * t),
				m22: 1f / t,
				m33: f / (f - n),
				m34: 1f,
				m43: (-n * f) / (f - n),

				m12: 0f,
				m13: 0f,
				m14: 0f,
				m21: 0f,
				m23: 0f,
				m24: 0f,
				m31: 0f,
				m32: 0f,
				m41: 0f,
				m42: 0f,
				m44: 0f
			),
			n,
			f
		).ThrowIfFailure();
	}

	void UpdateViewMatrixFromParameters(CameraHandle handle) {
		var parameters = _activeCameras[(UIntPtr) handle];

		var p = parameters.Position.ToVector3();
		var o = parameters.ViewDirection.ToVector3();
		var u = Vector3.Cross(parameters.UpDirection.ToVector3(), o);
		var v = Vector3.Cross(o, u);

		SetCameraViewMatrix(
			handle,
			new Matrix4x4(
				m11: u.X, m21: u.Y, m31: u.Z,
				m12: v.X, m22: v.Y, m32: v.Z,
				m13: o.X, m23: o.Y, m33: o.Z,
				m41: Vector3.Dot(p, -u),
				m42: Vector3.Dot(p, -v),
				m43: Vector3.Dot(p, -o),
				m14: 0f,
				m24: 0f,
				m34: 0f,
				m44: 1f
			)
		).ThrowIfFailure();
	}

	#region Native Methods
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "allocate_camera")]
	static extern InteropResult AllocateCamera(
		out CameraHandle outCameraHandle
	);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_camera_projection_matrix")]
	static extern InteropResult SetCameraProjectionMatrix(
		CameraHandle cameraHandle,
		in Matrix4x4 newMatrix,
		float nearPlaneDistance,
		float farPlaneDistance
	);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_camera_projection_matrix")]
	static extern InteropResult GetCameraProjectionMatrix(
		CameraHandle cameraHandle,
		out Matrix4x4 outMatrix,
		out float outNearPlaneDist,
		out float outFarPlaneDist
	);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "set_camera_view_matrix")]
	static extern InteropResult SetCameraViewMatrix(
		CameraHandle cameraHandle,
		in Matrix4x4 newMatrix
	);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_camera_view_matrix")]
	static extern InteropResult GetCameraViewMatrix(
		CameraHandle cameraHandle,
		out Matrix4x4 outMatrix
	);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "dispose_camera")]
	static extern InteropResult DisposeCamera(
		CameraHandle cameraHandle
	);
	#endregion

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _activeCameras) Dispose((CameraHandle) kvp.Key, removeFromMap: false);
		}
		finally {
			_isDisposed = true;
		}
	}

	public bool IsDisposed(CameraHandle handle) => _isDisposed || !_activeCameras.ContainsKey((UIntPtr) handle);

	public void Dispose(CameraHandle handle) => Dispose(handle, removeFromMap: true);
	void Dispose(CameraHandle handle, bool removeFromMap) {
		ThrowIfThisOrHandleIsDisposed(handle);
		DisposeCamera(handle).ThrowIfFailure();
		if (removeFromMap) _activeCameras.Remove((UIntPtr) handle);
	}

	void ThrowIfThisOrHandleIsDisposed(CameraHandle handle) {
		ThrowIfThisIsDisposed();
		ObjectDisposedException.ThrowIf(!_activeCameras.ContainsKey((UIntPtr) handle), typeof(Camera));
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(ICameraBuilder));
	}
	#endregion
}