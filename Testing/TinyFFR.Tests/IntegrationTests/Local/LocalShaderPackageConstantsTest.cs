// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Egodystonic.TinyFFR.Assets.Materials.Local.LocalShaderPackageConstants;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalShaderPackageConstantsTest {
	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		var shaderSet = new HashSet<string>();

		var simpleOptions = GetAllShaderOptions<
			SimpleMaterialShaderConstants.Flags, 
			SimpleMaterialShaderConstants.AlphaModeVariant
		>();

		var standardOptions = GetAllShaderOptions<
			StandardMaterialShaderConstants.Flags,
			StandardMaterialShaderConstants.AlphaModeVariant,
			StandardMaterialShaderConstants.OrmReflectanceVariant
		>();

		var transmissiveOptions = GetAllShaderOptions<
			TransmissiveMaterialShaderConstants.Flags,
			TransmissiveMaterialShaderConstants.AlphaModeVariant,
			TransmissiveMaterialShaderConstants.RefractionQualityVariant,
			TransmissiveMaterialShaderConstants.RefractionTypeVariant
		>();

		foreach (var option in simpleOptions) {
			var shader = SimpleMaterialShader.GetShaderResourceName(option.Flag, option.Variant1);
			Assert.IsFalse(shaderSet.Contains(shader));
			shaderSet.Add(shader);
		}

		foreach (var option in standardOptions) {
			var shader = StandardMaterialShader.GetShaderResourceName(option.Flag, option.Variant1, option.Variant2);
			Assert.IsFalse(shaderSet.Contains(shader));
			shaderSet.Add(shader);
		}

		foreach (var option in transmissiveOptions) {
			var shader = TransmissiveMaterialShader.GetShaderResourceName(option.Flag, option.Variant1, option.Variant2, option.Variant3);
			Assert.IsFalse(shaderSet.Contains(shader));
			shaderSet.Add(shader);
		}

		Console.WriteLine($"Completed with {shaderSet.Count} distinct shaders.");
	}

	T[] GetAllFlagCombinations<T>() where T : struct, Enum {
		Assert.AreEqual(typeof(int), Enum.GetUnderlyingType(typeof(T)), "Test needs updating to reflect new flag enum types");
		var allValues = (int[]) Enum.GetValuesAsUnderlyingType<T>();
		
		var msbValue = allValues.Max();

		return Enumerable.Range(0, msbValue << 1).Select(i => Unsafe.As<int, T>(ref i)).ToArray();
	}

	T[] GetAllVariants<T>() where T : struct, Enum => Enum.GetValues<T>();

	IEnumerable<(TFlag Flag, T1 Variant1)> GetAllShaderOptions<TFlag, T1>() where TFlag : struct, Enum where T1 : struct, Enum {
		var flags = GetAllFlagCombinations<TFlag>();
		var v1s = GetAllVariants<T1>();

		foreach (var f in flags) {
			foreach (var v1 in v1s) {
				yield return (f, v1);
			}
		}
	}

	IEnumerable<(TFlag Flag, T1 Variant1, T2 Variant2)> GetAllShaderOptions<TFlag, T1, T2>() where TFlag : struct, Enum where T1 : struct, Enum where T2 : struct, Enum {
		var flags = GetAllFlagCombinations<TFlag>();
		var v1s = GetAllVariants<T1>();
		var v2s = GetAllVariants<T2>();

		foreach (var f in flags) {
			foreach (var v1 in v1s) {
				foreach (var v2 in v2s) {
					yield return (f, v1, v2);
				}
			}
		}
	}

	IEnumerable<(TFlag Flag, T1 Variant1, T2 Variant2, T3 Variant3)> GetAllShaderOptions<TFlag, T1, T2, T3>() where TFlag : struct, Enum where T1 : struct, Enum where T2 : struct, Enum where T3 : struct, Enum {
		var flags = GetAllFlagCombinations<TFlag>();
		var v1s = GetAllVariants<T1>();
		var v2s = GetAllVariants<T2>();
		var v3s = GetAllVariants<T3>();

		foreach (var f in flags) {
			foreach (var v1 in v1s) {
				foreach (var v2 in v2s) {
					foreach (var v3 in v3s) {
						yield return (f, v1, v2, v3);
					}
				}
			}
		}
	}
} 