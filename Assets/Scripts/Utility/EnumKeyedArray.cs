using System;

using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace SplitRun.Utility
{
    public static class EnumKeyedArray
    {
        public const string k_ValuesField = "_values";
    }

    // The array index IS the enum value, so a slot can never be missing, duplicated, or mislabelled.
    // Requires the enum to declare explicit, contiguous, int-backed values starting at 0.
    [Serializable]
    public class EnumKeyedArray<TEnum, TValue> : ISerializationCallbackReceiver
        where TEnum : struct, Enum
    {
        private static readonly TEnum[] s_keys = (TEnum[])Enum.GetValues(typeof(TEnum));

        [SerializeField] private TValue[] _values = new TValue[s_keys.Length];

        public int Length => s_keys.Length;

        public TValue this[TEnum key] => _values[UnsafeUtility.EnumToInt(key)];

        public TValue this[int index] => _values[index];

        public void OnBeforeSerialize() => PinLength();

        public void OnAfterDeserialize() => PinLength();

        public Enumerator GetEnumerator() => new Enumerator(_values);

        private void PinLength()
        {
            if (_values == null)
            {
                _values = new TValue[s_keys.Length];
                return;
            }

            if (_values.Length != s_keys.Length)
                Array.Resize(ref _values, s_keys.Length);
        }

        // A struct enumerator keeps foreach allocation-free on the spawn and audio paths.
        public struct Enumerator
        {
            private readonly TValue[] _values;

            private int _index;

            public Enumerator(TValue[] values)
            {
                _values = values;
                _index  = -1;
            }

            public (TEnum Key, TValue Value) Current => (s_keys[_index], _values[_index]);

            public bool MoveNext() => ++_index < _values.Length;
        }
    }
}
