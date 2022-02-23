using System.IO;
using UnityEngine;

namespace SUMOInteraction
{
    class FileChecker
    {
        public static bool CheckFiles()
        {
            if (!File.Exists(PathConstants.pathLight))
            {
                Debug.LogError(PathConstants.pathLight + " is missing.");
                return false;
            }
            if (!File.Exists(PathConstants.pathTrafficLight))
            {
                Debug.LogError(PathConstants.pathTrafficLight + " is missing.");
                return false;
            }
            if (!File.Exists(PathConstants.pathGrassPrototypeTexture2D))
            {
                Debug.LogError(PathConstants.pathGrassPrototypeTexture2D + " is missing.");
                return false;
            }
            if (!File.Exists(PathConstants.pathGrassTexture2D))
            {
                Debug.LogError(PathConstants.pathGrassTexture2D + " is missing.");
                return false;
            }
            if (!File.Exists(PathConstants.pathRockTexture2D))
            {
                Debug.LogError(PathConstants.pathRockTexture2D + " is missing.");
                return false;
            }
            foreach (string path in PathConstants.pathBuildings)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError(path + " is missing.");
                    return false;
                }
            }

            if (!File.Exists(PathConstants.pathRoadMaterial))
            {
                Debug.LogError(PathConstants.pathRoadMaterial + " is missing.");
                return false;
            }

            if (!File.Exists(PathConstants.pathEgoVehicleWASD))
            {
                Debug.LogError(PathConstants.pathEgoVehicleWASD + " is missing.");
                return false;
            }

            if (!File.Exists(PathConstants.pathEgoVehicleUDP))
            {
                Debug.LogError(PathConstants.pathEgoVehicleUDP + " is missing.");
                return false;
            }

            foreach (string path in PathConstants.pathPlants)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError(path + " is missing.");
                    return false;
                }
            }

            if (!File.Exists(PathConstants.pathJunctionMaterial))
            {
                Debug.LogError(PathConstants.pathJunctionMaterial + " is missing.");
                return false;
            }

            return true;

            


        }
    }
}
