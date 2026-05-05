---
title: Animations
description: This page explains the concept of mesh animations in TinyFFR.
---

Some meshes can support pre-defined animation data (such as a person walking/running). TinyFFR supports these via the animation playback system.

## Skeletal Animations

### Basic Playback

Assuming you have an asset/model file that has pre-baked skeletal animation data, you can load it with `LoadAll()`:

```csharp
var modelData = assetLoader.LoadAll("myMesh.glb");
var animatedModel = objectBuilder.CreateModelInstance(modelData.Models[0]); // (1)!
scene.Add(animatedModel);
```

1. 	This code assumes there's at least one model that's been loaded (a `Model` is a `Mesh` + `Material` pair). If `Models.Count` is 0, an exception will be thrown.

	Most animated mesh assets will usually have a material exported alongside them as it's difficult to programmatically author materials for most realistic-looking animated skeletal meshes.

	However, if your asset file *does* only contain a `Mesh` (without a `Material`) you can still create a `ModelInstance` manually (e.g. load a material via `Load[...]Map()` and `materialBuilder.Create[...]()`, and then invoke `objectBuilder.CreateModelInstance(modelData.Meshes[0], myMaterial)`).

You can then access loaded animations and create a `MeshAnimationPlayer` for any of them:

```csharp
var player = animatedModel.GetAnimationPlayer( // (1)!
	animatedModel.Animations[0]
);

var player = animatedModel.GetAnimationPlayer( // (2)!
	animatedModel.Animations["run"]
);

var player = animatedModel.GetAnimationPlayerWithSpeedMultiplier( // (3)!
	animatedModel.Animations[0], 
	2f
);

var player = animatedModel.GetAnimationPlayerWithTargetDuration( // (4)!
	animatedModel.Animations["praise_the_sun"], 
	10f
);
```

1.	This line creates a `MeshAnimationPlayer` that plays the first animation loaded for the given `animatedModel`.

	Note that if `Animations.Count` is 0, attempting to access `Animations[0]` will throw an exception.
	
2.	This line creates a `MeshAnimationPlayer` that plays an animation named "run".

	Note that if no animation named "run" was defined in the asset file, attempting to access `Animations["run"]` will throw an exception.
	
3.	This line creates a `MeshAnimationPlayer` that plays the first animation loaded for the given `animatedModel`. Additionally, the player will play the animation at double speed (the `2f` argument represents a 2x playback speed).

4.	This line creates a `MeshAnimationPlayer` that plays the animation "praise_the_sun". Additionally, the player will play the animation with a target duration of 10 seconds.

Once you have a `MeshAnimationPlayer`, it can be used as follows to set the time point of the chosen animation on your model instance:

```csharp
player.SetTimePoint(10f); // (1)!
player.SetTimePoint(10f, AnimationWrapStyle.Once); // (2)!
player.SetTimePoint(10f, AnimationWrapStyle.OncePingPonged); // (3)!
player.SetTimePoint(10f, AnimationWrapStyle.Loop); // (4)!
player.SetTimePoint(10f, AnimationWrapStyle.LoopPingPonged); // (5)!
```

1.	This sets the animation to its defined pose at 10 seconds.

2.	This sets the animation to its defined pose at 10 seconds, but if the animation's duration is less than 10 seconds it will be clamped to its endpoint. 

	Note this is effectively the same as the line above, though it technically has an additional effect if the animation was authored with keyframes beyond its exported default duration (unlikely/unusual).
	
3.	This sets the animation to its defined pose at 10 seconds. However, once the set time point extends past the animation's duration, the player will begin to play the animation in reverse, until the set time point reaches 2x the original animation length.

	This "ping-pong" will happen exactly once, after which the animation will stop at its startpoint.

