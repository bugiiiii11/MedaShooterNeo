using System.Collections.Generic;
using Rene.Sdk;
using Rene.Sdk.Api.Game;
using UnityEditor;
using UnityEngine;

namespace ReneVerse
{
    public class ReneAPICreds : ScriptableObject
    {
        [HideInInspector] public string APIKey;
        [HideInInspector] public string PrivateKey;
        [HideInInspector] public string AuthToken;

        public GameAPI GameAPI;
        public API ReneAPI;

#if UNITY_EDITOR
        public void SaveAPIKeys(string apiKey, string privateKey)
        {
            APIKey = apiKey;
            PrivateKey = privateKey;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public void SaveAuthToken(string authToken)
        {
            AuthToken = authToken;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
        public void DeleteAuthToken()
        {
            AuthToken = null;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public void SaveGameApi(GameAPI gameAPI)
        {
            GameAPI = gameAPI;

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public void SaveAPI(API gameAPI)
        {
            ReneAPI = gameAPI;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private class ReadOnlyInspectorAttribute : PropertyAttribute
        {
        }

        /// <summary>
        /// Allows ReadOnly public fields in inspector
        /// </summary>
        [CustomPropertyDrawer(typeof(ReadOnlyInspectorAttribute))]
        private class ReadOnlyInspectorDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                GUI.enabled = false;
                EditorGUI.PropertyField(position, property, label);
                GUI.enabled = true;
            }
        }
#endif
    }
}