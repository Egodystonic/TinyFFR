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

/// <summary>
/// Identifies which kind of object a <see cref="SceneObject"/> wraps.
/// </summary>
/// <remarks>
/// Use the extension methods in <see cref="SceneObjectTypeExtensions"/> (such as <see cref="SceneObjectTypeExtensions.IsTransformed"/>) to ask which capabilities
/// a given type supports, rather than comparing against every value.
/// </remarks>
public enum SceneObjectType {
	/// <summary>
	/// No type; the value of a <c>default</c> <see cref="SceneObject"/>, which is not valid for use.
	/// </summary>
	Unspecified = 0,
	/// <summary>
	/// A <see cref="World.ModelInstance"/>.
	/// </summary>
	ModelInstance = (TransformedFlag | MaterialReceivingFlag | StoresOnlyModelInstanceFlag) + 1,
	/// <summary>
	/// A <see cref="World.ModelInstanceGroup"/>.
	/// </summary>
	ModelInstanceGroup = (TransformedFlag | MaterialReceivingFlag) + 2,
	/// <summary>
	/// A <see cref="Assets.Meshes.MutableGridInstance"/>.
	/// </summary>
	MutableGridInstance = (TransformedFlag | MaterialReceivingFlag | StoresOnlyModelInstanceFlag) + 3,
	/// <summary>
	/// A <see cref="Assets.Meshes.QuadInstance"/>.
	/// </summary>
	QuadInstance = (TransformedFlag | MaterialReceivingFlag | StoresOnlyModelInstanceFlag) + 4,
	/// <summary>
	/// A <see cref="Assets.Meshes.CameraLockedQuadInstance"/>.
	/// </summary>
	CameraLockedQuadInstance = (PositionedFlag | ScaledFlag | MaterialReceivingFlag | StoresOnlyModelInstanceFlag) + 5,
	/// <summary>
	/// A <see cref="Assets.Text.TextInstance"/>.
	/// </summary>
	TextInstance = (TransformedFlag | StoresOnlyModelInstanceFlag) + 6,
	/// <summary>
	/// A <see cref="Assets.Text.CameraLockedTextInstance"/>.
	/// </summary>
	CameraLockedTextInstance = (PositionedFlag | ScaledFlag | StoresOnlyModelInstanceFlag) + 7,
	/// <summary>
	/// A <see cref="World.PointLight"/>.
	/// </summary>
	PointLight = (PositionedFlag | ColoredFlag) + 8,
	/// <summary>
	/// A <see cref="World.SpotLight"/>.
	/// </summary>
	SpotLight = (PositionedFlag | OrientedFlag | ColoredFlag) + 9,
	/// <summary>
	/// A <see cref="World.DirectionalLight"/>.
	/// </summary>
	DirectionalLight = (OrientedFlag | ColoredFlag) + 10,
	/// <summary>
	/// A <see cref="World.Camera"/>.
	/// </summary>
	Camera = (PositionedFlag | OrientedFlag) + 11
}

/// <summary>
/// Extension methods for <see cref="SceneObjectType"/> that report which capabilities each type of scene object supports.
/// </summary>
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
		/// <summary>
		/// Returns <see langword="true"/> if objects of this type have a position that can be read and changed (see <see cref="IPositionedSceneObject"/>).
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsPositioned() => @this.FlagExists(PositionedFlag);
		/// <summary>
		/// Returns <see langword="true"/> if objects of this type have an orientation that can be read and changed (see <see cref="IOrientedSceneObject"/>).
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsOriented() => @this.FlagExists(OrientedFlag);
		/// <summary>
		/// Returns <see langword="true"/> if objects of this type have a scale that can be read and changed (see <see cref="IScaledSceneObject"/>).
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsScaled() => @this.FlagExists(ScaledFlag);
		/// <summary>
		/// Returns <see langword="true"/> if objects of this type are positioned, oriented and scaled, i.e. has a full <see cref="Transform"/> (see <see cref="ITransformedSceneObject"/>).
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsTransformed() => @this.FlagExists(TransformedFlag);
		/// <summary>
		/// Returns <see langword="true"/> if objects of this type can have their material set (see <see cref="IMaterialReceivingSceneObject"/>).
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsMaterialReceiving() => @this.FlagExists(MaterialReceivingFlag);
		/// <summary>
		/// Returns <see langword="true"/> if objects of this type have a colour that can be read and changed (see <see cref="IColoredSceneObject"/>).
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsColored() => @this.FlagExists(ColoredFlag);
		/// <summary>
		/// Returns <see langword="true"/> if objects of this type are backed by a single <see cref="ModelInstance"/>, and can therefore be explicitly converted to one.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsStoredAsModelInstance() => @this.FlagExists(StoresOnlyModelInstanceFlag);
	}
}

