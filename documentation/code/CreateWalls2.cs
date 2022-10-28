private List<Vector3> CollectWallVertices(
        Vector2Int startCorner,
        Vector2Int endCorner,
        bool isHorizontal
    )
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