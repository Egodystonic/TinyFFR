// Created on 2024-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

interface IMockAlphaResourceImplProvider : IResourceImplProvider<MockResourceAlpha>;
interface IMockBravoResourceImplProvider : IResourceImplProvider<MockResourceBravo>;

readonly struct MockResourceAlpha : IResource<MockResourceAlpha, IMockAlphaResourceImplProvider> {
	readonly string? _name;
	public ReadOnlySpan<char> Name {
		get => _name;
		init => _name = value.ToString();
	}
	public bool Equals(MockResourceAlpha other) => Handle.Equals(other.Handle) && Implementation.Equals(other.Implementation);
	public static MockResourceAlpha CreateFromHandleAndImpl(ResourceHandle<MockResourceAlpha> handle, IResourceImplProvider impl) {
		return new() { Handle = handle, Implementation = (IMockAlphaResourceImplProvider) impl };
	}
	public ResourceHandle<MockResourceAlpha> GetHandleWithoutDisposeCheck() => Handle;
	public ResourceHandle<MockResourceAlpha> Handle { get; init; }
	public IMockAlphaResourceImplProvider Implementation { get; init; }
	public string GetNameAsNewStringObject() => _name ?? "";
	public int GetNameLength() => GetNameAsNewStringObject().Length;
	public void CopyName(Span<char> destinationBuffer) => GetNameAsNewStringObject().CopyTo(destinationBuffer);
}
readonly struct MockResourceBravo : IResource<MockResourceBravo, IMockBravoResourceImplProvider> {
	readonly string? _name;
	public ReadOnlySpan<char> Name {
		get => _name;
		init => _name = value.ToString();
	}
	public bool Equals(MockResourceBravo other) => Handle.Equals(other.Handle) && Implementation.Equals(other.Implementation);
	public static MockResourceBravo CreateFromHandleAndImpl(ResourceHandle<MockResourceBravo> handle, IResourceImplProvider impl) {
		return new() { Handle = handle, Implementation = (IMockBravoResourceImplProvider) impl };
	}
	public ResourceHandle<MockResourceBravo> GetHandleWithoutDisposeCheck() => Handle;
	public ResourceHandle<MockResourceBravo> Handle { get; init; }
	public IMockBravoResourceImplProvider Implementation { get; init; }
	public string GetNameAsNewStringObject() => _name ?? "";
	public int GetNameLength() => GetNameAsNewStringObject().Length;
	public void CopyName(Span<char> destinationBuffer) => GetNameAsNewStringObject().CopyTo(destinationBuffer);
}
class MockAlphaResourceImplProvider : IMockAlphaResourceImplProvider {
	public Func<ResourceHandle, string>? OnGetNameAsNewStringObject;
	public Func<ResourceHandle, int>? OnGetNameLength;
	public Action<ResourceHandle, char[]>? OnCopyName;

	public string GetNameAsNewStringObject(ResourceHandle<MockResourceAlpha> handle) => OnGetNameAsNewStringObject?.Invoke(handle) ?? handle.ToString();
	public int GetNameLength(ResourceHandle<MockResourceAlpha> handle) => OnGetNameLength?.Invoke(handle) ?? GetNameAsNewStringObject(handle).Length;
	public void CopyName(ResourceHandle<MockResourceAlpha> handle, Span<char> destinationBuffer) {
		if (OnCopyName == null) {
			GetNameAsNewStringObject(handle).CopyTo(destinationBuffer);
			return;
		}
		var arr = new char[destinationBuffer.Length];
		OnCopyName(handle, arr);
		arr.CopyTo(destinationBuffer);
	}
}
class MockBravoResourceImplProvider : IMockBravoResourceImplProvider {
	public Func<ResourceHandle, string>? OnGetNameAsNewStringObject;
	public Func<ResourceHandle, int>? OnGetNameLength;
	public Action<ResourceHandle, char[]>? OnCopyName;

	public string GetNameAsNewStringObject(ResourceHandle<MockResourceBravo> handle) => OnGetNameAsNewStringObject?.Invoke(handle) ?? handle.ToString();
	public int GetNameLength(ResourceHandle<MockResourceBravo> handle) => OnGetNameLength?.Invoke(handle) ?? GetNameAsNewStringObject(handle).Length;
	public void CopyName(ResourceHandle<MockResourceBravo> handle, Span<char> destinationBuffer) {
		if (OnCopyName == null) {
			GetNameAsNewStringObject(handle).CopyTo(destinationBuffer);
			return;
		}
		var arr = new char[destinationBuffer.Length];
		OnCopyName(handle, arr);
		arr.CopyTo(destinationBuffer);
	}
}