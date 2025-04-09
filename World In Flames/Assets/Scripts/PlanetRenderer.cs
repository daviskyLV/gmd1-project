using System.Collections;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

public class PlanetRenderer : MonoBehaviour
{
    [SerializeField]
    [Range(2, 255)]
    private int resolution = 2;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var directions = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back};
        for (int i = 0; i < 6; i++)
        {
            StartCoroutine(RenderFaceDirection(directions[i]));
        }
    }

    private IEnumerator RenderFaceDirection(Vector3 localUp)
    {
        var meshObj = new GameObject($"Side facing {localUp}");
        meshObj.transform.parent = transform;

        meshObj.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
        var meshF = meshObj.AddComponent<MeshFilter>();
        meshF.mesh = new Mesh();

        var face = new PlanetFace(meshF.mesh, resolution, localUp);
        face.ConstructMesh();

        yield return null;
    }
}
