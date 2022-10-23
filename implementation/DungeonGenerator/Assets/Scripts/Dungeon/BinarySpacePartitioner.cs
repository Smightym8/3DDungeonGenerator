using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Dungeon
{
    public class BinarySpacePartitioner
    {
        private RoomNode _rootNode;
        public RoomNode RootNode => _rootNode;

        public BinarySpacePartitioner(int dungeonWidth, int dungeonLength)
        {
            _rootNode = new RoomNode(
                new Vector2Int(0,0),
                new Vector2Int(dungeonWidth, dungeonLength),
                null,
                0
            );
        }

        public List<RoomNode> PrepareNodesCollection(int maxIterations, int roomWidthMin, int roomLengthMin, 
            int roomWidthMax, int roomLengthMax)
        {
            Queue<RoomNode> graph = new Queue<RoomNode>();
            List<RoomNode> listToReturn = new List<RoomNode>();
            graph.Enqueue(_rootNode);
            listToReturn.Add(_rootNode);
        
            int iterations = 0;
            while ((iterations < maxIterations) && (graph.Count > 0))
            {
                iterations++;
            
                RoomNode currentNode = graph.Dequeue();
                if ((currentNode.Width >= roomWidthMin * 2) || (currentNode.Length >= roomLengthMin * 2) || 
                    (currentNode.Width > roomWidthMax * 2) || (currentNode.Length > roomLengthMax * 2))
                {
                    SplitSpace(currentNode, listToReturn, roomLengthMin, roomWidthMin, graph);
                }
            }
        
            return listToReturn;
        }

        private void SplitSpace(RoomNode currentNode, List<RoomNode> listToReturn, int roomLengthMin, int roomWidthMin, Queue<RoomNode> graph)
        {
            Line line = GetLineDividingSpace(
                currentNode.BottomLeftAreaCorner, 
                currentNode.TopRightAreaCorner, 
                roomWidthMin,
                roomLengthMin
            );

            RoomNode node1, node2;

            if (line.Orientation == Orientation.Horizontal)
            {
                node1 = new RoomNode(
                    currentNode.BottomLeftAreaCorner,
                    new Vector2Int(currentNode.TopRightAreaCorner.x, line.Coordinates.y),
                    currentNode,
                    currentNode.TreeLayerIndex + 1
                );
            
                node2 = new RoomNode(
                    new Vector2Int(currentNode.BottomLeftAreaCorner.x, line.Coordinates.y),
                    currentNode.TopRightAreaCorner,
                    currentNode,
                    currentNode.TreeLayerIndex + 1
                );
            }
            else
            {
                node1 = new RoomNode(
                    currentNode.BottomLeftAreaCorner,
                    new Vector2Int(line.Coordinates.x, currentNode.TopRightAreaCorner.y),
                    currentNode,
                    currentNode.TreeLayerIndex + 1
                );
            
                node2 = new RoomNode(
                    new Vector2Int(line.Coordinates.x, currentNode.BottomLeftAreaCorner.y),
                    currentNode.TopRightAreaCorner,
                    currentNode,
                    currentNode.TreeLayerIndex + 1
                );
            }

            AddNewNodeToCollection(listToReturn, graph, node1);
            AddNewNodeToCollection(listToReturn, graph, node2);
        }

        private void AddNewNodeToCollection(List<RoomNode> listToReturn, Queue<RoomNode> graph, RoomNode node)
        {
            listToReturn.Add(node);
            graph.Enqueue(node);
        }

        private Line GetLineDividingSpace(Vector2Int currentNodeBottomLeftAreaCorner, Vector2Int currentNodeTopRightAreaCorner, int roomWidthMin, int roomLengthMin)
        {
            Orientation orientation;
            bool lengthStatus = (currentNodeTopRightAreaCorner.y - currentNodeBottomLeftAreaCorner.y) >= (2 * roomLengthMin);
            bool widthStatus = (currentNodeTopRightAreaCorner.x - currentNodeBottomLeftAreaCorner.x) >= (2 * roomWidthMin);

            if (lengthStatus && widthStatus)
            {
                orientation = (Orientation)(Random.Range(0, 2));
            }
            else if (widthStatus)
            {
                orientation = Orientation.Vertical;
            }
            else
            {
                orientation = Orientation.Horizontal;
            }

            Vector2Int coordinates = GetCoordinatesForOrientation(orientation, currentNodeBottomLeftAreaCorner, 
                currentNodeTopRightAreaCorner, roomWidthMin, roomLengthMin);
        
            return new Line(orientation, coordinates);
        }

        private Vector2Int GetCoordinatesForOrientation(Orientation orientation, Vector2Int currentNodeBottomLeftAreaCorner, Vector2Int currentNodeTopRightAreaCorner, int roomWidthMin, int roomLengthMin)
        {
            Vector2Int coordinates;

            if (orientation == Orientation.Horizontal)
            {
                coordinates = new Vector2Int(
                    0, 
                    Random.Range(
                        (currentNodeBottomLeftAreaCorner.y + roomLengthMin),
                        (currentNodeTopRightAreaCorner.y - roomLengthMin))
                );
            }
            else
            {
                coordinates = new Vector2Int(
                    Random.Range(
                        (currentNodeBottomLeftAreaCorner.x + roomWidthMin),
                        (currentNodeTopRightAreaCorner.x - roomWidthMin)),
                    0
                );
            }

            return coordinates;
        }
    }
}
