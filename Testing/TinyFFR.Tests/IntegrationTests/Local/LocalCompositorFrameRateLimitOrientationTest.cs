// Created on 2026-08-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture]
class LocalCompositorFrameRateLimitOrientationTest {
	static readonly XYPair<int> BufferDimensions = (64, 64);

	[Test]
	public void RateLimitedOutputShouldMatchUnlimitedOutputOrientation() {
		using var factory = new LocalTinyFfrFactory();
		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer(BufferDimensions);
		using var markerTex = factory.TextureBuilder.CreateColorMap(StandardColor.White, false);

		using var canvasScene = factory.SceneBuilder.CreateCanvasScene();
		using var canvasRenderer = factory.RendererBuilder.CreateRenderer(canvasScene, buffer);
		using var marker = canvasScene.Add(markerTex);
		marker.CanvasAnchor = Orientation2D.Up;
		marker.WidthFraction = 0.5f;
		marker.HeightFraction = 0.2f;

		using var compositor = factory.RendererBuilder.CreateCompositor(buffer);
		compositor.Add(canvasRenderer, RenderCompositionType.Standard);

		var unlimitedCentroidRow = RenderAndFindMarkerCentroidRow(buffer, compositor);
		Console.WriteLine($"Unlimited marker centroid row: {unlimitedCentroidRow}");

		compositor.SetRendererFrameRateCap(canvasRenderer, 100_000);
		var limitedCentroidRow = RenderAndFindMarkerCentroidRow(buffer, compositor);
		Console.WriteLine($"Rate limited marker centroid row: {limitedCentroidRow}");

		Assert.That(unlimitedCentroidRow, Is.LessThan(BufferDimensions.Y / 2d), "Marker was not in the expected half when unlimited; the test's own premise is broken.");
		Assert.That(limitedCentroidRow, Is.EqualTo(unlimitedCentroidRow).Within(2d), "Rate limited output is not oriented the same way as unlimited output.");
	}

	[Test]
	public void RateLimitedSubAreaOutputShouldMatchUnlimitedOutputPlacement() {
		using var factory = new LocalTinyFfrFactory();
		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer(BufferDimensions);
		using var markerTex = factory.TextureBuilder.CreateColorMap(StandardColor.White, false);

		using var backdropScene = factory.SceneBuilder.CreateCanvasScene();
		using var backdropRenderer = factory.RendererBuilder.CreateRenderer(backdropScene, buffer);

		using var pipScene = factory.SceneBuilder.CreateCanvasScene();
		using var pipRenderer = factory.RendererBuilder.CreateRenderer(pipScene, buffer);
		using var marker = pipScene.Add(markerTex);
		marker.CanvasAnchor = Orientation2D.Up;
		marker.WidthFraction = 0.5f;
		marker.HeightFraction = 0.2f;
		pipRenderer.SetRenderSubAreaFraction(Orientation2D.UpLeft, (0.0f, 0.0f), (0.5f, 0.5f));

		using var compositor = factory.RendererBuilder.CreateCompositor(buffer);
		compositor.Add(backdropRenderer, RenderCompositionType.Standard);
		compositor.Add(pipRenderer, RenderCompositionType.RetainPreviousScenes);

		var unlimitedCentroid = RenderAndFindMarkerCentroid(buffer, compositor);
		Console.WriteLine($"Unlimited sub-area marker centroid: {unlimitedCentroid}");

		compositor.SetRendererFrameRateCap(pipRenderer, 100_000);
		var limitedCentroid = RenderAndFindMarkerCentroid(buffer, compositor);
		Console.WriteLine($"Rate limited sub-area marker centroid: {limitedCentroid}");

		Assert.That(unlimitedCentroid.Row, Is.LessThan(BufferDimensions.Y / 2d), "Sub-area marker was not in the expected half when unlimited; the test's own premise is broken.");
		Assert.That(unlimitedCentroid.Column, Is.LessThan(BufferDimensions.X / 2d), "Sub-area marker was not in the expected half when unlimited; the test's own premise is broken.");
		Assert.That(limitedCentroid.Row, Is.EqualTo(unlimitedCentroid.Row).Within(2d), "Rate limited sub-area output is not vertically placed the same way as unlimited output.");
		Assert.That(limitedCentroid.Column, Is.EqualTo(unlimitedCentroid.Column).Within(2d), "Rate limited sub-area output is not horizontally placed the same way as unlimited output.");
	}

	static double RenderAndFindMarkerCentroidRow(RenderOutputBuffer buffer, RendererCompositor compositor) {
		return RenderAndFindMarkerCentroid(buffer, compositor).Row;
	}

	static (double Row, double Column) RenderAndFindMarkerCentroid(RenderOutputBuffer buffer, RendererCompositor compositor) {
		var centroidRow = double.NaN;
		var centroidColumn = double.NaN;

		buffer.ReadNextFrame((size, texels) => {
			var weightedRowSum = 0d;
			var weightedColumnSum = 0d;
			var totalWeight = 0d;
			for (var y = 0; y < size.Y; ++y) {
				for (var x = 0; x < size.X; ++x) {
					var texel = texels[y * size.X + x];
					var weight = texel.R / 255d;
					weightedRowSum += weight * y;
					weightedColumnSum += weight * x;
					totalWeight += weight;
				}
			}
			if (totalWeight <= 0d) return;
			centroidRow = weightedRowSum / totalWeight;
			centroidColumn = weightedColumnSum / totalWeight;
		}, presentFrameTopToBottom: true);

		compositor.RenderAllAndWaitForGpu();

		Assert.That(centroidRow, Is.Not.NaN, "No marker pixels were rendered at all.");
		return (centroidRow, centroidColumn);
	}
}
