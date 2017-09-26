using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.IO;

//The saveable interface for components.  These functions should only be called
//by the SaveHandler class
public interface Saveable
{
    void OnSave(Stream stream, IFormatter formatter);

    void OnLoad(Stream stream, IFormatter formatter);
}
