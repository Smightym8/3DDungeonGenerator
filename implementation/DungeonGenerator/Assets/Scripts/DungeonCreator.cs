using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;

public class DungeonCreator : MonoBehaviour
{
    public int dungeonWidth, dungeonLength;
    public int roomWidthMin, roomLengthMin;
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
    
    private List<Vector3Int> _possibleDoorVerticalPositions;
    private List<Vector3Int> _possibleDoorHorizontalPositions;
    private List<Vector3Int> _possibleWallHorizontalPositions;
    private List<Vector3Int> _possibleWallVerticalPositions;

    private List<GameObject> _dungeonFloors = new List<GameObject>(); 
    
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
    private void CreateDungeon()
    {
        DungeonGenerator generator = new DungeonGenerator(dungeonWidth, dungeonLength);
        var listOfRooms = generator.CalculateDungeon(
            maxIterations, 
            roomWidthMin, 
            roomLengthMin,
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
            if (index < listOfRooms.Count / 2 && index > 0)
            {
                RoomNode currentRoom = (RoomNode)listOfRooms[index];
                float dist = Vector3.Distance(startRoom.CentrePoint, currentRoom.CentrePoint);

                if (!(maxDistance < dist)) continue;
        
                maxDistance = dist;
                endRoom = _dungeonFloors[index];
            }
        }

        if (endRoom != null)
        {
            endRoom.GetComponent<MeshRenderer>().material = endRoomMaterial;    
        }

        GameObject wallParent = new GameObject("WallParent");
        wallParent.transform.parent = transform;
        wallParent.layer = dungeonLayerIndex;
        
        CreateWalls(wallParent);

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
    }

    private void SpawnPlayer(GameObject player, RoomNode startRoom)
    {
        var transformPosition = startRoom.CentrePoint;
        transformPosition.y = 0.02f;
        player.transform.position = transformPosition;
    }

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

    private void CreateWall(GameObject wallParent, Vector3Int wallPosition, GameObject wallPrefab)
    {
        GameObject wall = Instantiate(wallPrefab, wallPosition, Quaternion.identity, wallParent.transform);
        wall.layer = dungeonLayerIndex;
    }

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
