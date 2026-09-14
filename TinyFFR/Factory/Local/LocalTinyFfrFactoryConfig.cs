// Created on 2024-10-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Factory.Local;

/// <summary>
/// Enum used for setting a selected <see cref="LocalTinyFfrFactoryConfig.MemoryUsageRubric"/> when constructing a local factory. 
/// </summary>
public enum MemoryUsageRubric {
	/// <summary>
	/// TinyFFR is permitted to use as much memory as it deems necessary to provide a high performance threshold.  
	/// </summary>
	Standard = 0,
	/// <summary>
	/// TinyFFR will reduce its memory footprint at the potential cost of performance.
	/// </summary>
	UseLessMemory = 1,
	/// <summary>
	/// TinyFFR will use as little memory as possible, regardless of the performance cost.
	/// </summary>
	UseSignificantlyLessMemory = 2
}

/// <summary>
/// Object used to configure a <see cref="Egodystonic.TinyFFR.Factory.Local.LocalTinyFfrFactory"/>'s general settings.
/// </summary>
public sealed class LocalTinyFfrFactoryConfig {
	/// <summary>
	/// The maximum permitted value for <see cref="MaxCpuToGpuAssetTransferSizeBytes"/>: <c>536,870,912</c>.
	/// </summary>
	public const int MaxMaxCpuToGpuAssetTransferSizeBytes = 1 << 29;
	/// <summary>
	/// The default value for <see cref="MaxCpuToGpuAssetTransferSizeBytes"/>: <c>104,857,600</c>.
	/// </summary>
	public const int DefaultMaxCpuToGpuAssetTransferSizeBytes = 1024 * 1024 * 100; // 100 MB  
	/// <summary>
	/// The default value for <see cref="MemoryUsageRubric"/>: <c>Standard</c>.
	/// </summary>
	public static readonly MemoryUsageRubric DefaultMemoryUsageRubric = MemoryUsageRubric.Standard;
	/// <summary>
	/// The default value for <see cref="EnhanceSecurity"/>: <c>false</c>.
	/// </summary>
	public const bool DefaultEnhanceSecurity = false;
	/// <summary>
	/// The default value for <see cref="HeadlessMode"/>: <c>false</c>.
	/// </summary>
	public const bool DefaultHeadlessMode = false;

	/// <summary>
	/// Maximum size, in bytes, of any loaded asset (mesh, texture, etc). Higher values must reserve more RAM.
	/// Defaults to <see cref="DefaultMaxCpuToGpuAssetTransferSizeBytes"/>.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if non-positive or greater than <see cref="MaxMaxCpuToGpuAssetTransferSizeBytes"/>.</exception>
	public int MaxCpuToGpuAssetTransferSizeBytes {
		get;
		init {
			if (value is <= 0 or > MaxMaxCpuToGpuAssetTransferSizeBytes) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max asset size must be between 1 and {MaxMaxCpuToGpuAssetTransferSizeBytes} bytes.");
			}

			field = value;
		}
	} = DefaultMaxCpuToGpuAssetTransferSizeBytes;

	/// <summary>
	/// Setting this property allows you to moderate how much RAM TinyFFR uses (at the cost of its performance ceiling).
	/// Defaults to <see cref="DefaultMemoryUsageRubric"/>.
	/// </summary>
	public MemoryUsageRubric MemoryUsageRubric {
		get;
		init {
			if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value), value, null);
			field = value;
		}
	} = DefaultMemoryUsageRubric;
	
	/// <summary>
	/// If true, under certain conditions where TinyFFR encounters an error (usually when loading asset data) that may
	/// leave memory in an uncertain condition, rather than attempt to recover (which may, in crafted scenarios, leak memory/data),
	/// it instead opts to <see cref="Environment.FailFast(string)">fail fast</see>.
	/// Defaults to <see cref="DefaultEnhanceSecurity"/>.
	/// </summary>
	public bool EnhanceSecurity { get; init; } = DefaultEnhanceSecurity;
	
	/// <summary>
	/// The threading config.
	/// </summary>
	public ThreadingConfig ThreadingConfig { get; init; } = new();

	/// <summary>
	/// If <c>true</c>, TinyFFR will not interact with the local system's rendering, windowing, and input subsystems.
	/// Defaults to <c>false</c>. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// There are generally two cases where you'd set this to <c>true</c>:
	/// <ul>
	/// <li>
	/// When integrating TinyFFR with a UI framework (e.g. Avalonia, WPF, or Windows Forms) you generally want the host application framework
	/// to control the rendering, display driving, and input. In fact, having TinyFFR attempt to <i>simultaneously</i> control those subsystems
	/// of the host machine will more-often-than-not cause conflicts; and therefore it's often better to run TinyFFR in headless mode in these cases.
	/// </li>
	/// <li>
	/// If you're attempting to run TinyFFR in a server environment or on a specialized operating system with a bespoke or non-existent shell it may
	/// crash when attempting to request OS services/APIs that are not present. Running TinyFFR in headless mode avoids attempting to instrument these
	/// APIs entirely, therefore avoiding this issue.
	/// </li>
	/// </ul>
	/// </para>
	/// <para>
	/// In headless mode, you can still render to <see cref="Rendering.RenderOutputBuffer">output buffers</see> and UI framework control panels. However
	/// you will not be able to create windows, discover displays, or access input data when looping frames.
	/// </para>
	/// <para>
	/// This means the <see cref="Egodystonic.TinyFFR.Environment.Local.IWindowBuilder">window builder</see>,
	/// <see cref="Egodystonic.TinyFFR.Environment.Local.IDisplayDiscoverer">display discoverer</see>, and
	/// built-in <see cref="Egodystonic.TinyFFR.Environment.Input.ILatestInputRetriever">input retriever</see> will be inoperable
	/// (though the one provided via UI framework loop will still work as that gathers its data from the framework itself).
	/// </para>
	/// </remarks>
	public bool HeadlessMode { get; init; } = DefaultHeadlessMode;
}