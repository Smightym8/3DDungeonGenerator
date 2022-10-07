using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Linq;

public class DungeonCreator : MonoBehaviour
{
    public int dungeonWidth, dungeonLength;
    public int roomWidthMin, roomLengthMin;
    public int maxIterations;
    public int corridorWidth;
    public Material floorMaterial;
    public Material startRoomMaterial;
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
    
    // Unity Methods
    // Start is called before the first frame update
    void Start()
    {
        Stopwatch sw = new Stopwatch();
    
        sw.Start();
        CreateDungeon();
        sw.Stop();
        
        UnityEngine.Debug.Log($"Dungeon creation time: {sw.Elapsed}");
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
        
        foreach (var (room, index) in listOfRooms.Select((room, index) => ( room, index )))
        {
            // Choose first generated room as start room
            if (index == 0)
            {
                CreateMesh(room.BottomLeftAreaCorner, room.TopRightAreaCorner, startRoomMaterial);    
            }
            else
            {
                CreateMesh(room.BottomLeftAreaCorner, room.TopRightAreaCorner, floorMaterial);
            }
        }

        GameObject wallParent = new GameObject("WallParent");
        wallParent.transform.parent = transform;
        wallParent.layer = dungeonLayerIndex;
        
        CreateWalls(wallParent);

        SpawnPlayer(playerPrefab, (RoomNode) listOfRooms[0]);
    }

    private void SpawnPlayer(GameObject player, RoomNode startRoom)
    {
        player.transform.position = startRoom.CentrePoint;
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

    private void CreateMesh(Vector2 bottomLeftCorner, Vector2 topRightCorner, Material material)
    {
        Vector3 bottomLeftV = new Vector3(bottomLeftCorner.x, 0, bottomLeftCorner.y);
        Vector3 bottomRightV = new Vector3(topRightCorner.x, 0, bottomLeftCorner.y);
        Vector3 topLeftV = new Vector3(bottomLeftCorner.x, 0, topRightCorner.y);
        Vector3 topRightV = new Vector3(topRightCorner.x, 0, topRightCorner.y);

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

        int[] triangles = {
            0,
            1,
            2,
            2,
            1,
            3
        };

        Mesh mesh = new Mesh
        {
            vertices = vertices,
            uv = uvs,
            triangles = triangles
        };

        GameObject dungeonFloor = new GameObject("Dungeon Mesh", typeof(MeshFilter), typeof(MeshRenderer))
        {
            transform =
            {
                position = Vector3.zero,
                localScale = Vector3.one
            }
        };
        
        dungeonFloor.GetComponent<MeshFilter>().mesh = mesh;
        dungeonFloor.GetComponent<MeshRenderer>().material = material;
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
