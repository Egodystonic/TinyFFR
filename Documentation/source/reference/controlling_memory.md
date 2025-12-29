---
title: Controlling Memory Usage
description: Snippet demonstrating how to reduce memory consumption
---

## Code

```csharp
var factory = new LocalTinyFfrFactory(
	factoryConfig: new LocalTinyFfrFactoryConfig {  
		MemoryUsageRubric = MemoryUsageRubric.UseLessMemory // (1)!
	}
);
```

1. 	Change this value to one of the following according to your desired behaviour: `Standard`, `UseLessMemory`, `UseSignificantlyLessMemory`

## Explanation

By default, TinyFFR uses more RAM to increase performance of your application. However, in some cases you may wish to reduce that memory usage instead, at the potential cost of framerate/performance.

* `MemoryUsageRubric.Standard` is the default setting. TinyFFR will use a larger amount of RAM to create internal buffers to speed up rendering performance.
* `MemoryUsageRubric.UseLessMemory` can be used to force TinyFFR to reduce its RAM usage. The tradeoff is you might see reduced performance on more complex or demanding scenes.
* `MemoryUsageRubric.UseSignificantlyLessMemory` should be used if you're only using TinyFFR to display very simple scenes (e.g. as a model viewer or similar). Attempting to create complex scenes with this option may result in artifacting as frame buffer data must be swapped out more often. However, this setting significantly reduces RAM usage.
