// Created on 2026-09-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using static Egodystonic.TinyFFR.World.SceneObjectTypeExtensions;
using static Egodystonic.TinyFFR.Resources.ResourceUtils;

namespace Egodystonic.TinyFFR.World;

public enum SceneObjectType {
	Unspecified = 0,
	ModelInstance = (TransformedFlag | MaterialReceivingFlag | StoresOnlyModelInstanceFlag) + 1,
	ModelInstanceGroup = (TransformedFlag | MaterialReceivingFlag) + 2,
	MutableGridInstance = (TransformedFlag | MaterialReceivingFlag | StoresOnlyModelInstanceFlag) + 3,
	QuadInstance = (TransformedFlag | MaterialReceivingFlag) + 4,
	CameraLockedQuadInstance = (PositionedFlag | ScaledFlag | MaterialReceivingFlag | StoresOnlyModelInstanceFlag) + 5,
	TextInstance = (TransformedFlag) + 6,
	CameraLockedTextInstance = (PositionedFlag | ScaledFlag | StoresOnlyModelInstanceFlag) + 7,
	PointLight = (PositionedFlag | ColoredFlag) + 8,
	SpotLight = (PositionedFlag | OrientedFlag | ColoredFlag) + 9,
	DirectionalLight = (OrientedFlag | ColoredFlag) + 10,
	Camera = (PositionedFlag | OrientedFlag) + 11
}

public static class SceneObjectTypeExtensions {
	internal const int TypeIdReservedBitShift = 6;
	internal const int TypeIdReservedBitCount = 1 << TypeIdReservedBitShift; // This is the max number of scene object types supported in the enum above, increase TypeIdReservedBitShift if necessary
	internal const int PositionedFlag = 0b1 << (TypeIdReservedBitShift + 0);
	internal const int OrientedFlag = 0b1 << (TypeIdReservedBitShift + 1);
	internal const int ScaledFlag = 0b1 << (TypeIdReservedBitShift + 2);
	internal const int TransformedFlag = PositionedFlag | OrientedFlag | ScaledFlag;
	internal const int MaterialReceivingFlag = 0b1 << (TypeIdReservedBitShift + 3);
	internal const int ColoredFlag = 0b1 << (TypeIdReservedBitShift + 4);
	internal const int StoresOnlyModelInstanceFlag = 0b1 << (TypeIdReservedBitShift + 5);

	extension(SceneObjectType @this) {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool FlagExists(int flag) => (((int) @this) & flag) == flag;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsPositioned() => @this.FlagExists(PositionedFlag);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsOriented() => @this.FlagExists(OrientedFlag);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsScaled() => @this.FlagExists(ScaledFlag);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsTransformed() => @this.FlagExists(TransformedFlag);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsMaterialReceiving() => @this.FlagExists(MaterialReceivingFlag);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsColored() => @this.FlagExists(ColoredFlag);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsStoredAsModelInstance() => @this.FlagExists(StoresOnlyModelInstanceFlag);
	}
}

public interface ISceneObject {
	static abstract SceneObjectType SceneObjectType { get; }
}

internal unsafe sealed class SceneObjectAdapterFunctionTable {
	interface IStubConverter<out TTargetType> {
		static abstract TTargetType FromStub(ResourceStub stub);
	}
	
	readonly struct ResourceStubConverter<TResource> : IStubConverter<TResource> where TResource : IResource<TResource> {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TResource FromStub(ResourceStub stub) => FastFromStub<TResource>(stub);
	}
	readonly struct ModelInstanceGroupStubConverter : IStubConverter<ModelInstanceGroup> {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ModelInstanceGroup FromStub(ResourceStub stub) => new(FastFromStub<ResourceGroup>(stub));
	}
	readonly struct QuadInstanceStubConverter : IStubConverter<QuadInstance> {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static QuadInstance FromStub(ResourceStub stub) => new(FastFromStub<ModelInstance>(stub));
	}
	readonly struct TextInstanceStubConverter : IStubConverter<TextInstance> {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TextInstance FromStub(ResourceStub stub) => new(FastFromStub<ModelInstance>(stub));
	}

