private void CombineMeshes(GameObject parent, bool isFloorOrRoof)
{
    Vector3 position = parent.transform.position;

    MeshFilter[] meshFilters = parent.GetComponentsInChildren<MeshFilter>();
    CombineInstance[] combine = new CombineInstance[meshFilters.Length];

    int i = 0;
    while (i < meshFilters.Length)
    {
        combine[i].mesh = meshFilters[i].sharedMesh;
        combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
        meshFilters[i].gameObject.SetActive(false);

        i++;
    }
    parent.transform.GetComponent<MeshFilter>().mesh = new Mesh();
    parent.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
    parent.isStatic = true;

    // Add material
    parent.GetComponent<Renderer>().material = isFloorOrRoof ? floorMaterial : wallMaterial;
    // Add collider
    parent.AddComponent<MeshCollider>();
    // Add layer
    parent.layer = isFloorOrRoof ? Layer.GroundLayer : Layer.WallLayer;

    parent.transform.position = position;
    parent.transform.gameObject.SetActive(true);
}