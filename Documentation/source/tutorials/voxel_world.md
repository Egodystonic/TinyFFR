---
title: Voxel World
description: A showcase example that generates an infinite voxel world, streams it in around a first-person player on background threads, and does so without generating garbage.
---

<div class="grid cards" markdown>

-   :octicons-beaker-16:{ : style="margin-right:0.3em" } __Overview__ [__View Code on Github &nbsp; :simple-github:__](https://github.com/Egodystonic/TinyFFR/tree/main/Examples/VoxelWorld){ : style="position: absolute; right: 1em;" }

    ![Image showing a blocky voxel landscape of grassy hills, with a debug readout in the top-left corner.](voxel_world_preview.png){ align=right : style="max-width:80%; margin: 0em; margin-left: 1em;" }
    
    This uses TinyFFR to demonstrate a simulated voxel-based game world.

</div>

## :material-flask-outline: Demonstrated Features

* [:material-book-open-page-variant: Dynamic Meshes & Mutable Grids](../reference/dynamic_meshes_and_mutable_grids.md#dynamic-vertex-buffers): Writing generated geometry in to dynamic vertex buffers, and re-using them as the world streams in and out;
* [:material-book-open-page-variant: First-Person Camera Controller](../reference/camera_controller_first_person.md): Looking around with a first-person camera controller;
* [:material-book-open-page-variant: Keyboard / Mouse Input](../reference/keyboard_and_mouse_input.md) | [:material-book-open-page-variant: Gamepad Input](../reference/gamepad_input.md): Keyboard, mouse, and gamepad controls;
* [:material-book-open-page-variant: Directional Lights](../reference/directional_lights.md) | [:material-book-open-page-variant: Fog](../reference/fog.md): A sun with shadows, and distance fog;
* [:material-book-open-page-variant: Canvas Scenes (2D)](../reference/canvas_scenes.md): A 2D debug readout drawn over the 3D scene;
* [:material-book-open-page-variant: Avoiding GC Stutter](../reference/avoiding_gc_stutter.md): Keeping the application free of garbage collection pauses after start-up.

## Running It

Download (or clone) the whole [`Examples/VoxelWorld`](https://github.com/Egodystonic/TinyFFR/tree/main/Examples/VoxelWorld) folder, then open a terminal in that folder and run `dotnet run -c Release`.

Every world is generated from a *seed* number; the same seed always produces the same world. To explore a different world, pass a seed after `--`; for example: `dotnet run -c Release -- 1234`.

| Action | Keyboard & Mouse | Gamepad |
| :-- | :-- | :-- |
| Look around | Mouse | Right stick |
| Move | W / A / S / D | Left stick |
| Sprint | Left Shift (hold) | Left stick button (hold) |
| Jump | Space | A |
| Exit | Escape | |

You can walk up single blocks without jumping, and jump up ledges two blocks high.

??? bug "TypeLoadWorkaround.cs (Crash with Exit Code 139)"
	On Linux, a bug in the .NET 9 and .NET 10 runtimes can make this example crash immediately on startup, before any of its code runs. There's no exception or error message; the process simply exits with a "Segmentation fault" (exit code `139`).

	The bug ([dotnet/runtime#130661](https://github.com/dotnet/runtime/issues/130661)) is in how the runtime loads certain struct types, and it's triggered when a program's `Main` method (including top-level statements like this example's `Program.cs`) uses some of TinyFFR's types before the types they contain have been loaded; such as canvas scenes, canvas images and text, and model bundles. It has been fixed in .NET 11.

	Until then, the example works around it with [`TypeLoadWorkaround.cs`](https://github.com/Egodystonic/TinyFFR/raw/main/Examples/VoxelWorld/TypeLoadWorkaround.cs), which makes the runtime load TinyFFR's types in a safe order before the program starts (it runs automatically via a [module initializer](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/general#moduleinitializer-attribute)).

	Windows and MacOS are not affected.

## Architecture

The world is divided in to *chunks*: columns of 16 × 16 blocks, 256 blocks tall. Near the player, each chunk is drawn as a full-detail mesh; further away, the terrain is drawn as coarser, low-detail *tiles* (see [Distant Terrain](#distant-terrain)), so that the world can be seen out to around two and a half kilometres. As the player moves, chunks and tiles that fall too far behind are removed and their GPU resources re-used for new ones ahead.

How the terrain itself is generated (from layers of noise, with biomes) isn't specific to TinyFFR, so it isn't covered on this page; see `TerrainGenerator.cs` if you're curious.

The expensive work (generating each chunk's blocks and building its mesh) happens on TinyFFR's background *worker* threads, so that the main thread can keep rendering frames smoothly. The main thread only hands out jobs, uploads finished meshes to the GPU, moves the player, and renders:

```mermaid
flowchart TB
	subgraph main ["Main thread (every frame)"]
		direction TB
		iterate["Iterate the application loop"]:::env
		manager["World streamer:<br/>work out which chunks & tiles are needed"]:::world
		upload["Upload finished meshes<br/>to their vertex buffers"]:::asset
		player["Player:<br/>input, gravity, collision"]:::step
		hud["Update the debug readout"]:::render
		render["Render the world and readout"]:::render
		iterate --> manager --> upload --> player --> hud --> render --> iterate
	end
	subgraph workers ["TinyFFR worker threads"]
		direction TB
		generate["Terrain generator:<br/>fill a chunk's blocks,<br/>or sample a tile's heights"]:::step
		mesh["Chunk / far terrain mesher:<br/>build vertices & triangles"]:::step
		generate --> mesh
	end
	manager -- "DispatchWorkerThreadJob()" --> generate
	mesh -- "completed job" --> upload

	classDef env fill:#219ebc,fill-opacity:0.3,stroke:#219ebc,stroke-width:2px
	classDef asset fill:#f4a300,fill-opacity:0.3,stroke:#e09400,stroke-width:2px
	classDef world fill:#2a9d5c,fill-opacity:0.3,stroke:#2a9d5c,stroke-width:2px
	classDef render fill:#8e5cc4,fill-opacity:0.3,stroke:#8e5cc4,stroke-width:2px
	classDef step fill:#8a939b,fill-opacity:0.2,stroke:#8a939b,stroke-width:2px
```

This is the "Streaming Open World" pattern from [Scaling Up: Application Flow](application_flow.md#streaming-open-world), except that the work here is generating data rather than loading asset files, so instead of TinyFFR's [asynchronous loading](../reference/asynchronous_loading.md) functions, the example hands its own jobs to the same worker threads TinyFFR uses for asynchronous loading.

The code is split in to one file per responsibility:

| File | Responsibility |
| :-- | :-- |
| `Program.cs` | Creates every resource, runs the application loop, and disposes everything at the end. |
| `WorldSettings.cs` | Constants that control the world (chunk size, render distance, seed, etc.); a good place to start tinkering. |
| `BlockType.cs` | The kinds of block, and which part of the texture atlas each face of each block uses. |
| `Noise.cs` | Seeded 2D noise, used to generate the terrain's shape. |
| `TerrainGenerator.cs` | Decides each column's height and biome, and fills a chunk's blocks. |
| `TerrainMesh.cs` | Mesh data shared by both meshers, and the code that writes one textured square (quad) in to it. |
| `ChunkMesher.cs` | Turns a chunk's blocks in to vertices and triangles. |
| `FarTerrainMesher.cs` | Builds a low-detail terrain tile's mesh straight from the terrain generator's heights. |
| `TerrainJobScheduler.cs` | Sends terrain jobs to TinyFFR's worker threads, and collects the finished results. |
| `Chunk.cs` / `TerrainTile.cs` / `Region.cs` | One chunk's blocks, one low-detail terrain tile, and coordinate types. Chunks and tiles each have a `GpuMeshSlot`. |
| `GpuMeshSlot.cs` | One dynamic vertex buffer, the mesh drawing it, and the model instance placing it in the scene. |
| `ChunkManager.cs` | The near level: decides which full-detail chunks are needed, and answers "is this block solid?" for collision. |
| `TerrainLevel.cs` | One low-detail level (used for both the mid and far levels): decides which of its tiles are needed and visible. |
| `WorldStreamer.cs` | Ties the three levels together, and hands each finished mesh to the level that asked for it. |
| `Player.cs` | Mouse/gamepad look, movement, gravity, jumping, and collision. |
| `DebugHud.cs` | The 2D readout in the top-left corner. |
| `TypeLoadWorkaround.cs` | A workaround for a .NET runtime bug (see [above](#running-it)). |

## Building Meshes

Each chunk is turned in to a list of square faces (only the faces of blocks that touch air are included, since the rest can never be seen). Each face is written as four `MeshVertex`es and two `VertexTriangle`s (see [Creating Meshes](../reference/creating_meshes.md#meshvertex-vertextriangle)):

```csharp
public static bool TryAddQuad(TerrainMeshData output, Vector3 faceCentre, in FaceDefinition face, float width, float height, int tile) {
	if (output.VertexCount + 4 > output.Vertices.Length) {
		output.Truncated = true;
		return false;
	}

	BlockTextures.GetTileUvs(tile, out var uMin, out var vMin, out var uMax, out var vMax);
	var halfTangent = face.Tangent * (width * 0.5f);
	var halfBitangent = face.Bitangent * (height * 0.5f);
	var corner0 = faceCentre - halfTangent - halfBitangent;
	var corner1 = faceCentre + halfTangent - halfBitangent;
	var corner2 = faceCentre + halfTangent + halfBitangent;
	var corner3 = faceCentre - halfTangent + halfBitangent;

	var firstVertex = output.VertexCount;
	output.Vertices[firstVertex] = new MeshVertex(Location.FromVector3(corner0), new XYPair<float>(uMin, vMin), face.TangentRotation);
	output.Vertices[firstVertex + 1] = new MeshVertex(Location.FromVector3(corner1), new XYPair<float>(uMax, vMin), face.TangentRotation);
	output.Vertices[firstVertex + 2] = new MeshVertex(Location.FromVector3(corner2), new XYPair<float>(uMax, vMax), face.TangentRotation);
	output.Vertices[firstVertex + 3] = new MeshVertex(Location.FromVector3(corner3), new XYPair<float>(uMin, vMax), face.TangentRotation);
	output.VertexCount += 4;

	output.Triangles[output.TriangleCount] = new VertexTriangle(firstVertex, firstVertex + 1, firstVertex + 2);
	output.Triangles[output.TriangleCount + 1] = new VertexTriangle(firstVertex, firstVertex + 2, firstVertex + 3);
	output.TriangleCount += 2;

	var lowest = (int) MathF.Floor(MathF.Min(corner0.Y, corner2.Y));
	var highest = (int) MathF.Ceiling(MathF.Max(corner0.Y, corner2.Y));
	output.LowestY = Math.Min(output.LowestY, lowest);
	output.HighestY = Math.Max(output.HighestY, highest);
	return true;
}
```

* Each vertex has a position, texture coordinates (pointing at the right tile in the texture atlas), and a tangent rotation describing which way the face points. The tangent rotations for the six possible face directions are calculated once, when the program starts, with `MeshVertex.CalculateTangentRotation()` (see `FaceDefinition` in `TerrainMesh.cs`).
* The mesh data is written in to plain arrays (rather than straight in to a TinyFFR resource), because it's built on a worker thread; it's copied in to a dynamic vertex buffer on the main thread afterwards (see [Uploading to the GPU](#uploading-to-the-gpu)).

All of a block type's textures live side-by-side in one image, `block_atlas.png`, so that the whole world can be drawn with a single [material](../reference/creating_materials.md). Each tile in the atlas is surrounded by a border of repeated edge pixels, which stops neighbouring tiles bleeding in to each other in the lower-resolution [mipmaps](../reference/loading_textures.md) used for distant blocks.

## Background Workers

TinyFFR keeps a pool of worker threads (one per CPU core) for its own [asynchronous loading](../reference/asynchronous_loading.md), and `ResourceAllocator.DispatchWorkerThreadJob()` lets an application share that pool for its own background work. `TerrainJobScheduler` uses it to generate and mesh each chunk (or build a distant tile's coarse mesh) in the background:

```csharp
bool TryDispatch(Chunk? chunk, TerrainTile? tile, int generation) {
	if (_freeJobCount == 0) return false;
	var job = _freeJobs[--_freeJobCount];
	job.Chunk = chunk;
	job.Tile = tile;
	job.Generation = generation;
	_inFlight[_inFlightCount++] = _allocator.DispatchWorkerThreadJob<TerrainJob, TerrainJob>(job, &Run);
	return true;
}

static TerrainJob? Run(TerrainJob? job) {
	if (job!.Chunk is { } chunk) {
		job.Generator.GenerateChunk(chunk.Coord, chunk.Blocks, chunk.ColumnHeights);
		ChunkMesher.BuildMesh(chunk.Blocks, chunk.ColumnHeights, job.MeshData);
	}
	else if (job.Tile is { } tile) {
		FarTerrainMesher.BuildMesh(tile, job.Generator, job.CellHeights, job.CellSurfaces, job.MeshData);
	}
	return job;
}
```

`DispatchWorkerThreadJob()` returns a `TinyFfrAsyncOperation`. Each frame, the world streamer collects any finished jobs (at most four per frame, so uploading never causes a stutter):

```csharp
public bool TryTakeResult(out TerrainJobResult result) {
	for (var i = 0; i < _inFlightCount; ++i) {
		if (!_inFlight[i].IsResultAvailable) continue;
		var job = _inFlight[i].GetResultAndDisposeOperation()!;
		_inFlight[i] = _inFlight[--_inFlightCount];
		_inFlight[_inFlightCount] = default;
		result = new TerrainJobResult(job.Chunk, job.Tile, job.Generation, job.MeshData, job);
		return true;
	}
	result = default;
	return false;
}
```

A few rules keep this safe and fast:

* **Jobs never touch TinyFFR resources.** They only produce plain data (block arrays, vertices and triangles); creating and modifying TinyFFR resources happens on the main thread.
* **There are only a fixed number of jobs.** Each `TerrainJob` holds everything a job writes in to (its mesh data and scratch arrays), and every job is created when the program starts. When none are free, no more work is dispatched until a finished job's mesh has been uploaded, which naturally limits how much work is in flight at once; and because the missing chunks are always visited nearest-first (see below), the nearest ones are always dispatched first.
* **The job is passed as the `context`, and `Run` as a function pointer** (`&Run`), so dispatching a job doesn't allocate a closure.
* **Every operation is consumed exactly once**, with `GetResultAndDisposeOperation()`. `IsResultAvailable` checks whether a job has finished without waiting for it; and when the application exits, `Dispose()` waits for any jobs still in flight.

In testing, this streamed the world in just as quickly as a dedicated pool of the example's own threads did (the full world was loaded around 2.5 seconds after start-up either way, with the same frame times), with eight fewer threads in the process.

## Streaming

Rather than creating and disposing TinyFFR resources as chunks come and go, every chunk and tile is drawn by one of a fixed number of *slots*, each created when the program starts. A slot owns a `GpuMeshSlot` (see [Uploading to the GPU](#uploading-to-the-gpu)), and is simply re-used whenever a new chunk needs it. Taking a chunk off-screen (or putting it back on) is just a matter of removing its model instance from the scene, or adding it again:

```csharp
public void Show() {
	if (IsVisible) return;
	_scene.Add(_instance);
	IsVisible = true;
}

public void Hide() {
	if (!IsVisible) return;
	_scene.Remove(_instance);
	IsVisible = false;
}
```

Removing a model instance from a scene doesn't dispose it (see [Scenes](../reference/scenes.md)), so it can be added back at any time, at almost no cost. The chunks to load are visited in order of distance from the player, so the chunks nearest the player are always dispatched to the worker threads first.

## Uploading to the GPU

Every chunk and tile slot owns a `GpuMeshSlot`: a [dynamic vertex buffer](../reference/dynamic_meshes_and_mutable_grids.md#dynamic-vertex-buffers), a mesh created from it, and a model instance that draws the mesh. When a mesh is ready, it's copied in to the buffer:

```csharp
public bool Upload(TerrainMeshData meshData, Location position, float footprintSize) {
	var resized = EnsureCapacity(meshData.VertexCount, meshData.TriangleCount);

	if (meshData.VertexCount > 0) {
		using var vertices = _vertexBuffer.BorrowVerticesSpan(recalculateBoundingBoxOnLeaseDispose: false, overwriteChildMeshBoundingBoxes: false, range: ..meshData.VertexCount);
		meshData.Vertices.AsSpan(0, meshData.VertexCount).CopyTo(vertices.Span);
	}

	var trianglesToWrite = Math.Max(meshData.TriangleCount, _uploadedTriangleCount);
	if (trianglesToWrite > 0) {
		using var triangles = _vertexBuffer.BorrowTrianglesSpan(recalculateBoundingBoxOnLeaseDispose: false, overwriteChildMeshBoundingBoxes: false, range: ..trianglesToWrite);
		meshData.Triangles.AsSpan(0, meshData.TriangleCount).CopyTo(triangles.Span);
		triangles.Span[meshData.TriangleCount..].Clear();
	}
	_uploadedTriangleCount = meshData.TriangleCount;

	var lowestY = Math.Min(meshData.LowestY, meshData.HighestY);
	var height = Math.Max(meshData.HighestY - lowestY, 1);
	var boundingBox = new PositionedCuboid(
		footprintSize, 
		height, 
		footprintSize, 
		new Location(footprintSize * 0.5f, lowestY + height * 0.5f, footprintSize * 0.5f)
	);
	_vertexBuffer.SetBoundingBox(boundingBox, overwriteChildMeshBoundingBoxes: true);
	_instance.Position = position;
	return resized;
}
```

* Only the part of the buffer that's actually used is borrowed, so only that part is uploaded.
* A chunk can have fewer triangles than the chunk that previously used the same buffer, so any leftover triangles from the previous chunk are cleared.
* The mesh's bounding box is already known (it's a square column, as tall as the highest and lowest faces), so it's set directly rather than recalculated from the vertices.
* At most a few meshes are uploaded per frame (`WorldSettings.MaxMeshUploadsPerFrame`), so that streaming never makes one frame take much longer than the rest.

Every slot's buffer, mesh, and instance are created when the program starts: 3584 faces for a chunk and 1024 for a low-detail tile (in testing, the busiest chunk on the steepest mountainside had around 3100 faces and the busiest tile around 780). A dynamic vertex buffer keeps a copy of its contents in RAM (so that it can be read back), so creating buffers as the player explores would allocate memory part-way through the run. A mesh that somehow needed more space would have its resources re-created larger.

## Distant Terrain

Drawing every block out to two and a half kilometres would need hundreds of thousands of chunks: far more memory and GPU time than the view is worth, as distant terrain covers only a few pixels per block. Instead, beyond the full-detail chunks the world is drawn as low-detail tiles, each built (by `FarTerrainMesher.cs`) from a 16 × 16 grid of large flat cells, using the same quads as the chunks. There are two levels of tile: 64 blocks across out to around 600 metres, and 256 blocks across beyond that.

From TinyFFR's point of view, a tile is no different to a chunk: it's another slot with its own `GpuMeshSlot`, and switching between levels of detail is just a matter of hiding one slot's model instance and showing another's, so the levels never draw the same ground twice (see `TerrainLevel.cs`).

Every slot's dynamic vertex buffer reserves enough GPU memory for its largest possible mesh, so the application's video memory use is fixed when it starts. In testing, at 1920 × 1080 the whole application used around 1.75 GB of video memory, of which around 0.7 GB was the slots' buffers (most of it the full-detail chunks').

## Player Movement

The camera's look direction is handled by a [first-person camera controller](../reference/camera_controller_first_person.md), driven by the mouse and the gamepad's right stick. The controller's built-in movement isn't used: the player's position is worked out by `Player` instead (with gravity, jumping, and collision against the blocks, none of which are TinyFFR-specific), and given to the controller each frame:

```csharp
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
```

* `GameControllersCombined` reads every connected gamepad as one, so it doesn't matter which one the player uses (see [Gamepad Input](../reference/gamepad_input.md)).
* Because the controller only follows the player's position, its position smoothing is turned off (`SmoothingStrength.None`) so that the camera doesn't lag behind.

The movement keys and the left stick are read every frame:

```csharp
var forwardInput = gamepad.LeftStickPosition.GetDisplacementVerticalWithDeadzone();
var strafeInput = gamepad.LeftStickPosition.GetDisplacementHorizontalWithDeadzone();
if (keyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.W)) forwardInput += 1f;
if (keyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.S)) forwardInput -= 1f;
if (keyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.D)) strafeInput += 1f;
if (keyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.A)) strafeInput -= 1f;
```

`GetDisplacementVerticalWithDeadzone()` and `GetDisplacementHorizontalWithDeadzone()` ignore the small amount a stick drifts when it isn't being touched (see [Gamepad Input](../reference/gamepad_input.md)), and `KeyIsCurrentlyDown()` reports whether a key is held (see [Keyboard / Mouse Input](../reference/keyboard_and_mouse_input.md)).

## Atmosphere

```csharp
using var camera = factory.CameraBuilder.CreateCamera(exposure: CameraExposurePreset.OutsideVeryBright);
using var cameraController = camera.CreateController<FirstPersonCameraController>();
camera.FarPlaneDistance = WorldSettings.RenderDistance * 1.5f;
```

```csharp
using var sun = factory.LightBuilder.CreateDirectionalLight(
	direction: new Direction(0.35f, -1f, 0.45f),
	brightnessPreset: DirectionalLightBrightnessPreset.Midday,
	castsShadows: true,
	showSunDisc: true
);
```

```csharp
var skyColor = new ColorVect(0.3f, 0.55f, 0.95f);
scene.SetBackdrop(BuiltInSceneBackdrop.Clouds, SceneBackdropBrightnessPreset.Midday);
scene.Add(sun);
scene.AddFog(new FogDescriptor {
	Color = skyColor with { Alpha = 0.9f },
	StartDistance = WorldSettings.NearDetailDistance,
	DensityMultiplier = 0.5f,
	SkywardDensityFalloffMultiplier = 0f
});
```

* The camera uses the `OutsideVeryBright` exposure preset, to match the brightness of a sunny day (see [Exposure & Brightness](../reference/exposure_and_brightness.md)).
* The sun is a [directional light](../reference/directional_lights.md) at midday brightness, casting shadows and drawing a sun disc in the sky.
* The sky is TinyFFR's built-in `Clouds` [backdrop](../reference/scenes.md#backdrops) at midday brightness, which also provides the soft light that fills in the shadows.
* Translucent sky-blue [fog](../reference/fog.md) starts at the edge of the full-detail terrain, so that distant terrain fades in to a haze, as it would on a real day.

## Debug Readout

The readout in the top-left corner is a single line of text on a [canvas scene](../reference/canvas_scenes.md), drawn over the world by a [compositor](../reference/compositing.md). It's only updated four times per second, and its text is written in to a stack-allocated buffer rather than building a new `string` each time:

```csharp
Span<char> textBuffer = stackalloc char[320];
var charsWritten = 0;
var builtText = _allocationBaseline is { } baseline
	? textBuffer.TryWrite($"FPS: {framesPerSecond:N0}\nPosition: {position.X:N1}, {position.Y:N1}, {position.Z:N1}\nBiome: {biomeName}\nTerrain: {world.VisibleChunkCount} chunks, {world.VisibleMidTileCount} mid tiles, {world.VisibleFarTileCount} far tiles, {world.PendingJobCount} pending\nAllocated since warm-up: {(GC.GetTotalAllocatedBytes(precise: false) - baseline) / 1024:N0} KB, Gen0 GCs: {GC.CollectionCount(0) - _gen0CollectionBaseline}", out charsWritten)
	: textBuffer.TryWrite($"FPS: {framesPerSecond:N0}\nPosition: {position.X:N1}, {position.Y:N1}, {position.Z:N1}\nBiome: {biomeName}\nTerrain: {world.VisibleChunkCount} chunks, {world.VisibleMidTileCount} mid tiles, {world.VisibleFarTileCount} far tiles, {world.PendingJobCount} pending\nWarming up...", out charsWritten);
if (builtText) _text.SetText(textBuffer[..charsWritten], TextJustification.Left);
```

Its last line shows how much managed memory the application has allocated (and how many garbage collections have happened) since ten seconds after start-up; see below.

## Avoiding Garbage

To avoid [garbage collection pauses](../reference/avoiding_gc_stutter.md), the example aims to allocate no managed memory once it's running:

* Every array, queue, chunk object, and GPU resource is created when the program starts, and re-used for the rest of the run.
* Per-frame code uses no LINQ, lambdas, or `string` building, and loops over arrays rather than interfaces.
* Background jobs only write in to buffers (held by their pooled `TerrainJob`) that were created at start-up.

In testing, the application allocated around 1.3 MB in its first half-minute or so (inside TinyFFR, as its internal pools warmed up while the readout's text was first rendered), and then nothing more while exploring; no garbage collections happened at all during a minute of exploration.

## Asset Credits

* `block_atlas.png`: Block textures from Kenney's [Voxel Pack](https://kenney.nl/assets/voxel-pack) by Kenney Vleugels ([www.kenney.nl](https://www.kenney.nl)). Licensed under [CC0](https://creativecommons.org/publicdomain/zero/1.0/).
