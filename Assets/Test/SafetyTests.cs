#if ENABLE_UNITY_COLLECTIONS_CHECKS
#define BLOBHASHMAP_SAFE
#endif

using System;
using System.Collections.Generic;
using BlobHashMaps;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Assert = NUnit.Framework.Assert;
using Random = System.Random;

namespace BlobHashMapsTest
{
    public class SafetyTests : MonoBehaviour
    {
        void Start()
        {
            #if !BLOBHASHMAP_SAFE
            Debug.LogError("BLOBHASHMAP_SAFE needs to be defined for safety tests");
            return;
            #endif
            
            HashMapsFull();
            HashMapsZeroCapacity();
            HashMapsEmpty();
            HashMapExactlyFull();
            HashMapAddContainsKey();
            MultiHashMapAddDuplicateKey();
            HashMapKeyNotPresent();
            HashMapSumValues();
            MultiHashMapSumValues();
            HashmapCheckKeys();
            MultiHashmapCheckKeys();
          
            
            Debug.Log("All test passed!");
        }

        void HashMapsFull()
        {
            // Hashmap full check
            Assert.Throws<InvalidOperationException>(() =>
            {
                BlobBuilder builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<BlobHashMap<int2, int>>();
                var hashMapBuilder = builder.AllocateHashMap(ref root, 5);
                for (int i = 0; i < 10; i++)
                {
                    hashMapBuilder.Add(new int2(i, 0), 0);
                }
            });

            // Multihashmap full check
            Assert.Throws<InvalidOperationException>(() =>
            {
                BlobBuilder builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<BlobMultiHashMap<int2, int>>();
                var hashMapBuilder = builder.AllocateMultiHashMap(ref root, 5);
                for (int i = 0; i < 10; i++)
                {
                    hashMapBuilder.Add(new int2(i, 0), 0);
                }
            });
        }

        void HashMapsZeroCapacity()
        {
            // Capacity 0 hashmap
            Assert.Throws<ArgumentException>(() =>
            {
                BlobBuilder builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<BlobHashMap<int2, int>>();
                builder.AllocateHashMap(ref root, 0);
            });
            
            // Capacity 0 multihashmap
            Assert.Throws<ArgumentException>(() =>
            {
                BlobBuilder builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<BlobMultiHashMap<int2, int>>();
                builder.AllocateMultiHashMap(ref root, 0);
            });
        }

        void HashMapsEmpty()
        {
            // Zero elements with capacity > 0 hashmap read:
            Assert.DoesNotThrow(() =>
            {
                // zero elements with capacity
                BlobBuilder builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<BlobHashMap<int2, int>>();
                var hashMapBuilder = builder.AllocateHashMap(ref root, 10);
                var blobRef = builder.CreateBlobAssetReference<BlobHashMap<int2, int>>(Allocator.Persistent);
                if (blobRef.Value.TryGetValue(int2.zero, out _))
                    throw new Exception("true was returned by TryGetValue while the hashmap is empty");
                
                blobRef.Dispose();
            });
            
            // Zero elements with capacity > 0 multihashmap read:
            Assert.DoesNotThrow(() =>
            {
                // zero elements with capacity
                BlobBuilder builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<BlobMultiHashMap<int2, int>>();
                var hashMapBuilder = builder.AllocateMultiHashMap(ref root, 10);
                var blobRef = builder.CreateBlobAssetReference<BlobMultiHashMap<int2, int>>(Allocator.Persistent);
                if (blobRef.Value.TryGetFirstValue(int2.zero, out _, out _))
                    throw new Exception("true was returned by TryGetValue while the multihashmap is empty");
                
                blobRef.Dispose();
            });
        }

