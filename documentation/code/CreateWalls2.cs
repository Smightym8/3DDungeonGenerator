private List<Vector3> CollectHorizontalWallVertices(
        Vector2Int startCorner, Vector2Int endCorner
    )
{
    var vertices = new List<Vector3>();

    // Start in one corner of the room and go the the other corner of the room
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