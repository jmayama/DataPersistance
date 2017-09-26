using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class GameManager : MonoBehaviour 
{
    public static GameManager Instance;
    
    //This isn't really needed for anything other than confirming that the game manager persisted between scenes
    public string OwningLevelName;

    public string SaveFileName = "TestSaveGame.sav";

    //This is a list of prefabs of objects that can be created dynamically.  This is done to allow support for saving the
    //objects.  By having a central location to create the objects we can ensure that objects that need to know their
    //creation id will have it set on them when they are created.
    public List<GameObject> DynamicObjectPrefabs;

    //The name of the empty game object used to position the player when he/she transitions between levels.
    const string TransitionPtName = "LevelTransitionPt";

    //Tracks the version of your save games.  You should increment this number whenever you make changes that will
    //effect what gets saved.  This will allow you to detect incompatible save game versions early, instead of getting
    //wierd hard to track down bugs.
    const int SaveGameVersionNum = 4;

    //The ids used to create dynamic objects
    public enum DynamicObjectsCreateId
    {
        Invalid = -1,
        Bullet1,
        Bullet2,
    }

    void Awake()
    {
        //This is similar to a singleton in that it only allows one instance to exist and there is instant global 
        //access to the GameManager using the static Instance member.
        //
        //This will set the instance to this object if it is the first time it is created.  Otherwise it will delete 
        //itself.
        if (Instance == null)
        {
            //This tells unity not to delete the object when you load another scene
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        m_LoadSceneState = LoadSceneState.NotLoadingScene;

        m_PersistentData = new GamePersistentData();
    }

	void Start () 
    {
        m_PersistentData.levelName = Application.loadedLevelName;
	}
	
	void Update () 
    {
	
	}

    public Player GetPlayer()
    {
        return m_CurrentPlayer;
    }

    public void RegisterPlayer(Player player)
    {
        m_CurrentPlayer = player;
    }

    //This will search for a game object based on a save id.  This might end up being slow if 
    //there are a lot of objects in a scene.
    public GameObject GetGameObjectBySaveId(string saveId)
    {
        GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject searchObj in gameObjects)
        {
            SaveHandler saveHandler = searchObj.GetComponent<SaveHandler>();
            if (saveHandler == null)
            {
                continue;
            }

            if (saveHandler.SaveId == saveId)
            {
                return searchObj;
            }
        }

        return null;
    }

    //This callback gets called when a scene is done loading
    void OnLevelWasLoaded(int level)
    {
        switch(m_LoadSceneState)
        {
            case LoadSceneState.NotLoadingScene:

                break;

            case LoadSceneState.LevelTransition:
                //Move the player to the proper transition position

                GameObject transitionPt = GameObject.Find(TransitionPtName);

                if (transitionPt != null && m_CurrentPlayer != null)
                {
                    m_CurrentPlayer.transform.position = transitionPt.transform.position;
                }
                break;

            case LoadSceneState.LoadingSaveGame:
                //This section will finish loading the save game.  We need to load the objects here since
                //the objects will get deleted as part of loading a new scene.

                //Load player
                m_CurrentPlayer.OnLoad(m_LoadGameStream, m_LoadGameFormatter);

                //Get number of objects to load
                int numObjectsToLoad = (int)m_LoadGameFormatter.Deserialize(m_LoadGameStream);

                //Load dynamic objects Stage 1.  The objects are loaded in two stages so that
                //by the time the second stage is running all of the objects will exist which
                //will make reconstructing relationships between everything easier
                List<GameObject> gameObjectsLoaded = new List<GameObject>();
                for (int i = 0; i < numObjectsToLoad; ++i)
                {
                    GameObject loadedObject = SaveHandler.LoadObject(m_LoadGameStream, m_LoadGameFormatter);

                    if (loadedObject != null)
                    {
                        gameObjectsLoaded.Add(loadedObject);
                    }
                }

                //Load dynamic objects Stage 2
                for (int i = 0; i < numObjectsToLoad; ++i)
                {
                    GameObject loadedObject = gameObjectsLoaded[i];

                    SaveHandler saveHandler = loadedObject.GetComponent<SaveHandler>();

                    saveHandler.LoadData(m_LoadGameStream, m_LoadGameFormatter);
                }

                //Clean up
                m_LoadGameStream.Close();
                m_LoadGameStream = null;
                m_LoadGameFormatter = null;
                break;

            default:
                DebugUtils.LogError("Unsupported load scene state after load: {0}", m_LoadSceneState);
                break;
        }

        m_PersistentData.levelName = Application.loadedLevelName;

        m_LoadSceneState = LoadSceneState.NotLoadingScene;
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 100, 30), "Save Game"))
        {
            Save(SaveFileName);
        }

        if (GUI.Button(new Rect(10, 50, 100, 30), "Load Game"))
        {
            Load(SaveFileName);
        }
    }

    //Call this function to transition the player to a new scene.
    public void TransitionToLevel(string levelName)
    {
        m_LoadSceneState = LoadSceneState.LevelTransition;

        //Move to the next scene
        DebugUtils.Log("Transitioning to level: {0}", levelName);

        Application.LoadLevel(levelName);
    }
    
    //Use this to save your game.  Make sure that the order that you serialize things is the same as the order that
    //you deserialize things
    public void Save(string fileName)
    {
        string savePath = GetSaveFilePath(fileName);

        DebugUtils.Log("Saving Game to file: '{0}'", savePath);

        BinaryFormatter binaryFormatter = new BinaryFormatter();
        FileStream file = File.Create(savePath);

        //Version number
        binaryFormatter.Serialize(file, SaveGameVersionNum);

        //Save persistent data
        binaryFormatter.Serialize(file, m_PersistentData);

        //Save player 
        m_CurrentPlayer.OnSave(file, binaryFormatter);

        //Save the objects
        GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();

        //Need to save out how many objects are saved so we konw how how many to loop over
        //when we load
        int saveObjectCount = 0;
        foreach (GameObject gameObjectToSave in gameObjects)
        {
            //Get the save handler on the object if there is one
            SaveHandler saveHandler = gameObjectToSave.GetComponent<SaveHandler>();
            if (saveHandler == null)
            {
                continue;
            }

            //Checking if this object can and should be saved
            if (!saveHandler.AllowSave)
            {
                continue;
            }

            ++saveObjectCount;
        }

        binaryFormatter.Serialize(file, saveObjectCount);

        //Save the game objects stage 1.  Saving out all of the data needed to recreate
        //the objects
        foreach (GameObject gameObjectToSave in gameObjects)
        {
            //Get the save handler on the object if there is one
            SaveHandler saveHandler = gameObjectToSave.GetComponent<SaveHandler>();
            if (saveHandler == null)
            {
                continue;
            }

            //Checking if this object can and should be saved
            if (!saveHandler.AllowSave)
            {
                continue;
            }

            saveHandler.SaveObject(file, binaryFormatter);
        }

        //Save the game objects stage 2.  Saving the rest of the data for the objects
        foreach (GameObject gameObjectToSave in gameObjects)
        {
            //Get the save handler on the object if there is one
            SaveHandler saveHandler = gameObjectToSave.GetComponent<SaveHandler>();
            if (saveHandler == null)
            {
                continue;
            }

            //Checking if this object can and should be saved
            if (!saveHandler.AllowSave)
            {
                continue;
            }

            saveHandler.SaveData(file, binaryFormatter);
        }

        //Clean up
        file.Close();
    }

    //Use this to load your game.  Make sure that the order that you deserialize things is the same as the order that
    //you serialize things
    public void Load(string fileName)
    {
        //Set the proper loading state
        m_LoadSceneState = LoadSceneState.LoadingSaveGame;

        //Get and verify the save path
        string savePath = GetSaveFilePath(fileName);

        DebugUtils.Log("Loading Game from file: {0}...", savePath);

        if (!File.Exists(savePath))
        {
            DebugUtils.Log("LoadFile doesn't exist: {0}", savePath);
            return;
        }

        m_LoadGameFormatter = new BinaryFormatter();
        m_LoadGameStream = File.Open(savePath, FileMode.Open);

        //Version number
        int versionNumber = (int)m_LoadGameFormatter.Deserialize(m_LoadGameStream);

        DebugUtils.Log("Load file version number: {0}", versionNumber);

        //Handle version numbers appropriately
        DebugUtils.Assert(versionNumber == SaveGameVersionNum, "Loading an incompatible version number (File version: {0}, Expected version: {1}).  Your game may become unstable.", versionNumber, SaveGameVersionNum);

        //Load GameSaveData
        m_PersistentData = (GamePersistentData)m_LoadGameFormatter.Deserialize(m_LoadGameStream);
        
        //Load the level
        Application.LoadLevel(m_PersistentData.levelName);

        //The rest will be loaded once the level is done loading.  (otherwise the loaded objects will be
        //deleted during the level transition)
    }

    //This is a factory method that is responsible for creating dynamic objects in the game
    public GameObject CreateDynamicObject(
        DynamicObjectsCreateId createId,
        Vector3 position,
        Quaternion rotation
        )
    {
        //Get the prefab to create the object
        GameObject prefab = DynamicObjectPrefabs[(int)createId];

        //Create the object
        GameObject newObject = (GameObject)Instantiate(prefab, position, rotation);

        //Give it the info it needs to load and save if it needs it.
        SaveHandler saveHandler = newObject.GetComponent<SaveHandler>();
        if (saveHandler != null)
        {
            saveHandler.Init(createId);
        }

        return newObject;
    }

    //A helper function to create the save path.  This uses the persistentDataPath, which will be a safe place
    //to store data on a user's machine without errors.
    string GetSaveFilePath(string fileName)
    {
        return Application.persistentDataPath + "/" + fileName;
    }

    //This is used to keep track of what kind of loading we need to do
    enum LoadSceneState
    {
        NotLoadingScene,
        LevelTransition,
        LoadingSaveGame
    }

    Player m_CurrentPlayer;

    LoadSceneState m_LoadSceneState;

    BinaryFormatter m_LoadGameFormatter;
    FileStream m_LoadGameStream;

    GamePersistentData m_PersistentData;
}

//This is some of the data that will be saved with each save game.  It is marked as serializeable so that if 
//we add any data to it, it will automatically get serialize without any other changes.
[Serializable]
struct GamePersistentData
{
    public string levelName;
}
