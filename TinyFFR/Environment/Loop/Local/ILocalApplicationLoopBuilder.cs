// Created on 2024-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Local;

public interface ILocalApplicationLoopBuilder : IApplicationLoopBuilder {
	ApplicationLoop IApplicationLoopBuilder.CreateLoop(in ApplicationLoopConfig config) => CreateLoop(new LocalApplicationLoopConfig(config));

	ApplicationLoop CreateLoop(int? frameRateCapHz = null, bool? waitForVsync = null, ReadOnlySpan<char> name = default) {
		return CreateLoop(new LocalApplicationLoopConfig {
			FrameRateCapHz = frameRateCapHz, 
			WaitForVSync = waitForVsync ?? LocalApplicationLoopConfig.DefaultWaitForVSync, 
			Name = name
		});
	}
	ApplicationLoop CreateLoop(in LocalApplicationLoopConfig config);
}