	public SceneObjectType SceneObjectType { get; }

	public delegate* managed<ResourceStub, Location> GetPosition { get; private set; }
	public delegate* managed<ResourceStub, Location, void> SetPosition { get; private set; }
	public delegate* managed<ResourceStub, Vect, void> MoveBy { get; private set; }

	public delegate* managed<ResourceStub, Rotation> GetRotation { get; private set; }
	public delegate* managed<ResourceStub, Rotation, void> SetRotation { get; private set; }
	public delegate* managed<ResourceStub, Quaternion> GetRotationQuaternion { get; private set; }
	public delegate* managed<ResourceStub, Quaternion, void> SetRotationQuaternion { get; private set; }
	public delegate* managed<ResourceStub, Rotation, void> RotateByRotation { get; private set; }
	public delegate* managed<ResourceStub, Quaternion, void> RotateByQuaternion { get; private set; }

	public delegate* managed<ResourceStub, Vect> GetScaling { get; private set; }
	public delegate* managed<ResourceStub, Vect, void> SetScaling { get; private set; }
	public delegate* managed<ResourceStub, float, void> ScaleByScalar { get; private set; }
	public delegate* managed<ResourceStub, Vect, void> ScaleByVect { get; private set; }
	public delegate* managed<ResourceStub, float, void> AdjustScaleByScalar { get; private set; }
	public delegate* managed<ResourceStub, Vect, void> AdjustScaleByVect { get; private set; }

	public delegate* managed<ResourceStub, Transform> GetTransform { get; private set; }
	public delegate* managed<ResourceStub, Transform, void> SetTransform { get; private set; }
	public delegate* managed<ResourceStub, Rotation, Location, void> RotateByRotationAroundPivot { get; private set; }
	public delegate* managed<ResourceStub, Quaternion, Location, void> RotateByQuaternionAroundPivot { get; private set; }

	public delegate* managed<ResourceStub, Material, void> SetMaterial { get; private set; }
	public delegate* managed<ResourceStub, ColorVect, void> SetDefaultMaterialBaseColor { get; private set; }
	public delegate* managed<ResourceStub, DefaultMaterialShadingStyle, void> SetDefaultMaterialShadingStyle { get; private set; }

	public delegate* managed<ResourceStub, Angle> GetColorHue { get; private set; }
	public delegate* managed<ResourceStub, Angle, void> SetColorHue { get; private set; }
	public delegate* managed<ResourceStub, float> GetColorSaturation { get; private set; }
	public delegate* managed<ResourceStub, float, void> SetColorSaturation { get; private set; }
	public delegate* managed<ResourceStub, float> GetColorLightness { get; private set; }
	public delegate* managed<ResourceStub, float, void> SetColorLightness { get; private set; }
	public delegate* managed<ResourceStub, Angle, void> AdjustColorHueBy { get; private set; }
	public delegate* managed<ResourceStub, float, void> AdjustColorSaturationBy { get; private set; }
	public delegate* managed<ResourceStub, float, void> AdjustColorLightnessBy { get; private set; }

	SceneObjectAdapterFunctionTable(SceneObjectType sceneObjectType) {
		SceneObjectType = sceneObjectType;
		SetAllToNoOp();
	}

	#region No-Op Defaults
	static Location NoOpGetPosition(ResourceStub _) => Location.Origin;
	static Rotation NoOpGetRotation(ResourceStub _) => Rotation.None;
	static Quaternion NoOpGetRotationQuaternion(ResourceStub _) => Quaternion.Identity;
	static Vect NoOpGetScaling(ResourceStub _) => Vect.One;
	static Transform NoOpGetTransform(ResourceStub _) => Transform.None;
	static Angle NoOpGetColorHue(ResourceStub _) => Angle.Zero;
	static float NoOpGetColorComponent(ResourceStub _) => 0f;
	static void NoOp<TArg>(ResourceStub _, TArg __) { }
	static void NoOp<TArg1, TArg2>(ResourceStub _, TArg1 __, TArg2 ___) { }

