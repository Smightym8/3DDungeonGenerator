using UnityEngine;

namespace Dungeon
{
    public class Line
    {
        private Orientation _orientation;
        private Vector2Int _coordinates;

        public Orientation Orientation
        {
            get => _orientation;
            set => _orientation = value;
        }
    
        public Vector2Int Coordinates 
        {
            get => _coordinates;
            set => _coordinates = value;
        }
    
        public Line(Orientation orientation, Vector2Int coordinates)
        {
            _orientation = orientation;
            _coordinates = coordinates;
        }
    }

    public enum Orientation
    {
        Horizontal = 0,
        Vertical = 1,
    }
}