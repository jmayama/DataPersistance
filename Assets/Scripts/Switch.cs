using UnityEngine;
using System.Collections;

public class Switch : MonoBehaviour 
{
    public bool StartOn;
    public Color OnColor;
    public Color OffColor;
    public GameObject ObjectToSwitch;

    void Start () 
    {
        m_SwitchInteractive = ObjectToSwitch.GetComponent(typeof(InteractiveObject)) as InteractiveObject;

        SetSwitchOn(StartOn);
	}
	
	void Update () 
    {
        if (m_SwitchInteractive != null)
        {
            bool switchedOn = m_SwitchInteractive.IsSwitchedOn();

            if (switchedOn != m_SwitchOn)
            {
                SetSwitchOn(switchedOn);
            }
        }
	}

    void OnTriggerEnter(Collider other) 
    {
        if (other.tag == "IgnoreSwitches")
        {
            return;
        }
        
        SetSwitchOn(!m_SwitchOn);
    }

    public void SetSwitchOn(bool on)
    {
        m_SwitchOn = on;

        if (m_SwitchInteractive != null)
        {
            m_SwitchInteractive.OnSwitchChange(m_SwitchOn);
        }

        UpdateSwitchVisuals();
    }

    void UpdateSwitchVisuals()
    {
        if (m_SwitchOn)
        {
            GetComponent<Renderer>().material.color = OnColor;
        }
        else
        {
            GetComponent<Renderer>().material.color = OffColor;
        }
    }

    bool m_SwitchOn;
    InteractiveObject m_SwitchInteractive;
}
