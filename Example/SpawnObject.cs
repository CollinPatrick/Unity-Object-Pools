using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnObject : MonoBehaviour
{
    [SerializeField] private float _speed = 0.1f;
    private GameObjectPool _returnPool = null;

    private GameObjectPool.IPoolObject _myObject;

    // Update is called once per frame
    void Update()
    {
        transform.Translate( -Vector3.left * _speed );
    }

    public void SetSpeed( float aSpeed ) {
        _speed = aSpeed;
    }

    public void Initialize( GameObjectPool.IPoolObject aPoolObj, GameObjectPool aReturnPool = null ) {
        _myObject = aPoolObj;
        _returnPool = aReturnPool;
    }

    private void OnCollisionEnter( Collision collision ) {
        if( collision.transform.name == "Wall" ) {
            if ( _myObject.GetStatus() == GameObjectPool.ObjectStatus.Active ) {
                if( _returnPool == null ) {
                    _myObject.ReturnToPool();
                }
                else {
                    _returnPool.ReturnToPool( _myObject );
                }
            }
        }
    }
}
