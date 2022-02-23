using System;
using UnityEditor;
using UnityEngine;

namespace SUMOInteraction
{
    class TerrainWindow : EditorWindow
    {
        static string sumoFilesPath;

        //Window Properties
        bool terrain = true;
        bool trees = true;
        bool grass = true;


        [MenuItem("SUMO/Import Terrain")]
        public static void ShowTerrainWindow()
        {
            if (FileChecker.CheckFiles())
            {
                sumoFilesPath = Application.dataPath;
                sumoFilesPath = sumoFilesPath.Substring(0, sumoFilesPath.LastIndexOf("/")); // Assets
                sumoFilesPath = sumoFilesPath.Substring(0, sumoFilesPath.LastIndexOf("/")); // Project Folder
                //sumoFilesPath += "C:/";
                sumoFilesPath = "C:\\Sam\\SUMO\\tools\\MU";
                sumoFilesPath.Replace('/', '\\');
                UnityEditor.EditorWindow.GetWindow(typeof(TerrainWindow));
            }
        }


        void OnGUI()
        {
            GUILayout.Label("Terrain + Street Network Generation Settings", EditorStyles.boldLabel);
            terrain = EditorGUILayout.Toggle("Generate Terrain?", terrain);
            trees = EditorGUILayout.Toggle("Generate Trees?", trees);
            grass = EditorGUILayout.Toggle("Generate Grass?", grass);

            sumoFilesPath = EditorGUILayout.TextField("SUMO Files", sumoFilesPath);
            if (GUILayout.Button("Change Folder Location"))
            {
                sumoFilesPath = EditorUtility.OpenFolderPanel("Chose the folder containing the SUMO files (map.edg.xml, map.nod.xml, map.net.xml, map.rou.xml)", Application.dataPath, "");
                EditorGUILayout.TextField("SUMO Files", sumoFilesPath);
            }
            GUILayout.Space(16);

            GUILayout.Label("Processing", EditorStyles.boldLabel);
            if (GUILayout.Button("Start"))
            {

                if (terrain)
                {
                    EditorUtility.DisplayProgressBar("Generation Progress", "Generating Terrain, Trees, Grass", 0.0f);
                    ImportAndGenerate.generateTerrain(sumoFilesPath, trees, grass);
                }

                EditorUtility.ClearProgressBar();

                this.Close();
            }

            GUILayout.Space(16);

        }

        void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
