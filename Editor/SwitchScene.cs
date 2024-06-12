using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityToolbarExtender;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SwitchScene
{
    #region Variables
    private static Rect buttonRect;
    #endregion

    static SwitchScene()
    {
        ToolbarExtender.LeftToolbarGUI.Add(DrawLeftGUI);
    }

    private static void DrawLeftGUI()
    {
        bool hasValidScene = true;
        if (EditorBuildSettings.scenes.Length > 0)
        {
            foreach (EditorBuildSettingsScene editorScene in EditorBuildSettings.scenes)
            {
                if (!PopupSwitchScene.SceneIsValid(editorScene))
                {
                    hasValidScene = false;
                    break;
                }
            }
        }
        else
        {
            hasValidScene = false;
        }

        if (hasValidScene)
        {
            GUILayout.FlexibleSpace();

            GUIContent content = new(SceneManager.GetActiveScene().name);

            if (EditorGUILayout.DropdownButton(content, FocusType.Passive, GUILayout.Width(150)))
                PopupWindow.Show(buttonRect, new PopupSwitchScene());

            if (Event.current.type == EventType.Repaint)
                buttonRect = GUILayoutUtility.GetLastRect();
        }
    }
}

public class PopupSwitchScene : PopupWindowContent
{
    #region Variables
    private static string editorStartupPathScene;
    private static string oldEditorStartupPathScene;

    private Dictionary<string, List<EditorBuildSettingsScene>> editorScenes = new();

    private static bool disableStartupScene;
    private bool oldDisableStartupScene;

    private Vector2 scrollPos;
    private readonly float popupWidth = 250f;
    private float popupHeight;
    private readonly float scrollViewHeight = 130f;
    private readonly float headerHeight = 20f;
    private readonly float buttonHeight = 20f;
    private float scrollViewContentHeight;
    private float marginRight;

    private static readonly GUIStyle headerLabelStyle;
    private static readonly GUIStyle headerButtonStyle;
    private static readonly GUIStyle buttonStyle;
    #endregion

    private static string EditorStartupPathScene
    {
        get { return editorStartupPathScene; }
        set
        {
            editorStartupPathScene = value;
            EditorPrefs.SetString("EditorStartupPathScene", value);
        }
    }

    private static bool DisableStartupScene
    {
        get { return disableStartupScene; }
        set
        {
            disableStartupScene = value;
            EditorPrefs.SetBool("DisableStartupScene", value);
        }
    }

    static PopupSwitchScene()
    {
        headerLabelStyle = new(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            margin = new RectOffset()
        };

        headerButtonStyle = new GUIStyle(GUI.skin.button)
        {
            normal = { background = MakeTex(30, 20, new Color(0, 0, 0, 0)) },
            margin = new RectOffset(0, 0, 0, 0)
        };

        buttonStyle = new(GUI.skin.button);
    }

    public override void OnOpen()
    {
        base.OnOpen();
        Initialize();
    }

    private void Initialize()
    {
        editorScenes.Clear();
        foreach (EditorBuildSettingsScene editorScene in EditorBuildSettings.scenes)
        {
            if (!SceneIsValid(editorScene))
                continue;

            string relativePath = Path.GetRelativePath("Assets", editorScene.path);
            string directoryPath = Path.GetDirectoryName(relativePath);

            if (!editorScenes.ContainsKey(directoryPath))
            {
                editorScenes[directoryPath] = new List<EditorBuildSettingsScene> { editorScene };
                scrollViewContentHeight += headerHeight + buttonHeight;
            }
            else
            {
                if (!editorScenes[directoryPath].Contains(editorScene))
                {
                    editorScenes[directoryPath].Add(editorScene);
                    scrollViewContentHeight += buttonHeight;
                }
            }
        }

        disableStartupScene = EditorPrefs.GetBool("DisableStartupScene");
        editorStartupPathScene = EditorPrefs.GetString("EditorStartupPathScene");

        if (!disableStartupScene)
            SetPlayModeStartScene(editorStartupPathScene);
    }

    #region Rendering
    public override Vector2 GetWindowSize()
    {
        if (scrollViewContentHeight + 30 > scrollViewHeight)
        {
            marginRight = -11f;
            popupHeight = 155f;
        }
        else
        {
            marginRight = 2f;
            popupHeight = buttonHeight + scrollViewContentHeight + 30;
        }

        return new Vector2(popupWidth, popupHeight);
    }

