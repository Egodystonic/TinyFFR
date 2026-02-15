// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.IO;
using System.Threading;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using static Egodystonic.TinyFFR.Assets.Materials.TextureCombinationSourceTexture;
using static Egodystonic.TinyFFR.ColorChannel;

namespace Egodystonic.TinyFFR.Assets;

public readonly record struct TextureReadMetadata(XYPair<int> Dimensions, bool IncludesAlphaChannel);
public readonly record struct MeshReadMetadata(int TotalVertexCount, int TotalTriangleCount, int TotalBoneCount, int SubMeshCount);
public readonly record struct MeshReadCountData(int NumVerticesWritten, int NumTrianglesWritten);

public enum AnisotropyRadialAngleRange {
	ZeroTo360,
	ZeroTo180
}

public interface IAssetLoader {
	IMeshBuilder MeshBuilder { get; }
	IMaterialBuilder MaterialBuilder { get; }
	ITextureBuilder TextureBuilder { get; }
	IBuiltInTexturePathLibrary BuiltInTexturePaths { get; }

	#region Load / Read Texture
	Texture LoadTexture(ReadOnlySpan<char> filePath, bool isLinearColorspace, ReadOnlySpan<char> name = default) {
		return LoadTexture(
			filePath, 
			new TextureCreationConfig {
				IsLinearColorspace = isLinearColorspace,
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	Texture LoadTexture(ReadOnlySpan<char> filePath, in TextureCreationConfig config) => LoadTexture(filePath, in config, new TextureReadConfig());
	Texture LoadTexture(ReadOnlySpan<char> filePath, in TextureCreationConfig config, in TextureReadConfig readConfig);

	TextureReadMetadata ReadTextureMetadata(ReadOnlySpan<char> filePath);
	int ReadTexture<TTexel>(ReadOnlySpan<char> filePath, Span<TTexel> destinationBuffer) where TTexel : unmanaged, ITexel<TTexel> => ReadTexture(filePath, TextureProcessingConfig.None, destinationBuffer);
	int ReadTexture<TTexel>(ReadOnlySpan<char> filePath, in TextureProcessingConfig processingConfig, Span<TTexel> destinationBuffer) where TTexel : unmanaged, ITexel<TTexel>;
	#endregion

	#region Load Maps
	private static readonly Lock _staticMutationLock = new();
	private static readonly HeapPool _mapTextureProcessingPool = new();

	Texture LoadColorMap(ReadOnlySpan<char> filePath) => LoadTexture(filePath, isLinearColorspace: false, name: Path.GetFileName(filePath));

	Texture LoadNormalMap(ReadOnlySpan<char> filePath, bool isDirectXFormat = false) {
		if (!isDirectXFormat) return LoadTexture(filePath, isLinearColorspace: true, name: Path.GetFileName(filePath));
		return LoadTexture(
			filePath, 
			new TextureCreationConfig {
				IsLinearColorspace = true, 
				ProcessingToApply = new TextureProcessingConfig { InvertYGreenChannel = true },
				Name = Path.GetFileName(filePath)
			}
		);
	}

	Texture LoadOcclusionRoughnessMetallicMap(ReadOnlySpan<char> filePath) => LoadTexture(filePath, isLinearColorspace: true, name: Path.GetFileName(filePath));
	Texture LoadOcclusionRoughnessMetallicMap(ReadOnlySpan<char> occlusionFilePath, ReadOnlySpan<char> roughnessFilePath, ReadOnlySpan<char> metallicFilePath) {
		var a = Path.GetFileName(occlusionFilePath);
		var b = Path.GetFileName(roughnessFilePath);
		var c = Path.GetFileName(metallicFilePath);
		Span<char> name = stackalloc char[a.Length + 1 + b.Length + 1 + c.Length];
		a.CopyTo(name);
		name[a.Length] = '+';
		b.CopyTo(name[(a.Length + 1)..]);
		name[(a.Length + 1 + b.Length)] = '+';
		c.CopyTo(name[(a.Length + 1 + b.Length + 1)..]);

		return LoadCombinedTexture(
			occlusionFilePath,
			roughnessFilePath,
			metallicFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureB, R),
				OutputTextureZBlueChannelSource = new(TextureC, R)
			},
			new TextureCreationConfig {
				IsLinearColorspace = true,
				Name = name
			}
		);
	}
	Texture LoadOcclusionRoughnessMetallicReflectanceMap(ReadOnlySpan<char> filePath) {
		if (ReadTextureMetadata(filePath).IncludesAlphaChannel) return LoadTexture(filePath, isLinearColorspace: true, name: Path.GetFileName(filePath));
		else return LoadOcclusionRoughnessMetallicReflectanceMap(filePath, BuiltInTexturePaths.DefaultReflectanceMap);
	}
	Texture LoadOcclusionRoughnessMetallicReflectanceMap(ReadOnlySpan<char> occlusionRoughnessMetallicFilePath, ReadOnlySpan<char> reflectanceFilePath) {
		var a = Path.GetFileName(occlusionRoughnessMetallicFilePath);
		var b = Path.GetFileName(reflectanceFilePath);
		Span<char> name = stackalloc char[a.Length + 1 + b.Length];
		a.CopyTo(name);
		name[a.Length] = '+';
		b.CopyTo(name[(a.Length + 1)..]);

		return LoadCombinedTexture(
			occlusionRoughnessMetallicFilePath,
			reflectanceFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureA, B),
				OutputTextureWAlphaChannelSource = new(TextureB, R),
			},
			new TextureCreationConfig {
				IsLinearColorspace = true,
				Name = name
			}
		);
	}
	Texture LoadOcclusionRoughnessMetallicReflectanceMap(ReadOnlySpan<char> occlusionFilePath, ReadOnlySpan<char> roughnessFilePath, ReadOnlySpan<char> metallicFilePath, ReadOnlySpan<char> reflectanceFilePath) {
		var a = Path.GetFileName(occlusionFilePath);
		var b = Path.GetFileName(roughnessFilePath);
		var c = Path.GetFileName(metallicFilePath);
		var d = Path.GetFileName(reflectanceFilePath);
		Span<char> name = stackalloc char[a.Length + 1 + b.Length + 1 + c.Length + 1 + d.Length];
		a.CopyTo(name);
		name[a.Length] = '+';
		b.CopyTo(name[(a.Length + 1)..]);
		name[(a.Length + 1 + b.Length)] = '+';
		c.CopyTo(name[(a.Length + 1 + b.Length + 1)..]);
		name[(a.Length + 1 + b.Length + 1 + c.Length)] = '+';
		d.CopyTo(name[(a.Length + 1 + b.Length + 1 + c.Length + 1)..]);

		return LoadCombinedTexture(
			occlusionFilePath,
			roughnessFilePath,
			metallicFilePath,
			reflectanceFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureB, R),
				OutputTextureZBlueChannelSource = new(TextureC, R),
				OutputTextureWAlphaChannelSource = new(TextureD, R),
			},
			new TextureCreationConfig {
				IsLinearColorspace = true,
				Name = name
			}
		);
	}

	Texture LoadAbsorptionTransmissionMap(ReadOnlySpan<char> filePath, bool invertAbsorption = false) {
		var includesTransmission = ReadTextureMetadata(filePath).IncludesAlphaChannel;
		if (!includesTransmission) return LoadAbsorptionTransmissionMap(filePath, BuiltInTexturePaths.DefaultTransmissionMap, invertAbsorption);
		if (!invertAbsorption) return LoadTexture(filePath, isLinearColorspace: false, name: Path.GetFileName(filePath));

		return LoadTexture(
			filePath, 
			new TextureCreationConfig {
				IsLinearColorspace = false,
				Name = Path.GetFileName(filePath),
				ProcessingToApply = TextureProcessingConfig.Invert(includeAlphaChannel: false)
			}
		);
	}
	Texture LoadAbsorptionTransmissionMap(ReadOnlySpan<char> absorptionFilePath, ReadOnlySpan<char> transmissionFilePath, bool invertAbsorption = false) {
		var a = Path.GetFileName(absorptionFilePath);
		var b = Path.GetFileName(transmissionFilePath);
		Span<char> name = stackalloc char[a.Length + 1 + b.Length];
		a.CopyTo(name);
		name[a.Length] = '+';
		b.CopyTo(name[(a.Length + 1)..]);

		return LoadCombinedTexture(
			absorptionFilePath, invertAbsorption ? TextureProcessingConfig.Invert(includeAlphaChannel: false) : TextureProcessingConfig.None,
			transmissionFilePath, TextureProcessingConfig.None,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureA, B),
				OutputTextureWAlphaChannelSource = new(TextureB, R)
			},
			new TextureCreationConfig {
				IsLinearColorspace = false,
				Name = name
			}
		);
	}

	Texture LoadEmissiveMap(ReadOnlySpan<char> filePath) {
		if (ReadTextureMetadata(filePath).IncludesAlphaChannel) return LoadTexture(filePath, isLinearColorspace: false, name: Path.GetFileName(filePath));
		else return LoadEmissiveMap(filePath, BuiltInTexturePaths.DefaultEmissiveIntensityMap);
	}
	Texture LoadEmissiveMap(ReadOnlySpan<char> emissiveColorFilePath, ReadOnlySpan<char> emissiveIntensityFilePath) {
		var a = Path.GetFileName(emissiveColorFilePath);
		var b = Path.GetFileName(emissiveIntensityFilePath);
		Span<char> name = stackalloc char[a.Length + 1 + b.Length];
		a.CopyTo(name);
		name[a.Length] = '+';
		b.CopyTo(name[(a.Length + 1)..]);

		return LoadCombinedTexture(
			emissiveColorFilePath,
			emissiveIntensityFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureA, B),
				OutputTextureWAlphaChannelSource = new(TextureB, R),
			},
			new TextureCreationConfig {
				IsLinearColorspace = false,
				Name = name
			}
		);
	}

	Texture LoadAnisotropyMapVectorFormatted(ReadOnlySpan<char> filePath, ColorChannel? strengthChannel) {
		return strengthChannel switch {
			B => LoadTexture(filePath, isLinearColorspace: true, name: Path.GetFileName(filePath)),
			A => LoadTexture(filePath, new TextureCreationConfig { IsLinearColorspace = true, Name = Path.GetFileName(filePath), ProcessingToApply = TextureProcessingConfig.Swizzle(blueSource: A) }),
			_ => LoadAnisotropyMapVectorFormatted(filePath, BuiltInTexturePaths.DefaultAnisotropyStrengthMap)
		};
	}
	Texture LoadAnisotropyMapVectorFormatted(ReadOnlySpan<char> vectorFilePath, ReadOnlySpan<char> strengthFilePath) {
		var a = Path.GetFileName(vectorFilePath);
		var b = Path.GetFileName(strengthFilePath);
		Span<char> name = stackalloc char[a.Length + 1 + b.Length];
		a.CopyTo(name);
		name[a.Length] = '+';
		b.CopyTo(name[(a.Length + 1)..]);

		return LoadCombinedTexture(
			vectorFilePath,
			strengthFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureA, G),
				OutputTextureZBlueChannelSource = new(TextureB, R),
			},
			new TextureCreationConfig {
				IsLinearColorspace = true,
				Name = name
			}
		);
	}

	static void ConvertRadialAngleToVectorFormatAnisotropy(Span<TexelRgb24> texels, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, ColorChannel? strengthChannel) {
		const float StrengthCoefficient = 1f / Byte.MaxValue;
		const float AngleCoefficientZeroTo180 = 0.5f / Byte.MaxValue;
		const float AngleCoefficientZeroTo360 = 1f / Byte.MaxValue;
		var angleAddition = Angle.From2DPolarAngle(zeroDirection) ?? Angle.Zero;
		var angleCoefficient = encodedRange == AnisotropyRadialAngleRange.ZeroTo180 ? AngleCoefficientZeroTo180 : AngleCoefficientZeroTo360;
		if (!encodedAnticlockwise) angleCoefficient *= -1f;

		if (strengthChannel is G or B) {
			for (var i = 0; i < texels.Length; ++i) {
				texels[i] = ITextureBuilder.CreateAnisotropyTexel(Angle.FromFullCircleFraction(texels[i].R * angleCoefficient) + angleAddition, texels[i][strengthChannel.Value] * StrengthCoefficient);
			}
		}
		else {
			for (var i = 0; i < texels.Length; ++i) {
				texels[i] = ITextureBuilder.CreateAnisotropyTexel(Angle.FromFullCircleFraction(texels[i].R * angleCoefficient) + angleAddition, ITextureBuilder.DefaultAnisotropyStrength);
			}
		}
	}
	static void ConvertRadialAngleToVectorFormatAnisotropy(Span<TexelRgba32> texels, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, ColorChannel? strengthChannel) {
		const float StrengthCoefficient = 1f / Byte.MaxValue;
		const float AngleCoefficientZeroTo180 = 0.5f / Byte.MaxValue;
		const float AngleCoefficientZeroTo360 = 1f / Byte.MaxValue;
		var angleAddition = Angle.From2DPolarAngle(zeroDirection) ?? Angle.Zero;
		var angleCoefficient = encodedRange == AnisotropyRadialAngleRange.ZeroTo180 ? AngleCoefficientZeroTo180 : AngleCoefficientZeroTo360;
		if (!encodedAnticlockwise) angleCoefficient *= -1f;

		if (strengthChannel is G or B or A) {
			for (var i = 0; i < texels.Length; ++i) {
				texels[i] = ITextureBuilder.CreateAnisotropyTexel(Angle.FromFullCircleFraction(texels[i].R * angleCoefficient) + angleAddition, texels[i][strengthChannel.Value] * StrengthCoefficient).ToRgba32();
			}
		}
		else {
			for (var i = 0; i < texels.Length; ++i) {
				texels[i] = ITextureBuilder.CreateAnisotropyTexel(Angle.FromFullCircleFraction(texels[i].R * angleCoefficient) + angleAddition, ITextureBuilder.DefaultAnisotropyStrength).ToRgba32();
			}
		}
	}
	Texture LoadAnisotropyMapRadialAngleFormatted(ReadOnlySpan<char> filePath, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise, ColorChannel? strengthChannel) {
		lock (_staticMutationLock) {
			var fileMetadata = ReadTextureMetadata(filePath);
			using var texelPoolMemory = _mapTextureProcessingPool.Borrow<TexelRgb24>(fileMetadata.Dimensions.Area);
			ReadTexture(filePath, texelPoolMemory.Buffer);
			ConvertRadialAngleToVectorFormatAnisotropy(texelPoolMemory.Buffer, zeroDirection, encodedRange, encodedAnticlockwise, strengthChannel);
			return TextureBuilder.CreateTexture(
				texelPoolMemory.Buffer, 
				new TextureGenerationConfig { Dimensions = fileMetadata.Dimensions }, 
				new TextureCreationConfig { IsLinearColorspace = true, Name = Path.GetFileName(filePath) }
			);
		}
	}
	Texture LoadAnisotropyMapRadialAngleFormatted(ReadOnlySpan<char> radialAngleFilePath, ReadOnlySpan<char> strengthFilePath, Orientation2D zeroDirection, AnisotropyRadialAngleRange encodedRange, bool encodedAnticlockwise) {
		var a = Path.GetFileName(radialAngleFilePath);
		var b = Path.GetFileName(strengthFilePath);
		Span<char> name = stackalloc char[a.Length + 1 + b.Length];
		a.CopyTo(name);
		name[a.Length] = '+';
		b.CopyTo(name[(a.Length + 1)..]);

		lock (_staticMutationLock) {
			var combinedTexMetadata = ReadCombinedTextureMetadata(radialAngleFilePath, strengthFilePath);
			using var texelPoolMemory = _mapTextureProcessingPool.Borrow<TexelRgb24>(combinedTexMetadata.Dimensions.Area);
			ReadCombinedTexture(
				radialAngleFilePath, 
				strengthFilePath,
				new TextureCombinationConfig(TextureA, R, TextureA, G, TextureB, R),
				TextureProcessingConfig.None,
				texelPoolMemory.Buffer
			);
			ConvertRadialAngleToVectorFormatAnisotropy(texelPoolMemory.Buffer, zeroDirection, encodedRange, encodedAnticlockwise, B);
			return TextureBuilder.CreateTexture(
				texelPoolMemory.Buffer,
				new TextureGenerationConfig { Dimensions = combinedTexMetadata.Dimensions },
				new TextureCreationConfig { IsLinearColorspace = true, Name = Path.GetFileName(name) }
			);
		}
	}

	Texture LoadClearCoatMap(ReadOnlySpan<char> filePath) => LoadTexture(filePath, isLinearColorspace: true, name: Path.GetFileName(filePath));
	Texture LoadClearCoatMap(ReadOnlySpan<char> thicknessFilePath, ReadOnlySpan<char> roughnessFilePath) {
		var a = Path.GetFileName(thicknessFilePath);
		var b = Path.GetFileName(roughnessFilePath);
		Span<char> name = stackalloc char[a.Length + 1 + b.Length];
		a.CopyTo(name);
		name[a.Length] = '+';
		b.CopyTo(name[(a.Length + 1)..]);

		return LoadCombinedTexture(
			thicknessFilePath,
			roughnessFilePath,
			new TextureCombinationConfig {
				OutputTextureXRedChannelSource = new(TextureA, R),
				OutputTextureYGreenChannelSource = new(TextureB, R),
				OutputTextureZBlueChannelSource = new(TextureA, B)
			},
			new TextureCreationConfig {
				IsLinearColorspace = true,
				Name = name
			}
		);
	}
	#endregion

	#region Load / Read Combined Texture
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => LoadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputConfig);
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => LoadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputConfig);
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		ReadOnlySpan<char> dFilePath,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	) => LoadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, dFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputConfig);
	Texture LoadCombinedTexture(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureCreationConfig finalOutputConfig
	);

	TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath);
	TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath, ReadOnlySpan<char> cFilePath);
	TextureReadMetadata ReadCombinedTextureMetadata(ReadOnlySpan<char> aFilePath, ReadOnlySpan<char> bFilePath, ReadOnlySpan<char> cFilePath, ReadOnlySpan<char> dFilePath);

	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> => ReadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputProcessingConfig, destinationBuffer);
	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32>;

	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> => ReadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputProcessingConfig, destinationBuffer);
	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32>;

	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath,
		ReadOnlySpan<char> bFilePath,
		ReadOnlySpan<char> cFilePath,
		ReadOnlySpan<char> dFilePath,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32> => ReadCombinedTexture(aFilePath, TextureProcessingConfig.None, bFilePath, TextureProcessingConfig.None, cFilePath, TextureProcessingConfig.None, dFilePath, TextureProcessingConfig.None, combinationConfig, in finalOutputProcessingConfig, destinationBuffer);
	int ReadCombinedTexture<TTexel>(
		ReadOnlySpan<char> aFilePath, in TextureProcessingConfig aProcessingConfig,
		ReadOnlySpan<char> bFilePath, in TextureProcessingConfig bProcessingConfig,
		ReadOnlySpan<char> cFilePath, in TextureProcessingConfig cProcessingConfig,
		ReadOnlySpan<char> dFilePath, in TextureProcessingConfig dProcessingConfig,
		TextureCombinationConfig combinationConfig, in TextureProcessingConfig finalOutputProcessingConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgba32>;
	#endregion

	#region Load Backdrop Texture
	BackdropTexture LoadPreprocessedBackdropTexture(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, ReadOnlySpan<char> name = default) {
		return LoadPreprocessedBackdropTexture(
			skyboxKtxFilePath, iblKtxFilePath,
			new BackdropTextureCreationConfig { Name = name }
		);
	}
	BackdropTexture LoadPreprocessedBackdropTexture(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, in BackdropTextureCreationConfig config);
	#endregion

	#region Load / Read Mesh
	Mesh LoadMesh(ReadOnlySpan<char> filePath, ReadOnlySpan<char> name = default) {
		return LoadMesh(
			filePath,
			new MeshCreationConfig {
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	Mesh LoadMesh(ReadOnlySpan<char> filePath, in MeshCreationConfig config) => LoadMesh(filePath, config, new MeshReadConfig());
	Mesh LoadMesh(ReadOnlySpan<char> filePath, in MeshCreationConfig config, in MeshReadConfig readConfig);
	MeshReadMetadata ReadMeshMetadata(ReadOnlySpan<char> filePath) => ReadMeshMetadata(filePath, new MeshReadConfig());
	MeshReadMetadata ReadMeshMetadata(ReadOnlySpan<char> filePath, in MeshReadConfig readConfig);
	MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer) => ReadMesh(filePath, vertexBuffer, triangleBuffer, new MeshReadConfig());
	MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertex> vertexBuffer, Span<VertexTriangle> triangleBuffer, in MeshReadConfig readConfig);
	MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertexSkeletal> vertexBuffer, Span<VertexTriangle> triangleBuffer) => ReadMesh(filePath, vertexBuffer, triangleBuffer, new MeshReadConfig());
	MeshReadCountData ReadMesh(ReadOnlySpan<char> filePath, Span<MeshVertexSkeletal> vertexBuffer, Span<VertexTriangle> triangleBuffer, in MeshReadConfig readConfig);
	#endregion

	#region Load Generic / Combined
	Model CreateModel(Mesh mesh, Material material, ReadOnlySpan<char> name = default);
	
	ResourceGroup LoadAll(ReadOnlySpan<char> filePath, ReadOnlySpan<char> name = default) {
		return LoadAll(
			filePath,
			new ModelCreationConfig {
				Name = name.IsEmpty ? Path.GetFileName(filePath) : name
			}
		);
	}
	ResourceGroup LoadAll(ReadOnlySpan<char> filePath, in ModelCreationConfig config) => LoadAll(filePath, in config, new ModelReadConfig());
	ResourceGroup LoadAll(ReadOnlySpan<char> filePath, in ModelCreationConfig config, in ModelReadConfig readConfig);
	#endregion
}