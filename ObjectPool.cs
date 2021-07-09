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
        PoolPayload GetPayload();
        ObjectStatus GetStatus();
        float GetActiveStartTime();
        float GetIdleStartTime();
        void ReturnToPool();
        void SetStatus( ObjectStatus aStatus );
    }

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

        public PoolReciept( T aObj, ObjectPool<T> aPool, ObjectStatus aStatus = ObjectStatus.Unknown) {
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
        ConstantFixed,
        ConstantLate,
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

    protected List<PoolReciept> _pool = new List<PoolReciept>();
    protected List<PoolReciept> _activeObjects = new List<PoolReciept>();
    protected List<PoolReciept> _overflowObjects = new List<PoolReciept>();
    protected List<PoolReciept> _idleObjects = new List<PoolReciept>();

    public delegate T ConstructObject( T aObject );
    protected ConstructObject _constructor;

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
    public int idleDontDestroy = 0; //TODO: IMPLIMENT
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

    public UpdateMode updateMode = UpdateMode.Interval;
    
    private UpdateType _updateType = UpdateType.Update;
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
                       ConstructObject aConstruct = null ) 
                       : this(aMaxObjects, aDestroyIdleWaitTimeInSeconds, aConstruct){

        updateIntervalInSeconds = aUpdateIntervalInSeconds;
        activeLifetimeInSeconds = aActiveLifetimeInSeconds;
        destroyIdle = aDestroyIdle;
        isOpenPool = aIsOpenPool;
        poolType = aPoolType;
        updateMode = aUpdateMode;
    }

    public ObjectPool( int aMaxObjects, float aDestroyIdleWaitTimeInSeconds, ConstructObject aConstruct = null )
                       : this( aConstruct ){
        maxObjects = aMaxObjects;
        destroyIdleWaitTimeInSeconds = aDestroyIdleWaitTimeInSeconds;
    }

    public ObjectPool( ConstructObject aContruct = null ) {
        _constructor = aContruct;
        UniversalMonobehavior.Tick.AddListener( Tick );
    }

    ~ObjectPool(){
        RemoveUpdate( updateType );
    }

    public void Initialize() {
        if ( _initialized ) {
            return;
        }

        updateType = _updateType;
        InternalInitialize();
        _initialized = true;
    }

    protected virtual void InternalInitialize() { }
    #endregion

    #region Object Handling
    protected virtual T CreateObject() {
        return ( _constructor == null ) ? new T() : _constructor( new T() );
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
        if( _pool.Count < maxObjects ) {
            T lNewObj = CreateObject();
            PoolReciept lReciept = new PoolReciept( lNewObj, this, ObjectStatus.Active ); ;
            _pool.Add( lReciept );
            _activeObjects.Add( lReciept );
            StartAction?.Invoke( lReciept.payload.obj );
            return lReciept;
        }
        else if( _idleObjects.Count > 0 ) {
            PoolReciept lIdle = _idleObjects[0];
            lIdle.SetStatus( ObjectStatus.Active );
            _activeObjects.Add( lIdle );
            _idleObjects.Remove( lIdle );
            StartAction?.Invoke( lIdle.payload.obj );
            return lIdle;
        }
        else if( poolType == PoolType.Recycle ) {
            PoolReciept lRecycledObj = _activeObjects[0];
            ReturnToPool( lRecycledObj );
            return RequestObject();
        }
        else if( poolType == PoolType.Overflow ) {
            T lNewObj = CreateObject();

            PoolReciept lReciept = new PoolReciept( lNewObj, this, ObjectStatus.Active ); ;
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

        PoolReciept lReciept = aObject as PoolReciept;
        if( aObject == null || lReciept == null ) {
            Debug.LogError( "Error: Cannot return null to object pool." );
            return;
        }

        if ( _activeObjects.Contains( lReciept ) ) {
            _activeObjects.Remove( lReciept );
            lReciept.SetStatus( ObjectStatus.Idle );
            _idleObjects.Add( lReciept );
            ReturnAction?.Invoke( lReciept.payload.obj );

        }else if( _overflowObjects.Contains( lReciept ) ) {
            if( _pool.Count < maxObjects ) {
                _overflowObjects.Remove( lReciept );
                _idleObjects.Add( lReciept );
                ReturnAction?.Invoke( lReciept.payload.obj );
            }
        }
        else if( isOpenPool ) {
            if( _pool.Count < maxObjects ) {
                _pool.Add( lReciept );
                _idleObjects.Add( lReciept );
                lReciept.payload.myPool.RemoveObject( lReciept );
                ReturnAction?.Invoke( lReciept.payload.obj );
            }
            else {
                if ( poolType == PoolType.Overflow ) {
                    _pool.Add( lReciept );
                    _overflowObjects.Add( lReciept );
                    lReciept.payload.myPool.RemoveObject( lReciept );
                    ReturnAction?.Invoke( lReciept.payload.obj );
                }
                else {
                    ReturnAction?.Invoke( lReciept.payload.obj );
                    DestroyObject( lReciept );
                }
            }
        }
        else {
            Debug.LogError( $"Error: Attempted to return a foreign object to a closed object pool. Attempting to return to original pool" );
            lReciept.payload.myPool.ReturnToPool( lReciept );
        }
    }
    #endregion

    #region Update Functions
    private void SetUpdate( UpdateType aUpdateType ) {
        switch ( aUpdateType ) {
            case UpdateType.Update:
                UniversalMonobehavior.Tick.AddListener( Tick );
                break;
            case UpdateType.FixedUpdate:
                UniversalMonobehavior.FixedTick.AddListener( Tick );
                break;
            case UpdateType.LateUpdate:
                UniversalMonobehavior.LateTick.AddListener( Tick );
                break;
        }
    }

    private void RemoveUpdate( UpdateType aUpdateType ) {
        switch ( aUpdateType ) {
            case UpdateType.Update:
                UniversalMonobehavior.Tick.RemoveListener( Tick );
                break;
            case UpdateType.FixedUpdate:
                UniversalMonobehavior.FixedTick.RemoveListener( Tick );
                break;
            case UpdateType.LateUpdate:
                UniversalMonobehavior.LateTick.RemoveListener( Tick );
                break;
        }
    }

    private void Tick() {

        if ( !_initialized ) {
            return;
        }

        switch ( updateMode ) {
            case UpdateMode.Interval:
                if ( lastUpdateTime + updateIntervalInSeconds >= Time.realtimeSinceStartup ) {
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
                DestroyObject( _idleObjects[i] );
            }
        }
    }

    private void UpdateActive() {
        for ( int i = 0; i < _activeObjects.Count; i++ ) {
            if ( _activeObjects[i].activeStartTime + activeLifetimeInSeconds <= Time.realtimeSinceStartup ) {
                DestroyObject( _activeObjects[i] );
            }
        }
    }

    private void UpdateOverflow() {
        for ( int i = 0; i < _overflowObjects.Count; i++ ) {
            if ( _overflowObjects[i].activeStartTime + activeLifetimeInSeconds <= Time.realtimeSinceStartup ) {
                DestroyObject( _overflowObjects[i] );
            }
        }
    }
    #endregion

    #region Setters
    public void SetConstructor(ConstructObject aConstruct) {
        _constructor = aConstruct;
    }
    #endregion
}

