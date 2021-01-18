using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using BlobHashMaps;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BlobHashMapsTest
{
    public class MultiHashMapTest : MonoBehaviour
    {
        private const int itemCount = 100000;
        protected Unity.Mathematics.Random random;

        private NativeMultiHashMap<int, int> source;
        private BlobAssetReference<BlobMultiHashMap<int, int>> blobMapRef;
    
        private NativeArray<int> allKeys;
        private NativeArray<int> allQueries;
    
        private NativeArray<int> nativeResult;
        private NativeArray<int> blobResult;

        // Start is called before the first frame update
        void Start()
        {
            nativeResult = new NativeArray<int>(1, Allocator.Persistent);
            blobResult = new NativeArray<int>(1, Allocator.Persistent);
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        
            // generate all keys
            allKeys = new NativeArray<int>(itemCount, Allocator.Persistent);
            allQueries = new NativeArray<int>(itemCount * 2, Allocator.Persistent);
            for (int i = 0; i < itemCount; i++)
            {
                allKeys[i] = random.NextInt(0, 50); // generate duplicate keys
            
                // 50/50 random queries and certainly existing keys
                allQueries[i * 2] = allKeys[i];
                allQueries[i * 2 + 1] = random.NextInt(-1000, 1000);
            }
        
            // construct source
            source = new NativeMultiHashMap<int, int>(allKeys.Length, Allocator.Persistent);
            for (int i = 0; i < itemCount; i++)
            {
                source.Add(allKeys[i], 1);
            }
        

            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<BlobMultiHashMap<int, int>>();
            builder.ConstructMultiHashMap(ref root, ref source);
            blobMapRef = builder.CreateBlobAssetReference<BlobMultiHashMap<int, int>>(Allocator.Persistent);
        
            //Debug.Log("NativeMultiHashMap Capacity: " + source.Capacity);
            //Debug.Log("BlobMultiHashMap Capacity: " + blobMapRef.Value.Capacity);

            StartCoroutine(TestInterval());
        }

        IEnumerator TestInterval()
        {
            while (true)
            {
                DoTest();
                yield return new WaitForSeconds(1);
            }
        }

        void DoTest()
        {
        
            Stopwatch sw1 = new Stopwatch();
            Stopwatch sw2 = new Stopwatch();

            var nativeJob = new NativeJob()
            {
                map = source,
                queries = allQueries,
                result = nativeResult
            };
        
            var blobJob = new BlobJob()
            {
                blobRef = blobMapRef,
                queries = allQueries,
                result = blobResult
            };
       
       
            sw1.Start();
            nativeJob.Run();
            sw1.Stop();

            sw2.Start();
            blobJob.Run();
            sw2.Stop();

            Assert.AreEqual(nativeResult[0], blobResult[0]);

            Debug.Log($"Native: {sw1.ElapsedTicks} ({sw1.ElapsedMilliseconds}ms) " +
                      $"Blob: {sw2.ElapsedTicks} ({sw2.ElapsedMilliseconds}ms) ");
        }

        private void OnDestroy()
        {
            allKeys.Dispose();
            allQueries.Dispose();
        
            blobMapRef.Dispose();

            nativeResult.Dispose();
            blobResult.Dispose();

            source.Dispose();
        }


        [BurstCompile]
        struct NativeJob : IJob
        {
            [ReadOnly] public NativeMultiHashMap<int, int> map;
            [ReadOnly] public NativeArray<int> queries;
            public NativeArray<int> result;

            public void Execute()
            {
                int sum = 0;

                for (int i = 0; i < queries.Length; i++)
                {
                    if (map.TryGetFirstValue(i, out int value, out var it))
                    {
                        do
                        {
                            sum += value;
                        } while (map.TryGetNextValue(out value, ref it));
                    }
                }
            
                result[0] = sum;
            }
        }
    
        [BurstCompile]
        struct BlobJob : IJob
        {
            [ReadOnly] public BlobAssetReference<BlobMultiHashMap<int, int>> blobRef;
            [ReadOnly] public NativeArray<int> queries;
            public NativeArray<int> result;

            public void Execute()
            {
                int sum = 0;
                ref var map = ref blobRef.Value;
                for (int i = 0; i < queries.Length; i++)
                {
                    if (map.TryGetFirstValue(i, out int value, out var it))
                    {
                        do
                        {
                            sum += value;
                        } while (map.TryGetNextValue(out value, ref it));
                    }
                }
            
                result[0] = sum;
            }
        }
    }
}