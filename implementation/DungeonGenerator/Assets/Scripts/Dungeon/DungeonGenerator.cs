using System.Collections.Generic;
using System.Linq;

namespace Dungeon
{
    public class DungeonGenerator
    {
        private List<RoomNode> _allNodes = new List<RoomNode>();
        private int _dungeonLength;
        private int _dungeonWidth;

        public DungeonGenerator(int dungeonLength, int dungeonWidth)
        {
            _dungeonLength = dungeonLength;
            _dungeonWidth = dungeonWidth;
        }

        public List<Node> CalculateDungeon(int maxIterations, int roomWidthMin, int roomLengthMin, int roomWidthMax, 
            int roomLengthMax, float roomBottomCornerModifier, float roomTopCornerModifier, int roomOffset, 
            int corridorWidth)
        {
            BinarySpacePartitioner bsp = new BinarySpacePartitioner(_dungeonWidth, _dungeonLength);
            _allNodes = bsp.PrepareNodesCollection(maxIterations, roomWidthMin, roomLengthMin, roomWidthMax, roomLengthMax);

            List<Node> roomSpaces = StructureHelper.TraverseGraphToExtractLowestLeaves(bsp.RootNode);
            RoomGenerator roomGenerator = new RoomGenerator();
            List<RoomNode> roomList = roomGenerator.GenerateRoomsInGivenSpace(
                roomSpaces, 
                roomBottomCornerModifier, 
                roomTopCornerModifier, 
                roomOffset
            );

            CorridorsGenerator corridorsGenerator = new CorridorsGenerator();
            var corridorList = corridorsGenerator.CreateCorridors(_allNodes, corridorWidth);
        
            return new List<Node>(roomList).Concat(corridorList).ToList();
        }
    }
}


