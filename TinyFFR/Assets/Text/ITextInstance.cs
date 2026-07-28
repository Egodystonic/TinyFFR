using System;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Text;

public interface ITextInstance : IDisposable, IStringSpanNameEnabled {
	FontPen Pen { get; set; }
	FontString String { get; set; }
}