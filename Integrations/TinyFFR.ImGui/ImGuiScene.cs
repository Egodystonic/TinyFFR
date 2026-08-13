// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Numerics;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.DearImGui.Input;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;
using Hexa.NET.ImGui;

namespace Egodystonic.TinyFFR.DearImGui;

public sealed unsafe class ImGuiScene : IDisposable {
	const int InitialVertexCapacity = 4096;
	const int InitialIndexCapacity = 8192;
	const float CameraDistance = 10f;
	static readonly PositionedCuboid MeshViewBoundingBox = new(new Cuboid(20000f, 20000f, 20000f), Location.Origin);

	readonly ITinyFfrFactory _factory;
	readonly LocalMaterialBuilder _materialBuilder;
	readonly LocalMeshBuilder _meshBuilder;
	readonly ImGuiContextPtr _context;
	readonly ImGuiInputPump _inputPump;
	readonly Dictionary<int, Texture> _textures = new();
	readonly List<DynamicVertexBuffer> _meshPool = new();
	readonly List<int> _meshVertexCapacities = new();
	readonly List<int> _meshIndexCapacities = new();
	readonly List<DrawCall> _drawCallPool = new();
	readonly List<Texture> _pendingTextureDisposals = new();
	readonly DynamicVertexBuffer _parkingVertexBuffer;
	readonly Mesh _parkingView;
	readonly Texture _parkingTexture;
	readonly ImGuiStyle _pristineStyle;

	XYPair<float> _dpiScale = new(1f, 1f);
	float _appliedStyleScale = 1f;
	int _activeDrawCallCount;
	bool _isDisposed;

	public Scene UnderlyingScene { get; }
	public Camera Camera { get; }
	public ImGuiContextPtr Context => _context;

	sealed class DrawCall {
		public required ModelInstance Instance { get; init; }
		public required Material Material { get; init; }
		public Mesh? CurrentView { get; set; }
		public Texture CurrentTexture { get; set; }
	}

	internal ImGuiScene(ITinyFfrFactory factory, in ImGuiSceneCreationConfig config) {
		ArgumentNullException.ThrowIfNull(factory);
		config.ThrowIfInvalid();
		_factory = factory;
		_materialBuilder = (LocalMaterialBuilder) factory.MaterialBuilder;
		_inputPump = new ImGuiInputPump(config.EnableGamepadNavigation, config.GamepadStickDeadzone, config.GamepadTriggerDeadzone);

		UnderlyingScene = factory.SceneBuilder.CreateScene(new SceneCreationConfig { InitialBackdropColor = null, Name = "ImGui Scene" });
		Camera = factory.CameraBuilder.CreateCamera(new CameraCreationConfig {
			ProjectionType = CameraProjectionType.Orthographic,
			Position = new Location(0f, 0f, -CameraDistance),
			ViewDirection = Direction.Forward,
			UpDirection = Direction.Up,
			NearPlaneDistance = 0.5f,
			FarPlaneDistance = CameraDistance + 10f,
			Name = "ImGui Camera"
		});

		_meshBuilder = (LocalMeshBuilder) factory.MeshBuilder;
		_parkingVertexBuffer = _meshBuilder.CreateImGuiDynamicVertexBuffer(3, 3, "ImGui Parking Mesh");
		_parkingVertexBuffer.SetImGuiIndices(stackalloc ushort[] { 0, 1, 2 });
		_parkingView = _parkingVertexBuffer.CreateMesh(0..3, MeshViewBoundingBox);
		_parkingTexture = factory.TextureBuilder.CreateTexture(
			stackalloc TexelRgba32[] { new TexelRgba32(255, 255, 255, 255) },
			new TextureGenerationConfig { Dimensions = new(1, 1) },
			new TextureCreationConfig {
				IsLinearColorspace = true,
				GenerateMipMaps = false,
				Name = "ImGui Parking Texture"
			}
		);

		_context = ImGui.CreateContext();
		ImGui.SetCurrentContext(_context);
		var io = ImGui.GetIO();
		io.BackendFlags |= ImGuiBackendFlags.RendererHasTextures | ImGuiBackendFlags.HasMouseCursors;
		if (config.EnableDocking) io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
		if (config.EnableKeyboardNavigation) io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
		if (config.EnableGamepadNavigation) io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;
		ImGui.StyleColorsDark();
		_pristineStyle = *ImGui.GetStyle().Handle;
	}

