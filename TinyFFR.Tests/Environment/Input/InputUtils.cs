// Created on 2025-05-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using NSubstitute.Core;
using NUnit.Framework;
using System.Linq;
using static NSubstitute.Arg;
using System.Runtime.InteropServices;

namespace Egodystonic.TinyFFR.Environment.Input;

[TestFixture]
class InputUtilsTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyProvideAllKeys() {
		var allKeysArr = InputUtils.AllKeys.ToArray();

		for (var i = 0; i < allKeysArr.Length; ++i) {
			Assert.IsTrue(Enum.IsDefined(allKeysArr[i]));
			for (var j = 0; j < i; ++j) {
				Assert.AreNotEqual(allKeysArr[i], allKeysArr[j]);
			}
		}

		foreach (var key in Enum.GetValues<KeyboardOrMouseKey>()) {
			Assert.AreEqual(key != KeyboardOrMouseKey.Unknown, allKeysArr.Contains(key));
		}
	}

	[Test]
	public void ShouldCorrectlyProvideAllCategories() {
		var allCatsArr = InputUtils.AllCategories.ToArray();

		for (var i = 0; i < allCatsArr.Length; ++i) {
			Assert.IsTrue(Enum.IsDefined(allCatsArr[i]));
			for (var j = 0; j < i; ++j) {
				Assert.AreNotEqual(allCatsArr[i], allCatsArr[j]);
			}
		}

		foreach (var cat in Enum.GetValues<KeyboardOrMouseKeyCategory>()) {
			Assert.AreEqual(cat != KeyboardOrMouseKeyCategory.Other, allCatsArr.Contains(cat));
		}
	}

	[Test]
	public void ShouldCorrectlyConvertKeysToAndFromNumericValues() {
		void AssertPair(int? expectedValue, KeyboardOrMouseKey key) {
			Assert.AreEqual(expectedValue, InputUtils.KeyToNumericValue(key));
			Assert.AreEqual(key.GetNumericValue(), InputUtils.KeyToNumericValue(key));

			if (expectedValue is { } value) {
				Assert.AreEqual(key, InputUtils.KeyFromNumericValue(value, key.GetCategory() == KeyboardOrMouseKeyCategory.NumberRow));
			}
		}

		Assert.AreEqual(null, InputUtils.KeyFromNumericValue(-1, true));
		Assert.AreEqual(null, InputUtils.KeyFromNumericValue(-1, false));
		Assert.AreEqual(null, InputUtils.KeyFromNumericValue(10, true));
		Assert.AreEqual(null, InputUtils.KeyFromNumericValue(10, false));
		
		foreach (var key in InputUtils.AllKeys) {
			int? expectation = key switch {
				KeyboardOrMouseKey.NumberRow0 => 0,
				KeyboardOrMouseKey.NumberRow1 => 1,
				KeyboardOrMouseKey.NumberRow2 => 2,
				KeyboardOrMouseKey.NumberRow3 => 3,
				KeyboardOrMouseKey.NumberRow4 => 4,
				KeyboardOrMouseKey.NumberRow5 => 5,
				KeyboardOrMouseKey.NumberRow6 => 6,
				KeyboardOrMouseKey.NumberRow7 => 7,
				KeyboardOrMouseKey.NumberRow8 => 8,
				KeyboardOrMouseKey.NumberRow9 => 9,
				KeyboardOrMouseKey.Numpad1 => 1,
				KeyboardOrMouseKey.Numpad2 => 2,
				KeyboardOrMouseKey.Numpad3 => 3,
				KeyboardOrMouseKey.Numpad4 => 4,
				KeyboardOrMouseKey.Numpad5 => 5,
				KeyboardOrMouseKey.Numpad6 => 6,
				KeyboardOrMouseKey.Numpad7 => 7,
				KeyboardOrMouseKey.Numpad8 => 8,
				KeyboardOrMouseKey.Numpad9 => 9,
				KeyboardOrMouseKey.Numpad0 => 0,
				_ => null
			};
			AssertPair(expectation, key);
		}
	}

	[Test]
	public void ShouldCorrectlyConvertKeysToAndFromCharacterValues() {
		void AssertPair(char? expectedValue, KeyboardOrMouseKey key) {
			Assert.AreEqual(expectedValue, InputUtils.KeyToCharacterValue(key));
			Assert.AreEqual(key.GetCharacterValue(), InputUtils.KeyToCharacterValue(key));

			if (expectedValue is { } value) {
				Assert.AreEqual(key, InputUtils.KeyFromCharacterValue(value));
			}
		}

		Assert.AreEqual(null, InputUtils.KeyFromCharacterValue((char) 2000));
		Assert.AreEqual(null, InputUtils.KeyFromCharacterValue((char) 0));

		foreach (var key in InputUtils.AllKeys) {
			char? expectation = key switch {
				KeyboardOrMouseKey.Return => '\r',
				KeyboardOrMouseKey.Escape => '\x1B',
				KeyboardOrMouseKey.Backspace => '\b',
				KeyboardOrMouseKey.Tab => '\t',
				KeyboardOrMouseKey.Space => ' ',
				KeyboardOrMouseKey.ExclamationMark => '!',
				KeyboardOrMouseKey.DoubleQuote => '"',
				KeyboardOrMouseKey.Hash => '#',
				KeyboardOrMouseKey.Percent => '%',
				KeyboardOrMouseKey.Dollar => '$',
				KeyboardOrMouseKey.Ampersand => '&',
				KeyboardOrMouseKey.SingleQuote => '\'',
				KeyboardOrMouseKey.OpeningParenthesis => '(',
				KeyboardOrMouseKey.ClosingParenthesis => ')',
				KeyboardOrMouseKey.Asterisk => '*',
				KeyboardOrMouseKey.Plus => '+',
				KeyboardOrMouseKey.Comma => ',',
				KeyboardOrMouseKey.Minus => '-',
				KeyboardOrMouseKey.Period => '.',
				KeyboardOrMouseKey.ForwardSlash => '/',
				KeyboardOrMouseKey.NumberRow0 => '0',
				KeyboardOrMouseKey.NumberRow1 => '1',
				KeyboardOrMouseKey.NumberRow2 => '2',
				KeyboardOrMouseKey.NumberRow3 => '3',
				KeyboardOrMouseKey.NumberRow4 => '4',
				KeyboardOrMouseKey.NumberRow5 => '5',
				KeyboardOrMouseKey.NumberRow6 => '6',
				KeyboardOrMouseKey.NumberRow7 => '7',
				KeyboardOrMouseKey.NumberRow8 => '8',
				KeyboardOrMouseKey.NumberRow9 => '9',
				KeyboardOrMouseKey.Colon => ':',
				KeyboardOrMouseKey.Semicolon => ';',
				KeyboardOrMouseKey.LessThan => '<',
				KeyboardOrMouseKey.Equals => '=',
				KeyboardOrMouseKey.GreaterThan => '>',
				KeyboardOrMouseKey.QuestionMark => '?',
				KeyboardOrMouseKey.AtSymbol => '@',
				KeyboardOrMouseKey.LeftSquareBracket => '[',
				KeyboardOrMouseKey.BackSlash => '\\',
				KeyboardOrMouseKey.RightSquareBracket => ']',
				KeyboardOrMouseKey.Caret => '^',
				KeyboardOrMouseKey.Underscore => '_',
				KeyboardOrMouseKey.Backtick => '`',
				KeyboardOrMouseKey.A => 'a',
				KeyboardOrMouseKey.B => 'b',
				KeyboardOrMouseKey.C => 'c',
				KeyboardOrMouseKey.D => 'd',
				KeyboardOrMouseKey.E => 'e',
				KeyboardOrMouseKey.F => 'f',
				KeyboardOrMouseKey.G => 'g',
				KeyboardOrMouseKey.H => 'h',
				KeyboardOrMouseKey.I => 'i',
				KeyboardOrMouseKey.J => 'j',
				KeyboardOrMouseKey.K => 'k',
				KeyboardOrMouseKey.L => 'l',
				KeyboardOrMouseKey.M => 'm',
				KeyboardOrMouseKey.N => 'n',
				KeyboardOrMouseKey.O => 'o',
				KeyboardOrMouseKey.P => 'p',
				KeyboardOrMouseKey.Q => 'q',
				KeyboardOrMouseKey.R => 'r',
				KeyboardOrMouseKey.S => 's',
				KeyboardOrMouseKey.T => 't',
				KeyboardOrMouseKey.U => 'u',
				KeyboardOrMouseKey.V => 'v',
				KeyboardOrMouseKey.W => 'w',
				KeyboardOrMouseKey.X => 'x',
				KeyboardOrMouseKey.Y => 'y',
				KeyboardOrMouseKey.Z => 'z',
				KeyboardOrMouseKey.Delete => '\x7F',
				_ => null
			};
			AssertPair(expectation, key);
		}
	}
}