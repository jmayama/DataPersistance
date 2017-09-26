using UnityEngine;
using System.Collections;

public class SwingDoor : MonoBehaviour, InteractiveObject
{
    public bool StartOpen = false;
    public float OpenDegPerSec = 90;

	void Start () 
    {
        OnSwitchChange(StartOpen);	    
	}
	
	void Update () 
    {
	
	}

    public void OnSwitchChange(bool switchOn)
    {
        SetOpenState(switchOn);
    }

    public bool IsSwitchedOn()
    {
        return m_Open;
    }

    void SetOpenState(bool open)
    {
        m_Open = open;

        JointMotor hingeMotor = GetComponent<HingeJoint>().motor;

        if (m_Open)
        {
            hingeMotor.targetVelocity = -OpenDegPerSec;
        }
        else
        {
            hingeMotor.targetVelocity = OpenDegPerSec;
        }

        GetComponent<HingeJoint>().motor = hingeMotor;
    }

    bool m_Open;
}