	public void BeginFrame(TimeSpan deltaTime, ILatestInputRetriever input, Window window) {
		BeginFrame(deltaTime, input, window.Size, ((IRenderTarget) window).ViewportDimensions, window);
	}

	public void BeginFrame(TimeSpan deltaTime, ILatestInputRetriever input, XYPair<int> logicalSize, XYPair<int> framebufferSize) {
		BeginFrame(deltaTime, input, logicalSize, framebufferSize, null);
	}

	void BeginFrame(TimeSpan deltaTime, ILatestInputRetriever input, XYPair<int> logicalSize, XYPair<int> framebufferSize, Window? window) {
		ThrowIfDisposed();
		ImGui.SetCurrentContext(_context);

		_dpiScale = logicalSize is {X: > 0, Y: > 0}
			? new XYPair<float>(framebufferSize.X / (float) logicalSize.X, framebufferSize.Y / (float) logicalSize.Y)
			: new XYPair<float>(1f, 1f);

		var io = ImGui.GetIO();
		io.DisplaySize = new Vector2(framebufferSize.X, framebufferSize.Y);
		io.DisplayFramebufferScale = Vector2.One;
		io.DeltaTime = MathF.Max((float) deltaTime.TotalSeconds, 1f / 1000f);

		ApplyStyleScale(MathF.Max(_dpiScale.X, _dpiScale.Y));

		Camera.SetOrthographicHeight(framebufferSize.Y);
		Camera.SetAspectRatio(framebufferSize.Ratio ?? 1f);

		_inputPump.Pump(io, input, window, _dpiScale);
		ImGui.NewFrame();
	}

	void ApplyStyleScale(float newScale) {
		if (MathF.Abs(newScale - _appliedStyleScale) <= 0.001f) return;
		var style = ImGui.GetStyle();
		*style.Handle = _pristineStyle;
		style.ScaleAllSizes(newScale);
		style.FontScaleDpi = newScale;
		_appliedStyleScale = newScale;
	}

	public void EndFrame() {
		ThrowIfDisposed();
		ImGui.SetCurrentContext(_context);
		ImGui.Render();
		TranslateDrawData(ImGui.GetDrawData());
	}

	void TranslateDrawData(ImDrawDataPtr drawData) {
		ParkAllDrawCalls();
		_activeDrawCallCount = 0;

		if (drawData.IsNull) return;
		FlushPendingTextureDisposals();
		ServiceTextureCreations(drawData);

		var framebufferSize = new XYPair<int>(
			(int) (drawData.DisplaySize.X * drawData.FramebufferScale.X),
			(int) (drawData.DisplaySize.Y * drawData.FramebufferScale.Y)
		);
		if (framebufferSize.X <= 0 || framebufferSize.Y <= 0) {
			HideUnusedDrawCalls();
			ServiceTextureDestructions(drawData);
			return;
		}

		var displayPos = drawData.DisplayPos;
		var scale = drawData.FramebufferScale;
		var drawOrder = 0;
		var imGuiToWorldTransform = new Transform(
			translation: new Vect(framebufferSize.X * 0.5f, framebufferSize.Y * 0.5f, 0f),
			scaling: new Vect(-1f, -1f, 1f)
		);

		for (var listIndex = 0; listIndex < drawData.CmdListsCount; ++listIndex) {
			var cmdList = drawData.CmdLists[listIndex];
			var vertexCount = cmdList.VtxBuffer.Size;
			var indexCount = cmdList.IdxBuffer.Size;
			if (vertexCount == 0 || indexCount == 0) continue;

			var mesh = GetOrGrowMesh(listIndex, vertexCount, indexCount);
			mesh.SetImGuiVertices(new ReadOnlySpan<MeshVertexImGui>(cmdList.VtxBuffer.Data, vertexCount));
			mesh.SetImGuiIndices(new ReadOnlySpan<ushort>(cmdList.IdxBuffer.Data, indexCount));

			for (var cmdIndex = 0; cmdIndex < cmdList.CmdBuffer.Size; ++cmdIndex) {
				var cmd = cmdList.CmdBuffer.Data[cmdIndex];
				if (cmd.ElemCount == 0 || cmd.UserCallback != null) continue;

				var clipMinX = MathF.Max((cmd.ClipRect.X - displayPos.X) * scale.X, 0f);
				var clipMinY = MathF.Max((cmd.ClipRect.Y - displayPos.Y) * scale.Y, 0f);
				var clipMaxX = MathF.Min((cmd.ClipRect.Z - displayPos.X) * scale.X, framebufferSize.X);
				var clipMaxY = MathF.Min((cmd.ClipRect.W - displayPos.Y) * scale.Y, framebufferSize.Y);
				if (clipMaxX <= clipMinX || clipMaxY <= clipMinY) continue;

				var scissorLeft = (int) clipMinX;
				var scissorBottom = (int) (framebufferSize.Y - clipMaxY);
				var scissorWidth = (int) (clipMaxX - clipMinX);
				var scissorHeight = (int) (clipMaxY - clipMinY);

				var indexStart = (int) cmd.IdxOffset;
				var view = mesh.CreateMesh(indexStart..(indexStart + (int) cmd.ElemCount), MeshViewBoundingBox);

				var drawCall = GetOrCreateDrawCall(_activeDrawCallCount);
				var instance = drawCall.Instance;
				instance.Mesh = view;
				instance.SetTransform(imGuiToWorldTransform);
				drawCall.CurrentView = view;
				instance.SetScissorRect(new(scissorLeft, scissorBottom), new(scissorWidth, scissorHeight));
				var commandTexture = ResolveTexture(cmd.TexRef);
				if (drawCall.CurrentTexture != commandTexture) {
					_materialBuilder.SetImGuiMaterialColorMap(instance.GetOrCreatePrivateMaterial(), commandTexture);
					drawCall.CurrentTexture = commandTexture;
				}
				instance.DrawOrderDeferralAmount = Int32.Min(drawOrder, ModelInstance.MaxDrawOrderDeferralAmount);
				UnderlyingScene.Add(instance);

				++drawOrder;
				++_activeDrawCallCount;
			}
		}

		HideUnusedDrawCalls();
		ServiceTextureDestructions(drawData);
	}

