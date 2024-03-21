// Created on 2024-03-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

public readonly ref struct RefEnumerator<TOwner, TItem> where TOwner : IRefEnumerable<TItem> {
	public ref struct Enumerator {
		readonly ref TOwner _owner;
		readonly int _count;
		int _curIndex = -1;

		public Enumerator(ref TOwner owner) {
			_owner = ref owner;
			_count = owner.Count;
		}

		public TItem Current => _owner.ElementAt(_curIndex);

		public bool MoveNext() {
			_curIndex++;
			return _curIndex < _count;
		}
		public void Reset() => _curIndex = -1;
		public void Dispose() { /* no op */ }
	}

	readonly ref TOwner _owner;

	public int Count {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _owner.Count;
	}
	public TItem this[int index] {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _owner.ElementAt(index);
	}

	public RefEnumerator(ref TOwner owner) => _owner = ref owner;

	public Enumerator GetEnumerator() => new(ref _owner);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TItem ElementAt(int index) => _owner.ElementAt(index);

	public void CopyTo(Span<TItem> dest) {
		for (var i = 0; i < Count; ++i) dest[i] = this[i];
	}
	public bool TryCopyTo(Span<TItem> dest) {
		if (dest.Length < Count) return false;
		CopyTo(dest);
		return true;
	}
}