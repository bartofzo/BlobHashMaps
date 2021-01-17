using Unity.Mathematics;

namespace BlobHashMapsTest
{
    public class Int2HashMapTest : GenericHashMapTest<int2>
    {
        protected override int2 GenerateKey(int i)
        {
            return random.NextInt2(new int2(-1000, -1000), new int2(1000, 1000));
        }

        protected override int2 GenerateQuery(int i, int2 keyAtWrappedIndex)
        {
            // half random queries, half existing
            if (i % 2 == 0)
                return keyAtWrappedIndex;

            return random.NextInt2(new int2(-1000, -1000), new int2(1000, 1000));
        }
    }
}