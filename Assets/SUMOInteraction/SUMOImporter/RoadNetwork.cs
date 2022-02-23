using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace SUMOInteraction
{
    public class RoadNetwork
    {
        private GameObject network;
        private float xmin;
        private float xmax;
        private float ymin;
        private float ymax;
        private readonly List<Vector3[]> polygons;
        private readonly bool linearOption;
        private int laneCounter;
        private readonly int streetLightCounter;
        private readonly float streeLampDistance = 6f;
        private readonly float minLengthForStreetLamp = 12;

        private readonly float trafficLightDistance = 2f;

        Dictionary<string, NetFileJunction> junctions;
        Dictionary<string, NetFileLane> lanes;
        Dictionary<string, NetFileEdge> edges;

        public RoadNetwork(GameObject network, Dictionary<string, NetFileJunction> junctions, Dictionary<string, NetFileLane> lanes, Dictionary<string, NetFileEdge> edges, float xmin, float xmax, float ymin, float ymax)
        {
            polygons = new List<Vector3[]>();
            linearOption = true;
            laneCounter = 0;
            streetLightCounter = 0;
            this.junctions = junctions;
            this.lanes = lanes;
            this.edges = edges;
            this.network = network;
            this.xmin = xmin;
            this.xmax = xmax;
            this.ymin = ymin;
            this.ymax = ymax;
        }

        public void GenerateRoad()
        {
            Debug.Log("Inserting 3d Streets");
            DrawEdges();
            DrawJunctions();
            DrawTrafficLights();



        }

        private void DrawEdges()
        {
            if (edges != null)
            {

                int streetLightCounter = 0;

                Debug.Log("Inserting 3d Junctions");

                int junctionCounter = 0;
                foreach (NetFileEdge j in edges.Values)
                {
                    foreach(NetFileLane l in j.getLanes())
                    {
                        List<int> indices = new List<int>();
                        Vector2[] vertices2D = new Vector2[l.shape.Count];

                        for (int i = 0; i < l.shape.Count; i++)
                        {
                            vertices2D[i] = new Vector3((float)(l.shape[i])[0] - xmin, (float)(l.shape[i])[1] - ymin);
                        }

                        // Use the triangulator to get indices for creating triangles
                        Triangulator tr = new Triangulator(vertices2D);
                        List<int> bottomIndices = new List<int>(tr.Triangulate());
                        indices.AddRange(bottomIndices);

                        // Create the Vector3 vertices
                        Vector3[] vertices = new Vector3[vertices2D.Length];
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            vertices[i] = new Vector3(vertices2D[i].x, 0, vertices2D[i].y);
                        }

                        Mesh mesh = new Mesh();
                        mesh.Clear();
                        mesh.vertices = vertices;
                        mesh.triangles = indices.ToArray();
                        mesh.RecalculateNormals();
                        mesh.RecalculateBounds();

                        Bounds bounds = mesh.bounds;
                        Vector2[] uvs = new Vector2[vertices.Length];
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            uvs[i] = new Vector2(vertices[i].x / bounds.size.x, vertices[i].z / bounds.size.z);
                        }
                        mesh.uv = uvs;
                        mesh.RecalculateNormals();
                        mesh.RecalculateBounds();

                        // Set up game object with mesh;
                        GameObject junction3D = new GameObject("lane_" + junctionCounter++);
                        MeshRenderer r = (MeshRenderer)junction3D.AddComponent(typeof(MeshRenderer));
                        Material material = AssetDatabase.LoadMainAssetAtPath(PathConstants.pathJunctionMaterial) as Material;
                        r.material = material;
                        MeshFilter filter = junction3D.AddComponent(typeof(MeshFilter)) as MeshFilter;
                        filter.mesh = mesh;
                        junction3D.transform.SetParent(network.transform);

                        // (3.1) Add junctions to polygon list for tree placement check
                        polygons.Add(vertices);
                    }
                }

                //foreach (NetFileEdge e in edges.Values)
                //{
                //    int edgeCounter = 0;

                //    //Create new GameObject and set the network as father
                //    GameObject spline = new GameObject("StreetSegment_" + laneCounter++);
                //    spline.transform.SetParent(network.transform);

                //    Spline splineObject = spline.AddComponent<Spline>();

                //    if (linearOption)
                //        splineObject.interpolationMode = Spline.InterpolationMode.Linear;
                //    else
                //        splineObject.interpolationMode = Spline.InterpolationMode.BSpline;

                //    foreach (NetFileLane l in e.getLanes())
                //    {
                //        foreach (double[] coordPair in l.shape)
                //        {
                //            // Add Node
                //            GameObject splineNode = new GameObject("Node_" + edgeCounter++);
                //            splineNode.transform.SetParent(spline.transform);
                //            SplineNode splineNodeObject = splineNode.AddComponent<SplineNode>();
                //            splineNode.transform.position = new Vector3((float)coordPair[0] - xmin, 0, (float)coordPair[1] - ymin);
                //            splineObject.splineNodesArray.Add(splineNodeObject);
                //        }

                //        // Add meshes
                //        Material material = AssetDatabase.LoadAssetAtPath<Material>(PathConstants.pathRoadMaterial);
                //        MeshRenderer mRenderer = mRenderer = spline.GetComponent<MeshRenderer>();
                //        if (mRenderer == null)
                //        {
                //            mRenderer = spline.AddComponent<MeshRenderer>();
                //        }
                //        mRenderer.material = material;

                //        SplineMesh sMesh = spline.AddComponent<SplineMesh>();
                //        sMesh.spline = splineObject;
                //        sMesh.baseMesh = AssetDatabase.LoadAssetAtPath<Mesh>(PathConstants.pathSuperSplinesBox);
                //        sMesh.startBaseMesh = AssetDatabase.LoadAssetAtPath<Mesh>(PathConstants.pathSuperSplinesBox);
                //        sMesh.endBaseMesh = AssetDatabase.LoadAssetAtPath<Mesh>(PathConstants.pathSuperSplinesBox);
                //        sMesh.uvScale = new Vector2(uvScaleU, uvScaleV);
                //        sMesh.xyScale = new Vector2(meshScaleX, 0);




                //        //// (1.1) Add Lanes to polygon list for tree placement check
                //        for (int i = 0; i < l.shape.Count - 1; i++)
                //        {
                //            double length = Math.Sqrt(Math.Pow(l.shape[i][0] - xmin - (l.shape[i + 1][0] - xmin), 2) + Math.Pow(l.shape[i][1] - ymin - (l.shape[i + 1][1] - ymin), 2));
                //            // Calc the position (in line with the lane)
                //            float x1 = (float)l.shape[i][0] - xmin;
                //            float y1 = (float)l.shape[i][1] - ymin;
                //            float x2 = (float)l.shape[i + 1][0] - xmin;
                //            float y2 = (float)l.shape[i + 1][1] - ymin;
                //            double Dx = x2 - x1;
                //            double Dy = y2 - y1;
                //            double D = Math.Sqrt(Dx * Dx + Dy * Dy);
                //            double W = 10;
                //            Dx = 0.5 * W * Dx / D;
                //            Dy = 0.5 * W * Dy / D;
                //            Vector3[] polygon = new Vector3[] { new Vector3((float)(x1 - Dy), 0, (float)(y1 + Dx)),
                //                                        new Vector3((float)(x1 + Dy), 0, (float)(y1 - Dx)),
                //                                        new Vector3((float)(x2 + Dy), 0, (float)(y2 - Dx)),
                //                                        new Vector3((float)(x2 - Dy), 0, (float)(y2 + Dx)) };
                //            polygons.Add(polygon);


                //            // (2) Add Street Lamps (only if long enough)
                //            if (length >= minLengthForStreetLamp)
                //            {
                //                streetLightCounter = addLight(y2, y1, x2, x1, length, streetLightCounter);
                //            }
                //        }

                //    }

                //    //splineObject.nodes.RemoveAt(0);
                //    //splineObject.nodes.RemoveAt(splineObject.nodes.Count-1);
                //}
            }
        }

        private int addLight(float y2, float y1, float x2, float x1, double length, int streetLightCounter)
        {
            float angle = Mathf.Atan2(y2 - y1, x2 - x1) * 180 / Mathf.PI;

            // Allway located at the middle of a street
            double ratioRotPoint = 0.5;
            double ratio = 0.5 + streeLampDistance / length;
            float xDest = (float)((1 - ratio) * x1 + ratio * x2);
            float yDest = (float)((1 - ratio) * y1 + ratio * y2);

            float xRotDest = (float)((1 - ratioRotPoint) * x1 + ratioRotPoint * x2);
            float yRotDest = (float)((1 - ratioRotPoint) * y1 + ratioRotPoint * y2);

            GameObject streetLampPrefab = AssetDatabase.LoadMainAssetAtPath(PathConstants.pathLight) as GameObject;
            GameObject streetLamp = GameObject.Instantiate(streetLampPrefab, new Vector3(xDest, 0, yDest), Quaternion.Euler(new Vector3(0, 0, 0)));
            streetLightCounter++;
            streetLamp.name = "StreetLight_" + streetLightCounter;
            streetLamp.transform.SetParent(network.transform);
            streetLamp.transform.RotateAround(new Vector3(xRotDest, 0, yRotDest), Vector3.up, -90.0f);
            streetLamp.transform.Rotate(Vector3.up, -angle);
            return streetLightCounter;
        }

        private void DrawJunctions()
        {
            Debug.Log("Inserting 3d Junctions");

            int junctionCounter = 0;
            foreach (NetFileJunction j in junctions.Values)
            {
                List<int> indices = new List<int>();

                Vector2[] vertices2D = new Vector2[j.shape.Count];
                for (int i = 0; i < j.shape.Count; i++)
                {
                    vertices2D[i] = new Vector3((float)(j.shape[i])[0] - xmin, (float)(j.shape[i])[1] - ymin);
                }

                // Use the triangulator to get indices for creating triangles
                Triangulator tr = new Triangulator(vertices2D);
                List<int> bottomIndices = new List<int>(tr.Triangulate());
                indices.AddRange(bottomIndices);


                // Create the Vector3 vertices
                Vector3[] vertices = new Vector3[vertices2D.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = new Vector3(vertices2D[i].x, 0, vertices2D[i].y);
                }

                Mesh mesh = new Mesh();
                mesh.Clear();
                mesh.vertices = vertices;
                mesh.triangles = indices.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                Bounds bounds = mesh.bounds;
                Vector2[] uvs = new Vector2[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    uvs[i] = new Vector2(vertices[i].x / bounds.size.x, vertices[i].z / bounds.size.z);
                }
                mesh.uv = uvs;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                // Set up game object with mesh;
                GameObject junction3D = new GameObject("junction_" + junctionCounter++);
                MeshRenderer r = (MeshRenderer)junction3D.AddComponent(typeof(MeshRenderer));
                Material material = AssetDatabase.LoadMainAssetAtPath(PathConstants.pathJunctionMaterial) as Material;
                r.material = material;
                MeshFilter filter = junction3D.AddComponent(typeof(MeshFilter)) as MeshFilter;
                filter.mesh = mesh;
                junction3D.transform.SetParent(network.transform);

                // (3.1) Add junctions to polygon list for tree placement check
                polygons.Add(vertices);
            }
        }

        private void DrawTrafficLights()
        {
            Debug.Log("Inserting 3d Traffic Lights");

            foreach (NetFileJunction j in junctions.Values)
            {
                if (j.type == junctionTypeType.traffic_light)
                {
                    int index = 0;
                    foreach (NetFileLane l in j.incLanes)
                    {
                        // Calc the position (in line with the lane)
                        float x1 = (float)l.shape[0][0] - xmin;
                        float y1 = (float)l.shape[0][1] - ymin;
                        float x2 = (float)l.shape[1][0] - xmin;
                        float y2 = (float)l.shape[1][1] - ymin;
                        float length = (float)Math.Sqrt(Math.Pow(y2 - y1, 2) + Math.Pow(x2 - x1, 2));
                        float angle = Mathf.Atan2(y2 - y1, x2 - x1) * 180 / Mathf.PI;

                        double ratio = (length - trafficLightDistance) / length;

                        float xDest = (float)((1 - ratio) * x1 + ratio * x2);
                        float yDest = (float)((1 - ratio) * y1 + ratio * y2);

                        // Insert the 3d object, rotate from lane 90° to the right side and then orientate the traffic light towards the vehicles
                        GameObject trafficLightPrefab = AssetDatabase.LoadMainAssetAtPath(PathConstants.pathTrafficLight) as GameObject;
                        GameObject trafficLight = GameObject.Instantiate(trafficLightPrefab, new Vector3(xDest, 0, yDest), Quaternion.Euler(new Vector3(0, 0, 0)));
                        trafficLight.name = "TrafficLight_" + j.id;
                        trafficLight.transform.SetParent(network.transform);
                        trafficLight.transform.RotateAround(new Vector3(x2, 0, y2), Vector3.up, -90.0f);
                        trafficLight.transform.Rotate(Vector3.up, -angle);

                        // Insert traffic light index as empty GameObject into traffic light
                        GameObject TLindex = new GameObject("index");
                        GameObject TLindexVal = new GameObject(Convert.ToString(index++));
                        TLindexVal.transform.SetParent(TLindex.transform);
                        TLindex.transform.SetParent(trafficLight.transform);
                    }
                }
            }
        }
    }
}

