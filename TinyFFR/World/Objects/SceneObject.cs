// Created on 2026-09-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using static Egodystonic.TinyFFR.World.SceneObjectTypeExtensions;
using static Egodystonic.TinyFFR.Resources.ResourceUtils;

namespace Egodystonic.TinyFFR.World;

public enum SceneObjectType {
	Unspecified = 0,
	ModelInstance = (TransformedFlag | MaterialReceivingFlag) + 1,
	ModelInstanceGroup = (TransformedFlag | MaterialReceivingFlag) + 2,
	MutableGridInstance = (TransformedFlag | MaterialReceivingFlag) + 3,
	QuadInstance = (TransformedFlag | MaterialReceivingFlag) + 4,
	CameraLockedQuadInstance = (TransformedFlag | MaterialReceivingFlag) + 5,
	TextInstance = (TransformedFlag) + 6,
	CameraLockedTextInstance = (TransformedFlag) + 7,
	PointLight = (PositionedFlag | ColoredFlag) + 8,
	SpotLight = (PositionedFlag | OrientedFlag | ColoredFlag) + 9,
	DirectionalLight = (OrientedFlag | ColoredFlag) + 10
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
	}
}

public interface ISceneObject {
	static abstract SceneObjectType SceneObjectType { get; }
}

internal unsafe sealed class SceneObjectAdapterFunctionTable {
	public SceneObjectType SceneObjectType { get; }
	public delegate* managed<ResourceStub, Material, void> SetMaterial { get; }

	public SceneObjectAdapterFunctionTable(SceneObjectType sceneObjectType, delegate*<ResourceStub, Material, void> setMaterial) {
		SceneObjectType = sceneObjectType;
		SetMaterial = setMaterial;
	}
	
	public static SceneObjectAdapterFunctionTable ForModelInstance { get; } = CreateForModelInstance();
	static SceneObjectAdapterFunctionTable CreateForModelInstance() {
		static void SetMaterial(ResourceStub stub, Material material) => FastFromStub<ModelInstance>(stub).SetMaterial(material);
		
		return new(
			SceneObjectType.ModelInstance,
			&SetMaterial
		);
	}
}

public readonly unsafe record struct SceneObject : ITransformedSceneObject, IColoredSceneObject, IMaterialReceivingSceneObject {
	internal ResourceStub Stub { get; }
	internal SceneObjectAdapterFunctionTable FunctionTable { get; }
	
	public SceneObjectType Type => FunctionTable.SceneObjectType;
	static SceneObjectType ISceneObject.SceneObjectType { get; } = SceneObjectType.Unspecified;

	internal SceneObject(ResourceStub stub, SceneObjectAdapterFunctionTable functionTable) {
		Stub = stub;
		FunctionTable = functionTable;
	}
	public SceneObject(ModelInstance mi) : this(ToStub(mi), SceneObjectAdapterFunctionTable.ForModelInstance) { }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator SceneObject(ModelInstance operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator ModelInstance(SceneObject operand) => FromStub<ModelInstance>(operand.Stub);

	public void SetMaterial(Material material) => FunctionTable.SetMaterial(Stub, material);
}