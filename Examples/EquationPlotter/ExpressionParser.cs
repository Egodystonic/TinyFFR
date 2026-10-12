namespace EquationPlotter;

sealed class ExpressionParser {
	static readonly Dictionary<string, (MathFunction Function, int Arity)> Functions = new() {
		["sin"] = (MathFunction.Sin, 1), ["cos"] = (MathFunction.Cos, 1), ["tan"] = (MathFunction.Tan, 1),
		["asin"] = (MathFunction.Asin, 1), ["acos"] = (MathFunction.Acos, 1), ["atan"] = (MathFunction.Atan, 1),
		["atan2"] = (MathFunction.Atan2, 2), ["sinh"] = (MathFunction.Sinh, 1), ["cosh"] = (MathFunction.Cosh, 1),
		["tanh"] = (MathFunction.Tanh, 1), ["sqrt"] = (MathFunction.Sqrt, 1), ["abs"] = (MathFunction.Abs, 1),
		["exp"] = (MathFunction.Exp, 1), ["log"] = (MathFunction.Log, 1), ["ln"] = (MathFunction.Log, 1),
		["floor"] = (MathFunction.Floor, 1), ["ceil"] = (MathFunction.Ceiling, 1), ["round"] = (MathFunction.Round, 1),
		["sign"] = (MathFunction.Sign, 1), ["min"] = (MathFunction.Min, 2), ["max"] = (MathFunction.Max, 2),
		["clamp"] = (MathFunction.Clamp, 3), ["pow"] = (MathFunction.Pow, 2), ["mod"] = (MathFunction.Mod, 2)
	};

	static readonly Dictionary<string, float> Constants = new() {
		["pi"] = MathF.PI,
		["tau"] = MathF.Tau,
		["e"] = MathF.E
	};

	readonly List<Token> _tokens;
	readonly IReadOnlyList<string> _variableNames;
	readonly List<Instruction> _instructions = new();
	int _index;
	int _stackDepth;
	int _maxStackDepth;

	ExpressionParser(List<Token> tokens, IReadOnlyList<string> variableNames) {
		_tokens = tokens;
		_variableNames = variableNames;
	}

	Token Current => _tokens[_index];

	public static CompiledExpression Compile(string source, IReadOnlyList<string> variableNames) {
		if (String.IsNullOrWhiteSpace(source)) throw new ExpressionException("The expression is empty", 0);
		var parser = new ExpressionParser(ExpressionTokenizer.Tokenize(source), variableNames);
		parser.ParseSum();
		if (parser.Current.Kind != TokenKind.End) throw new ExpressionException($"Unexpected '{parser.Current.Text}'", parser.Current.Position);
		return new CompiledExpression(source, parser._instructions.ToArray());
	}

	void ParseSum() {
		ParseProduct();
		while (Current.Kind is TokenKind.Plus or TokenKind.Minus) {
			var opCode = Current.Kind == TokenKind.Plus ? OpCode.Add : OpCode.Subtract;
			++_index;
			ParseProduct();
			Emit(opCode, popCount: 2);
		}
	}

	void ParseProduct() {
		ParseUnary();
		while (true) {
			if (Current.Kind is TokenKind.Star or TokenKind.Slash) {
				var opCode = Current.Kind == TokenKind.Star ? OpCode.Multiply : OpCode.Divide;
				++_index;
				ParseUnary();
				Emit(opCode, popCount: 2);
			}
			else if (Current.Kind is TokenKind.Number or TokenKind.Identifier or TokenKind.OpenParen) {
				ParsePower();
				Emit(OpCode.Multiply, popCount: 2);
			}
			else {
				return;
			}
		}
	}

	void ParseUnary() {
		if (Current.Kind == TokenKind.Minus) {
			++_index;
			ParseUnary();
			Emit(OpCode.Negate, popCount: 1);
			return;
		}
		if (Current.Kind == TokenKind.Plus) {
			++_index;
			ParseUnary();
			return;
		}
		ParsePower();
	}

	void ParsePower() {
		ParsePrimary();
		if (Current.Kind != TokenKind.Caret) return;
		++_index;
		ParseUnary();
		Emit(OpCode.Power, popCount: 2);
	}

	void ParsePrimary() {
		var token = Current;
		switch (token.Kind) {
			case TokenKind.Number:
				++_index;
				Emit(OpCode.PushConstant, popCount: 0, constant: token.Value);
				return;
			case TokenKind.OpenParen:
				++_index;
				ParseSum();
				Expect(TokenKind.CloseParen, "')'");
				return;
			case TokenKind.Identifier:
				++_index;
				ParseIdentifier(token);
				return;
			case TokenKind.End:
				throw new ExpressionException("The expression ends unexpectedly", token.Position);
			default:
				throw new ExpressionException($"Unexpected '{token.Text}'", token.Position);
		}
	}

	void ParseIdentifier(Token token) {
		if (Functions.TryGetValue(token.Text, out var function)) {
			Expect(TokenKind.OpenParen, $"'(' after '{token.Text}'");
			var argumentCount = 0;
			if (Current.Kind != TokenKind.CloseParen) {
				while (true) {
					ParseSum();
					++argumentCount;
					if (Current.Kind != TokenKind.Comma) break;
					++_index;
				}
			}
			Expect(TokenKind.CloseParen, "')'");
			if (argumentCount != function.Arity) {
				throw new ExpressionException($"'{token.Text}' takes {function.Arity} argument{(function.Arity == 1 ? "" : "s")} (was given {argumentCount})", token.Position);
			}
			Emit(OpCode.Call, popCount: function.Arity, operand: (int) function.Function);
			return;
		}

		for (var i = 0; i < _variableNames.Count; ++i) {
			if (_variableNames[i] != token.Text) continue;
			Emit(OpCode.PushVariable, popCount: 0, operand: i);
			return;
		}

		if (Constants.TryGetValue(token.Text, out var constant)) {
			Emit(OpCode.PushConstant, popCount: 0, constant: constant);
			return;
		}

		throw new ExpressionException($"Unknown name '{token.Text}' (the variables here are {String.Join(", ", _variableNames)})", token.Position);
	}

	void Expect(TokenKind kind, string description) {
		if (Current.Kind != kind) {
			throw new ExpressionException(Current.Kind == TokenKind.End ? $"Expected {description} but the expression ended" : $"Expected {description} but found '{Current.Text}'", Current.Position);
		}
		++_index;
	}

	void Emit(OpCode opCode, int popCount, float constant = 0f, int operand = 0) {
		_instructions.Add(new Instruction(opCode, constant, operand));
		_stackDepth += 1 - popCount;
		_maxStackDepth = Math.Max(_maxStackDepth, _stackDepth);
		if (_maxStackDepth > CompiledExpression.MaxStackDepth) throw new ExpressionException("The expression is too deeply nested", Current.Position);
	}
}
