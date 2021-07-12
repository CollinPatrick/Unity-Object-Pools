# Unity Object Pools

#### **This code has not been used in a production environment. Use at your own discretion.**

- This repo inclues my UniversalMonobehavior script which allows non-Monobehaviors to utilize aspects of a Monobehavior such as Update and Coroutines with an easy to access singleton instance.

## ObjectPool, GameObjectPool, & ComponentPool
**ObjectPool**, **GameObjectPool**, and **ComponentPool** are serialized classes that can be exposed in the Unity editor.

The **GameObjectPool** class is a derived class of ObjectPool<T> which is used to pool Unity GameObjects. This class automatically handles the instantiation and destruction of GameObjects. 
<details>
  <summary>Constructors</summary>
  
  
  #### Generic
  ```c#
  new ObjectPool<T>( Action<IPoolObject> aContruct = null );
  ```
  ```c#
  new ObjectPool<T>( int aMaxObjects, float aDestroyIdleWaitTimeInSeconds, 
                     Action<IPoolObject> aConstruct = null );
  ```
  ```c#
  new ObjectPool<T>( int aMaxObjects, float aDestroyIdleWaitTimeInSeconds,
                     float aUpdateIntervalInSeconds, float aActiveLifetimeInSeconds, 
                     bool aDestroyIdle, bool aIsOpenPool,
                     PoolType aPoolType = PoolType.Recycle,
                     UpdateMode aUpdateMode = UpdateMode.Interval,
                     Action<IPoolObject> aConstruct = null );
  ````
  #### GameObject
  ```c#
  new GameObjectPool( GameObject aPrefab, ConstructObject aContruct = null );
  ```
  ```c#
  new GameObjectPool( GameObject aPrefab, int aMaxObjects, 
                      float aDestroyIdleWaitTimeInSeconds, 
                      Action<IPoolObject> aConstruct = null );
  ```
  ```c#
  new GameObjectPool( GameObject aPrefab, int aMaxObjects, float aDestroyIdleWaitTimeInSeconds,
                      float aUpdateIntervalInSeconds, float aActiveLifetimeInSeconds, 
                      bool aDestroyIdle, bool aIsOpenPool,
                      PoolType aPoolType = PoolType.Recycle,
                      UpdateMode aUpdateMode = UpdateMode.Interval,
                      Action<IPoolObject> aConstruct = null );
````
</details>

<details>
  <summary>Public Variables</summary>
  
  Type | Name | Summary
  ---- | ---- | -------
  System.Action\<T\> | StartAction | An action to perform on an object before it becomes active.
  System.Action\<T\> | ReturnAction | An action to perform on an object before it returns to the pool.
  System.Action\<T\> | RemoveAction | An action to perform on an object before it is removed from the pool.
  System.Action\<T\> | DestroyAction | An action to perform on an object before it gets destroyed.
  int | maxObjects | The max number of objects allowed in the pool at a given time.
  int | idleDontDestroy | The number of idle object to ignore destorying even after the idle wait time has passed.
  float | destroyIdleWaitTimeInSeconds | How long to wait before destroying an idle object.
  float | activeLifetimeInSeconds | The lifetime of an active object in seconds before it is automatically returned to the pool. <br> A value less than zero will not automatically return objects to the pool.
  float | updateIntervalInSeconds | How often to update the pool in seconds if the update mode is set to "Interval". <br>Longer times between intervals may improve performance.
  bool | destroyIdle | Should the pool destroy idle objects? <br>If false, destroyIdleWaitTimeInSeconds is ignored.
  bool | isOpenPool | Can objects not belonging to this pool be returned to this pool? <br>If false, foreign objects returned to this pool will be redirected to their associated pool instead.
  PoolType(enum) | poolType | Recycle = Reuse objects while they are still active if pool is full,<br>Overflow = Create temporary objects that get destroyed upon return if pool is full.
  UpdateMode(enum) | updateMode | Interval = Update the pool using a pre-defined interval.<br>Constant = Update the pool every frame.<br>None = Do not update the pool.
  UpdateType(enum) | updateType | Update = Update the pool Unity's Update method.<br>FixedUpdate = Update the pool Unity's FixedUpdate method.<br>LateUpdate = Update the pool using Unity's LateUpdate method.
  Transform | parentObject | The transform of a GameObject to parent pooled objects to.<br>**\*GameObjectPool & ComponentPool Only.**
   </details>

<details>
  <summary>Public Methods</summary>
  
  Return Type | Method | Summary
  ----------- | ------ | -------
  IPoolObject | RequestObject() | Retrieves an object from the pool To be used.
  void | ReturnToPool(IPoolObject) | Returns an object to the pool.
  void | SetConstructor(System.Action\<IPoolObject\>) | Sets the constructon action for objects upon creation.
  void | SetPrefab(GameObject) | Sets the prefab used to create new objects in the pool<br> **\*GameObjectPool & Component Only.**
 </details>
  
