#if ENABLE_UNITY_COLLECTIONS_CHECKS
#define BLOBHASHMAP_SAFE
#endif

using System;
using System.Collections.Generic;
using BlobHashMaps.Data;
using Unity.Collections;
using Unity.Entities;

namespace BlobHashMaps
{
    /// <summary>
    /// A read only hashmap that can be used as a blob asset
    /// </summary>
    [MayOnlyLiveInBlobStorage]
    public struct BlobHashMap<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal BlobHashMapData<TKey, TValue> data;
        
        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                    return value;
            
#if BLOBHASHMAP_SAFE
                throw new KeyNotFoundException($"Key: {key} is not present in the BlobHashMap.");
#else
                 return default;
#endif
            }
        }

        public bool TryGetValue(TKey key, out TValue value) => data.TryGetFirstValue(key, out value, out _);
        public bool ContainsKey(TKey key) => TryGetValue(key, out _);
        public int Count => data.count[0];
        
        public NativeArray<TKey> GetKeyArray(Allocator allocator) => data.GetKeys(allocator);
        public NativeArray<TValue> GetValueArray(Allocator allocator) => data.GetValues(allocator);
    }
    
    public ref struct BlobBuilderHashMap<TKey, TValue> 
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal BlobBuilderHashMapData<TKey, TValue> data;

        internal BlobBuilderHashMap(int capacity, int bucketCapacityRatio, ref BlobBuilder blobBuilder, ref BlobHashMapData<TKey, TValue> data)
        {
            #if BLOBHASHMAP_SAFE
            if (capacity <= 0)
                throw new ArgumentException("Must be greater than zero", nameof(capacity));
            if (bucketCapacityRatio <= 0)
                throw new ArgumentException("Must be greater than zero", nameof(bucketCapacityRatio));
            #endif
            
            this.data = new BlobBuilderHashMapData<TKey, TValue>(capacity, bucketCapacityRatio, ref blobBuilder, ref data);
        }

        public void Add(TKey key, TValue item)
        {

#if BLOBHASHMAP_SAFE
            if (!data.TryAdd(key, item, false))
                throw new ArgumentException($"An item with key {key} already exists", nameof(key));
#else
            TryAdd(key, item);
#endif
        }

        public bool TryAdd(TKey key, TValue value) => data.TryAdd(key, value, false);
        public bool ContainsKey(TKey key) => data.ContainsKey(key);
        public int Capacity => data.keyCapacity;
        public int Count => data.Count;
    }
}