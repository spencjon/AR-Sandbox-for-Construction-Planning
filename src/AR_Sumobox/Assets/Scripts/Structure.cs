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
/// Poly struct holds polygon data that represents arbitrary network shapes.
/// </summary>
[Serializable]
public struct Poly
{
    public string Id { get; set; }
    public string Type { get; set; }
    public Vector3 Color { get; set; }
    public List<CodingConnected.TraCI.NET.Types.Position2D> Shape { get; set; }
}

/// <summary>
/// Structure class stores and builds all simulation network buildings and Points of Interest.
/// </summary>
public class Structure : MonoBehaviour
{
    /// <summary>
    /// The Structures main Game Object.
    /// </summary>
    private GameObject Structures_GO;
    /// <summary>
    /// The list of polygon data.
    /// </summary>
    public List<Poly> Polys;
    /// <summary>
    /// The parking lot shader.
    /// </summary>
    public Shader Concrete_Shader;
    /// <summary>
    /// The building extrusion shader.
    /// </summary>
    public Shader Building_Shader;
    /// <summary>
    /// Some extra colors for polygons.
    /// </summary>
    private Color[] BuildingColors = new Color[4];

    /// <summary>
    /// Clear all current simulation polygon data.
    /// </summary>
    public void ClearData()
    {
        Polys.Clear();
    }

    // Start is called before the first frame update
    void Start()
    {
        Structures_GO = GameObject.Find("Structures");
        Polys = new List<Poly>();
        BuildingColors[0] = new Color(153.0f / 255.0f, 102.0f / 255.0f, 51.0f / 255.0f, 1.0f);
        BuildingColors[1] = new Color(102.0f / 255.0f, 51.0f / 255.0f, 0.0f / 255.0f, 1.0f);
        BuildingColors[2] = new Color(153.0f / 255.0f, 153.0f / 255.0f, 102.0f / 255.0f, 1.0f);
        BuildingColors[3] = new Color(153.0f / 255.0f, 51.0f / 255.0f, 0.0f / 255.0f, 1.0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Build all stored polygon data.
    /// </summary>
    public void Build()
    {
        foreach (Poly p in Polys)
        {
            bool building = false;
            GameObject chunk = new GameObject();
            chunk.name = p.Id;
            MeshRenderer mr = chunk.AddComponent<MeshRenderer>();

            Material m;
            if (p.Type.Contains("building"))
            {
                building = true;
                //m = Resources.Load("Materials/Concrete_Material", typeof(Material)) as Material;
                m = new Material(Building_Shader);
                System.Random rnd = new System.Random();
                int bc = rnd.Next(1, 4) - 1;
                m.color = BuildingColors[bc];
            }
            else
            {
                m = Resources.Load("Materials/Concrete_Material", typeof(Material)) as Material;
                if (p.Color != null)
                {
                    m.color = new Color(p.Color.x / 255.0f, p.Color.y / 255.0f, p.Color.z / 255.0f, 1.0f);
                }
                
            }

            mr.material = m;

            List<Vector2> pshape = new List<Vector2>();
            foreach (CodingConnected.TraCI.NET.Types.Position2D pos in p.Shape)
            {
                pshape.Add(new Vector3((float)pos.X,(float)pos.Y));
            }

            Triangulator tr = new Triangulator(pshape.ToArray());
            int[] indices = tr.Triangulate();

            Vector3[] verts = new Vector3[pshape.Count];
            for (int j = 0; j < pshape.Count; j++)
            {
                if (building)
                {
                    verts[j] = new Vector3(pshape[j].x, 0.11f, pshape[j].y);
                }
                else
                {
                    verts[j] = new Vector3(pshape[j].x, 0.09f, pshape[j].y);
                }
                
            }

            Vector3[] norms = new Vector3[pshape.Count];
            for (int k = 0; k < pshape.Count; k++)
            {
                norms[k] = Vector3.up;
            }

            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.triangles = indices;
            mesh.normals = norms;
            mesh.RecalculateBounds();
            MeshFilter mf = chunk.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            chunk.transform.parent = Structures_GO.transform;
        }
    }
}
