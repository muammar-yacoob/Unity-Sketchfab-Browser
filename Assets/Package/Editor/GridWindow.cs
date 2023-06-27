using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GridPanel 
{
    private bool showDetails;
    //if (searchThumbs == null || searchThumbs.Count == 0) return;
        
    int rowCount = 6;
    int columnCount = 2;

    float panelHeight = 100f;
    float padding = 2f;

    public void Draw(float w, SketchfabBrowser.Model[] models, List<SearchThumb> searchThumbs)
    {
        float panelWidth = (w - 100) / columnCount;
        Rect rect = new Rect(0, 0, panelWidth, 0);
        int thumbIndex = 0;
        for (int row = 0; row < rowCount; row++)
        {
            GUILayout.BeginHorizontal();
            // GUILayout.Space(padding);
            for (int col = 0; col < columnCount; col++)
            {
                if (thumbIndex >= searchThumbs.Count) break;

                var m = models[thumbIndex];
                var icon = searchThumbs[thumbIndex].thumb;

                Rect panelRect = GUILayoutUtility.GetRect(panelWidth, panelHeight);

                GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
                labelStyle.normal.textColor = Color.white;
                labelStyle.alignment = TextAnchor.LowerCenter;

                // Background
                EditorGUI.DrawRect(panelRect, Color.blue);

                // Thumbnail
                Rect imageRect = new Rect(panelRect.x, panelRect.y, panelRect.width, 80);

                if (GUI.Button(imageRect, GUIContent.none, GUIStyle.none))
                {
                    showDetails = !showDetails;
                }
                GUI.DrawTexture(imageRect, icon, ScaleMode.ScaleToFit);

                // Button
                Rect buttonRect = new Rect(panelRect.x, panelRect.y + panelRect.height - 20f, panelRect.width, 20f);
                if (GUI.Button(buttonRect, "Download"))
                {
                    Debug.Log("Download");
                }

                // Description
                if (showDetails)
                {
                    Rect labelRect = new Rect(panelRect.x, panelRect.y + panelRect.height - 40f, panelRect.width, 20f);
                    GUI.Label(labelRect, models[thumbIndex].name, labelStyle);
                }

                thumbIndex++;
            }
            GUILayout.EndHorizontal();
            rect.height += panelHeight;
            // GUILayout.Space(padding + 60);
        }
    }
}
