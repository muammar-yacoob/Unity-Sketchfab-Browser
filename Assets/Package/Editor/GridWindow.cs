using UnityEditor;
using UnityEngine;

public class GridWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private GridPanel panelDrawer;

    [MenuItem("Window/Scrollable Grid")]
    public static void ShowWindow()
    {
        GetWindow<GridWindow>("Scrollable Grid");
    }

    private void OnEnable()
    {
        panelDrawer = new GridPanel();
    }

    private void OnGUI()
    {
        int rowCount = 20;
        int columnCount = 3;
        float panelWidth = (position.width -60) / columnCount;
        float panelHeight = 60f;
        float padding = 10f;

        GUILayout.Space(padding);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        for (int row = 0; row < rowCount; row++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(padding);
            for (int col = 0; col < columnCount; col++)
            {
                Rect panelRect = GUILayoutUtility.GetRect(panelWidth, panelHeight);
                panelDrawer.Draw(panelRect, row, col);
                GUILayout.Space(padding);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(padding);
        }

        EditorGUILayout.EndScrollView();
    }
}

public class GridPanel
{
    public void Draw(Rect rect, int row, int col)
    {
        EditorGUI.DrawRect(rect, Color.blue);

        GUIContent labelContent = new GUIContent($"Panel {row}-{col}");
        GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.normal.textColor = Color.white;
        labelStyle.alignment = TextAnchor.MiddleCenter;

        EditorGUI.LabelField(rect, labelContent, labelStyle);
    }
}
