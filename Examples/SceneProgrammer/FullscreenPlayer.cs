using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.World;

namespace SceneProgrammer;

readonly record struct PlaybackSummary(float DurationSeconds, int FrameCount) {
	public float AverageFramesPerSecond => DurationSeconds > 0f ? FrameCount / DurationSeconds : 0f;
}

static class FullscreenPlayer {
	const float HoldOnFinalFrameSeconds = 1f;

	public static PlaybackSummary Play(EditorSession session) {
		var factory = session.Factory;
		var display = factory.DisplayDiscoverer.Primary ?? throw new InvalidOperationException("No display connected.");

		using var window = factory.WindowBuilder.CreateWindow(display, WindowFullscreenStyle.FullscreenBorderless, title: "Scene Programmer");
		using var camera = factory.CameraBuilder.CreateCamera();
		using var controller = camera.CreateController<ProgrammedCameraController>();
		using var renderer = factory.RendererBuilder.CreateRenderer(session.Scene, camera, window);
		using var loop = factory.ApplicationLoopBuilder.CreateLoop(new LocalApplicationLoopCreationConfig {
			FrameRateCapHz = null,
			IterationShouldPumpSystemEventQueue = true
		});

		window.LockCursor = true;
		camera.Exposure = session.Environment.Exposure;
		session.Rendering.ApplyTo(renderer, camera);
		session.Track.ApplyTo(controller);

		var endTime = session.Track.PlaybackLengthSeconds + HoldOnFinalFrameSeconds;
		var elapsed = 0f;
		var frameCount = 0;
		while (true) {
			var deltaTime = loop.IterateOnce().AsDeltaTime();
			var input = loop.Input;
			if (input.UserQuitRequested || input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape) || elapsed >= endTime) break;

			elapsed += deltaTime;
			controller.Progress(deltaTime);
			renderer.Render();
			++frameCount;
		}

		return new PlaybackSummary(elapsed, frameCount);
	}
}
