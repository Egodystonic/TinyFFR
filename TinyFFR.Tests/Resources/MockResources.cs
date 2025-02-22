// Created on 2024-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

interface IMockResourceImplProvider : IResourceImplProvider;

readonly struct MockResourceAlpha : IResource<MockResourceAlpha, IMockResourceImplProvider> {
	readonly string? _name;
	public ReadOnlySpan<char> Name {
		get => _name;
		init => _name = value.ToString();
	}
	public bool Equals(MockResourceAlpha other) => Handle.Equals(other.Handle) && Implementation.Equals(other.Implementation);
	public static MockResourceAlpha CreateFromHandleAndImpl(ResourceHandle<MockResourceAlpha> handle, IResourceImplProvider impl) {
		return new() { Handle = handle, Implementation = (IMockResourceImplProvider) impl };
	}
	public ResourceHandle<MockResourceAlpha> Handle { get; init; }
	public IMockResourceImplProvider Implementation { get; init; }
}
readonly struct MockResourceBravo : IResource<MockResourceBravo, IMockResourceImplProvider> {
	readonly string? _name;
	public ReadOnlySpan<char> Name {
		get => _name;
		init => _name = value.ToString();
	}
	public bool Equals(MockResourceBravo other) => Handle.Equals(other.Handle) && Implementation.Equals(other.Implementation);
	public static MockResourceBravo CreateFromHandleAndImpl(ResourceHandle<MockResourceBravo> handle, IResourceImplProvider impl) {
		return new() { Handle = handle, Implementation = (IMockResourceImplProvider) impl };
	}
	public ResourceHandle<MockResourceBravo> Handle { get; init; }
	public IMockResourceImplProvider Implementation { get; init; }
}
public class MockResourceImplProvider : IMockResourceImplProvider {
	public Func<UIntPtr, string>? OnRawHandleGetName { get; set; }
	public ReadOnlySpan<char> GetName(ResourceHandle handle) => OnRawHandleGetName?.Invoke(handle) ?? "";
}