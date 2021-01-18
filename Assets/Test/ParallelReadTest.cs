using BlobHashMaps;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace BlobHashMapsTest
{
    public class ParallelReadTest : MonoBehaviour
    {
        private const int count = 1000;
        
        private BlobAssetReference<BlobHashMap<int, int>> blobRef;
        private NativeArray<int> results;
        private JobHandle jobHandle;
        private int expectedResult;
        
        private void Start()
        {
            results = new NativeArray<int>(count, Allocator.Persistent);
          
            
            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<BlobHashMap<int, int>>();
            var hashMapBuilder = builder.AllocateHashMap(ref root, count);
            for (int i = 0; i < count; i++)
            {
                hashMapBuilder.Add(i, i);
                expectedResult += i;
            }
           
            blobRef = builder.CreateBlobAssetReference<BlobHashMap<int, int>>(Allocator.Persistent);


            var job = new ReadJob()
            {
                count = count,
                blobRef = blobRef,
                results = results
            };

            jobHandle = job.Schedule(count, count / 10);
        }

        private void OnDestroy()
        {
            blobRef.Dispose();
            results.Dispose();
        }

        private void Update()
        {
            if (jobHandle.IsCompleted)
            {
                jobHandle.Complete();

                for (int i = 0; i < results.Length; i++)
                {
                    Assert.AreEqual(results[i], expectedResult);
                }
                
                Debug.Log(Time.frameCount + " OK!");
                Destroy(this);
            }
        }
    }
    
    [BurstCompile(CompileSynchronously = true)]
    struct ReadJob : IJobParallelFor
    {
        public int count;
        public BlobAssetReference<BlobHashMap<int, int>> blobRef;
        public NativeArray<int> results;

        public void Execute(int index)
        {
            ref var map = ref blobRef.Value;
            int sum = 0;
            for (int i = 0; i < count; i++)
            {
                if (map.TryGetValue(i, out int val))
                    sum += val;
            }

            results[index] = sum;
        }
    }
}