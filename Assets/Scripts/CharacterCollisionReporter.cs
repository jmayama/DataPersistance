using UnityEngine;
using System.Collections;

public class CharacterCollisionReporter : MonoBehaviour 
{
    public Player ParentObject;
    
    void Start () 
    {
        ParentObject.CollisionReporter = this;
	}

    void OnCollisionStay(Collision collision)
    {
        //Passing the collision info on to the "parent" 
        ParentObject.OnCollisionStay(collision);
    }
}
