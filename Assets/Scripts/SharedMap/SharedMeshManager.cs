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

    private GameObject LoadedMap = null;

    private MeshFilter rootMesh;

    // object of the script ARMarkersDataManager.c, that allows to serialize transformation of game object (position and orientation)
    private ARmarkersContainer ARmanager;

    private ARmarkersContainer ARData;

    //string url = "C:/Users/francesco testa/Desktop/tesi/mesh/map.bin";

    private void Start()
    {
        ARmanager = new ARmarkersContainer();
        ARData = new ARmarkersContainer();
    }

    /// <summary>
    /// Place the mesh in the proper position with respect to its anchor
    /// </summary>
    /// <param name="root"></param>
    /// <param name="anchor"></param>
    static void meshLocation(GameObject root, GameObject anchor)
    {
        root.transform.parent = anchor.transform;
        Vector3 directionVector = root.transform.InverseTransformPoint(new Vector3(0, 0, 0));
        root.transform.localPosition = -directionVector;
        root.transform.localRotation = Quaternion.Euler(root.transform.InverseTransformDirection(directionVector));
    }

    /// <summary>
    /// Combine all separate mesh pieces attached to the SceneRoot Game Object into a single mesh component. Please notice that the root must be an empty gameObject
    /// </summary>
    /// <returns></returns>
    public Mesh combineMesh(GameObject root, GameObject anchor)
    {
        GameObject parallelSceneRoot = Instantiate(root);

        parallelSceneRoot.transform.parent = anchor.transform;
        //Vector3 realPosition = parallelSceneRoot.transform.localPosition;
        //Quaternion realRotation = parallelSceneRoot.transform.localRotation;
        parallelSceneRoot.name = "parallelSceneroot";

        parallelSceneRoot.transform.localPosition = Vector3.zero;
        parallelSceneRoot.transform.localRotation = Quaternion.identity;

        MeshFilter[] meshFilters = parallelSceneRoot.GetComponentsInChildren<MeshFilter>();

        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        Mesh finalMesh = new Mesh();


        for (int i = 0; i < meshFilters.Length; i++)
        {
            if (meshFilters[i] != null)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;

                //Debug.Log("parent mesh ser name" + meshFilters[i].transform.parent.name);

                // I have to specify the mesh transform w.r.t. a reference frame (different from the object's mesh), in this case I use the anchor frame since the parallelSceneRoot
                // is a child of anchor and I m creating the local transform matrix. 
                combine[i].transform = Matrix4x4.TRS(meshFilters[i].transform.localPosition, meshFilters[i].transform.localRotation, meshFilters[i].transform.localScale);

                meshFilters[i].gameObject.SetActive(false);
            }

        }

        MeshFilter rootMesh = parallelSceneRoot.transform.GetComponent<MeshFilter>();
        //Because the scene root has no MeshFilter component but only its childs, so we need to add it
        if (rootMesh != null)
        {
            Debug.Log("reset mesh parallelSceneRoot");
            rootMesh.mesh = new Mesh();
        }
        else
        {
            Debug.Log("parallelSceneRoot has no Mesh component, I add it");
            rootMesh = parallelSceneRoot.AddComponent(typeof(MeshFilter)) as MeshFilter;

            MeshRenderer rootMeshRender = parallelSceneRoot.AddComponent<MeshRenderer>() as MeshRenderer;
            rootMeshRender.material = material;
        }

        finalMesh.CombineMeshes(combine);

        DestroyImmediate(parallelSceneRoot);
        return finalMesh;
    }


    /// <summary>
    /// Combine all peaces of mesh of the root, and return a byte array containing its serialization. Please notice that the root must be an empty gameObject
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    public byte[] combineMeshAsByte(GameObject root, GameObject anchor)
    {
        GameObject parallelSceneRoot = Instantiate(root);

        parallelSceneRoot.transform.parent = anchor.transform;
        //Vector3 realPosition = parallelSceneRoot.transform.localPosition;
        //Quaternion realRotation = parallelSceneRoot.transform.localRotation;
        parallelSceneRoot.name = "parallelSceneroot";

        parallelSceneRoot.transform.localPosition = Vector3.zero;
        parallelSceneRoot.transform.localRotation = Quaternion.identity;

        MeshFilter[] meshFilters = parallelSceneRoot.GetComponentsInChildren<MeshFilter>();

        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        Mesh finalMesh = new Mesh();


        for (int i = 0; i < meshFilters.Length; i++)
        {
            if (meshFilters[i] != null)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;

                //Debug.Log("parent mesh ser name" + meshFilters[i].transform.parent.name);

                // I have to specify the mesh transform w.r.t. a reference frame (different from the object's mesh), in this case I use the anchor frame since the parallelSceneRoot
                // is a child of anchor and I m creating the local transform matrix. 
                combine[i].transform = Matrix4x4.TRS(meshFilters[i].transform.localPosition, meshFilters[i].transform.localRotation, meshFilters[i].transform.localScale);

                meshFilters[i].gameObject.SetActive(false);
            }

        }

        MeshFilter rootMesh = parallelSceneRoot.transform.GetComponent<MeshFilter>();
        //Because the scene root has no MeshFilter component but only its childs, so we need to add it
        if (rootMesh != null)
        {
            Debug.Log("reset mesh parallelSceneRoot");
            rootMesh.mesh = new Mesh();
        }
        else
        {
            Debug.Log("parallelSceneRoot has no Mesh component, I add it");
            rootMesh = parallelSceneRoot.AddComponent(typeof(MeshFilter)) as MeshFilter;

            MeshRenderer rootMeshRender = parallelSceneRoot.AddComponent<MeshRenderer>() as MeshRenderer;
            rootMeshRender.material = material;
        }

        finalMesh.CombineMeshes(combine);

        byte[] meshByte = MeshSerializer.SerializeMesh(finalMesh);
        Debug.Log("combine mesh position" + rootMesh.transform.position);
        DestroyImmediate(parallelSceneRoot);
        return meshByte;
    }

    /*public byte[] combineMeshAsByte(GameObject root, GameObject anchor)
    {
        GameObject parallelSceneRoot = Instantiate(root);
        parallelSceneRoot.name = "parallelSceneroot";

        Vector3 realPosition = parallelSceneRoot.transform.position;
        Quaternion realOrientation = parallelSceneRoot.transform.rotation;

        parallelSceneRoot.transform.position = Vector3.zero;
        parallelSceneRoot.transform.rotation = Quaternion.identity;

        Debug.Log("parall local position " + parallelSceneRoot.transform.localPosition);

        MeshFilter[] meshFilters = parallelSceneRoot.GetComponentsInChildren<MeshFilter>();

        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        Mesh finalMesh = new Mesh();

        for (int i = 0; i < meshFilters.Length; i++)
        {
            if (meshFilters[i])
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;

                meshFilters[i].gameObject.SetActive(false);
            }

        }
        MeshFilter rootMesh = parallelSceneRoot.transform.GetComponent<MeshFilter>();
        //Because the scene root has no mesh component but only its childs, so we need to add it
        if (rootMesh != null)
        {
            Debug.Log("reset mesh parallelSceneRoot");
            rootMesh.mesh = new Mesh();
        }
        else
        {
            Debug.Log("parallelSceneRoot has no Mesh component, I add it");
            rootMesh = parallelSceneRoot.AddComponent(typeof(MeshFilter)) as MeshFilter;

            MeshRenderer rootMeshRender = parallelSceneRoot.AddComponent<MeshRenderer>() as MeshRenderer;
            rootMeshRender.material = material;
        }

        finalMesh.CombineMeshes(combine);
        parallelSceneRoot.GetComponent<MeshFilter>().mesh = finalMesh;
        parallelSceneRoot.transform.position = realPosition;
        parallelSceneRoot.transform.rotation = realOrientation;

        MeshFilter meshNmae = parallelSceneRoot.GetComponent<MeshFilter>();

        byte[] meshByte = MeshSerializer.SerializeMesh(meshNmae.mesh);
        Debug.Log("combine mesh position" + meshNmae.transform.position);
        DestroyImmediate(parallelSceneRoot);
        return meshByte;
    }*/


    #region Save and Load Mesh Locally
    public void SaveMesh()
    {
        //string path = Path.Combine(Application.persistentDataPath, url);
        //combineMesh(SceneRoot);

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

    // Old methods
    #region Old methods
    /// <summary>
    /// Return the whole mesh of the scene (SceneRoot)
    /// </summary>
    /// <returns></returns>
    /*public byte[] MeshAsByte()
    {
        rootMesh = combineMesh(SceneRoot);
        //Serialize the combined mesh of the map
        
        return MeshSerializer.SerializeMesh(rootMesh.mesh);
        //return MeshSerializer.SerializeMesh(SceneRoot.transform.GetComponent<MeshFilter>().mesh);
    }*/



    /// <summary>
    /// Return the AR data of the scene (SceneRoot) as byte array
    /// </summary>
    /// <returns></returns>
    public byte[] ARDataAsByte(GameObject root, GameObject anchor)
    {
        // Combine all separate mesh pieces into one
        root.transform.parent = anchor.transform;
        ARmanager.SetAnchorPosition(root.transform.localPosition);
        ARmanager.SetAnchorOrientation(root.transform.localRotation);
        // per svincolare lo sceneRoot
        root.transform.parent = null;
        // Serialize the ARdata
        return ARmanager.ARDataBinarySerialize(ARmanager);
    }

    /// <summary>
    /// Joins the dowloaded mesh map (stored in parallelSceneRoot) with the  
    /// </summary>
    /// <param name="ARDataByte"></param>
    public void LocateSubMap(byte[] ARDataByte, byte[] meshByte, GameObject anchor)
    {
        Debug.Log("Start joining map ");
        if (ARDataByte != null && meshByte != null)
        {           
            LoadedMap = new GameObject("LoadedMap");
            if (LoadedMap.GetComponent<MeshFilter>() == false)
            {
                LoadedMap.AddComponent<MeshFilter>();
                Debug.Log("MeshFilter added");
            }
            if (LoadedMap.GetComponent<MeshRenderer>() == false)
            {
                LoadedMap.AddComponent<MeshRenderer>();
                Debug.Log("MeshRender added");
            }

            material.SetColor("_Color", Color.red);
            LoadedMap.transform.GetComponent<MeshRenderer>().material = material;
            LoadedMap.transform.GetComponent<MeshFilter>().mesh = MeshSerializer.DeserializeMesh(meshByte);
            LoadedMap.transform.parent = anchor.transform;
            LoadedMap.transform.localPosition = Vector3.zero;
            LoadedMap.transform.localRotation = Quaternion.identity;
            ARmarkersContainer ARData = (ARmarkersContainer)ARmanager.ARDataBinaryDeserialize(ARDataByte);
            Debug.Log("ARData deserialized properly");
            LoadedMap.transform.localPosition = ARData.GetAnchorPosition();
            LoadedMap.transform.localRotation = ARData.GetAchorOrientation();
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

            LoadedMap.AddComponent<MeshFilter>();
            Debug.Log("Mesh Filter component added to LoadedMap");
            

            LoadedMap.AddComponent<MeshRenderer>();
            Debug.Log("Mesh Render component added to LoadedMap");
            
            material.SetColor("_Color", Color.red);
            LoadedMap.transform.GetComponent<MeshRenderer>().material = material;
            Debug.Log("Add mesh render ");
            // add downloded mesh
            LoadedMap.transform.GetComponent<MeshFilter>().mesh = MeshSerializer.DeserializeMesh(meshByte);
            Debug.Log("add mesh filter");
            // reduce size
            float scale = 0.5f;
            Vector3 mapLocalPos = LoadedMap.transform.localPosition;
            LoadedMap.transform.localPosition = new Vector3(mapLocalPos.x * scale, mapLocalPos.y * scale, mapLocalPos.z * scale);

            LoadedMap.transform.localScale = new Vector3(scale, scale, scale);



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

    /// <summary>
    /// Takes a Serialized mesh and locate it properly w.r.t. the spatial anchor. 
    /// </summary>
    /// <param name="meshByte"></param>
    public void locateMap(byte[] meshByte)
    {
        if (meshByte != null)
        {
            LoadedMap = new GameObject("LoadedMap");
            if (LoadedMap.GetComponent<MeshFilter>() == false)
            {
                LoadedMap.AddComponent<MeshFilter>();
                Debug.Log("MeshFilter added");
                //final.GetComponent<MeshFilter>().transform.parent = GameObject.Find("Anchor").transform;
            }
            if (LoadedMap.GetComponent<MeshRenderer>() == false)
            {
                LoadedMap.AddComponent<MeshRenderer>();
                Debug.Log("MeshRender added");
            }

            material.SetColor("_Color", Color.red);
            LoadedMap.transform.GetComponent<MeshRenderer>().material = material;
            Debug.Log("Add mesh render ");
            // add downloded mesh
            LoadedMap.transform.GetComponent<MeshFilter>().mesh = MeshSerializer.DeserializeMesh(meshByte);
            Debug.Log("add mesh filter");
            meshLocation(LoadedMap, GameObject.Find("CloudDataManager"));
            LoadedMap.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Debug.Log("Locate mesh");
        }
        else {
            Debug.LogError("Mesh was wrongly combined!");
        }
    }


    #endregion
}
