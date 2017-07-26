using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;

public class LayoutManager : MonoBehaviour, IInputClickHandler
{

	// Use this for initialization
	void Start () {

    }

    public void OnInputClicked(InputClickedEventData eventData)
    {
        enabled = false;
    }

    // Update is called once per frame
    void Update () {
		
	}
}
