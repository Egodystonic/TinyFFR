using System.Globalization;

namespace EquationPlotter;

enum TokenKind {
	Number,
	Identifier,
	Plus,
	Minus,
	Star,
	Slash,
	Caret,
	OpenParen,
	CloseParen,
	Comma,
	End
}

readonly record struct Token(TokenKind Kind, int Position, string Text, float Value);

sealed class ExpressionException : Exception {
	public int Position { get; }

	public ExpressionException(string message, int position) : base(message) => Position = position;
}

static class ExpressionTokenizer {
	public static List<Token> Tokenize(string source) {
		var result = new List<Token>();
		var i = 0;
		while (i < source.Length) {
			var c = source[i];
			if (Char.IsWhiteSpace(c)) {
				++i;
				continue;
			}

			if (Char.IsDigit(c) || (c == '.' && i + 1 < source.Length && Char.IsDigit(source[i + 1]))) {
				var start = i;
				while (i < source.Length && (Char.IsDigit(source[i]) || source[i] == '.')) ++i;
				if (i < source.Length && source[i] is 'e' or 'E' && i + 1 < source.Length && (Char.IsDigit(source[i + 1]) || source[i + 1] is '+' or '-')) {
					i += 2;
					while (i < source.Length && Char.IsDigit(source[i])) ++i;
				}
				var text = source[start..i];
				if (!Single.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) throw new ExpressionException($"'{text}' is not a valid number", start);
				result.Add(new Token(TokenKind.Number, start, text, value));
				continue;
			}

			if (Char.IsLetter(c) || c == '_') {
				var start = i;
				while (i < source.Length && (Char.IsLetterOrDigit(source[i]) || source[i] == '_')) ++i;
				result.Add(new Token(TokenKind.Identifier, start, source[start..i].ToLowerInvariant(), 0f));
				continue;
			}

			var kind = c switch {
				'+' => TokenKind.Plus,
				'-' => TokenKind.Minus,
				'*' => TokenKind.Star,
				'/' => TokenKind.Slash,
				'^' => TokenKind.Caret,
				'(' => TokenKind.OpenParen,
				')' => TokenKind.CloseParen,
				',' => TokenKind.Comma,
				_ => throw new ExpressionException($"Unexpected character '{c}'", i)
			};
			result.Add(new Token(kind, i, c.ToString(), 0f));
			++i;
		}
		result.Add(new Token(TokenKind.End, source.Length, "", 0f));
		return result;
	}
}