        void HashMapAddContainsKey()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                BlobBuilder builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<BlobHashMap<int, int>>();
                var hashMapBuilder = builder.AllocateHashMap(ref root, 10);
                hashMapBuilder.Add(420, 69);
                hashMapBuilder.Add(420, 69);
            });
        }

        void HashMapExactlyFull()
        {
            Assert.DoesNotThrow(() =>
            {
                int size = 10;
                
                // zero elements with capacity
                BlobBuilder builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<BlobHashMap<int, int>>();
                var hashMapBuilder = builder.AllocateHashMap(ref root, size);
                
                for (int i = 0; i < size; i++)
                    hashMapBuilder.Add(i, i);

                var blobRef = builder.CreateBlobAssetReference<BlobHashMap<int, int>>(Allocator.Persistent);

                for (int i = 0; i < size; i++)
                {
                    int value = blobRef.Value[i];
                    Assert.IsTrue(value == i);
                }

                blobRef.Dispose();
            });
        }
        
        
        void MultiHashMapAddDuplicateKey()
        {
            Assert.DoesNotThrow(() =>
            {
                BlobBuilder builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<BlobMultiHashMap<int, int>>();
                var hashMapBuilder = builder.AllocateMultiHashMap(ref root, 10);
                
                hashMapBuilder.Add(420, 1);
                hashMapBuilder.Add(420, 2);
                hashMapBuilder.Add(420, 3);
                hashMapBuilder.Add(69, 1);
                hashMapBuilder.Add(69, 2);
                hashMapBuilder.Add(69, 3);
            });
        }

        void HashMapKeyNotPresent()
        {
            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<BlobHashMap<int, int>>();
            var hashMapBuilder = builder.AllocateHashMap(ref root, 10);
            hashMapBuilder.Add(420, 69);
            var blobRef = builder.CreateBlobAssetReference<BlobHashMap<int, int>>(Allocator.Persistent);
            int dummy = 0;
            Assert.Throws<KeyNotFoundException>(() => dummy = blobRef.Value[0] + blobRef.Value[1]);
        }

        void HashMapSumValues()
        {
            int size = 42;
            
            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<BlobHashMap<int, int>>();
            var hashMapBuilder = builder.AllocateHashMap(ref root, size);

            int expectedSum = 0;
            for (int i = 0; i < size; i++)
            {
                hashMapBuilder.Add(i, i);
                expectedSum += i;
            }
                
            var blobRef = builder.CreateBlobAssetReference<BlobHashMap<int, int>>(Allocator.Persistent);

            int sum = 0;
            for (int i = 0; i < size; i++)
            {
                int value = blobRef.Value[i];
                sum += value;
            }

            Assert.IsTrue(sum == expectedSum);
        }
        
        void MultiHashMapSumValues()
        {
            int size = 100;
            
            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<BlobMultiHashMap<int, int>>();
            var hashMapBuilder = builder.AllocateMultiHashMap(ref root, size);
            
            int expectedSum = 0;
            for (int i = 0; i < size; i++)
            {
                // test multiple same keys too
                hashMapBuilder.Add(i / 5, i);
                expectedSum += i;
            }
                
            var blobRef = builder.CreateBlobAssetReference<BlobMultiHashMap<int, int>>(Allocator.Persistent);

            int sum = 0;
            for (int i = 0; i < size; i++)
            {
                if (blobRef.Value.TryGetFirstValue(i, out int value, out var it))
                {
                    do
                    {
                        sum += value;
                    } while (blobRef.Value.TryGetNextValue(out value, ref it));
                }
            }
          
            Assert.IsTrue(sum == expectedSum);
        }

        void HashmapCheckKeys()
        {
            int size = 10;
            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<BlobHashMap<int, int>>();
            var hashMapBuilder = builder.AllocateHashMap(ref root, size * 2);

            int expectedSum = 0;
            for (int i = 0; i < size; i++)
            {
                hashMapBuilder.TryAdd(i, i);
                expectedSum += i;
            }
                
            var blobRef = builder.CreateBlobAssetReference<BlobHashMap<int, int>>(Allocator.Persistent);

            var keys = blobRef.Value.GetKeyArray(Allocator.Temp);
            var values = blobRef.Value.GetValueArray(Allocator.Temp);

            int sum = 0;
            for (int i = 0; i < keys.Length; i++)
            {
                UnityEngine.Assertions.Assert.AreEqual(keys[i], values[i]);
                sum += values[i];
            }
            
            Debug.Log(blobRef.Value.Count);
            
            UnityEngine.Assertions.Assert.AreEqual(expectedSum, sum);
        }
        
        void MultiHashmapCheckKeys()
        {
            int size = 10;
            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<BlobMultiHashMap<int, int>>();
            var hashMapBuilder = builder.AllocateMultiHashMap(ref root, size * 2); // test capacity > items were adding

            int expectedSum = 0;
            for (int i = 0; i < size; i++)
            {
                hashMapBuilder.Add(0, i);
                
                expectedSum += i;
            }
                
            var blobRef = builder.CreateBlobAssetReference<BlobMultiHashMap<int, int>>(Allocator.Persistent);

            var keys = blobRef.Value.GetKeyArray(Allocator.Temp);
            var values = blobRef.Value.GetValueArray(Allocator.Temp);

//            Unity.Assertions.Assert.IsTrue(keys.Length == 1);
            UnityEngine.Assertions.Assert.IsTrue(values.Length == size);
            int sum = 0;
            for (int i = 0; i < values.Length; i++)
            {
                Debug.Log(keys[i] + " - " + values[i]);
                
                sum += values[i];
            }
            
            UnityEngine.Assertions.Assert.AreEqual(expectedSum, sum);
        }
        
    }
}