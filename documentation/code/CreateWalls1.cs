// Bottom horizontal wall
if (corridor.TopLeftAreaCorner.y == room.BottomLeftAreaCorner.y &&
    room.BottomLeftAreaCorner.x < corridor.TopLeftAreaCorner.x &&
    corridor.TopLeftAreaCorner.x < room.BottomRightAreaCorner.x &&
    room.BottomLeftAreaCorner.x < corridor.TopRightAreaCorner.x &&
    corridor.TopRightAreaCorner.x < room.BottomRightAreaCorner.x)
{
    isCorridorBetweenHorizontalBottom = true;

    vertices = CollectHorizontalWallVertices(
        room.BottomLeftAreaCorner, corridor.TopLeftAreaCorner
    );
    CreateWallMesh(
        vertices, room.BottomLeftAreaCorner, corridor.TopLeftAreaCorner, true,true
    );
    vertices = CollectHorizontalWallVertices(
        corridor.TopRightAreaCorner, room.BottomRightAreaCorner
    );
    CreateWallMesh(
        vertices, corridor.TopRightAreaCorner, room.BottomRightAreaCorner, true, true
    );
}
...