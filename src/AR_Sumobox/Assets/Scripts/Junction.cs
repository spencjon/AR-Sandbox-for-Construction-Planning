using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using CodingConnected.TraCI.NET;

[Serializable]
public struct Intersection
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public string IncomingLanes { get; set; }
    public string InternalLanes { get; set; }
    public List<CodingConnected.TraCI.NET.Types.Position2D> Shape { get; set; }
}

/// <summary>
/// Junction class represents road network intersection. 
/// </summary>
public class Junction : MonoBehaviour
{
    private GameObject Junctions_GO;
    public Shader Road_Shader;
    public List<Intersection> Junction_List;
    public bool Built;

    // Start is called before the first frame update
    void Start()
    {
        Junction_List = new List<Intersection>();
        Junctions_GO = GameObject.Find("Junctions");
        Built = false;
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Clear all current simulation data.
    /// </summary>
    public void ClearData()
    {
        Junction_List.Clear();
    }

    /// <summary>
    /// Build an Intersection.
    /// </summary>
    public void BuildJunction(Intersection inter)
    {
        // String points to floats
        if (inter.Shape == null)
        {
            return;
        }

        int numverts = inter.Shape.Count();
        if (numverts > 5)
        {
            // Get Meshfilter and create a new mesh
            GameObject chunk = new GameObject();
            chunk.name = inter.Id;

            chunk.AddComponent<MeshRenderer>();
            Material m = new Material(Road_Shader);
            chunk.GetComponent<MeshRenderer>().material = m;
            Mesh mesh = new Mesh();

            // Build Vertices
            Vector3[] verts = new Vector3[numverts + 1];
            for (int i = 0; i < inter.Shape.Count(); i++)
            {
                CodingConnected.TraCI.NET.Types.Position2D pos = inter.Shape.ElementAt(i);
                verts[i] = new Vector3((float)pos.X, 0.01f, (float)pos.Y);
            }

            // Center of junction
            verts[verts.Length - 1] = new Vector3(inter.X, 0.01f, inter.Y);
            mesh.vertices = verts;

            // Build Triangles
            int[] tris = new int[(verts.Length - 1) * 3];
            int triscounter = 0;
            int trisindex = 0;
            for (int j = 0; j < verts.Length - 1; j++)
            {
                tris[trisindex] = triscounter;
                tris[trisindex + 1] = triscounter + 1;
                tris[trisindex + 2] = verts.Length - 1;
                triscounter++;
                trisindex += 3;
            }
            tris[tris.Length - 1] = 0;
            tris[tris.Length - 2] = verts.Length - 2;
            tris[tris.Length - 3] = verts.Length - 1;
            mesh.triangles = tris;

            // Build Normals
            Vector3[] norms = new Vector3[numverts + 1];
            for (int k = 0; k < numverts + 1; k++)
            {
                norms[k] = Vector3.up;
            }
            mesh.normals = norms;

            chunk.AddComponent<MeshFilter>().mesh = mesh;
            //chunk.isStatic = true;
            chunk.transform.parent = Junctions_GO.transform;
        }
    }

    public void Build()
    {
        foreach (Intersection i in Junction_List)
        {
            BuildJunction(i);
        }

        Built = true;
    }
}

