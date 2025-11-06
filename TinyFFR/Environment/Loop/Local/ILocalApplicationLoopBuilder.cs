// Created on 2024-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Local;

public interface ILocalApplicationLoopBuilder : IApplicationLoopBuilder {
	ApplicationLoop IApplicationLoopBuilder.CreateLoop(in ApplicationLoopCreationConfig config) => CreateLoop(new LocalApplicationLoopCreationConfig(config));

	ApplicationLoop CreateLoop(in LocalApplicationLoopCreationConfig config);
}