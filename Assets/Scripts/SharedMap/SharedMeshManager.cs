using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using B83.MeshTools;
using ARMarkersDataManager;

#if WINDOWS_UWP
    using WindowsStorage = global::Windows.Storage;
    using Windows.Storage;
    using Windows.Storage.Streams;
    //using Microsoft.VisualStudio.Data;
#endif

public class SharedMeshManager : MonoBehaviour
{
    [Tooltip("Reference to the root of the scene (SceneRoot).")]
    [SerializeField]
    public GameObject SceneRoot = null;
    [Tooltip("Mesh Material.")]
    [SerializeField]
    public Material material = null;
    [Tooltip("Reference to the root of the scene (Load Root).")]
    [SerializeField]
    public GameObject LoadedMap = null;

    

    private GameObject parallelSceneRoot;

    private MeshFilter rootMesh;

    private ARmarkersContainer ARmanager;

    //string url = "C:/Users/francesco testa/Desktop/tesi/mesh/map.bin";

    private void Start()
    {
        ARmanager = new ARmarkersContainer();
    }


    /// <summary>
    /// Combine all separate mesh pieces attached to the SceneRoot Game Object into a single mesh component
    /// </summary>
    /// <returns></returns>
    public MeshFilter combineMesh()
    {
        // make a snapshot of the current sceneRoot 
        parallelSceneRoot = Instantiate(SceneRoot);
        //place it
        parallelSceneRoot.transform.position = new Vector3(0, 0, 2);
        
        // combilne all peaces of mesh of the snapshot
        MeshFilter[] meshFilters = parallelSceneRoot.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        Debug.Log(meshFilters.Length);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            Debug.Log(meshFilters[i]);
            if (meshFilters[i])
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                Debug.Log(combine[i].mesh);
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                // destroy all child 
                DestroyImmediate(meshFilters[i].gameObject);
            }
        }
        Debug.Log("combine length " + combine.Length);
        //DestroyAllGameObjectsUnderParent(root.transform);
        rootMesh = parallelSceneRoot.transform.GetComponent<MeshFilter>();
        if (rootMesh != null)
        {
            // the parallelSceneRoot already has a mesh component
            rootMesh.mesh = new Mesh();
        }
        else
        {
            Debug.Log("parallelSceneRoot has no Mesh component, I add it");
            rootMesh = parallelSceneRoot.AddComponent(typeof(MeshFilter)) as MeshFilter;
            MeshRenderer rootMeshRender = parallelSceneRoot.AddComponent<MeshRenderer>() as MeshRenderer;
            rootMeshRender.material = material;
        }
        rootMesh.mesh.CombineMeshes(combine);
        rootMesh.transform.gameObject.SetActive(false);
        //textObj.text = "Mesh Combined";
        parallelSceneRoot.transform.rotation = Quaternion.Euler(0, 0, 0);
        parallelSceneRoot.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        return rootMesh;
    }

    #region Save and Load Mesh Locally
    public void SaveMesh()
    {
        //string path = Path.Combine(Application.persistentDataPath, url);
        combineMesh();

#if WINDOWS_UWP
                var folder = WindowsStorage.ApplicationData.Current.LocalFolder;
                string path = folder.Path + "/meshMap.bin";
                byte[] bytes = MeshSerializer.SerializeMesh(rootMesh.mesh);
                File.WriteAllBytes(path, bytes);
                

#else
        Debug.Log("Load on Device is only supported in Universal Windows Applications");
#endif
    }

    public void LoadMesh()
    {
        //string path = Path.Combine(Application.persistentDataPath, url);

#if WINDOWS_UWP
                var folder = WindowsStorage.ApplicationData.Current.LocalFolder;
                string path = folder.Path + "/meshMap.bin";
                if (File.Exists(path) == true)
                        {
                            //textObj.text ="File found";
                            byte[] bytes = File.ReadAllBytes(path);
                            //textObj.text = "Bytes loaded";
                            if(bytes == null){
                               //textObj.text = "Bytes are null";
                            }
                            LoadedMap.AddComponent<MeshFilter>();
                            LoadedMap.AddComponent<MeshRenderer>();

                            material.SetColor("_Color", Color.red);
                            LoadedMap.transform.GetComponent<MeshRenderer>().material = material;

                            //textObj.text = "Add material correctly";
                            LoadedMap.transform.GetComponent<MeshFilter>().mesh = MeshSerializer.DeserializeMesh(bytes);
                            LoadedMap.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                            //LoadedMap.transform.position = new Vector3(1,0,3);
                            LoadedMap.transform.rotation = Quaternion.Euler(0, 0, 0);
                            //textObj.text = "hologram componets added";
                            //LoadedMap.AddComponent<MeshCollider>();
                            //LoadedMap.AddComponent<ObjectManipulator>();
                            //LoadedMap.AddComponent<NearInteractionGrabbable>();
                            //textObj.text = "Mesh Map Loaded correctly";
                        }                

#else
        Debug.Log("Load on Device is only supported in Universal Windows Applications");
#endif
    }
    #endregion

    #region Serialized Mesh and AR data as Byte array

    /// <summary>
    /// Return the whole mesh of the scene (SceneRoot)
    /// </summary>
    /// <returns></returns>
    public byte[] MeshAsByte()
    {
        rootMesh = combineMesh();
        // Serialize the combined mesh of the map
        
        return MeshSerializer.SerializeMesh(rootMesh.mesh);
    }
    /// <summary>
    /// Return the AR data of the scene (SceneRoot) as byte array
    /// </summary>
    /// <returns></returns>
    public byte[] ARDataAsByte()
    {
        // Combine all separate mesh pieces into one
        SceneRoot.transform.parent = GameObject.Find("CloudDataManager").transform;
        ARmanager.SetAnchorPosition(SceneRoot.transform.localPosition);
        ARmanager.SetAnchorOrientation(SceneRoot.transform.localRotation);
        // per svincolare lo sceneRoot
        SceneRoot.transform.parent = null;
        // Serialize the ARdata
        return ARmanager.ARDataBinarySerialize(ARmanager);
    }

    public void joinSubMap(byte[] ARDataByte)
    {
        if (ARDataByte != null)
        {
            ARmarkersContainer ARData = ARmanager.ARDataBinaryDeserialize(ARDataByte);
            parallelSceneRoot.transform.parent = GameObject.Find("CloudDataManager").transform;  /// PRIMA ERA LoadedMapp
            parallelSceneRoot.transform.localPosition = ARData.GetAnchorPosition();
            parallelSceneRoot.transform.localRotation = ARData.GetAchorOrientation();
            // riducendo le dimenioni di local map dovrebbero cambiare le proporzioni rispetto all ancora 
            // se così non fosse allora bisogna ridurre la scala dell 'ancora
            Debug.Log("Submaps joined correctly");
        }
        else
        {
            Debug.LogError("Error joining submaps");
        }
    }

    /// <summary>
    /// Load the mesh given a byte array
    /// </summary>
    /// <param name="meshByte"></param>
    public void LoadMeshByte(byte[] meshByte)
    {
        if (meshByte != null)
        {
            // ATT !!!  LoadedMap E' il CloudDataManager non dovrebbe essere questo subito provare ad usare parallelSceneRoot
            if (LoadedMap.GetComponent<MeshFilter>() == false)   /// PRIMA ERA LoadedMapp  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            {
                LoadedMap.AddComponent<MeshFilter>();
            }
            if (LoadedMap.GetComponent<MeshRenderer>() == false)
            {
                LoadedMap.AddComponent<MeshRenderer>();
            }
            

            material.SetColor("_Color", Color.red);
            LoadedMap.transform.GetComponent<MeshRenderer>().material = material;
            // add downloded mesh
            LoadedMap.transform.GetComponent<MeshFilter>().mesh = MeshSerializer.DeserializeMesh(meshByte);
            // reduce size
            LoadedMap.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            //LoadedMap.transform.rotation = Quaternion.Euler(0, 0, 0);
            //LoadedMap.AddComponent<MeshCollider>();
            //LoadedMap.AddComponent<ObjectManipulator>();
            //LoadedMap.AddComponent<NearInteractionGrabbable>();
            
            Debug.Log("Downloaded Mesh Placed");
        }
        else {
            Debug.LogError("Error placing downloaded mesh");
        }
    }


    #endregion
}