/// <summary>
/// Base interface for every object that can be placed in (or used to view) a scene, and therefore wrapped in a <see cref="SceneObject"/>.
/// </summary>
public interface ISceneObject {
	/// <summary>
	/// The <see cref="World.SceneObjectType"/> that identifies this kind of object.
	/// </summary>
	static abstract SceneObjectType SceneObjectType { get; }
}

readonly struct GroupInstanceCache : IEquatable<GroupInstanceCache> {
	public ModelInstance[] Instances { get; }
	public int Count { get; }
	public GroupInstanceCache(ModelInstance[] instances, int count) {
		Instances = instances;
		Count = count;
	}
	public bool Equals(GroupInstanceCache other) => true;
	public override bool Equals(object? obj) => obj is GroupInstanceCache;
	public override int GetHashCode() => 0;
}

internal unsafe sealed class SceneObjectAdapterFunctionTable {
	interface IStubConverter<out TTargetType> {
		static abstract TTargetType FromSceneObject(in SceneObject sceneObject);
	}
	
	readonly struct ResourceStubConverter<TResource> : IStubConverter<TResource> where TResource : IResource<TResource> {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TResource FromSceneObject(in SceneObject sceneObject) => FastFromStub<TResource>(sceneObject.Stub);
	}
	readonly struct ModelInstanceGroupStubConverter : IStubConverter<ModelInstanceGroup> {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ModelInstanceGroup FromSceneObject(in SceneObject sceneObject) => new(FastFromStub<ResourceGroup>(sceneObject.Stub));
	}
	readonly struct QuadInstanceStubConverter : IStubConverter<QuadInstance> {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static QuadInstance FromSceneObject(in SceneObject sceneObject) => new(FastFromStub<ModelInstance>(sceneObject.Stub));
	}
	readonly struct TextInstanceStubConverter : IStubConverter<TextInstance> {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TextInstance FromSceneObject(in SceneObject sceneObject) => new(FastFromStub<ModelInstance>(sceneObject.Stub));
	}

	public SceneObjectType SceneObjectType { get; }

	public delegate* managed<in SceneObject, Location> GetPosition { get; private set; }
	public delegate* managed<in SceneObject, Location, void> SetPosition { get; private set; }
	public delegate* managed<in SceneObject, Vect, void> MoveBy { get; private set; }

	public delegate* managed<in SceneObject, Rotation> GetRotation { get; private set; }
	public delegate* managed<in SceneObject, Rotation, void> SetRotation { get; private set; }
	public delegate* managed<in SceneObject, Quaternion> GetRotationQuaternion { get; private set; }
	public delegate* managed<in SceneObject, Quaternion, void> SetRotationQuaternion { get; private set; }
	public delegate* managed<in SceneObject, Rotation, void> RotateByRotation { get; private set; }
	public delegate* managed<in SceneObject, Quaternion, void> RotateByQuaternion { get; private set; }

	public delegate* managed<in SceneObject, Vect> GetScaling { get; private set; }
	public delegate* managed<in SceneObject, Vect, void> SetScaling { get; private set; }
	public delegate* managed<in SceneObject, float, void> ScaleByScalar { get; private set; }
	public delegate* managed<in SceneObject, Vect, void> ScaleByVect { get; private set; }
	public delegate* managed<in SceneObject, float, void> AdjustScaleByScalar { get; private set; }
	public delegate* managed<in SceneObject, Vect, void> AdjustScaleByVect { get; private set; }

	public delegate* managed<in SceneObject, Transform> GetTransform { get; private set; }
	public delegate* managed<in SceneObject, Transform, void> SetTransform { get; private set; }
	public delegate* managed<in SceneObject, Rotation, Location, void> RotateByRotationAroundPivot { get; private set; }
	public delegate* managed<in SceneObject, Quaternion, Location, void> RotateByQuaternionAroundPivot { get; private set; }

	public delegate* managed<in SceneObject, Material, void> SetMaterial { get; private set; }
	public delegate* managed<in SceneObject, ColorVect, void> SetDefaultMaterialBaseColor { get; private set; }
	public delegate* managed<in SceneObject, DefaultMaterialShadingStyle, void> SetDefaultMaterialShadingStyle { get; private set; }

