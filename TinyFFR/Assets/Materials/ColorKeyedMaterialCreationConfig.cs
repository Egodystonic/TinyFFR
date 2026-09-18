// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.World;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Describes a material whose appearance is driven entirely by one key texture.
/// </summary>
/// <remarks>
/// Color-keyed materials use a key teture whose R, G, B, and A values represent a strength mapping of per-object colour values
/// (set via <see cref="ModelInstance.SetKeyedMaterialColor"/>.
/// </remarks>
public readonly ref struct ColorKeyedMaterialCreationConfig : IConfigStruct<ColorKeyedMaterialCreationConfig> {
	static char[]? _keyMapParameterString = null;
	/// <summary>
	/// The name the shader uses for the key map, for use when setting it through a material's parameters directly.
	/// </summary>
	/// <remarks>
	/// You only need this when addressing a material's shader parameters by name; creating a material through
	/// the material builder never requires it.
	/// </remarks>
	public static ReadOnlySpan<char> KeyMapParameterString => MaterialCreationConfig.GetOrCreateParameterString(ref _keyMapParameterString, LocalShaderPackageConstants.ColorKeyedMaterialShader.ParamKeyMap);

	/// <summary>
	/// The texture whose colours select what is drawn, and where. This property is required.
	/// </summary>
	/// <remarks>
	/// Can be a three or four channel texture..
	/// </remarks>
	public required Texture KeyMap { get; init; }
	
	/// <summary>
	/// Whether the material's output blends with the scene behind it rather than replacing it. Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// Setting this to <c>true</c> turns on alpha-sorting for this material which allows setting key colours with translucency,
	/// but alpha-sorting can sometimes "flicker" against similar objects sharing the same space in the world. Alpha-sorting also
	/// has a slight additional performance cost.
	/// </remarks>
	public bool BlendOutputAlphaWithScene { get; init; } = false;

	/// <summary>
	/// The settings common to every kind of material, such as its name.
	/// </summary>
	public MaterialCreationConfig BaseConfig { get; private init; } = new();
	
	/// <summary>
	/// The name to give the material. May be left empty.
	/// </summary>
	/// <remarks>
	/// This is <see cref="MaterialCreationConfig.Name"/> on <see cref="BaseConfig"/>, surfaced here for convenience.
	/// </remarks>
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	/// <summary>
	/// Constructs a new <see cref="ColorKeyedMaterialCreationConfig"/> with default values for every property except <see cref="KeyMap"/>, which must be supplied.
	/// </summary>
	public ColorKeyedMaterialCreationConfig() { }
	
	/// <summary>
	/// Constructs a new <see cref="ColorKeyedMaterialCreationConfig"/> from the given common settings.
	/// </summary>
	/// <param name="baseConfig">The value for <see cref="BaseConfig"/>.</param>
	public ColorKeyedMaterialCreationConfig(MaterialCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
		if (KeyMap == default) throw InvalidObjectException.InvalidDefault<Texture>(nameof(KeyMap));
	}

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in ColorKeyedMaterialCreationConfig src) {
		return	SerializationSizeOfResource() // KeyMap
			+	SerializationSizeOfBool() // OutputIncludesAlphaChannel
			+	SerializationSizeOfSubConfig(src.BaseConfig); // BaseConfig
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in ColorKeyedMaterialCreationConfig src) {
		SerializationWriteAndAllocateResource(ref dest, src.KeyMap);
		SerializationWriteBool(ref dest, src.BlendOutputAlphaWithScene);
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
	}
	/// <inheritdoc />
	public static ColorKeyedMaterialCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new ColorKeyedMaterialCreationConfig {
			KeyMap = SerializationReadResource<Texture>(ref src),
			BlendOutputAlphaWithScene = SerializationReadBool(ref src),
			BaseConfig = SerializationReadSubConfig<MaterialCreationConfig>(ref src)
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeResourceHandle(src);
	}
}
