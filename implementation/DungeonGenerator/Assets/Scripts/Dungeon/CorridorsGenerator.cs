using System.Collections.Generic;
using System.Linq;

public class CorridorsGenerator
{
    public List<Node> CreateCorridors(List<RoomNode> allNodes, int corridorWidth)
    {
        List<Node> corridorList = new List<Node>();
        Queue<RoomNode> structuresToCheck = new Queue<RoomNode>(
            allNodes.OrderByDescending(node => node.TreeLayerIndex)
        );

        while (structuresToCheck.Count > 0)
        {
            var node = structuresToCheck.Dequeue();

            if (node.ChildrenNodes.Count == 0)
            {
                continue;
            }

            CorridorNode corridor = new CorridorNode(node.ChildrenNodes[0], node.ChildrenNodes[1], corridorWidth);
            corridorList.Add(corridor);
        }
        
        return corridorList;
    }
}