	public delegate* managed<in SceneObject, Angle> GetColorHue { get; private set; }
	public delegate* managed<in SceneObject, Angle, void> SetColorHue { get; private set; }
	public delegate* managed<in SceneObject, float> GetColorSaturation { get; private set; }
	public delegate* managed<in SceneObject, float, void> SetColorSaturation { get; private set; }
	public delegate* managed<in SceneObject, float> GetColorLightness { get; private set; }
	public delegate* managed<in SceneObject, float, void> SetColorLightness { get; private set; }
	public delegate* managed<in SceneObject, Angle, void> AdjustColorHueBy { get; private set; }
	public delegate* managed<in SceneObject, float, void> AdjustColorSaturationBy { get; private set; }
	public delegate* managed<in SceneObject, float, void> AdjustColorLightnessBy { get; private set; }

	SceneObjectAdapterFunctionTable(SceneObjectType sceneObjectType) {
		SceneObjectType = sceneObjectType;
		SetAllToNoOp();
	}

	#region No-Op Defaults
	static Location NoOpGetPosition(in SceneObject _) => Location.Origin;
	static Rotation NoOpGetRotation(in SceneObject _) => Rotation.None;
	static Quaternion NoOpGetRotationQuaternion(in SceneObject _) => Quaternion.Identity;
	static Vect NoOpGetScaling(in SceneObject _) => Vect.One;
	static Transform NoOpGetTransform(in SceneObject _) => Transform.None;
	static Angle NoOpGetColorHue(in SceneObject _) => Angle.Zero;
	static float NoOpGetColorComponent(in SceneObject _) => 0f;
	static void NoOp<TArg>(in SceneObject _, TArg __) { }
	static void NoOp<TArg1, TArg2>(in SceneObject _, TArg1 __, TArg2 ___) { }

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
		static Location GetPositionAdapter(in SceneObject sceneObject) => TConverter.FromSceneObject(sceneObject).Position;
		static void SetPositionAdapter(in SceneObject sceneObject, Location position) {
			var obj = TConverter.FromSceneObject(sceneObject);
			obj.Position = position;
		}
		static void MoveByAdapter(in SceneObject sceneObject, Vect translation) => TConverter.FromSceneObject(sceneObject).MoveBy(translation);

