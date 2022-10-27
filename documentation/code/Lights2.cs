private void SaveLightPosition(Vector3 position, int rotation, bool isGettingLight)
{
    Vector3Int point = Vector3Int.CeilToInt(position);

    if (_possibleLightPositions.Contains(point))
    {
        _possibleLightPositions.Remove(point);
        _ignoreLightPositions.Add(point);

        if (_lightPositions.ContainsKey(point))
        {
            _lightPositions.Remove(point);
        }
    }
    else if(!_ignoreLightPositions.Contains(point))
    {
        _possibleLightPositions.Add(point);
        if (isGettingLight)
        {
            _lightPositions.Add(point, rotation);
        }
    }
}