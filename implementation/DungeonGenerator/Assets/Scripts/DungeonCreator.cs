using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class DungeonCreator : MonoBehaviour
{
    public int dungeonWidth, dungeonLength;
    public int roomWidthMin, roomLengthMin;
    public int maxIterations;
    public int corridorWidth;
    public Material material;
    [Range(0.0f, 0.3f)]
    public float roomBottomCornerModifier;
    [Range(0.7f, 1.0f)]
    public float roomTopCornerModifier;
    [Range(0.0f, 2.0f)]
    public int roomOffset;
    public GameObject wallVertical, wallHorizontal;
    
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

        foreach (var room in listOfRooms)
        {
            CreateMesh(room.BottomLeftAreaCorner, room.TopRightAreaCorner);
        }

        GameObject wallParent = new GameObject("WallParent");
        wallParent.transform.parent = transform;
        
        CreateWalls(wallParent);
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
        Instantiate(wallPrefab, wallPosition, Quaternion.identity, wallParent.transform);
    }

    private void CreateMesh(Vector2 bottomLeftCorner, Vector2 topRightCorner)
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

        GameObject dungeonFloor = new GameObject("Mesh", typeof(MeshFilter), typeof(MeshRenderer))
        {
            transform =
            {
                position = Vector3.zero,
                localScale = Vector3.one
            }
        };
        
        dungeonFloor.GetComponent<MeshFilter>().mesh = mesh;
        dungeonFloor.GetComponent<MeshRenderer>().material = material;

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