	void SetAllToNoOp() {
		GetPosition = &NoOpGetPosition;
		SetPosition = &NoOp<Location>;
		MoveBy = &NoOp<Vect>;

		GetRotation = &NoOpGetRotation;
		SetRotation = &NoOp<Rotation>;
		GetRotationQuaternion = &NoOpGetRotationQuaternion;
		SetRotationQuaternion = &NoOp<Quaternion>;
		RotateByRotation = &NoOp<Rotation>;
		RotateByQuaternion = &NoOp<Quaternion>;

		GetScaling = &NoOpGetScaling;
		SetScaling = &NoOp<Vect>;
		ScaleByScalar = &NoOp<float>;
		ScaleByVect = &NoOp<Vect>;
		AdjustScaleByScalar = &NoOp<float>;
		AdjustScaleByVect = &NoOp<Vect>;

		GetTransform = &NoOpGetTransform;
		SetTransform = &NoOp<Transform>;
		RotateByRotationAroundPivot = &NoOp<Rotation, Location>;
		RotateByQuaternionAroundPivot = &NoOp<Quaternion, Location>;

		SetMaterial = &NoOp<Material>;
		SetDefaultMaterialBaseColor = &NoOp<ColorVect>;
		SetDefaultMaterialShadingStyle = &NoOp<DefaultMaterialShadingStyle>;

		GetColorHue = &NoOpGetColorHue;
		SetColorHue = &NoOp<Angle>;
		GetColorSaturation = &NoOpGetColorComponent;
		SetColorSaturation = &NoOp<float>;
		GetColorLightness = &NoOpGetColorComponent;
		SetColorLightness = &NoOp<float>;
		AdjustColorHueBy = &NoOp<Angle>;
		AdjustColorSaturationBy = &NoOp<float>;
		AdjustColorLightnessBy = &NoOp<float>;
	}
	#endregion

	#region Capability Adapters
	void AddPositioned<T, TConverter>() where T : IPositionedSceneObject where TConverter : IStubConverter<T> {
		static Location GetPositionAdapter(ResourceStub stub) => TConverter.FromStub(stub).Position;
		static void SetPositionAdapter(ResourceStub stub, Location position) {
			var obj = TConverter.FromStub(stub);
			obj.Position = position;
		}
		static void MoveByAdapter(ResourceStub stub, Vect translation) => TConverter.FromStub(stub).MoveBy(translation);

		GetPosition = &GetPositionAdapter;
		SetPosition = &SetPositionAdapter;
		MoveBy = &MoveByAdapter;
	}

	void AddOriented<T, TConverter>() where T : IOrientedSceneObject where TConverter : IStubConverter<T> {
		static Rotation GetRotationAdapter(ResourceStub stub) => TConverter.FromStub(stub).Rotation;
		static void SetRotationAdapter(ResourceStub stub, Rotation rotation) {
			var obj = TConverter.FromStub(stub);
			obj.Rotation = rotation;
		}
		static Quaternion GetRotationQuaternionAdapter(ResourceStub stub) => TConverter.FromStub(stub).RotationQuaternion;
		static void SetRotationQuaternionAdapter(ResourceStub stub, Quaternion rotationQuaternion) {
			var obj = TConverter.FromStub(stub);
			obj.RotationQuaternion = rotationQuaternion;
		}
		static void RotateByRotationAdapter(ResourceStub stub, Rotation rotation) => TConverter.FromStub(stub).RotateBy(rotation);
		static void RotateByQuaternionAdapter(ResourceStub stub, Quaternion rotationQuaternion) => TConverter.FromStub(stub).RotateBy(rotationQuaternion);

		GetRotation = &GetRotationAdapter;
		SetRotation = &SetRotationAdapter;
		GetRotationQuaternion = &GetRotationQuaternionAdapter;
		SetRotationQuaternion = &SetRotationQuaternionAdapter;
		RotateByRotation = &RotateByRotationAdapter;
		RotateByQuaternion = &RotateByQuaternionAdapter;
	}

