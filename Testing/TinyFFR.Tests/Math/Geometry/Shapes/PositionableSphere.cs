// Created on 2026-04-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Reflection;

namespace Egodystonic.TinyFFR;

[TestFixture]
class PositionableSphereTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyDelegateMembers() {
		var testType = typeof(PositionableSphere);
		var implType = typeof(TranslatedConvexShape<Sphere>);
		
		(PropertyInfo ImplProp, PropertyInfo TestProp)[] PrintAndFilterProperties(IEnumerable<PropertyInfo> props) {
			return props.Select(p => {
				var testProp = testType.GetProperty(p.Name);
				if (testProp == null) Console.WriteLine($"Skipping un-matched property '{p.Name}'.");
				else Console.WriteLine($"Including property '{p.Name}'.");
				return (ImplProp: p, TestProp: testProp);
			})
			.Where(tuple => tuple.TestProp != null)
			.ToArray()!;
		}
		
		(MethodInfo ImplMethod, MethodInfo TestMethod)[] PrintAndFilterMethods(IEnumerable<MethodInfo> methods) {
			return methods.Select(m => {
				
			})
			.Where(tuple => tuple.TestMethod != null)
			.ToArray()!;
		}
		
		var instanceFlags = BindingFlags.Public | BindingFlags.Instance;
		var staticFlags = BindingFlags.Public | BindingFlags.Static;
		var instanceProperties = PrintAndFilterProperties(implType.GetProperties(instanceFlags));
		var staticProperties = PrintAndFilterProperties(implType.GetProperties(staticFlags));
		
		for (var i = 0; i < 10_000; ++i) {
			var input = PositionableSphere.Random();
			var castInput = (TranslatedConvexShape<Sphere>) input;
			
			foreach (var tuple in instanceProperties) {
				Assert.AreEqual(tuple.ImplProp.GetValue(castInput), tuple.TestProp.GetValue(input), $"Discrepancy for property '{tuple.ImplProp.Name}', input {input}");
			}
		}
		
		foreach (var tuple in staticProperties) {
			Assert.AreEqual(tuple.ImplProp.GetValue(null), tuple.TestProp.GetValue(null), $"Discrepancy for property '{tuple.ImplProp.Name}'");
		}
	}
}