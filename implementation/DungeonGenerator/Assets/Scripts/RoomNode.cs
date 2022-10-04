using UnityEngine;

public class RoomNode : Node
{
    public int Width => TopRightAreaCorner.x - BottomLeftAreaCorner.x;
    public int Length => TopRightAreaCorner.y - BottomLeftAreaCorner.y;
    
    public RoomNode(Vector2Int bottomLeftAreaCorner, Vector2Int topRightAreaCorner, 
                    Node parentNode, int index) : base(parentNode)
    {
        BottomLeftAreaCorner = bottomLeftAreaCorner;
        TopRightAreaCorner = topRightAreaCorner;
        BottomRightAreaCorner = new Vector2Int(topRightAreaCorner.x, bottomLeftAreaCorner.y);
        TopLeftAreaCorner = new Vector2Int(bottomLeftAreaCorner.x, topRightAreaCorner.y);
        TreeLayerIndex = index;
    }
}
