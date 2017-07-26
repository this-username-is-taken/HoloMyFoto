using System;
using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;
using UnityEngine;

public class GameStartScanner : MonoBehaviour
{
    public float MinAreaForCompletion = 5.0f;
    public TextMesh ScanningText;

    private bool scanComplete = false;
    private SpatialUnderstanding SU;

    const int QueryResultMaxCount = 512;
    const int DisplayResultMaxCount = 32;

    public float MinHeightOfWallSpace = 0.5f;
    public float MinWidthOfWallSpace = 0.5f;
    public float MinHeightAboveFloor = 1.0f;

    private SpatialUnderstandingDllTopology.TopologyResult[] _resultsTopology = new SpatialUnderstandingDllTopology.TopologyResult[QueryResultMaxCount];

    public GameObject hangr;

    void Start()
    {
        SU = SpatialUnderstanding.Instance;
        SU.ScanStateChanged += Instance_ScanStateChanged;
        SU.RequestBeginScanning();
    }

    private void Instance_ScanStateChanged()
    {
        if (SU.AllowSpatialUnderstanding && SU.ScanState == SpatialUnderstanding.ScanStates.Done)
        {
            scanComplete = true;


            var resultsTopologyPtr = SpatialUnderstanding.Instance.UnderstandingDLL.PinObject(_resultsTopology);
            //var locationCount = SpatialUnderstandingDllTopology.QueryTopology_FindLargePositionsOnWalls(
            //    MinHeightOfWallSpace, MinWidthOfWallSpace, MinHeightAboveFloor, 0.0f, _resultsTopology.Length, resultsTopologyPtr);
            
            var locationCount = SpatialUnderstandingDllTopology.QueryTopology_FindLargestWall(resultsTopologyPtr);
            for (int i = 0; i < locationCount; i++)
            {
                var topology = _resultsTopology[i];
                Debug.Log(topology.position + " | " + topology.normal + " | " + topology.width + ", " + topology.length);

                /*
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = topology.position;
                cube.transform.forward = topology.normal;
                cube.transform.localScale = new Vector3(1.0f, 0.5f, 0.05f);
                */

                hangr.transform.position = topology.position;
                hangr.transform.forward = topology.normal;
            }

        }
    }

    void Update()
    {
        if (ScanningText != null)
            ScanningText.text = PrimaryText;

        if (!scanComplete && ReadyForCompletion)
        {
            scanComplete = true;
            SU.RequestFinishScan();
            SU.GetComponent<SpatialUnderstandingCustomMesh>().DrawProcessedMesh = false;
        }

    }

    public string PrimaryText
    {
        get
        {
            if (!SU.AllowSpatialUnderstanding)
                return "";

            switch (SU.ScanState)
            {
                case SpatialUnderstanding.ScanStates.Scanning:
                    // Get the scan stats
                    IntPtr statsPtr = SU.UnderstandingDLL.GetStaticPlayspaceStatsPtr();
                    if (SpatialUnderstandingDll.Imports.QueryPlayspaceStats(statsPtr) == 0)
                    {
                        return "Failed to scan find a wall!";
                    }
                    var stats = SU.UnderstandingDLL.GetStaticPlayspaceStats();

                    // The stats tell us if we could potentially finish
                    if (ReadyForCompletion)
                    {
                        return "When ready, air tap to start placing photos";
                    }
                    return "Mapped area: " + stats.TotalSurfaceArea +
                        " Walls found: " + (stats.NumWall_XNeg + stats.NumWall_XPos + stats.NumWall_ZNeg + stats.NumWall_ZPos) +
                        " Wall area: " + stats.WallSurfaceArea;
                case SpatialUnderstanding.ScanStates.Finishing:
                    return "Finalizing scan (please wait)";
                case SpatialUnderstanding.ScanStates.Done:
                    return "Scan completed";
                default:
                    return "I'm working, ScanState = " + SU.ScanState.ToString();
            }
        }
    }


    public bool ReadyForCompletion
    {
        get
        {
            // Only allow this when we are actually scanning
            if (!SU.AllowSpatialUnderstanding || SU.ScanState != SpatialUnderstanding.ScanStates.Scanning)
            {
                return false;
            }

            // Query the current playspace stats
            var statsPtr = SU.UnderstandingDLL.GetStaticPlayspaceStatsPtr();
            if (SpatialUnderstandingDll.Imports.QueryPlayspaceStats(statsPtr) == 0)
            {
                return false;
            }
            var stats = SU.UnderstandingDLL.GetStaticPlayspaceStats();

            if (stats.WallSurfaceArea > MinAreaForCompletion)
            {
                return true;
            }
            return false;
        }
    }
}