	void AddScaled<T, TConverter>() where T : IScaledSceneObject where TConverter : IStubConverter<T> {
		static Vect GetScalingAdapter(ResourceStub stub) => TConverter.FromStub(stub).Scaling;
		static void SetScalingAdapter(ResourceStub stub, Vect scaling) {
			var obj = TConverter.FromStub(stub);
			obj.Scaling = scaling;
		}
		static void ScaleByScalarAdapter(ResourceStub stub, float scalar) => TConverter.FromStub(stub).ScaleBy(scalar);
		static void ScaleByVectAdapter(ResourceStub stub, Vect vect) => TConverter.FromStub(stub).ScaleBy(vect);
		static void AdjustScaleByScalarAdapter(ResourceStub stub, float scalar) => TConverter.FromStub(stub).AdjustScaleBy(scalar);
		static void AdjustScaleByVectAdapter(ResourceStub stub, Vect vect) => TConverter.FromStub(stub).AdjustScaleBy(vect);

		GetScaling = &GetScalingAdapter;
		SetScaling = &SetScalingAdapter;
		ScaleByScalar = &ScaleByScalarAdapter;
		ScaleByVect = &ScaleByVectAdapter;
		AdjustScaleByScalar = &AdjustScaleByScalarAdapter;
		AdjustScaleByVect = &AdjustScaleByVectAdapter;
	}

	void AddTransformed<T, TConverter>() where T : ITransformedSceneObject where TConverter : IStubConverter<T> {
		static Transform GetTransformAdapter(ResourceStub stub) => TConverter.FromStub(stub).Transform;
		static void SetTransformAdapter(ResourceStub stub, Transform transform) {
			var obj = TConverter.FromStub(stub);
			obj.Transform = transform;
		}
		static void RotateByRotationAroundPivotAdapter(ResourceStub stub, Rotation rotation, Location pivotPoint) => TConverter.FromStub(stub).RotateBy(rotation, pivotPoint);
		static void RotateByQuaternionAroundPivotAdapter(ResourceStub stub, Quaternion rotationQuaternion, Location pivotPoint) => TConverter.FromStub(stub).RotateBy(rotationQuaternion, pivotPoint);

		AddPositioned<T, TConverter>();
		AddOriented<T, TConverter>();
		AddScaled<T, TConverter>();
		GetTransform = &GetTransformAdapter;
		SetTransform = &SetTransformAdapter;
		RotateByRotationAroundPivot = &RotateByRotationAroundPivotAdapter;
		RotateByQuaternionAroundPivot = &RotateByQuaternionAroundPivotAdapter;
	}

	void AddMaterialReceiving<T, TConverter>() where T : IMaterialReceivingSceneObject where TConverter : IStubConverter<T> {
		static void SetMaterialAdapter(ResourceStub stub, Material material) => TConverter.FromStub(stub).SetMaterial(material);
		static void SetDefaultMaterialBaseColorAdapter(ResourceStub stub, ColorVect baseColor) => TConverter.FromStub(stub).SetDefaultMaterialBaseColor(baseColor);
		static void SetDefaultMaterialShadingStyleAdapter(ResourceStub stub, DefaultMaterialShadingStyle style) => TConverter.FromStub(stub).SetDefaultMaterialShadingStyle(style);

		SetMaterial = &SetMaterialAdapter;
		SetDefaultMaterialBaseColor = &SetDefaultMaterialBaseColorAdapter;
		SetDefaultMaterialShadingStyle = &SetDefaultMaterialShadingStyleAdapter;
	}

