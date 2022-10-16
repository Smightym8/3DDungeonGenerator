using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace Dungeon
{
    public class DungeonCreator : MonoBehaviour
    {
        public int dungeonWidth, dungeonLength;
        public int roomWidthMin, roomLengthMin;
        public int roomWidthMax, roomLengthMax;
        public int maxIterations;
        public int corridorWidth;
        public Material floorMaterial;
        public Material roofMaterial;
        public Material startRoomMaterial;
        public Material endRoomMaterial;
        public int dungeonLayerIndex = 6; // Dungeon layer for the camera
        
        [Range(0.0f, 0.3f)]
        public float roomBottomCornerModifier;
        [Range(0.7f, 1.0f)]
        public float roomTopCornerModifier;
        [Range(0.0f, 2.0f)]
        public int roomOffset;
        public GameObject wallVertical, wallHorizontal;
        
        public GameObject playerPrefab;
        public List<GameObject> enemyPrefabs;
        public List<RuntimeAnimatorController> enemyAnimators;
        public List<Avatar> enemyAvatars;
        
        private List<Vector3Int> _possibleDoorVerticalPositions;
        private List<Vector3Int> _possibleDoorHorizontalPositions;
        private List<Vector3Int> _possibleWallHorizontalPositions;
        private List<Vector3Int> _possibleWallVerticalPositions;

        private readonly List<GameObject> _dungeonFloors = new();

        // Unity Methods
        void Start()
        {
            Stopwatch sw = new Stopwatch();
        
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
            
            _possibleDoorVerticalPositions = new List<Vector3Int>();
            _possibleDoorHorizontalPositions = new List<Vector3Int>(); 
            _possibleWallHorizontalPositions = new List<Vector3Int>();
            _possibleWallVerticalPositions = new List<Vector3Int>();

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
                        CreateMesh(
                            room.BottomLeftAreaCorner,
                            room.TopRightAreaCorner,
                            startRoomMaterial,
                            0,
                            true
                        )
                    );
                }
                else
                {
                    _dungeonFloors.Add(
                        CreateMesh(
                            room.BottomLeftAreaCorner,
                            room.TopRightAreaCorner,
                            floorMaterial,
                            0,
                            true
                        )
                    );
                }
        
                // Check distance between start room and each other room
                // to find the room which is the most distant from the start room
                if (index < listOfRooms.Count / 2 && index > 0)
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

            var wallParent = new GameObject("WallParent")
            {
                transform = { parent = transform },
                layer = dungeonLayerIndex
            };

            CreateWalls(wallParent);

            // Create the roof
            foreach (var room in listOfRooms)
            {
                _dungeonFloors.Add(
                    CreateMesh(
                        room.BottomLeftAreaCorner,
                        room.TopRightAreaCorner,
                        roofMaterial,
                        6,
                        false
                    )
                );
            }
            
            SpawnPlayer(playerPrefab, (RoomNode) listOfRooms[0]);

            for (int i = 1; i < listOfRooms.Count; i++)
            {
                SpawnEnemy(enemyPrefabs[0], enemyAnimators[0], enemyAvatars[0], (RoomNode) listOfRooms[i]);    
            }
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
        /// This method spawns an enemy in a given room
        /// </summary>
        /// <param name="enemy">Contains the prefab of the enemy</param>
        /// <param name="animatorController">Contains the controller that will be used for the animations of the enemy</param>
        /// <param name="avatar">Contains the avatar that is needed for the animations</param>
        /// <param name="room">Contains the room where the enemy will be spawned</param>
        private void SpawnEnemy(GameObject enemy, RuntimeAnimatorController animatorController, Avatar avatar, RoomNode room)
        {
            var spawnPosition = room.CentrePoint;
            spawnPosition.y = 0.02f;

            var instantiatedEnemy = Instantiate(enemy);
            instantiatedEnemy.transform.position = spawnPosition;
            
            // Add animation controller to instantiated enemy
            instantiatedEnemy.AddComponent<Animator>();
            Animator animator = instantiatedEnemy.GetComponent<Animator>();
            animator.runtimeAnimatorController = animatorController;
            animator.avatar = avatar;
            
            // Add collider
            instantiatedEnemy.AddComponent<BoxCollider>();
            var boxCollider = instantiatedEnemy.GetComponent<BoxCollider>();
            boxCollider.center = new Vector3(0.3f,0.8f,0);
            boxCollider.size = new Vector3(1.5f, 1.5f, 0.5f);
            
            // Add Rigidbody
            instantiatedEnemy.AddComponent<Rigidbody>();
            var rigidBody = instantiatedEnemy.GetComponent<Rigidbody>();
            rigidBody.isKinematic = true;
            
            // Add scripts
            instantiatedEnemy.AddComponent<Enemy.EnemyMovement>();
            instantiatedEnemy.AddComponent<Health>();
            
            // Assign animator to enemy movement script
            var enemyMovementScript = instantiatedEnemy.GetComponent<Enemy.EnemyMovement>();
            enemyMovementScript.animator = animator;
            
            // Add tag
            instantiatedEnemy.tag = Tag.Enemy;
        }

        /// <summary>
        /// This method creates the walls of the dungeons.
        /// </summary>
        /// <param name="wallParent">Is the parent game object for all the walls</param>
        private void CreateWalls(GameObject wallParent)
        {
            foreach (var wallPosition in _possibleWallHorizontalPositions)
            {
                CreateWall(wallParent, wallPosition, wallHorizontal);
            }

            foreach (var wallPosition in _possibleWallVerticalPositions)
            {
                CreateWall(wallParent, wallPosition, wallVertical);
            }
        }

        /// <summary>
        /// This method creates a new wall game object
        /// </summary>
        /// <param name="wallParent">Is the parent game object of the wall that will be created</param>
        /// <param name="wallPosition">Contains the position where the wall will be created</param>
        /// <param name="wallPrefab">Is the prefab for the wall object</param>
        private void CreateWall(GameObject wallParent, Vector3Int wallPosition, GameObject wallPrefab)
        {
            GameObject wall = Instantiate(wallPrefab, wallPosition, Quaternion.identity, wallParent.transform);
            wall.layer = dungeonLayerIndex;
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
        /// <returns></returns>
        private GameObject CreateMesh(Vector2 bottomLeftCorner, Vector2 topRightCorner, Material material, int height, bool isFloor)
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

            GameObject dungeonFloor = new GameObject("Dungeon Floor Mesh", typeof(MeshFilter), typeof(MeshRenderer))
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
            dungeonFloor.layer = dungeonLayerIndex;

            for (int row = (int) bottomLeftV.x; row < (int) bottomRightV.x; row++)
            {
                var wallPosition = new Vector3(row, 0, bottomLeftV.z);
                AddWallPositionToList(wallPosition, _possibleWallHorizontalPositions, _possibleDoorHorizontalPositions);
            }

            for (int row = (int) topLeftV.x; row < (int) topRightCorner.x; row++)
            {
                var wallPosition = new Vector3(row, 0, topRightV.z);
                AddWallPositionToList(wallPosition, _possibleWallHorizontalPositions, _possibleDoorHorizontalPositions);
            }

            for (int col = (int) bottomLeftV.z; col < (int) topLeftV.z; col++)
            {
                var wallPosition = new Vector3(bottomLeftV.x, 0, col);
                AddWallPositionToList(wallPosition, _possibleWallVerticalPositions, _possibleDoorVerticalPositions);
            }
            
            for (int col = (int) bottomRightV.z; col < (int) topRightV.z; col++)
            {
                var wallPosition = new Vector3(bottomRightV.x, 0, col);
                AddWallPositionToList(wallPosition, _possibleWallVerticalPositions, _possibleDoorVerticalPositions);
            }

            return dungeonFloor;
        }

        /// <summary>
        /// This method searches the positions for the walls and adds them to a the wallList
        /// </summary>
        /// <param name="wallPosition">Contains the position for the wall</param>
        /// <param name="wallList">Is the list where the wall positions will be added</param>
        /// <param name="doorList">Contains the positions of the doors for the corridors</param>
        private void AddWallPositionToList(Vector3 wallPosition, List<Vector3Int> wallList, List<Vector3Int> doorList)
        {
            Vector3Int point = Vector3Int.CeilToInt(wallPosition);

            if (wallList.Contains(point))
            {
                doorList.Add(point);
                wallList.Remove(point);
            }
            else
            {
                wallList.Add(point);
            }
        }
    }    
}

