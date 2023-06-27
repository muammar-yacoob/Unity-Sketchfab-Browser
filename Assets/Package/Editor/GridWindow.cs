using UnityEditor;
using UnityEngine;

public class GridPanel
{
    private bool showDetails;

    public void Draw(Rect rect, int row, int col, Texture2D image, SketchfabBrowser.Model modelInfo)
    {
        if (showDetails) rect.height += 60f;
        
        GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.normal.textColor = Color.white;
        labelStyle.alignment = TextAnchor.LowerCenter;
        
        // Background
        //EditorGUI.DrawRect(rect, Color.gray);

        // Thumbnail
        Rect imageRect = new Rect(rect.x, rect.y, rect.width,  80);
        
        if (GUI.Button(imageRect, GUIContent.none, GUIStyle.none)) showDetails = !showDetails;
        GUI.DrawTexture(imageRect, image, ScaleMode.ScaleToFit);

        // Button
        Rect buttonRect = new Rect(rect.x, rect.y + rect.height - 20f, rect.width, 20f);
        if (GUI.Button(buttonRect, "Download"))
        {
            // Button click logic here
        }
        
        // Description
        if (!showDetails) return;
        Rect labelRect = new Rect(rect.x, rect.y + rect.height - 40f, rect.width, 20f);
        GUI.Label(labelRect, modelInfo.name, labelStyle);
    }
}
