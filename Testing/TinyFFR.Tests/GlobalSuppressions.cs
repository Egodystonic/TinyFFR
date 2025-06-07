using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
	"Assertion", 
	"NUnit2005:Consider using Assert.That(actual, Is.EqualTo(expected)) instead of Assert.AreEqual(expected, actual)", 
	Justification = "I considered it but I hate fluent APIs. Don't see any need.", 
	Scope = "module"
)]
