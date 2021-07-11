using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ObjectPool<T> where T : new() {

    #region Wrapper Classes
    public enum ObjectStatus {
        Active = 0,
        Expired = 1,
        Idle = 2,
        Destroyed = 3,
        Removed = 4,
        Unknown = 5,
    }

    public interface IPoolObject {
        /// <summary>
        /// Returns a struct containing the pooled object and its associated pool.
        /// </summary>
        PoolPayload GetPayload();

        /// <summary>
        /// Returns the pool object.
        /// </summary>
        T GetObject();

        /// <summary>
        /// Returns the pool the object belongs to.
        /// </summary>
        ObjectPool<T> GetPool();

        /// <summary>
        /// Gets the status of the pooled object.
        /// </summary>
        /// <returns></returns>
        ObjectStatus GetStatus();

        /// <summary>
        /// Gets the Time.realTimeSinceStartup of when the pooled object last became active.
        /// </summary>
        /// <returns></returns>
        float GetActiveStartTime();

        /// <summary>
        /// Gets the Time.realTimeSinceStartup of when the pooled object last became idle.
        /// </summary>
        float GetIdleStartTime();

        /// <summary>
        /// Returns the object to its associated pool.
        /// </summary>
        void ReturnToPool();
    }

    [Serializable]
    protected class PoolReciept : IPoolObject {

        public PoolPayload payload;

        private ObjectStatus _status = ObjectStatus.Unknown;
        public ObjectStatus status { 
            get {
                return _status;
            } 
            set {
                switch ( value ) {
                    case ObjectStatus.Active:
                        activeStartTime = Time.realtimeSinceStartup;
                        break;
                    case ObjectStatus.Idle:
                        idleStartTime = Time.realtimeSinceStartup;
                        break;
                }
                _status = value;
            } 
        }

        public float activeStartTime = -1f;
        public float idleStartTime = -1f;

        public PoolReciept( T aObj, ObjectPool<T> aPool, ObjectStatus aStatus = ObjectStatus.Unknown ) {
            payload = new PoolPayload {
                myPool = aPool,
                obj = aObj
            };

            status = aStatus;
        }

        #region Interface
        public PoolPayload GetPayload() {
            return payload;
        }

        public T GetObject() {
            return payload.obj;
        }

        public ObjectPool<T> GetPool() {
            return payload.myPool;
        }

        public ObjectStatus GetStatus() {
            return status;
        }

        public float GetActiveStartTime() {
            return activeStartTime;
        }

        public float GetIdleStartTime() {
            return idleStartTime;
        }

        public void ReturnToPool() {
            payload.myPool.ReturnToPool( this );
        }

        public void SetStatus( ObjectStatus aStatus ) {
            status = aStatus;
        }
        #endregion
    }
   
    public struct PoolPayload {

        public ObjectPool<T> myPool;
        public T obj;

        public PoolPayload( ObjectPool<T> aPool, T aObj ) {
            myPool = aPool;
            obj = aObj;
        }
    }
    #endregion

    #region Enums
    public enum UpdateMode {
        Interval,
        Constant,
        None
    }

    public enum PoolType {
        Recycle,
        Overflow
    }

    public enum UpdateType {
        Update,
        FixedUpdate,
        LateUpdate
    }
    #endregion

    //used only for property drawer
    [SerializeField, HideInInspector]private T myType;

    private bool _initialized = false;

    [SerializeField, HideInInspector] protected List<PoolReciept> _pool = new List<PoolReciept>();
    [SerializeField, HideInInspector] protected List<PoolReciept> _activeObjects = new List<PoolReciept>();
    [SerializeField, HideInInspector] protected List<PoolReciept> _overflowObjects = new List<PoolReciept>();
    [SerializeField, HideInInspector] protected List<PoolReciept> _idleObjects = new List<PoolReciept>();

    public delegate IPoolObject ConstructObject( IPoolObject aObject );
    protected Action<IPoolObject> _constructor;

    /// <summary>
    /// An action to perform on an object before it becomes active
    /// </summary>
    public Action<T> StartAction;
    /// <summary>
    /// An action to perform on an object before it returns to the pool
    /// </summary>
    public Action<T> ReturnAction;
    /// <summary>
    /// An action to perform on an object before it is removed from the pool
    /// </summary>
    public Action<T> RemoveAction;
    /// <summary>
    /// An action to perform on an object before it gets destroyed
    /// </summary>
    public Action<T> DestroyAction;

    /// <summary>
    /// The max number of objects allowed in the pool at a given time.
    /// </summary>
    public int maxObjects = 10;
    /// <summary>
    /// The number of idle object to ignore destorying even after the idle wait time has passed.
    /// </summary>
    public int idleDontDestroy = 0;
    /// <summary>
    /// How long to wait before destroying an idle object.
    /// </summary>
    public float destroyIdleWaitTimeInSeconds = 5f;
    /// <summary>
    /// The lifetime of an active object in seconds before it is automatically returned to the pool.
    /// A value less than zero will not automatically return objects to the pool.
    /// </summary>
    public float activeLifetimeInSeconds = -1f;
    /// <summary>
    /// How often to update the pool in seconds if the update mode is set to "Interval".
    /// Longer times between intervals may improve performance
    /// </summary>
    public float updateIntervalInSeconds = 1f;
    /// <summary>
    /// Should the pool destroy idle objects?
    /// If false, destroyIdleWaitTimeInSeconds is ignored.
    /// </summary>
    public bool destroyIdle = true;
    /// <summary>
    /// Can objects not belonging to this pool be returned to this pool?
    /// If false, foreign objects returned to this pool will be redirected to their associated pool instead.
    /// </summary>
    public bool isOpenPool = false;

    /// <summary>
    /// Recycle = Reuse objects while they are still active if pool is full,
    /// Overflow = Create temporary objects that get destroyed upon return if pool is full.
    /// </summary>
    public PoolType poolType = PoolType.Recycle;

    /// <summary>
    /// Interval = Update the pool using a pre-defined interval.
    /// Constant = Update the pool every frame.
    /// None = Do not update the pool.
    /// </summary>
    public UpdateMode updateMode = UpdateMode.Interval;
    
    [SerializeField] private UpdateType _updateType = UpdateType.Update;

    /// <summary>
    /// Update = Update the pool Unity's Update method.
    /// FixedUpdate = Update the pool Unity's FixedUpdate method.
    /// LateUpdate = Update the pool using Unity's LateUpdate method.
    /// </summary>
    public UpdateType updateType { 
        get {
            return _updateType;
        }
        set {
            RemoveUpdate( _updateType );
            _updateType = value;
            SetUpdate( _updateType );
        }
    }

    private float lastUpdateTime = 0;

    #region Constructors & Initialization
    public ObjectPool( int aMaxObjects, float aDestroyIdleWaitTimeInSeconds,
                       float aUpdateIntervalInSeconds, float aActiveLifetimeInSeconds, 
                       bool aDestroyIdle, bool aIsOpenPool,
                       PoolType aPoolType = PoolType.Recycle,
                       UpdateMode aUpdateMode = UpdateMode.Interval,
                       Action<IPoolObject> aConstruct = null ) 
                       : this(aMaxObjects, aDestroyIdleWaitTimeInSeconds, aConstruct){

        updateIntervalInSeconds = aUpdateIntervalInSeconds;
        activeLifetimeInSeconds = aActiveLifetimeInSeconds;
        destroyIdle = aDestroyIdle;
        isOpenPool = aIsOpenPool;
        poolType = aPoolType;
        updateMode = aUpdateMode;
    }

    public ObjectPool( int aMaxObjects, float aDestroyIdleWaitTimeInSeconds, Action<IPoolObject> aConstruct = null )
                       : this( aConstruct ){
        maxObjects = aMaxObjects;
        destroyIdleWaitTimeInSeconds = aDestroyIdleWaitTimeInSeconds;
    }

    public ObjectPool( Action<IPoolObject> aContruct = null ) {
        _constructor = aContruct;
    }

    ~ObjectPool(){
        RemoveUpdate( updateType );
    }

    private void Initialize() {
        if ( _initialized ) {
            return;
        }

        _pool = new List<PoolReciept>();
        _activeObjects = new List<PoolReciept>();
        _overflowObjects = new List<PoolReciept>();
        _idleObjects = new List<PoolReciept>();

        updateType = _updateType;
        InternalInitialize();
        _initialized = true;
    }

    protected virtual void InternalInitialize() { }
    #endregion

    #region Object Handling
    protected virtual PoolReciept CreateObject() {
        T lObj = new T();
        PoolReciept lReciept = new PoolReciept( lObj, this, ObjectStatus.Unknown );
        if( _constructor != null ) {
            _constructor( lReciept );
        }
        return lReciept;
    }

    protected virtual void DestroyObject( PoolReciept aReciept ) {
        RemoveObject( aReciept );
        DestroyAction?.Invoke( aReciept.payload.obj );
        aReciept.status = ObjectStatus.Destroyed;
    }

    protected virtual void RemoveObject( PoolReciept aReciept ) {
        if ( _pool.Contains( aReciept ) ) {
            aReciept.status = ObjectStatus.Removed;
            _pool.Remove( aReciept );
        }

        if ( _activeObjects.Contains( aReciept ) ) {
            _activeObjects.Remove( aReciept );
            RemoveAction?.Invoke( aReciept.payload.obj );
            return;
        }

        if ( _overflowObjects.Contains( aReciept ) ) {
            _overflowObjects.Remove( aReciept );
            RemoveAction?.Invoke( aReciept.payload.obj );
            return;
        }

        if ( _idleObjects.Contains( aReciept ) ) {
            _idleObjects.Remove( aReciept );
            RemoveAction?.Invoke( aReciept.payload.obj );
            return;
        }

    }

    public IPoolObject RequestObject() {

        if ( !_initialized ) {
            Initialize();
        }

        //Cleans out any leftover overflow objects before using active objects.
        if( poolType == PoolType.Recycle && _overflowObjects.Count > 0 ) {
            PoolReciept lRecycledObj = _overflowObjects[0];
            ReturnToPool( lRecycledObj );
            return RequestObject();
        }

        if ( _idleObjects.Count > 0 ) {
            PoolReciept lIdle = _idleObjects[0];
            lIdle.SetStatus( ObjectStatus.Active );
            _activeObjects.Add( lIdle );
            _idleObjects.Remove( lIdle );
            StartAction?.Invoke( lIdle.payload.obj );
            return lIdle;
        }
        else if ( _pool.Count < maxObjects ) {
            PoolReciept lReciept = CreateObject();
            lReciept.SetStatus( ObjectStatus.Active );
            _pool.Add( lReciept );
            _activeObjects.Add( lReciept );
            StartAction?.Invoke( lReciept.payload.obj );
            return lReciept;
        }
        else if( poolType == PoolType.Recycle ) {
            PoolReciept lRecycledObj = _activeObjects[0];
            ReturnToPool( lRecycledObj );
            return RequestObject();
        }
        else if( poolType == PoolType.Overflow ) {
            PoolReciept lReciept = CreateObject();
            lReciept.SetStatus( ObjectStatus.Active );
            _pool.Add( lReciept );
            _overflowObjects.Add( lReciept );
            StartAction?.Invoke( lReciept.payload.obj );
            return lReciept;
        }
        else {
            //This should never happen
            return null;
        }
    }

    public void ReturnToPool( IPoolObject aObject ) {

        if ( !_initialized ) {
            Initialize();
        }

        PoolReciept lReciept = aObject as PoolReciept;
        if( aObject == null || lReciept == null ) {
            Debug.LogError( $"[{nameof( ObjectPool<T> )}] - Error: Cannot return null to object pool." );
            return;
        }

        if ( _activeObjects.Contains( lReciept ) ) {
            _activeObjects.Remove( lReciept );
            lReciept.SetStatus( ObjectStatus.Idle );
            _idleObjects.Add( lReciept );
            ReturnAction?.Invoke( lReciept.payload.obj );

        }else if( _overflowObjects.Contains( lReciept ) ) {
            if( _pool.Count < maxObjects || poolType == PoolType.Recycle ) {
                _overflowObjects.Remove( lReciept );
                _idleObjects.Add( lReciept );
                ReturnAction?.Invoke( lReciept.payload.obj );
            }
            else {
                DestroyObject( lReciept );
            }
        }
        else if( isOpenPool ) {
            if( _pool.Count < maxObjects ) {
                _pool.Add( lReciept );
                _idleObjects.Add( lReciept );
                lReciept.payload.myPool.RemoveObject( lReciept );
                lReciept.payload = new PoolPayload( this, lReciept.payload.obj );
                ReturnAction?.Invoke( lReciept.payload.obj );
            }
            else {
                if ( poolType == PoolType.Overflow ) {
                    _pool.Add( lReciept );
                    _overflowObjects.Add( lReciept );
                    lReciept.payload.myPool.RemoveObject( lReciept );
                    lReciept.payload = new PoolPayload( this, lReciept.payload.obj );
                    ReturnAction?.Invoke( lReciept.payload.obj );
                }
                else {
                    ReturnAction?.Invoke( lReciept.payload.obj );
                    DestroyObject( lReciept );
                }
            }
        }
        else {
            Debug.LogError( $"[{nameof(ObjectPool<T>)}] - Error: Attempted to return a foreign object to a closed object pool. Attempting to return to original pool" );
            lReciept.payload.myPool.ReturnToPool( lReciept );
            ReturnAction?.Invoke( lReciept.payload.obj );
        }
    }
    #endregion

    #region Update Functions
    private void SetUpdate( UpdateType aUpdateType ) {
        switch ( aUpdateType ) {
            case UpdateType.Update:
                UniversalMonobehavior.AddToUpdate( Tick );
                break;
            case UpdateType.FixedUpdate:
                UniversalMonobehavior.AddToFixedUpdate( Tick );
                break;
            case UpdateType.LateUpdate:
                UniversalMonobehavior.AddToLateUpdate( Tick );
                break;
        }
    }

    private void RemoveUpdate( UpdateType aUpdateType ) {
        switch ( aUpdateType ) {
            case UpdateType.Update:
                UniversalMonobehavior.RemoveFromUpdate( Tick );
                break;
            case UpdateType.FixedUpdate:
                UniversalMonobehavior.RemoveFromFixedUpdate( Tick );
                break;
            case UpdateType.LateUpdate:
                UniversalMonobehavior.RemoveFromLateUpdate( Tick );
                break;
        }
    }

    private void Tick() {

        if ( !_initialized ) {
            return;
        }

        switch ( updateMode ) {
            case UpdateMode.Interval:
                if ( lastUpdateTime + updateIntervalInSeconds <= Time.realtimeSinceStartup ) {
                    if ( destroyIdle ) {
                        UpdateIdle();
                    }
                    if( activeLifetimeInSeconds > 0 ) {
                        UpdateActive();
                    }
                    if(poolType == PoolType.Overflow ) {
                        UpdateOverflow();
                    }
                    lastUpdateTime = Time.realtimeSinceStartup;
                }
                break;
            case UpdateMode.Constant:
                if ( destroyIdle ) {
                    UpdateIdle();
                }
                if ( activeLifetimeInSeconds > 0 ) {
                    UpdateActive();
                }
                if ( poolType == PoolType.Overflow ) {
                    UpdateOverflow();
                }
                lastUpdateTime = Time.realtimeSinceStartup;
                break;
            case UpdateMode.None:
                break;
        }
    }

    private void UpdateIdle() {
        for ( int i = 0; i < _idleObjects.Count; i++ ) {
            if ( _idleObjects[i].idleStartTime + destroyIdleWaitTimeInSeconds <= Time.realtimeSinceStartup ) {
                if( _idleObjects.Count > idleDontDestroy ) {
                    DestroyObject( _idleObjects[i] );
                }
            }
        }
    }

    private void UpdateActive() {
        for ( int i = 0; i < _activeObjects.Count; i++ ) {
            if ( _activeObjects[i].activeStartTime + activeLifetimeInSeconds <= Time.realtimeSinceStartup ) {
                _activeObjects[i].ReturnToPool();
                //DestroyObject( _activeObjects[i] );
            }
        }
    }

    private void UpdateOverflow() {
        for ( int i = 0; i < _overflowObjects.Count; i++ ) {
            if ( _overflowObjects[i].activeStartTime + activeLifetimeInSeconds <= Time.realtimeSinceStartup ) {
                _overflowObjects[i].ReturnToPool();
            }
        }
    }
    #endregion

    #region Setters
    public void SetConstructor( Action<IPoolObject> aConstruct ) {
        _constructor = aConstruct;
    }
    #endregion
}

