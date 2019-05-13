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
using System.Net;

/// <summary>
/// SumoCreator class is used for creating Open Street Map networks with SUMO's
/// OSM Web Wizard and reading SUMO generated files that describe a networks logic 
/// and layout.
/// </summary>
public class SumoCreator : MonoBehaviour
{
    /// <summary>
    /// ProjectionData parent GameObject and script.
    /// </summary>
    private GameObject Projection_Data_GO;
    /// <summary>
    /// Junctions parent GameObject and script.
    /// </summary>
    private GameObject Junctions_GO;
    /// <summary>
    /// Edges parent GameObject and script.
    /// </summary>
    private GameObject Edges_GO;
    /// <summary>
    /// Structures parent GameObject and script.
    /// </summary>
    private GameObject Structures_GO;
    /// <summary>
    /// TraciController Game object (Script)
    /// </summary>
    private GameObject Traci_GO;
    /// <summary>
    /// A handle to the main scene camera.
    /// </summary>
    private GameObject Main_Camera;

    /// <summary>
    /// The name of the current simulation configuration file.
    /// </summary>
    private string CFG_FILE = null;
    /// <summary>
    /// Find all parent GameObjects at start.
    /// </summary>
    private void Start()
    {
        Projection_Data_GO = GameObject.Find("Projection_Data");
        Junctions_GO = GameObject.Find("Junctions");
        Edges_GO = GameObject.Find("Edges");
        Structures_GO = GameObject.Find("Structures");
        Traci_GO = GameObject.Find("Traci_Controller");
        Main_Camera = GameObject.Find("Main_Camera");
    }

    /// <summary>
    /// Builds the network pieces by parsing a Sumo network file.
    /// Reads through a network and saves all the network data to the handling class.
    /// There are classes for ProjectionData, Edge, Junction and Structure.
    /// After all data is read in each class builds its own shapes.
    /// </summary>
    /// <param name="file">The name of the SUMO .net file to parse given as a string.</param>
    private void BuildNetwork(string file)
    {
        Main_Camera.GetComponentInChildren<Canvas>().gameObject.SetActive(false);

        TraCIClient theclient = Traci_GO.GetComponent<TraciController>().Client;
        if (theclient != null)
        {
            // Get the network size information from the 'location' 
            // node and build the terrain.
            TraCIResponse<CodingConnected.TraCI.NET.Types.Polygon> bounds_xy = theclient.Simulation.GetNetBoundary(file);
            Projection_Data_GO.GetComponent<ProjectionData>().originalBounds = bounds_xy.Content;
            Projection_Data_GO.GetComponent<ProjectionData>().BuildTerrain();

            // Get all the Junction/Intersection information from the 'junction' 
            // nodes and build the Junctions.
            TraCIResponse<List<String>> junction_ids = theclient.Junction.GetIdList();
            foreach (String j_id in junction_ids.Content)
            {
                theclient.Junction.GetShape(j_id);
                Intersection theJunction = new Intersection();
                theJunction.Id = j_id;
                theJunction.Name = j_id;
                theJunction.Type = j_id;
                theJunction.X = (float)theclient.Junction.GetPosition(j_id).Content.X;
                theJunction.Y = (float)theclient.Junction.GetPosition(j_id).Content.Y;
                theJunction.Shape = theclient.Junction.GetShape(j_id).Content.Points;
                Junctions_GO.GetComponent<Junction>().Junction_List.Add(theJunction);
            }
            Junctions_GO.GetComponent<Junction>().Build();

            // Get all the Edge/Road information from the 'edge' nodes.
            // Then build the Roads.
            TraCIResponse<List<String>> edge_ids = theclient.Edge.GetIdList();
            foreach (string e_id in edge_ids.Content)
            {
                // Create the Road and add the Edges Attributes.
                // An Id will always be present but need to check the rest.
                Road newEdge = new Road();
                newEdge.Built = false;
                newEdge.Id = e_id;
                newEdge.Name = theclient.Edge.GetStreetName(e_id).Content;
                newEdge.From = null;
                newEdge.To = null;
                newEdge.Type = null;
                newEdge.Shape = null;
                newEdge.Function = null;
                Edges_GO.GetComponent<Edge>().RoadList.Add(newEdge);
            }

            // Get all the Lanes that belong to the current Edge.
            TraCIResponse<List<String>> lane_ids = theclient.Lane.GetIdList(); 
            foreach (string l_id in lane_ids.Content)
            {
                // Create a new Lane and add the Lanes Attributes.
                // Then save the Lane in the Road.Lanes list.
                Lane newLane = new Lane();
                newLane.Built = false;
                newLane.Id = l_id;
                newLane.Edge_Id = theclient.Lane.GetEdgeId(l_id).Content;
                newLane.Width = (float)theclient.Lane.GetWidth(l_id).Content;
                newLane.Index = "None";
                newLane.Speed = (float)theclient.Lane.GetMaxSpeed(l_id).Content;
                newLane.DefaultSpeed = newLane.Speed;
                newLane.Length = (float)theclient.Lane.GetLength(l_id).Content;
                newLane.Allow = theclient.Lane.GetAllowed(l_id).Content;
                newLane.Disallow = theclient.Lane.GetDisallowed(l_id).Content;
                newLane.Shape = theclient.Lane.GetShape(l_id).Content.Points;
                Edges_GO.GetComponent<Edge>().LaneList.Add(newLane);
            }
            // Let the Edge script build all the Networks Roads/Edges.
            // This can be a very time consuming function given a large network.
            Edges_GO.GetComponent<Edge>().BuildEdges();
            

        }       
    }

