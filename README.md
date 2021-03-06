# NativePhysicsBVH
A Bounding Volume Hierarchy with basic physics queries for Unity DOTS.

- Supported primitives: **Boxes** and **Spheres**
- Supported queries: **Raycasts** and **Distance queries**
- Supports basic **layers**

Even though there's reasonable test coverage, this shouldn't be used in a production environment yet and is likely to have critical bugs. Use with caution!

### Performance
With a world of 1500 bodies, the insertion takes ~2ms and 10k raycasts take ~8ms. A similar setup with Unity.Physics takes ~0.2ms for the insertion and ~17ms for the raycasts. The time difference in insertion is largely because of the different construction of the world. In this project the BVH insertion is done with an "Incremental" approach, Unity.Physics seemingly "Top Down".

This means that this project is generally better for many static objects and not tens of thousands of moving objects. However, thousends of moving moving objects should still be fine, especially when increasing the bounds of objects a bit so the tree doesn't have to update constantly (see `BVHWorld` as an example for this).

Tests are done with an Intel i7-10750H CPU @ 2.60GHz.

### Examples

The tests are a good place to see how to use the BVH. But here's some examples as well.

#### Insertion

First you have to create the tree:
```
var tree = new NativeBVHTree(64, Allocator.Persistent);  
```
And then you can insert a collider like this:

```
tree.InsertLeaf(BoxCollider.Create(new float3(1, 1, 1), new float3(1, 1, 1)));  
```

Or alternatively, in case you have a transform that has a position and rotation, insertion can be done like this:

```
var leaf = new Leaf {  
    collider = BoxCollider.Create(new float3(0, 0, 0), new float3(1, 1, 1)),  
    transform = new RigidTransform {  
	    pos = new float3(1, 1, 1),  
	    rot = quaternion.identity  
	}  
});
var leafIndex = tree.InsertLeaf(leaf);
```

And in case the transform changed, the leaf can be updated:

```
tree.Reinsert(leafIndex , leaf );
```

#### Raycast

```
// Create tree
var tree = new NativeBVHTree(64, Allocator.Persistent);  

// Insert a leaf
tree.InsertLeaf(BoxCollider.Create(new float3(1, 1, 1), new float3(1, 1, 1)));  
  
// Cast ray
var rayResults = new NativeList<int>(64, Allocator.Temp);  
var ray = new NativeBVHTree.Ray {  
    origin = new float3(-1, 1, 0),  
    direction = new float3(10, 0, 10),  
    maxDistance = 20  
};  
tree.RaycastQuery(ray, rayResults);
```
Note that in order to to get the performance benefits of Burst, insertion and querying usually should be done in batches in jobs.

#### Updating moving objects

There's a basic wrapper "world" around the BVH in the project to show how to update a BVH with (many) moving objects. It should be seen as an example and starting point. Here's an example on how to use it:

```
// Create the world (it will create a BVH)
var world = new BVHTreeWorld(64, Allocator.Persistent);

// Add a collider
int index = world.Add(BoxCollider.Create(0, new float3(0, 0, 0)));  

// Update its position (this doesn't update the BVH yet, can be done many times in a frame)
world.UpdateTransform(index, new RigidTransform {pos = new float3(1, 1, 1)});

// And update the BVH (once per frame)
world.Update();
```
### Debugging

There's a debug scene, select the debug drawer in it to see the results of the last ran test.

![](https://i.imgur.com/4R6ygoZ.png)


### To do
- Write more tests for edge-cases (such as thousands of objects at the same position)
- Ensure thread safety by adding checks, there are none currently
- Support more primitives such as cylinders and capsules 
- Have more queries and expand existing queries to return more hit data
- Optimize the Distance query with SIMD operations
- BVHWorld: Increase bounds of moving objects based on collider size and tranform velocity
- BVHWorld: Separate dynamic and static bodies
- Improve the performance comparison with Unity.Physics
