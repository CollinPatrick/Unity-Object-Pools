using UnityEngine.Events;
using UnityEngine;
using System;
using System.Collections.Generic;

public class UniversalMonobehavior : MonoBehaviour
{
    public static UniversalMonobehavior Instance { get; private set; }

    private static List<Action> _onTick = new List<Action>();
    private static List<Action> _onFixedTick = new List<Action>();
    private static List<Action> _onLateTick = new List<Action>();

    private void Awake() {
        UniversalMonobehavior component = GetComponent<UniversalMonobehavior>();
        if ( Instance == null ) {
            Instance = component;
            DontDestroyOnLoad( this );
        }
        else if ( Instance != component ) {
            Destroy( gameObject );
            return;
        }
    }

    private static void Initialize() {
        GameObject lGameObject = Instantiate( new GameObject( "Universal Monobehavior" ) );
        lGameObject.name = lGameObject.name.Replace( "(Clone)", "" );
        lGameObject.AddComponent<UniversalMonobehavior>();
    }

    private void Update() {
        for ( int i = _onTick.Count - 1; i >= 0; i-- ) {
            if ( _onTick[i] != null ) {
                _onTick[i]?.Invoke();
            }
            else {
                _onTick.RemoveAt( i );
            }
        }
    }

    private void FixedUpdate() {
        for ( int i = _onFixedTick.Count - 1; i >= 0; i-- ) {
            if ( _onFixedTick[i] != null ) {
                _onFixedTick[i]?.Invoke();
            }
            else {
                _onFixedTick.RemoveAt( i );
            }
        }
    }

    private void LateUpdate() {
        for ( int i = _onLateTick.Count - 1; i >= 0; i-- ) {
            if( _onLateTick[i] != null ) {
                _onLateTick[i]?.Invoke();
            }
            else {
                _onLateTick.RemoveAt( i );
            } 
        }
    }

    private static bool Add( List<Action> aList, Action aAction ) {
        if ( Instance == null ) {
            Initialize();
        }

        if ( aList.Contains( aAction ) ) {
            return false;
        }

        aList.Add( aAction );
        return true;
    }

    private static bool Remove( List<Action> aList, Action aAction ) {
        if ( Instance == null ) {
            Initialize();
        }

        if ( !aList.Contains( aAction ) ) {
            return false;
        }

        aList.Remove( aAction );
        return true;
    }

    public static bool AddToUpdate( Action aAction ) => Add( _onTick, aAction );
    public static bool AddToFixedUpdate( Action aAction ) => Add( _onFixedTick, aAction );
    public static bool AddToLateUpdate( Action aAction ) => Add( _onLateTick, aAction );

    public static bool RemoveFromUpdate( Action aAction ) => Remove( _onTick, aAction );
    public static bool RemoveFromFixedUpdate( Action aAction ) => Remove( _onFixedTick, aAction );
    public static bool RemoveFromLateUpdate( Action aAction ) => Remove( _onLateTick, aAction );
}
