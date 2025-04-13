using System;
using System.Collections;
using UnityEngine;

public class PlanetRenderer : MonoBehaviour
{
    [SerializeField]
    [Range(2, 255)]
    private int resolution = 2;
    [SerializeField]
    [Range(0f, 1f)]
    private float seaLevel = 0.25f;
    [SerializeField]
    private Material testMaterial;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var directions = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back};
        var testArr = new float[resolution * resolution];
        for (int i = 0; i < testArr.Length; i++)
        {
            var x = i % resolution;
            var y = i / resolution;
            testArr[i] = ((x % 7) + (y % 7)) / 14f;
        }
        for (int i = 0; i < 6; i++)
        {
            StartCoroutine(RenderFaceDirection(directions[i], testArr));
        }
    }

    private IEnumerator RenderFaceDirection(Vector3 localUp, float[] heightmap)
    {
        var meshObj = new GameObject($"Side facing {localUp}");
        meshObj.transform.parent = transform;

        meshObj.AddComponent<MeshRenderer>().material = testMaterial;
        var meshF = meshObj.AddComponent<MeshFilter>();
        meshF.mesh = new Mesh();

        var face = new PlanetFace(meshF.mesh, resolution, localUp, heightmap, seaLevel);
        face.ConstructMesh();

        yield return null;
    }
}
