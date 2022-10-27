var bottomLeftCorner = new Vector3(
    room.BottomLeftAreaCorner.x,
    0,
    room.BottomLeftAreaCorner.y
);

var horizontalBottomRotation = 0;

var horizontalBottomLength = bottomRightCorner.x - bottomLeftCorner.x;

// Horizontal bottom
bool isGettingLight = false;
int lightFrequencyTemp = lightFrequency;
int distancePerLight = (int)Math.Ceiling(horizontalBottomLength / lightFrequencyTemp);
int steps = 1;

for (var x = (int)bottomLeftCorner.x; x <= (int)bottomRightCorner.x; x++)
{
    var position = new Vector3(x, 0, bottomLeftCorner.z);

    if (x == (int)bottomLeftCorner.x + (distancePerLight * steps))
    {
        steps++;
        isGettingLight = true;
    }

    SaveLightPosition(position, horizontalBottomRotation, isGettingLight);
    isGettingLight = false;
}
...