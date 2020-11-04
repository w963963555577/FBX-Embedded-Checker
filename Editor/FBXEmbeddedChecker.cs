using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
//using UnityEditor.Formats.Fbx.Exporter;

public class FBXEmbeddedChecker : EditorWindow
{
    /*
    [MenuItem("Funcy/Export FBX TEST")]
    public static void ExportFBXTEST()
    {
        var filter = Selection.gameObjects[0].GetComponent<MeshFilter>();
        var o = filter.sharedMesh;
        var m = Instantiate(filter.sharedMesh);
        m.name = filter.sharedMesh.name;
        filter.sharedMesh = m;
        string filePath = Path.Combine(Application.dataPath, "Funcy_URP_Demo", "zero", Selection.gameObjects[0].name + ".fbx");
        ModelExporter.ExportObjects(filePath, Selection.gameObjects);
        filter.sharedMesh = o;
    }
    */
    
    [MenuItem("Tools/FBX Embedded Checker")]
    public static void Open()
    {
        var editorList = Resources.FindObjectsOfTypeAll<FBXEmbeddedChecker>().ToList();

        var window = editorList.Count > 0 ? editorList[0] : GetWindow<FBXEmbeddedChecker>();
        window.minSize = new Vector2(300, 800);
        window.titleContent = new GUIContent("FBX Embedded Checker");
        window.Show();
        window.Focus();
    }
    bool finding = false;
    float currentRate = 1.0f;
    string status = "Waiting...";
    private async void OnGUI()
    {        
        GUILayout.Label("root floder", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal("Box");
        EditorGUILayout.LabelField(floder, EditorStyles.miniBoldLabel);
        if(Selection.activeObject && GUILayout.Button("Pick Selected", EditorStyles.miniButton, GUILayout.MaxWidth(100)) )
        {
            floder = AssetDatabase.GetAssetPath(Selection.activeObject);
        }
        GUILayout.EndHorizontal();

        if (finding)
        {
            EditorGUI.ProgressBar(new Rect(3, position.height - 15, position.width - 6, 15), currentRate, status);
        }        

        EditorGUI.BeginDisabledGroup(finding);
        if(GUILayout.Button("Find"))
        {
            status = "Waiting...";
            finding = true;
            fbxList = new List<GameObject>();

            var fbxPaths = Directory.GetFiles(Path.Combine(Application.dataPath.Replace("/Assets", "/"), floder), "*", SearchOption.AllDirectories).ToList().FindAll(f => Path.GetExtension(f).ToLower() == ".fbx");
            float index = 0;
            foreach(var fbxPath in fbxPaths)
            {
                var path = fbxPath.Replace(Application.dataPath, "Assets");
                if (CheckFBXEmbeddedTextures(path))
                {
                    fbxList.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));                    
                    Repaint();
                }
                await Task.Delay(10);
                index++;
                currentRate = index / fbxPaths.Count;
            }
            status = "Finished!!";
            Repaint();
            await Task.Delay(2000);            
            finding = false;
            Repaint();
        }
        EditorGUI.EndDisabledGroup();
        


        foreach(var fbx in fbxList)
        {
            EditorGUILayout.ObjectField(fbx, typeof(GameObject), false);
        }

    }


    public static bool CheckFBXEmbeddedTextures(string fbxPath)/*fbxPath Example : @"Assets\TESTFBX\Demo_10.fbx" */
    {
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        ModelImporter fbx = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        var fbx_editor = Editor.CreateEditor(fbx);

        var tabsProp = fbx_editor.GetType().GetProperty("tabs", flags);
        System.Array tabs = (System.Array)tabsProp.GetValue(fbx_editor);

        var modelImporterMaterialEditor = tabs.GetValue(3);
        var hasEmbeddedTexturesProp = modelImporterMaterialEditor.GetType().GetField("m_HasEmbeddedTextures", flags);

        SerializedProperty hasEmbeddedTextures = hasEmbeddedTexturesProp.GetValue(modelImporterMaterialEditor) as SerializedProperty;
        bool result = hasEmbeddedTextures.boolValue;
        Editor.DestroyImmediate(fbx_editor, false);

        return result;
    }


    #region Props
    [SerializeField] string floder = @"Assets";
    [SerializeField] List<GameObject> fbxList = new List<GameObject>();
    #endregion
    
    #region Editor
    
    #endregion
}
