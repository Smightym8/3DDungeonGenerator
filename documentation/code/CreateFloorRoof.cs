// Place triangles inverted if it is not the floor
int[] triangles = new int[6];
if (isFloor)
{
    triangles[0] = 0;
    triangles[1] = 1;
    triangles[2] = 2;
    triangles[3] = 2;
    triangles[4] = 1;
    triangles[5] = 3;
}
else
{
    triangles[0] = 2;
    triangles[1] = 1;
    triangles[2] = 0;
    triangles[3] = 3;
    triangles[4] = 1;
    triangles[5] = 2;
}