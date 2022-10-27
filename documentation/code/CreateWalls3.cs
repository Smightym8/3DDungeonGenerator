int length = isHorizontal ? endCorner.x - startCorner.x :
                            endCorner.y - startCorner.y;
...
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
...