    /// <summary>
    /// Parse the XML file and procedurally create all buildings and landmarks.
    /// </summary>
    /// <param name="file">The name of the SUMO .net file to parse given as a string.</param>
    private void BuildStructures(string file)
    {
        TraCIClient theclient = Traci_GO.GetComponent<TraciController>().Client;
        if (theclient != null)
        {
            List<string> id_list = theclient.POI.GetIdList().Content;

            foreach(string p_id in id_list)
            {
                Poly newpoly = new Poly();
                newpoly.Id = p_id;
                newpoly.Color = new Vector3(theclient.POI.GetColor(p_id).Content.R, theclient.POI.GetColor(p_id).Content.G, theclient.POI.GetColor(p_id).Content.B);
                newpoly.Type = theclient.POI.GetType(p_id).Content;
                newpoly.Shape = theclient.Polygon.GetShape(p_id).Content.Points;
                Structures_GO.GetComponent<Structure>().Polys.Add(newpoly);
            }
        }
        Structures_GO.GetComponent<Structure>().Build();
    }

    /// <summary>
    /// Open the OSMWebWizard to build a real world road network.
    /// The user will save the new network to a zipfile when done.
    /// The processes remain open so the user can build multiple network at once.
    /// </summary>
    public void GenerateOsmNetwork()
    {
        Main_Camera.GetComponentInChildren<Canvas>().gameObject.SetActive(false);
        try
        {
            Process p = new Process();
            ProcessStartInfo si = new ProcessStartInfo()
            {
                WorkingDirectory = "C:\\Sumo\\tools\\",
                FileName = "osmWebWizard.py"
            };
            p.StartInfo = si;
            p.Start();
           
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(e.Message);
        }
    }

    /// <summary>
    /// Go through all network description files and build the network into Unity.
    /// Most files will be passed over but there are some handles left for upgrades.
    /// </summary>
    public void LoadNetwork()
    {
        string[] files = null;
        // Lets a user pick a generated network with a file selection prompt.
        try
        {
            string path = EditorUtility.OpenFolderPanel("Select a SUMO Network Folder.", "", "");
            files = Directory.GetFiles(path);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogException(e);
        }

        if (files != null)
        {
            foreach (string file in files)
            {
                // The trips files.
                if (file.EndsWith(".trips.xml"))
                {
                    continue;
                }
                // The routes files.
                else if (file.EndsWith(".rou.xml"))
                {
                    continue;
                }
                // The network file.
                else if (file.EndsWith(".net.xml"))
                {
                    BuildNetwork(file);
                }
                // The polygon file.
                else if (file.EndsWith(".poly.xml"))
                {
                    BuildStructures(file);
                }
                // The view file.
                else if (file.EndsWith(".view.xml"))
                {
                    continue;
                }
                // The config file.
                else if (file.EndsWith(".sumocfg"))
                {
                    CFG_FILE = file;
                    continue;
                }
                else
                {
                    // Ignore all batch files but we should log unknowns.
                    if (!file.EndsWith(".bat"))
                    {
                        UnityEngine.Debug.Log("Unknown File Extention " + file.ToString());
                    }
                }
            }
            UnityEngine.Debug.Assert(CFG_FILE != null, "No .sumocfg file created, something may have gone wrong with the osmWebWizard.py. Try using Python 2.X with unicode support");
            if (CFG_FILE != null)
            {
                StartSumo(CFG_FILE);
            }
        }
    }

    /// <summary>
    /// Starts Traci and Sumo to run traffic simulations
    /// </summary>
    /// <param name="ConfigFile"></param>
    private void StartSumo(string ConfigFile)
    {
        try
        {
            Traci_GO.GetComponent<TraciController>().Port = 80;
            Traci_GO.GetComponent<TraciController>().HostName = Dns.GetHostEntry("localhost").AddressList[1].ToString();
            Traci_GO.GetComponent<TraciController>().ConfigFile = ConfigFile;
            Traci_GO.GetComponent<TraciController>().Invoke("ConnectToSumo", 0);             
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(e.Message);
        }
    }
}
