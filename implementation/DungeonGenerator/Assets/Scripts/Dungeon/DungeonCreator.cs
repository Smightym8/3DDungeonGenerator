using System;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

// TODO: Combine meshes
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
        public Material floorMaterial;
        public Material roofMaterial;
        public Material startRoomMaterial;
        public Material endRoomMaterial;
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
        public List<GameObject> sceneryPrefabs;

        private Dictionary<Vector3Int, int> _lightPositions;
        private List<Vector3Int> _ignoreLightPositions;

        private readonly List<GameObject> _dungeonFloors = new();
        
        private int[] _triangles;
        private Vector2[] _uvCoordinates;

        // Unity Methods
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
            var listOfRooms = generator.CalculateDungeon(
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

            _lightPositions = new Dictionary<Vector3Int, int>();
            _ignoreLightPositions = new List<Vector3Int>();

            RoomNode startRoom = (RoomNode) listOfRooms[0];
            GameObject endRoom = null;
            float maxDistance = 0f;
            
            foreach (var (room, index) in listOfRooms.Select((room, index) => ( room, index )))
            {
                // Create floor
                // Choose first generated room as start room
                if (index == 0)
                {
                    _dungeonFloors.Add(
                        CreateFloorMesh(
                            room.BottomLeftAreaCorner,
                            room.TopRightAreaCorner,
                            startRoomMaterial,
                            0,
                            true,
                            index
                        )
                    );
                }
                else
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
                }
        
                // Check distance between start room and each other room
                // to find the room which is the most distant from the start room
                if (index > 0 && index < listOfRooms.Count / 2)
                {
                    RoomNode currentRoom = (RoomNode)listOfRooms[index];
                    float dist = Vector3.Distance(startRoom.CentrePoint, currentRoom.CentrePoint);

                    if (!(maxDistance < dist)) continue;
            
                    maxDistance = dist;
                    endRoom = _dungeonFloors[index];
                }
            }

            // Visualize end room if one was found
            if (endRoom != null)
            {
                endRoom.GetComponent<MeshRenderer>().material = endRoomMaterial;    
            }

            // Create the roof
            foreach (var (room, index) in listOfRooms.Select((room, index) => ( room, index )))
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

            foreach (var room in listOfRooms)
            {
                CreateWalls(room, listOfRooms);
                CollectLightPositions(room);
            }
            
            foreach (var (position, rotation) in _lightPositions)
            {
                PlaceLight(position, rotation);
            }

            // Add random scenery to rooms
            /*
            for (int i = 0; i < listOfRooms.Count / 2; i++)
            {
                PlaceRandomScenery(listOfRooms[i]);
            }
            */
            
            SpawnPlayer(playerPrefab, (RoomNode) listOfRooms[0]);
        }

        private void CollectLightPositions(Node room)
        {
            Vector3 bottomLeftCorner = new Vector3(room.BottomLeftAreaCorner.x, 0, room.BottomLeftAreaCorner.y);
            Vector3 bottomRightCorner = new Vector3(room.BottomRightAreaCorner.x, 0, room.BottomRightAreaCorner.y);
            Vector3 topLeftCorner = new Vector3(room.TopLeftAreaCorner.x, 0, room.TopLeftAreaCorner.y);
            Vector3 topRightCorner = new Vector3(room.TopRightAreaCorner.x, 0, room.TopRightAreaCorner.y);

            for (var x = bottomLeftCorner.x; x <= bottomRightCorner.x; x++)
            {
                var position = new Vector3(x, 0, bottomLeftCorner.z);
                SaveLightPosition(position, 0);
            }
            
            for (var x = topLeftCorner.x; x <= topRightCorner.x; x++)
            {
                var position = new Vector3(x, 0, topLeftCorner.z);
                SaveLightPosition(position, 180);
            }

            for (var z = bottomLeftCorner.z; z <= topLeftCorner.z; z++)
            {
                var position = new Vector3(bottomLeftCorner.x, 0, z);
                SaveLightPosition(position, 90);
            }
            
            for (var z = bottomRightCorner.z; z <= topRightCorner.z; z++)
            {
                var position = new Vector3(bottomRightCorner.x, 0, z);
                SaveLightPosition(position, -90);
            }
        }

        private void SaveLightPosition(Vector3 position, int rotation)
        {
            // TODO: Don't choose every possible position to place lights
            Vector3Int point = Vector3Int.CeilToInt(position);

            if (_lightPositions.ContainsKey(point))
            {
                _lightPositions.Remove(point);
                _ignoreLightPositions.Add(point);
            }
            else if(!_ignoreLightPositions.Contains(point))
            {
                _lightPositions.Add(point, rotation);
            }
        }

        private void PlaceRandomScenery(Node room)
        {
            int randomSceneryIndex = Random.Range(0, sceneryPrefabs.Count);
            int randomX = Random.Range(room.BottomLeftAreaCorner.x + 1, room.BottomRightAreaCorner.x - 1);
            int randomZ = Random.Range(room.BottomLeftAreaCorner.y + 1, room.TopLeftAreaCorner.y - 1);
            var position = new Vector3(randomX, 0, randomZ);

            Instantiate(sceneryPrefabs[randomSceneryIndex], position, Quaternion.identity);
        }

        private void PlaceLight(Vector3Int lightPosition, int rotation)
        {
            lightPosition.y = dungeonHeight - 1;

            var lightGameObject = Instantiate(torchPrefab, lightPosition, Quaternion.identity);
            lightGameObject.transform.Rotate(new Vector3(0, rotation, 0));
            Light lightSource = lightGameObject.AddComponent<Light>();
            lightSource.type = LightType.Point;
            lightSource.intensity = 3;
            lightSource.range = 3;
            lightSource.color = lightColor;
        }

        private void CreateWalls(Node room, List<Node> rooms)
        {
            // When collect vertices for a room check if any corridor has its corner points
            // in the range of the wall

            List<Vector3> vertices;

            bool isCorridorBetweenHorizontalBottom = false;
            bool isCorridorBetweenHorizontalTop = false;
            bool isCorridorBetweenVerticalLeft = false;
            bool isCorridorBetweenVerticalRight = false;
            
            // Split wall into two walls
            // Check if there is a corridor between the start and endpoint of the wall
            // But only if it is a room and no corridor
            if (!room.IsCorridor)
            {
                for (var i = (rooms.Count / 2 + 1); i < rooms.Count; i++)
                {
                    Node corridor = rooms[i];

                    // Bottom horizontal wall
                    if (corridor.TopLeftAreaCorner.y == room.BottomLeftAreaCorner.y &&
                        room.BottomLeftAreaCorner.x < corridor.TopLeftAreaCorner.x &&
                        corridor.TopLeftAreaCorner.x < room.BottomRightAreaCorner.x &&
                        room.BottomLeftAreaCorner.x < corridor.TopRightAreaCorner.x &&
                        corridor.TopRightAreaCorner.x < room.BottomRightAreaCorner.x)
                    {
                        isCorridorBetweenHorizontalBottom = true;

                        vertices = CollectHorizontalWallVertices(room.BottomLeftAreaCorner, corridor.TopLeftAreaCorner);
                        CreateWallMesh(vertices, room.BottomLeftAreaCorner, corridor.TopLeftAreaCorner, true,true);
                        
                        vertices = CollectHorizontalWallVertices(corridor.TopRightAreaCorner, room.BottomRightAreaCorner);
                        CreateWallMesh(vertices, corridor.TopRightAreaCorner, room.BottomRightAreaCorner, true, true);
                    }
                    
                    // Top horizontal wall
                    if (corridor.BottomLeftAreaCorner.y == room.TopLeftAreaCorner.y &&
                        room.TopLeftAreaCorner.x < corridor.BottomLeftAreaCorner.x &&
                        corridor.BottomLeftAreaCorner.x < room.TopRightAreaCorner.x &&
                        room.TopLeftAreaCorner.x < corridor.BottomRightAreaCorner.x &&
                        corridor.BottomRightAreaCorner.x < room.TopRightAreaCorner.x)
                    {
                        isCorridorBetweenHorizontalTop = true;

                        vertices = CollectHorizontalWallVertices(room.TopLeftAreaCorner, corridor.BottomLeftAreaCorner);
                        CreateWallMesh(vertices, room.TopLeftAreaCorner, corridor.BottomLeftAreaCorner, true,false);
                        
                        vertices = CollectHorizontalWallVertices(corridor.BottomRightAreaCorner, room.TopRightAreaCorner);
                        CreateWallMesh(vertices, corridor.BottomRightAreaCorner, room.TopRightAreaCorner, true, false);
                    }
                    
                    // Left vertical wall
                    if (corridor.BottomRightAreaCorner.x == room.BottomLeftAreaCorner.x &&
                        room.BottomLeftAreaCorner.y < corridor.BottomRightAreaCorner.y &&
                        corridor.BottomRightAreaCorner.y < room.TopLeftAreaCorner.y &&
                        room.BottomLeftAreaCorner.y < corridor.TopRightAreaCorner.y &&
                        corridor.TopRightAreaCorner.y < room.TopLeftAreaCorner.y)
                    {
                        isCorridorBetweenVerticalLeft = true;
                        
                        vertices = CollectVerticalWallVertices(room.BottomLeftAreaCorner, corridor.BottomRightAreaCorner);
                        CreateWallMesh(vertices, room.BottomLeftAreaCorner, corridor.BottomRightAreaCorner, false,false);
                        
                        vertices = CollectVerticalWallVertices(corridor.TopRightAreaCorner, room.TopLeftAreaCorner);
                        CreateWallMesh(vertices, corridor.TopRightAreaCorner, room.TopLeftAreaCorner, false, false);
                    }
                    
                    // Right vertical
                    if (corridor.BottomLeftAreaCorner.x == room.BottomRightAreaCorner.x &&
                        room.BottomRightAreaCorner.y < corridor.BottomLeftAreaCorner.y &&
                        corridor.BottomLeftAreaCorner.y < room.TopRightAreaCorner.y &&
                        room.BottomRightAreaCorner.y < corridor.TopLeftAreaCorner.y &&
                        corridor.TopLeftAreaCorner.y < room.TopRightAreaCorner.y)
                    {
                        isCorridorBetweenVerticalRight = true;
                        
                        vertices = CollectVerticalWallVertices(room.BottomRightAreaCorner, corridor.BottomLeftAreaCorner);
                        CreateWallMesh(vertices, room.BottomRightAreaCorner, corridor.BottomLeftAreaCorner, false,true);
                        
                        vertices = CollectVerticalWallVertices(corridor.TopLeftAreaCorner, room.TopRightAreaCorner);
                        CreateWallMesh(vertices, corridor.TopLeftAreaCorner, room.TopRightAreaCorner, false, true);
                    }
                }    
            }
            
            // Create walls normal as one wall
            // Horizontal
            if (!room.IsCorridor || room.IsHorizontalCorridor)
            {
                // Bottom horizontal wall
                if (!isCorridorBetweenHorizontalBottom)
                {
                    vertices = CollectHorizontalWallVertices(room.BottomLeftAreaCorner, room.BottomRightAreaCorner);
                    CreateWallMesh(vertices, room.BottomLeftAreaCorner, room.BottomRightAreaCorner, true,true);
                }
                
                if (!isCorridorBetweenHorizontalTop)
                {
                    // Top horizontal wall
                    vertices = CollectHorizontalWallVertices(room.TopLeftAreaCorner, room.TopRightAreaCorner);
                    CreateWallMesh(vertices, room.TopLeftAreaCorner, room.TopRightAreaCorner, true, false);
                }
            }

            // Vertical
            if (!room.IsCorridor || !room.IsHorizontalCorridor)
            {
                if (!isCorridorBetweenVerticalLeft)
                {
                    // Left vertical wall
                    vertices = CollectVerticalWallVertices(room.BottomLeftAreaCorner, room.TopLeftAreaCorner);
                    CreateWallMesh(vertices, room.BottomLeftAreaCorner, room.TopLeftAreaCorner, false, false);    
                }

                if (!isCorridorBetweenVerticalRight)
                {
                    // Right vertical wall
                    vertices = CollectVerticalWallVertices(room.BottomRightAreaCorner, room.TopRightAreaCorner);
                    CreateWallMesh(vertices, room.BottomRightAreaCorner, room.TopRightAreaCorner, false, true);    
                }
            }
        }

        private List<Vector3> CollectHorizontalWallVertices(Vector2Int startCorner, Vector2Int endCorner)
        {
            var vertices = new List<Vector3>();
            
            for (var height = 0; height <= dungeonHeight; height++)
            {
                for (var x = startCorner.x; x <= endCorner.x; x++)
                {
                    var vertex = new Vector3(x, height, startCorner.y);
                    vertices.Add(vertex);
                }    
            }
            
            return vertices;
        }
        
        private List<Vector3> CollectVerticalWallVertices(Vector2Int startCorner, Vector2Int endCorner)
        {
            var vertices = new List<Vector3>();
            
            for (var height = 0; height <= dungeonHeight; height++)
            {
                for (var z = startCorner.y; z <= endCorner.y; z++)
                {
                    var vertex = new Vector3(startCorner.x, height, z);
                    vertices.Add(vertex);
                }
            }
            
            return vertices;
        }

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
                    localScale = Vector3.one
                }
            };
            
            wall.GetComponent<MeshFilter>().mesh = mesh;
            wall.GetComponent<MeshRenderer>().material = wallMaterial;
            // TODO: Add collider
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

            GameObject dungeonFloor = new GameObject("Dungeon Floor Mesh " + index, typeof(MeshFilter), typeof(MeshRenderer))
            {
                transform =
                {
                    position = Vector3.zero,
                    localScale = Vector3.one
                }
            };
            
            dungeonFloor.GetComponent<MeshFilter>().mesh = mesh;
            dungeonFloor.GetComponent<MeshRenderer>().material = material;
            dungeonFloor.gameObject.AddComponent<BoxCollider>();
            dungeonFloor.layer = Layer.GroundLayer;
            dungeonFloor.isStatic = true;
            
            return dungeonFloor;
        }


        // For developing purpose to see if the points are on the correct position
        private void OnDrawGizmos()
        {
            if (_lightPositions == null) return;

            Gizmos.color = Color.black;

            foreach (var (point, rotation) in _lightPositions)
            {
                Gizmos.DrawSphere(point, 0.1f);
            }
        }
    }    
}