## IPoolObject
<details>
  <summary>Public Methods</summary>
  
  Return Type | Method | Summary
  ----------- | ------ | -------
  PoolPayload | GetPayload() | Returns a struct containing the pooled object and its associated pool.
  T | GetObject() | Returns the pool object.
  ObjectPool<T> | GetPool() | Returns the pool the object belongs to.
  ObjectStatus(enum) | GetStatus() | Returns the status of the pooled object.
  float | GetActiveStartTime() | Returns the Time.realTimeSinceStartup of when the pooled object last became active.
  float | GetIdleStartTime() | Gets the Time.realTimeSinceStartup of when the pooled object last became idle.
  void | ReturnToPool() | Returns the object to its associated pool.
 </details>
  
## UniversalMonobehavior
  This class will automatically create a GameObject instance of itself when a static method is called.
  
  <details>
  <summary>Static Variables</summary>
  
  Type | Name | Summary
  ---- | ---- | -------
  UniversalMonobehavior | Instance | A singleton instance of this class that persists between scenes.
 </details>
  
  <details>
  <summary>Static Methods</summary>
  
  Return Type | Method | Summary
  ----------- | ------ | -------
  bool | AddToUpdate(System.Action) | Adds an action to be called in Unity's Update method.<br>Returns if operation was successful.
  bool | AddToFixedUpdate(System.Action) | Adds an action to be called in Unity's FixedUpdate method.<br>Returns if operation was successful.
  bool | AddToLateUpdate(System.Action) | Adds an action to be called in Unity's LateUpdate method.<br>Returns if operation was successful.
  bool | RemoveFromUpdate(System.Action) | Removes an action set to be called in Unity's Update method.<br>Returns if operation was successful.
  bool | RemoveFromFixedUpdate(System.Action) | Removes an action set to be called in Unity's FixedUpdate method.<br>Returns if operation was successful.
  bool | RemoveFromLateUpdate(System.Action) | Removes an action set to be called in Unity's LateUpdate method.<br>Returns if operation was successful.
 </details>
  
## Example
An object pool can be created by either constructing the object or exposing it to the inspector inside the Unity editor.

<details>
  <summary>Configuration</summary>
  
  ### Constructors
Because all objects created in the pool use their parameterless constructor and some objects need additional configuration when created, a constructor callback can be set. This callback runs after an object is created and functions like a regualr constructor to remedy this limitation. 

For example, this pool handles bullet GameObjects. Each bullet needs a reference to its IPoolObject interface. This could be set when the object is requested, but for efficiency, the reference can be set once, when the bullet is created inside the constructor callback.
```C#
GameObjectPool _pool;
  
  ...
  
_pool.SetConstructor( ( lObj ) => {
  lObj.GetObject().GetComponent<SpawnObject>().Initialize( lObj );
} );
```
  
This can be simplifed even further by using **ComponentPool\<T\>** which also automatically handles the instantiation and destruction of GameObjects, but returns a component type instead. This is useful if a specific component on a GameObject is constantly used.

For example the previous constructor can be simplified like so:
```C#
ComponentPool<ComponentSpawnObject> _pool;
  
  ...
  
_pool.SetConstructor( ( lObj ) => {
    lObj.GetObject().Initialize( lObj );
} );
```
  
  ### Callbacks
Aside from the configuration variables avalible in the constructor and editor fields, the object pools also have callback methods that run when an object is requested, returned, removed, and deleted.
These can be useful if an object needs additional setup during each step of its life.

For example, this pool handles bullets which need to be enabled/disabled and moved to a specified location when retrieved and returned to and from the pool.
```C#
GameObjectPool _pool;
  
  ...
  
 _pool.StartAction = ( obj ) => {
    obj.transform.position = transform.position + Vector3.up;
    obj.SetActive( true );
};

_pool.ReturnAction = ( obj ) => {
    obj.transform.position = transform.position;
    obj.SetActive( false );
};
```
</details>
  
<details>
  <summary>Useage</summary>
  
  Using an object pool is very simple. 
  
  To request an object from the pool, use ``RequestObject()`` which will return an IPoolObject interface that constains the object, assigned pool, status, and active and idle start times. 
  
  To manually return an object to its assigned pool, either call the method ``ReturnToPool()`` inside the IPoolObject interface, or call the ``ReturnToPool(IPoolObject)`` method inside the pool object with the IPoolObject you want to return to the pool.
  
  It is possible to return an object to a differnt pool of the same type if the new pool is flaged as an "open pool" in its configuration settings. To do so, simply call the ``ReturnToPool(IPoolObject)`` method inside the pool you want to return the object to and supply the IPoolObject as the parameter. If an object is returned to a foreign pool that is not flagged as open or of a different type, the object will be returned to its currently assigned pool. If an object is successfully returned to a foreign pool, it will be removed from its previously assigned pool.
</details>

