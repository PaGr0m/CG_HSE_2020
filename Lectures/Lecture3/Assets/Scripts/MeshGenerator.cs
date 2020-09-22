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

    private const float scale = 0.5f;
    private const int edgeLength = 10;

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
        var miniCube = new Vector3(scale, scale, scale);
        for (float l = -edgeLength; l < edgeLength; l += miniCube.x)
        {
            for (float w = -edgeLength; w < edgeLength; w += miniCube.y)
            {
                for (float h = -edgeLength; h < edgeLength; h += miniCube.z)
                {
                    // Create mini cube
                    var offset = new Vector3(l, w, h);


                    // Compute function at vertices
                    var cubeVertices = MarchingCubes.Tables
                        ._cubeVertices
                        .Select(vertex => vertex * scale + offset)
                        .Select(vertex => Field.F(vertex))
                        .ToList();

                    // Compute mask -- triangle index
                    var cubeIndex = GetTriangleIndex(cubeVertices);

                    // Triangle count
                    var triangleCount = MarchingCubes.Tables.CaseToTrianglesCount[cubeIndex];

                    // For by 
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
        // _mesh.RecalculateNormals(); 
        // Use _mesh.SetNormals(normals) instead when you calculate them

        // Upload mesh data to the GPU
        _mesh.UploadMeshData(false);
    }

    private Vector3 GetPointByInterpolation(int edge, Vector3 offset)
    {
        var vertex1 = MarchingCubes.Tables._cubeEdges[edge][0];
        var vertex2 = MarchingCubes.Tables._cubeEdges[edge][1];

        var vertexPoint1 = MarchingCubes.Tables._cubeVertices[vertex1] * scale + offset;
        var vertexPoint2 = MarchingCubes.Tables._cubeVertices[vertex2] * scale + offset;

        var paramT = -Field.F(vertexPoint1) / (Field.F(vertexPoint2) - Field.F(vertexPoint1));

        return Vector3.Lerp(vertexPoint1, vertexPoint2, paramT);
    }

    private Vector3 GetNormal(Vector3 point)
    {
        var dx = new Vector3(scale / 5, 0, 0);
        var dy = new Vector3(0, scale / 5, 0);
        var dz = new Vector3(0, 0, scale / 5);

        return Vector3.Normalize(new Vector3(
            Field.F(point + dx) - Field.F(point - dx),
            Field.F(point + dy) - Field.F(point - dy),
            Field.F(point + dz) - Field.F(point - dz)
        ));
    }

    private int GetTriangleIndex(IReadOnlyList<float> cubeVertices)
    {
        var triangleIndex = 0;
        for (var i = 0; i < 8; i++)
        {
            if (cubeVertices[i] > 0)
            {
                triangleIndex |= 1 << i;
            }
        }

        return triangleIndex;
    }
}