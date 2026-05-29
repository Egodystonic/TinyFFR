---
title: Vertex Mutation
description: Snippet demonstrating how to modify a model instance's vertices in realtime
---

## Code

Mark a mesh indicating its vertices can be modified post-creation and then modify those vertices:

```csharp
var mesh = meshBuilder.CreateMesh( // (1)!
	Cuboid.UnitCube, 
	centreTextureOrigin: false, 
	generationConfig: new(), 
	config: new() { AllowsPerInstanceVertexMutation = true }
);

var instance = factory.ObjectBuilder.CreateModelInstance(mesh, material);

var numVertices = mesh.GetVertexCountIfAllowsMutation(); // (2)!
var vertexBuffer = factory.ResourceAllocator.CreatePooledMemoryBuffer<MeshVertex>(numVertices); // (3)!
_ = mesh.CopyNonModifiedVerticesIfAllowsMutation(vertexBuffer.Span); // (4)!

// ... Typically per-frame:

_ = instance.CopyModifiedVerticesIfAllowsMutation(vertexBuffer.Span); // (5)!

var modifiedVertex = vertexBuffer[0] with { // (6)!
	Location = vertexBuffer[0].Location.AsVect().WithLengthIncreasedBy(deltaTime).AsLocation() 
};
instance.ModifyVertices(0, new ReadOnlySpan<MeshVertex>(in modifiedVertex)); // (7)!

ReadOnlySpan<MeshVertex> extraVertices = stackalloc[] { // (8)!
	vertexBuffer[15] with { 
		Location = vertexBuffer[15].Location.AsVect().WithLengthIncreasedBy(deltaTime).AsLocation() 
	},
	vertexBuffer[16],
	vertexBuffer[17] with { 
		Location = vertexBuffer[17].Location.AsVect().WithLengthIncreasedBy(deltaTime).AsLocation() 
	},
};
instance.ModifyVertices(15, extraVertices); // (9)!
```

1. 	In this example we're creating a cuboid mesh, but this setup works equally well when loading an asset from a file.

	The important part is setting `AllowsPerInstanceVertexMutation` to `true` in the `MeshCreationConfig`. If you don't set this, the methods below will not work (they will throw exceptions instead).
	
	You can check whether a mesh was created with the `AllowsPerInstanceVertexMutation` flag by using its `AllowsPerInstanceVertexMutation` property (i.e. `mesh.AllowsPerInstanceVertexMutation`).
	
2.	The gets the number of vertices that the mesh is comprised of.

	This method will throw an exception if `mesh.AllowsPerInstanceVertexMutation` is `false`.

3.	This creates a place to store the current or default vertices. You can just use a standard array if you don't need a low-GC pathway.

	You can skip this step if you don't need to read back the default/current vertices at any point.
	
4.	This copies the default (non-mutated) vertices for the `mesh` in to `vertexBuffer`.

	This method will throw an exception if `mesh.AllowsPerInstanceVertexMutation` is `false`.
	
5.	This copies the current state of the vertices (including all prior mutations) for this `instance` in to `vertexBuffer`.

	This method will throw an exception if `mesh.AllowsPerInstanceVertexMutation` is `false`.
	
6.	This creates a new `MeshVertex` that is a slight modification of the existing one at `vertexBuffer[0]`.

7.	This replaces the vertex at index `0` for this `instance` with `modifiedVertex`.

	This method will throw an exception if `mesh.AllowsPerInstanceVertexMutation` is `false`.
	
8.	This creates a small buffer of three vertices. You can just use a standard array if you don't need a low-GC pathway.

9.	This replaces three vertices (because there are three vertices in `extraVertices`) in `instance` starting at index `15`.

	This method will throw an exception if `mesh.AllowsPerInstanceVertexMutation` is `false`.

## Explanation

For certain types of effect you may wish to be able to programmatically modify the vertices of any `ModelInstance`'s underlying `Mesh` object per-frame. Setting `AllowsPerInstanceVertexMutation = true` in the `MeshCreationConfig` when loading/creating a mesh permits this.

It is permitted to modify any property of any vertex or number of vertices, including their location, texture coords, and tangent rotation.

!!! warning "Non-Skeletal Vertices Only"
	Vertex mutation currently only supports non-skeletal meshes.
	
	Note that vertex mutation is not the same as skeletal vertex skinning, if you want to use skeletal animations see [Animations](/tutorials/animations.md) instead.

### Memory Usage

Normally when you create/load a mesh asset its data is stored on VRAM but not RAM. However, when a mesh is marked as as vertex-mutable its vertices must be stored in a CPU-side buffer too. Furthermore, each mutated `ModelInstance` maintains its own buffer in addition. Therefore, it is recommended to not mark meshes as vertex-mutable unless necessary.

### Performance

Vertex mutation is a reasonably quick process on the CPU-side (just involving memory copies) but the subsequent alteration of the data on the GPU does also incur a small penalty. Although this impact is relatively small, it can add up if you're doing a lot of vertex mutation every frame. Therefore, try to seek alternative approaches to vertex mutation where possible if you need to modify large numbers of instances.

