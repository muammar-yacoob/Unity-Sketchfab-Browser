using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GridPanel 
{
    private bool showDetails;
    //if (searchThumbs == null || searchThumbs.Count == 0) return;
        
    int rowCount = 6;
    int columnCount = 2;

    float panelHeight = 150f;
    float padding = 2f;
    private GUIStyle hyperlinkStyle;
    private GUIStyle labelStyle;

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

                labelStyle ??= new GUIStyle(GUI.skin.label) { normal = { textColor = Color.white } , alignment = TextAnchor.LowerCenter};
                hyperlinkStyle ??= new GUIStyle(GUI.skin.label) { normal = { textColor = Color.cyan } , alignment = TextAnchor.LowerCenter};

                // Background
                //EditorGUI.DrawRect(panelRect, Color.black);

                // Thumbnail
                Rect imageRect = new Rect(panelRect.x, panelRect.y, panelRect.width, panelHeight - 20f);

                if (GUI.Button(imageRect, GUIContent.none, GUIStyle.none))
                {
                    showDetails = !showDetails;
                }
                GUI.DrawTexture(imageRect, icon, ScaleMode.ScaleToFit);

                // Button
                Rect buttonRect = new Rect(panelRect.x, panelRect.y + panelRect.height - 20f, imageRect.width, 20f);
                
                // Description
                if (showDetails)
                {
                    EditorGUI.DrawRect(panelRect, new Color(0,0,0,0.7f));
                    Rect labelRect = new Rect(panelRect.x, panelRect.y, panelRect.width, 20f);
                    GUI.Label(labelRect, m.name, labelStyle);
                    labelRect.y += 20;
                    GUI.Label(labelRect, m.license.label, labelStyle);
                    labelRect.y += 20;
                    GUI.Label(labelRect, "Vertices: "+m.vertexCount.ToString("N0"), labelStyle);                
                    
                    
                    // labelRect.y += 20;
                    // GUI.Label(labelRect, "Materials: "+m.materialCount, labelStyle);     
                    //
                    // labelRect.y += 20;
                    // GUI.Label(labelRect, "Textures: "+m.textureCount, labelStyle);   
                    
                    if(m.animationCount > 0)
                    {
                        labelRect.y += 20;
                        GUI.Label(labelRect, "Animations: " + m.animationCount, labelStyle);
                    }
                    
                    if (GUI.Button(buttonRect, "More")) Application.OpenURL(m.viewerUrl);
                }
                else 
                {
                    string buttonText = m.price == 0 ? "Download" : $"Buy ${m.price}";
                    GUI.enabled = !SketchfabBrowser.Instance.CurrentModel.IsDownloading;
                    if (GUI.Button(buttonRect, buttonText))
                    {
                        SketchfabBrowser.Instance.DownloadModel(m.uid, m.name);
                    }
                    GUI.enabled = true;
                }

                thumbIndex++;
            }
            GUILayout.EndHorizontal();
            rect.height += panelHeight;
            // GUILayout.Space(padding + 60);
        }
    }
}
