// Created on 2026-03-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly record struct SkeletalAnimationNode(Matrix4x4 DefaultLocalTransform, Matrix4x4 BindPoseInversion, int? ParentNodeIndex, int? CorrespondingBoneIndex);