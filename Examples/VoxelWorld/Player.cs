using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.World;

namespace VoxelWorld;

sealed class Player {
	const float HalfWidth = 0.3f;
	const float Height = 1.8f;
	const float EyeHeight = 1.62f;
	const float WalkSpeed = 4.5f;
	const float SprintSpeed = 8f;
	const float JumpSpeed = 12f;
	const float Gravity = 28f;
	const float TerminalFallSpeed = 50f;
	const float StepHeight = 1.05f;
	const float MaxCollisionStep = 0.05f;
	const float GroundProbeDistance = 0.06f;
	const float CollisionEpsilon = 0.001f;

	readonly Camera _camera;
	readonly FirstPersonCameraController _cameraController;
	readonly WorldStreamer _world;
	float _x;
	float _y;
	float _z;
	float _verticalSpeed;
	float _rightX = -1f;
	float _rightZ;

	public bool IsOnGround { get; private set; }
	public Location Position => new(_x, _y, _z);

	public Player(Camera camera, FirstPersonCameraController cameraController, WorldStreamer world, TerrainGenerator generator) {
		_camera = camera;
		_cameraController = cameraController;
		_world = world;
		_x = 0.5f;
		_z = 0.5f;
		_y = generator.GetSurfaceHeight(_x, _z) + 1.5f;
		_cameraController.PositionSmoothingStrength = SmoothingStrength.None;
		_cameraController.Position = new Location(_x, _y + EyeHeight, _z);
	}

	public void Update(ILatestInputRetriever input, float deltaTime) {
		var keyboardAndMouse = input.KeyboardAndMouse;
		var gamepad = input.GameControllersCombined;

		_cameraController.AdjustYawViaMouseCursor(keyboardAndMouse);
		_cameraController.AdjustPitchViaMouseCursor(keyboardAndMouse);
		_cameraController.AdjustYawViaControllerStick(gamepad, deltaTime);
		_cameraController.AdjustPitchViaControllerStick(gamepad, deltaTime);

		if (_world.IsChunkLiveAt(_x, _z)) {
			UpdateMovement(keyboardAndMouse, gamepad, deltaTime);
		}

		_cameraController.Position = new Location(_x, _y + EyeHeight, _z);
		_cameraController.Progress(deltaTime);
	}

	void UpdateMovement(ILatestKeyboardAndMouseInputRetriever keyboardAndMouse, ILatestGameControllerInputRetriever gamepad, float deltaTime) {
		var forwardInput = gamepad.LeftStickPosition.GetDisplacementVerticalWithDeadzone();
		var strafeInput = gamepad.LeftStickPosition.GetDisplacementHorizontalWithDeadzone();
		if (keyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.W)) forwardInput += 1f;
		if (keyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.S)) forwardInput -= 1f;
		if (keyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.D)) strafeInput += 1f;
		if (keyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.A)) strafeInput -= 1f;

		var inputMagnitude = MathF.Sqrt(forwardInput * forwardInput + strafeInput * strafeInput);
		if (inputMagnitude > 1f) {
			forwardInput /= inputMagnitude;
			strafeInput /= inputMagnitude;
		}

		var isSprinting = keyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.LeftShift) || gamepad.ButtonIsCurrentlyDown(GameControllerButton.LeftStick);
		var speed = isSprinting ? SprintSpeed : WalkSpeed;
		UpdateHorizontalRight();
		var forwardX = _rightZ;
		var forwardZ = -_rightX;
		var moveX = (forwardX * forwardInput + _rightX * strafeInput) * speed * deltaTime;
		var moveZ = (forwardZ * forwardInput + _rightZ * strafeInput) * speed * deltaTime;

		var wantsToJump = keyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Space) || gamepad.ButtonIsCurrentlyDown(GameControllerButton.A);
		if (IsOnGround && wantsToJump) _verticalSpeed = JumpSpeed;
		_verticalSpeed = MathF.Max(_verticalSpeed - Gravity * deltaTime, -TerminalFallSpeed);

		MoveHorizontally(moveX, moveZ);
		MoveVertically(_verticalSpeed * deltaTime);
		IsOnGround = Collides(_x, _y - GroundProbeDistance, _z);
	}

	void UpdateHorizontalRight() {
		var right = _camera.GetRelativeOrientationDirection(Orientation.Right);
		var length = MathF.Sqrt(right.X * right.X + right.Z * right.Z);
		if (length < 0.0001f) return;
		_rightX = right.X / length;
		_rightZ = right.Z / length;
	}

	void MoveHorizontally(float moveX, float moveZ) {
		var steps = (int) MathF.Ceiling(MathF.Max(MathF.Abs(moveX), MathF.Abs(moveZ)) / MaxCollisionStep);
		if (steps == 0) return;
		var stepX = moveX / steps;
		var stepZ = moveZ / steps;
		for (var i = 0; i < steps; ++i) {
			if (!TryMoveTo(_x + stepX, _y, _z)) TryStepUpTo(_x + stepX, _z);
			if (!TryMoveTo(_x, _y, _z + stepZ)) TryStepUpTo(_x, _z + stepZ);
		}
	}

	void MoveVertically(float moveY) {
		var steps = (int) MathF.Ceiling(MathF.Abs(moveY) / MaxCollisionStep);
		if (steps == 0) return;
		var stepY = moveY / steps;
		for (var i = 0; i < steps; ++i) {
			if (TryMoveTo(_x, _y + stepY, _z)) continue;
			_verticalSpeed = 0f;
			return;
		}
	}

	bool TryMoveTo(float x, float y, float z) {
		if (Collides(x, y, z)) return false;
		_x = x;
		_y = y;
		_z = z;
		return true;
	}

	void TryStepUpTo(float x, float z) {
		if (!IsOnGround || Collides(_x, _y + StepHeight, _z)) return;
		TryMoveTo(x, _y + StepHeight, z);
	}

	bool Collides(float x, float y, float z) {
		var minX = (int) MathF.Floor(x - HalfWidth);
		var maxX = (int) MathF.Floor(x + HalfWidth - CollisionEpsilon);
		var minY = (int) MathF.Floor(y);
		var maxY = (int) MathF.Floor(y + Height - CollisionEpsilon);
		var minZ = (int) MathF.Floor(z - HalfWidth);
		var maxZ = (int) MathF.Floor(z + HalfWidth - CollisionEpsilon);
		for (var bx = minX; bx <= maxX; ++bx) {
			for (var by = minY; by <= maxY; ++by) {
				for (var bz = minZ; bz <= maxZ; ++bz) {
					if (_world.IsSolid(bx, by, bz)) return true;
				}
			}
		}
		return false;
	}
}
