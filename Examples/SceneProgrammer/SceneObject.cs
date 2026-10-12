using CommunityToolkit.Mvvm.ComponentModel;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.World;

namespace SceneProgrammer;

sealed partial class SceneObject : ObservableObject {
	public string Name { get; }
	public ModelBundle Bundle { get; }
	public ModelInstanceGroup Instances { get; }
	public LookAtOption AsLookAtTarget { get; }

	[ObservableProperty] public partial decimal PositionX { get; set; }
	[ObservableProperty] public partial decimal PositionY { get; set; }
	[ObservableProperty] public partial decimal PositionZ { get; set; }
	[ObservableProperty] public partial decimal YawDegrees { get; set; }
	[ObservableProperty] public partial decimal PitchDegrees { get; set; }
	[ObservableProperty] public partial decimal RollDegrees { get; set; }
	[ObservableProperty] public partial decimal Scale { get; set; } = 1m;

	public Location Position {
		get => new((float) PositionX, (float) PositionY, (float) PositionZ);
		set {
			PositionX = Math.Round((decimal) value.X, 3);
			PositionY = Math.Round((decimal) value.Y, 3);
			PositionZ = Math.Round((decimal) value.Z, 3);
		}
	}

	public SceneObject(string name, ModelBundle bundle, ModelInstanceGroup instances) {
		Name = name;
		Bundle = bundle;
		Instances = instances;
		AsLookAtTarget = new LookAtOption(name, this);
	}

	partial void OnPositionXChanged(decimal value) => ApplyTransform();
	partial void OnPositionYChanged(decimal value) => ApplyTransform();
	partial void OnPositionZChanged(decimal value) => ApplyTransform();
	partial void OnYawDegreesChanged(decimal value) => ApplyTransform();
	partial void OnPitchDegreesChanged(decimal value) => ApplyTransform();
	partial void OnRollDegreesChanged(decimal value) => ApplyTransform();
	partial void OnScaleChanged(decimal value) => ApplyTransform();

	public void ApplyTransform() {
		var rotation = (Angle.FromDegrees((float) YawDegrees) % Direction.Up)
			+ (Angle.FromDegrees((float) PitchDegrees) % Direction.Left)
			+ (Angle.FromDegrees((float) RollDegrees) % Direction.Forward);
		Instances.SetPosition(Position);
		Instances.SetRotation(rotation);
		Instances.SetScaling(new Vect(MathF.Max((float) Scale, 0.001f)));
	}

	public PositionedCuboid CalculateWorldBoundingBox() {
		var boxes = new PositionedRotatedCuboid[Instances.Count];
		for (var i = 0; i < boxes.Length; ++i) boxes[i] = Instances[i].GetWorldSpaceBoundingBox();
		return PositionedCuboid.FromSmallestEnclosingCuboid(boxes);
	}

	public override string ToString() => Name;
}