[Serializable]
public class GameObjectPool : ObjectPool<GameObject>{

    [SerializeField] private GameObject _prefab;
    public Transform parentObject = null;

    public GameObjectPool( GameObject aPrefab, int aMaxObjects, float aDestroyIdleWaitTimeInSeconds,
                           float aUpdateIntervalInSeconds, float aActiveLifetimeInSeconds, 
                           bool aDestroyIdle, bool aIsOpenPool,
                           PoolType aPoolType = PoolType.Recycle,
                           UpdateMode aUpdateMode = UpdateMode.Interval,
                           Action<IPoolObject> aConstruct = null )
                           : base( aMaxObjects, aDestroyIdleWaitTimeInSeconds, aUpdateIntervalInSeconds, 
                                   aActiveLifetimeInSeconds, aDestroyIdle, aIsOpenPool, aPoolType,
                                   aUpdateMode, aConstruct ) {
        _prefab = aPrefab;
    }

    public GameObjectPool( GameObject aPrefab, int aMaxObjects, float aDestroyIdleWaitTimeInSeconds, Action<IPoolObject> aConstruct = null )
                           : base( aMaxObjects, aDestroyIdleWaitTimeInSeconds, aConstruct ) {
        _prefab = aPrefab;
    }

    public GameObjectPool( GameObject aPrefab, Action<IPoolObject> aConstruct = null ) : base( aConstruct ) {
        _prefab = aPrefab;
    }

    protected override PoolReciept CreateObject() {
        GameObject lObj = GameObject.Instantiate( _prefab );

        if( parentObject != null ) {
            lObj.transform.parent = parentObject;
        }

        PoolReciept lReciept = new PoolReciept( lObj, this, ObjectStatus.Unknown );
        if( _constructor != null ) {
            _constructor( lReciept );
        }

        return lReciept;
    }

    protected sealed override void DestroyObject( PoolReciept aReciept ) {
        RemoveObject( aReciept );
        GameObject.Destroy( aReciept.payload.obj );
    }

    public void SetPrefab( GameObject aPrefab ) {
        _prefab = aPrefab;
    }
}
