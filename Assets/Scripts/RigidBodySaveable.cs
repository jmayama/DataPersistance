using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.IO;

//Put this component on objects that are raw physics objects that just need their rigid bodies serialized
public class RigidBodySaveable : MonoBehaviour, Saveable
{
    public void OnSave(Stream stream, IFormatter formatter)
    {
        GameUtils.SerializeVector3(stream, formatter, transform.position);
        GameUtils.SerializeQuaternion(stream, formatter, transform.rotation);

        GameUtils.SerializeVector3(stream, formatter, GetComponent<Rigidbody>().velocity);
    }

    public void OnLoad(Stream stream, IFormatter formatter)
    {
        transform.position = GameUtils.DeserializeVector3(stream, formatter);
        transform.rotation = GameUtils.DeserializeQuaternion(stream, formatter);

        GetComponent<Rigidbody>().velocity = GameUtils.DeserializeVector3(stream, formatter);
    }
}