	void AddColored<T, TConverter>() where T : IColoredSceneObject where TConverter : IStubConverter<T> {
		static Angle GetColorHueAdapter(ResourceStub stub) => TConverter.FromStub(stub).ColorHue;
		static void SetColorHueAdapter(ResourceStub stub, Angle hue) {
			var obj = TConverter.FromStub(stub);
			obj.ColorHue = hue;
		}
		static float GetColorSaturationAdapter(ResourceStub stub) => TConverter.FromStub(stub).ColorSaturation;
		static void SetColorSaturationAdapter(ResourceStub stub, float saturation) {
			var obj = TConverter.FromStub(stub);
			obj.ColorSaturation = saturation;
		}
		static float GetColorLightnessAdapter(ResourceStub stub) => TConverter.FromStub(stub).ColorLightness;
		static void SetColorLightnessAdapter(ResourceStub stub, float lightness) {
			var obj = TConverter.FromStub(stub);
			obj.ColorLightness = lightness;
		}
		static void AdjustColorHueByAdapter(ResourceStub stub, Angle adjustment) => TConverter.FromStub(stub).AdjustColorHueBy(adjustment);
		static void AdjustColorSaturationByAdapter(ResourceStub stub, float adjustment) => TConverter.FromStub(stub).AdjustColorSaturationBy(adjustment);
		static void AdjustColorLightnessByAdapter(ResourceStub stub, float adjustment) => TConverter.FromStub(stub).AdjustColorLightnessBy(adjustment);

		GetColorHue = &GetColorHueAdapter;
		SetColorHue = &SetColorHueAdapter;
		GetColorSaturation = &GetColorSaturationAdapter;
		SetColorSaturation = &SetColorSaturationAdapter;
		GetColorLightness = &GetColorLightnessAdapter;
		SetColorLightness = &SetColorLightnessAdapter;
		AdjustColorHueBy = &AdjustColorHueByAdapter;
		AdjustColorSaturationBy = &AdjustColorSaturationByAdapter;
		AdjustColorLightnessBy = &AdjustColorLightnessByAdapter;
	}
	#endregion

	#region Per-Type Tables
	public static SceneObjectAdapterFunctionTable ForModelInstance { get; } = CreateForModelInstance();
	static SceneObjectAdapterFunctionTable CreateForModelInstance() {
		var result = new SceneObjectAdapterFunctionTable(SceneObjectType.ModelInstance);
		result.AddTransformed<ModelInstance, ResourceStubConverter<ModelInstance>>();
		result.AddMaterialReceiving<ModelInstance, ResourceStubConverter<ModelInstance>>();
		return result;
	}

	public static SceneObjectAdapterFunctionTable ForModelInstanceGroup { get; } = CreateForModelInstanceGroup();
	static SceneObjectAdapterFunctionTable CreateForModelInstanceGroup() {
		var result = new SceneObjectAdapterFunctionTable(SceneObjectType.ModelInstanceGroup);
		result.AddTransformed<ModelInstanceGroup, ModelInstanceGroupStubConverter>();
		result.AddMaterialReceiving<ModelInstanceGroup, ModelInstanceGroupStubConverter>();
		return result;
	}

	public static SceneObjectAdapterFunctionTable ForMutableGridInstance { get; } = CreateForMutableGridInstance();
	static SceneObjectAdapterFunctionTable CreateForMutableGridInstance() {
		var result = new SceneObjectAdapterFunctionTable(SceneObjectType.MutableGridInstance);
		result.AddTransformed<ModelInstance, ResourceStubConverter<ModelInstance>>();
		result.AddMaterialReceiving<ModelInstance, ResourceStubConverter<ModelInstance>>();
		return result;
	}

	public static SceneObjectAdapterFunctionTable ForQuadInstance { get; } = CreateForQuadInstance();
	static SceneObjectAdapterFunctionTable CreateForQuadInstance() {
		var result = new SceneObjectAdapterFunctionTable(SceneObjectType.QuadInstance);
		result.AddTransformed<QuadInstance, QuadInstanceStubConverter>();
		result.AddMaterialReceiving<QuadInstance, QuadInstanceStubConverter>();
		return result;
	}

