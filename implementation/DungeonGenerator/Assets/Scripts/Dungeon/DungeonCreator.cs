using System;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;
using Random = System.Random;

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
        public int torchFrequencyInPercent = 20;
        public Material floorMaterial;
        public Material roofMaterial;
        public Material startRoomMaterial;
        public Material endRoomMaterial;
        public Material wallMaterial;

        [Range(0.0f, 0.3f)]
        public float roomBottomCornerModifier;
        [Range(0.7f, 1.0f)]
        public float roomTopCornerModifier;
        [Range(0.0f, 2.0f)]
        public int roomOffset;

        public GameObject playerPrefab;
        public GameObject torchPrefab;
        public GameObject doorPrefab;
        
        private List<Vector3Int> _possibleWallHorizontalPositions;
        private List<Vector3Int> _possibleWallVerticalPositions;
        private List<Vector3Int> _possibleTorchHorizontalPosition;
        private List<Vector3Int> _possibleTorchVerticalPosition;

        private readonly List<GameObject> _dungeonFloors = new();

        private List<Vector3> _vertices;
        private int[] _triangles;
        private Vector2[] _uvCoordinates;

        // To visualize points
        private List<Vector3> _testList = new List<Vector3>();

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
            
            _possibleWallHorizontalPositions = new List<Vector3Int>();
            _possibleWallVerticalPositions = new List<Vector3Int>();
            _possibleTorchHorizontalPosition = new List<Vector3Int>();
            _possibleTorchVerticalPosition = new List<Vector3Int>();
            
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

            _vertices = new List<Vector3>();

            foreach (var room in listOfRooms)
            {
                CreateWalls(room);

                if (room.IsCorridor)
                {
                    PlaceDoors(room);
                }
            }

            SpawnPlayer(playerPrefab, (RoomNode) listOfRooms[0]);
        }

        private void PlaceTorch(Vector3Int torchPosition)
        {
            torchPosition.y = dungeonHeight - 1;
            
            Instantiate(torchPrefab, torchPosition, Quaternion.identity);
        }

        private void PlaceDoors(Node room)
        {
            Vector3 positionFirstDoor;
            Vector3 positionSecondDor;
            bool isHorizontalDoor = false;
            
            if (!room.IsHorizontalCorridor)
            {
                int bottomLengthHalf = Math.Abs(room.BottomRightAreaCorner.x - room.BottomLeftAreaCorner.x) / 2;
                int topLengthHalf = Math.Abs(room.TopRightAreaCorner.x - room.TopLeftAreaCorner.x) / 2;
                positionFirstDoor = new Vector3(room.BottomLeftAreaCorner.x + bottomLengthHalf, 0, room.BottomLeftAreaCorner.y);
                positionSecondDor = new Vector3(room.TopLeftAreaCorner.x + topLengthHalf, 0, room.TopLeftAreaCorner.y);
            }
            else
            {
                int leftLengthHalf = Math.Abs(room.TopLeftAreaCorner.y - room.BottomLeftAreaCorner.y) / 2;
                int rightLengthHalf = Math.Abs(room.TopRightAreaCorner.y - room.BottomRightAreaCorner.y) / 2;
                positionFirstDoor = new Vector3(room.BottomLeftAreaCorner.x, 0, room.BottomLeftAreaCorner.y + leftLengthHalf);
                positionSecondDor = new Vector3(room.BottomRightAreaCorner.x, 0, room.BottomRightAreaCorner.y  + rightLengthHalf);
                isHorizontalDoor = true;
            }
            
            var door1 = Instantiate(doorPrefab, positionFirstDoor, Quaternion.identity);
            var door2 = Instantiate(doorPrefab, positionSecondDor, Quaternion.identity);

            if (isHorizontalDoor)
            {
                var ninetyDegreesRotation = new Vector3(0, 90, 0);
                door1.transform.Rotate(ninetyDegreesRotation);
                door2.transform.Rotate(ninetyDegreesRotation);
            }
        }

        private void CreateWalls(Node room)
        {
            // Horizontal
            var vertices = CollectHorizontalWallVertices(room.BottomLeftAreaCorner, room.BottomRightAreaCorner);
            CreateWallMesh(vertices, room.BottomLeftAreaCorner, room.BottomRightAreaCorner, true,true);

            // Horizontal
            vertices = CollectHorizontalWallVertices(room.TopLeftAreaCorner, room.TopRightAreaCorner);
            CreateWallMesh(vertices, room.TopLeftAreaCorner, room.TopRightAreaCorner, true, false);

            // Vertical
            vertices = CollectVerticalWallVertices(room.BottomLeftAreaCorner, room.TopLeftAreaCorner);
            CreateWallMesh(vertices, room.BottomLeftAreaCorner, room.TopLeftAreaCorner, false, false);

            // Vertical
            vertices = CollectVerticalWallVertices(room.BottomRightAreaCorner, room.TopRightAreaCorner);
            CreateWallMesh(vertices, room.BottomRightAreaCorner, room.TopRightAreaCorner, false, true);
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
            wall.layer = Layer.WallLayer;
            wall.isStatic = true;
        }
        
        private void AddVertexToList(Vector3 vertex)
        {
            _vertices.Add(vertex);
            /*
            if (_vertices.Contains(vertex))
            {
                _vertices.Remove(vertex);
            }
            else
            {
                _vertices.Add(vertex);    
            }
            */
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

            bool isGettingTorch = false;
            
            for (int row = (int) bottomLeftV.x; row < (int) bottomRightV.x; row++)
            {
                var wallPosition = new Vector3(row, 0, bottomLeftV.z);
                isGettingTorch = GenerateRandomBoolean();
                AddWallPositionToList(wallPosition, _possibleWallHorizontalPositions, _possibleTorchHorizontalPosition, isGettingTorch);
            }

            for (int row = (int) topLeftV.x; row < (int) topRightCorner.x; row++)
            {
                var wallPosition = new Vector3(row, 0, topRightV.z);
                isGettingTorch = GenerateRandomBoolean();
                AddWallPositionToList(wallPosition, _possibleWallHorizontalPositions, _possibleTorchHorizontalPosition, isGettingTorch);
            }

            for (int col = (int) bottomLeftV.z; col < (int) topLeftV.z; col++)
            {
                var wallPosition = new Vector3(bottomLeftV.x, 0, col);
                isGettingTorch = GenerateRandomBoolean();
                AddWallPositionToList(wallPosition, _possibleWallVerticalPositions, _possibleTorchVerticalPosition, isGettingTorch);
            }
            
            for (int col = (int) bottomRightV.z; col < (int) topRightV.z; col++)
            {
                var wallPosition = new Vector3(bottomRightV.x, 0, col);
                isGettingTorch = GenerateRandomBoolean();
                AddWallPositionToList(wallPosition, _possibleWallVerticalPositions, _possibleTorchVerticalPosition, isGettingTorch);
            }

            return dungeonFloor;
        }

        /// <summary>
        /// This method searches the positions for the walls and adds them to the wallList
        /// </summary>
        /// <param name="wallPosition">Contains the position for the wall</param>
        /// <param name="wallList">Is the list where the wall positions will be added</param>
        /// <param name="torchList">Contains the positions of the torches</param>
        private void AddWallPositionToList(Vector3 wallPosition, List<Vector3Int> wallList, List<Vector3Int> torchList, bool isWallGettingTorch)
        {
            Vector3Int point = Vector3Int.CeilToInt(new Vector3(wallPosition.x, 0, wallPosition.z));

            if (wallList.Contains(point))
            {
                wallList.Remove(point);
            }
            else
            {
                wallList.Add(point);

                if (isWallGettingTorch)
                {
                    torchList.Add(point);
                }
            }
        }

        private bool GenerateRandomBoolean()
        {
            Random random = new Random();
            if(random.Next(100) < torchFrequencyInPercent)
            {
                return true;
            }

            return false;
        }
        
        // For developing purpose to see if the points are on the correct position
        private void OnDrawGizmos()
        {
            if (_possibleTorchHorizontalPosition == null) return;
            if (_possibleTorchVerticalPosition == null) return;

            Gizmos.color = Color.black;

            foreach (var point in _possibleTorchHorizontalPosition)
            {
                Gizmos.DrawSphere(point, 0.1f);
            }
            
            foreach (var point in _possibleTorchVerticalPosition)
            {
                Gizmos.DrawSphere(point, 0.1f);
            }
        }
    }    
}