using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SparkGames.SketchfabBrowser.Editor
{
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
        private Vector2 scrollPosition;

        public void Draw(float w, Model[] models, List<SearchThumb> searchThumbs)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            float panelWidth = (w - 100) / columnCount;
            Rect rect = new Rect(0, 0, panelWidth, 0);
            int thumbIndex = 0;
            for (int row = 0; row < rowCount; row++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(padding);
                for (int col = 0; col < columnCount; col++)
                {
                    if (thumbIndex >= searchThumbs.Count)
                    {
                        GUILayout.EndHorizontal(); 
                        EditorGUILayout.EndScrollView();
                        return; 
                    }

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
                        EditorGUI.DrawRect(panelRect, new Color(0,0,0,0.6f));
                        Rect labelRect = new Rect(panelRect.x, panelRect.y, panelRect.width, 20f);
                        labelRect.y += (m.animationCount == 0) ? 25 : 15;
                        
                        GUI.Label(labelRect, m.name, labelStyle);
                        labelRect.y += 20;
                        GUI.Label(labelRect, m.license.label, labelStyle);
                        labelRect.y += 20;
                        GUI.Label(labelRect, "Vertices: "+m.vertexCount.ToString("N0"), labelStyle);                
                    
                        if(m.animationCount > 0)
                        {
                            labelRect.y += 20;
                            GUI.Label(labelRect, "Animations: " + m.animationCount, labelStyle);
                        }
                    
                        if (GUI.Button(buttonRect, "More")) Application.OpenURL(m.viewerUrl);
                    }
                    else 
                    {
                        if (m.IsDownloading == false)
                        {

                            string buttonText = m.price == 0 ? "Download" : $"Buy ${m.price}";
                            //GUI.enabled = !SketchfabBrowser.Instance.CurrentModel.IsDownloading;
                        
                            if(m.IsDownloaded)
                            {
                                buttonText = "Downloaded";
                                GUI.enabled = false;
                            }
                            if (GUI.Button(buttonRect, buttonText))
                            {
                              _ = ModelDownloader.Instance.DownloadModel(m, onDownloadProgress: (p) => m.DownloadProgress = p);
                            }
                            GUI.enabled = true;
                        }
                        else
                        {
                            DrawProgressBar(m.DownloadProgress, buttonRect);
                        }

                        GUI.enabled = true;
                    }

                    thumbIndex++;
                }
                GUILayout.EndHorizontal();
                rect.height += panelHeight;
            }
            EditorGUILayout.EndScrollView();
        }

    
        private void DrawProgressBar(float percent, Rect buttonRect)
        {
            string percentString = (percent * 100).ToString("N0") + "%";
            EditorGUI.ProgressBar(buttonRect, percent, percentString);
            //Debug.Log(percentString);
        }
    }
}
