# NativePhysicsBVH
A Bounding Volume Hierarchy with basic physics queries for Unity DOTS.

- Supported primitives: **Boxes** and **Spheres**
- Supported queries: **Raycasts** and **Distance queries**
- Supports basic **layers**

Even though there's reasonable test coverage, this shouldn't be used in a production environment yet and is likely to have critical bugs. Use with caution!

### Examples

The tests are a good place to see how the tree is being used. But here's some examples as well.

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
