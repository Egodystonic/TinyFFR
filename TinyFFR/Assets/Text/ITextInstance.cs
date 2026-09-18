using System;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Text;

/// <summary>
/// Common ground between the kinds of text object, so that either can be held, read and disposed without knowing which it is.
/// </summary>
public interface ITextInstance : IDisposable, IStringSpanNameEnabled {
	/// <summary>
	/// The pen this text is drawn with, which supplies its colours.
	/// </summary>
	FontPen Pen { get; set; }
	/// <summary>
	/// The prepared string this text displays.
	/// </summary>
	/// <remarks>
	/// Setting this replaces what the object shows. Preparing a string is not free, so where text changes every frame it is
	/// usually better to keep a small set of prepared strings than to build a new one each time.
	/// </remarks>
	FontString String { get; set; }
}