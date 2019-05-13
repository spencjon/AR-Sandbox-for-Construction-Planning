using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;
using UnityEditor;

/// <summary>
/// A struct representing a Sumo Network Lane.
/// </summary>
[Serializable]
public struct Lane
{
    public string Id { get; set; }
    public string Edge_Id { get; set; }
    public string Index { get; set; }
    public float Speed { get; set; }
    public float Length { get; set; }
    public float Width { get; set; }
    public List<string> Allow { get; set; }
    public List<string> Disallow { get; set; }
    public List<CodingConnected.TraCI.NET.Types.Position2D> Shape { get; set; }
    public bool Built { get; set; }
    public float DefaultSpeed { get; set; }
    public bool ConstructionZone { get; set; }
}

/// <summary>
/// A struct representing a Sumo Network Edge.
/// </summary>
[Serializable]
public struct Road
{
    public string Id { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string Name { get; set; }
    public List<CodingConnected.TraCI.NET.Types.Position2D> Shape { get; set; }
    public bool Built { get; set; }
    public string Type { get; set; }
    public string Function { get; set; }
    public float Occupancy { get; set; }
}


/// <summary>
/// The Edge class stores Network Road and Lane information and builds roads (Edges) for SUMO networks.
/// </summary>
public class Edge : MonoBehaviour
{
    /// <summary>
    /// Handle to Edge Parent GameObject and script.
    /// </summary>    
    private GameObject Edges_GO;
    /// <summary>
    /// The list of the Networks roads.
    /// </summary>    
    public List<Road> RoadList;
    public List<Lane> LaneList;
    public Shader Road_Shader;
    public Shader Concrete_Shader;
    /// <summary>
    /// The width to make lanes in meters.
    /// </summary>
    public float LANEWIDTH = 3.4f;

    /// <summary>
    /// Set the Edeg parent GameObject and create a new List<Road>() in Edge.RoadList.
    /// </summary>    
    void Start()
    {
        Edges_GO = GameObject.Find("Edges");
        RoadList = new List<Road>();
        LaneList = new List<Lane>();
        Edges_GO.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>    
    void Update()
    {
    }

    /// <summary>
    /// Clear all saved Network Road Data.
    /// </summary>    
    public void ClearData()
    {
        RoadList.Clear();
        LaneList.Clear();
    }

    /// <summary>
    /// Builds Road shapes from lane and road position data.
    /// Shapes are built from Unitys LineRenderer.
    /// </summary>
    /// <param name="shapelist">A List of floating point x,y position</param>
    /// <param name="id">The Sumo ID as a string.</param>
    /// <param name="type">"Road" or "Pedestrian". This will set the materials used.</param>
    /// <param name="width">The road width as a float.</param>
    /// <param name="flat">True, use flat LineRenderer. False, use shader to extrude LineRenderer.</param>
    private void BuildShapeLR(List<CodingConnected.TraCI.NET.Types.Position2D> shapelist, string id, string type, float width, bool flat)
    {
        List<Vector3> shape = new List<Vector3>();
        foreach(CodingConnected.TraCI.NET.Types.Position2D pos in shapelist)
        {
            shape.Add(new Vector3((float)pos.X, 0.0f, (float)pos.Y));
        }

        GameObject newShape = new GameObject();
        newShape.name = id;
        LineRenderer LR = newShape.AddComponent<LineRenderer>();
        if (flat)
        {
            LR.material = Resources.Load("Materials/Road_Material", typeof(Material)) as Material;  
        }
        else
        {
            LR.material = new Material(Road_Shader);
        }
        LR.useWorldSpace = true;
        LR.textureMode = LineTextureMode.Tile;
        LR.alignment = LineAlignment.View;
        LR.endWidth = LR.startWidth = width;
        LR.numCapVertices = 5;
        LR.numCornerVertices = 5;
        LR.positionCount = shapelist.Count;
        LR.SetPositions(shape.ToArray());
        LR.transform.parent = Edges_GO.transform;
    }

    /// <summary>
    /// Builds Road shapes from lane and road position data.
    /// Shapes are built as polygon meshes.
    /// </summary>
    /// <param name="shapelist">A List of floating point x,y position</param>
    /// <param name="id">The string id of the Road or Lane Shape.</param>
    /// <param name="type">"Road" or "Pedestrian". This will set the materials used.</param>
    /// <param name="width">The Road or Lane width in meters.</param>
    private void BuildShapeMesh(List<Vector3> shapelist, string id, string type, float width)
    {
        GameObject chunk = new GameObject();
        chunk.name = id;

        MeshRenderer mr = chunk.AddComponent<MeshRenderer>();
        Material m;
        if (type == "Road")
        {
            //m = new Material(Road_Shader);
            m = Resources.Load("Materials/Road_Material", typeof(Material)) as Material;
        }
        else
        {
            m = Resources.Load("Materials/Road_Material", typeof(Material)) as Material;
        }
        
        
        mr.material = m;
        Mesh mesh = new Mesh();
        int numMeshVerts = shapelist.Count * 2;
        // Build Vertices
        float offset = LANEWIDTH / 2.0f;
        int slcounter = 0;
        Vector2[] verts = new Vector2[numMeshVerts];
        for (int i = 0; i < numMeshVerts; i+=2)
        {
            verts[i] = new Vector2(shapelist[slcounter].x - offset, shapelist[slcounter].z - offset);
            verts[i + 1] = new Vector2(shapelist[slcounter].x + offset, shapelist[slcounter].z + offset);
            slcounter++;   
        }

        Triangulator tr = new Triangulator(verts.ToArray());
        int[] indices = tr.Triangulate();

        Vector3[] vertices = new Vector3[numMeshVerts];
        for (int j = 0; j < numMeshVerts; j++)
        {
            vertices[j] = new Vector3(verts[j].x, 0.0f, verts[j].y);
        }

        Vector3[] normals = new Vector3[numMeshVerts];
        for (int k = 0; k < numMeshVerts; k++)
        {
            vertices[k] = -Vector3.up;
        }

        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.normals = normals;
        mesh.RecalculateBounds();
        MeshFilter mf = chunk.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        chunk.transform.parent = Edges_GO.transform;
    }

    /// <summary>
    /// Parses the Road list and builds all valid Roads
    /// </summary>
    public void BuildEdges()
    {
        foreach(Lane lane in LaneList)
        {
            BuildShapeLR(lane.Shape, lane.Id, "lane", lane.Width, true);
        }
    }
}
