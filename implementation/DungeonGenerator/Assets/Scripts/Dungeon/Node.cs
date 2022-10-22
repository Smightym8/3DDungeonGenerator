using System.Collections.Generic;
using UnityEngine;

namespace Dungeon
{
    public abstract class Node
    {
        private List<Node> _childrenNodes;
        public List<Node> ChildrenNodes => _childrenNodes;
        public Vector2Int BottomLeftAreaCorner { get; set; }
        public Vector2Int BottomRightAreaCorner { get; set; }
        public Vector2Int TopRightAreaCorner { get; set; }
        public Vector2Int TopLeftAreaCorner { get; set; }
        public int TreeLayerIndex { get; set; }
        public bool IsCorridor { get; set; }
        public bool IsHorizontalCorridor { get; set; }
        private Node Parent { get; }

        protected Node(Node parentNode)
        {
            _childrenNodes = new List<Node>();
            Parent = parentNode;

            if (Parent != null)
            {
                parentNode.AddChild(this);
            }
        }

        private void AddChild(Node node)
        {
            _childrenNodes.Add(node);
        }
    }
}
