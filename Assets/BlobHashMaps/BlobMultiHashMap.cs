#if ENABLE_UNITY_COLLECTIONS_CHECKS
#define BLOBHASHMAP_SAFE
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using BlobHashMaps.Data;
using Unity.Collections;
using Unity.Entities;

namespace BlobHashMaps
{
    /// <summary>
    /// A read only multihashmap that can be used as a blob asset
    /// </summary>
    [MayOnlyLiveInBlobStorage]
    public struct BlobMultiHashMap<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal BlobHashMapData<TKey, TValue> data;
        
        public bool TryGetFirstValue(TKey key, out TValue value, out BlobMultiHashMapIterator<TKey> it) =>
            data.TryGetFirstValue(key, out value, out it);

        public bool TryGetNextValue(out TValue item, ref BlobMultiHashMapIterator<TKey> it) =>
            data.TryGetNextValue(out item, ref it);

        public int Count => data.count[0];
        public bool ContainsKey(TKey key) => data.TryGetFirstValue(key, out _, out _);

        public NativeArray<TKey> GetKeyArray(Allocator allocator) => data.GetKeys(allocator);
        public NativeArray<TValue> GetValueArray(Allocator allocator) => data.GetValues(allocator);
    }

    public ref struct BlobBuilderMultiHashMap<TKey, TValue> 
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal BlobBuilderHashMapData<TKey, TValue> data;

        internal BlobBuilderMultiHashMap(int capacity, int bucketCapacityRatio, ref BlobBuilder blobBuilder, ref BlobHashMapData<TKey, TValue> data)
        {
#if BLOBHASHMAP_SAFE
            if (capacity <= 0)
                throw new ArgumentException("Must be greater than zero", nameof(capacity));
            if (bucketCapacityRatio <= 0)
                throw new ArgumentException("Must be greater than zero", nameof(bucketCapacityRatio));
#endif
            
            this.data = new BlobBuilderHashMapData<TKey, TValue>(capacity, bucketCapacityRatio, ref blobBuilder, ref data);
        }

        public void Add(TKey key, TValue item) => data.TryAdd(key, item, true);
        public int Capacity => data.keyCapacity;
        public int Count => data.Count;
    }
    
    public struct BlobMultiHashMapIterator<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        internal TKey key;
        internal int nextIndex;
    }
}