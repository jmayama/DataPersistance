using UnityEngine;
using System.Collections;

public class LevelTransitionTrigger : MonoBehaviour 
{
    public string NextLevelName;

    void OnTriggerEnter(Collider collider)
    {
        if (collider.tag == "Player")
        {
            GameManager.Instance.TransitionToLevel(NextLevelName);
        }
    }
}
