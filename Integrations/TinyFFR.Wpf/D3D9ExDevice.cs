// Created on 2026-11-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Runtime.InteropServices;

namespace Egodystonic.TinyFFR.Wpf;

sealed unsafe class D3D9ExDevice : IDisposable {
	const uint D3DSdkVersion = 32U;
	const uint D3DDevTypeHal = 1U;
	const uint D3DCreateFpuPreserve = 0x2U;
	const uint D3DCreateMultithreaded = 0x4U;
	const uint D3DCreateHardwareVertexProcessing = 0x40U;
	const uint D3DFormatUnknown = 0U;
	const uint D3DFormatA8R8G8B8 = 21U;
	const uint D3DSwapEffectDiscard = 1U;
	const uint D3DPresentIntervalImmediate = 0x80000000U;
	const uint D3DUsageRenderTarget = 0x1U;
	const uint D3DPoolDefault = 0U;
	const int LuidLengthBytes = 8;

	const int VtableRelease = 2;
	const int Direct3D9VtableGetAdapterCount = 4;
	const int Direct3D9ExVtableCreateDeviceEx = 20;
	const int Direct3D9ExVtableGetAdapterLuid = 21;
	const int DeviceVtableCreateTexture = 23;
	const int TextureVtableGetSurfaceLevel = 18;

	[StructLayout(LayoutKind.Sequential)]
	struct PresentParameters {
		public uint BackBufferWidth;
		public uint BackBufferHeight;
		public uint BackBufferFormat;
		public uint BackBufferCount;
		public uint MultiSampleType;
		public uint MultiSampleQuality;
		public uint SwapEffect;
		public nint DeviceWindow;
		public int Windowed;
		public int EnableAutoDepthStencil;
		public uint AutoDepthStencilFormat;
		public uint Flags;
		public uint FullScreenRefreshRateInHz;
		public uint PresentationInterval;
	}

	[DllImport("d3d9.dll")]
	static extern int Direct3DCreate9Ex(uint sdkVersion, out nint direct3D9Ex);
	[DllImport("user32.dll")]
	static extern nint GetDesktopWindow();

	nint _direct3D;
	nint _device;

	D3D9ExDevice(nint direct3D, nint device) {
		_direct3D = direct3D;
		_device = device;
	}

	static void** Vtable(nint comObject) => *(void***) comObject;

	static void Release(nint comObject) {
		if (comObject == 0) return;
		((delegate* unmanaged[Stdcall]<nint, uint>) Vtable(comObject)[VtableRelease])(comObject);
	}

	public static D3D9ExDevice? TryCreate(ReadOnlySpan<byte> adapterLuid, out string? failureReason) {
		if (adapterLuid.Length != LuidLengthBytes) throw new ArgumentException($"Adapter LUID must be {LuidLengthBytes} bytes long.", nameof(adapterLuid));
		if (Direct3DCreate9Ex(D3DSdkVersion, out var direct3D) < 0 || direct3D == 0) {
			failureReason = "Direct3D 9Ex is not available";
			return null;
		}

		var vtable = Vtable(direct3D);
		var adapterCount = ((delegate* unmanaged[Stdcall]<nint, uint>) vtable[Direct3D9VtableGetAdapterCount])(direct3D);
		var matchingAdapter = -1L;
		Span<byte> candidateLuid = stackalloc byte[LuidLengthBytes];
		for (var adapter = 0U; adapter < adapterCount; ++adapter) {
			fixed (byte* candidateLuidPtr = candidateLuid) {
				if (((delegate* unmanaged[Stdcall]<nint, uint, byte*, int>) vtable[Direct3D9ExVtableGetAdapterLuid])(direct3D, adapter, candidateLuidPtr) < 0) continue;
			}
			if (!candidateLuid.SequenceEqual(adapterLuid)) continue;
			matchingAdapter = adapter;
			break;
		}
		if (matchingAdapter < 0) {
			Release(direct3D);
			failureReason = $"no Direct3D 9 adapter matches TinyFFR's GPU (LUID {Convert.ToHexString(adapterLuid)})";
			return null;
		}

		var presentParameters = new PresentParameters {
			BackBufferWidth = 1U,
			BackBufferHeight = 1U,
			BackBufferFormat = D3DFormatUnknown,
			BackBufferCount = 1U,
			SwapEffect = D3DSwapEffectDiscard,
			DeviceWindow = GetDesktopWindow(),
			Windowed = 1,
			PresentationInterval = D3DPresentIntervalImmediate
		};
		nint device;
		var createResult = ((delegate* unmanaged[Stdcall]<nint, uint, uint, nint, uint, PresentParameters*, void*, nint*, int>) vtable[Direct3D9ExVtableCreateDeviceEx])(
			direct3D,
			(uint) matchingAdapter,
			D3DDevTypeHal,
			presentParameters.DeviceWindow,
			D3DCreateHardwareVertexProcessing | D3DCreateMultithreaded | D3DCreateFpuPreserve,
			&presentParameters,
			null,
			&device
		);
		if (createResult < 0 || device == 0) {
			Release(direct3D);
			failureReason = $"creating the Direct3D 9Ex device failed (HRESULT 0x{createResult:X8})";
			return null;
		}

		failureReason = null;
		return new D3D9ExDevice(direct3D, device);
	}

	public bool TryOpenSharedSurface(int width, int height, nint legacySharedHandle, out nint texture, out nint surface) {
		ObjectDisposedException.ThrowIf(_device == 0, this);
		texture = 0;
		surface = 0;
		var sharedHandle = legacySharedHandle;
		nint openedTexture;
		var textureResult = ((delegate* unmanaged[Stdcall]<nint, uint, uint, uint, uint, uint, uint, nint*, nint*, int>) Vtable(_device)[DeviceVtableCreateTexture])(
			_device,
			(uint) width,
			(uint) height,
			1U,
			D3DUsageRenderTarget,
			D3DFormatA8R8G8B8,
			D3DPoolDefault,
			&openedTexture,
			&sharedHandle
		);
		if (textureResult < 0 || openedTexture == 0) return false;

		nint openedSurface;
		var surfaceResult = ((delegate* unmanaged[Stdcall]<nint, uint, nint*, int>) Vtable(openedTexture)[TextureVtableGetSurfaceLevel])(openedTexture, 0U, &openedSurface);
		if (surfaceResult < 0 || openedSurface == 0) {
			Release(openedTexture);
			return false;
		}

		texture = openedTexture;
		surface = openedSurface;
		return true;
	}

	public static void ReleaseSharedSurface(nint texture, nint surface) {
		Release(surface);
		Release(texture);
	}

	public void Dispose() {
		Release(_device);
		Release(_direct3D);
		_device = 0;
		_direct3D = 0;
	}
}
