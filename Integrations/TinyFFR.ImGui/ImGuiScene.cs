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

/// <summary>
/// Hosts a Dear ImGui interface, drawing it over a TinyFFR scene.
/// </summary>
/// <remarks>
/// <para>
/// An interface is a separate scene with its own renderer, composited over your three-dimensional pass in the same way a canvas is.
/// Add the interface's renderer to the compositor last, with a composition type that retains the scenes drawn before it, so that
/// your scene shows through translucent panels.
/// </para>
/// <para>
/// Each frame, call one of the <c>BeginFrame</c> overloads, make your ImGui calls, then call <see cref="EndFrame"/>.
/// </para>
/// </remarks>
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
	readonly Dictionary<int, Texture> _userTextures = new();
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
	int _nextUserTextureId = -1;
	XYPair<int> _fullTargetSize;
	bool _isDisposed;

	/// <summary>
	/// The scene this interface's geometry is placed in.
	/// </summary>
	/// <remarks>
	/// Pass this to the renderer builder when creating a renderer for the interface, or use the <c>CreateRenderer</c> overload
	/// that takes an <see cref="ImGuiScene"/> directly and does it for you.
	/// </remarks>
	public Scene UnderlyingScene { get; }
	/// <summary>
	/// The camera the interface is viewed through.
	/// </summary>
	/// <remarks>
	/// This is an orthographic camera spanning the whole render target, maintained automatically as the target is resized. There
	/// is no reason to move it.
	/// </remarks>
	public Camera Camera { get; }
	/// <summary>
	/// The ImGui context this scene owns, for passing to ImGui functions that take one explicitly.
	/// </summary>
	/// <remarks>
	/// The context is made current at the start of every frame, so ordinary widget calls need not reference it.
	/// </remarks>
	public ImGuiContextPtr Context => _context;

	internal XYPair<int> LastFrameSubAreaOffset { get; private set; }
	internal XYPair<int> LastFrameSubAreaSize { get; private set; }

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
				DataType = TextureDataType.LinearData,
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

	/// <summary>
	/// Starts an ImGui frame sized to the given window.
	/// </summary>
	/// <remarks>
	/// Every ImGui call must be made between this and <see cref="EndFrame"/>. Note that ImGui text fields need real typed
	/// characters rather than raw key codes, so set the application loop's <c>EnableInputTextTranscription</c> to
	/// <see langword="true"/> or text boxes will not accept input; it is off by default.
	/// </remarks>
	/// <param name="deltaTime">How long has elapsed since the previous frame.</param>
	/// <param name="input">The input state accumulated since the previous frame, which is forwarded to ImGui as its keyboard, mouse and gamepad state.</param>
	/// <param name="window">The window being drawn to, whose size and framebuffer dimensions are used for the display size.</param>
	public void BeginFrame(TimeSpan deltaTime, ILatestInputRetriever input, Window window) {
		var framebufferSize = ((IRenderTarget) window).ViewportDimensions;
		BeginFrame(deltaTime, input, window.Size, framebufferSize, XYPair<int>.Zero, framebufferSize, window);
	}

	/// <summary>
	/// Starts an ImGui frame sized to the given window, confined to the region the given renderer draws in to.
	/// </summary>
	/// <remarks>
	/// Use this where the interface should occupy only part of the window, as set by the renderer's render sub-area.
	/// </remarks>
	/// <param name="deltaTime">How long has elapsed since the previous frame.</param>
	/// <param name="input">The input state accumulated since the previous frame, which is forwarded to ImGui as its keyboard, mouse and gamepad state.</param>
	/// <param name="window">The window being drawn to, whose size and framebuffer dimensions are used for the display size.</param>
	/// <param name="renderer">The renderer this interface will be drawn with. Its sub-area is adopted as the region the interface occupies, and the renderer is told that the sub-area is being applied here rather than by it.</param>
	public void BeginFrame(TimeSpan deltaTime, ILatestInputRetriever input, Window window, Renderer renderer) {
		AdoptSubAreaFrom(renderer);
		BeginFrame(deltaTime, input, window.Size, ((IRenderTarget) window).ViewportDimensions, renderer.GetRenderSubAreaPixelOffset(), renderer.GetRenderSubAreaPixelDimensions(), window);
	}

	/// <summary>
	/// Starts an ImGui frame for a drawing area of the given size, confined to the region the given renderer draws in to.
	/// </summary>
	/// <param name="deltaTime">How long has elapsed since the previous frame.</param>
	/// <param name="input">The input state accumulated since the previous frame, which is forwarded to ImGui as its keyboard, mouse and gamepad state.</param>
	/// <param name="logicalSize">The size of the drawing area in the operating system's own units. Together with <paramref name="framebufferSize"/> this is what tells ImGui the display's scaling factor.</param>
	/// <param name="framebufferSize">The size of the drawing area in real pixels.</param>
	/// <param name="renderer">The renderer this interface will be drawn with. Its sub-area is adopted as the region the interface occupies, and the renderer is told that the sub-area is being applied here rather than by it.</param>
	public void BeginFrame(TimeSpan deltaTime, ILatestInputRetriever input, XYPair<int> logicalSize, XYPair<int> framebufferSize, Renderer renderer) {
		AdoptSubAreaFrom(renderer);
		BeginFrame(deltaTime, input, logicalSize, framebufferSize, renderer.GetRenderSubAreaPixelOffset(), renderer.GetRenderSubAreaPixelDimensions(), null);
	}

	// Filament decides what a MaterialInstance scissor rectangle is relative to via
	// Renderer.cpp's `setScissorViewport(useIntermediateBuffer ? xvp : vp)`, directly beneath a FIXME conceding the
	// choice is unreliable when rendering straight into a swapchain. ImGui scenes use Canvas quality, which disables
	// post-processing and therefore takes exactly that unreliable branch: with a non-zero Filament viewport offset and
	// the view composited into a shared beginFrame/endFrame, our per-command scissors silently clip away nearly all of
	// the UI. Verified three ways -- it renders correctly uncomposited, correctly with post-processing forced on, and
	// correctly at any zero-offset viewport.
	// So we never give Filament an offset viewport for ImGui. The renderer keeps reporting the sub-area the caller
	// asked for (GetRenderSubArea* below still work), but its GPU viewport is forced to the whole target and we place
	// the UI ourselves: the orthographic camera covers the full target and the ImGui-to-world transform and scissor
	// rectangles are offset into the sub-area instead.
	static void AdoptSubAreaFrom(Renderer renderer) => renderer.MarkSubAreaAsHandledDownstream(true);

	/// <summary>
	/// Starts an ImGui frame for a drawing area of the given size.
	/// </summary>
	/// <remarks>
	/// This is the windowless form, for drawing to a render output buffer or when hosted inside another user interface
	/// framework, where the sizes must be supplied rather than read from a window.
	/// </remarks>
	/// <param name="deltaTime">How long has elapsed since the previous frame.</param>
	/// <param name="input">The input state accumulated since the previous frame, which is forwarded to ImGui as its keyboard, mouse and gamepad state.</param>
	/// <param name="logicalSize">The size of the drawing area in the operating system's own units. Together with <paramref name="framebufferSize"/> this is what tells ImGui the display's scaling factor.</param>
	/// <param name="framebufferSize">The size of the drawing area in real pixels.</param>
	public void BeginFrame(TimeSpan deltaTime, ILatestInputRetriever input, XYPair<int> logicalSize, XYPair<int> framebufferSize) {
		BeginFrame(deltaTime, input, logicalSize, framebufferSize, XYPair<int>.Zero, framebufferSize, null);
	}

	/// <summary>
	/// Starts an ImGui frame for a drawing area of the given size, confined to an explicit sub-area of it.
	/// </summary>
	/// <param name="deltaTime">How long has elapsed since the previous frame.</param>
	/// <param name="input">The input state accumulated since the previous frame, which is forwarded to ImGui as its keyboard, mouse and gamepad state.</param>
	/// <param name="logicalSize">The size of the drawing area in the operating system's own units. Together with <paramref name="framebufferSize"/> this is what tells ImGui the display's scaling factor.</param>
	/// <param name="framebufferSize">The size of the drawing area in real pixels.</param>
	/// <param name="subAreaOffsetFromTopLeft">Where the interface's region begins, in pixels from the top-left of the drawing area.</param>
	/// <param name="subAreaDimensions">How large the interface's region is, in pixels.</param>
	public void BeginFrame(TimeSpan deltaTime, ILatestInputRetriever input, XYPair<int> logicalSize, XYPair<int> framebufferSize, XYPair<int> subAreaOffsetFromTopLeft, XYPair<int> subAreaDimensions) {
		BeginFrame(deltaTime, input, logicalSize, framebufferSize, subAreaOffsetFromTopLeft, subAreaDimensions, null);
	}

	void BeginFrame(TimeSpan deltaTime, ILatestInputRetriever input, XYPair<int> logicalSize, XYPair<int> framebufferSize, XYPair<int> subAreaOffset, XYPair<int> subAreaSize, Window? window) {
		ThrowIfDisposed();
		ImGui.SetCurrentContext(_context);

		_dpiScale = logicalSize is {X: > 0, Y: > 0}
			? new XYPair<float>(framebufferSize.X / (float) logicalSize.X, framebufferSize.Y / (float) logicalSize.Y)
			: new XYPair<float>(1f, 1f);

		var io = ImGui.GetIO();
		io.DisplaySize = new Vector2(subAreaSize.X, subAreaSize.Y);
		io.DisplayFramebufferScale = Vector2.One;
		io.DeltaTime = MathF.Max((float) deltaTime.TotalSeconds, 1f / 1000f);

		LastFrameSubAreaOffset = subAreaOffset;
		LastFrameSubAreaSize = subAreaSize;
		_fullTargetSize = framebufferSize;

		ApplyStyleScale(MathF.Max(_dpiScale.X, _dpiScale.Y));

		// The camera always spans the whole render target, never just the sub-area, because the sub-area is applied to
		// the geometry rather than to Filament's viewport (see the comment on AdoptSubAreaFrom above).
		Camera.SetOrthographicHeight(framebufferSize.Y);
		Camera.SetAspectRatio(framebufferSize.Ratio ?? 1f);

		_inputPump.Pump(io, input, window, _dpiScale, subAreaOffset);
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

	/// <summary>
	/// Ends the ImGui frame and turns the widgets drawn since <c>BeginFrame</c> in to renderable geometry.
	/// </summary>
	/// <remarks>
	/// Nothing appears until this is called. The geometry is placed in <see cref="UnderlyingScene"/>, so the interface is drawn
	/// by whichever renderer targets that scene.
	/// </remarks>
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
		// Offsets the UI into the sub-area, because the camera and Filament viewport both span the whole target
		// rather than the sub-area itself -- see AdoptSubAreaFrom.
		var imGuiToWorldTransform = new Transform(
			translation: new Vect(_fullTargetSize.X * 0.5f - LastFrameSubAreaOffset.X, _fullTargetSize.Y * 0.5f - LastFrameSubAreaOffset.Y, 0f),
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

				// Scissor rectangles are in whole-target space (offset into the sub-area, Y flipped to Filament's
				// bottom-left origin) because the Filament viewport spans the whole target -- see AdoptSubAreaFrom.
				var scissorLeft = LastFrameSubAreaOffset.X + (int) clipMinX;
				var scissorBottom = _fullTargetSize.Y - (LastFrameSubAreaOffset.Y + (int) clipMaxY);
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
				DataType = TextureDataType.LinearData,
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
		if (_textures.TryGetValue(id, out var atlasTexture)) return atlasTexture;
		if (_userTextures.TryGetValue(id, out var userTexture)) return userTexture;
		return _parkingTexture;
	}

	/// <summary>
	/// Registers a texture so that it can be drawn inside an ImGui widget, returning the identifier ImGui refers to it by.
	/// </summary>
	/// <remarks>
	/// Pass the returned identifier to <see cref="Hexa.NET.ImGui.ImGui.Image(ImTextureRef, Vector2)"/>
	/// (e.g. <c>ImGui.Image(new ImTextureRef(null, registeredTexture), ...)</c>).
	/// The texture is not owned by this scene and must still be disposed by you, but unregister it first.
	/// </remarks>
	/// <param name="texture">The texture to make available to ImGui.</param>
	public ImTextureID RegisterTexture(Texture texture) {
		ThrowIfDisposed();
		var id = _nextUserTextureId--;
		_userTextures[id] = texture;
		return new ImTextureID(id);
	}

	/// <summary>
	/// Unregisters a texture previously made available to ImGui.
	/// </summary>
	/// <remarks>
	/// Do this before disposing the texture itself. Unregistering an identifier that is not registered does nothing.
	/// </remarks>
	/// <param name="id">The identifier returned when the texture was registered.</param>
	public void UnregisterTexture(ImTextureID id) {
		ThrowIfDisposed();
		if (!_userTextures.Remove((int) id.Handle, out var texture)) return;
		for (var i = 0; i < _drawCallPool.Count; ++i) {
			if (_drawCallPool[i].CurrentTexture != texture) continue;
			ParkDrawCallTexture(_drawCallPool[i]);
		}
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

	/// <summary>
	/// Disposes this scene, releasing its ImGui context and every resource it created for drawing.
	/// </summary>
	/// <remarks>
	/// Any renderer targeting <see cref="UnderlyingScene"/> must be disposed first.
	/// </remarks>
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
			_userTextures.Clear();
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
