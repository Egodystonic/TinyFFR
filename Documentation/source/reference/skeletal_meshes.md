---
title: Skeletal Meshes
description: Information on how skeletal meshes work in TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Skeletal tree, node, and animation data are supported and loaded by default when present. :material-arrow-right: [Loading and Using Skeletal Mesh Data](#loading-and-using-skeletal-mesh-data)
    * Playing loaded animations and calculating the resultant position of nodes is described in [Playing Skeletal Animations](playing_skeletal_animations.md).
    * It's possible to author your own skeletal meshes programmatically. :material-arrow-right: [Manually Creating Skeletal Meshes](#manually-creating-skeletal-meshes)

</div>

## Loading and Using Skeletal Mesh Data

Skeletal meshes (& animations) are supported in TinyFFR. You can load skeletal meshes either via [LoadAll(...)](bundled_assets.md) or [LoadMesh(...)](loading_meshes.md).

???+ warning "LoadMesh() Limitations"
	`LoadMesh()` can load files that contain multiple "sub" meshes (they are merged in to one `Mesh`), but it can only load skeletal data for a single sub-mesh. When loading a skeletal file with multiple sub-meshes, either select one via `readConfig.SubMeshIndex` or use `LoadAll()` instead; otherwise the mesh is loaded as a static (non-skeletal) mesh and a warning is written to the console.

Skeletal data is loaded as long as the given `readConfig.LoadSkeletalAnimationDataIfPresent` property is `true`. The property is `true` by default which means any mesh with skeletal data present has that data (and associated animations + nodes) loaded by default.

TinyFFR supports skeletons of up to 255 bones (`IMeshBuilder.MaxSkeletalBoneCount`). A mesh whose skeleton has more bones than this is loaded as a static mesh instead, and a warning is written to the console.

??? info "Animation Speed"
	Animation keyframes are stored in files in "ticks" rather than seconds, and the file is expected to also specify how many ticks there are per second. Some formats do not record this, in which case TinyFFR assumes 25 ticks per second.
	
	If a loaded animation plays at the wrong speed, set `readConfig.AnimationTicksPerSecondOverride` to the correct tick rate.

### Nodes

```csharp
var nodes = mesh.Skeleton.Nodes;
Console.WriteLine($"Skeleton has {nodes.Count} nodes:");
foreach (var node in nodes) {
	Console.WriteLine($"\t#{node.Index}: {node.GetNameAsNewStringObject()}");
}

var rightHand = nodes.TryGetNodeByName("RightHand"); // (1)!
if (rightHand != null) {
	mesh.Skeleton.GetBindPoseNodeTransforms(rightHand.Value, out var handTransform); // (2)!
	Console.WriteLine($"Right hand bind-pose transform: {handTransform}");
}
```

1.	Alternatively, `nodes["RightHand"]` returns the node directly, but throws a `KeyNotFoundException` if no node has that name. `nodes[3]` returns the node at index 3.

2.	The returned `Matrix4x4` is the node's transform in model space (i.e. relative to the mesh's origin) when the mesh is in its bind pose. Multiply it by a model instance's own transform to get the node's world-space transform.

Every `Mesh` has a `Skeleton` property. Its `Nodes` property lists every node (joint) in the skeleton, and lets you look them up by index or by name.

