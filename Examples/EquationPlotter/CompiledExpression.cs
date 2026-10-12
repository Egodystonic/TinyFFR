namespace EquationPlotter;

enum OpCode : byte {
	PushConstant,
	PushVariable,
	Add,
	Subtract,
	Multiply,
	Divide,
	Power,
	Negate,
	Call
}

enum MathFunction : byte {
	Sin, Cos, Tan, Asin, Acos, Atan, Atan2, Sinh, Cosh, Tanh,
	Sqrt, Abs, Exp, Log, Floor, Ceiling, Round, Sign,
	Min, Max, Clamp, Pow, Mod
}

readonly record struct Instruction(OpCode OpCode, float Constant, int Operand);

sealed class CompiledExpression {
	public const int MaxStackDepth = 64;

	readonly Instruction[] _instructions;

	public string Source { get; }

	public CompiledExpression(string source, Instruction[] instructions) {
		Source = source;
		_instructions = instructions;
	}

	public float Evaluate(ReadOnlySpan<float> variables) {
		Span<float> stack = stackalloc float[MaxStackDepth];
		var top = -1;
		foreach (var instruction in _instructions) {
			switch (instruction.OpCode) {
				case OpCode.PushConstant:
					stack[++top] = instruction.Constant;
					break;
				case OpCode.PushVariable:
					stack[++top] = variables[instruction.Operand];
					break;
				case OpCode.Add:
					stack[top - 1] += stack[top--];
					break;
				case OpCode.Subtract:
					stack[top - 1] -= stack[top--];
					break;
				case OpCode.Multiply:
					stack[top - 1] *= stack[top--];
					break;
				case OpCode.Divide:
					stack[top - 1] /= stack[top--];
					break;
				case OpCode.Power:
					stack[top - 1] = MathF.Pow(stack[top - 1], stack[top--]);
					break;
				case OpCode.Negate:
					stack[top] = -stack[top];
					break;
				case OpCode.Call:
					top = Call((MathFunction) instruction.Operand, stack, top);
					break;
			}
		}
		return stack[top];
	}

	static int Call(MathFunction function, Span<float> stack, int top) {
		var a = stack[top];
		switch (function) {
			case MathFunction.Sin: stack[top] = MathF.Sin(a); return top;
			case MathFunction.Cos: stack[top] = MathF.Cos(a); return top;
			case MathFunction.Tan: stack[top] = MathF.Tan(a); return top;
			case MathFunction.Asin: stack[top] = MathF.Asin(a); return top;
			case MathFunction.Acos: stack[top] = MathF.Acos(a); return top;
			case MathFunction.Atan: stack[top] = MathF.Atan(a); return top;
			case MathFunction.Sinh: stack[top] = MathF.Sinh(a); return top;
			case MathFunction.Cosh: stack[top] = MathF.Cosh(a); return top;
			case MathFunction.Tanh: stack[top] = MathF.Tanh(a); return top;
			case MathFunction.Sqrt: stack[top] = MathF.Sqrt(a); return top;
			case MathFunction.Abs: stack[top] = MathF.Abs(a); return top;
			case MathFunction.Exp: stack[top] = MathF.Exp(a); return top;
			case MathFunction.Log: stack[top] = MathF.Log(a); return top;
			case MathFunction.Floor: stack[top] = MathF.Floor(a); return top;
			case MathFunction.Ceiling: stack[top] = MathF.Ceiling(a); return top;
			case MathFunction.Round: stack[top] = MathF.Round(a); return top;
			case MathFunction.Sign: stack[top] = MathF.Sign(a); return top;
		}

		var b = stack[top - 1];
		switch (function) {
			case MathFunction.Atan2: stack[top - 1] = MathF.Atan2(b, a); return top - 1;
			case MathFunction.Min: stack[top - 1] = MathF.Min(b, a); return top - 1;
			case MathFunction.Max: stack[top - 1] = MathF.Max(b, a); return top - 1;
			case MathFunction.Pow: stack[top - 1] = MathF.Pow(b, a); return top - 1;
			case MathFunction.Mod: stack[top - 1] = b - a * MathF.Floor(b / a); return top - 1;
		}

		stack[top - 2] = Math.Clamp(stack[top - 2], b, a);
		return top - 2;
	}
}
