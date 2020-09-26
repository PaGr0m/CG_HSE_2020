using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    public MetaBallField Field = new MetaBallField();

    private MeshFilter _filter;
    private Mesh _mesh;

    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<int> indices = new List<int>();

    private const float scale = 0.2f;
    private const int edgeLength = 50;

    private float[,,] grid = new float [edgeLength, edgeLength, edgeLength];
    private Vector3 cubeOffset = new Vector3(edgeLength * scale, edgeLength * scale, edgeLength * scale) / 2;

    private static readonly Vector3 DeltaX = new Vector3(scale / 5, 0, 0);
    private static readonly Vector3 DeltaY = new Vector3(0, scale / 5, 0);
    private static readonly Vector3 DeltaZ = new Vector3(0, 0, scale / 5);


    /// <summary>
    /// Executed by Unity upon object initialization. <see cref="https://docs.unity3d.com/Manual/ExecutionOrder.html"/>
    /// </summary>
    private void Awake()
    {
        // Getting a component, responsible for storing the mesh
        _filter = GetComponent<MeshFilter>();

        // instantiating the mesh
        _mesh = _filter.mesh = new Mesh();

        // Just a little optimization, telling unity that the mesh is going to be updated frequently
        _mesh.MarkDynamic();
    }

    /// <summary>
    /// Executed by Unity on every frame <see cref="https://docs.unity3d.com/Manual/ExecutionOrder.html"/>
    /// You can use it to animate something in runtime.
    /// </summary>
    private void Update()
    {
        vertices.Clear();
        indices.Clear();
        normals.Clear();

        Field.Update();

        // ----------------------------------------------------------------
        // Generate mesh here.
        // ----------------------------------------------------------------
        // var miniCube = new Vector3(sa, edgeLength, edgeLength)
        for (var l = 0; l < edgeLength; ++l)
        {
            for (var w = 0; w < edgeLength; ++w)
            {
                for (var h = 0; h < edgeLength; ++h)
                {
                    grid[l, w, h] = Field.F(new Vector3(l, w, h) * scale - cubeOffset);
                }
            }
        }

        for (var l = 0; l < edgeLength - 1; ++l)
        {
            for (var w = 0; w < edgeLength - 1; ++w)
            {
                for (var h = 0; h < edgeLength - 1; ++h)
                {
                    // Create mini cube.
                    var offset = new Vector3(l, w, h) * scale - cubeOffset;

                    // Compute function at vertices. Compute mask -- cube index.
                    var cubeIndex = GetCubeIndex(l, w, h);

                    // Triangle count.
                    var triangleCount = MarchingCubes.Tables.CaseToTrianglesCount[cubeIndex];

                    // Fill lists.
                    for (var triangleIdx = 0; triangleIdx < triangleCount; triangleIdx++)
                    {
                        var triangleEdges = MarchingCubes.Tables.CaseToVertices[cubeIndex][triangleIdx];

                        indices.Add(vertices.Count);
                        vertices.Add(GetPointByInterpolation(triangleEdges.x, offset));
                        normals.Add(GetNormal(vertices.Last()));

                        indices.Add(vertices.Count);
                        vertices.Add(GetPointByInterpolation(triangleEdges.y, offset));
                        normals.Add(GetNormal(vertices.Last()));

                        indices.Add(vertices.Count);
                        vertices.Add(GetPointByInterpolation(triangleEdges.z, offset));
                        normals.Add(GetNormal(vertices.Last()));
                    }
                }
            }
        }

        // Here unity automatically assumes that vertices are points and hence (x, y, z)
        // will be represented as (x, y, z, 1) in homogenous coordinates
        _mesh.Clear();
        _mesh.SetVertices(vertices);
        _mesh.SetTriangles(indices, 0);
        _mesh.SetNormals(normals);

        // Upload mesh data to the GPU
        _mesh.UploadMeshData(false);
    }

    private Vector3 GetPointByInterpolation(int edge, Vector3 offset)
    {
        var vertex1 = MarchingCubes.Tables._cubeEdges[edge][0];
        var vertex2 = MarchingCubes.Tables._cubeEdges[edge][1];

        var vertexPoint1 = MarchingCubes.Tables._cubeVertices[vertex1] * scale + offset;
        var vertexPoint2 = MarchingCubes.Tables._cubeVertices[vertex2] * scale + offset;

        var f1 = Field.F(vertexPoint1);
        var f2 = Field.F(vertexPoint2);

        return Vector3.Lerp(vertexPoint1, vertexPoint2, -f1 / (f2 - f1));
    }

    private Vector3 GetNormal(Vector3 point)
    {
        return Vector3.Normalize(new Vector3(
            Field.F(point) - Field.F(point - DeltaX),
            Field.F(point) - Field.F(point - DeltaY),
            Field.F(point) - Field.F(point - DeltaZ)
        ));
    }

    private int GetCubeIndex(int l, int w, int h)
    {
        return (grid[l, w, h] > 0 ? 0b1 : 0) |
               (grid[l, w + 1, h] > 0 ? 0b10 : 0) |
               (grid[l + 1, w + 1, h] > 0 ? 0b100 : 0) |
               (grid[l + 1, w, h] > 0 ? 0b1000 : 0) |
               (grid[l, w, h + 1] > 0 ? 0b10000 : 0) |
               (grid[l, w + 1, h + 1] > 0 ? 0b100000 : 0) |
               (grid[l + 1, w + 1, h + 1] > 0 ? 0b1000000 : 0) |
               (grid[l + 1, w, h + 1] > 0 ? 0b10000000 : 0);
    }
}