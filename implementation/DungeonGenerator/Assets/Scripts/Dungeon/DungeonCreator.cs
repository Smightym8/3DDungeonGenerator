using System;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Linq;
using General;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace Dungeon
{
    public class DungeonCreator : MonoBehaviour
    {
        public int dungeonWidth, dungeonLength;
        public int roomWidthMin, roomLengthMin;
        public int roomWidthMax, roomLengthMax;
        public int maxIterations;
        public int corridorWidth;
        public int dungeonHeight;
        public int lightFrequency;
        public int lightIntensity;
        public int lightRange;
        public Material floorMaterial;
        public Material roofMaterial;
        public Material wallMaterial;
        public Color lightColor = Color.yellow;

        [Range(0.0f, 0.3f)]
        public float roomBottomCornerModifier;
        [Range(0.7f, 1.0f)]
        public float roomTopCornerModifier;
        [Range(0.0f, 2.0f)]
        public int roomOffset;

        public GameObject playerPrefab;
        public GameObject torchPrefab;
        public GameObject tableWithKeyPrefab;
        public GameObject doorPrefab;
        public List<GameObject> sceneryPrefabs;

        private List<Vector3Int> _possibleLightPositions;
        private Dictionary<Vector3Int, int> _lightPositions;
        private List<Vector3Int> _ignoreLightPositions;
        private readonly List<GameObject> _dungeonFloors = new();
        private GameObject _floorParent, _roofParent, _wallParent, _lightsParent, _sceneryParent;
        private List<Vector3> _sceneryPositions;

        public static DungeonCreator dungeonCreator;

        // Unity Methods
        private void Awake()
        {
            dungeonCreator = this;
        }

        private void Start()
        {
            var sw = new Stopwatch();
        
            sw.Start();
            CreateDungeon();
            sw.Stop();
            
            Debug.Log($"Dungeon creation time: {sw.Elapsed}");
        }

        // Custom Methods
        /// <summary>
        /// This method is the entry method which calls all the sub method to create the whole dungeon.
        /// </summary>
        private void CreateDungeon()
        {
            DungeonGenerator generator = new DungeonGenerator(dungeonWidth, dungeonLength);
            var listOfRoomsAndCorridors = generator.CalculateDungeon(
                maxIterations, 
                roomWidthMin, 
                roomLengthMin,
                roomWidthMax,
                roomLengthMax,
                roomBottomCornerModifier,
                roomTopCornerModifier,
                roomOffset,
                corridorWidth
            );
            
            var corridors = listOfRoomsAndCorridors.Where(x => x.IsCorridor).ToList();
            var rooms = listOfRoomsAndCorridors.Where(x => !x.IsCorridor).ToList();
            
            _possibleLightPositions = new List<Vector3Int>();
            _lightPositions = new Dictionary<Vector3Int, int>();
            _ignoreLightPositions = new List<Vector3Int>();
            _sceneryPositions = new List<Vector3>();
            
            // Parent objects for floor, roof, walls and scenery
            var currentTransform = transform;
            _floorParent = new GameObject("Floors", typeof(MeshFilter), typeof(MeshRenderer))
            {
                transform =
                {
                    parent = currentTransform
                }
            };
            
            _roofParent = new GameObject("Roofs", typeof(MeshFilter), typeof(MeshRenderer))
            {
                transform =
                {
                    parent = currentTransform
                }
            };
            
            _wallParent = new GameObject("Walls", typeof(MeshFilter), typeof(MeshRenderer))
            {
                transform =
                {
                    parent = currentTransform
                }
            };

            _lightsParent = new GameObject("Lights", typeof(MeshFilter), typeof(MeshRenderer))
            {
                transform =
                {
                    parent = currentTransform,
                }
            };
            
            _sceneryParent = new GameObject("Scenery", typeof(MeshFilter), typeof(MeshRenderer))
            {
                transform =
                {
                    parent = currentTransform,
                }
            };

            // Choose first generated room as start room
            RoomNode startRoom = (RoomNode) rooms[0];
            Node endRoomNode = null;
            float maxDistance = 0f;
            
            foreach (var (room, index) in listOfRoomsAndCorridors.Select((room, index) => ( room, index )))
            {
                _dungeonFloors.Add(
                    CreateFloorMesh(
                        room.BottomLeftAreaCorner,
                        room.TopRightAreaCorner,
                        floorMaterial,
                        0,
                        true,
                        index
                    )
                );
        
                // Check distance between start room and each other room
                // to find the room which is the most distant from the start room
                if (index > 0 && index < rooms.Count)
                {
                    RoomNode currentRoom = (RoomNode)listOfRoomsAndCorridors[index];
                    float dist = Vector3.Distance(startRoom.CentrePoint, currentRoom.CentrePoint);

                    if (!(maxDistance < dist)) continue;
            
                    maxDistance = dist;
                    endRoomNode = currentRoom;
                }
            }
            
            // Easy mode
            PlaceNextLevelDoor(rooms[1], corridors); 
            //PlaceNextLevelDoor(endRoomNode, corridors);
            
            // Create the roof
            foreach (var (room, index) in listOfRoomsAndCorridors.Select((room, index) => ( room, index )))
            {
                _dungeonFloors.Add(
                    CreateFloorMesh(
                        room.BottomLeftAreaCorner,
                        room.TopRightAreaCorner,
                        roofMaterial,
                        dungeonHeight,
                        false,
                        index
                    )
                );
            }
            
            // TODO: Reduce amount of lamps in hallways
            foreach (var room in listOfRoomsAndCorridors)
            {
                CreateWalls(room, corridors);
                CollectLightPositions(room);
            }

            CombineMeshes(_floorParent, true);
            CombineMeshes(_roofParent, true);
            CombineMeshes(_wallParent, false);
            
            // Place lights
            foreach (var (position, rotation) in _lightPositions)
            {
                PlaceLight(position, rotation);
            }
            
            // Easy mode
            PlaceTableWithKey((RoomNode) rooms[1]);
            // Place table with key to get to next level
            //var randomRoomIndex = Random.Range(1, rooms.Count);
            //PlaceTableWithKey((RoomNode) rooms[randomRoomIndex]);
            
            // Add random scenery to room
            for (var i = 1; i < rooms.Count; i++)
            {
                PlaceScenery((RoomNode) rooms[i]);
            }
            
            SpawnPlayer(playerPrefab, (RoomNode) rooms[0]);
        }

        private void PlaceNextLevelDoor(Node room, List<Node> corridors)
        {
            var position = Vector3.zero;
            var rotation = Vector3.zero;

            var bottomHorizontalHasCorridor = false;
            var topHorizontalHasCorridor = false;
            var leftVerticalHasCorridor = false;
            var rightVerticalHasCorridor = false;
            
            // Check which wall doesn't have a corridor so the door can be placed properly
            foreach (var corridor in corridors)
            {
                // If one side becomes true, do not check it again with other corridors
                // Bottom horizontal
                if (!bottomHorizontalHasCorridor)
                {
                    bottomHorizontalHasCorridor = HasCorridorBetween(room.BottomLeftAreaCorner, room.BottomRightAreaCorner,
                        corridor.TopLeftAreaCorner, corridor.TopRightAreaCorner, true);
                }

                if (!topHorizontalHasCorridor)
                {
                    // Top horizontal
                    topHorizontalHasCorridor = HasCorridorBetween(room.TopLeftAreaCorner, room.TopRightAreaCorner,
                        corridor.BottomLeftAreaCorner, corridor.BottomRightAreaCorner, true);    
                }

                if (!leftVerticalHasCorridor)
                {
                    // Left vertical
                    leftVerticalHasCorridor = HasCorridorBetween(room.BottomLeftAreaCorner, room.TopLeftAreaCorner,
                        corridor.BottomRightAreaCorner, corridor.TopRightAreaCorner, false);    
                }

                if (!rightVerticalHasCorridor)
                {
                    // Right vertical
                    rightVerticalHasCorridor = HasCorridorBetween(room.BottomRightAreaCorner, room.TopRightAreaCorner,
                        corridor.BottomLeftAreaCorner, corridor.TopLeftAreaCorner, false);    
                }
            }

            // Place door at wall without corridor
            if (!bottomHorizontalHasCorridor)
            {
                var halfLength = Math.Abs(room.BottomRightAreaCorner.x - room.BottomLeftAreaCorner.x) / 2;
                position = new Vector3(room.BottomLeftAreaCorner.x, 0, room.BottomLeftAreaCorner.y);
                position.x += halfLength;
            }
            else if (!topHorizontalHasCorridor)
            {
                var halfLength = Math.Abs(room.TopRightAreaCorner.x - room.TopLeftAreaCorner.x) / 2;
                position = new Vector3(room.TopLeftAreaCorner.x, 0, room.TopLeftAreaCorner.y);
                position.x += halfLength;
            }
            else if (!leftVerticalHasCorridor)
            {
                var halfLength = Math.Abs(room.TopLeftAreaCorner.y - room.BottomLeftAreaCorner.y) / 2;
                position = new Vector3(room.BottomLeftAreaCorner.x, 0, room.BottomLeftAreaCorner.y);
                position.y += halfLength;
            }
            else if (!rightVerticalHasCorridor)
            {
                var halfLength = Math.Abs(room.TopRightAreaCorner.y - room.BottomRightAreaCorner.y) / 2;
                position = new Vector3(room.BottomRightAreaCorner.x, 0, room.BottomRightAreaCorner.y);
                position.y += halfLength;
            }
            
            var door = Instantiate(doorPrefab, position, Quaternion.identity);
            door.AddComponent<BoxCollider>();
            door.tag = Tag.NextLevelDoor;
            door.transform.parent = _sceneryParent.transform;
            door.transform.Rotate(rotation);
            
            // Add sphere collider as trigger for interaction with player
            var triggerCollider = door.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 1.5f;
        }

        private void PlaceTableWithKey(RoomNode room)
        {
            var position = room.CentrePoint;
            var keyTable = Instantiate(tableWithKeyPrefab, position, Quaternion.identity);
            keyTable.tag = Tag.KeyTable;
            keyTable.transform.parent = _sceneryParent.transform;
            
            _sceneryPositions.Add(keyTable.transform.position);
        }

        private void CombineMeshes(GameObject parent, bool isFloorOrRoof)
        {
            Vector3 position = parent.transform.position;
            
            MeshFilter[] meshFilters = parent.GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];

            int i = 0;
            while (i < meshFilters.Length)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                meshFilters[i].gameObject.SetActive(false);

                i++;
            }
            parent.transform.GetComponent<MeshFilter>().mesh = new Mesh();
            parent.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
            parent.isStatic = true;
            
            // Add material
            parent.GetComponent<Renderer>().material = isFloorOrRoof ? floorMaterial : wallMaterial;
            // Add collider
            parent.AddComponent<MeshCollider>();
            // Add layer
            parent.layer = isFloorOrRoof ? Layer.GroundLayer : Layer.WallLayer;
            
            parent.transform.position = position;
            parent.transform.gameObject.SetActive(true);
        }

        private void CollectLightPositions(Node room)
        {
            var bottomLeftCorner = new Vector3(room.BottomLeftAreaCorner.x, 0, room.BottomLeftAreaCorner.y);
            var bottomRightCorner = new Vector3(room.BottomRightAreaCorner.x, 0, room.BottomRightAreaCorner.y);
            var topLeftCorner = new Vector3(room.TopLeftAreaCorner.x, 0, room.TopLeftAreaCorner.y);
            var topRightCorner = new Vector3(room.TopRightAreaCorner.x, 0, room.TopRightAreaCorner.y);
            
            var horizontalBottomRotation = 0;
            var horizontalTopRotation = 180;
            var verticalLeftRotation = 90;
            var verticalRightRotation = -90;

            var horizontalBottomLength = bottomRightCorner.x - bottomLeftCorner.x;
            var horizontalTopLength = topRightCorner.x - topLeftCorner.x;
            var verticalLeftLength = topLeftCorner.z - bottomLeftCorner.z;
            var verticalRightLength = topRightCorner.z - bottomRightCorner.z;
            
            // Horizontal bottom
            bool isGettingLight = false;
            int lightFrequencyTemp = lightFrequency;
            int distancePerLight = (int)Math.Ceiling(horizontalBottomLength / lightFrequencyTemp);
            int steps = 1;
            
            for (var x = (int)bottomLeftCorner.x; x <= (int)bottomRightCorner.x; x++)
            {
                var position = new Vector3(x, 0, bottomLeftCorner.z);
                if (x == (int)bottomLeftCorner.x + (distancePerLight * steps))
                {
                    steps++;
                    isGettingLight = true;
                }

                if (room.IsCorridor && room.IsHorizontalCorridor)
                {
                    isGettingLight = false;
                }
                
                SaveLightPosition(position, horizontalBottomRotation, isGettingLight);
                isGettingLight = false;
            }
            
            // Horizontal top
            lightFrequencyTemp = lightFrequency;
            distancePerLight = (int)Math.Ceiling(horizontalTopLength / lightFrequencyTemp);
            steps = 1;
            
            for (var x = (int)topLeftCorner.x; x <= (int)topRightCorner.x; x++)
            {
                var position = new Vector3(x, 0, topLeftCorner.z);
                if (x == (int)topLeftCorner.x + (distancePerLight * steps))
                {
                    steps++;
                    isGettingLight = true;
                }
                
                if (room.IsCorridor && room.IsHorizontalCorridor)
                {
                    isGettingLight = false;
                }
                
                SaveLightPosition(position, horizontalTopRotation, isGettingLight);
                isGettingLight = false;
            }
            
            // Vertical left
            lightFrequencyTemp = lightFrequency;
            distancePerLight = (int)Math.Ceiling(verticalLeftLength / lightFrequencyTemp);
            steps = 1;
            
            for (var z = (int)bottomLeftCorner.z; z <= (int)topLeftCorner.z; z++)
            {
                var position = new Vector3(bottomLeftCorner.x, 0, z);
                if (z == (int)bottomLeftCorner.z + (distancePerLight * steps))
                {
                    steps++;
                    isGettingLight = true;
                }
                
                if (room.IsCorridor && !room.IsHorizontalCorridor)
                {
                    isGettingLight = false;
                }
                
                SaveLightPosition(position, verticalLeftRotation, isGettingLight);
                isGettingLight = false;
            }
            
            // Vertical right
            lightFrequencyTemp = lightFrequency;
            distancePerLight = (int)Math.Ceiling(verticalRightLength / lightFrequencyTemp);
            steps = 1;
            
            for (var z = (int)bottomRightCorner.z; z <= (int)topRightCorner.z; z++)
            {
                var position = new Vector3(bottomRightCorner.x, 0, z);
                if (z == (int)bottomRightCorner.z + (distancePerLight * steps))
                {
                    steps++;
                    isGettingLight = true;
                }
                
                if (room.IsCorridor && !room.IsHorizontalCorridor)
                {
                    isGettingLight = false;
                }
                
                SaveLightPosition(position, verticalRightRotation, isGettingLight);
                isGettingLight = false;
            }
        }

        /// <summary>
        /// This method saves the positions and rotations of the lights.
        /// </summary>
        /// <param name="position">Contains the current position that will be checked</param>
        /// <param name="rotation">Contains the rotation for the given point</param>
        /// <param name="isGettingLight">Specifies if a light will be placed at this point</param>
        private void SaveLightPosition(Vector3 position, int rotation, bool isGettingLight)
        {
            Vector3Int point = Vector3Int.CeilToInt(position);

            if (_possibleLightPositions.Contains(point))
            {
                _possibleLightPositions.Remove(point);
                _ignoreLightPositions.Add(point);

                if (_lightPositions.ContainsKey(point))
                {
                    _lightPositions.Remove(point);
                }
            }
            else if(!_ignoreLightPositions.Contains(point))
            {
                _possibleLightPositions.Add(point);
                if (isGettingLight)
                {
                    _lightPositions.Add(point, rotation);    
                }
            }
        }

        /// <summary>
        /// These method places scenery in a room
        /// </summary>
        /// <param name="room">Contains the room where the scenery will be placed</param>
        private void PlaceScenery(RoomNode room)
        {
            int randomSceneryIndex = Random.Range(0, sceneryPrefabs.Count);
            var position = _sceneryPositions[0];
            
            while (_sceneryPositions.Contains(position))
            {
                int randomX = Random.Range(room.BottomLeftAreaCorner.x + 1, room.BottomRightAreaCorner.x - 1);
                int randomZ = Random.Range(room.BottomLeftAreaCorner.y + 1, room.TopLeftAreaCorner.y - 1);
                position = new Vector3(randomX, 0, randomZ);
            }
            
            var instantiatedScenery = Instantiate(sceneryPrefabs[randomSceneryIndex], position, Quaternion.identity);
            instantiatedScenery.transform.parent = _sceneryParent.transform;
            
            _sceneryPositions.Add(position);
            
            instantiatedScenery.AddComponent<BoxCollider>();
            var direction = (room.CentrePoint - transform.position).normalized;
            instantiatedScenery.transform.rotation = Quaternion.LookRotation(direction);
        }

        /// <summary>
        /// This method places a prefab on the given position with the given rotation and adds a light source to it.
        /// </summary>
        /// <param name="lightPosition">Contains the position for the light prefab</param>
        /// <param name="rotation">Contains the rotation for the light prefab</param>
        private void PlaceLight(Vector3Int lightPosition, int rotation)
        {
            lightPosition.y = dungeonHeight - 1;

            var lightGameObject = Instantiate(torchPrefab, lightPosition, Quaternion.identity);
            lightGameObject.transform.parent = _lightsParent.transform;
            lightGameObject.transform.Rotate(new Vector3(0, rotation, 0));
            lightGameObject.isStatic = true;

            var lightSourceGameObject = new GameObject("LightSource")
            {
                transform =
                {
                    parent = lightGameObject.transform
                }
            };

            var position = lightGameObject.transform.position;
            switch (rotation)
            {
                case 0:
                    lightSourceGameObject.transform.position = new Vector3(position.x, position.y - 0.5f, position.z + 1);
                    break;
                case 180:
                    lightSourceGameObject.transform.position = new Vector3(position.x, position.y - 0.5f, position.z - 1);
                    break;
                case 90:
                    lightSourceGameObject.transform.position = new Vector3(position.x + 1, position.y - 0.5f, position.z);
                    break;
                case -90:
                    lightSourceGameObject.transform.position = new Vector3(position.x - 1, position.y - 0.5f, position.z);
                    break;
            }
            
            Light lightSource = lightSourceGameObject.AddComponent<Light>();
            lightSource.type = LightType.Point;
            lightSource.intensity = lightIntensity;
            lightSource.range = lightRange;
            lightSource.color = lightColor;
            lightSource.renderMode = LightRenderMode.ForcePixel;
        }

        /// <summary>
        /// This method is the entry method to create all walls for one room. It calls the appropriate methods
        /// to collect the vertices and to create the mesh.
        /// </summary>
        /// <param name="room">Contains the room for which the walls will be created</param>
        /// <param name="corridors">Contains the corridors to check if a wall has to be split</param>
        private void CreateWalls(Node room, List<Node> corridors)
        {
            // When collect vertices for a room check if any corridor has its corner points
            // in the range of the wall
            List<Vector3> vertices;

            var isNormalWallHorizontalBottom = true;
            var isNormalWallHorizontalTop = true;
            var isNormalWallVerticalLeft = true;
            var isNormalWallVerticalRight = true;
            
            // Split wall into two walls
            // Check if there is a corridor between the start and endpoint of the wall
            // But only if it is a room and no corridor
            if (!room.IsCorridor)
            {
                foreach (var corridor in corridors)
                {
                    // Bottom horizontal wall
                    if (HasCorridorBetween(room.BottomLeftAreaCorner, room.BottomRightAreaCorner,
                            corridor.TopLeftAreaCorner, corridor.TopRightAreaCorner, true))
                    {
                        isNormalWallHorizontalBottom = false;

                        vertices = CollectWallVertices(room.BottomLeftAreaCorner, corridor.TopLeftAreaCorner, true);
                        CreateWallMesh(vertices, room.BottomLeftAreaCorner, corridor.TopLeftAreaCorner, true,true);
                        
                        vertices = CollectWallVertices(corridor.TopRightAreaCorner, room.BottomRightAreaCorner, true);
                        CreateWallMesh(vertices, corridor.TopRightAreaCorner, room.BottomRightAreaCorner, true, true);
                    }
                    else if (HasCorridorAtLeftCorner(room.BottomLeftAreaCorner, room.BottomRightAreaCorner,
                                 corridor.TopLeftAreaCorner, corridor.TopRightAreaCorner, true))
                    {
                        isNormalWallHorizontalBottom = false;
                        
                        vertices = CollectWallVertices(corridor.TopLeftAreaCorner, room.BottomLeftAreaCorner, true);
                        CreateWallMesh(vertices, corridor.TopLeftAreaCorner, room.BottomLeftAreaCorner, true,false);
                        
                        vertices = CollectWallVertices(corridor.TopRightAreaCorner, room.BottomRightAreaCorner, true);
                        CreateWallMesh(vertices, corridor.TopRightAreaCorner, room.BottomRightAreaCorner, true,true);
                    }
                    else if (HasCorridorAtRightCorner(room.BottomLeftAreaCorner, room.BottomRightAreaCorner,
                                 corridor.TopLeftAreaCorner, corridor.TopRightAreaCorner, true))
                    {
                        isNormalWallHorizontalBottom = false;
                        
                        vertices = CollectWallVertices(room.BottomLeftAreaCorner, corridor.TopLeftAreaCorner, true);
                        CreateWallMesh(vertices, room.BottomLeftAreaCorner, corridor.TopLeftAreaCorner, true,true);
                        
                        vertices = CollectWallVertices(room.BottomRightAreaCorner, corridor.TopRightAreaCorner, true);
                        CreateWallMesh(vertices, room.BottomRightAreaCorner, corridor.TopRightAreaCorner, true,false);
                    }
                    
                    // Top horizontal wall
                    if (HasCorridorBetween(room.TopLeftAreaCorner, room.TopRightAreaCorner, 
                            corridor.BottomLeftAreaCorner, corridor.BottomRightAreaCorner, true))
                    {
                        isNormalWallHorizontalTop = false;

                        vertices = CollectWallVertices(room.TopLeftAreaCorner, corridor.BottomLeftAreaCorner, true);
                        CreateWallMesh(vertices, room.TopLeftAreaCorner, corridor.BottomLeftAreaCorner, true,false);
                        
                        vertices = CollectWallVertices(corridor.BottomRightAreaCorner, room.TopRightAreaCorner, true);
                        CreateWallMesh(vertices, corridor.BottomRightAreaCorner, room.TopRightAreaCorner, true, false);
                    }
                    else if (HasCorridorAtLeftCorner(room.TopLeftAreaCorner, room.TopRightAreaCorner, 
                                 corridor.BottomLeftAreaCorner, corridor.BottomRightAreaCorner, true))
                    {
                        isNormalWallHorizontalTop = false;

                        vertices = CollectWallVertices(corridor.BottomLeftAreaCorner, room.TopLeftAreaCorner, true);
                        CreateWallMesh(vertices, corridor.BottomLeftAreaCorner, room.TopLeftAreaCorner, true,true);
                        
                        vertices = CollectWallVertices(corridor.BottomRightAreaCorner, room.TopRightAreaCorner, true);
                        CreateWallMesh(vertices, corridor.BottomRightAreaCorner, room.TopRightAreaCorner, true, false);
                    }
                    else if (HasCorridorAtRightCorner(room.TopLeftAreaCorner, room.TopRightAreaCorner, 
                                 corridor.BottomLeftAreaCorner, corridor.BottomRightAreaCorner, true))
                    {
                        isNormalWallHorizontalTop = false;

                        vertices = CollectWallVertices(room.TopLeftAreaCorner, corridor.BottomLeftAreaCorner, true);
                        CreateWallMesh(vertices, room.TopLeftAreaCorner, corridor.BottomLeftAreaCorner, true,false);
                        
                        vertices = CollectWallVertices(room.TopRightAreaCorner, corridor.BottomRightAreaCorner, true);
                        CreateWallMesh(vertices, room.TopRightAreaCorner, corridor.BottomRightAreaCorner, true, true);
                    }
                    
                    // Left vertical wall
                    if (HasCorridorBetween(room.BottomLeftAreaCorner, room.TopLeftAreaCorner, 
                            corridor.BottomRightAreaCorner, corridor.TopRightAreaCorner, false))
                    {
                        isNormalWallVerticalLeft = false;
                        
                        vertices = CollectWallVertices(room.BottomLeftAreaCorner, corridor.BottomRightAreaCorner, false);
                        CreateWallMesh(vertices, room.BottomLeftAreaCorner, corridor.BottomRightAreaCorner, false,false);
                        
                        vertices = CollectWallVertices(corridor.TopRightAreaCorner, room.TopLeftAreaCorner, false);
                        CreateWallMesh(vertices, corridor.TopRightAreaCorner, room.TopLeftAreaCorner, false, false);
                    }
                    else if (HasCorridorAtLeftCorner(room.BottomLeftAreaCorner, room.TopLeftAreaCorner, 
                                 corridor.BottomRightAreaCorner, corridor.TopRightAreaCorner, false))
                    {
                        isNormalWallVerticalLeft = false;
                        
                        vertices = CollectWallVertices(corridor.BottomRightAreaCorner, room.BottomLeftAreaCorner, false);
                        CreateWallMesh(vertices, corridor.BottomRightAreaCorner, room.BottomLeftAreaCorner, false,true);
                        
                        vertices = CollectWallVertices(corridor.TopRightAreaCorner, room.TopLeftAreaCorner, false);
                        CreateWallMesh(vertices, corridor.TopRightAreaCorner, room.TopLeftAreaCorner, false, false);
                    }
                    else if (HasCorridorAtRightCorner(room.BottomLeftAreaCorner, room.TopLeftAreaCorner, 
                                 corridor.BottomRightAreaCorner, corridor.TopRightAreaCorner, false))
                    {
                        isNormalWallVerticalLeft = false;
                        
                        vertices = CollectWallVertices(room.BottomLeftAreaCorner, corridor.BottomRightAreaCorner, false);
                        CreateWallMesh(vertices, room.BottomLeftAreaCorner, corridor.BottomRightAreaCorner, false,false);
                        
                        vertices = CollectWallVertices(room.TopLeftAreaCorner, corridor.TopRightAreaCorner, false);
                        CreateWallMesh(vertices, room.TopLeftAreaCorner, corridor.TopRightAreaCorner, false, true);
                    }
                    
                    // Right vertical
                    if (HasCorridorBetween(room.BottomRightAreaCorner, room.TopRightAreaCorner, 
                            corridor.BottomLeftAreaCorner, corridor.TopLeftAreaCorner, false))
                    {
                        isNormalWallVerticalRight = false;
                        
                        vertices = CollectWallVertices(room.BottomRightAreaCorner, corridor.BottomLeftAreaCorner, false);
                        CreateWallMesh(vertices, room.BottomRightAreaCorner, corridor.BottomLeftAreaCorner, false,true);
                        
                        vertices = CollectWallVertices(corridor.TopLeftAreaCorner, room.TopRightAreaCorner, false);
                        CreateWallMesh(vertices, corridor.TopLeftAreaCorner, room.TopRightAreaCorner, false, true);
                    }
                    else if (HasCorridorAtLeftCorner(room.BottomRightAreaCorner, room.TopRightAreaCorner, 
                                 corridor.BottomLeftAreaCorner, corridor.TopLeftAreaCorner, false))
                    {
                        isNormalWallVerticalRight = false;
                        
                        vertices = CollectWallVertices(corridor.BottomLeftAreaCorner, room.BottomRightAreaCorner, false);
                        CreateWallMesh(vertices, corridor.BottomLeftAreaCorner, room.BottomRightAreaCorner, false,false);
                        
                        vertices = CollectWallVertices(corridor.TopLeftAreaCorner, room.TopRightAreaCorner, false);
                        CreateWallMesh(vertices, corridor.TopLeftAreaCorner, room.TopRightAreaCorner, false, true);
                    }
                    else if (HasCorridorAtRightCorner(room.BottomRightAreaCorner, room.TopRightAreaCorner, 
                                 corridor.BottomLeftAreaCorner, corridor.TopLeftAreaCorner, false))
                    {
                        isNormalWallVerticalRight = false;
                        
                        vertices = CollectWallVertices(room.BottomRightAreaCorner, corridor.BottomLeftAreaCorner, false);
                        CreateWallMesh(vertices, room.BottomRightAreaCorner, corridor.BottomLeftAreaCorner, false,true);
                        
                        vertices = CollectWallVertices(room.TopRightAreaCorner, corridor.TopLeftAreaCorner, false);
                        CreateWallMesh(vertices, room.TopRightAreaCorner, corridor.TopLeftAreaCorner, false, false);
                    }
                }
            }
            
            // Create walls normal as one wall
            // Horizontal
            if (!room.IsCorridor || room.IsHorizontalCorridor)
            {
                // Bottom horizontal wall
                if (isNormalWallHorizontalBottom)
                {
                    vertices = CollectWallVertices(room.BottomLeftAreaCorner, room.BottomRightAreaCorner, true);
                    CreateWallMesh(vertices, room.BottomLeftAreaCorner, room.BottomRightAreaCorner, true,true);
                }
                
                if (isNormalWallHorizontalTop)
                {
                    // Top horizontal wall
                    vertices = CollectWallVertices(room.TopLeftAreaCorner, room.TopRightAreaCorner, true);
                    CreateWallMesh(vertices, room.TopLeftAreaCorner, room.TopRightAreaCorner, true, false);
                }
            }

            // Vertical
            if (!room.IsCorridor || !room.IsHorizontalCorridor)
            {
                if (isNormalWallVerticalLeft)
                {
                    // Left vertical wall
                    vertices = CollectWallVertices(room.BottomLeftAreaCorner, room.TopLeftAreaCorner, false);
                    CreateWallMesh(vertices, room.BottomLeftAreaCorner, room.TopLeftAreaCorner, false, false);    
                }

                if (isNormalWallVerticalRight)
                {
                    // Right vertical wall
                    vertices = CollectWallVertices(room.BottomRightAreaCorner, room.TopRightAreaCorner, false);
                    CreateWallMesh(vertices, room.BottomRightAreaCorner, room.TopRightAreaCorner, false, true);    
                }
            }
        }
        
        /// <summary>
        /// This method checks if a corridor is between the two corners of a given wall side
        /// </summary>
        /// <param name="roomStartCorner">Contains the start corner of the room</param>
        /// <param name="roomEndCorner">Contains the end corner of the room</param>
        /// <param name="corridorStartCorner">Contains the start corner of the corridor</param>
        /// <param name="corridorEndCorner">Contains the end corner of the corridor</param>
        /// <param name="isHorizontal">Marks the room side as horizontal</param>
        /// <returns>true if the corridor is between the room corners or false if it is not</returns>
        private bool HasCorridorBetween(Vector2Int roomStartCorner, Vector2Int roomEndCorner, 
        Vector2Int corridorStartCorner, Vector2Int corridorEndCorner, bool isHorizontal)
        {
            // The room and corridor coordinates are not changing into this direction
            // and have to be equal so in 2D they are on the same height
            var roomStaticDirectionPos = isHorizontal ? roomStartCorner.y : roomStartCorner.x;
            var corridorStaticDirectionPos = isHorizontal ? corridorStartCorner.y : corridorStartCorner.x;

            var roomStartCoordinate = isHorizontal ? roomStartCorner.x : roomStartCorner.y;
            var roomEndCoordinate = isHorizontal ? roomEndCorner.x : roomEndCorner.y;
            var corridorStartCoordinate = isHorizontal ? corridorStartCorner.x : corridorStartCorner.y;
            var corridorEndCoordinate = isHorizontal ? corridorEndCorner.x : corridorEndCorner.y;
            
            return roomStaticDirectionPos == corridorStaticDirectionPos &&
                   roomStartCoordinate <= corridorStartCoordinate &&
                   corridorStartCoordinate <= roomEndCoordinate &&
                   roomStartCoordinate <= corridorEndCoordinate &&
                   corridorEndCoordinate <= roomEndCoordinate;
        }

        private bool HasCorridorAtLeftCorner(Vector2Int roomStartCorner, Vector2Int roomEndCorner, 
            Vector2Int corridorStartCorner, Vector2Int corridorEndCorner, bool isHorizontal)
        {
            var roomStaticDirectionPos = isHorizontal ? roomStartCorner.y : roomStartCorner.x;
            var corridorStaticDirectionPos = isHorizontal ? corridorStartCorner.y : corridorStartCorner.x;

            var roomStartCoordinate = isHorizontal ? roomStartCorner.x : roomStartCorner.y;
            var roomEndCoordinate = isHorizontal ? roomEndCorner.x : roomEndCorner.y;
            var corridorStartCoordinate = isHorizontal ? corridorStartCorner.x : corridorStartCorner.y;
            var corridorEndCoordinate = isHorizontal ? corridorEndCorner.x : corridorEndCorner.y;
            
            return roomStaticDirectionPos == corridorStaticDirectionPos &&
                   corridorStartCoordinate < roomStartCoordinate &&
                   roomStartCoordinate < corridorEndCoordinate &&
                   corridorEndCoordinate < roomEndCoordinate;
        }
        
        private bool HasCorridorAtRightCorner(Vector2Int roomStartCorner, Vector2Int roomEndCorner, 
            Vector2Int corridorStartCorner, Vector2Int corridorEndCorner, bool isHorizontal)
        {
            var roomStaticDirectionPos = isHorizontal ? roomStartCorner.y : roomStartCorner.x;
            var corridorStaticDirectionPos = isHorizontal ? corridorStartCorner.y : corridorStartCorner.x;

            var roomStartCoordinate = isHorizontal ? roomStartCorner.x : roomStartCorner.y;
            var roomEndCoordinate = isHorizontal ? roomEndCorner.x : roomEndCorner.y;
            var corridorStartCoordinate = isHorizontal ? corridorStartCorner.x : corridorStartCorner.y;
            var corridorEndCoordinate = isHorizontal ? corridorEndCorner.x : corridorEndCorner.y;
            
            return roomStaticDirectionPos == corridorStaticDirectionPos &&
                   roomStartCoordinate < corridorStartCoordinate &&
                   corridorStartCoordinate < roomEndCoordinate &&
                   roomEndCoordinate < corridorEndCoordinate;
        }

        /// <summary>
        /// This method collects the vertices for a wall
        /// </summary>
        /// <param name="startCorner">Contains the start</param>
        /// <param name="endCorner">Contains the end</param>
        /// <param name="isHorizontal">Is needed to decide if the 2D x or y direction is used</param>
        /// <returns>vertices</returns>
        private List<Vector3> CollectWallVertices(Vector2Int startCorner, Vector2Int endCorner, bool isHorizontal)
        {
            var vertices = new List<Vector3>();
            var start = isHorizontal ? startCorner.x : startCorner.y;
            var end = isHorizontal ? endCorner.x : endCorner.y;
            
            for (var height = 0; height <= dungeonHeight; height++)
            {
                for (var length = start; length <= end; length++)
                {
                    var vertex = isHorizontal ? 
                        new Vector3(length, height, startCorner.y) : 
                        new Vector3(startCorner.x, height, length);
                    
                    vertices.Add(vertex);
                }
            }
            
            return vertices;
        }

        /// <summary>
        /// This method creates the mesh for a wall for the given vertices
        /// </summary>
        /// <param name="vertices">Contains the vertices that will be connected with triangles</param>
        /// <param name="startCorner">Contains the start corner to calculate the length</param>
        /// <param name="endCorner">Contains the end corner to calculate the length</param>
        /// <param name="isHorizontal">This boolean is a switch to use either x or z as direction</param>
        /// <param name="isFlip">This boolean is a switch to flip the triangle creation so the walls are visible inside the room</param>
        private void CreateWallMesh(List<Vector3> vertices, Vector2Int startCorner, Vector2Int endCorner, 
            bool isHorizontal, bool isFlip)
        {
            int length = isHorizontal ? Math.Abs(endCorner.x - startCorner.x) : Math.Abs(endCorner.y - startCorner.y);

            int[] triangles = new int[(length * dungeonHeight * 6)];
            int tris = 0;
            int vert = 0;

            if (!isFlip)
            {
                for (int y = 0; y < dungeonHeight; y++)
                {
                    for (int x = 0; x < length; x++)
                    {
                        triangles[tris] = vert;
                        triangles[tris + 1] = triangles[tris + 4] = vert + length + 1;
                        triangles[tris + 2] = triangles[tris + 3] = vert + 1;
                        triangles[tris + 5] = vert + length + 2;

                        vert++;
                        tris += 6;
                    }

                    vert++;
                }
            }
            else
            {
                for (int y = 0; y < dungeonHeight; y++)
                {
                    for (int x = 0; x < length; x++)
                    {
                        triangles[tris] = vert;
                        triangles[tris + 1] = triangles[tris + 4] = vert + 1; 
                        triangles[tris + 2] = triangles[tris + 3] = vert + length + 1;
                        triangles[tris + 5] = vert + length + 2;

                        vert++;
                        tris += 6;
                    }

                    vert++;
                }
            }

            Vector2[] uvCoordinates = new Vector2[vertices.Count];
            for (int i = 0, y = 0; y <= dungeonHeight; y++)
            {
                for (var x = 0; x <= length; x++)
                {
                    uvCoordinates[i] = new Vector2(x, y);
                    i++;
                }
            }

            var mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles,
                uv = uvCoordinates
            };
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds(); // Checking how big the mesh is for the camera

            var wall = new GameObject("Wall", typeof(MeshFilter), typeof(MeshRenderer))
            {
                transform =
                {
                    position = Vector3.zero,
                    localScale = Vector3.one,
                    parent = _wallParent.transform
                }
            };
            
            wall.GetComponent<MeshFilter>().mesh = mesh;
            wall.GetComponent<MeshRenderer>().material = wallMaterial;
            wall.layer = Layer.WallLayer;
            wall.isStatic = true;
        }

        /// <summary>
        /// This method spawns the player in a given room
        /// </summary>
        /// <param name="player">Is the player game object that will be positioned</param>
        /// <param name="startRoom">Is the room where the game object will be spawned</param>
        private void SpawnPlayer(GameObject player, RoomNode startRoom)
        {
            var spawnPosition = startRoom.CentrePoint;
            spawnPosition.y = 0.02f;
            player.transform.position = spawnPosition;
        }

        /// <summary>
        /// This method creates the mesh for the floor and the roof
        /// </summary>
        /// <param name="bottomLeftCorner">Contains the bottom left corner of the mesh</param>
        /// <param name="topRightCorner">Contains the top right corner of the mesh</param>
        /// <param name="material">Is the material that will be used for the mesh</param>
        /// <param name="height">Is the height of the generated mesh. It is necessary to use the same method
        /// for the floor and the roof</param>
        /// <param name="isFloor">Is a switch variable to change the creation of the triangles</param>
        /// <param name="index">Contains the index of the current wall</param>
        /// <returns></returns>
        private GameObject CreateFloorMesh(Vector2 bottomLeftCorner, Vector2 topRightCorner, Material material, 
            int height, bool isFloor, int index)
        {
            Vector3 bottomLeftV = new Vector3(bottomLeftCorner.x, height, bottomLeftCorner.y);
            Vector3 bottomRightV = new Vector3(topRightCorner.x, height, bottomLeftCorner.y);
            Vector3 topLeftV = new Vector3(bottomLeftCorner.x, height, topRightCorner.y);
            Vector3 topRightV = new Vector3(topRightCorner.x, height, topRightCorner.y);

            Vector3[] vertices = {
                topLeftV,
                topRightV,
                bottomLeftV,
                bottomRightV
            };

            Vector2[] uvs = new Vector2[vertices.Length];

            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
            }

            // Place triangles inverted if it is not the floor
            int[] triangles = new int[6];
            if (isFloor)
            {
                triangles[0] = 0;
                triangles[1] = 1;
                triangles[2] = 2;
                triangles[3] = 2;
                triangles[4] = 1;
                triangles[5] = 3;
            }
            else
            {
                triangles[0] = 2;
                triangles[1] = 1;
                triangles[2] = 0;
                triangles[3] = 3;
                triangles[4] = 1;
                triangles[5] = 2;
            }
            
            Mesh mesh = new Mesh
            {
                vertices = vertices,
                uv = uvs,
                triangles = triangles
            };

            // Assign right parent and name
            var parentTransform = isFloor ? _floorParent.transform : _roofParent.transform;
            string dungeonMeshName = isFloor ? "Dungeon Floor " + index : "Dungeon Roof " + index;
            GameObject dungeonFloor = new GameObject(dungeonMeshName, typeof(MeshFilter), typeof(MeshRenderer))
            {
                transform =
                {
                    position = Vector3.zero,
                    localScale = Vector3.one,
                    parent = parentTransform
                }
            };

            dungeonFloor.GetComponent<MeshFilter>().mesh = mesh;
            dungeonFloor.GetComponent<MeshRenderer>().material = material;
            dungeonFloor.gameObject.AddComponent<BoxCollider>();
            dungeonFloor.layer = Layer.GroundLayer;
            dungeonFloor.isStatic = true;
            
            return dungeonFloor;
        }

        public void ResetDungeon()
        {
            foreach (Transform child in transform) {
                Destroy(child.gameObject);
            }
            
            CreateDungeon();
        }
        
        /*
        // For developing purpose to see if the points are on the correct position
        private void OnDrawGizmos()
        {
            if (_tempList == null) return;

            Gizmos.color = Color.black;

            foreach (var point in _tempList)
            {
                Gizmos.DrawSphere(point, 0.1f);
            }
        }
        */
    }    
}