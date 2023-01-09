# BlobHashMaps

From: https://docs.unity3d.com/Packages/com.unity.entities@0.3/api/Unity.Entities.BlobBuilder.html

A blob asset is an immutable data structure stored in unmanaged memory. 
Blob assets can contain primitive types, strings, structs, arrays, and arrays of arrays. Arrays and structs must only contain blittable types.

This project provides read only hashmap structures that store their data in BlobArrays and is fully compatible with Unity's ECS and blob assets.
There is no unsafe code and it only uses Unity's public API to minimze the chances of this breaking in any future Unity updates.

## Important note:
Unfortately, since Entities 0.17, you'll get a ConstructBlobWithRefTypeViolation when trying to use this.
The generic type parameters are (wrongly) interpreted as a class. Unity has not provided a fix for this yet. 
You can circumvent this by commenting out line 149 in BlobAssetSafetyVerifier.cs and moving that entire package inside of your project.

See this post:
<a href="https://forum.unity.com/threads/entities-0-17-changelog.1020202/page-3#post-6791726">Entities 0.17 changelog</a>


### Use case
Blob(Multi)HashMap can be stored as a BlobAssetReference on an ECS component. 
This means individual entities can have access to unique hashmaps in a single job. Which is not possible using a Native(Multi)HashMap.

### Performance
Implementation wise the blob hashmaps are very similar to NativeHashMap, some optimizations could be made because they are read only 
and my tests have shown that they perform about 20-30% faster than NativeHashMap and NativeMultiHashmap.

### Usage
Copy the entire contents of Assets/BlobHashMaps into your project.

Blob(Multi)HashMap follows the same pattern as allocating a BlobArray on a BlobAsset. 
Additionally, it can be constructed by passing in a source NativeHashMap, NativeMultiHashMap or a Dictionary. 
Note: Constructing it from a dictionary is not supported in a burst accellerated job.

Example code:
```csharp
// construct source
source = new NativeHashMap<int3, float>(64, Allocator.Temp);
// ...add values to the source hashmap
            
// Create a builder
BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            
// Our blob asset will just be the hashmap itself, but it can also be a member of a larger struct
ref var root = ref builder.ConstructRoot<BlobHashMap<int3, float>>();
            
// ConstructHashMap is an extension method on BlobBuilder
// we pass in the source hashmap to copy
builder.ConstructHashMap(ref root, ref source);
            
// create our blob asset reference, this can be a member of IComponentData
var blobMapRef = builder.CreateBlobAssetReference<BlobHashMap<int3, float>>(Allocator.Persistent);
```

### Advanced usage
If you need to convert between types or are constructing your blob asset from a different source, Allocate(Multi)HashMap can also
return a BlobBuilder(Multi)HashMap which can be temporarily used to add items manually.

```csharp
// Allocate a builder
BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            
// Our blob asset will just be the hashmap itself, but it can also be a member of a larger struct
ref var root = ref builder.ConstructRoot<BlobMultiHashMap<FixedString32, float>>();
            
// when using a manual builder, we must give it a capacity (ideally the number of items we're going to add)
// resizing the hashmap is not possible
var multiHashMapBuilder = builder.AllocateMultiHashMap(ref root, 4);
            
// add some items
multiHashMapBuilder.Add(new FixedString32("a"), 1f);
multiHashMapBuilder.Add(new FixedString32("a"), 2f);
multiHashMapBuilder.Add(new FixedString32("b"), 42f);
multiHashMapBuilder.Add(new FixedString32("b"), 69f);

// create our blob asset reference, this can be a member of IComponentData
var blobMapRef = builder.CreateBlobAssetReference<BlobMultiHashMap<FixedString32, float>>(Allocator.Persistent);
```

### Limitations
- Blob(Multi)HashMaps are read only after creation
- Creation is limited to adding items only
- IEnumerable is not implemented, this would require unsafe code. 