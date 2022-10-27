...
foreach (var (room, index) in listOfRooms.Select((room, index) => ( room, index )))
{
    ...
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
...