// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Builder interface that allows you to create <see cref="Scene"/>s and <see cref="CanvasScene"/>s.
/// </summary>
public interface ISceneBuilder {
	/// <summary>
	/// Creates a new empty <see cref="Scene"/> with a default backdrop.
	/// </summary>
	/// <param name="name">Optional name for the new scene.</param>
	Scene CreateScene(ReadOnlySpan<char> name = default) {
		return CreateScene(new SceneCreationConfig { Name = name });
	}
	/// <summary>
	/// Creates a new empty <see cref="Scene"/> with a flat colour as its backdrop.
	/// </summary>
	/// <remarks>
	/// The colour also supplies the scene's ambient light, so a mid-grey lights the scene evenly from every direction whilst a near-black leaves it almost unlit.
	/// </remarks>
	/// <param name="backdropColor">The colour to fill the background with, or <see langword="null"/> for no backdrop at all.</param>
	/// <param name="name">Optional name for the new scene.</param>
	Scene CreateScene(ColorVect? backdropColor, ReadOnlySpan<char> name = default) {
		return CreateScene(new SceneCreationConfig { InitialBackdropColor = backdropColor, Name = name });
	}
	/// <summary>
	/// Creates a new empty <see cref="Scene"/> with one of the built-in backdrops.
	/// </summary>
	/// <param name="backdrop">Which built-in backdrop to use.</param>
	/// <param name="name">Optional name for the new scene.</param>
	Scene CreateScene(BuiltInSceneBackdrop backdrop, ReadOnlySpan<char> name = default) {
		return CreateScene(new SceneCreationConfig { InitialBackdrop = backdrop, Name = name });
	}
	/// <summary>
	/// Creates a new empty <see cref="Scene"/> with a loaded <see cref="BackdropTexture"/> as its backdrop.
	/// </summary>
	/// <param name="backdrop">The image to use as the backdrop.</param>
	/// <param name="name">Optional name for the new scene.</param>
	Scene CreateScene(BackdropTexture backdrop, ReadOnlySpan<char> name = default) {
		return CreateScene(new SceneCreationConfig { InitialBackdropTexture = backdrop, Name = name });
	}
	/// <summary>
	/// Creates a new empty <see cref="Scene"/> according to the given <paramref name="config"/>.
	/// </summary>
	/// <param name="config">Configuration for the new scene, including its name and backdrop.</param>
	Scene CreateScene(in SceneCreationConfig config);

	/// <summary>
	/// Creates a new empty <see cref="CanvasScene"/> (used for drawing 2D content).
	/// </summary>
	/// <param name="name">Optional name for the new canvas.</param>
	CanvasScene CreateCanvasScene(ReadOnlySpan<char> name = default) {
		return CreateCanvasScene(new CanvasSceneCreationConfig { Name = name });
	}
	/// <summary>
	/// Creates a new empty <see cref="CanvasScene"/> (used for drawing 2D content) according to the given <paramref name="config"/>.
	/// </summary>
	/// <param name="config">Configuration for the new canvas, including its name and background colour.</param>
	CanvasScene CreateCanvasScene(in CanvasSceneCreationConfig config);
}