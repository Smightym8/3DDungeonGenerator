using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Dungeon
{
    public class CorridorNode : Node
    {
        private Node _structure1;
        private Node _structure2;
        private int _corridorWidth;
        private int _modifierDistanceFromWall = 1;

        public CorridorNode(Node structure1, Node structure2, int corridorWidth) : base(null)
        {
            _structure1 = structure1;
            _structure2 = structure2;
            _corridorWidth = corridorWidth;
            IsCorridor = true;

            GenerateCorridor();
        }

        private void GenerateCorridor()
        {
            var relativePositionOfStructure2 = CheckPositionStructure2AgainstStructure1();

            switch (relativePositionOfStructure2)
            {
                case RelativePosition.Up:
                    ProcessRoomInRelationUpOrDown(_structure1, _structure2);
                    break;
                case RelativePosition.Down:
                    ProcessRoomInRelationUpOrDown(_structure2, _structure1);
                    break;
                case RelativePosition.Right:
                    ProcessRoomInRelationRightOrLeft(_structure1, _structure2);
                    break;
                case RelativePosition.Left:
                    ProcessRoomInRelationRightOrLeft(_structure2, _structure1);
                    break;
                default:
                    break;
            }
        }

        private void ProcessRoomInRelationRightOrLeft(Node structure1, Node structure2)
        {
            Node leftStructure;
            var leftStructureChildren = StructureHelper.TraverseGraphToExtractLowestLeaves(structure1);
            var rightStructureChildren = StructureHelper.TraverseGraphToExtractLowestLeaves(structure2);

            var sortedLeftStructure = leftStructureChildren.OrderByDescending(child => child.TopRightAreaCorner.x).ToList();
            if (sortedLeftStructure.Count == 1)
            {
                leftStructure = sortedLeftStructure[0];
            }
            else
            {
                int maxX = sortedLeftStructure[0].TopRightAreaCorner.x;
                sortedLeftStructure = sortedLeftStructure.Where(child => 
                    Math.Abs(maxX - child.TopRightAreaCorner.x) < 10
                ).ToList();

                int index = Random.Range(0, sortedLeftStructure.Count);
                leftStructure = sortedLeftStructure[index];
            }

            var possibleNeighboursInRightStructureList = rightStructureChildren.Where(child => 
                GetValidNeighbourForLeftRight(
                    leftStructure.TopRightAreaCorner,
                    leftStructure.BottomRightAreaCorner,
                    child.TopLeftAreaCorner,
                    child.BottomLeftAreaCorner
                ) != -1        
            ).OrderBy(child => child.BottomRightAreaCorner.x).ToList();

            var rightStructure = possibleNeighboursInRightStructureList.Count <= 0 ? structure2 : possibleNeighboursInRightStructureList[0];

            int y = GetValidNeighbourForLeftRight(
                leftStructure.TopRightAreaCorner,
                leftStructure.BottomRightAreaCorner,
                rightStructure.TopLeftAreaCorner,
                rightStructure.BottomLeftAreaCorner
            );

            while (y == -1 && sortedLeftStructure.Count > 1)
            {
                sortedLeftStructure = sortedLeftStructure.Where(child => 
                    child.TopLeftAreaCorner.y != leftStructure.TopLeftAreaCorner.y
                ).ToList();

                leftStructure = sortedLeftStructure[0];
                y = GetValidNeighbourForLeftRight(
                    leftStructure.TopRightAreaCorner,
                    leftStructure.BottomRightAreaCorner,
                    rightStructure.TopLeftAreaCorner,
                    rightStructure.BottomLeftAreaCorner
                );
            }

            BottomLeftAreaCorner = new Vector2Int(leftStructure.BottomRightAreaCorner.x, y);
            TopRightAreaCorner = new Vector2Int(rightStructure.TopLeftAreaCorner.x, y + _corridorWidth);
            BottomRightAreaCorner = new Vector2Int(TopRightAreaCorner.x, BottomLeftAreaCorner.y);
            TopLeftAreaCorner = new Vector2Int(BottomLeftAreaCorner.x, TopRightAreaCorner.y);
            IsHorizontalCorridor = true;
        }

        private int GetValidNeighbourForLeftRight(Vector2Int leftNodeUp, Vector2Int leftNodeDown, Vector2Int rightNodeUp, Vector2Int rightNodeDown)
        {
            if (rightNodeUp.y >= leftNodeUp.y && leftNodeDown.y >= rightNodeDown.y)
            {
                return StructureHelper.CalculateMiddlePoint(
                    leftNodeDown + new Vector2Int(0, _modifierDistanceFromWall),
                    leftNodeUp - new Vector2Int(0, _modifierDistanceFromWall + _corridorWidth)
                ).y;
            } 
        
            if (rightNodeUp.y <= leftNodeUp.y && leftNodeDown.y <= rightNodeDown.y)
            {
                return StructureHelper.CalculateMiddlePoint(
                    rightNodeDown + new Vector2Int(0, _modifierDistanceFromWall),
                    rightNodeUp - new Vector2Int(0, _modifierDistanceFromWall + _corridorWidth)
                ).y;
            }

            if (leftNodeUp.y >= rightNodeDown.y && leftNodeUp.y <= rightNodeUp.y)
            {
                return StructureHelper.CalculateMiddlePoint(
                    rightNodeDown + new Vector2Int(0, _modifierDistanceFromWall),
                    leftNodeUp - new Vector2Int(0, _modifierDistanceFromWall + _corridorWidth)
                ).y;
            }

            if (leftNodeDown.y >= rightNodeDown.y && leftNodeDown.y <= rightNodeUp.y)
            {
                return StructureHelper.CalculateMiddlePoint(
                    leftNodeDown + new Vector2Int(0, _modifierDistanceFromWall),
                    rightNodeUp - new Vector2Int(0, _modifierDistanceFromWall + _corridorWidth)
                ).y;
            }

            return -1;
        }

        private void ProcessRoomInRelationUpOrDown(Node structure1, Node structure2)
        {
            Node bottomStructure;
            var structureBottomChildren = StructureHelper.TraverseGraphToExtractLowestLeaves(structure1);
            var structureAboveChildren = StructureHelper.TraverseGraphToExtractLowestLeaves(structure2);

            var sortedBottomStructure = structureBottomChildren.OrderByDescending(child => child.TopRightAreaCorner.y).ToList();

            if (sortedBottomStructure.Count == 1)
            {
                bottomStructure = structureBottomChildren[0];
            }
            else
            {
                int maxY = sortedBottomStructure[0].TopLeftAreaCorner.y;
                sortedBottomStructure = sortedBottomStructure.Where(child => Mathf.Abs(maxY - child.TopLeftAreaCorner.y) < 10).ToList();
                int index = Random.Range(0, sortedBottomStructure.Count);
                bottomStructure = sortedBottomStructure[index];
            }

            var possibleNeighboursInTopStructure = structureAboveChildren.Where(
                child => GetValidXForNeighbourUpDown(
                    bottomStructure.TopLeftAreaCorner,
                    bottomStructure.TopRightAreaCorner,
                    child.BottomLeftAreaCorner,
                    child.BottomRightAreaCorner
                ) != -1).OrderBy(child => child.BottomRightAreaCorner.y).ToList();
        
            var topStructure = possibleNeighboursInTopStructure.Count == 0 ? structure2 : possibleNeighboursInTopStructure[0];
        
            int x = GetValidXForNeighbourUpDown(
                bottomStructure.TopLeftAreaCorner,
                bottomStructure.TopRightAreaCorner,
                topStructure.BottomLeftAreaCorner,
                topStructure.BottomRightAreaCorner);
        
            while(x==-1 && sortedBottomStructure.Count > 1)
            {
                sortedBottomStructure = sortedBottomStructure.Where(child => 
                    child.TopLeftAreaCorner.x != topStructure.TopLeftAreaCorner.x
                ).ToList();
            
                bottomStructure = sortedBottomStructure[0];
            
                x = GetValidXForNeighbourUpDown(
                    bottomStructure.TopLeftAreaCorner,
                    bottomStructure.TopRightAreaCorner,
                    topStructure.BottomLeftAreaCorner,
                    topStructure.BottomRightAreaCorner);
            }
        
            BottomLeftAreaCorner = new Vector2Int(x, bottomStructure.TopLeftAreaCorner.y);
            TopRightAreaCorner = new Vector2Int(x + _corridorWidth, topStructure.BottomLeftAreaCorner.y);
            BottomRightAreaCorner = new Vector2Int(TopRightAreaCorner.x, BottomLeftAreaCorner.y);
            TopLeftAreaCorner = new Vector2Int(BottomLeftAreaCorner.x, TopRightAreaCorner.y);
            IsHorizontalCorridor = false;
        }
    
        private int GetValidXForNeighbourUpDown(Vector2Int bottomNodeLeft, Vector2Int bottomNodeRight, 
            Vector2Int topNodeLeft, Vector2Int topNodeRight)
        {
            if(topNodeLeft.x < bottomNodeLeft.x && bottomNodeRight.x < topNodeRight.x)
            {
                return StructureHelper.CalculateMiddlePoint(
                    bottomNodeLeft + new Vector2Int(_modifierDistanceFromWall, 0),
                    bottomNodeRight - new Vector2Int(this._corridorWidth + _modifierDistanceFromWall, 0)
                ).x;
            }
        
            if(topNodeLeft.x >= bottomNodeLeft.x && bottomNodeRight.x >= topNodeRight.x)
            {
                return StructureHelper.CalculateMiddlePoint(
                    topNodeLeft+new Vector2Int(_modifierDistanceFromWall,0),
                    topNodeRight - new Vector2Int(this._corridorWidth + _modifierDistanceFromWall,0)
                ).x;
            }
        
            if(bottomNodeLeft.x >= (topNodeLeft.x) && bottomNodeLeft.x <= topNodeRight.x)
            {
                return StructureHelper.CalculateMiddlePoint(
                    bottomNodeLeft + new Vector2Int(_modifierDistanceFromWall,0),
                    topNodeRight - new Vector2Int(this._corridorWidth + _modifierDistanceFromWall,0)
                ).x;
            }
        
            if(bottomNodeRight.x <= topNodeRight.x && bottomNodeRight.x >= topNodeLeft.x)
            {
                return StructureHelper.CalculateMiddlePoint(
                    topNodeLeft + new Vector2Int(_modifierDistanceFromWall, 0),
                    bottomNodeRight - new Vector2Int(this._corridorWidth + _modifierDistanceFromWall, 0)
                ).x;
            }
        
            return -1;
        }

        private RelativePosition CheckPositionStructure2AgainstStructure1()
        {
            var middlePointStructure1Temp = ((Vector2)_structure1.TopRightAreaCorner + _structure1.BottomLeftAreaCorner) / 2;
            var middlePointStructure2Temp = ((Vector2)_structure2.TopRightAreaCorner + _structure2.BottomLeftAreaCorner) / 2;
            var angle = CalculateAngle(middlePointStructure1Temp, middlePointStructure2Temp);

            switch (angle)
            {
                case < 45 and >= 0:
                case > -45 and < 0:
                    return RelativePosition.Right;
                case > 45 and < 135:
                    return RelativePosition.Up;
                case > -135 and < -45:
                    return RelativePosition.Down;
                default:
                    return RelativePosition.Left;
            }
        }

        private float CalculateAngle(Vector2 middlePointStructure1Temp, Vector2 middlePointStructure2Temp)
        {
            return Mathf.Atan2(
                middlePointStructure2Temp.y - middlePointStructure1Temp.y,
                middlePointStructure2Temp.x - middlePointStructure1Temp.x
            ) * Mathf.Rad2Deg;
        }
    }
}