Each `MeshNode` has an `Index` (its position in the skeleton's node list) and a name (accessed via `GetNameAsNewStringObject()`, or `GetNameLength()` + `CopyName()` to avoid allocating). Nodes loaded from a file take their names from that file; nodes that were never given a name are reported as `node_<index>`. Nodes belong to their `Mesh` and are disposed along with it, so they do not need disposing themselves.

The `Skeleton` also offers the following methods:

<span class="def-icon">:material-code-block-parentheses:</span> `GetBindPoseNodeTransforms()`

:   Gets the model-space transform of one or more nodes in the mesh's bind pose (i.e. the pose it takes when no animation is applied). Overloads take a single `MeshNode`, a span of `MeshNode`s, or a span of node indices (which, unlike `MeshNode`s, can be `stackalloc`ed); along with an output destination.

<span class="def-icon">:material-code-block-parentheses:</span> `ApplyBindPose()`

:   Returns the given [ModelInstance](model_instances.md) (which must be using this mesh) to its bind pose; use this to reset an instance after you've finished animating it.

For a mesh without skeletal data, `Nodes` is empty, `GetBindPoseNodeTransforms()` returns identity matrices, and `ApplyBindPose()` does nothing.

### Animations

```csharp
Console.WriteLine($"Mesh has {mesh.Animations.Count} animations:");
foreach (var animation in mesh.Animations) {
	Console.WriteLine($"\t{animation.GetNameAsNewStringObject()}: {animation.DefaultDurationSeconds}s");
}

var walkAnimation = mesh.Animations["Walk"]; // (1)!
var runAnimation = mesh.Animations.TryGetAnimationByName("Run"); // (2)!
```

1.	Throws a `KeyNotFoundException` if the mesh has no animation named "Walk".

2.	Returns `null` if the mesh has no animation named "Run".

Every `Mesh` has an `Animations` property, which lists every animation attached to that mesh. As well as enumerating it, you can look animations up by name (using the indexer or `TryGetAnimationByName()`) or by index (`mesh.Animations[0]`). The `Skeletal` property lists only skeletal animations (currently all animations in TinyFFR are skeletal).

Each `MeshAnimation` has:

* A name (accessed via `GetNameAsNewStringObject()`, or `GetNameLength()` + `CopyName()`). Animations loaded from a file take their names from that file.
* A `Type`, which is `MeshAnimationType.Skeletal`.
* A `DefaultDurationSeconds`, which indicates how long the animation takes to play at its authored speed.

Animations belong to their `Mesh` and are disposed along with it.

To actually play an animation on a [ModelInstance](model_instances.md), see [Playing Skeletal Animations](playing_skeletal_animations.md).

## Manually Creating Skeletal Meshes

Though complex, it is supported and possible to programmatically define skeletal meshes & their animations. This is done via two stages:

* Firstly, you must create the mesh with skeletal vertex data + node data;
* Secondly, you must add animation definitions.

??? abstract "How are Skeletal Animations Defined?"
	Vertex-skinning animations work by first defining a tree of skeletal nodes. There is always one root/parent node, and every other node is either a child of this root or of another node further down the tree hierarchy.
	
	Additionally, some nodes in this hierarchy will be labelled as bones. Not all nodes are bones, but those that are will directly be used to define how the mesh's vertices will transform under animation. Non-bone nodes are still useful as they define interim transformations along the skeletal hierarchy (e.g. imagine a chest node that is not itself a bone but can twist/bend- connected bones such as arms still need to follow this chest node even if there are no vertices *directly* affected by it).
	
	A skeletal mesh must supply the typical mesh vertex data for a mesh as well as bone weightings for each vertex. These weightings define how bone nodes in the node tree affect each vertex individually. Each vertex in TinyFFR can be affected by up to four bones simultaneously.
	
	Animations are then added as lists of time-series keyframes that define how each node transforms over time (scaling, rotating, and translating). When applying an animation, TinyFFR walks the nodal tree starting from the parent/root node, applying the node-local transform for each node as it goes. Each node's transform is applied cumulatively- meaning a transform on the root node affects all child nodes, and so on.

### CreateFromVertices()

The `IMeshBuilder` interface offers overloads for `CreateFromVertices()` that accept a span of `MeshVertexSkeletal` instances (instead of plain `MeshVertex`) and a span of `VertexTriangle`s, alongside a span of `SkeletalAnimationNode`s.

The nodes may define at most 255 bones (`IMeshBuilder.MaxSkeletalBoneCount`); supplying more will throw an `ArgumentException`. If the nodes define no bones at all, TinyFFR substitutes a single root bone with identity transforms, meaning the mesh will behave as a static (non-skeletal) mesh.

Skeletal meshes do not support per-instance vertex mutation; setting `MeshCreationConfig.AllowsPerInstanceVertexMutation` to `true` when creating one will throw an exception.

#### MeshVertexSkeletal

Each `MeshVertexSkeletal` requires the standard vertex data as defined in [Meshes](meshes.md#meshvertex), as well as the following additional properties:

* __BoneIndices__ :material-arrow-right: This is an [inline array](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-12.0/inline-arrays) of four bytes, each one indexes a bone in the skeleton's array of bones (bones are nodes, but not all nodes are bones). Each index is paired with a `BoneWeights` entry, together they are used to define how this vertex will be transformed in model space when applying animations to the target nodes.
* __BoneWeights__ :material-arrow-right: This is an [inline array](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-12.0/inline-arrays) of four floats, each one defines how much 'weight' or 'pull' its corresponding bone has on this vertex. These four weights should usually sum to `1f`.

It's possible to easily create a `BoneIndexArray` or `BoneWeightArray` using the static utility functions defined on those types:

```csharp
var indexArray = MeshVertexSkeletal.BoneIndexArray.Create(0, 1, 255, 255);
var weightArray = MeshVertexSkeletal.BoneWeightArray.Create(0f, 1f, 0f, 0f);
```

Note: The position of the vertices supplied here are known as the mesh's *bind pose*.

#### SkeletalAnimationNode

Each `SkeletalAnimationNode` represents a joint in the nodal tree that comprises this mesh's skeleton. 

Each node must supply the following parameters:

* __DefaultLocalTransform__ :material-arrow-right: This is the transform matrix relative to the parent node that puts this node in the correct position to maintain the bind pose when no animation is playing.
* __BindPoseInversion__ :material-arrow-right: This is the transform matrix used to transform vertices from model space to this node's local space.
* __ParentNodeIndex__ :material-arrow-right: This is the index of the `SkeletalAnimationNode` that is this node's parent, or `null` if this is the root node.
* __CorrespondingBoneIndex__ :material-arrow-right: This is the index of the bone associated with this node, or `null` if this node does not represent a bone.

### AttachAnimation()

After creating a skeletal mesh, you can use `IMeshBuilder.AttachAnimation()` to attach animations to it. `AttachAnimation()` returns the newly-attached `MeshAnimation`, and throws an `InvalidOperationException` if the given mesh was not created with skeletal data.

Each animation requires the following arguments passed to `AttachAnimation()`:

<span class="def-icon">:material-code-json:</span> `mesh`

:   This is the corresponding `Mesh` that was created above.

<span class="def-icon">:material-code-json:</span> `scalingKeyframes`, `rotationKeyframes`, `translationKeyframes`

:   These are the time-series transform lists- each represents a timepoint in the animation and a corresponding scaling, rotation, or translation of a node. The affected node for each is defined later in the `boneMutations` argument.

	Each keyframe is constructed from a time (in seconds) and a value; e.g. `new SkeletalAnimationScalingKeyframe(0.5f, new Vect(1f, 2f, 1f))`. A `SkeletalAnimationRotationKeyframe` can be constructed from either a `Quaternion` or a `Rotation`.

	The keyframes belonging to each node (i.e. each range specified by a `boneMutations` entry, see below) must be ordered by time (ascending, e.g. starting at 0 seconds and moving forward in time), otherwise an `ArgumentException` will be thrown. The time points do not need to be the same in each span (the spans do not even need to have the same number of elements).
	
	Any of these spans may be empty (e.g. if your animation never scales any node, you can pass an empty `scalingKeyframes` span).
	
<span class="def-icon">:material-code-json:</span> `boneMutations`

:   This is a span detailing how the transform keyframes specified above should be applied to each `SkeletalAnimationNode`. Essentially, this span acts as a "lookup" or "index" mapping the keyframe data supplied above to the skeletal node tree. 

	The `TargetNodeIndex` for each mutation indicates which node this mutation is indexing, and the `[...]KeyframeStartIndex` and `[...]KeyframeCount` define which `scalingKeyframes`, `rotationKeyframes`, or `translationKeyframes` are applicable to it. Any of the counts may be `0`.
	
	!!! warning "Mutations Replace the Default Local Transform"
		For every node targeted by a mutation, the animation *replaces* that node's `DefaultLocalTransform` entirely with the keyframed scaling, rotation, and translation; they are not combined with it.
		
		If a mutation has no keyframes for one of these components, that component falls back to its identity value: a scaling of `(1, 1, 1)`, no rotation, and a translation of `(0, 0, 0)`. Therefore, if a node's `DefaultLocalTransform` includes a translation from its parent (as most do), you must supply that translation as a keyframe too, even if the animation only rotates the node.
		
		Nodes that are not targeted by any mutation keep their `DefaultLocalTransform`.
	
	When the animation is sampled at a time before a node's first keyframe or after its last one, the value of that first/last keyframe is used.
	
<span class="def-icon">:material-code-json:</span> `defaultCompletionTimeSeconds`

:   This determines how long in seconds the animation should take to play by default.

<span class="def-icon">:material-code-json:</span> `name`

:   Every animation must have a name that is not empty and is unique amongst the animations attached to its mesh. Attempting to attach an animation with a name the mesh already has will throw an `InvalidOperationException`.
	
### SetSkeletonNodeName()
	
This optional method on the `IMeshBuilder` allows you to set names for each node in a created skeletal mesh. This can be important if you need to look up those nodes later by name for e.g. getting their transform matrices post-animation.

If the given name is already used by another node on the same mesh, a number is appended to make it unique (e.g. a second node named "Hand" becomes "Hand2"). Passing a node index that is out of range throws an `ArgumentOutOfRangeException`, and invoking this method on a mesh with no skeletal data has no effect.

Nodes of meshes loaded from a file are already named according to that file.

### Example

The following example creates a 2m-tall, 0.2m-wide vertical strip that faces the camera, with a two-bone skeleton: a "Base" bone at the bottom and a "Tip" bone halfway up. It then attaches a one-second "Bend" animation that tilts the top half of the strip 45° to one side:

```csharp
static MeshVertexSkeletal CreateVertex(float x, float y, byte boneA, byte boneB, float boneBWeight) {
	return new MeshVertexSkeletal(
		location: (x, y, 0f),
		textureCoords: ((0.1f - x) / 0.2f, y / 2f),
		tangent: Direction.Right,
		bitangent: Direction.Up,
		normal: Direction.Backward,
		boneIndices: MeshVertexSkeletal.BoneIndexArray.Create(boneA, boneB, 0, 0),
		boneWeights: MeshVertexSkeletal.BoneWeightArray.Create(1f - boneBWeight, boneBWeight, 0f, 0f)
	);
}

var vertices = new[] { // (1)!
	CreateVertex(0.1f, 0f, 0, 0, 0f),
	CreateVertex(-0.1f, 0f, 0, 0, 0f),
	CreateVertex(0.1f, 1f, 0, 1, 0.5f),
	CreateVertex(-0.1f, 1f, 0, 1, 0.5f),
	CreateVertex(0.1f, 2f, 0, 1, 1f),
	CreateVertex(-0.1f, 2f, 0, 1, 1f),
};
var triangles = new VertexTriangle[] { // (2)!
	new(0, 1, 3),
	new(0, 3, 2),
	new(2, 3, 5),
	new(2, 5, 4),
};
var nodes = new SkeletalAnimationNode[] { // (3)!
	new(
		DefaultLocalTransform: Matrix4x4.Identity,
		BindPoseInversion: Matrix4x4.Identity,
		ParentNodeIndex: null,
		CorrespondingBoneIndex: 0
	),
	new(
		DefaultLocalTransform: Matrix4x4.CreateTranslation(0f, 1f, 0f),
		BindPoseInversion: Matrix4x4.CreateTranslation(0f, -1f, 0f),
		ParentNodeIndex: 0,
		CorrespondingBoneIndex: 1
	),
};

var mesh = factory.MeshBuilder.CreateFromVertices(
	vertices,
	triangles,
	nodes,
	new MeshCreationConfig {
		Name = "Bending Strip",
		BoundingBoxOverride = new PositionedCuboid(2f, 2.2f, 0.2f, centerPoint: (0f, 1f, 0f)) // (4)!
	}
);
factory.MeshBuilder.SetSkeletonNodeName(mesh, 0, "Base");
factory.MeshBuilder.SetSkeletonNodeName(mesh, 1, "Tip");

var rotationKeyframes = new SkeletalAnimationRotationKeyframe[] {
	new(0f, Rotation.None),
	new(1f, Direction.Forward % 45f),
};
var translationKeyframes = new SkeletalAnimationTranslationKeyframe[] { // (5)!
	new(0f, new Vect(0f, 1f, 0f)),
};
var mutations = new SkeletalAnimationNodeMutationDescriptor[] {
	new(
		TargetNodeIndex: 1,
		ScalingKeyframeStartIndex: 0, ScalingKeyframeCount: 0,
		RotationKeyframeStartIndex: 0, RotationKeyframeCount: 2,
		TranslationKeyframeStartIndex: 0, TranslationKeyframeCount: 1
	),
};

var bendAnimation = factory.MeshBuilder.AttachAnimation(
	mesh,
	ReadOnlySpan<SkeletalAnimationScalingKeyframe>.Empty, // (6)!
	rotationKeyframes,
	translationKeyframes,
	mutations,
	defaultCompletionTimeSeconds: 1f,
	name: "Bend"
);
```

1.	The strip is made of three rows of two vertices, at heights of 0m, 1m, and 2m.

	The bottom row is moved entirely by bone 0 ("Base"), the top row entirely by bone 1 ("Tip"), and the middle row is split evenly between the two so that the strip bends smoothly around it.

2.	Four triangles join the six vertices. Each is wound anticlockwise when viewed from the front (i.e. from a camera looking `Forward` at the strip).

3.	Node 0 is the root node and corresponds to bone 0; it sits at the mesh's origin.

	Node 1 is a child of node 0 and corresponds to bone 1. Its `DefaultLocalTransform` places it 1m above its parent, and its `BindPoseInversion` is the inverse of its model-space bind-pose transform (moving vertices from model space in to its local space).

4.	The animation moves the top of the strip outside the box that covers the bind pose, so we supply a bounding box large enough to cover the whole animation (see [Bounding Boxes](#bounding-boxes) below).

5.	The animation only rotates node 1, but because a mutation *replaces* the node's `DefaultLocalTransform`, we must also supply its 1m translation from its parent. Without this keyframe, node 1 would be moved down to its parent's origin during the animation.

6.	This animation never scales any node, so the scaling keyframes span is empty.

### Bounding Boxes

The vertices of a skeletal mesh are stored in their unposed form and are moved in to place on the GPU by the mesh's bone transforms. This means a skeletal mesh's `BoundingBox` can not simply be the extents of its vertex data; instead TinyFFR calculates it by applying the bind pose to the vertices, and then widening the result to cover each animation attached to the mesh, sampled at its keyframe times (up to 64 samples per animation). This makes the box suitable for frustum culling no matter which animation is playing; though in rare cases an extreme pose *between* two sampled keyframes may extend slightly beyond it.

Meshes loaded from a file via the `AssetLoader` get this treatment automatically, as their animations are known when the mesh is created.

Meshes you build yourself via `IMeshBuilder.CreateFromVertices()` are a special case: because `AttachAnimation()` is necessarily invoked *after* the mesh has been created, the mesh's bounding box covers the bind pose only. If your animations move vertices outside of that box, supply your own box via `MeshCreationConfig.BoundingBoxOverride` when creating the mesh. (You can also use `BoundingBoxOverride` for loaded meshes if the sampled box is not large enough.)

In all cases, the box is additionally enlarged by `MeshCreationConfig.BoundingBoxAdditionalMargin`.
