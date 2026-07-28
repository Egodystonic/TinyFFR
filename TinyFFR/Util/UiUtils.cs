// Created on 2026-05-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR;

public static class UiUtils {
	public static XYPair<int> TranslateAnchoredCanvasOffset(XYPair<int> canvasSize, DiagonalOrientation2D canvasOrigin, Orientation2D anchor, XYPair<int> anchorOffset) {
		if (!Enum.IsDefined(canvasOrigin) || canvasOrigin == DiagonalOrientation2D.None) {
			throw new ArgumentOutOfRangeException(nameof(canvasOrigin), canvasOrigin, $"Canvas origin must be one of {DiagonalOrientation2D.DownLeft}, {DiagonalOrientation2D.DownRight}, {DiagonalOrientation2D.UpLeft} or {DiagonalOrientation2D.UpRight}.");
		}

		// Step 1: Determine the coord assuming the TinyFFR convention of bottom-left being (0, 0)
		var downLeftOriginResult = new XYPair<int>(
			anchor.GetHorizontalComponent() switch {
				HorizontalOrientation2D.Right => canvasSize.X - anchorOffset.X,
				HorizontalOrientation2D.Left => anchorOffset.X,
				_ => (canvasSize.X / 2) + anchorOffset.X,
			},
			anchor.GetVerticalComponent() switch {
				VerticalOrientation2D.Up => canvasSize.Y - anchorOffset.Y,
				VerticalOrientation2D.Down => anchorOffset.Y,
				_ => (canvasSize.Y / 2) + anchorOffset.Y,
			}
		);

		// Step 2: Convert for the actually-requested origin point
		return new XYPair<int>(
			canvasOrigin.GetHorizontalComponent() switch {
				HorizontalOrientation2D.Right => canvasSize.X - downLeftOriginResult.X,
				_ => downLeftOriginResult.X
			},
			canvasOrigin.GetVerticalComponent() switch {
				VerticalOrientation2D.Up => canvasSize.Y - downLeftOriginResult.Y,
				_ => downLeftOriginResult.Y
			}
		);
	}
	
	public static XYPair<float> TranslateAnchoredCanvasOffsetNormalized(DiagonalOrientation2D canvasOrigin, Orientation2D anchor) {
		return TranslateAnchoredCanvasOffset((2, 2), canvasOrigin, anchor, (0, 0)).Cast<float>().ScaledBy(0.5f);
	}
	
	public static XYPair<int> GetAnchoredCanvasAreaStartCoord(XYPair<int> canvasSize, DiagonalOrientation2D canvasOrigin, Orientation2D anchor, XYPair<int> anchorOffset, XYPair<int> elementArea) {
		var anchorCoord = TranslateAnchoredCanvasOffset(canvasSize, canvasOrigin, anchor, anchorOffset);
		var anchorH = anchor.GetHorizontalComponent();
		var anchorV = anchor.GetVerticalComponent();
		var canvasH = canvasOrigin.GetHorizontalComponent();
		var canvasV = canvasOrigin.GetVerticalComponent();
		
		return new XYPair<int>(
			(anchorH, canvasH) switch {
				(HorizontalOrientation2D.None, _) => anchorCoord.X - elementArea.X / 2,
				_ when anchorH != canvasH => anchorCoord.X - elementArea.X,
				_ => anchorCoord.X
			},
			(anchorV, canvasV) switch {
				(VerticalOrientation2D.None, _) => anchorCoord.Y - elementArea.Y / 2,
				_ when anchorV != canvasV => anchorCoord.Y - elementArea.Y,
				_ => anchorCoord.Y
			}
		);
	}
}