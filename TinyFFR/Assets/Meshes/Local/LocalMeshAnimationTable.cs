// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Reflection.Metadata;
using System.Security;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering.Local.Sync;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

unsafe abstract class LocalMeshAnimationTable<TAnimData> where TAnimData : struct {
	static readonly ObjectPool<ArrayPoolBackedStringKeyMap<TAnimData>> _tableMapPool = new(&CreateNewTableMap);
	static ArrayPoolBackedStringKeyMap<TAnimData> CreateNewTableMap() => new();
	
	protected ArrayPoolBackedStringKeyMap<TAnimData> TableMap = null!;

	public void OnRented() {
		TableMap = _tableMapPool.Rent();
	}
	
	public void OnReturning() {
		TableMap.Clear();
		_tableMapPool.Return(TableMap);
	}
	
	public int Count => TableMap.Count;
	public TAnimData? FindByName(ReadOnlySpan<char> name) => TableMap.TryGetValue(name, out var animData) ? animData : null;
	public void AddAnimation(ReadOnlySpan<char> name, TAnimData animData) => TableMap.Add(name, animData);
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, TAnimData>.KeyEnumerator Keys => TableMap.Keys;
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, TAnimData>.ValueEnumerator Values => TableMap.Values;
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, TAnimData>.Enumerator GetEnumerator() => TableMap.GetEnumerator();
}

sealed class LocalMeshSkeletalAnimationTable : LocalMeshAnimationTable<SkeletalAnimationData> {
	
}

sealed class LocalMeshMorphingAnimationTable : LocalMeshAnimationTable<MorphingAnimationData> {
	
}