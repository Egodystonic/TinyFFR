// Created on 2024-05-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR;

static class ConfigStructTestUtils {
	public readonly record struct ObjectAssertionBuilder<T>() where T : struct, IConfigStruct<T>, allows ref struct {
		readonly List<byte> _data = new();
		readonly List<(IResource Resource, int DataIndex)> _resources = new();

		public ObjectAssertionBuilder<T> Float(float f) {
			Span<byte> buf = stackalloc byte[sizeof(float)];
			BinaryPrimitives.WriteSingleLittleEndian(buf, f);
			_data.AddRange(buf);
			return this;
		}
		public ObjectAssertionBuilder<T> Int(int i) {
			Span<byte> buf = stackalloc byte[sizeof(int)];
			BinaryPrimitives.WriteInt32LittleEndian(buf, i);
			_data.AddRange(buf);
			return this;
		}
		public ObjectAssertionBuilder<T> Long(long l) {
			Span<byte> buf = stackalloc byte[sizeof(long)];
			BinaryPrimitives.WriteInt64LittleEndian(buf, l);
			_data.AddRange(buf);
			return this;
		}
		public ObjectAssertionBuilder<T> Bool(bool b) {
			_data.Add(b ? Byte.MaxValue : Byte.MinValue);
			return this;
		}
		public ObjectAssertionBuilder<T> Obj<TValue>(TValue v) where TValue : IFixedLengthByteSpanSerializable<TValue> {
			var buf = new byte[TValue.SerializationByteSpanLength];
			TValue.SerializeToBytes(buf, v);
			_data.AddRange(buf);
			return this;
		}
		public ObjectAssertionBuilder<T> String(ReadOnlySpan<char> s) {
			_ = Int(s.Length * sizeof(char));
			_data.AddRange(MemoryMarshal.AsBytes(s));
			return this;
		}
		public ObjectAssertionBuilder<T> SubConfig<TValue>(scoped in TValue v) where TValue : struct, IConfigStruct<TValue>, allows ref struct {
			_ = Int(TValue.GetHeapStorageFormattedLength(v));
			var buf = new byte[TValue.GetHeapStorageFormattedLength(v)];
			TValue.AllocateAndConvertToHeapStorage(buf, v);
			_data.AddRange(buf);
			return this;
		}
		public ObjectAssertionBuilder<T> Resource<TValue>(TValue v) where TValue : IResource<TValue> {
			var buf = new byte[IResource.SerializedLengthBytes];

			v.AllocateGcHandleAndSerializeResource(buf);
			_resources.Add((v, _data.Count));

			_data.AddRange(buf);
			return this;
		}
		public ObjectAssertionBuilder<T> ZeroResource() {
			var buf = new byte[IResource.SerializedLengthBytes];

			_data.AddRange(buf);
			return this;
		}

		public ObjectAssertionBuilder<T> For(scoped T input) {
			try {
				if (_resources.Count > 0) {
					var expectation = _data.ToArray();
					Assert.AreEqual(expectation.Length, T.GetHeapStorageFormattedLength(input));
					var actual = new byte[T.GetHeapStorageFormattedLength(input)];
					T.AllocateAndConvertToHeapStorage(actual, in input);

					foreach (var res in _resources) {
						Assert.AreEqual(
							IResource.ReadHandleFromSerializedResource(expectation[res.DataIndex..]), 
							IResource.ReadHandleFromSerializedResource(actual[res.DataIndex..])
						);
						Assert.IsTrue(ReferenceEquals(
							IResource.ReadGcHandleFromSerializedResource(expectation[res.DataIndex..]).Target, 
							IResource.ReadGcHandleFromSerializedResource(actual[res.DataIndex..]).Target
						));
						actual.AsSpan()[res.DataIndex..(res.DataIndex + IResource.SerializedLengthBytes)].CopyTo(expectation.AsSpan()[res.DataIndex..]);
					}

					Assert.IsTrue(expectation.SequenceEqual(actual));
				}
				else {
					AssertHeapSerializationWithBytes(input, _data.ToArray());
				}

				return this;
			}
			catch {
				try {
					var deserialized = T.ConvertFromAllocatedHeapStorage(_data.ToArray());
					Console.WriteLine("Run test with debugger attached to auto-break here and inspect values.");
					if (Debugger.IsAttached) Debugger.Break(); // Got here? Inspect "input" and "deserialized" in debugger
				}
				catch {
					Console.WriteLine("Could not deserialize coherent value from built-up array.");
					if (Debugger.IsAttached) Debugger.Break(); // Got here? Built up _data array was not valid for recreating a T.
					throw;
				}

				throw;
			}
		}
	}

	public readonly record struct ObjectPropertiesAsserter<T>() where T : struct, IConfigStruct<T>, allows ref struct {
		readonly List<string> _properties = new();

		public ObjectPropertiesAsserter<T> Including(string propName) {
			_properties.Add(propName);
			return this;
		}

		public void End() {
			foreach (var propInfo in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
				Assert.True(_properties.Contains(propInfo.Name, StringComparer.Ordinal), $"Property '{propInfo.Name}' is not accounted for.");
			}
		}
	}

	public static void AssertRoundTripHeapStorage<T>(scoped T input, Action<T, T> comparisonAssertionFunc) where T : struct, IConfigStruct<T>, allows ref struct {
		var dest = new byte[T.GetHeapStorageFormattedLength(input)];
		T.AllocateAndConvertToHeapStorage(dest, in input);
		var roundTripConverted = T.ConvertFromAllocatedHeapStorage(dest);
		comparisonAssertionFunc(input, roundTripConverted);
	}

	public static void AssertHeapSerializationWithBytes<T>(scoped T sampleItem, params byte[] expectation) where T : struct, IConfigStruct<T>, allows ref struct {
		Assert.AreEqual(expectation.Length, T.GetHeapStorageFormattedLength(sampleItem));
		var span = new byte[T.GetHeapStorageFormattedLength(sampleItem)];
		T.AllocateAndConvertToHeapStorage(span, in sampleItem);
		Assert.IsTrue(span.SequenceEqual(expectation));
	}

	public static ObjectAssertionBuilder<T> AssertHeapSerializationWithObjects<T>() where T : struct, IConfigStruct<T>, allows ref struct {
		return new ObjectAssertionBuilder<T>();
	}

	public static ObjectPropertiesAsserter<T> AssertPropertiesAccountedFor<T>() where T : struct, IConfigStruct<T>, allows ref struct {
		return new ObjectPropertiesAsserter<T>();
	}

	static void Test() {
		Console.WriteLine();
	}
}