using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;


[CustomPropertyDrawer( typeof( ObjectPool<> ) )]
public class ObjectPoolEditor : PropertyDrawer {

    protected bool _propertyFoldout = true;
    protected float propertyHeight = 0;

    public override VisualElement CreatePropertyGUI( SerializedProperty property ) {
        string lType = property.FindPropertyRelative( "myType" ).type;
        var container = new VisualElement();
        var lValueField = new PropertyField( property, $"{property.displayName} <{lType}>" );

        container.Add( lValueField );

        return container;
    }

    public override void OnGUI( Rect position, SerializedProperty property, GUIContent label ) {
        string lType = property.FindPropertyRelative( "myType" ).type;

        propertyHeight = 0;

        EditorGUI.BeginProperty( position, label, property );
        position.height = 16;
        var lNewPosition = position;

        _propertyFoldout = EditorGUI.Foldout( position, _propertyFoldout, new GUIContent( $"{label.text} <{lType}>" ), true );
        if ( _propertyFoldout ) {
            EditorGUI.indentLevel++;

            lNewPosition = DrawPool( lNewPosition, property, label );

            lNewPosition = new Rect( lNewPosition.x, lNewPosition.y + 10, lNewPosition.width, lNewPosition.height );
            propertyHeight += 10;

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
        EditorUtility.SetDirty( property.serializedObject.targetObject );
    }

    public override float GetPropertyHeight( SerializedProperty property, GUIContent label ) {
        return EditorGUI.GetPropertyHeight( property, true ) + propertyHeight;
    }

    protected Rect NewLine( Rect lPosition, SerializedProperty aProperty, string aPropertyName ) {
        return new Rect( lPosition.x, lPosition.y + GetHeight( aProperty, aPropertyName ) + 2, lPosition.width, lPosition.height );
    }

    protected float GetHeight( SerializedProperty aProperty, string aPropertyName ) {
        return EditorGUI.GetPropertyHeight( aProperty.FindPropertyRelative( aPropertyName ), true );
    }

    protected Rect DrawProperty( Rect aPosition, SerializedProperty aProperty, string aPropertyName ) {
        EditorGUI.PropertyField( aPosition, aProperty.FindPropertyRelative( aPropertyName ), true );
        Rect lNewPosition = NewLine( aPosition, aProperty, aPropertyName );
        propertyHeight += GetHeight( aProperty, aPropertyName );
        return lNewPosition;
    }

    protected Rect DrawPool( Rect position, SerializedProperty property, GUIContent label ) {
        var lNewPosition = position;

        lNewPosition = new Rect( lNewPosition.x, lNewPosition.y + 20, lNewPosition.width, lNewPosition.height );
        propertyHeight += 20;

        EditorGUI.LabelField( lNewPosition, $"Total Objects: {property.FindPropertyRelative( "_pool" ).arraySize}" );
        lNewPosition = new Rect( lNewPosition.x, lNewPosition.y + 20, lNewPosition.width, lNewPosition.height );
        propertyHeight += 20;

        EditorGUI.LabelField( lNewPosition, $"Active Objects: {property.FindPropertyRelative( "_activeObjects" ).arraySize}" );
        lNewPosition = new Rect( lNewPosition.x, lNewPosition.y + 20, lNewPosition.width, lNewPosition.height );
        propertyHeight += 20;

        EditorGUI.LabelField( lNewPosition, $"Overflow Objects: {property.FindPropertyRelative( "_overflowObjects" ).arraySize}" );
        lNewPosition = new Rect( lNewPosition.x, lNewPosition.y + 20, lNewPosition.width, lNewPosition.height );
        propertyHeight += 20;

        EditorGUI.LabelField( lNewPosition, $"Idle Objects: {property.FindPropertyRelative( "_idleObjects" ).arraySize}" );
        lNewPosition = new Rect( lNewPosition.x, lNewPosition.y + 20, lNewPosition.width, lNewPosition.height );
        propertyHeight += 20;

        lNewPosition = new Rect( lNewPosition.x, lNewPosition.y + 20, lNewPosition.width, lNewPosition.height );
        propertyHeight += 20;

        lNewPosition = DrawProperty( lNewPosition, property, "maxObjects" );
        lNewPosition = DrawProperty( lNewPosition, property, "idleDontDestroy" );
        lNewPosition = DrawProperty( lNewPosition, property, "destroyIdleWaitTimeInSeconds" );
        lNewPosition = DrawProperty( lNewPosition, property, "activeLifetimeInSeconds" );
        lNewPosition = DrawProperty( lNewPosition, property, "updateIntervalInSeconds" );
        lNewPosition = DrawProperty( lNewPosition, property, "destroyIdle" );
        lNewPosition = DrawProperty( lNewPosition, property, "isOpenPool" );
        lNewPosition = DrawProperty( lNewPosition, property, "poolType" );
        lNewPosition = DrawProperty( lNewPosition, property, "updateMode" );
        lNewPosition = DrawProperty( lNewPosition, property, "_updateType" );

        return lNewPosition;
    }
}

[CustomPropertyDrawer( typeof( GameObjectPool ) )]
public class GameObjectPoolEditor : ObjectPoolEditor {

