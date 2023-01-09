using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using BlobHashMaps;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace BlobHashMapsTest
{
    /// <summary>
    /// Benchmark to test blob hashmap's performance against nativehashmap using different bucket capacity ratios
    /// </summary>
    public abstract class CapacityTest<TKey> : MonoBehaviour
        where TKey : unmanaged, IEquatable<TKey>
    {
        private const int randomQueryRatio = 2;

        private static readonly int[] Capacities = new[] {1024, 8192, 16383, 16384, 32758, 65536};
        private static readonly int[] Ratios = new[] { 2, 3, 4 };
        
        private NativeParallelHashMap<TKey, int>[] hashmaps;
        private BlobAssetReference<BlobHashMap<TKey, int>>[] blobHashmaps;
        private NativeArray<TKey>[] keys;
        private NativeArray<int> hashMapResult;
        private NativeArray<int> blobHashMapResult;

        private long[,] nativeTicks;
        private long[,] blobTicks;
        private bool allocated;
        private Unity.Mathematics.Random random;

        protected abstract TKey GetRandomKey(Unity.Mathematics.Random random); //  random.NextInt2(new int2(-1000, -1000), new int2(1000, 1000));

        private void Start()
        {
            random = new Unity.Mathematics.Random(69);
            
            blobTicks = new long[Ratios.Length, Capacities.Length];
            nativeTicks = new long[Ratios.Length, Capacities.Length];

            TestRecursive(0);
        }
        
        private void Fill(NativeArray<TKey> queries, NativeParallelHashMap<TKey, int> hashmap, ref BlobBuilderHashMap<TKey, int> blobBuilderHashMap, 
            int amt)
        {
            for (int i = 0; i < amt; i++)
            {
                TKey key = GetRandomKey(random);
                int randomValue = random.NextInt(1000);

                hashmap.TryAdd(key, randomValue);
                blobBuilderHashMap.TryAdd(key, randomValue);

                // 50/50, query random and existing keys
                queries[i * 2] = GetRandomKey(random);
                queries[i * 2 + 1] = key;
            }
        }
 
        private void TestRecursive(int ratioIndex)
        {
            if (ratioIndex >= Ratios.Length)
            {
                PrintFinalResults();
                return;
            }

            int ratio = Ratios[ratioIndex];
            Allocate(ratio);
            StartCoroutine(DoTest(ratioIndex, () =>
            {
                
                Deallocate();
                TestRecursive(ratioIndex + 1);
                
            }));  
        }

        private void Allocate(int capacityFactor)
        {
            if (hashmaps == null)
                hashmaps = new NativeParallelHashMap<TKey, int>[Capacities.Length];
            if (blobHashmaps == null)
                blobHashmaps = new BlobAssetReference<BlobHashMap<TKey, int>>[Capacities.Length];
            if (keys == null)
                keys = new NativeArray<TKey>[Capacities.Length];
            
            hashMapResult = new NativeArray<int>(1, Allocator.Persistent);
            blobHashMapResult = new NativeArray<int>(1, Allocator.Persistent);
            
            for (int i = 0; i < Capacities.Length; i++)
            {
                int capacity = Capacities[i];
                hashmaps[i] = new NativeParallelHashMap<TKey, int>(capacity, Allocator.Persistent);
                
                BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp);
                ref var root = ref blobBuilder.ConstructRoot<BlobHashMap<TKey, int>>();
                var blobBuilderHashmap = blobBuilder.AllocateHashMap(ref root, capacity, capacityFactor);
                
                
                keys[i] = new NativeArray<TKey>(capacity * 2, Allocator.Persistent);
                Fill(keys[i], hashmaps[i], ref blobBuilderHashmap, capacity);
                
                // create the blob ref
                blobHashmaps[i] = blobBuilder.CreateBlobAssetReference<BlobHashMap<TKey, int>>(Allocator.Persistent);
            }

            allocated = true;
        }

        IEnumerator DoTest(int currentRatioIndex, Action callback)
        {
            var sw1 = new Stopwatch();
            var sw2 = new Stopwatch();
            
            for (int i = 0; i < Capacities.Length; i++)
            {
                
                //Debug.Log($"Performing test {i}, capacity: ({hashmaps[i].Capacity}, {blobHashmaps[i].Value.Capacity})");
                yield return null;

                var job1 = new HashMapJob()
                {
                    hashmap = hashmaps[i],
                    queries = keys[i],
                    result = hashMapResult
                };
                
                var job2 = new BlobHashMapJob()
                {
                    blobHashMapRef = blobHashmaps[i],
                    queries = keys[i],
                    result = blobHashMapResult
                };
                
                sw1.Restart();
                job1.Run();
                sw1.Stop();
                
                sw2.Restart();
                job2.Run();
                sw2.Stop();
                
                Assert.AreEqual(hashMapResult[0], blobHashMapResult[0]);

                // native ticks should be the same for each ratio since it's not variable
                nativeTicks[currentRatioIndex, i] = sw1.ElapsedTicks;
                // store the time the blob hashmap took
                blobTicks[currentRatioIndex, i] = sw2.ElapsedTicks;

                float ratio = (float) sw2.ElapsedTicks / sw1.ElapsedTicks;

                Debug.Log($"Native: {sw1.ElapsedTicks} ({sw1.ElapsedMilliseconds}ms) " +
                          $"Blob: {sw2.ElapsedTicks} ({sw2.ElapsedMilliseconds}ms) " +
                          $"Ratio: {Mathf.FloorToInt(ratio * 100)}%");
            }

            yield return null;
            callback();
        }
        
        private void PrintFinalResults()
        {
            StringBuilder sb = new StringBuilder();
            
            // skip index zero, it's base ticks
            for (int f = 0; f < Ratios.Length ; f++)
            {
                int r = Ratios[f];
                
                sb.Clear();
                sb.AppendLine($"Ratio: {r}: ");
                
                for (int i = 0; i < Capacities.Length; i++)
                {
                    long baseTicks = blobTicks[0, i];
                    long ticksAt = blobTicks[f, i];
                    long nativeTicksAt = nativeTicks[f, i];
                    
                    float ratio = (float) ticksAt / baseTicks;
                    float ratioToNative = (float) ticksAt / nativeTicksAt;
                    
                    sb.AppendLine($"Capacity: {Capacities[i]}, ticks: {ticksAt}, " +
                                  $"took {Mathf.RoundToInt(ratio * 100)}% of base time and" +
                                  $" {Mathf.RoundToInt(ratioToNative * 100)}% of native time");
                }

                Debug.Log(sb.ToString());
            }
        }
        
        private void Deallocate()
        {
            for (int i = 0; i < Capacities.Length; i++)
            {
                hashmaps[i].Dispose();
                blobHashmaps[i].Dispose();
                keys[i].Dispose();
            }
            
            hashMapResult.Dispose();
            blobHashMapResult.Dispose();
            allocated = false;
        }

        private void OnDestroy()
        {
            if (allocated)
                Deallocate();
        }

        [BurstCompile]
        public struct HashMapJob : IJob
        {
            [ReadOnly] public NativeArray<TKey> queries;
            [ReadOnly] public NativeParallelHashMap<TKey, int> hashmap;
            public NativeArray<int> result;

            public void Execute()
            {
                int sum = 0;
                for (int i = 0; i < queries.Length; i++)
                {
                    if (hashmap.TryGetValue(queries[i], out int value))
                        sum += value;
                }

                result[0] = sum;
            }
        }
        
        [BurstCompile]
        public struct BlobHashMapJob : IJob
        {
            [ReadOnly] public NativeArray<TKey> queries;
            [ReadOnly] public BlobAssetReference<BlobHashMap<TKey, int>> blobHashMapRef;
            public NativeArray<int> result;

            public void Execute()
            {
                int sum = 0;
                ref var hashmap = ref blobHashMapRef.Value;
                
                for (int i = 0; i < queries.Length; i++)
                {
                    if (hashmap.TryGetValue(queries[i], out int value))
                        sum += value;
                }

                result[0] = sum;
            }
        }
    }
}