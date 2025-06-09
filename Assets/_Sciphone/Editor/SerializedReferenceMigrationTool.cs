#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Object = UnityEngine.Object;

public class SerializeReferenceMigrationTool : EditorWindow
{
    [System.Serializable]
    public class TypeMigration
    {
        public int instanceId;
        public long[] referenceIds;
        public string oldClassName;
        public string oldAssemblyName;
        public string newClassName;
        public string newAssemblyName;
    }

    private List<TypeMigration> typeMigrations = new List<TypeMigration>();
    private Vector2 scrollPosition;
    private bool includeScenes = true;
    private bool includePrefabs = true;
    private bool includeScriptableObjects = true;
    private bool includeAssets = true;
    private bool scanInProgress;
    private string statusMessage;

    static readonly Regex typeLineRegex = new Regex(
    @"type:\s*\{class:\s*(?<class>[\w\.]+),\s*ns:\s*(?<ns>[\w\.]*),\s*asm:\s*(?<asm>[\w\.]+)\}",
    RegexOptions.Compiled);

    [MenuItem("Tools/Serialize Reference Migration Tool/Open Editor Window")]
    public static void ShowWindow()
    {
        GetWindow<SerializeReferenceMigrationTool>("Missing Reference Finder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Missing Reference Finder", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(scanInProgress);
        {
            includeScenes = EditorGUILayout.Toggle("Include Scenes", includeScenes);
            includePrefabs = EditorGUILayout.Toggle("Include Prefabs", includePrefabs);
            includeScriptableObjects = EditorGUILayout.Toggle("Include Scriptable Objects", includeScriptableObjects);
            includeAssets = EditorGUILayout.Toggle("Include Other Assets", includeAssets);

            if (GUILayout.Button("Scan for Missing References"))
            {
                ScanForMissingReferences();
            }
            if (GUILayout.Button("Fix Missing References"))
            {
                FixMissingTypesInProject();
                AssetDatabase.Refresh();
            }
        }
        EditorGUI.EndDisabledGroup();

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        GUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Missing Types Found", EditorStyles.boldLabel);

        if (GUILayout.Button("Clear Missing References List"))
        {
            typeMigrations.Clear();
        }
        EditorGUILayout.EndHorizontal();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < typeMigrations.Count; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.TextField("Old Assembly", typeMigrations[i].oldAssemblyName);
            EditorGUILayout.TextField("Old Class", typeMigrations[i].oldClassName);
            string newAssemblyName = EditorGUILayout.TextField("New Assembly", typeMigrations[i].newAssemblyName);
            string newClassName = EditorGUILayout.TextField("New Class", typeMigrations[i].newClassName);

            if (EditorGUI.EndChangeCheck())
            {
                typeMigrations[i].newAssemblyName = newAssemblyName;
                typeMigrations[i].newClassName = newClassName;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Remove element from list"))
            {
                typeMigrations.RemoveAt(i);
                i--;
            }
            if (GUILayout.Button("Delete Managed References"))
            {
                RemoveMissingReference(typeMigrations[i]);
                typeMigrations.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
        }

        EditorGUILayout.EndScrollView();
    }

    private void ScanForMissingReferences()
    {
        scanInProgress = true;
        typeMigrations.Clear();
        statusMessage = "Scanning...";
        Repaint();

        try
        {
            if (includeScenes)
            {
                ScanScenes();
            }

            if (includePrefabs)
            {
                ScanPrefabs();
            }

            if (includeScriptableObjects)
            {
                ScanScriptableObjects();
            }

            // Remove duplicates
            typeMigrations = typeMigrations
                .GroupBy(x => new { x.oldAssemblyName, x.oldClassName })
                .Select(g => g.First())
                .ToList();

            statusMessage = $"Scan complete. Found {typeMigrations.Count} missing types.";
        }
        catch (System.Exception e)
        {
            statusMessage = $"Scan failed: {e.Message}";
            Debug.LogError(e);
        }
        finally
        {
            scanInProgress = false;
            Repaint();
        }
    }

    private void ScanScenes()
    {
        string[] scenePaths = AssetDatabase.FindAssets("t:Scene")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !path.StartsWith("Packages/"))
            .ToArray();

        foreach (string scenePath in scenePaths)
        {
            try
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                try
                {
                    GameObject[] rootObjects = scene.GetRootGameObjects();
                    foreach (GameObject rootObject in rootObjects)
                    {
                        ScanGameObject(rootObject);
                    }
                }
                finally
                {
                    // Only close if it's not the last open scene
                    if (EditorSceneManager.sceneCount > 1)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to scan scene {scenePath}: {e.Message}");
            }
        }
    }


    private void ScanPrefabs()
    {
        string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab")
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();

        foreach (string prefabPath in prefabPaths)
        {
            try
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    ScanGameObject(prefab);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to scan prefab {prefabPath}: {e.Message}");
            }
        }
    }