[Serializable]
public class GameObjectPool : ObjectPool<GameObject>{

    [SerializeField] private GameObject _prefab;

    public GameObjectPool( GameObject aPrefab, int aMaxObjects, float aDestroyIdleWaitTimeInSeconds,
                           float aUpdateIntervalInSeconds, float aActiveLifetimeInSeconds, 
                           bool aDestroyIdle, bool aIsOpenPool,
                           PoolType aPoolType = PoolType.Recycle,
                           UpdateMode aUpdateMode = UpdateMode.Interval,
                           ConstructObject aConstruct = null )
                           : base( aMaxObjects, aDestroyIdleWaitTimeInSeconds, aUpdateIntervalInSeconds, 
                                   aActiveLifetimeInSeconds, aDestroyIdle, aIsOpenPool, aPoolType,
                                   aUpdateMode, aConstruct ) {
        _prefab = aPrefab;
    }

    public GameObjectPool( GameObject aPrefab, int aMaxObjects, float aDestroyIdleWaitTimeInSeconds, ConstructObject aConstruct = null )
                           : base( aMaxObjects, aDestroyIdleWaitTimeInSeconds, aConstruct ) {
        _prefab = aPrefab;
    }

    public GameObjectPool( GameObject aPrefab, ConstructObject aConstruct = null ) : base( aConstruct ) {
        _prefab = aPrefab;
    }

    protected override GameObject CreateObject() {
        GameObject lObj = GameObject.Instantiate( _prefab );
        return ( _constructor == null ) ? lObj : _constructor( lObj );
    }

    protected override void DestroyObject( PoolReciept aReciept ) {
        RemoveObject( aReciept );
        GameObject.Destroy( aReciept.payload.obj );
    }

    public void SetPrefab( GameObject aPrefab ) {
        _prefab = aPrefab;
    }
}
