﻿using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Projection Data class stores and creates a simulation networks terrain.
/// </summary>
public class ProjectionData : MonoBehaviour
{
    /// <summary>
    /// The Projection Data Game Object
    /// </summary>
    private GameObject Projection_Data_GO;
    /// <summary>
    /// The Projection Data Terrain Shader.
    /// </summary>
    public Shader Terrain_Shader;
    /// <summary>
    /// A handle to the main camera.
    /// </summary>
    public Camera Main_Camera;
    /// <summary>
    /// The offset for network projections.
    /// </summary>
    public string offset;
    /// <summary>
    /// The networks original bounds Lat/Lon
    /// </summary>
    public CodingConnected.TraCI.NET.Types.Polygon originalBounds;
    /// <summary>
    /// The networks projected bound. Cartesian
    /// </summary>
    public CodingConnected.TraCI.NET.Types.Polygon projectedBounds;

    /// <summary>
    /// Sumo shape sting to List of floats point order is x1, y1, x2, y2, ....
    /// </summary>
    /// <param name="shape">A Sumo formatted shape string.</param>
    /// <returns></returns>
    public List<float> ShapeStringToFloatList(string shape)
    {
        List<float> points = new List<float>();
        char[] find = new char[2];
        find[0] = ',';
        find[1] = ' ';
        string[] cuts = shape.Split(find);
        List<string> cutList = cuts.ToList();
        foreach (string cut in cutList)
        {
            points.Add(float.Parse(cut, CultureInfo.InvariantCulture.NumberFormat));
        }
        return points;
    }

    /// <summary>
    /// Adds a Terrain_Plane to the scene the size of the network and
    /// sets the camera to the center of the plane.
    /// </summary>
    public void BuildTerrain()
    {
        float x = (float)originalBounds.Points[1].X - (float)originalBounds.Points[0].X;
        float y = (float)originalBounds.Points[1].Y - (float)originalBounds.Points[0].Y;
        //float z = 1.0f;
        GameObject chunk = new GameObject()
        {
            name = "Terrain_Plane"
        };
        MeshRenderer mr = chunk.AddComponent<MeshRenderer>();
        Material m = new Material(Terrain_Shader);
        mr.material = m;

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[4] {
            new Vector3(0.0f,0.0f,y),
            new Vector3(x,0.0f,y),
            new Vector3(x,0.0f,0.0f),
            new Vector3(0.0f,0.0f,0.0f)
        };

        mesh.triangles = new int[6] { 0, 1, 3, 1, 2, 3 };

        mesh.uv = new Vector2[4] {
            new Vector2(0.0f, 0.0f),
            new Vector2(0.0f, y),
            new Vector2(x, y),
            new Vector2(x, 0.0f)
        };

        mesh.normals = new Vector3[4]{
            -Vector3.up,
            -Vector3.up,
            -Vector3.up,
            -Vector3.up
        };

        chunk.AddComponent<MeshFilter>().mesh = mesh;
        chunk.isStatic = true;
        chunk.transform.parent = Projection_Data_GO.transform;
        float xcenter = ((float)originalBounds.Points[1].X + (float)originalBounds.Points[0].X) / 2.0f;
        float ycenter = ((float)originalBounds.Points[1].Y - (float)originalBounds.Points[0].Y) / 2.0f;

        // Adjust Camera
        Main_Camera.transform.position = new Vector3(xcenter, 50.0f, ycenter);
        Main_Camera.nearClipPlane = 0.01f;
        Main_Camera.farClipPlane = 1000000.0f;
        Main_Camera.usePhysicalProperties = true;
        Main_Camera.focalLength = 50.0f;
    }

    // Start is called before the first frame update
    void Start()
    {
        Projection_Data_GO = GameObject.Find("Projection_Data");
        Projection_Data_GO.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
