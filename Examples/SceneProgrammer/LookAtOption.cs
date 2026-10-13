namespace SceneProgrammer;

sealed class LookAtOption {
	public static readonly LookAtOption None = new("(None)", null);
	public static readonly LookAtOption Point = new("(Point)", null);

	public string Name { get; }
	public SceneObject? Target { get; }

	public LookAtOption(string name, SceneObject? target) {
		Name = name;
		Target = target;
	}

	public override string ToString() => Name;
}
