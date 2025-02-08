// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

sealed class LocalCameraBuilder : ICameraBuilder, ICameraImplProvider, IDisposable {
	readonly record struct CameraParameters(Location Position, Direction ViewDirection, Direction UpDirection, float VerticalFovRadians, float AspectRatio, float NearPlaneDistance, float FarPlaneDistance);

	const string DefaultCameraName = "Unnamed Camera";
	const float DefaultAspectRatio = 16f / 9f;
	readonly ArrayPoolBackedMap<CameraHandle, CameraParameters> _activeCameras = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	bool _isDisposed = false;

	public LocalCameraBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
	}

	public Camera CreateCamera(in CameraCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		AllocateCamera(out var newCameraHandle).ThrowIfFailure();

		var parameters = new CameraParameters(
			config.Position,
			config.ViewDirection,
			GetReorthogonalizedUpOrViewDirection(config.UpDirection, config.ViewDirection),
			config.FieldOfViewIsVertical ? config.FieldOfView.Radians : ConvertHorizontalFovToVertical(config.FieldOfView.Radians, DefaultAspectRatio),
			DefaultAspectRatio,
			config.NearPlaneDistance,
			config.FarPlaneDistance
		);

		_activeCameras.Add(newCameraHandle, parameters);
		_globals.StoreResourceNameIfNotEmpty(((CameraHandle) newCameraHandle).Ident, config.Name);
		UpdateProjectionMatrixFromParameters(newCameraHandle);
		UpdateModelMatrixFromParameters(newCameraHandle);
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
		return _activeCameras[handle].Position;
	}
	public void SetPosition(CameraHandle handle, Location newPosition) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_activeCameras[handle] = _activeCameras[handle] with { Position = newPosition };
		UpdateModelMatrixFromParameters(handle);
	}

	public Direction GetViewDirection(CameraHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeCameras[handle].ViewDirection;
	}
	public void SetViewDirection(CameraHandle handle, Direction newDirection) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (newDirection == Direction.None) throw new ArgumentException($"View direction can not be '{nameof(Direction.None)}'.", nameof(newDirection));
		_activeCameras[handle] = _activeCameras[handle] with {
			ViewDirection = newDirection, 
			UpDirection = GetReorthogonalizedUpOrViewDirection(_activeCameras[handle].UpDirection, newDirection)
		};
		UpdateModelMatrixFromParameters(handle);
	}

	public Direction GetUpDirection(CameraHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeCameras[handle].UpDirection;
	}
	public void SetUpDirection(CameraHandle handle, Direction newDirection) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (newDirection == Direction.None) throw new ArgumentException($"Up direction can not be '{nameof(Direction.None)}'.", nameof(newDirection));
		_activeCameras[handle] = _activeCameras[handle] with {
			UpDirection = newDirection,
			ViewDirection = GetReorthogonalizedUpOrViewDirection(_activeCameras[handle].ViewDirection, newDirection)
		};
		UpdateModelMatrixFromParameters(handle);
	}

	public Angle GetVerticalFieldOfView(CameraHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return Angle.FromRadians(_activeCameras[handle].VerticalFovRadians);
	}
	public void SetVerticalFieldOfView(CameraHandle handle, Angle newFov) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (newFov < Camera.FieldOfViewMin || newFov > Camera.FieldOfViewMax) {
			throw new ArgumentException(
				$"Field of view must be between {nameof(Camera)}.{nameof(Camera.FieldOfViewMin)} ({Camera.FieldOfViewMin}) and {nameof(Camera)}.{nameof(Camera.FieldOfViewMax)} ({Camera.FieldOfViewMax}).",
				nameof(newFov)
			);
		}
		_activeCameras[handle] = _activeCameras[handle] with { VerticalFovRadians = newFov.Radians };
		UpdateProjectionMatrixFromParameters(handle);
	}

	public Angle GetHorizontalFieldOfView(CameraHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return Angle.FromRadians(ConvertVerticalFovToHorizontal(_activeCameras[handle].VerticalFovRadians, _activeCameras[handle].AspectRatio));
	}
	public void SetHorizontalFieldOfView(CameraHandle handle, Angle newFov) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetVerticalFieldOfView(handle, Angle.FromRadians(ConvertHorizontalFovToVertical(newFov.Radians, _activeCameras[handle].AspectRatio)));
	}

	public float GetNearPlaneDistance(CameraHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeCameras[handle].NearPlaneDistance;
	}
	public void SetNearPlaneDistance(CameraHandle handle, float newDistance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!Single.IsNormal(newDistance) || newDistance < Camera.NearPlaneDistanceMin) {
			throw new ArgumentException(
				$"Near-plane distance must be a normal floating-point value greater than or equal to {nameof(Camera)}.{nameof(Camera.NearPlaneDistanceMin)} ({Camera.NearPlaneDistanceMin}).", 
				nameof(newDistance)
			);
		}

		var farDist = _activeCameras[handle].FarPlaneDistance;
		if (newDistance >= farDist) {
			throw new ArgumentException(
				$"Near-plane distance must be less than far-plane distance (currently {farDist}).",
				nameof(newDistance)
			);
		}

		_activeCameras[handle] = _activeCameras[handle] with {
			NearPlaneDistance = newDistance,
			FarPlaneDistance = MathF.Min(farDist, newDistance * Camera.NearFarPlaneDistanceRatioMax)
		};

		UpdateProjectionMatrixFromParameters(handle);
	}
	public float GetFarPlaneDistance(CameraHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeCameras[handle].FarPlaneDistance;
	}
	public void SetFarPlaneDistance(CameraHandle handle, float newDistance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!Single.IsNormal(newDistance)) {
			throw new ArgumentException(
				$"Far-plane distance must be a normal floating-point value.",
				nameof(newDistance)
			);
		}

		var nearDist = _activeCameras[handle].NearPlaneDistance;
		if (newDistance <= nearDist) {
			throw new ArgumentException(
				$"Far-plane distance must be greater than near-plane distance (currently {nearDist}).",
				nameof(newDistance)
			);
		}

		_activeCameras[handle] = _activeCameras[handle] with {
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
		SetCameraProjectionMatrix(handle, in newMatrix, _activeCameras[handle].NearPlaneDistance, _activeCameras[handle].FarPlaneDistance)
			.ThrowIfFailure();
	}

	public void GetModelMatrix(CameraHandle handle, out Matrix4x4 outMatrix) {
		ThrowIfThisOrHandleIsDisposed(handle);
		GetCameraModelMatrix(handle, out outMatrix).ThrowIfFailure();
	}

	public void SetModelMatrix(CameraHandle handle, in Matrix4x4 newMatrix) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetCameraModelMatrix(handle, in newMatrix)
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

	public void Translate(CameraHandle handle, Vect translation) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var curParams = _activeCameras[handle];
		_activeCameras[handle] = curParams with { Position = curParams.Position + translation };
		UpdateModelMatrixFromParameters(handle);
	}
	public void Rotate(CameraHandle handle, Rotation rotation) {
		ThrowIfThisOrHandleIsDisposed(handle);
		SetViewDirection(handle, _activeCameras[handle].ViewDirection * rotation);
	}

	void UpdateProjectionMatrixFromParameters(CameraHandle handle) {
		var parameters = _activeCameras[handle];

		var near = parameters.NearPlaneDistance;
		var far = parameters.FarPlaneDistance;

		var h = MathF.Tan(parameters.VerticalFovRadians * 0.5f) * near;
		var w = h * parameters.AspectRatio;

		var left = -w;
		var right = w;
		var bottom = -h;
		var top = h;

		SetCameraProjectionMatrix(
			handle,
			new Matrix4x4(
				(near * 2f) / (right - left),			0f,										0f,										0f,
				0f,										(near * 2f) / (top - bottom),			0f,										0f,
				(right + left) / (right - left),		(top + bottom) / (top - bottom),		-(far + near) / (far - near),			-1f,
				0f,										0f,										-(2f * far * near) / (far - near),		0f
			),
			parameters.NearPlaneDistance,
			parameters.FarPlaneDistance
		).ThrowIfFailure();
	}

	void UpdateModelMatrixFromParameters(CameraHandle handle) {
		var parameters = _activeCameras[handle];

		var p = parameters.Position.ToVector3();
		var z = parameters.ViewDirection.ToVector3();
		var x = Vector3.Cross(z, parameters.UpDirection.ToVector3());
		var y = Vector3.Cross(x, z);
		z = -z;

		SetCameraModelMatrix(
			handle,
			new Matrix4x4(
				m11: x.X, m12: x.Y, m13: x.Z,
				m21: y.X, m22: y.Y, m23: y.Z,
				m31: z.X, m32: z.Y, m33: z.Z,
				m41: p.X,
				m42: p.Y,
				m43: p.Z,
				m14: 0f,
				m24: 0f,
				m34: 0f,
				m44: 1f
			)
		).ThrowIfFailure();
	}

	public ReadOnlySpan<char> GetName(CameraHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultCameraName);
	}

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "allocate_camera")]
	static extern InteropResult AllocateCamera(
		out UIntPtr outCameraHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_camera_projection_matrix")]
	static extern InteropResult SetCameraProjectionMatrix(
		UIntPtr cameraHandle,
		in Matrix4x4 newMatrix,
		float nearPlaneDistance,
		float farPlaneDistance
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_camera_projection_matrix")]
	static extern InteropResult GetCameraProjectionMatrix(
		UIntPtr cameraHandle,
		out Matrix4x4 outMatrix,
		out float outNearPlaneDist,
		out float outFarPlaneDist
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_camera_model_matrix")]
	static extern InteropResult SetCameraModelMatrix(
		UIntPtr cameraHandle,
		in Matrix4x4 newMatrix
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_camera_model_matrix")]
	static extern InteropResult GetCameraModelMatrix(
		UIntPtr cameraHandle,
		out Matrix4x4 outMatrix
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_camera_view_matrix")]
	static extern InteropResult SetCameraViewMatrix(
		UIntPtr cameraHandle,
		in Matrix4x4 newMatrix
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_camera_view_matrix")]
	static extern InteropResult GetCameraViewMatrix(
		UIntPtr cameraHandle,
		out Matrix4x4 outMatrix
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_camera")]
	static extern InteropResult DisposeCamera(
		UIntPtr cameraHandle
	);
	#endregion

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Camera HandleToInstance(CameraHandle h) => new(h, this);

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _activeCameras) Dispose(kvp.Key, removeFromMap: false);
			_activeCameras.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	public bool IsDisposed(CameraHandle handle) => _isDisposed || !_activeCameras.ContainsKey(handle);

	public void Dispose(CameraHandle handle) => Dispose(handle, removeFromMap: true);
	void Dispose(CameraHandle handle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		DisposeCamera(handle).ThrowIfFailure();
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromMap) _activeCameras.Remove(handle);
	}

	void ThrowIfThisOrHandleIsDisposed(CameraHandle handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Camera));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}