	public static SceneObjectAdapterFunctionTable ForCameraLockedQuadInstance { get; } = CreateForCameraLockedQuadInstance();
	static SceneObjectAdapterFunctionTable CreateForCameraLockedQuadInstance() {
		var result = new SceneObjectAdapterFunctionTable(SceneObjectType.CameraLockedQuadInstance);
		result.AddPositioned<ModelInstance, ResourceStubConverter<ModelInstance>>();
		result.AddScaled<ModelInstance, ResourceStubConverter<ModelInstance>>();
		result.AddMaterialReceiving<ModelInstance, ResourceStubConverter<ModelInstance>>();
		return result;
	}

	public static SceneObjectAdapterFunctionTable ForTextInstance { get; } = CreateForTextInstance();
	static SceneObjectAdapterFunctionTable CreateForTextInstance() {
		var result = new SceneObjectAdapterFunctionTable(SceneObjectType.TextInstance);
		result.AddTransformed<TextInstance, TextInstanceStubConverter>();
		return result;
	}

	public static SceneObjectAdapterFunctionTable ForCameraLockedTextInstance { get; } = CreateForCameraLockedTextInstance();
	static SceneObjectAdapterFunctionTable CreateForCameraLockedTextInstance() {
		var result = new SceneObjectAdapterFunctionTable(SceneObjectType.CameraLockedTextInstance);
		result.AddPositioned<ModelInstance, ResourceStubConverter<ModelInstance>>();
		result.AddScaled<ModelInstance, ResourceStubConverter<ModelInstance>>();
		return result;
	}

	public static SceneObjectAdapterFunctionTable ForPointLight { get; } = CreateForPointLight();
	static SceneObjectAdapterFunctionTable CreateForPointLight() {
		var result = new SceneObjectAdapterFunctionTable(SceneObjectType.PointLight);
		result.AddPositioned<PointLight, ResourceStubConverter<PointLight>>();
		result.AddColored<PointLight, ResourceStubConverter<PointLight>>();
		return result;
	}

	public static SceneObjectAdapterFunctionTable ForSpotLight { get; } = CreateForSpotLight();
	static SceneObjectAdapterFunctionTable CreateForSpotLight() {
		var result = new SceneObjectAdapterFunctionTable(SceneObjectType.SpotLight);
		result.AddPositioned<SpotLight, ResourceStubConverter<SpotLight>>();
		result.AddOriented<SpotLight, ResourceStubConverter<SpotLight>>();
		result.AddColored<SpotLight, ResourceStubConverter<SpotLight>>();
		return result;
	}

	public static SceneObjectAdapterFunctionTable ForDirectionalLight { get; } = CreateForDirectionalLight();
	static SceneObjectAdapterFunctionTable CreateForDirectionalLight() {
		var result = new SceneObjectAdapterFunctionTable(SceneObjectType.DirectionalLight);
		result.AddOriented<DirectionalLight, ResourceStubConverter<DirectionalLight>>();
		result.AddColored<DirectionalLight, ResourceStubConverter<DirectionalLight>>();
		return result;
	}

	public static SceneObjectAdapterFunctionTable ForCamera { get; } = CreateForCamera();
	static SceneObjectAdapterFunctionTable CreateForCamera() {
		var result = new SceneObjectAdapterFunctionTable(SceneObjectType.Camera);
		result.AddPositioned<Camera, ResourceStubConverter<Camera>>();
		result.AddOriented<Camera, ResourceStubConverter<Camera>>();
		return result;
	}
	#endregion
}

