using UnityEngine;
using System.Collections;

public interface InteractiveObject 
{
    void OnSwitchChange(bool switchOn);

    bool IsSwitchedOn();

}
