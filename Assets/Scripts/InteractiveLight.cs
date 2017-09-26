using UnityEngine;
using System.Collections;

public class InteractiveLight : MonoBehaviour, InteractiveObject
{
    public bool StartOn = true;

    public GameObject Light;
    public GameObject LightMesh;

    public Material OnMaterial;
    public Material OffMaterial;

	void Start () 
    {
        SetOnState(StartOn);
	}
	
	void Update () 
    {
	
	}

    public void OnSwitchChange(bool switchOn)
    {
        SetOnState(switchOn);
    }

    public bool IsSwitchedOn()
    {
        return m_On;
    }

    void SetOnState(bool on)
    {
        m_On = on;

        if (m_On)
        {
            Light.SetActive(true);
            LightMesh.GetComponent<Renderer>().material = OnMaterial;
        }
        else
        {
            Light.SetActive(false);
            LightMesh.GetComponent<Renderer>().material = OffMaterial;
        }
    }

    bool m_On;
}
