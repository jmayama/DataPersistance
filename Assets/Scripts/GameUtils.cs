

using UnityEngine;
using System;
using System.Runtime.Serialization;
using System.IO;

public class GameUtils
{
    //Recursively sets the alpha on all objects in the heirarchy, starting at the first object you 
    //pass in through currentObj.
    //
    //Note:  Make sure your material supports transparency, or this probably wont have an effect.
    public static void SetAlpha(float alpha, GameObject currentObj)
    {
        //Set color on object
        MeshRenderer meshRenderer = currentObj.GetComponent<Renderer>() as MeshRenderer;

        if (meshRenderer != null && meshRenderer.material != null)
        {
            foreach (Material material in meshRenderer.materials)
            {
                Vector4 color = material.color;
                color.w = alpha;

                material.color = color;
            }
        }

        //Set alpha on children
        for (int i = 0; i < currentObj.transform.childCount; ++i)
        {
            SetAlpha(alpha, currentObj.transform.GetChild(i).gameObject);
        }
    }

    public static void SerializeVector3(Stream stream, IFormatter formatter, Vector3 value)
    {
        formatter.Serialize(stream, value.x);
        formatter.Serialize(stream, value.y);
        formatter.Serialize(stream, value.z);
    }

    public static Vector3 DeserializeVector3(Stream stream, IFormatter formatter)
    {
        Vector3 value = new Vector3();

        value.x = (float)formatter.Deserialize(stream);
        value.y = (float)formatter.Deserialize(stream);
        value.z = (float)formatter.Deserialize(stream);

        return value;
    }

    public static void SerializeQuaternion(Stream stream, IFormatter formatter, Quaternion value)
    {
        formatter.Serialize(stream, value.w);
        formatter.Serialize(stream, value.x);
        formatter.Serialize(stream, value.y);
        formatter.Serialize(stream, value.z);
    }

    public static Quaternion DeserializeQuaternion(Stream stream, IFormatter formatter)
    {
        Quaternion value = new Quaternion();

        value.w = (float)formatter.Deserialize(stream);
        value.x = (float)formatter.Deserialize(stream);
        value.y = (float)formatter.Deserialize(stream);
        value.z = (float)formatter.Deserialize(stream);

        return value;
    }
}
