using Unity.Mathematics;

namespace BlobHashMapsTest
{
    public class CapacityTestImpl : CapacityTest<int3>
    {
        protected override int3 GetRandomKey(Random random)
        {
            return random.NextInt3(new int3(-1000, -1000, -1000), new int3(1000, 1000, 1000));
        }
    }
}