using UnityEngine;
using System.Collections;

public abstract class UsableItem : MonoBehaviour 
{

	void Start () 
    {
	
	}
	
	void Update () 
    {
	
	}

    public virtual void Equip(Player player)
    {
        gameObject.SetActive(true);
    }

    public virtual void Unequip()
    {
        InUse = false;

        gameObject.SetActive(false);
    }

    public bool InUse { get; set; }

}
