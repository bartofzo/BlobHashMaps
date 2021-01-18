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
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BlobHashMapsTest
{
    public abstract class GenericHashMapTest<TKey> : MonoBehaviour
        where TKey : struct, IEquatable<TKey>
    {
        private const int itemCount = 10000;
        private const int queryCount = 100000;
        private const int testCount = 25;

        protected Unity.Mathematics.Random random;

        private NativeHashMap<TKey, int> source;
        private BlobAssetReference<BlobHashMap<TKey, int>> blobMapRef;

        private NativeArray<TKey> allKeys;
        private NativeArray<TKey> allQueries;

        private NativeArray<int> nativeResult;
        private NativeArray<int> blobResult;

        // Start is called before the first frame update
        void Start()
        {
            // result storage
            nativeResult = new NativeArray<int>(1, Allocator.Persistent);
            blobResult = new NativeArray<int>(1, Allocator.Persistent);

            random = new Unity.Mathematics.Random((uint) System.DateTime.Now.Ticks);

            // generate all keys
            allKeys = new NativeArray<TKey>(itemCount, Allocator.Persistent);
            for (int i = 0; i < itemCount; i++)
            {
                allKeys[i] = GenerateKey(i);
            }

            // generate queries
            allQueries = new NativeArray<TKey>(queryCount, Allocator.Persistent);
            for (int i = 0; i < queryCount; i++)
            {
                allQueries[i] = GenerateQuery(i, itemCount > 0 ? allKeys[i % itemCount] : default);
            }

            // construct source
            source = new NativeHashMap<TKey, int>(16, Allocator.Persistent);
            for (int i = 0; i < itemCount; i++)
            {
                source.TryAdd(allKeys[i], itemCount - i);
            }


            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<BlobHashMap<TKey, int>>();
            builder.ConstructHashMap(ref root, ref source);


            blobMapRef = builder.CreateBlobAssetReference<BlobHashMap<TKey, int>>(Allocator.Persistent);


            Debug.Log("NativeHashMap Capacity: " + source.Capacity);
            Debug.Log("BlobHashMap Capacity ?");

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

            var job1 = new Job1()
            {
                map = source,
                queries = allQueries,
                result = nativeResult,
                testCount = testCount
            };

            var job2 = new Job2()
            {
                blob = blobMapRef,
                queries = allQueries,
                result = blobResult,
                testCount = testCount
            };


            sw1.Start();
            job1.Run();
            sw1.Stop();

            sw2.Start();
            job2.Run();
            sw2.Stop();

            Assert.AreEqual(nativeResult[0], blobResult[0], "Native and Blob sum should be the same!");

            Debug.Log("Native: " + sw1.ElapsedTicks + " ms: " + sw1.ElapsedMilliseconds + " avg: " +
                      ((float) sw1.ElapsedMilliseconds / testCount));
            Debug.Log("Blob: " + sw2.ElapsedTicks + " ms: " + sw2.ElapsedMilliseconds + " avg: " +
                      ((float) sw2.ElapsedMilliseconds / testCount));
        }

        protected abstract TKey GenerateKey(int i);
        protected abstract TKey GenerateQuery(int i, TKey keyAtWrappedIndex);

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
        struct Job1 : IJob
        {
            [ReadOnly] public NativeHashMap<TKey, int> map;
            [ReadOnly] public NativeArray<TKey> queries;
            public int testCount;
            public NativeArray<int> result;

            public void Execute()
            {
                int sum = 0;

                for (int c = 0; c < testCount; c++)
                {
                    for (int i = 0; i < queries.Length; i++)
                    {
                        if (map.TryGetValue(queries[i], out int value))
                            sum += value;
                    }
                }


                result[0] = sum;
            }
        }

        [BurstCompile]
        struct Job2 : IJob
        {
            [ReadOnly] public BlobAssetReference<BlobHashMap<TKey, int>> blob;
            [ReadOnly] public NativeArray<TKey> queries;
            public NativeArray<int> result;
            public int testCount;

            public void Execute()
            {
                int sum = 0;
                ref var map = ref blob.Value;

                for (int c = 0; c < testCount; c++)
                {
                    for (int i = 0; i < queries.Length; i++)
                    {
                        if (map.TryGetValue(queries[i], out int value))
                            sum += value;
                    }
                }

                result[0] = sum;
            }
        }
    }
}