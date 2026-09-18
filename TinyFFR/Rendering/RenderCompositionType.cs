// Created on 2026-05-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// Specifies how a <see cref="Renderer"/>'s output should combine with whatever was already drawn to the same target by previously-added renderers, when added to a <see cref="RendererCompositor"/> (see <see cref="RendererCompositor.Add"/>).
/// </summary>
public enum RenderCompositionType {
	/// <summary>
	/// This renderer's output fully replaces whatever pixel data was already at the target, as if it were the only thing being rendered.
	/// </summary>
	Standard,
	/// <summary>
	/// This renderer's output is drawn on top of whatever was already rendered to the target,
	/// letting previously-composited layers show through wherever this renderer doesn't otherwise draw over them.
	/// Note that this effect requires the rendered <see cref="Renderer.TargetScene">target scene</see> to have no backdrop set
	/// (see <see cref="Scene.RemoveBackdrop"/>).
	/// </summary>
	RetainPreviousScenes
}