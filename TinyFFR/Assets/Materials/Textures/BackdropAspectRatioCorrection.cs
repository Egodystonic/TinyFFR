// Created on 2026-10-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Specifies how a high-dynamic-range <c>.hdr</c> image that is not already a supported shape is adjusted before it is converted in to a <see cref="BackdropTexture"/>.
/// </summary>
/// <remarks>
/// <para>
/// Backdrop textures can only be created from images that are either 2:1 equirectangular panoramas (twice as wide as they are tall), or cross-shaped
/// cubemaps (4:3 or 3:4, where the longer side is a power of two). Images in any other shape (such as a 16:9 render of a sky or nebula) are
/// adjusted to a 2:1 panorama according to this setting before being converted.
/// </para>
/// <para>
/// Images that are already a supported shape are never altered. Only <c>.hdr</c> images can be adjusted; <c>.exr</c> images must already be a supported shape.
/// </para>
/// </remarks>
public enum BackdropAspectRatioCorrection {
	/// <summary>
	/// Resamples the image to a 2:1 aspect ratio, stretching it to wrap all the way around the scene.
	/// The whole image is kept, but it is distorted, and its left and right edges meet on the far side of the backdrop, which can show a seam if they differ.
	/// </summary>
	Stretch,
	/// <summary>
	/// Places the image next to a horizontally-mirrored copy of itself and resamples the pair to a 2:1 aspect ratio.
	/// The image appears twice, but there is no visible seam where its edges meet.
	/// </summary>
	MirrorTile,
	/// <summary>
	/// Centers the image unaltered in a 2:1 panorama, filling the rest of the panorama with the average color of the whole image.
	/// The image is not distorted, and the filled area lights the scene in proportion to the image's overall brightness, but there is usually a visible step where the image meets the fill.
	/// </summary>
	PadWithAverageColor,
	/// <summary>
	/// Centers the image unaltered in a 2:1 panorama, filling the rest of the panorama with the average colors found along the image's edges.
	/// Bands to the sides of the image fade from the color of its right edge to the color of its left edge, so that they blend in to the image on both sides;
	/// bands above and below the image are filled with the color of its top and bottom edges respectively.
	/// </summary>
	PadWithEdgeColor,
	/// <summary>
	/// Throws an exception if the image is not already a supported shape, instead of adjusting it.
	/// </summary>
	Throw
}