public readonly unsafe record struct SceneObject : ITransformedSceneObject, IColoredSceneObject, IMaterialReceivingSceneObject, IStringSpanNameEnabled {
	internal ResourceStub Stub { get; }
	internal SceneObjectAdapterFunctionTable FunctionTable => field ?? throw InvalidObjectException.InvalidDefault<SceneObject>();

	public SceneObjectType Type => FunctionTable.SceneObjectType;
	static SceneObjectType ISceneObject.SceneObjectType { get; } = SceneObjectType.Unspecified;

	internal SceneObject(ResourceStub stub, SceneObjectAdapterFunctionTable functionTable) {
		Stub = stub;
		FunctionTable = functionTable;
	}
	public SceneObject(ModelInstance mi) : this(ToStub(mi), SceneObjectAdapterFunctionTable.ForModelInstance) { }
	public SceneObject(ModelInstanceGroup group) : this(ToStub(group.UnderlyingResourceGroup), SceneObjectAdapterFunctionTable.ForModelInstanceGroup) { }
	public SceneObject(MutableGridInstance grid) : this(ToStub(grid.UnderlyingModelInstance), SceneObjectAdapterFunctionTable.ForMutableGridInstance) { }
	public SceneObject(QuadInstance quad) : this(ToStub(quad.UnderlyingModelInstance), SceneObjectAdapterFunctionTable.ForQuadInstance) { }
	public SceneObject(CameraLockedQuadInstance quad) : this(ToStub(quad.UnderlyingQuadInstance.UnderlyingModelInstance), SceneObjectAdapterFunctionTable.ForCameraLockedQuadInstance) { }
	public SceneObject(TextInstance text) : this(ToStub(text.UnderlyingModelInstance), SceneObjectAdapterFunctionTable.ForTextInstance) { }
	public SceneObject(CameraLockedTextInstance text) : this(ToStub(text.UnderlyingTextInstance.UnderlyingModelInstance), SceneObjectAdapterFunctionTable.ForCameraLockedTextInstance) { }
	public SceneObject(PointLight light) : this(ToStub(light), SceneObjectAdapterFunctionTable.ForPointLight) { }
	public SceneObject(SpotLight light) : this(ToStub(light), SceneObjectAdapterFunctionTable.ForSpotLight) { }
	public SceneObject(DirectionalLight light) : this(ToStub(light), SceneObjectAdapterFunctionTable.ForDirectionalLight) { }
	public SceneObject(Camera camera) : this(ToStub(camera), SceneObjectAdapterFunctionTable.ForCamera) { }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(ModelInstance operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(ModelInstanceGroup operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(MutableGridInstance operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(QuadInstance operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(CameraLockedQuadInstance operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(TextInstance operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(CameraLockedTextInstance operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(PointLight operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(SpotLight operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(DirectionalLight operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(Camera operand) => new(operand);

	public static explicit operator ModelInstance(SceneObject operand) {
		if (operand.Type.IsStoredAsModelInstance()) return FastFromStub<ModelInstance>(operand.Stub);
		throw operand.CreateInvalidCastException(SceneObjectType.ModelInstance);
	}
	public static explicit operator ModelInstanceGroup(SceneObject operand) {
		operand.ThrowIfNotOfType(SceneObjectType.ModelInstanceGroup);
		return new(FastFromStub<ResourceGroup>(operand.Stub));
	}
	public static explicit operator QuadInstance(SceneObject operand) {
		operand.ThrowIfNotOfType(SceneObjectType.QuadInstance);
		return new(FastFromStub<ModelInstance>(operand.Stub));
	}
	public static explicit operator TextInstance(SceneObject operand) {
		operand.ThrowIfNotOfType(SceneObjectType.TextInstance);
		return new(FastFromStub<ModelInstance>(operand.Stub));
	}
	public static explicit operator PointLight(SceneObject operand) {
		operand.ThrowIfNotOfType(SceneObjectType.PointLight);
		return FastFromStub<PointLight>(operand.Stub);
	}
	public static explicit operator SpotLight(SceneObject operand) {
		operand.ThrowIfNotOfType(SceneObjectType.SpotLight);
		return FastFromStub<SpotLight>(operand.Stub);
	}
	public static explicit operator DirectionalLight(SceneObject operand) {
		operand.ThrowIfNotOfType(SceneObjectType.DirectionalLight);
		return FastFromStub<DirectionalLight>(operand.Stub);
	}
	public static explicit operator Camera(SceneObject operand) {
		operand.ThrowIfNotOfType(SceneObjectType.Camera);
		return FastFromStub<Camera>(operand.Stub);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void ThrowIfNotOfType(SceneObjectType targetType) {
		if (Type != targetType) throw CreateInvalidCastException(targetType);
	}
	InvalidCastException CreateInvalidCastException(SceneObjectType targetType) {
		return new InvalidCastException($"Can not convert {nameof(SceneObject)} of type {Type} to {targetType}.");
	}

	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetPosition(Stub);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetPosition(Stub, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveBy(Vect translation) => FunctionTable.MoveBy(Stub, translation);

	public Rotation Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetRotation(Stub);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetRotation(Stub, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotation(Rotation rotation) => Rotation = rotation;

	public Quaternion RotationQuaternion {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetRotationQuaternion(Stub);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetRotationQuaternion(Stub, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotationQuaternion(Quaternion rotationQuaternion) => RotationQuaternion = rotationQuaternion;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Rotation rotation) => FunctionTable.RotateByRotation(Stub, rotation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Quaternion rotationQuaternion) => FunctionTable.RotateByQuaternion(Stub, rotationQuaternion);

	public Vect Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetScaling(Stub);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetScaling(Stub, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetScaling(Vect scaling) => Scaling = scaling;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(float scalar) => FunctionTable.ScaleByScalar(Stub, scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(Vect vect) => FunctionTable.ScaleByVect(Stub, vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(float scalar) => FunctionTable.AdjustScaleByScalar(Stub, scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(Vect vect) => FunctionTable.AdjustScaleByVect(Stub, vect);

	public Transform Transform {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetTransform(Stub);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetTransform(Stub, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTransform(Transform transform) => Transform = transform;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Rotation rotation, Location pivotPoint) => FunctionTable.RotateByRotationAroundPivot(Stub, rotation, pivotPoint);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Quaternion rotationQuaternion, Location pivotPoint) => FunctionTable.RotateByQuaternionAroundPivot(Stub, rotationQuaternion, pivotPoint);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetMaterial(Material material) => FunctionTable.SetMaterial(Stub, material);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDefaultMaterialBaseColor(ColorVect baseColor) => FunctionTable.SetDefaultMaterialBaseColor(Stub, baseColor);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDefaultMaterialShadingStyle(DefaultMaterialShadingStyle style) => FunctionTable.SetDefaultMaterialShadingStyle(Stub, style);

	public Angle ColorHue {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetColorHue(Stub);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetColorHue(Stub, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorHue(Angle hue) => ColorHue = hue;

	public float ColorSaturation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetColorSaturation(Stub);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetColorSaturation(Stub, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorSaturation(float saturation) => ColorSaturation = saturation;

	public float ColorLightness {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetColorLightness(Stub);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetColorLightness(Stub, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorLightness(float lightness) => ColorLightness = lightness;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustColorHueBy(Angle adjustment) => FunctionTable.AdjustColorHueBy(Stub, adjustment);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustColorSaturationBy(float adjustment) => FunctionTable.AdjustColorSaturationBy(Stub, adjustment);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustColorLightnessBy(float adjustment) => FunctionTable.AdjustColorLightnessBy(Stub, adjustment);

	public string GetNameAsNewStringObject() {
		if (Type == SceneObjectType.Unspecified) throw InvalidObjectException.InvalidDefault<ResourceStub>();
		return Stub.GetNameAsNewStringObject();
	}
	public int GetNameLength() {
		if (Type == SceneObjectType.Unspecified) throw InvalidObjectException.InvalidDefault<ResourceStub>();
		return Stub.GetNameLength();
	}
	public void CopyName(Span<char> destinationBuffer) {
		if (Type == SceneObjectType.Unspecified) throw InvalidObjectException.InvalidDefault<ResourceStub>();
		Stub.CopyName(destinationBuffer);
	}

	public void DisposeUnderlyingObject() {
		if (Type == SceneObjectType.Unspecified) throw InvalidObjectException.InvalidDefault<ResourceStub>();
		Stub.Dispose();
	}

	public override string ToString() => $"Scene Object ({Type}) \"{Stub.GetNameAsNewStringObject()}\"";
}
