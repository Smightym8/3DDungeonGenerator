foreach (var (room, index) in listOfRooms.Select((room, index) => ( room, index )))
{
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
    ...
}