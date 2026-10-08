---
title: Bundled Assets (glTF & Others)
description: Information on how to load composite asset files (including glTF/glb) in TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * You can load entire assets via `assetLoader.LoadBundledAsset(@"my_file.gltf")`. :material-arrow-right: [Bundled/Composite Asset Files](#bundledcomposite-asset-files)
    * Every component texture, mesh, material, and model definition are returned together as a `ModelBundle`. :material-arrow-right: [ModelBundles](#modelbundles)
    * It's possible to customize exactly how the resources are loaded. :material-arrow-right: [Customizing the Load Operation](#customizing-the-load-operation)

</div>

## Bundled/Composite Asset Files

```csharp
using var carBundle = factory.AssetLoader.LoadBundledAsset(@"Assets/Models/ToyCar.glb"); // (1)!
using var carModelInstances = factory.ObjectBuilder.CreateModelInstances(carBundle); // (2)!
scene.Add(carModelInstances); // (3)!
```

1.	This line instructs TinyFFR to load the "ToyCar.glb" model file from "Assets/Models/ToyCar.glb".

	The returned `carBundle` is a `ModelBundle` (explained below) containing all loaded meshes, textures, materials, and models.
	
2.	This line creates a single [ModelInstance](model_instances.md) for each `Model` (explained below) loaded amongst `carBundle`.

	The returned `carModelInstances` is a `ModelInstanceGroup` (essentially a specialized `ResourceGroup`) that contains all loaded model instances.
	
3.	This line adds all the newly-instantiated model instances to a pre-existing [Scene](scenes.md); ready to be rendered.

Modern 3D assets are typically packaged in a composite/bundled "transmission" format such as `.glTF` or `.glb`. These files usually contain the meshes, textures, materials definitions, and animations required to define a single unified asset.

??? abstract "Supported Formats"
	TinyFFR can load the following asset formats. Some more esoteric features of 3D model formats such as scenes, multi-material objects, or exported material systems are not loaded, and therefore support for some of the file formats listed below may be partial.

	- 3D
	- [3DS](https://en.wikipedia.org/wiki/.3ds)
	- [3MF](https://en.wikipedia.org/wiki/3D_Manufacturing_Format)
	- AC
	- [AC3D](https://en.wikipedia.org/wiki/AC3D)
	- ACC
	- AMF
	- ASE
	- ASK
	- ASSBIN
	- B3D
	- [BLEND](https://en.wikipedia.org/wiki/.blend_(file_format))
	- BSP / PK3
	- [BVH](https://en.wikipedia.org/wiki/Biovision_Hierarchy)
	- CSM
	- COB
	- [DAE/ZAE/Collada](https://en.wikipedia.org/wiki/COLLADA)
	- [DXF](https://en.wikipedia.org/wiki/AutoCAD_DXF)
	- ENFF
	- [FBX](https://en.wikipedia.org/wiki/FBX)
	- [glTF 1.0](https://en.wikipedia.org/wiki/GlTF#glTF_1.0) + GLB
	- [glTF 2.0](https://en.wikipedia.org/wiki/GlTF#glTF_2.0)
	- HMP
	- IFC-STEP / IFCZIP
	- IQM
	- IRR / IRRMESH
	- [LWO](https://en.wikipedia.org/wiki/LightWave_3D)
	- LWS
	- LXO
	- MD2
	- MD3
	- MD5
	- MDC
	- MDL
	- MESH / MESH.XML
	- MOT
	- MS3D
	- NDO
	- NFF
	- [OBJ](https://en.wikipedia.org/wiki/Wavefront_.obj_file)
	- [OFF](https://en.wikipedia.org/wiki/OFF_(file_format))
	- [OGEX](https://en.wikipedia.org/wiki/Open_Game_Engine_Exchange)
	- [PLY](https://en.wikipedia.org/wiki/PLY_(file_format))
	- PMX
	- PRJ
	- Q3O
	- Q3S
	- RAW
	- SCN
	- SIB
	- SMD
	- [STP](https://en.wikipedia.org/wiki/ISO_10303-21)
	- [STL](https://en.wikipedia.org/wiki/STL_(file_format))
	- TER
	- UC
	- VRM
	- VTA
	- X
	- [X3D / X3DB](https://en.wikipedia.org/wiki/X3D)
	- XGL
	- ZGL

#### What's Actually Loaded

`LoadBundledAsset()` reads every `Mesh`, `Texture` and `Material` inside the target file. It associates linked `Mesh`es and `Material`s in to `Model`s (see below).

Skeletal animation data, if present, will also be loaded. See [Skeletal Meshes](skeletal_meshes.md) for more info on how this data is loaded, or see [Playing Skeletal Animations](playing_skeletal_animations.md) for information on playing the animations in a scene.

Everything is returned inside a single returned resource called a `ModelBundle`:

## ModelBundles

`LoadBundledAsset()` returns a `ModelBundle`. As its name implies, it represents a bundle of tightly-related loaded resources (in this case all textures, meshes, materials, models, and any shared animation data loaded as part of the composite asset file). 

The `Textures`, `Materials`, `Meshes` and `Models` properties let you enumerate each kind of resource in the bundle, while `Animations` and `Skeleton` expose the bundle's shared animation data.

`CalculateCombinedBoundingBox()` returns the box enclosing every mesh in the bundle (i.e. the model-space bounding box of a group of instances created from it), and `CalculateScalingForSize()` calculates the scaling that makes such a group measure a given size (see [Size](scene_objects.md#size)).

Disposing the `ModelBundle` also disposes all contained resources, meaning invoking `carBundle.Dispose()` also disposes every `Mesh`, `Material`, `Texture`, etc loaded by `LoadBundledAsset()`.

## Models

When you invoke `LoadBundledAsset()` TinyFFR determines which materials are meant for which meshes and pairs them in to `Model` instances. Most of the time, when loading composite asset files (such as glTF/glb) it's the `Model`s that we actually want to use.

A `Model` does not *itself* represent any loaded data on the system; instead it is a logical pairing of a `Mesh` and `Material` that combined make up a single fully-textured object model.

We can pass a `Model` or `ModelBundle` to the `factory.ObjectBuilder` to create instances of those models. Passing the whole `ModelBundle` (as in the example at the top of this page) creates an instance of every model in it. Those [ModelInstances](model_instances.md) can then be added to any [Scene](scenes.md).

??? info "Creating Models Manually"
	If you wish to logically link a `Mesh` and `Material` together in to your own `Model`, it's as simple as invoking `factory.AssetLoader.CreateModel(mesh, material)`.
	
	The returned `Model` must be disposed before disposing its constituent `Mesh` and `Material`.

## Customizing the Load Operation

You can pass a `ModelCreationConfig` and a `ModelReadConfig` to `LoadBundledAsset()` to customize how the bundled data should be loaded:

### ModelCreationConfig

<span class="def-icon">:material-card-bulleted-outline:</span> `MeshConfig`

:   This specifies the [MeshCreationConfig](loading_meshes.md#meshcreationconfig) that should be applied to every `Mesh` contained in the composite asset file.

<span class="def-icon">:material-card-bulleted-outline:</span> `TextureConfig`

:   This specifies the [TextureCreationConfig](loading_textures.md#texturecreationconfig) that should be applied to every `Texture` contained in the composite asset file.

	The `DataType` property required on this `TextureCreationConfig` is used only for textures that have no specific data type or interpretation specified by the file format. In most cases all textures in a composite format file *do* have a specific data type/interpretation, so this property is largely ignored in this context.
	
	If in doubt, you can set it to `ColorSrgb` which covers a lot of common texture types.

<span class="def-icon">:material-card-bulleted-outline:</span> `TransmissiveMaterialQuality`

:   The [quality](transmissive_materials.md#refraction) used for any transmissive (e.g. glass) materials found in the file. Defaults to `FullReflectionsAndRefraction`.

	Setting this to `SkyboxOnlyReflectionsAndRefraction` can make scenes with visible glass or mirror surfaces considerably cheaper to render, at the cost of the glass/mirror showing only the backdrop (rather than the other objects in the scene).
	
### ModelReadConfig

<span class="def-icon">:material-card-bulleted-outline:</span> `MeshConfig`

:   This specifies the [MeshReadConfig](loading_meshes.md#meshreadconfig) that should be applied to every `Mesh` contained in the composite asset file.

<span class="def-icon">:material-card-bulleted-outline:</span> `TextureConfig`

:   This specifies the [TextureReadConfig](loading_textures.md#texturereadconfig) that should be applied to every `Texture` contained in the composite asset file.

<span class="def-icon">:material-card-bulleted-outline:</span> `HandleUriEscapedStrings`

:   Some file formats embed references to sub-assets using a URI-encoded string format (e.g. `my%20file.jpg` rather than `my file.jpg`). By default, TinyFFR does not attempt to interpret and decode these strings.

	Setting this property to `true` enables this decoding.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `EmissiveStrengthScalar`

:	A factor applied to emissive strengths read from the file (defaults to `0.05f`).

	Some file types (most notably `glTF`/`glb`) record emissive strength on a scale that does not correspond to TinyFFR's, so a file's values are scaled down to keep glowing surfaces from overwhelming the scene. Raise this if imported emissive surfaces look too dim; lower it if they seem too bright.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `EmissiveStrengthCap`

:   A normalized value (0 to 1) indicating the max emissive strength of any material imported from this file.

	Defaults to `1f`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `EmbeddedTextureMapScalingStrategy`

:   Sometimes composite asset files provide multiple texture files for different material properties that must be combined in to a single map file. 

	In the case those texture files have differing resolutions, this property controls how the lower-resolution textures should be upscaled. 