	void ServiceTextureCreations(ImDrawDataPtr drawData) {
		var textureList = drawData.Textures;
		for (var i = 0; i < textureList.Size; ++i) {
			var texData = textureList.Data[i];
			if (texData.IsNull) continue;
			switch (texData.Status) {
				case ImTextureStatus.WantCreate:
					CreateTexture(texData);
					break;
				case ImTextureStatus.WantUpdates:
					UpdateTexture(texData);
					break;
			}
		}
	}

	void ServiceTextureDestructions(ImDrawDataPtr drawData) {
		var textureList = drawData.Textures;
		for (var i = 0; i < textureList.Size; ++i) {
			var texData = textureList.Data[i];
			if (texData.IsNull) continue;
			if (texData is {Status: ImTextureStatus.WantDestroy, UnusedFrames: > 0}) DestroyTexture(texData);
		}
	}

	void CreateTexture(ImTextureDataPtr texData) {
		var texelCount = texData.Width * texData.Height;
		var texels = new ReadOnlySpan<TexelRgba32>(texData.Pixels, texelCount);
		var texture = _factory.TextureBuilder.CreateTexture(
			texels,
			new TextureGenerationConfig { Dimensions = new(texData.Width, texData.Height) },
			new TextureCreationConfig {
				IsLinearColorspace = true,
				GenerateMipMaps = false,
				AllowsDynamicWrites = true,
				RenderingConfig = new(disableTextureRepeat: true, disableTexelBlending: false, Quality.Standard),
				Name = "ImGui Texture"
			}
		);
		_textures[texData.UniqueID] = texture;
		texData.SetTexID(new ImTextureID(texData.UniqueID));
		texData.SetStatus(ImTextureStatus.Ok);
	}

	void UpdateTexture(ImTextureDataPtr texData) {
		if (!_textures.TryGetValue(texData.UniqueID, out var texture)) {
			CreateTexture(texData);
			return;
		}
		var texelCount = texData.Width * texData.Height;
		texture.OverwriteTexels(new ReadOnlySpan<TexelRgba32>(texData.Pixels, texelCount));
		texData.SetStatus(ImTextureStatus.Ok);
	}

	void DestroyTexture(ImTextureDataPtr texData) {
		if (_textures.Remove(texData.UniqueID, out var texture)) _pendingTextureDisposals.Add(texture);
		texData.SetTexID(ImTextureID.Null);
		texData.SetStatus(ImTextureStatus.Destroyed);
	}