    public override VisualElement CreatePropertyGUI( SerializedProperty property ) {

        var container = new VisualElement();
        var lValueField = new PropertyField( property, $"{property.displayName} <GameObject>" );


        container.Add( lValueField );

        return container;
    }

    public override void OnGUI( Rect position, SerializedProperty property, GUIContent label ) {

        propertyHeight = 0;

        EditorGUI.BeginProperty( position, label, property );

        position.height = 16;

        var lNewPosition = position;

        _propertyFoldout = EditorGUI.Foldout( position, _propertyFoldout, new GUIContent( $"{label.text} <GameObject>" ), true );

        if ( _propertyFoldout ) {
            EditorGUI.indentLevel++;

            lNewPosition = DrawPool( lNewPosition, property, label );

            lNewPosition = DrawProperty( lNewPosition, property, "_prefab" );
            lNewPosition = DrawProperty( lNewPosition, property, "parentObject" );

            lNewPosition = new Rect( lNewPosition.x, lNewPosition.y + 10, lNewPosition.width, lNewPosition.height );
            propertyHeight += 10;

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
        EditorUtility.SetDirty( property.serializedObject.targetObject );
    }
}

[CustomPropertyDrawer( typeof( ComponentPool<> ) )]
public class GameObjectComponentPoolEditor : ObjectPoolEditor {

    public override VisualElement CreatePropertyGUI( SerializedProperty property ) {

        var container = new VisualElement();
        var lValueField = new PropertyField( property, $"{property.displayName} <GameObject>" );


        container.Add( lValueField );

        return container;
    }

    public override void OnGUI( Rect position, SerializedProperty property, GUIContent label ) {
        string lType = property.FindPropertyRelative( "myType" ).type;
        lType = lType.Replace( "PPtr<$", "" );
        lType = lType.Replace( ">", "" );

        propertyHeight = 0;

        EditorGUI.BeginProperty( position, label, property );

        position.height = 16;

        var lNewPosition = position;

        _propertyFoldout = EditorGUI.Foldout( position, _propertyFoldout, new GUIContent( $"{label.text} <GameObject:{lType}>" ), true );

        if ( _propertyFoldout ) {
            EditorGUI.indentLevel++;

            lNewPosition = DrawPool( lNewPosition, property, label );

            lNewPosition = DrawProperty( lNewPosition, property, "_prefab" );
            lNewPosition = DrawProperty( lNewPosition, property, "parentObject" );

            lNewPosition = new Rect( lNewPosition.x, lNewPosition.y + 10, lNewPosition.width, lNewPosition.height );
            propertyHeight += 10;

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
        EditorUtility.SetDirty( property.serializedObject.targetObject );
    }
} 

