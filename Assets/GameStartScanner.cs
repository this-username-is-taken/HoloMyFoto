using System;
using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;
using UnityEngine;

public class GameStartScanner : MonoBehaviour
{

    public float MinAreaForCompletion = 200.0f;
    public TextMesh ScanningText;

    private bool scanComplete = false;
    private SpatialUnderstanding SU;

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

                    // The stats tell us if we could potentially finish
                    if (ReadyForCompletion)
                    {
                        return "When ready, air tap to start placing photos";
                    }
                    return "Look around while we detect walls";
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

            if (stats.VirtualWallSurfaceArea > MinAreaForCompletion)
            {
                return true;
            }
            return false;
        }
    }
}