    private static Texture2D MakeTex(int width, int height, Color col)
    {
        Color32[] pix = new Color32[width * height];
        for (int i = 0; i < pix.Length; ++i)
            pix[i] = col;

        Texture2D result = new(width, height);
        result.SetPixels32(pix);
        result.Apply();
        return result;
    }
    #endregion

    #region GUI
    public override void OnGUI(Rect rect)
    {
        GUILayout.BeginVertical();

        DisableStartupScene = GUILayout.Toggle(DisableStartupScene, "Disable startup scene", GUI.skin.button, GUILayout.Width(popupWidth - 8 + marginRight), GUILayout.Height(headerHeight - 2));
        if (DisableStartupScene != oldDisableStartupScene)
        {
            if (DisableStartupScene)
                ResetStartScene();
            else
                RevertStartScene();

            oldDisableStartupScene = DisableStartupScene;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollViewHeight));

        foreach (KeyValuePair<string, List<EditorBuildSettingsScene>> editorScene in editorScenes)
        {
            HeaderGUI(editorScene);
            SceneListGUI(editorScene);
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void HeaderGUI(KeyValuePair<string, List<EditorBuildSettingsScene>> editorScene)
    {
        GUILayout.BeginHorizontal(GUI.skin.box);

        if (!string.IsNullOrEmpty(editorScene.Key))
        {
            EditorGUILayout.LabelField(editorScene.Key, headerLabelStyle, GUILayout.Width(popupWidth - 45 + marginRight));

            if (GUILayout.Button(EditorGUIUtility.IconContent("Search On Icon"), headerButtonStyle, GUILayout.Width(26), GUILayout.Height(headerHeight - 2)))
            {
                if (editorScene.Value[0] != null)
                {
                    string scenePath = editorScene.Value[0].path;
                    string directoryPath = Path.GetDirectoryName(scenePath);
                    FocusDirectory(directoryPath);
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("Assets", headerLabelStyle, GUILayout.Width(popupWidth - 16 + marginRight));
        }

        GUILayout.EndHorizontal();
    }

    private void SceneListGUI(KeyValuePair<string, List<EditorBuildSettingsScene>> editorScene)
    {
        foreach (EditorBuildSettingsScene scene in editorScene.Value)
        {
            bool sceneIsStartup = EditorStartupPathScene == scene.path;
            string sceneName = Path.GetFileNameWithoutExtension(scene.path);

            GUILayout.BeginHorizontal();

            if (GUILayout.Toggle(sceneIsStartup, EditorGUIUtility.IconContent("PlayButton"), GUI.skin.button, GUILayout.Width(20), GUILayout.Height(buttonHeight)))
            {
                EditorStartupPathScene = scene.path;

                if (!DisableStartupScene)
                    SetPlayModeStartScene(scene.path);
            }

            if (scene.enabled)
            {
                if (GUILayout.Button(sceneName, GUILayout.Width(popupWidth - 32 + marginRight), GUILayout.Height(buttonHeight)))
                    OpenScene(scene.path);
            }
            else
            {
                buttonStyle.normal.textColor = new Color(255, 255, 255, .5f);
                buttonStyle.hover.textColor = new Color(255, 255, 255, .5f);

                if (GUILayout.Button(sceneName, buttonStyle, GUILayout.Width(popupWidth - 32 + marginRight), GUILayout.Height(buttonHeight)))
                    OpenScene(scene.path);
            }

            GUILayout.EndHorizontal();
        }
    }
    #endregion

    #region Utilities
    public static bool SceneIsValid(EditorBuildSettingsScene editorScene)
    {
        bool result;
        try
        {
            result = AssetDatabase.LoadAssetAtPath<SceneAsset>(editorScene.path);
        }
        catch
        {
            result = false;
        }
        return result;
    }

    private void FocusDirectory(string path)
    {
        EditorUtility.FocusProjectWindow();
        Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
        Selection.activeObject = obj;
        EditorGUIUtility.PingObject(obj);
    }

    private static void SetPlayModeStartScene(string scenePath)
    {
        if (EditorStartupPathScene != oldEditorStartupPathScene)
        {
            SceneAsset startupScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (startupScene != null)
                EditorSceneManager.playModeStartScene = startupScene;

            oldEditorStartupPathScene = EditorStartupPathScene;
        }
    }

    private void ResetStartScene()
    {
        if (EditorSceneManager.playModeStartScene != null)
            EditorSceneManager.playModeStartScene = null;
    }

    private void RevertStartScene()
    {
        SetPlayModeStartScene(editorStartupPathScene);
    }

    private void OpenScene(string path)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }
    #endregion
}