using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityToolbarExtender;

[InitializeOnLoad]
public static class ClearConsole
{
	static bool m_enabled;

	static bool Enabled
	{
		get { return m_enabled; }
		set
		{
			m_enabled = value;
			EditorPrefs.SetBool("ClearConsoleOnSceneChanged", value);
		}
	}

	static ClearConsole()
	{
		m_enabled = EditorPrefs.GetBool("ClearConsoleOnSceneChanged", false);
		EditorApplication.playModeStateChanged += OnPlayModeChanged;

		ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
	}

	static void OnPlayModeChanged(PlayModeStateChange playModeState)
	{
		if (playModeState == PlayModeStateChange.EnteredPlayMode)
		{
			EditorSceneManager.activeSceneChanged += OnActiveSceneChanged;
		}
		else if (playModeState == PlayModeStateChange.ExitingPlayMode)
		{
			EditorSceneManager.activeSceneChanged -= OnActiveSceneChanged;
		}
	}

	private static void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
	{
		if (Enabled)
		{
			ClearLog();
		}
	}

	static void ClearLog()
	{
		Assembly assembly = Assembly.GetAssembly(typeof(Editor));
		Type type = assembly.GetType("UnityEditor.LogEntries");
		MethodInfo method = type.GetMethod("Clear");
		method.Invoke(new object(), null);
	}

	static void OnToolbarGUI()
	{
		GUI.changed = false;

		GUILayout.Toggle(m_enabled, new GUIContent("Clear Console", "Clear Console on scene changed during play mode"), GUI.skin.GetStyle("Button"), GUILayout.Width(150));
		if (GUI.changed)
		{
			Enabled = !Enabled;
		}
	}
}