4.	This sets the animation to its defined pose at 10 seconds, but looping (e.g. as the time point extends past the end of the animation's duration, the animation will repeat infinitely).

5.	This sets the animation to its defined pose at 10 seconds, looping like the above example. 
	
	However, rather than restarting the animation at timepoint 0 on every iteration, the animation will "ping-pong" back and forward, moving in reverse every other loop.
	
???+ tip "Animation Player Performance Tip"
	The `MeshAnimationPlayer` is just a regular struct; it is not a resource and does not need to be disposed. Creating a `MeshAnimationPlayer` is cheap and can be done per-anim, per-frame.
	
	The same is applicable to the `MeshBlendedAnimationPlayer` (described below).

### Animation Blending

It is also possible to blend two animations together. This is often used when transitioning from animation to another- it allows moving between animations smoothly.

To create a `MeshBlendedAnimationPlayer`, simply supply a start and end animation to `GetAnimationPlayer()`:

```csharp
var player = animatedModel.GetAnimationPlayer( // (1)!
	animatedModel.Animations[0], 
	animatedModel.Animations[1]
);

var player = animatedModel.GetAnimationPlayer( // (2)!
	animatedModel.Animations["run"], 
	animatedModel.Animations["walk"]
);

var player = animatedModel.GetAnimationPlayerWithSpeedMultiplier( // (3)!
	animatedModel.Animations[0], 
	2f, 
	animatedModel.Animations[1], 
	0.5f
);

var player = animatedModel.GetAnimationPlayerWithTargetDuration( // (4)!
	animatedModel.Animations["praise_the_sun"], 
	10f, 
	animatedModel.Animations["blaspheme_the_moon"], 
	0.5f
);
```

1.	This line creates a `MeshBlendedAnimationPlayer` that blends between the first and second animation loaded for `animatedModel`.
	
2.	This line creates a `MeshBlendedAnimationPlayer` that blends between the the "run" and "walk" animations.
	
3.	This line creates a `MeshBlendedAnimationPlayer` that blends between the first and second animation loaded for `animatedModel`. Additionally, the player will play the first animation at double speed (the `2f` argument represents a 2x playback speed) and the second animation at half speed (`0.5f` represents 50% playback speed).

4.	This line creates a `MeshBlendedAnimationPlayer` that blends between the "praise_the_sun" and "blaspheme_the_moon" animations. Additionally, the player will play the first animation with a target duration of 10 seconds and the second one with a target duration of 0.5 seconds.

Playing blended animations then looks similar to playing non-blended ones; except it is required to specify a time point for both animations as well as an interpolation distance between them:

```csharp
player.SetTimePoint(5f, 10f, 0.5f); // (1)!
player.SetTimePoint( // (2)!
	3f, 
	AnimationWrapStyle.Loop, 
	7f, 
	AnimationWrapStyle.OncePingPonged, 
	0.2f
);
```

1.	This sets the first animation to its defined pose at 5 seconds and the second to its defined pose at 10 seconds.

	The animations are blended exactly 50/50 (the third argument of `0.5f` represents an interpolation distance of 50%).

2.	This sets the first animation to its defined pose at 3 seconds with looping applied and the second animation to 7 seconds with a once-ping-pong wrapping applied.

	The resultant animations are blended with a 20% distance from start to end.
	
???+ info "Completion Fractions vs Time Points"
	For every `SetTimePoint()` example shown above, you can also instead opt to use an alternative method named `SetCompletionFraction()`:
	
	`#!csharp player.SetCompletionFraction(0.4f, AnimationWrapStyle.Loop, 1f, AnimationWrapStyle.OncePingPonged, 0.2f);`
	
	Whereas `SetTimePoint()` sets the animation pose to a specific timestamp, `SetCompletionFraction()` sets the animation at a percentage of its overall runtime. For example, `SetCompletionFraction(0.6f)` sets the animation to 60% completed.
	
### Node Transform Retrieval

When setting a mesh instance to a given animation time point, it is often useful to also ascertain where one or more nodes/bones in the mesh "end up" (for example, this can be useful when you want to give the illusion of a character model "holding" on to an item).

For every `SetTimePoint()` or `SetCompletionFraction()` overload shown above, there is a further overload named `SetTimePointAndGetNodeTransforms()` or `SetCompletionFractionAndGetNodeTransforms()`.

To use these functions, you need to specify which node(s) you want to capture the transforms for, and a `Matrix4x4` will be filled in for you. In this first example, we capture the resultant position of the "left_hand" node and use it to place a sword in the player's hand:

```csharp
var leftHandNode = playerInstance.Skeleton.Nodes["left_hand"];

player.SetTimePointAndGetNodeTransforms( // (1)!
	1f,
	leftHandNode,
	out var leftHandTransform
);

sword.SetTransform(leftHandTransform * playerInstance.Transform.ToMatrix()); // (2)!
```

1.	This sets the time point for the animation to 1 second, but also passes `leftHandNode` as a second parameter. 

	The `leftHandTransform` out-parameter is a `Matrix4x4` that will be equal to the model-space transform of the left hand when this animation is set to its 1-second time point.
	
2.	We can now move the sword (presumbed to be a `ModelInstance`) to the position of the player model's left hand by taking the model-space transform matrix and multiplying it by the player instance's transform matrix.

We can capture the transform of multiple nodes:

```csharp
var leftFootNode = playerInstance.Skeleton.Nodes["left_foot"];
var rightFootNode = playerInstance.Skeleton.Nodes["right_foot"];

ReadOnlySpan<int> nodeIndices = stackalloc int[] { leftFootNode.Index, rightFootNode.Index };
Span<Matrix4x4> transforms = stackalloc Matrix4x4[nodeIndices.Length];
player.SetTimePointAndGetNodeTransforms(1f, nodeIndices, transforms); // (1)!

var playerTransform = playerInstance.Transform.ToMatrix();

leftShoeInstance.SetTransform(transforms[0] * playerTransform);
rightShoeInstance.SetTransform(transforms[1] * playerTransform);
```

1.	Here we pass a read-only span of two nodes that we wish to retrieve the transforms for (note: We're actually passing the indices of the nodes-- you can pass the nodes themselves but only the indices are allocatable on the stack, hence we pass these instead).

	The `transforms` span is also passed, and each node we passed via `nodeIndices` will have its model-space transform written in the corresponding index in this span.

## Manually Creating Skeletal Meshes

Though complex, it is supported and possible to programmatically define skeletal meshes & their animations. This is done via two stages:

* Firstly, you must create the mesh with skeletal vertex data + node data;
* Secondly, you must add animation definitions.

??? abstract "How are Skeletal Animations Defined?"
	Vertex-skinning animations work by first defining a tree of skeletal nodes. There is always one root/parent node, and every other node is either a child of this root or of another node further down the tree hierarchy.
	
	Additionally, some nodes in this hierarchy will be labelled as bones. Not all nodes are bones, but those that are will directly be used to define how the mesh's vertices will transform under animation. Non-bone nodes are still useful as they define interim transformations along the skeletal hierarchy (e.g. imagine a chest node that is not itself a bone but can twist/bend- connected bones such as arms still need to follow this chest node even if there are no vertices *directly* affected by it).
	
	A skeletal mesh must supply the typical mesh vertex data for a mesh as well as bone weightings for each vertex. These weightings define how bone nodes in the node tree affect each vertex individually. Each vertex in TinyFFR can be affected by up to four bones simultaneously.
	
	Animations are then added as lists of time-series keyframes that define how each node transforms over time (scaling, rotating, and translating). When applying an animation, TinyFFR walks the nodal tree starting from the parent/root node, applying the node-local transform for each node as it goes. Each node's transform is applied cumulatively- meaning a transform on the root node affects all child nodes, and so on.

### CreateMesh()

The `IMeshBuilder` interface offers overloads for `CreateMesh()` that accept a span of `MeshVertexSkeletal` instances (instead of plain `MeshVertex`) alongside a span of `SkeletalAnimationNode`s.

#### MeshVertexSkeletal

Each `MeshVertexSkeletal` requires the standard vertex data as defined in [Meshes](meshes.md#meshvertex), as well as the following additional properties:

* __BoneIndices__ :material-arrow-right: This is an [inline array](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-12.0/inline-arrays) of four bytes, each one indexes a bone in the skeleton's array of bones (bones are nodes, but not all nodes are bones). Each index is paired with a `BoneWeight`, together they are used to define how this vertex will be transformed in model space when applying animations to the target nodes.
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

After creating a skeletal mesh, you can use `IMeshBuilder.AttachAnimation()` to attach animations to it.

Each animation requires the following arguments passed to `AttachAnimation()`:

<span class="def-icon">:material-code-json:</span> `mesh`

:   This is the corresponding `Mesh` that was created above.

<span class="def-icon">:material-code-json:</span> `scalingKeyframes`, `rotationKeyframes`, `translationKeyframes`

:   These are the time-series transform lists- each represents a timepoint in the animation and a corresponding scaling, rotation, or translation of a node. The affected node for each is defined later in the `boneMutations` argument.

	It is a requirement that all keyframes supplied in these span are ordered by time (ascending, e.g. starting at 0 seconds and moving forward in time). The time points do not need to be the same in each span (the spans do not even need to have the same number of elements).
	
	It is required that each span has a length of at least `1`.
	
<span class="def-icon">:material-code-json:</span> `boneMutations`

:   This is a span detailing how the transform keyframes specified above should be applied to each `SkeletalAnimationNode`. Essentially, this span acts as a "lookup" or "index" mapping the keyframe data supplied above to the skeletal node tree. 

	The `TargetNodeIndex` for each mutation indicates which node this mutation is indexing, and the `[...]KeyframeStartIndex` and `[...]KeyframeCount` define which `scalingKeyframes`, `rotationKeyframes`, or `translationKeyframes` are applicable to it.
	
<span class="def-icon">:material-code-json:</span> `defaultCompletionTimeSeconds`

:   This determines how long in seconds the animation should take to play by default.

<span class="def-icon">:material-code-json:</span> `name`

:   Every animation in TinyFFR must have a unique name.
	
### SetSkeletonNodeName()
	
This optional method on the `IMeshBuilder` allows you to set names for each node in a created skeletal mesh. This can be important if you need to look up those nodes later by name for e.g. getting their transform matrices post-animation.
