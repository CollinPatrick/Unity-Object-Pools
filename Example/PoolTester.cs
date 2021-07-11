using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolTester : MonoBehaviour
{
    [SerializeField] private GameObjectPool _pool;
    [SerializeField] private GameObjectPool _pool2;
    [SerializeField] private float _shootInterval = 0.5f;

    private float _shootTimer = 0;
    private float _shootTimer2 = 0;

    void Start()
    {
        _pool.Initialize();
        _pool.StartAction = ( obj ) => {
            obj.transform.position = transform.position + Vector3.up;
            obj.SetActive( true );
        };

        _pool.ReturnAction = ( obj ) => {
            obj.transform.position = transform.position;
            obj.SetActive( false );
        };

        _pool2.Initialize();
        _pool2.StartAction = ( obj ) => {
            obj.transform.position = transform.position - Vector3.up;
            obj.SetActive( true );
        };
        _pool.ReturnAction = ( obj ) => {
            obj.transform.position = transform.position;
            obj.SetActive( false );
        };
    }

    // Update is called once per frame
    void Update()
    {
        if ( Input.GetMouseButton( 0 ) ) { 
            Shoot();
        }

        if ( Input.GetMouseButton( 1 ) ) {
            Shoot2();
        }
    }

    private void Shoot() {
        _shootTimer += Time.deltaTime;
        if( _shootTimer >= _shootInterval ) {
            GameObjectPool.IPoolObject lObj = _pool.RequestObject();
            lObj.GetObject().GetComponent<SpawnObject>().Initialize( lObj );
            _shootTimer = 0;
        }
    }

    private void Shoot2() {
        _shootTimer2 += Time.deltaTime;
        if ( _shootTimer2 >= _shootInterval ) {
            GameObjectPool.IPoolObject lObj = _pool2.RequestObject();
            lObj.GetObject().GetComponent<SpawnObject>().Initialize( lObj, _pool );
            _shootTimer2 = 0;
        }
    }
}
