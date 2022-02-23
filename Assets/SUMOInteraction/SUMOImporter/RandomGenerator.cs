using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SUMOInteraction
{
    internal class RandomGenerator : TerrainGenerator
    {
        private readonly bool treesEnabled;
        private readonly bool grassEnabled;
        private readonly float xmin;
        private readonly float xmax;
        private readonly float ymin;
        private readonly float ymax;
        private readonly List<Vector3[]> polygons;
        private readonly float scaleLength = 2;
        private readonly float scaleWidth = 2;
        private readonly int resolution = 2048;



        private int length;
        private int width;
        private float x;
        private float y;
        private int height;
        private TerrainData terrainData;
        private Terrain Terrain;
        private int numPlants;
        private Texture2D FlatTexture;
        private Texture2D SteepTexture;



        public RandomGenerator(bool treesEnabled, bool grassEnabled, float xmin, float xmax, float ymin, float ymax, List<Vector3[]> polygons)
        {
            this.treesEnabled = treesEnabled;
            this.grassEnabled = grassEnabled;

            this.grassEnabled = false;


            this.xmin = xmin;
            this.xmax = xmax;
            this.ymin = ymin;
            this.ymax = ymax;
            this.polygons = polygons;
        }

        public void GenerateTerrain()
        {
            GenerateDimensions();
            AmountPlants();
            LoadTextures();
            NewTerrain();
            PopulateTerrain();
            TexturizeTerrain();
            InsertTerrain();
            if (treesEnabled)
            {
                InsertTrees();
            }
            if (grassEnabled)
            {
                InsertGras();
            }

            Terrain.Flush();

        }

        private void InsertGras()
        {
            DetailPrototype grass = new DetailPrototype
            {
                prototypeTexture = (Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D>(PathConstants.pathGrassPrototypeTexture2D),
                minHeight = 1,
                minWidth = 1,
                maxHeight = 2,
                maxWidth = 2,
                noiseSpread = 0.1f,
                dryColor = new Color(0.804f, 0.737f, 0.102f, 1.0f),
                healthyColor = new Color(0.263f, 0.976f, 0.165f, 1.0f)
            };

            terrainData.detailPrototypes = new DetailPrototype[] { grass };
            terrainData.SetDetailResolution(256, 8);

            int detailCountPerDetailPixel = 6;
            int detailIndexToMassPlace = 0;
            int alphamapWidth = terrainData.alphamapWidth;
            int alphamapHeight = terrainData.alphamapHeight;
            int detailWidth = terrainData.detailResolution;
            int detailHeight = detailWidth;
            int[,] newDetailLayer = new int[detailWidth, detailHeight];
            int delta = 1;

            for (int j = 0; j < detailWidth; j++)
            {
                for (int k = 0; k < detailHeight; k++)
                {
                    float[] coord = GetCornerCoordinatesFromPixel(j, k, detailWidth, detailHeight, Terrain.gameObject.transform.position.x, Terrain.gameObject.transform.position.z, terrainData.size.x, terrainData.size.z);
                    // Check if there is a street/junction at the coordinate
                    Boolean occupied = false;
                    foreach (Vector3[] polygon in polygons)
                    {
                        // Check sorrounding of coordinate
                        occupied = InPolyChecker.IsPointInPolygon(coord[0], coord[1], polygon) ||
                                        InPolyChecker.IsPointInPolygon(coord[0] - delta, coord[1] - delta, polygon) ||
                                        InPolyChecker.IsPointInPolygon(coord[0] - delta, coord[1], polygon) ||
                                        InPolyChecker.IsPointInPolygon(coord[0] - delta, coord[1] + delta, polygon) ||
                                        InPolyChecker.IsPointInPolygon(coord[0] + delta, coord[1] - delta, polygon) ||
                                        InPolyChecker.IsPointInPolygon(coord[0] + delta, coord[1], polygon) ||
                                        InPolyChecker.IsPointInPolygon(coord[0] + delta, coord[1] + delta, polygon);
                        if (occupied)
                            break;
                    }
                    if (!occupied)
                    {
                        newDetailLayer[j, k] = detailCountPerDetailPixel;
                    }
                }
            }
            terrainData.SetDetailLayer(0, 0, detailIndexToMassPlace, newDetailLayer);
        }

        private void InsertTrees()
        {
            Debug.Log("Inserting 3d Plants");

            System.Random rand = new System.Random();

            List<TreePrototype> tpa = new List<TreePrototype>();

            for (int i = 0; i < PathConstants.pathPlants.Length; i++)
            {
                TreePrototype tp = new TreePrototype
                {
                    prefab = AssetDatabase.LoadMainAssetAtPath(PathConstants.pathPlants[i]) as GameObject
                };
                tpa.Add(tp);
            }
            terrainData.treePrototypes = tpa.ToArray();


            if (polygons != null)
            {
                for (int i = 0; i < numPlants; i++)
                {
                    float xnRel = (float)rand.NextDouble();
                    float xn = terrainData.size.x * xnRel;
                    float ynRel = (float)rand.NextDouble();
                    float yn = terrainData.size.z * ynRel;

                    float xGlobal = Terrain.transform.TransformPoint(new Vector3(xn, 0, yn)).x;
                    float yGlobal = Terrain.transform.TransformPoint(new Vector3(xn, 0, yn)).z;

                    Boolean freeSpotFound = false;
                    int counter = 0;


                    while (!freeSpotFound && counter < 10)
                    {
                        // Try 10 random positions, if all fail, then discard this plant
                        foreach (Vector3[] polygon in polygons)
                        {
                            freeSpotFound = !InPolyChecker.IsPointInPolygon(xGlobal, yGlobal, polygon);
                            if (!freeSpotFound)
                                break;
                        }
                        if (!freeSpotFound)
                        {
                            xnRel = (float)rand.NextDouble();
                            xn = terrainData.size.x * xnRel;
                            ynRel = (float)rand.NextDouble();
                            yn = terrainData.size.z * ynRel;

                            xGlobal = Terrain.transform.TransformPoint(new Vector3(xn, 0, yn)).x;
                            yGlobal = Terrain.transform.TransformPoint(new Vector3(xn, 0, yn)).z;

                            counter++;
                            break;
                        }

                    }

                    if (freeSpotFound)
                    {
                        TreeInstance tree = new TreeInstance
                        {
                            position = new Vector3(xnRel, 0, ynRel),
                            color = Color.white,
                            lightmapColor = Color.white,
                            prototypeIndex = rand.Next(0, PathConstants.pathPlants.Length),
                            heightScale = 1,
                            widthScale = 1
                        };
                        Terrain.AddTreeInstance(tree);
                    }
                }
            }
        }

        private void GenerateDimensions()
        {
            this.length = (int)((xmax - xmin) * scaleLength);
            this.width = (int)((ymax - ymin) * scaleWidth);
            this.x = xmin - (xmax - xmin) / 2 - xmin;
            this.y = ymin - (ymax - ymin) / 2 - ymin;

            this.height = (length < 250 || width < 250) ? 30 : 80;
        }

        private void AmountPlants()
        {
            this.numPlants = (int)(length * width / 20);
            if (this.numPlants < 100)
            {
                this.numPlants = 500;
            }
        }

        private void LoadTextures()
        {
            FlatTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PathConstants.pathGrassTexture2D);
            SteepTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PathConstants.pathRockTexture2D);
        }

        private void NewTerrain()
        {
            terrainData = new TerrainData
            {
                heightmapResolution = resolution,
                alphamapResolution = resolution
            };
        }

        private void PopulateTerrain()
        {
            var heightmap = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
            System.Random rand = new System.Random();

            for (var zRes = 0; zRes < terrainData.heightmapResolution; zRes++)
            {
                for (var xRes = 0; xRes < terrainData.heightmapResolution; xRes++)
                {
                    // Reduce factor
                    float distX = 0;
                    float distY = 0;
                    float marginStart = 20;
                    float marginEnd = 50;
                    float reduceFactor = 0;
                    float constHeight = 0.5f;

                    if (Math.Abs(terrainData.heightmapResolution - xRes) < xRes)
                    {
                        distX = Math.Abs(terrainData.heightmapResolution - xRes);
                    }
                    else
                    {
                        distX = xRes;
                    }
                    if (Math.Abs(terrainData.heightmapResolution - zRes) < zRes)
                    {
                        distY = Math.Abs(terrainData.heightmapResolution - zRes);
                    }
                    else
                    {
                        distY = zRes;
                    }

                    if (distX > marginStart && distY > marginStart && distX <= marginEnd && distY <= marginEnd)
                    {
                        reduceFactor = 1 - (float)(((distX - marginStart) + (distY - marginStart)) / (2 * (marginEnd - marginStart)));
                        heightmap[zRes, xRes] = reduceFactor * constHeight;
                    }
                    else if (distY > marginStart && distY <= marginEnd && distX > marginEnd)
                    {
                        reduceFactor = 1 - ((distY - marginStart) / (marginEnd - marginStart));
                        heightmap[zRes, xRes] = reduceFactor * constHeight;
                    }
                    else if (distX > marginStart && distX <= marginEnd && distY > marginEnd)
                    {
                        reduceFactor = 1 - ((distX - marginStart) / (marginEnd - marginStart));
                        heightmap[zRes, xRes] = reduceFactor * constHeight;
                    }
                    else
                    {
                        reduceFactor = 1;
                        heightmap[zRes, xRes] = reduceFactor * constHeight;
                    }

                    // Actual height values from neighbours

                    double randFactor = 0.1f;
                    double selfFactor = 1.5;
                    float value = 0;

                    if (zRes > 0 && xRes > 0 && xRes < terrainData.heightmapResolution - 1 && zRes < terrainData.heightmapResolution - 1)
                    {
                        value = (float)(
                           heightmap[zRes, xRes] * selfFactor +
                           rand.NextDouble() * randFactor +
                           heightmap[zRes - 1, xRes - 1] +
                           heightmap[zRes, xRes - 1] +
                           heightmap[zRes - 1, xRes] +
                           heightmap[zRes + 1, xRes - 1] +
                           heightmap[zRes - 1, xRes + 1] +
                           heightmap[zRes + 1, xRes + 1] +
                           heightmap[zRes + 1, xRes] +
                           heightmap[zRes, xRes + 1]) / 10;
                    }
                    else if (xRes == 0 && zRes == 0)
                    {
                        value = (float)(
                            heightmap[zRes, xRes] * selfFactor +
                            rand.NextDouble() * randFactor +
                            heightmap[zRes + 1, xRes + 1] +
                            heightmap[zRes + 1, xRes] +
                            heightmap[zRes, xRes + 1]) / 5;
                    }
                    else if (xRes == 0 && zRes > 0 && zRes < terrainData.heightmapResolution - 1)
                    {
                        value = (float)(
                            heightmap[zRes, xRes] * selfFactor +
                            rand.NextDouble() * randFactor +
                            heightmap[zRes - 1, xRes] +
                            heightmap[zRes - 1, xRes + 1] +
                            heightmap[zRes + 1, xRes + 1] +
                            heightmap[zRes + 1, xRes] +
                            heightmap[zRes, xRes + 1]) / 7;
                    }
                    else if (xRes == 0 && zRes == terrainData.heightmapResolution - 1)
                    {
                        value = (float)(
                            heightmap[zRes, xRes] * selfFactor +
                            rand.NextDouble() * randFactor * randFactor +
                            heightmap[zRes - 1, xRes] +
                            heightmap[zRes - 1, xRes + 1] +
                            heightmap[zRes, xRes + 1]) / 5;
                    }
                    else if (xRes > 0 && xRes < terrainData.heightmapResolution - 1 && zRes == 0)
                    {
                        value = (float)(
                            heightmap[zRes, xRes] * selfFactor +
                            rand.NextDouble() * randFactor +
                            heightmap[zRes, xRes - 1] +
                            heightmap[zRes + 1, xRes - 1] +
                            heightmap[zRes + 1, xRes + 1] +
                            heightmap[zRes + 1, xRes] +
                            heightmap[zRes, xRes + 1]) / 7;

                    }
                    else if (xRes == terrainData.heightmapResolution - 1 && zRes == 0)
                    {
                        value = (float)(
                            heightmap[zRes, xRes] * selfFactor +
                            rand.NextDouble() * randFactor +
                            heightmap[zRes, xRes - 1] +
                            heightmap[zRes + 1, xRes - 1] +
                            heightmap[zRes + 1, xRes]) / 5;
                    }
                    else if (xRes == terrainData.heightmapResolution - 1 && zRes > 0 && zRes < terrainData.heightmapResolution - 1)
                    {
                        value = (float)(
                            heightmap[zRes, xRes] * selfFactor +
                            rand.NextDouble() * randFactor +
                            heightmap[zRes - 1, xRes - 1] +
                            heightmap[zRes, xRes - 1] +
                            heightmap[zRes - 1, xRes] +
                            heightmap[zRes + 1, xRes - 1] +
                            heightmap[zRes + 1, xRes]) / 7;
                    }
                    else if (xRes == terrainData.heightmapResolution - 1 && zRes == terrainData.heightmapResolution - 1)
                    {
                        value = (float)(
                            heightmap[zRes, xRes] * selfFactor +
                            rand.NextDouble() * randFactor +
                            heightmap[zRes - 1, xRes - 1] +
                            heightmap[zRes, xRes - 1] +
                            heightmap[zRes - 1, xRes]) / 5;
                    }
                    else if (xRes > 0 && xRes < terrainData.heightmapResolution - 1 && zRes == terrainData.heightmapResolution - 1)
                    {
                        value = (float)(
                            heightmap[zRes, xRes] * selfFactor +
                            rand.NextDouble() * randFactor +
                            heightmap[zRes - 1, xRes - 1] +
                            heightmap[zRes, xRes - 1] +
                            heightmap[zRes - 1, xRes] +
                            heightmap[zRes - 1, xRes + 1] +
                            heightmap[zRes, xRes + 1]) / 7;
                    }

                    if (value < 0 || distX > marginEnd && distY > marginEnd)
                    {
                        value = 0;
                    }

                    heightmap[zRes, xRes] = value;

                }
            }

            terrainData.SetHeights(0, 0, heightmap);
        }

        private void TexturizeTerrain()
        {
            var flatSplat = new SplatPrototype();
            var steepSplat = new SplatPrototype();

            flatSplat.texture = FlatTexture;
            steepSplat.texture = SteepTexture;

            terrainData.splatPrototypes = new SplatPrototype[]
            {
                flatSplat,
                steepSplat
            };

            terrainData.RefreshPrototypes();

            var splatMap = new float[terrainData.alphamapResolution, terrainData.alphamapResolution, 2];

            for (var zRes = 0; zRes < terrainData.alphamapHeight; zRes++)
            {
                for (var xRes = 0; xRes < terrainData.alphamapWidth; xRes++)
                {
                    var normalizedX = (float)xRes / (terrainData.alphamapWidth - 1);
                    var normalizedZ = (float)zRes / (terrainData.alphamapHeight - 1);

                    var steepness = terrainData.GetSteepness(normalizedX, normalizedZ);
                    var steepnessNormalized = Mathf.Clamp(steepness / 1.5f, 0, 1f);

                    splatMap[zRes, xRes, 0] = 1f - steepnessNormalized;
                    splatMap[zRes, xRes, 1] = steepnessNormalized;
                }
            }

            terrainData.SetAlphamaps(0, 0, splatMap);
        }

        private void InsertTerrain()
        {
            terrainData.size = new Vector3(length, height, width);
            var newTerrainGameObject = Terrain.CreateTerrainGameObject(terrainData);
            newTerrainGameObject.transform.position = new Vector3(x, 0, y);

            Terrain = newTerrainGameObject.GetComponent<Terrain>();
            Terrain.heightmapPixelError = 8;
            Terrain.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }
        
        private float[] GetCornerCoordinatesFromPixel(int xIndex, int yIndex, int pixelWidth, int pixelHeight, float xTerrain, float yTerrain, float lengthTerrain, float widthTerrain)
        {
            float[] coord = new float[2];
            if (lengthTerrain <= widthTerrain)
            {
                coord[0] = (float)((float)(yIndex) / (float)(pixelHeight - 1)) * Math.Min(widthTerrain, lengthTerrain) + Math.Max(xTerrain, yTerrain);
                coord[1] = (float)((float)(xIndex) / (float)(pixelWidth - 1)) * Math.Max(widthTerrain, lengthTerrain) + Math.Min(xTerrain, yTerrain);
            }
            else
            {
                coord[0] = (float)((float)(yIndex) / (float)(pixelHeight - 1)) * Math.Max(widthTerrain, lengthTerrain) + Math.Min(xTerrain, yTerrain);
                coord[1] = (float)((float)(xIndex) / (float)(pixelWidth - 1)) * Math.Min(widthTerrain, lengthTerrain) + Math.Max(xTerrain, yTerrain);
            }
            return coord;
        }
    }
}