using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

// Similar to terrain face class from https://www.youtube.com/watch?v=QN39W020LqU
public class PlanetFace
{
    private Mesh mesh;
    private readonly int resolution;
    private readonly Vector3 localUp;

    private Vector3 axisA;
    private Vector3 axisB;

    public PlanetFace(Mesh mesh, int resolution, Vector3 localUp)
    {
        this.mesh = mesh;
        this.resolution = resolution;
        this.localUp = localUp;

        axisA = new(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }

    public void ConstructMesh()
    {
        var resSq = resolution * resolution;
        var faceJob = new PlanetFaceMeshJob {
            Width = resolution,
            LocalUp = localUp,
            AxisA = axisA,
            AxisB = axisB,
            Vertices = new(resSq, Allocator.TempJob),
            Quads = new(resSq, Allocator.TempJob)
        };
        var handle = faceJob.Schedule(resSq, 64);
        handle.Complete();

        // Parsing data from job
        var vertices = new Vector3[resSq];
        var triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        var triIndex = 0;
        for (int i = 0; i < resSq; i++)
        {
            var v = faceJob.Vertices[i];
            var q = faceJob.Quads[i];
            vertices[i] = new(v.x, v.y, v.z);

            if (!q.Valid)
                continue;
            // Extracting the quad triangle points
            triangles[triIndex] = q.TriOne.x;
            triangles[triIndex+1] = q.TriOne.y;
            triangles[triIndex+2] = q.TriOne.z;

            triangles[triIndex + 3] = q.TriTwo.x;
            triangles[triIndex + 4] = q.TriTwo.y;
            triangles[triIndex + 5] = q.TriTwo.z;
            triIndex += 6;
        }

        // Updating the mesh
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

    }
}
