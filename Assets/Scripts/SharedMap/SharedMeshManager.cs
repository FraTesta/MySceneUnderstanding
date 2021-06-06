using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;

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


    public TextMeshPro textObj = null;

    public GameObject parallelSceneRoot;

    private MeshFilter rootMesh;

    //string url = "C:/Users/francesco testa/Desktop/tesi/mesh/map.bin";


    // Start is called before the first frame update
    /*void Start()
    {
        MeshFilter rootMesh = combineMesh();
        SaveMesh(rootMesh.mesh);
        Mesh mesh = LoadMesh();
        gameObject.GetComponent<MeshFilter>().mesh = mesh;
    }*/



    public MeshFilter combineMesh()
    {
        parallelSceneRoot = Instantiate(SceneRoot);
        //piazzare questo gameObject
        parallelSceneRoot.transform.position = new Vector3(0, 0, 2);
        

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
            Debug.Log("Different from null");
            rootMesh.mesh = new Mesh();
        }
        else
        {
            Debug.Log("Equal from null");
            rootMesh = parallelSceneRoot.AddComponent(typeof(MeshFilter)) as MeshFilter;
            MeshRenderer rootMeshRender = parallelSceneRoot.AddComponent<MeshRenderer>() as MeshRenderer;
            rootMeshRender.material = material;
        }
        rootMesh.mesh.CombineMeshes(combine);
        rootMesh.transform.gameObject.SetActive(true);
        textObj.text = "Mesh Combined";
        parallelSceneRoot.transform.rotation = Quaternion.Euler(0, 0, 0);
        parallelSceneRoot.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        return rootMesh;
    }


    public void SaveMesh()
    {
        //string path = Path.Combine(Application.persistentDataPath, url);
        combineMesh();
        textObj.text = "After Combine Mesh";
#if WINDOWS_UWP
                var folder = WindowsStorage.ApplicationData.Current.LocalFolder;
                string path = folder.Path + "/meshMap.bin";
                byte[] bytes = MeshSerializer.WriteMesh(rootMesh.mesh, true);
                File.WriteAllBytes(path, bytes);
                textObj.text ="Save Data here :" + path;

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
                            textObj.text ="File found";
                            byte[] bytes = File.ReadAllBytes(path);
                            textObj.text = "Bytes loaded";
                            if(bytes == null){
                               textObj.text = "Bytes are null";
                            }
                            LoadedMap.AddComponent<MeshFilter>();
                            LoadedMap.AddComponent<MeshRenderer>();

                            material.SetColor("_Color", Color.red);
                            LoadedMap.transform.GetComponent<MeshRenderer>().material = material;

                            textObj.text = "Add material correctly";
                            LoadedMap.transform.GetComponent<MeshFilter>().mesh = MeshSerializer.ReadMesh(bytes);
                            LoadedMap.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                            //LoadedMap.transform.position = new Vector3(1,0,3);
                            LoadedMap.transform.rotation = Quaternion.Euler(0, 0, 0);
                            textObj.text = "hologram componets added";
                            //LoadedMap.AddComponent<MeshCollider>();
                            //LoadedMap.AddComponent<ObjectManipulator>();
                            //LoadedMap.AddComponent<NearInteractionGrabbable>();
                            textObj.text = "Mesh Map Loaded correctly";
                        }                

#else
        Debug.Log("Load on Device is only supported in Universal Windows Applications");
#endif
    }

    public byte[] SaveMeshAsByte()
    {
        combineMesh();
        return MeshSerializer.WriteMesh(rootMesh.mesh, true);
    }

    public void LoadMeshByte(byte[] meshByte)
    {
        if (meshByte != null)
        {
            if (LoadedMap.GetComponent<MeshFilter>() == false)
            {
                LoadedMap.AddComponent<MeshFilter>();
            }
            if (LoadedMap.GetComponent<MeshRenderer>() == false)
            {
                LoadedMap.AddComponent<MeshRenderer>();
            }
            

            material.SetColor("_Color", Color.red);
            LoadedMap.transform.GetComponent<MeshRenderer>().material = material;

            textObj.text = "Add material correctly";
            LoadedMap.transform.GetComponent<MeshFilter>().mesh = MeshSerializer.ReadMesh(meshByte);
            LoadedMap.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            //LoadedMap.transform.position = new Vector3(1,0,3);
            LoadedMap.transform.rotation = Quaternion.Euler(0, 0, 0);
            textObj.text = "hologram componets added";
            //LoadedMap.AddComponent<MeshCollider>();
            //LoadedMap.AddComponent<ObjectManipulator>();
            //LoadedMap.AddComponent<NearInteractionGrabbable>();
            textObj.text = "Mesh Map Loaded correctly";

        }
        else {
            textObj.text = "Bytes are null";
        }
    }
}
