using System.Collections.Generic;
using UnityEngine;

public abstract class Node
{
    private List<Node> _childrenNodes;
    public List<Node> ChildrenNodes => _childrenNodes;
    public bool Visited { get; set; }
    public Vector2Int BottomLeftAreaCorner { get; set; }
    public Vector2Int BottomRightAreaCorner { get; set; }
    public Vector2Int TopRightAreaCorner { get; set; }
    public Vector2Int TopLeftAreaCorner { get; set; }
    public int TreeLayerIndex { get; set; }
    public Node Parent { get; set; }

    public Node(Node parentNode)
    {
        _childrenNodes = new List<Node>();
        Parent = parentNode;

        if (Parent != null)
        {
            parentNode.AddChild(this);
        }
    }

    public void AddChild(Node node)
    {
        _childrenNodes.Add(node);
    }

    public void RemoveChild(Node node)
    {
        _childrenNodes.Remove(node);
    }
}
