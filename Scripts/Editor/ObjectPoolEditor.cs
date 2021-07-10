using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace HiddenFace.Utilities.Editor {
    [CustomPropertyDrawer( typeof( ObjectPool<> ) )]
    public class ObjectPoolEditor : PropertyDrawer {

        public override VisualElement CreatePropertyGUI( SerializedProperty property ) {
            string lType = property.FindPropertyRelative( "myType" ).type;
            var container = new VisualElement();
            var lValueField = new PropertyField( property, $"{property.displayName} <{lType}>" );


            container.Add( lValueField );

            return container;
        }

        public override void OnGUI( Rect position, SerializedProperty property, GUIContent label ) {
            string lType = property.FindPropertyRelative( "myType" ).type;
            EditorGUI.PropertyField( position, property, new GUIContent( $"{label.text} <{lType}>"), true );
        }

        public override float GetPropertyHeight( SerializedProperty property, GUIContent label ) {
            return EditorGUI.GetPropertyHeight( property );
        }
    }

    [CustomPropertyDrawer( typeof( GameObjectPool ) )]
    public class GameObjectPoolEditor : PropertyDrawer {

        public override VisualElement CreatePropertyGUI( SerializedProperty property ) {

            var container = new VisualElement();
            var lValueField = new PropertyField( property, $"{property.displayName} <GameObject>" );


            container.Add( lValueField );

            return container;
        }

        public override void OnGUI( Rect position, SerializedProperty property, GUIContent label ) {
            EditorGUI.PropertyField( position, property, new GUIContent( $"{label.text} <GameObject>" ), true );
        }

        public override float GetPropertyHeight( SerializedProperty property, GUIContent label ) {
            return EditorGUI.GetPropertyHeight( property );
        }
    }
}

