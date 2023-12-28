// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Utilities.WebRequestRest.Interfaces;
using Object = UnityEngine.Object;

namespace Utilities.Rest.Editor
{
    public abstract class BaseConfigurationInspector<TConfiguration> : UnityEditor.Editor
        where TConfiguration : ScriptableObject, IConfiguration
    {
        protected static SettingsProvider GetSettingsProvider(string name, Action deactivateHandler)
            => new SettingsProvider($"Project/{name}", SettingsScope.Project, new[] { name })
            {
                label = name,
                guiHandler = OnPreferencesGui,
                deactivateHandler = deactivateHandler
            };

        protected static void OnPreferencesGui(string searchContext)
        {
            if (EditorApplication.isPlaying ||
                EditorApplication.isCompiling)
            {
                return;
            }

            var instance = GetOrCreateInstance();

            if (Selection.activeObject != instance)
            {
                Selection.activeObject = instance;
            }

            var instanceEditor = CreateEditor(instance);
            instanceEditor.OnInspectorGUI();
        }

        protected static TConfiguration GetOrCreateInstance(Object target = null)
        {
            var update = false;
            TConfiguration instance;

            if (!Directory.Exists("Assets/Resources"))
            {
                Directory.CreateDirectory("Assets/Resources");
                update = true;
            }

            if (target != null)
            {
                instance = target as TConfiguration;

                var currentPath = AssetDatabase.GetAssetPath(instance);

                if (string.IsNullOrWhiteSpace(currentPath))
                {
                    return instance;
                }

                if (!currentPath.Contains("Resources"))
                {
                    var newPath = $"Assets/Resources/{instance!.name}.asset";

                    if (!File.Exists(newPath))
                    {
                        File.Move(Path.GetFullPath(currentPath), Path.GetFullPath(newPath));
                        File.Move(Path.GetFullPath($"{currentPath}.meta"), Path.GetFullPath($"{newPath}.meta"));
                    }
                    else
                    {
                        AssetDatabase.DeleteAsset(currentPath);
                        var instances = AssetDatabase.FindAssets($"t:{typeof(TConfiguration).Name}");

                        if (instances is { Length: > 0 })
                        {
                            var path = AssetDatabase.GUIDToAssetPath(instances[0]);
                            instance = AssetDatabase.LoadAssetAtPath<TConfiguration>(path);
                        }
                    }

                    update = true;
                }
            }
            else
            {
                var instances = AssetDatabase.FindAssets($"t:{typeof(TConfiguration).Name}");

                if (instances.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(instances[0]);
                    instance = AssetDatabase.LoadAssetAtPath<TConfiguration>(path);
                }
                else
                {
                    instance = CreateInstance<TConfiguration>();
                    AssetDatabase.CreateAsset(instance, $"Assets/Resources/{typeof(TConfiguration).Name}.asset");
                    update = true;
                }
            }

            if (update)
            {
                EditorApplication.delayCall += () =>
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    EditorGUIUtility.PingObject(instance);
                };
            }

            return instance;
        }
    }
}
