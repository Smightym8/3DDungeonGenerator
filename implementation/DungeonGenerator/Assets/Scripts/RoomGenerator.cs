using System.Collections.Generic;
using UnityEngine;

public class RoomGenerator
{
    private int _maxIterations;
    private int _roomWidthMin;
    private int _roomLengthMin;

    public RoomGenerator(int maxIterations, int roomWidthMin, int roomLengthMin)
    {
        _maxIterations = maxIterations;
        _roomWidthMin = roomWidthMin;
        _roomLengthMin = roomLengthMin;
    }

    public List<RoomNode> GenerateRoomsInGivenSpace(List<Node> roomSpaces, float roomBottomCornerModifier, 
        float roomTopCornerModifier, int roomOffset)
    {
        List<RoomNode> listToReturn = new List<RoomNode>();

        foreach (var space in roomSpaces)
        {
            Vector2Int newBottomLeftPoint = StructureHelper.GenerateBottomLeftCornerBetween(
                space.BottomLeftAreaCorner,
                space.TopRightAreaCorner,
                roomBottomCornerModifier,
                roomOffset
            );
            
            Vector2Int newTopRightPoint = StructureHelper.GenerateTopRightCornerBetween(
                space.BottomLeftAreaCorner,
                space.TopRightAreaCorner,
                roomTopCornerModifier,
                roomOffset
            );

            space.BottomLeftAreaCorner = newBottomLeftPoint;
            space.TopRightAreaCorner = newTopRightPoint;
            space.BottomRightAreaCorner = new Vector2Int(newTopRightPoint.x, newBottomLeftPoint.y);
            space.TopLeftAreaCorner = new Vector2Int(newBottomLeftPoint.x, newTopRightPoint.y);
            
            listToReturn.Add((RoomNode) space);
        }
        
        return listToReturn;
    }
}