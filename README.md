# Unity Object Pools

#### **This code is still in development and has not been thoroughly tested. Use at your own discretion.**

## Class ObjectPool

<details>
  <summary>Constructors</summary>
  
  
  #### Generic
  ```c#
  new ObjectPool<T>( ConstructObject aContruct = null );
  ```
  ```c#
  new ObjectPool<T>( int aMaxObjects, float aDestroyIdleWaitTimeInSeconds, ConstructObject aConstruct = null );
  ```
  ```c#
  new ObjectPool<T>( int aMaxObjects, float aDestroyIdleWaitTimeInSeconds,
                     float aUpdateIntervalInSeconds, float aActiveLifetimeInSeconds, 
                     bool aDestroyIdle, bool aIsOpenPool,
                     PoolType aPoolType = PoolType.Recycle,
                     UpdateMode aUpdateMode = UpdateMode.Interval,
                     ConstructObject aConstruct = null );
  ````
  #### GameObject
  ```c#
  new GameObjectPool( GameObject aPrefab, ConstructObject aContruct = null );
  ```
  ```c#
  new GameObjectPool( GameObject aPrefab, int aMaxObjects, 
                      float aDestroyIdleWaitTimeInSeconds, 
                      ConstructObject aConstruct = null );
  ```
  ```c#
  new GameObjectPool( GameObject aPrefab, int aMaxObjects, float aDestroyIdleWaitTimeInSeconds,
                      float aUpdateIntervalInSeconds, float aActiveLifetimeInSeconds, 
                      bool aDestroyIdle, bool aIsOpenPool,
                      PoolType aPoolType = PoolType.Recycle,
                      UpdateMode aUpdateMode = UpdateMode.Interval,
                      ConstructObject aConstruct = null );
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
   </details>

<details>
  <summary>Public Methods</summary>
  
  Return Type | Method | Summary
  ----------- | ------ | -------
  IPoolObject | RequestObject() | Retrieves an object from the pool To be used.
  void | ReturnToPool(IPoolObject) | Returns an object to the pool.
  void | SetConstructor(ConstructObject) | Sets the constructon action for objects upon creation.
  void | SetPrefab(GameObject) | Sets the prefab used to create new objects in the pool<br> **\*GameObjectPool Only.**
 </details>
  
``Both ObjectPool and GameObjectPool are serialized classes that can be exposed in the Unity editor.``
  
## Interface IPoolObject
<details>
  <summary>Public Methods</summary>
  
  Return Type | Method | Summary
  ----------- | ------ | -------
  PoolPayload | GetPayload() | Returns a struct containing the pooled object and its associated pool.
  ObjectStatus(enum) | GetStatus() | Returns the status of the pooled object.
  float | GetActiveStartTime() | Returns the Time.realTimeSinceStartup of when the pooled object last became active.
  float | GetIdleStartTime() | Gets the Time.realTimeSinceStartup of when the pooled object last became idle.
  void | ReturnToPool() | Returns the object to its associated pool.
 </details>
  
## Class UniversalMonobehavior
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