    private void ScanGameObject(GameObject gameObject)
    {
        CheckObjectForMissingReferences(gameObject);

        // Check all components
        Component[] components = gameObject.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component != null)
            {
                CheckObjectForMissingReferences(component);
            }
        }

        // Recursively check children
        foreach (Transform child in gameObject.transform)
        {
            ScanGameObject(child.gameObject);
        }
    }

    private void ScanScriptableObjects()
    {
        string[] scriptableObjectPaths = AssetDatabase.FindAssets("t:ScriptableObject")
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();

        foreach (string soPath in scriptableObjectPaths)
        {
            try
            {
                ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(soPath);
                if (so != null)
                {
                    CheckObjectForMissingReferences(so);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to scan ScriptableObject {soPath}: {e.Message}");
            }
        }
    }

    private void CheckObjectForMissingReferences(UnityEngine.Object obj)
    {
        if ((obj is not MonoBehaviour && obj is not ScriptableObject) || obj == null)
            return;

        if (!SerializationUtility.HasManagedReferencesWithMissingTypes(obj))
            return;

        var missingReferences = SerializationUtility.GetManagedReferencesWithMissingTypes(obj);

        // Group by (assemblyName, className)
        var grouped = missingReferences
            .GroupBy(m => new { m.assemblyName, m.className })
            .ToDictionary(
                g => g.Key,
                g => g.Select(m => m.referenceId).Distinct().ToArray()
            );

        foreach (var group in grouped)
        {
            var migration = new TypeMigration
            {
                instanceId = obj.GetInstanceID(),
                referenceIds = group.Value,
                oldAssemblyName = group.Key.assemblyName,
                oldClassName = group.Key.className,
                newAssemblyName = "",
                newClassName = ""
            };

            typeMigrations.Add(migration);
        }
    }

    private void FixMissingTypesInProject()
    {
        string[] assetPaths = AssetDatabase.GetAllAssetPaths()
            .Where(p =>
                (includePrefabs && p.EndsWith(".prefab")) ||
                (includeScriptableObjects && p.EndsWith(".asset")) ||
                (includeScenes && p.EndsWith(".unity")) ||
                (includeAssets && !p.StartsWith("Packages/")))
            .ToArray();

        foreach (string path in assetPaths)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), path);

            // Skip directories
            if (!File.Exists(fullPath))
                continue;

            string text = File.ReadAllText(fullPath);
            bool modified = false;

            string result = typeLineRegex.Replace(text, match =>
            {
                string oldClass = match.Groups["class"].Value;
                string oldAsm = match.Groups["asm"].Value;

                var migration = typeMigrations.FirstOrDefault(m =>
                    m.oldClassName == oldClass && m.oldAssemblyName == oldAsm);

                if (migration == null)
                    return match.Value;

                string newClassName = string.IsNullOrEmpty(migration.newClassName) ? migration.oldClassName : migration.newClassName;
                string newAssemblyName = string.IsNullOrEmpty(migration.newAssemblyName) ? migration.oldAssemblyName : migration.newAssemblyName;

                // Validate type via reflection
                if (!TypeExists(newClassName, newAssemblyName))
                    return match.Value;

                string replacement = $"type: {{class: {newClassName}, ns: , asm: {newAssemblyName}}}";
                modified = true;
                return replacement;
            });

            if (modified)
            {
                File.WriteAllText(fullPath, result);
                Debug.Log($"Fixed references in: {path}");
            }
        }

        AssetDatabase.Refresh();
        statusMessage = "Fix completed.";
    }

    private bool TypeExists(string className, string assemblyName)
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);

        if (assembly == null)
            return false;

        return assembly.GetTypes().Any(t => t.FullName == className);
    }

    public void RemoveMissingReference(TypeMigration migration)
    {
        Object obj = EditorUtility.InstanceIDToObject(migration.instanceId);
        if (obj == null)
        {
            Debug.LogWarning($"Could not find object with instance ID {migration.instanceId}");
            return;
        }

        // Clear all the specific missing references
        foreach (var refId in migration.referenceIds)
        {
            bool cleared = SerializationUtility.ClearManagedReferenceWithMissingType(obj, refId);
            if (!cleared)
            {
                Debug.LogWarning($"Failed to clear missing reference {refId} on object {obj.name} ({migration.instanceId})");
            }
        }

        // Mark the object dirty so Unity saves the changes
        EditorUtility.SetDirty(obj);

        // Save the asset or scene depending on object type
        if (EditorUtility.IsPersistent(obj))
        {
            AssetDatabase.SaveAssets();
        }
        else if (obj is Component component)
        {
            var scene = component.gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
#endif