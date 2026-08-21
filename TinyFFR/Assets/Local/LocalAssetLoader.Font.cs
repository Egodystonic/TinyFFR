// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.IO;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Rendering.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Assets.Local;

unsafe partial class LocalAssetLoader : IResourceDirectory<Font> {
	readonly LocalFontLoader _fontLoader;

	public Font LoadFont(BuiltInFont font, in FontCreationConfig config) {
		ThrowIfThisIsDisposed();
		return _fontLoader.LoadFont(font, in config);
	}
	public Font LoadFont(ReadOnlySpan<char> fontFilePath, in FontCreationConfig config) {
		ThrowIfThisIsDisposed();
		return _fontLoader.LoadFont(fontFilePath, in config);
	}

	public TinyFfrAsyncOperation<Font> LoadFontAsync(BuiltInFont font, in FontCreationConfig config) {
		ThrowIfThisIsDisposed();
		return _fontLoader.LoadFontAsync(font, in config);
	}
	public TinyFfrAsyncOperation<Font> LoadFontAsync(ReadOnlySpan<char> fontFilePath, in FontCreationConfig config) {
		ThrowIfThisIsDisposed();
		return _fontLoader.LoadFontAsync(fontFilePath, in config);
	}

	IndirectEnumerable<object, Font> IResourceDirectory<Font>.AllActiveInstances {
		get {
			ThrowIfThisIsDisposed();
			return _fontLoader.AllActiveInstances;
		}
	}
	bool IResourceDirectory<Font>.ResourceNameMatchIsMatching(Font resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		ThrowIfThisIsDisposed();
		return _fontLoader.ResourceNameMatchIsMatching(resource, name, allowPartialMatch, comparisonType);
	}
}