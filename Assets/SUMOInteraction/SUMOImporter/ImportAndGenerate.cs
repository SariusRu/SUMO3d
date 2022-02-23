using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace SUMOInteraction
{
    public class ImportAndGenerate
    {
        static GameObject network;

        public static Dictionary<string, NetFileJunction> junctions;
        public static Dictionary<string, NetFileLane> lanes;
        public static Dictionary<string, NetFileEdge> edges;
        public static Dictionary<string, Shape> shapes;

        public static List<Vector3[]> polygons;

        static string sumoNetFile;
        static string sumoShapeFile;

        static float xmin;
        static float xmax;
        static float ymin;
        static float ymax;

        static Terrain Terrain;
        static TerrainData terrainData;

        static float numPlants;

        static float meshScaleX = 3.3f;
        static float uvScaleV = 5;
        static float uvScaleU = 1;

        static float junctionHeight = 0.01f;
        static float trafficLightDistance = 2f;
        static float minLengthForStreetLamp = 12;
        static float streeLampDistance = 6f;

        static Boolean grassEnabled = true;
        static Boolean treesEnabled = true;

        static bool additionalImported = false;
        static bool networkImported = false;

        private static bool fileExists(string file)
        {
            return File.Exists(file);
        }

        public static void parseNetworkFileLocation(string path)
        {
            string tmpSumoNetFile = path + "/osm.net.xml";
            string tmpSumoShapeFile = path + "/osm.poly.xml";

            parseNetworkFiles(tmpSumoNetFile, tmpSumoShapeFile);
        }

        public static void parseNetworkFiles(string netFile = "/osm.net.xml", string polyFile = "/osm.poly.xml")
        {
            sumoNetFile = netFile;
            sumoShapeFile = polyFile;

            network = new GameObject("StreetNetwork");

            if (fileExists(sumoNetFile))
            {

                lanes = new Dictionary<string, NetFileLane>();
                edges = new Dictionary<string, NetFileEdge>();
                junctions = new Dictionary<string, NetFileJunction>();
                shapes = new Dictionary<string, Shape>();

                netType networkFile;
                XmlSerializer serializer = new XmlSerializer(typeof(netType));
                FileStream fs = new FileStream(sumoNetFile, FileMode.Open);
                TextReader rd = new StreamReader(fs);



                try
                {
                    networkFile = (netType)serializer.Deserialize(rd);

                    foreach (junctionType junction in networkFile.junction)
                    {
                        if (junction.type != junctionTypeType.@internal)
                        {
                            NetFileJunction j = new NetFileJunction(junction.id, junction.type, junction.x, junction.y, junction.z, junction.incLanes, junction.shape);

                            // Add to global list
                            if (!junctions.ContainsKey(j.id))
                                junctions.Add(j.id, j);
                        }
                    }

                    foreach (edgeType edge in networkFile.edge)
                    {
                        if (!edge.functionSpecified)
                        {
                            // Only non-internal edges
                            NetFileEdge e = new NetFileEdge(edge.id, edge.from, edge.to, edge.priority, edge.shape);

                            // Add to global list
                            if (!edges.ContainsKey(edge.id))
                                edges.Add(edge.id, e);

                            foreach (object l in edge.Items)
                            {
                                if (l.GetType() == typeof(laneType))
                                {
                                    laneType laneT = (laneType)l;
                                    // Add all lanes which belong to this edge
                                    e.addLane(laneT.id, laneT.index, laneT.speed, laneT.length, laneT.shape);
                                }
                            }
                        }
                    }

                    string[] boundaries = networkFile.location.convBoundary.Split(',');
                    xmin = float.Parse(boundaries[0]);
                    ymin = float.Parse(boundaries[1]);
                    xmax = float.Parse(boundaries[2]);
                    ymax = float.Parse(boundaries[3]);
                    networkImported = true;
                    fs.Close();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                    networkImported = false;
                    fs.Close();
                }
            }
            if (fileExists(polyFile))
            {
                // Now import polygons/shapes for buildings information
                additionalType additional;
                XmlSerializer serializer = new XmlSerializer(typeof(additionalType));
                FileStream fs = new FileStream(sumoShapeFile, FileMode.Open);
                StreamReader rd = new StreamReader(fs);
                try
                {
                    additional = (additionalType)serializer.Deserialize(rd);

                    // Get all junctions and preinstanciate lanes
                    foreach (object item in additional.Items)
                    {
                        if (item.GetType() == typeof(polygonType))
                        {
                            Shape shape = new Shape();
                            polygonType polygon = (polygonType)item;
                            foreach (String s in polygon.shape.Split(' '))
                            {
                                shape.addCoordPair(Convert.ToDouble(s.Split(',')[0]), Convert.ToDouble(s.Split(',')[1]));
                            }
                            shape.removeLastCoordPairAndFixOrder();
                            shapes.Add(polygon.id, shape);
                        }
                    }
                    additionalImported = true;
                    fs.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    additionalImported = false;
                    fs.Close();
                }
            }
        }

        public static void drawStreetNetwork(string sumoFilePath)
        {
            if (!networkImported || !additionalImported)
            {
                parseNetworkFileLocation(sumoFilePath);
            }

            RoadNetwork roads = new RoadNetwork(network, junctions, lanes, edges, xmin, xmax, ymin, ymax);
            roads.GenerateRoad();
        }

        public static void generateTerrain(string sumoFilePath, bool treesEnabled = true, bool grassEnabled = true)
        {
            //if(!networkImported || !additionalImported)
            //{
            parseNetworkFileLocation(sumoFilePath);
            //}

            RandomGenerator terrain = new RandomGenerator(treesEnabled, grassEnabled, xmin, xmax, ymin, ymax, polygons);
            terrain.GenerateTerrain();
        }

    }




}
