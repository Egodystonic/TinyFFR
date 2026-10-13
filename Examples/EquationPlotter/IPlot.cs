namespace EquationPlotter;

interface IPlot : IDisposable {
	string Description { get; }
	float ColorRangeMin { get; }
	float ColorRangeMax { get; }
	void Update(float time, CompiledExpression[] expressions);
}