		GetPosition = &GetPositionAdapter;
		SetPosition = &SetPositionAdapter;
		MoveBy = &MoveByAdapter;
	}

	void AddOriented<T, TConverter>() where T : IOrientedSceneObject where TConverter : IStubConverter<T> {
		static Rotation GetRotationAdapter(in SceneObject sceneObject) => TConverter.FromSceneObject(sceneObject).Rotation;
		static void SetRotationAdapter(in SceneObject sceneObject, Rotation rotation) {
			var obj = TConverter.FromSceneObject(sceneObject);
			obj.Rotation = rotation;
		}
		static Quaternion GetRotationQuaternionAdapter(in SceneObject sceneObject) => TConverter.FromSceneObject(sceneObject).RotationQuaternion;
		static void SetRotationQuaternionAdapter(in SceneObject sceneObject, Quaternion rotationQuaternion) {
			var obj = TConverter.FromSceneObject(sceneObject);
			obj.RotationQuaternion = rotationQuaternion;
		}
		static void RotateByRotationAdapter(in SceneObject sceneObject, Rotation rotation) => TConverter.FromSceneObject(sceneObject).RotateBy(rotation);
		static void RotateByQuaternionAdapter(in SceneObject sceneObject, Quaternion rotationQuaternion) => TConverter.FromSceneObject(sceneObject).RotateBy(rotationQuaternion);

		GetRotation = &GetRotationAdapter;
		SetRotation = &SetRotationAdapter;
		GetRotationQuaternion = &GetRotationQuaternionAdapter;
		SetRotationQuaternion = &SetRotationQuaternionAdapter;
		RotateByRotation = &RotateByRotationAdapter;
		RotateByQuaternion = &RotateByQuaternionAdapter;
	}

	void AddScaled<T, TConverter>() where T : IScaledSceneObject where TConverter : IStubConverter<T> {
		static Vect GetScalingAdapter(in SceneObject sceneObject) => TConverter.FromSceneObject(sceneObject).Scaling;
		static void SetScalingAdapter(in SceneObject sceneObject, Vect scaling) {
			var obj = TConverter.FromSceneObject(sceneObject);
			obj.Scaling = scaling;
		}
		static void ScaleByScalarAdapter(in SceneObject sceneObject, float scalar) => TConverter.FromSceneObject(sceneObject).ScaleBy(scalar);
		static void ScaleByVectAdapter(in SceneObject sceneObject, Vect vect) => TConverter.FromSceneObject(sceneObject).ScaleBy(vect);
		static void AdjustScaleByScalarAdapter(in SceneObject sceneObject, float scalar) => TConverter.FromSceneObject(sceneObject).AdjustScaleBy(scalar);
		static void AdjustScaleByVectAdapter(in SceneObject sceneObject, Vect vect) => TConverter.FromSceneObject(sceneObject).AdjustScaleBy(vect);

		GetScaling = &GetScalingAdapter;
		SetScaling = &SetScalingAdapter;
		ScaleByScalar = &ScaleByScalarAdapter;
		ScaleByVect = &ScaleByVectAdapter;
		AdjustScaleByScalar = &AdjustScaleByScalarAdapter;
		AdjustScaleByVect = &AdjustScaleByVectAdapter;
	}

	void AddTransformed<T, TConverter>() where T : ITransformedSceneObject where TConverter : IStubConverter<T> {
		static Transform GetTransformAdapter(in SceneObject sceneObject) => TConverter.FromSceneObject(sceneObject).Transform;
		static void SetTransformAdapter(in SceneObject sceneObject, Transform transform) {
			var obj = TConverter.FromSceneObject(sceneObject);
			obj.Transform = transform;
		}
		static void RotateByRotationAroundPivotAdapter(in SceneObject sceneObject, Rotation rotation, Location pivotPoint) => TConverter.FromSceneObject(sceneObject).RotateBy(rotation, pivotPoint);
		static void RotateByQuaternionAroundPivotAdapter(in SceneObject sceneObject, Quaternion rotationQuaternion, Location pivotPoint) => TConverter.FromSceneObject(sceneObject).RotateBy(rotationQuaternion, pivotPoint);

		AddPositioned<T, TConverter>();
		AddOriented<T, TConverter>();
		AddScaled<T, TConverter>();
		GetTransform = &GetTransformAdapter;
		SetTransform = &SetTransformAdapter;
		RotateByRotationAroundPivot = &RotateByRotationAroundPivotAdapter;
		RotateByQuaternionAroundPivot = &RotateByQuaternionAroundPivotAdapter;
	}

	void AddMaterialReceiving<T, TConverter>() where T : IMaterialReceivingSceneObject where TConverter : IStubConverter<T> {
		static void SetMaterialAdapter(in SceneObject sceneObject, Material material) => TConverter.FromSceneObject(sceneObject).SetMaterial(material);
		static void SetDefaultMaterialBaseColorAdapter(in SceneObject sceneObject, ColorVect baseColor) => TConverter.FromSceneObject(sceneObject).SetDefaultMaterialBaseColor(baseColor);
		static void SetDefaultMaterialShadingStyleAdapter(in SceneObject sceneObject, DefaultMaterialShadingStyle style) => TConverter.FromSceneObject(sceneObject).SetDefaultMaterialShadingStyle(style);

		SetMaterial = &SetMaterialAdapter;
		SetDefaultMaterialBaseColor = &SetDefaultMaterialBaseColorAdapter;
		SetDefaultMaterialShadingStyle = &SetDefaultMaterialShadingStyleAdapter;
	}

	void AddColored<T, TConverter>() where T : IColoredSceneObject where TConverter : IStubConverter<T> {
		static Angle GetColorHueAdapter(in SceneObject sceneObject) => TConverter.FromSceneObject(sceneObject).ColorHue;
		static void SetColorHueAdapter(in SceneObject sceneObject, Angle hue) {
			var obj = TConverter.FromSceneObject(sceneObject);
			obj.ColorHue = hue;
		}
		static float GetColorSaturationAdapter(in SceneObject sceneObject) => TConverter.FromSceneObject(sceneObject).ColorSaturation;
		static void SetColorSaturationAdapter(in SceneObject sceneObject, float saturation) {
			var obj = TConverter.FromSceneObject(sceneObject);
			obj.ColorSaturation = saturation;
		}
		static float GetColorLightnessAdapter(in SceneObject sceneObject) => TConverter.FromSceneObject(sceneObject).ColorLightness;
		static void SetColorLightnessAdapter(in SceneObject sceneObject, float lightness) {
			var obj = TConverter.FromSceneObject(sceneObject);
			obj.ColorLightness = lightness;
		}
		static void AdjustColorHueByAdapter(in SceneObject sceneObject, Angle adjustment) => TConverter.FromSceneObject(sceneObject).AdjustColorHueBy(adjustment);
		static void AdjustColorSaturationByAdapter(in SceneObject sceneObject, float adjustment) => TConverter.FromSceneObject(sceneObject).AdjustColorSaturationBy(adjustment);
		static void AdjustColorLightnessByAdapter(in SceneObject sceneObject, float adjustment) => TConverter.FromSceneObject(sceneObject).AdjustColorLightnessBy(adjustment);

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

/// <summary>
/// A "wrapper" type that abstracts over any object that can be added to a <see cref="Scene"/> and potentially also transformed, coloured, etc. 
/// </summary>
/// <remarks>
/// <para>
/// Every supported object type converts implicitly to <see cref="SceneObject"/>, so any APIs that accepts one can be passed any of them directly.
/// Use <see cref="Type"/> to find out what is wrapped, and the explicit conversion operators to get the original object back.
/// </para>
/// <para>
/// Note that not every type can be fully converted back however-- notably camera-locked types lose their camera lock data when converted to a SceneObject.
/// You can still convert these types back to a <see cref="ModelInstance"/>, but conversion back to the full camera-locked type requires usage of that target
/// type's smuggle API. 
/// </para>
/// <para>
/// Members that are not relevant to the wrapped object do nothing. For example, setting the colour of a model instance or the material of a light is
/// ignored, and reading an unsupported property returns a neutral default (i.e. <see cref="Rotation"/> returns <see cref="Rotation.None"/>). Check <see cref="Type"/>
/// (see <see cref="SceneObjectTypeExtensions"/>) first if you need to know whether a member will have an effect.
/// </para>
/// <para>
/// This type itself represents no managed or unmanaged memory and does not need to be disposed. It is cheap to create and use.
/// If you want to dispose the underlying object you can with <see cref="DisposeUnderlyingObject"/>. You <b>must not</b> dispose the underlying object (either
/// via <see cref="DisposeUnderlyingObject"/> or via its own <c>Dispose()</c> method) and then continue to use this SceneObject.
/// </para>
/// <para>
/// A <c>default</c> <see cref="SceneObject"/> wraps nothing and is not valid for use.
/// </para>
/// </remarks>
public readonly unsafe record struct SceneObject : ITransformedSceneObject, IColoredSceneObject, IMaterialReceivingSceneObject, IStringSpanNameEnabled {
	internal ResourceStub Stub { get; }
	internal SceneObjectAdapterFunctionTable FunctionTable => field ?? throw InvalidObjectException.InvalidDefault<SceneObject>();

	/// <summary>
	/// Which kind of object this wraps.
	/// </summary>
	public SceneObjectType Type => FunctionTable.SceneObjectType;
	static SceneObjectType ISceneObject.SceneObjectType { get; } = SceneObjectType.Unspecified;

	internal SceneObject(ResourceStub stub, SceneObjectAdapterFunctionTable functionTable) {
		Stub = stub;
		FunctionTable = functionTable;
	}
	/// <summary>
	/// Constructs a new <see cref="SceneObject"/> wrapping the given <see cref="ModelInstance"/>.
	/// You can also use the implicit conversion operator.
	/// </summary>
	/// <param name="mi">The object to wrap.</param>
	public SceneObject(ModelInstance mi) : this(ToStub(mi), SceneObjectAdapterFunctionTable.ForModelInstance) { }
	/// <summary>
	/// Constructs a new <see cref="SceneObject"/> wrapping the given <see cref="ModelInstanceGroup"/>.
	/// You can also use the implicit conversion operator.
	/// </summary>
	/// <param name="group">The object to wrap.</param>
	public SceneObject(ModelInstanceGroup group) : this(ToStub(group.UnderlyingResourceGroup), SceneObjectAdapterFunctionTable.ForModelInstanceGroup) { }
	/// <summary>
	/// Constructs a new <see cref="SceneObject"/> wrapping the given <see cref="MutableGridInstance"/>.
	/// You can also use the implicit conversion operator.
	/// </summary>
	/// <param name="grid">The object to wrap.</param>
	public SceneObject(MutableGridInstance grid) : this(ToStub(grid.UnderlyingModelInstance), SceneObjectAdapterFunctionTable.ForMutableGridInstance) { }
	/// <summary>
	/// Constructs a new <see cref="SceneObject"/> wrapping the given <see cref="QuadInstance"/>.
	/// You can also use the implicit conversion operator.
	/// </summary>
	/// <param name="quad">The object to wrap.</param>
	public SceneObject(QuadInstance quad) : this(ToStub(quad.UnderlyingModelInstance), SceneObjectAdapterFunctionTable.ForQuadInstance) { }
	/// <summary>
	/// Constructs a new <see cref="SceneObject"/> wrapping the given <see cref="CameraLockedQuadInstance"/>.
	/// You can also use the implicit conversion operator.
	/// </summary>
	/// <param name="quad">The object to wrap.</param>
	public SceneObject(CameraLockedQuadInstance quad) : this(ToStub(quad.UnderlyingQuadInstance.UnderlyingModelInstance), SceneObjectAdapterFunctionTable.ForCameraLockedQuadInstance) { }
	/// <summary>
	/// Constructs a new <see cref="SceneObject"/> wrapping the given <see cref="TextInstance"/>.
	/// You can also use the implicit conversion operator.
	/// </summary>
	/// <param name="text">The object to wrap.</param>
	public SceneObject(TextInstance text) : this(ToStub(text.UnderlyingModelInstance), SceneObjectAdapterFunctionTable.ForTextInstance) { }
	/// <summary>
	/// Constructs a new <see cref="SceneObject"/> wrapping the given <see cref="CameraLockedTextInstance"/>.
	/// You can also use the implicit conversion operator.
	/// </summary>
	/// <param name="text">The object to wrap.</param>
	public SceneObject(CameraLockedTextInstance text) : this(ToStub(text.UnderlyingTextInstance.UnderlyingModelInstance), SceneObjectAdapterFunctionTable.ForCameraLockedTextInstance) { }
	/// <summary>
	/// Constructs a new <see cref="SceneObject"/> wrapping the given <see cref="PointLight"/>.
	/// You can also use the implicit conversion operator.
	/// </summary>
	/// <param name="light">The object to wrap.</param>
	public SceneObject(PointLight light) : this(ToStub(light), SceneObjectAdapterFunctionTable.ForPointLight) { }
	/// <summary>
	/// Constructs a new <see cref="SceneObject"/> wrapping the given <see cref="SpotLight"/>.
	/// You can also use the implicit conversion operator.
	/// </summary>
	/// <param name="light">The object to wrap.</param>
	public SceneObject(SpotLight light) : this(ToStub(light), SceneObjectAdapterFunctionTable.ForSpotLight) { }
	/// <summary>
	/// Constructs a new <see cref="SceneObject"/> wrapping the given <see cref="DirectionalLight"/>.
	/// You can also use the implicit conversion operator.
	/// </summary>
	/// <param name="light">The object to wrap.</param>
	public SceneObject(DirectionalLight light) : this(ToStub(light), SceneObjectAdapterFunctionTable.ForDirectionalLight) { }
	/// <summary>
	/// Constructs a new <see cref="SceneObject"/> wrapping the given <see cref="Camera"/>.
	/// You can also use the implicit conversion operator.
	/// </summary>
	/// <param name="camera">The object to wrap.</param>
	public SceneObject(Camera camera) : this(ToStub(camera), SceneObjectAdapterFunctionTable.ForCamera) { }

	/// <summary>
	/// Wraps the given <see cref="ModelInstance"/> in a <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The object to wrap.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(ModelInstance operand) => new(operand);
	/// <summary>
	/// Wraps the given <see cref="ModelInstanceGroup"/> in a <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The object to wrap.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(ModelInstanceGroup operand) => new(operand);
	/// <summary>
	/// Wraps the given <see cref="MutableGridInstance"/> in a <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The object to wrap.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(MutableGridInstance operand) => new(operand);
	/// <summary>
	/// Wraps the given <see cref="QuadInstance"/> in a <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The object to wrap.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(QuadInstance operand) => new(operand);
	/// <summary>
	/// Wraps the given <see cref="CameraLockedQuadInstance"/> in a <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The object to wrap.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(CameraLockedQuadInstance operand) => new(operand);
	/// <summary>
	/// Wraps the given <see cref="TextInstance"/> in a <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The object to wrap.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(TextInstance operand) => new(operand);
	/// <summary>
	/// Wraps the given <see cref="CameraLockedTextInstance"/> in a <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The object to wrap.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(CameraLockedTextInstance operand) => new(operand);
	/// <summary>
	/// Wraps the given <see cref="PointLight"/> in a <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The object to wrap.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(PointLight operand) => new(operand);
	/// <summary>
	/// Wraps the given <see cref="SpotLight"/> in a <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The object to wrap.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(SpotLight operand) => new(operand);
	/// <summary>
	/// Wraps the given <see cref="DirectionalLight"/> in a <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The object to wrap.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(DirectionalLight operand) => new(operand);
	/// <summary>
	/// Wraps the given <see cref="Camera"/> in a <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The object to wrap.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(Camera operand) => new(operand);

	/// <summary>
	/// Returns the <see cref="ModelInstance"/> wrapped by the given <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The scene object to unwrap.</param>
	/// <exception cref="InvalidCastException">Thrown if <paramref name="operand"/> does not wrap a <see cref="ModelInstance"/> (i.e. its <see cref="Type"/> is one for which <see cref="SceneObjectTypeExtensions.IsStoredAsModelInstance"/> returns <see langword="true"/>).</exception>
	public static explicit operator ModelInstance(SceneObject operand) {
		if (operand.Type.IsStoredAsModelInstance()) return FastFromStub<ModelInstance>(operand.Stub);
		throw operand.CreateInvalidCastException(SceneObjectType.ModelInstance);
	}
	/// <summary>
	/// Returns the <see cref="ModelInstanceGroup"/> wrapped by the given <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The scene object to unwrap.</param>
	/// <exception cref="InvalidCastException">Thrown if <paramref name="operand"/> does not wrap a <see cref="ModelInstanceGroup"/> (i.e. its <see cref="Type"/> is <see cref="SceneObjectType.ModelInstanceGroup"/>).</exception>
	public static explicit operator ModelInstanceGroup(SceneObject operand) {
		operand.ThrowIfNotOfType(SceneObjectType.ModelInstanceGroup);
		return new(FastFromStub<ResourceGroup>(operand.Stub));
	}
	internal static ResourceGroup GetUnderlyingResourceGroupFromModelInstanceGroup(SceneObject o) {
		o.ThrowIfNotOfType(SceneObjectType.ModelInstanceGroup);
		return FastFromStub<ResourceGroup>(o.Stub);
	}
	/// <summary>
	/// Returns the <see cref="QuadInstance"/> wrapped by the given <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The scene object to unwrap.</param>
	/// <exception cref="InvalidCastException">Thrown if <paramref name="operand"/> does not wrap a <see cref="QuadInstance"/> (i.e. its <see cref="Type"/> is <see cref="SceneObjectType.QuadInstance"/>).</exception>
	public static explicit operator QuadInstance(SceneObject operand) {
		operand.ThrowIfNotOfType(SceneObjectType.QuadInstance);
		return new(FastFromStub<ModelInstance>(operand.Stub));
	}
	/// <summary>
	/// Returns the <see cref="TextInstance"/> wrapped by the given <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The scene object to unwrap.</param>
	/// <exception cref="InvalidCastException">Thrown if <paramref name="operand"/> does not wrap a <see cref="TextInstance"/> (i.e. its <see cref="Type"/> is <see cref="SceneObjectType.TextInstance"/>).</exception>
	public static explicit operator TextInstance(SceneObject operand) {
		operand.ThrowIfNotOfType(SceneObjectType.TextInstance);
		return new(FastFromStub<ModelInstance>(operand.Stub));
	}
	/// <summary>
	/// Returns the <see cref="PointLight"/> wrapped by the given <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The scene object to unwrap.</param>
	/// <exception cref="InvalidCastException">Thrown if <paramref name="operand"/> does not wrap a <see cref="PointLight"/> (i.e. its <see cref="Type"/> is <see cref="SceneObjectType.PointLight"/>).</exception>
	public static explicit operator PointLight(SceneObject operand) {
		operand.ThrowIfNotOfType(SceneObjectType.PointLight);
		return FastFromStub<PointLight>(operand.Stub);
	}
	/// <summary>
	/// Returns the <see cref="SpotLight"/> wrapped by the given <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The scene object to unwrap.</param>
	/// <exception cref="InvalidCastException">Thrown if <paramref name="operand"/> does not wrap a <see cref="SpotLight"/> (i.e. its <see cref="Type"/> is <see cref="SceneObjectType.SpotLight"/>).</exception>
	public static explicit operator SpotLight(SceneObject operand) {
		operand.ThrowIfNotOfType(SceneObjectType.SpotLight);
		return FastFromStub<SpotLight>(operand.Stub);
	}
	/// <summary>
	/// Returns the <see cref="DirectionalLight"/> wrapped by the given <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The scene object to unwrap.</param>
	/// <exception cref="InvalidCastException">Thrown if <paramref name="operand"/> does not wrap a <see cref="DirectionalLight"/> (i.e. its <see cref="Type"/> is <see cref="SceneObjectType.DirectionalLight"/>).</exception>
	public static explicit operator DirectionalLight(SceneObject operand) {
		operand.ThrowIfNotOfType(SceneObjectType.DirectionalLight);
		return FastFromStub<DirectionalLight>(operand.Stub);
	}
	/// <summary>
	/// Returns the <see cref="Camera"/> wrapped by the given <see cref="SceneObject"/>.
	/// </summary>
	/// <param name="operand">The scene object to unwrap.</param>
	/// <exception cref="InvalidCastException">Thrown if <paramref name="operand"/> does not wrap a <see cref="Camera"/> (i.e. its <see cref="Type"/> is <see cref="SceneObjectType.Camera"/>).</exception>
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

	/// <inheritdoc />
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetPosition(this);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetPosition(this, value);
	}
	/// <summary>
	/// Sets <see cref="Position"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="position">The new value for <see cref="Position"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveBy(Vect translation) => FunctionTable.MoveBy(this, translation);

	/// <inheritdoc />
	public Rotation Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetRotation(this);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetRotation(this, value);
	}
	/// <summary>
	/// Sets <see cref="Rotation"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="rotation">The new value for <see cref="Rotation"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotation(Rotation rotation) => Rotation = rotation;

	/// <inheritdoc />
	public Quaternion RotationQuaternion {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetRotationQuaternion(this);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetRotationQuaternion(this, value);
	}
	/// <summary>
	/// Sets <see cref="RotationQuaternion"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="rotationQuaternion">The new value for <see cref="RotationQuaternion"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotationQuaternion(Quaternion rotationQuaternion) => RotationQuaternion = rotationQuaternion;

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Rotation rotation) => FunctionTable.RotateByRotation(this, rotation);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Quaternion rotationQuaternion) => FunctionTable.RotateByQuaternion(this, rotationQuaternion);

	/// <inheritdoc />
	public Vect Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetScaling(this);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetScaling(this, value);
	}
	/// <summary>
	/// Sets <see cref="Scaling"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="scaling">The new value for <see cref="Scaling"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetScaling(Vect scaling) => Scaling = scaling;

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(float scalar) => FunctionTable.ScaleByScalar(this, scalar);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(Vect vect) => FunctionTable.ScaleByVect(this, vect);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(float scalar) => FunctionTable.AdjustScaleByScalar(this, scalar);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(Vect vect) => FunctionTable.AdjustScaleByVect(this, vect);

	/// <inheritdoc />
	public Transform Transform {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetTransform(this);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetTransform(this, value);
	}
	/// <summary>
	/// Sets <see cref="Transform"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="transform">The new value for <see cref="Transform"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTransform(Transform transform) => Transform = transform;

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Rotation rotation, Location pivotPoint) => FunctionTable.RotateByRotationAroundPivot(this, rotation, pivotPoint);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Quaternion rotationQuaternion, Location pivotPoint) => FunctionTable.RotateByQuaternionAroundPivot(this, rotationQuaternion, pivotPoint);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetMaterial(Material material) => FunctionTable.SetMaterial(this, material);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDefaultMaterialBaseColor(ColorVect baseColor) => FunctionTable.SetDefaultMaterialBaseColor(this, baseColor);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDefaultMaterialShadingStyle(DefaultMaterialShadingStyle style) => FunctionTable.SetDefaultMaterialShadingStyle(this, style);

	/// <inheritdoc />
	public Angle ColorHue {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetColorHue(this);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetColorHue(this, value);
	}
	/// <summary>
	/// Sets <see cref="ColorHue"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="hue">The new value for <see cref="ColorHue"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorHue(Angle hue) => ColorHue = hue;

	/// <inheritdoc />
	public float ColorSaturation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetColorSaturation(this);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetColorSaturation(this, value);
	}
	/// <summary>
	/// Sets <see cref="ColorSaturation"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="saturation">The new value for <see cref="ColorSaturation"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorSaturation(float saturation) => ColorSaturation = saturation;

	/// <inheritdoc />
	public float ColorLightness {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FunctionTable.GetColorLightness(this);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => FunctionTable.SetColorLightness(this, value);
	}
	/// <summary>
	/// Sets <see cref="ColorLightness"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="lightness">The new value for <see cref="ColorLightness"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorLightness(float lightness) => ColorLightness = lightness;

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustColorHueBy(Angle adjustment) => FunctionTable.AdjustColorHueBy(this, adjustment);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustColorSaturationBy(float adjustment) => FunctionTable.AdjustColorSaturationBy(this, adjustment);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustColorLightnessBy(float adjustment) => FunctionTable.AdjustColorLightnessBy(this, adjustment);

	/// <inheritdoc />
	public string GetNameAsNewStringObject() {
		if (Type == SceneObjectType.Unspecified) throw InvalidObjectException.InvalidDefault<ResourceStub>();
		return Stub.GetNameAsNewStringObject();
	}
	/// <inheritdoc />
	public int GetNameLength() {
		if (Type == SceneObjectType.Unspecified) throw InvalidObjectException.InvalidDefault<ResourceStub>();
		return Stub.GetNameLength();
	}
	/// <inheritdoc />
	public void CopyName(Span<char> destinationBuffer) {
		if (Type == SceneObjectType.Unspecified) throw InvalidObjectException.InvalidDefault<ResourceStub>();
		Stub.CopyName(destinationBuffer);
	}

	/// <summary>
	/// Disposes the object this wraps.
	/// </summary>
	/// <exception cref="InvalidObjectException">Thrown if this is a <c>default</c> <see cref="SceneObject"/>.</exception>
	public void DisposeUnderlyingObject() {
		if (Type == SceneObjectType.Unspecified) throw InvalidObjectException.InvalidDefault<ResourceStub>();
		Stub.Dispose();
	}

	/// <inheritdoc />
	public override string ToString() => $"Scene Object ({Type}) \"{Stub.GetNameAsNewStringObject()}\"";
}