	void FlushPendingTextureDisposals() {
		if (_pendingTextureDisposals.Count == 0) return;
		for (var i = 0; i < _drawCallPool.Count; ++i) ParkDrawCallTexture(_drawCallPool[i]);
		for (var i = 0; i < _pendingTextureDisposals.Count; ++i) _pendingTextureDisposals[i].Dispose();
		_pendingTextureDisposals.Clear();
	}

	void ParkDrawCallTexture(DrawCall drawCall) {
		if (drawCall.CurrentTexture == _parkingTexture) return;
		_materialBuilder.SetImGuiMaterialColorMap(drawCall.Instance.GetOrCreatePrivateMaterial(), _parkingTexture);
		drawCall.CurrentTexture = _parkingTexture;
	}

	Texture ResolveTexture(ImTextureRef texRef) {
		var id = (int) texRef.GetTexID().Handle;
		return _textures.GetValueOrDefault(id);
	}

	DynamicVertexBuffer GetOrGrowMesh(int index, int vertexCount, int indexCount) {
		while (_meshPool.Count <= index) {
			_meshPool.Add(_meshBuilder.CreateImGuiDynamicVertexBuffer(InitialVertexCapacity, InitialIndexCapacity, "ImGui Draw List Mesh"));
			_meshVertexCapacities.Add(InitialVertexCapacity);
			_meshIndexCapacities.Add(InitialIndexCapacity);
		}

		var mesh = _meshPool[index];
		if (_meshVertexCapacities[index] < vertexCount) {
			var newCapacity = (int) (vertexCount * 1.5f);
			mesh.ResizeVertexBuffer(newCapacity);
			_meshVertexCapacities[index] = newCapacity;
		}
		if (_meshIndexCapacities[index] < indexCount) {
			var newCapacity = (int) (indexCount * 1.5f);
			mesh.ResizeIndexBuffer(newCapacity);
			_meshIndexCapacities[index] = newCapacity;
		}
		return mesh;
	}

	DrawCall GetOrCreateDrawCall(int index) {
		if (index < _drawCallPool.Count) return _drawCallPool[index];

		var material = _materialBuilder.AllocateImGuiMaterialInstance(_parkingTexture, "ImGui Draw Call Material");
		var instance = _factory.ObjectBuilder.CreateModelInstance(_parkingView, material, name: "ImGui Draw Call");

		var result = new DrawCall { Instance = instance, Material = material, CurrentTexture = _parkingTexture };
		_drawCallPool.Add(result);
		return result;
	}

	void HideUnusedDrawCalls() {
		for (var i = _activeDrawCallCount; i < _drawCallPool.Count; ++i) {
			var drawCall = _drawCallPool[i];
			UnderlyingScene.Remove(drawCall.Instance);
			ParkDrawCallTexture(drawCall);
		}
	}

	void ParkAllDrawCalls() {
		for (var i = 0; i < _drawCallPool.Count; ++i) {
			var drawCall = _drawCallPool[i];
			if (drawCall.CurrentView is not { } view) continue;
			var instance = drawCall.Instance;
			instance.Mesh = _parkingView;
			view.Dispose();
			drawCall.CurrentView = null;
		}
	}

	void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, typeof(ImGuiScene));

	public void Dispose() {
		if (_isDisposed) return;
		try {
			for (var i = 0; i < _drawCallPool.Count; ++i) UnderlyingScene.Remove(_drawCallPool[i].Instance);
			ParkAllDrawCalls();
			for (var i = 0; i < _drawCallPool.Count; ++i) {
				_drawCallPool[i].Instance.Dispose();
				_drawCallPool[i].Material.Dispose();
			}
			_drawCallPool.Clear();
			_parkingView.Dispose();
			_parkingVertexBuffer.Dispose();
			for (var i = 0; i < _meshPool.Count; ++i) _meshPool[i].Dispose();
			_meshPool.Clear();
			foreach (var kvp in _textures) kvp.Value.Dispose();
			_textures.Clear();
			for (var i = 0; i < _pendingTextureDisposals.Count; ++i) _pendingTextureDisposals[i].Dispose();
			_pendingTextureDisposals.Clear();
			_parkingTexture.Dispose();
			Camera.Dispose();
			UnderlyingScene.Dispose();
			if (!_context.IsNull) ImGui.DestroyContext(_context);
		}
		finally {
			_isDisposed = true;
		}
	}
}
