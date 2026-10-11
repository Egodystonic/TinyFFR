using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

static class OverlayManager {
	const float HueAdjustmentDegrees = 15f;
	const float BrightnessAdjustment = 0.1f;

	static CanvasScene _canvas;
	static Texture _pointLightIconTexture;
	static Texture _spotLightIconTexture;
	static Texture _minusButtonTexture;
	static Texture _plusButtonTexture;
	static Font _font;
	static FontPen _pen;

	static CanvasImage _pointLightIcon;
	static CanvasText _pointLightCountText;
	static CanvasImage _spotLightIcon;
	static CanvasText _spotLightCountText;
	static CanvasText _hueText;
	static CanvasImage _hueMinusButton;
	static CanvasImage _huePlusButton;
	static CanvasText _brightnessText;
	static CanvasImage _brightnessMinusButton;
	static CanvasImage _brightnessPlusButton;

	public static void CreateOverlay(ILocalTinyFfrFactory factory, CanvasScene canvas) {
		_canvas = canvas;
		_pointLightIconTexture = factory.AssetLoader.LoadCanvasTexture("point_light_icon.png");
		_spotLightIconTexture = factory.AssetLoader.LoadCanvasTexture("spot_light_icon.png");
		_minusButtonTexture = factory.AssetLoader.LoadCanvasTexture("button_minus.png");
		_plusButtonTexture = factory.AssetLoader.LoadCanvasTexture("button_plus.png");
		_font = factory.AssetLoader.LoadFont();
		_pen = _font.CreatePen(BuiltInFontPenStyle.WhiteWithOutline);

		_pointLightIcon = canvas.Add(_pointLightIconTexture);
		_pointLightIcon.SetPlacementPixels(Orientation2D.UpLeft, (20, 20), (80, 80));
		_pointLightCountText = canvas.Add("", _pen);
		_pointLightCountText.SetDockParent<CanvasImage>(_pointLightIcon);
		_pointLightCountText.SetPlacementFraction(Orientation2D.Right, (-0.15f, 0f), 0.45f);
		_pointLightCountText.ObjectAnchor = Orientation2D.Left;

		_spotLightIcon = canvas.Add(_spotLightIconTexture);
		_spotLightIcon.SetPlacementPixels(Orientation2D.UpLeft, (20, 110), (80, 80));
		_spotLightCountText = canvas.Add("", _pen);
		_spotLightCountText.SetDockParent<CanvasImage>(_spotLightIcon);
		_spotLightCountText.SetPlacementFraction(Orientation2D.Right, (-0.15f, 0f), 0.45f);
		_spotLightCountText.ObjectAnchor = Orientation2D.Left;

		_brightnessPlusButton = canvas.Add(_plusButtonTexture);
		_brightnessPlusButton.SetPlacementPixels(Orientation2D.DownRight, (20, 20), (60, 60));
		_brightnessMinusButton = canvas.Add(_minusButtonTexture);
		_brightnessMinusButton.SetPlacementPixels(Orientation2D.DownRight, (90, 20), (60, 60));
		_brightnessText = canvas.Add("", _pen);
		_brightnessText.SetPlacementPixels(Orientation2D.DownRight, (170, 33), 34);

		_huePlusButton = canvas.Add(_plusButtonTexture);
		_huePlusButton.SetPlacementPixels(Orientation2D.DownRight, (20, 90), (60, 60));
		_hueMinusButton = canvas.Add(_minusButtonTexture);
		_hueMinusButton.SetPlacementPixels(Orientation2D.DownRight, (90, 90), (60, 60));
		_hueText = canvas.Add("", _pen);
		_hueText.SetPlacementPixels(Orientation2D.DownRight, (170, 103), 34);
	}

	public static void UpdateOverlay(ResourceGroup lights, Light? mostRecentLight) {
		static void UpdateLightCountText(CanvasText text, int count, string lightTypeName) {
			Span<char> textBuffer = stackalloc char[32];
			textBuffer.TryWrite($"{count} {lightTypeName}{(count == 1 ? "" : "s")}", out var charsWritten);
			text.SetText(textBuffer[..charsWritten]);
		}

		UpdateLightCountText(_pointLightCountText, lights.PointLights.Count, "Point Light");
		UpdateLightCountText(_spotLightCountText, lights.SpotLights.Count, "Spot Light");

		var panelIsVisible = mostRecentLight.HasValue;
		_hueText.IsVisible = panelIsVisible;
		_hueMinusButton.IsVisible = panelIsVisible;
		_huePlusButton.IsVisible = panelIsVisible;
		_brightnessText.IsVisible = panelIsVisible;
		_brightnessMinusButton.IsVisible = panelIsVisible;
		_brightnessPlusButton.IsVisible = panelIsVisible;
		if (mostRecentLight is not { } light) return;

		Span<char> textBuffer = stackalloc char[32];
		textBuffer.TryWrite($"Hue: {light.ColorHue.Degrees:N0}°", out var charsWritten);
		_hueText.SetText(textBuffer[..charsWritten]);
		textBuffer.TryWrite($"Brightness: {PercentageUtils.ConvertFractionToPercentageString(light.Brightness, "N0")}", out charsWritten);
		_brightnessText.SetText(textBuffer[..charsWritten]);
	}

	public static void HandleInput(ILatestKeyboardAndMouseInputRetriever kbm, ResourceGroup lights, Light? mostRecentLight) {
		var cursorPosition = _canvas.ConvertRenderTargetCoordToLocal(kbm.MouseCursorPosition);
		foreach (var button in (ReadOnlySpan<CanvasImage>) [_hueMinusButton, _huePlusButton, _brightnessMinusButton, _brightnessPlusButton]) {
			button.Opacity = button.Contains(cursorPosition) ? 1f : 0.7f;
		}

		if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.MouseLeft) && mostRecentLight is { } light) {
			var clickedImage = _canvas.QueryProvider.GetTopmostObjectUnderLocalCoord<CanvasImage>(cursorPosition);
			if (clickedImage == _hueMinusButton) light.ColorHue -= HueAdjustmentDegrees;
			else if (clickedImage == _huePlusButton) light.ColorHue += HueAdjustmentDegrees;
			else if (clickedImage == _brightnessMinusButton) light.Brightness = MathF.Max(0f, light.Brightness - BrightnessAdjustment);
			else if (clickedImage == _brightnessPlusButton) light.Brightness += BrightnessAdjustment;
			UpdateOverlay(lights, mostRecentLight);
		}
	}

	public static void DisposeOverlay() {
		_pointLightCountText.Dispose();
		_pointLightIcon.Dispose();
		_spotLightCountText.Dispose();
		_spotLightIcon.Dispose();
		_hueText.Dispose();
		_hueMinusButton.Dispose();
		_huePlusButton.Dispose();
		_brightnessText.Dispose();
		_brightnessMinusButton.Dispose();
		_brightnessPlusButton.Dispose();
		_pen.Dispose();
		_font.Dispose();
		_pointLightIconTexture.Dispose();
		_spotLightIconTexture.Dispose();
		_minusButtonTexture.Dispose();
		_plusButtonTexture.Dispose();
	}
}
