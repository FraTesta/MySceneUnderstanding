using System;
using System.Collections;
using System.Linq;
using UnityEngine.XR.WSA.Input;
using UnityEngine.Windows.Speech;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using Microsoft.MixedReality.Toolkit.Input;
using MRTK.Tutorials.AzureCloudServices.Scripts.Managers;
using B83.MeshTools;
using System.IO;
using UnityEngine.UI;


#if WINDOWS_UWP
    using WindowsStorage = global::Windows.Storage;
#endif

#if WINDOWS_UWP
using Windows.Storage;
#endif

public class InputManager : MonoBehaviour
{
    #region Public Variables



    #endregion


    #region Member Variables


    private GameObject suMinimap = null;

    private GameObject loadedMapCopy = null;

    private GameObject joinMaps = null;

    private GameObject combinedJoinMaps = null;

    private GameObject ARmarker = null;

    //private GameObject UIobject = null;
    private List<GameObject> UIobjects = new List<GameObject>();

    //Transform Matrix from the SceneRoot frame w.r.t the WorldFrame
    private Matrix4x4 SceneRootTrasnMat;

    #endregion

    #region Setting parameters

    [Header("Scene Understanding Scripts")]
    [Tooltip("Reference to the main scene understanding manager for default commands.")]
    [SerializeField]
    private MySUManager suManager;
    [Tooltip("Reference to the scene object placer to handle the placed object")]
    [SerializeField]
    private SceneUnderstandingObjectPlacer SceneObjPlacer;


    [Header("Cloud Storage Scripts")]
    [Tooltip("Reference to the DataManager to handle the Azure Servicies")]
    [SerializeField]
    private DataManager dataManager;
    [Tooltip("Reference to the DataManager to handle the Azure Servicies")]
    [SerializeField]
    private SharedMeshManager sharedMeshManager;

    [Header("Cloud Spatial Anchor Script")]
    [Tooltip("Reference to the AzureModule to handle the Azure Spatial Anchors")]
    [SerializeField]
    private AnchorModuleScript AzureModule;
    [Tooltip("Find nearby  anchor")]
    [SerializeField]
    private bool findNearSA = false;


    [Header("Navigation Script")]
    [Tooltip("agent controller module")]
    [SerializeField]
    private myAgentController agentController = null;



    [Header("Prefab and Materials")]
    [Tooltip("Prefab of the AR marker to place")]
    [SerializeField]
    private GameObject objToPlaceRef;
    [Tooltip("Prefab of the anchor to place")]
    [SerializeField]
    private GameObject anchorPlaceRef;
    [Tooltip("User Prefab")]
    [SerializeField]
    private GameObject userPerfab = null;
    [Tooltip("Minimap material")]
    [SerializeField]
    private Material MinimapMaterial = null;
    [Tooltip("Joined map material")]
    [SerializeField]
    private Material joinedMapMaterial = null;

    [Header("Tests")]
    [Tooltip("Test script")]
    [SerializeField]
    private performanceTest testScript = null;

    #endregion

    string path = null;

    private bool toggleMiniMap = false;

    private bool toggleSceenRoot = false;

    private bool toggleRenderNav = false;

    #region test variables

    string anchorPlaceFile = "anchorPlace.txt";
    string anchorFindFile = "anchorFind.txt";
    string minimapFile = "minimap.txt";
    string uploadBlobFile = "uploadBLOB.txt";
    string downloadBLobFile = "downloadBLOB.txt";
    string navigationFile = "navigation.txt";

    private void Awake()
    {
        path = Application.persistentDataPath;
    }
    #endregion

    /* private void Update()
     {
         updateCurrentMCSA();
     }*/

    #region Toggle Map Features
    public async void updateMesh()
    {
        await suManager.DisplayDataAsync();
        if(testScript != null)
            testScript.updateVertices();
        Debug.Log("MESH UPDATED");
    }
    public async void enableMeshWorld()
    {
        suManager.RenderWorldMesh = !suManager.RenderWorldMesh;
        await suManager.DisplayDataAsync();
        Debug.Log("MESH WORLD TOGGLED ");
    }

    public async void togglePlatform()
    {
        suManager.RenderPlatformSceneObjects = !suManager.RenderPlatformSceneObjects;
        await suManager.DisplayDataAsync();
    }

    public async void toggleQuads()
    {
        suManager.RenderSceneObjects = !suManager.RenderSceneObjects;
        await suManager.DisplayDataAsync();
        Debug.Log("QUAD TOGGLED ");
    }

    public void toggleRenderNavigation()
    {
        toggleRenderNav = !toggleRenderNav;
        if (toggleRenderNav)
        {
            RenderForNavigationOn();
        }
        else {
            RenderForNavigationOff();
        }
    }
    public async void RenderForNavigationOn()
    {
        suManager.RenderWorldMesh = true;
        suManager.RenderSceneObjects = true;
        suManager.renderForNavigation = true;
        await suManager.DisplayDataAsync();

    }
    public async void RenderForNavigationOff()
    {
        suManager.RenderWorldMesh = false;
        suManager.RenderSceneObjects = false;
        suManager.renderForNavigation = false;
        await suManager.DisplayDataAsync();
    }

    public void increaseRadius()
    {
        suManager.BoundingSphereRadiusInMeters += 10;
        Debug.Log("SPHERE RADISU = " + suManager.BoundingSphereRadiusInMeters);

    }
    public void decreaseRadius()
    {
        suManager.BoundingSphereRadiusInMeters -= 10;
        Debug.Log("SPHERE RADISU = " + suManager.BoundingSphereRadiusInMeters);
    }
    #endregion

     #region UI object resize

    /// <summary>
    /// This function attach all UIObject to the MiniMap hologram in order to represent them in the resized map.
    /// </summary>
    private void addUIobjects()
    {
        if (SceneObjPlacer.holoObjects.Count != 0)
        {

            for (int i = 0; i < SceneObjPlacer.holoObjects.Count; i++)
            {
                UIobjects.Add(Instantiate(SceneObjPlacer.holoObjects[i]));
                UIobjects[i].name = "anchor" + i;
                UIobjects[i].transform.parent = suMinimap.transform;
                SceneObjPlacer.holoObjects[i].SetActive(false);
                Debug.Log("Placed Anchor" + i);
            }
        }
    }
    // cheack if I destroy the suMinimap pbject I will destroy all the UIobjects copies as well. 

    private void removeUIObject()
    {
        if (UIobjects.Count > 0)
        {
            for (int i = 0; i < SceneObjPlacer.holoObjects.Count; i++)
            {
                suMinimap.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
             
                var UIObjectWorldFrame = SceneRootTrasnMat * UIobjects[i].transform.localPosition;
                SceneObjPlacer.holoObjects[i].transform.position = new Vector3(UIObjectWorldFrame.x, UIObjectWorldFrame.y, UIObjectWorldFrame.z);
                //textObj.text = "update position ";
                //SceneObjPlacer.holoObjects[i].transform.localRotation = UIobjects[i].transform.localRotation;
               

                SceneObjPlacer.holoObjects[i].SetActive(true);
                DestroyImmediate(UIobjects[i]);
                //SceneObjPlacer.holoObjects[i].transform.parent = null;
                //textObj.text = "parent null";
                //SceneObjPlacer.holoObjects[i].transform.localScale = new Vector3(10.0f, 10.0f, 10.0f);
                //textObj.text = "Aggiorate le dimensioni degli UI Obj";
            }
            UIobjects.Clear();
        }


    }

    private void UserPose()
    {
        GameObject userPose = Instantiate(userPerfab, Vector3.zero, Quaternion.identity);
        userPose.transform.position = Camera.main.transform.position;
        userPose.transform.rotation = Camera.main.transform.rotation;
        userPose.transform.rotation = Quaternion.Euler(-90, userPose.transform.rotation.eulerAngles.y, userPose.transform.rotation.eulerAngles.z);
        userPose.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        userPose.transform.parent = suMinimap.transform;
    }
    #endregion

    #region MiniMap Resize

    /// <summary>
    /// Turns the mini map on.
    /// </summary>
    private async void MiniMapOn()
    {
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        // If the world mesh is turned of, it will be turned on to visualize the minimap as well
        if (!suManager.RenderWorldMesh)
            suManager.RenderWorldMesh = true;

        


        if (suMinimap == null)
        {

            // Update the transform mutrix with the current position and rotation of the SceneRoot frame w.r.t. the global one
            SceneRootTrasnMat = Matrix4x4.TRS(suManager.SceneRoot.transform.position, suManager.SceneRoot.transform.rotation, Vector3.one);

            if (suManager.AddColliders == false)
            {
                suManager.AddColliders = true;
                Debug.Log("Collider Activated");
                await suManager.DisplayDataAsync();

                suMinimap = Instantiate(suManager.SceneRoot);
                suManager.AddColliders = false;
                Debug.Log("Collider Deactivated");
            }
            else {
                suMinimap = Instantiate(suManager.SceneRoot);
            }

            suMinimap.name = "suMinimap";



            // fare copia dell'ancora 
            // rendere suMinimap figlia e unire in un unico game object 
            // elminare su minimap
            // poi con nella joinMaps function posso usare questo medodo prima e unire la mappa scaricata alla mappa appena generata perchè il suo frame sarà quello dell'ancora 


            if (GameObject.Find("LoadedMap") != null) {
                Debug.Log("multiple minimap...");
                multipleMinimap();
                    }
            else {

                // add resized UI objects
                addUIobjects();
                // add the user pose
                UserPose();
                //enableMiniPath();

                // spawn the minimap in front of the user view
                suMinimap.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
                suMinimap.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);

                // add the collider component
                //suMinimap.AddComponent<MeshCollider>();

                suMinimap.AddComponent<ObjectManipulator>();

                suMinimap.AddComponent<NearInteractionGrabbable>();

                suManager.SceneRoot.SetActive(false);
            }
        }
        watch.Stop();
        saveData(minimapFile, watch.ElapsedMilliseconds);
    }

   

    /// <summary>
    /// Turns the mini map off.
    /// </summary>
    private void MiniMapOff()
    {
        if (suMinimap != null)
        {
            removeUIObject();
            DestroyImmediate(suMinimap);
            //suMinimap.transform.parent = null;
            suMinimap = null;
        }
        if (GameObject.Find("LoadedMap") != null)
        {

            DestroyImmediate(loadedMapCopy);
            GameObject.Find("LoadedMap").SetActive(true); 
            GameObject.Find("LoadedMap").transform.parent = GameObject.Find("CloudDataManager").transform;
            //DestroyImmediate(GameObject.Find("joinMaps"));
            loadedMapCopy = null;
            Debug.Log("LoadedMap reloaded");

        }
        // destroy all objects
        suManager.SceneRoot.SetActive(true);
        Debug.Log("Mesh active");
    }


    /// <summary>
    /// Toggles the mini map on and off.
    /// </summary>
    public void MiniMapToggle()
    {
        toggleMiniMap = !toggleMiniMap;
        if (toggleMiniMap)
        {
            MiniMapOn();
            //Resize UISceneObject 
            //resizeUIObject();
        }
        else
        {
            MiniMapOff();
            //unresizeUIObject();
        }
    }

    public void resizeLoadedMap()
    {
        GameObject.Find("LoadedMap").transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
    }
    #endregion

    #region Save And Load Scene Locally (non più utile )
    /// <summary>
    /// Save the current scene on the device as binary file
    /// </summary>
    public void SaveData()
    {
        var bytes = suManager.SaveBytesToDiskAsync();
        //var objs = suManager.SaveObjsToDiskAsync();
        //textObj.text = "Save Map";
    }
    /// <summary>
    /// Load a scene from the device 
    /// </summary>
    public void LoadData()
    {
        var bytes = suManager.LoadByteFromDiskAsync();
        //textObj.text = "Map Loaded";
    }

    #endregion

    #region Utility Function
    /// <summary>
    /// To toggle the SceneRoot the visibility GameObject just for testing
    /// </summary>
    public void toggoleSceneRootMesh()
    {
        toggleSceenRoot = !toggleSceenRoot;
        suManager.SceneRoot.SetActive(toggleSceenRoot);
    }
    #endregion

    #region Share Map Data BLOB
    /// <summary>
    /// Method to upload the current Map data (ARdata and Mesh) (attached to the SceneRoot Game Object defined in the SharedMapManager script) on the Azure Cloud as binary file
    /// </summary>
    public async void uploadMapDataOnBLOB()
    {
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();
        string containerName = AzureModule.currentAzureAnchorID;
        if (containerName == "")
        {
            Debug.LogError("The anchor ID is null, No BLOB container can be found");
            return;
        }
            
        await dataManager.setBLOBContainer(containerName);
        Debug.Log("Start uploading BLOB data\n");
        byte[] ARdata = sharedMeshManager.ARDataAsByte(suManager.SceneRoot, GameObject.Find("CloudDataManager"));
        await dataManager.UploadBlob(ARdata, "AR data BLOB");
        Debug.Log("AR data uploaded on BLOB storage successfully");
        byte[] mesh = sharedMeshManager.combineMeshAsByte(suManager.SceneRoot, GameObject.Find("CloudDataManager"));
        await dataManager.UploadBlob(mesh, "mesh BLOB");
        Debug.Log("Mesh uploaded on BLOB storage successfully");

        watch.Stop();
        saveData(uploadBlobFile, watch.ElapsedMilliseconds);

    }

    /*public async void uploadMapDataOnBLOB()
    {
        Debug.Log("START UPLOADING MAP");
        byte[] meshByte = sharedMeshManager.combineMeshAsByte(suManager.SceneRoot);
        if (meshByte == null)
        {
            Debug.LogError("Serialized mesh is null"); 
        }
        else { 
            Debug.Log("Mesh serialized properly"); 
        }
        
        await dataManager.UploadBlob(meshByte, "mesh BLOB");
        Debug.Log("Mesh uploaded on BLOB storage correctly");
    }*/


    /// <summary>
    /// Method to download the last mesh and its relative AR data uploaded on the Azure BLOB Cloud with the name specified below. 
    /// At the end it will join the current map and the dowloaded one in the Anchor (CloudDataManager) frame location.
    /// </summary>
    public async void downloadMapDataFromBLOB()
    {
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();
        string containerName = AzureModule.currentAzureAnchorID;
        if (containerName == "")
        {
            Debug.LogError("The anchor ID is null, No BLOB container can be found");
            return;
        }
        await dataManager.setBLOBContainer(containerName);
        Debug.Log("Start downloading BLOB data\n");
        var returnedARData = dataManager.DownloadBlob("AR data BLOB");
        byte[] ARDataByte = await returnedARData;
        if (ARDataByte == null)
        {
            Debug.Log("ERROR: AR data downloaded wrong!!!");
        }
        else {
            Debug.Log("AR data downloaded properly");
        }
        // download mesh 
        var returnedMesh = dataManager.DownloadBlob("mesh BLOB");
        byte[] meshByte = await returnedMesh;
        if (meshByte == null)
        {
            Debug.Log("ERROR: mesh data downloaded wrong!!!");
        }
        else
        {
            Debug.Log("Mesh data downloaded properly");
        }
        sharedMeshManager.LocateSubMap(ARDataByte, meshByte, GameObject.Find("CloudDataManager"));
        Debug.Log("SUBMAP LOCATED SUCCESSFULLY \n");
        watch.Stop();
        saveData(downloadBLobFile, watch.ElapsedMilliseconds);
    }

    /*public async void downloadMapDataFromBLOB()
    {
        Debug.Log("Start download BLOB data\n");
        // first I place the map frame

        var returnedMesh = dataManager.DownloadBlob("mesh BLOB");
        byte[] mesh = await returnedMesh;
        Debug.Log("Map Mesh downloaded correctly\n");
        sharedMeshManager.locateMap(mesh);
        Debug.Log("MAP LOCATED");
    }*/
    #endregion
     

    #region Azure Spatial Anchor

    /// <summary>
    /// Initialize the Azure clous spatial anchor session and share the main anchor (CloudSpatialAnchor game object).
    /// </summary>
    public async void shareMCSA()
    { 
        await AzureModule.StartAzureSession();
        await AzureModule.CreateAzureAnchor(GameObject.Find("CloudDataManager"));
        Debug.Log("Main Anchor created");
        //AzureModule.ShareAzureAnchorIdToNetwork();
        AzureModule.SaveAzureAnchorIdToDisk();
    }
    /// <summary>
    /// Share the second anchor called Anchor2, which should instanciated at the application start
    /// </summary>
    public async void shareNewAnchor()
    {
        Vector3 frontCameraPosition = Camera.main.transform.position + Camera.main.transform.forward;
        GameObject placedAnchor = Instantiate<GameObject>(anchorPlaceRef, frontCameraPosition, Quaternion.Euler(new Vector3(-90, 0, 0)));
        
        placedAnchor.name = "anchor";
        placedAnchor.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        await AzureModule.CreateAzureAnchor(placedAnchor);
        Debug.Log("Second anchor shared");
        AzureModule.ShareAzureAnchorIdToNetwork();
    }

    /// <summary>
    /// Search the anchor with the ID saved on the cloud. It initialize a azure session as well.
    /// </summary>
    public async void findAnchor()
    {

        //AzureModule.GetAzureAnchorIdFromNetwork();
        AzureModule.GetAzureAnchorIdFromDisk();
        await AzureModule.StartAzureSession();
        if (AzureModule.currentAzureAnchorID != null || AzureModule.currentAzureAnchorID != "")
        {

            AzureModule.FindAzureAnchor();
        }
        else {
            Debug.LogError("ERROR no anchor ID found");
        }

    }

    public void findNearbyAnchors()
    {

        if (findNearSA == true && AzureModule.CurrentCloudAnchor != null)
            AzureModule.LocateNearByAnchors(AzureModule.CurrentCloudAnchor);

    }



    #endregion

    #region multiple maps handlers
    void combineLocalMinimap()
    {
        // combine all map component of the sceneRoot (local map)
        Mesh mesh = sharedMeshManager.combineMesh(suMinimap, GameObject.Find("CloudDataManager"));

        if (suMinimap.GetComponent<MeshFilter>() == false)
        {
            suMinimap.AddComponent<MeshFilter>();
            Debug.Log("MeshFilter added");
        }
        if (suMinimap.GetComponent<MeshRenderer>() == false)
        {
            suMinimap.AddComponent<MeshRenderer>();
            Debug.Log("MeshRender added");
        }

        MinimapMaterial.SetColor("_Color", Color.blue);
        suMinimap.transform.GetComponent<MeshRenderer>().material = MinimapMaterial;
        Debug.Log("Mesh Render set");

        if (mesh == null)
            Debug.LogError("The combined mesh is null!!!");

        suMinimap.transform.GetComponent<MeshFilter>().mesh = mesh;
        Debug.Log("Minimap located successfully");
    }
    /// <summary>
    /// Render a minimap showing also the downloaded map joined to the local one
    /// </summary>
    void multipleMinimap()
    {
        suManager.SceneRoot.transform.parent = GameObject.Find("CloudDataManager").transform;
        Vector3 sceneRootPos = suManager.SceneRoot.transform.localPosition;
        Quaternion sceneRootRot = suManager.SceneRoot.transform.localRotation;
        suManager.SceneRoot.transform.parent = null;

        combineLocalMinimap();

        joinMaps = new GameObject("joinMaps");
        joinMaps.transform.position = GameObject.Find("CloudDataManager").transform.position;
        joinMaps.transform.rotation = GameObject.Find("CloudDataManager").transform.rotation;

        //GameObject.Find("LoadedMap").transform.parent = joinMaps.transform;
        // set the LoadedMap as child of the joinMap
        loadedMapCopy = Instantiate(GameObject.Find("LoadedMap"));
        loadedMapCopy.transform.parent = GameObject.Find("CloudDataManager").transform;
        loadedMapCopy.transform.localPosition = GameObject.Find("LoadedMap").transform.localPosition;
        loadedMapCopy.transform.localRotation = GameObject.Find("LoadedMap").transform.localRotation;
        loadedMapCopy.transform.localPosition = GameObject.Find("LoadedMap").transform.localPosition;
        loadedMapCopy.transform.localScale = GameObject.Find("LoadedMap").transform.localScale;
        Debug.Log("LoadedMap copy instanciated");

        loadedMapCopy.transform.parent = joinMaps.transform;
        
        Debug.Log("Parenthood set");



        suMinimap.transform.parent = joinMaps.transform;
        //joinMaps.transform.parent = null;
        joinMaps.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);

        joinMaps.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
        joinMaps.AddComponent<ObjectManipulator>();

        joinMaps.AddComponent<NearInteractionGrabbable>();

        GameObject.Find("LoadedMap").SetActive(false);
        suManager.SceneRoot.SetActive(false);
    }

    public void combineSubMaps()
    {
        MiniMapOn();

        Mesh mesh = sharedMeshManager.combineMesh(GameObject.Find("joinMaps"), GameObject.Find("CloudDataManager"));

        if (mesh == null)
            Debug.LogError("The combined mesh is null!!!");

        combinedJoinMaps = new GameObject("combinedJoinMaps");


        if (combinedJoinMaps.GetComponent<MeshFilter>() == false)
        {
            combinedJoinMaps.AddComponent<MeshFilter>();
            Debug.Log("MeshFilter added");
        }
        if (combinedJoinMaps.GetComponent<MeshRenderer>() == false)
        {
            combinedJoinMaps.AddComponent<MeshRenderer>();
            Debug.Log("MeshRender added");
        }

        combinedJoinMaps.transform.GetComponent<MeshRenderer>().material = joinedMapMaterial;
        Debug.Log("material applied");
        combinedJoinMaps.transform.GetComponent<MeshFilter>().mesh = mesh;
        Debug.Log("mesh applied");

        combinedJoinMaps.transform.position = Camera.main.transform.position + Camera.main.transform.forward;

        joinMaps.AddComponent<MeshCollider>();
        joinMaps.AddComponent<ObjectManipulator>();
        joinMaps.AddComponent<NearInteractionGrabbable>();
    }

    #endregion

   

    #region Navigation

    public void enableNavigation()
    {
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();
        agentController.initNavigation();
        Debug.Log("Initialization Finished");
        agentController.setDestination(agentController.Target.transform.position);
        Debug.Log("Navigation Started Properly");
        watch.Stop();
        saveData(navigationFile, watch.ElapsedMilliseconds);
    }

    public void enableMiniPath()
    {
        GameObject minimPath = Instantiate(GameObject.Find("InputManager"));
        Debug.Log("Mini path instatnciated");
        minimPath.transform.parent = suMinimap.transform;
        Debug.Log("path parent set");
        
    }
    #endregion

    private void updateCurrentMCSA()
    {
        
        if (GameObject.FindGameObjectsWithTag("anchor") != null)
        {
            float distanceMCSA = Vector3.Distance(GameObject.Find("CloudDataManager").transform.position, GameObject.Find("Main Camera").transform.position);
            float distance;
            foreach (GameObject g in GameObject.FindGameObjectsWithTag("anchor"))
            {
                distance = Vector3.Distance(g.transform.position, GameObject.Find("Main Camera").transform.position);
                if (distance < distanceMCSA)
                {
                    // aggiornare currentAnchor
                    // invertire game object
                    AzureModule.RemoveLocalAnchor(GameObject.Find("CloudDataManager"));
                    Vector3 MCSAposition = GameObject.Find("CloudDataManager").transform.position;
                    GameObject.Find("CloudDataManager").transform.position = g.transform.position;

                    AzureModule.RemoveLocalAnchor(g);
                    g.transform.position = MCSAposition;
                    Debug.Log("MCSA updated");

                }
            }
        }
       
    }

    #region join local maps Legacy
    /// <summary>
    /// make a copy of the current Scene (map), combine its mesh components into a single one and anchor it to the current spactial anchor 
    /// </summary>
    /*private void mapAnchorReference()
    {
        // If the world mesh is turned of, it will be turned on to visualize the minimap as well
        if (suManager.RenderWorldMesh == false)
        {
            suManager.RenderWorldMesh = true;
            Debug.Log("\n World mesh enabled from map");
        }
        anchorMap = new GameObject("anchorMap");


        // Update the transform mutrix with the current position and rotation of the SceneRoot frame w.r.t. the global one
        SceneRootTrasnMat = Matrix4x4.TRS(suManager.SceneRoot.transform.position, suManager.SceneRoot.transform.rotation, Vector3.one);

        // get relative position and rotation of SceneRoot (local map) w.r.t. the current main anchor
        suManager.SceneRoot.transform.parent = GameObject.Find("CloudDataManager").transform;
        Vector3 sceneRootPos = suManager.SceneRoot.transform.localPosition;
        Quaternion sceneRootRot = suManager.SceneRoot.transform.localRotation;
        suManager.SceneRoot.transform.parent = null;

        // combine all map component of the sceneRoot (local map)
        Mesh mesh = sharedMeshManager.combineMesh(suManager.SceneRoot, GameObject.Find("CloudDataManager"));


        if (anchorMap.GetComponent<MeshFilter>() == false)
        {
            anchorMap.AddComponent<MeshFilter>();
            Debug.Log("MeshFilter added");
        }
        if (anchorMap.GetComponent<MeshRenderer>() == false)
        {
            anchorMap.AddComponent<MeshRenderer>();
            Debug.Log("MeshRender added");
        }

        MinimapMaterial.SetColor("_Color", Color.blue);
        anchorMap.transform.GetComponent<MeshRenderer>().material = MinimapMaterial;
        Debug.Log("Mesh Render set");

        if (mesh == null)
            Debug.LogError("The combined mesh is null!!!");


        anchorMap.transform.parent = GameObject.Find("CloudDataManager").transform;
        //anchorMap.transform.localPosition = Vector3.zero;
        //anchorMap.transform.localRotation = Quaternion.identity;
        anchorMap.transform.localPosition = sceneRootPos;
        anchorMap.transform.localRotation = sceneRootRot;
        anchorMap.transform.GetComponent<MeshFilter>().mesh = mesh;

        Debug.Log("Minimap located successfully");

        suManager.SceneRoot.SetActive(false);
    }*/

    /// <summary>
    /// To join the downloaded map (LoadedMap GameObject) with the minimap (suMiniMap )
    /// </summary>
   /* private void joinMap()
    {

        joinMaps = new GameObject("joinMaps");
        joinMaps.transform.position = GameObject.Find("CloudDataManager").transform.position;
        joinMaps.transform.rotation = GameObject.Find("CloudDataManager").transform.rotation;

        // set the LoadedMap as child of the joinMap
        GameObject.Find("LoadedMap").transform.parent = joinMaps.transform;

        if (anchorMap == null)
            mapAnchorReference();
        anchorMap.transform.parent = joinMaps.transform;

        joinMaps.transform.parent = null;
        joinMaps.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);

    }

    /// <summary>
    /// Combine the previusly joined maps
    /// </summary>
    private void combineSubMapsLegacy()
    {
        GameObject combinedJoinMaps = new GameObject("combinedJoinMaps");

        // combine the mesh of the local map and the downloaded one 
        Mesh mesh = sharedMeshManager.combineMesh(joinMaps, GameObject.Find("CloudDataManager"));

        if (combinedJoinMaps.GetComponent<MeshFilter>() == false)
        {
            combinedJoinMaps.AddComponent<MeshFilter>();
            Debug.Log("MeshFilter added");
        }
        if (combinedJoinMaps.GetComponent<MeshRenderer>() == false)
        {
            combinedJoinMaps.AddComponent<MeshRenderer>();
            Debug.Log("MeshRender added");
        }

        combinedJoinMaps.transform.GetComponent<MeshRenderer>().material = joinedMapMaterial;
        Debug.Log("Render material set");

        if (mesh == null)
            Debug.LogError("The combined mesh is null!!!");

        combinedJoinMaps.transform.GetComponent<MeshFilter>().mesh = mesh;
        combinedJoinMaps.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        combinedJoinMaps.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
        DestroyImmediate(joinMaps);
        DestroyImmediate(anchorMap);
        combinedJoinMaps.AddComponent<MeshCollider>();
        combinedJoinMaps.AddComponent<ObjectManipulator>();
        combinedJoinMaps.AddComponent<NearInteractionGrabbable>();
    }*/
    #endregion

    void saveData(string fileName, float timeData)
    {

#if WINDOWS_UWP
                    StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                    path = storageFolder.Path.Replace('\\', '/') + "/";
#endif

        string filePath = Path.Combine(path, fileName);

        if (fileName == navigationFile)
        {
            float distance = Vector3.Distance(GameObject.Find("Main Camera").transform.position, GameObject.Find("CloudDataManager").transform.position);
            File.AppendAllText(filePath, timeData.ToString() + "," + distance.ToString() + Environment.NewLine + ".");
        }
        else {
            int vertex = countVertices();
            File.AppendAllText(filePath, timeData.ToString() + "," + vertex.ToString() + Environment.NewLine +  ".");
        }




        Debug.Log("Data time saved");
    }

    public int countVertices()
    {
        int totalVertex = 0;
        MeshFilter[] meshFilters = suManager.SceneRoot.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter mf in meshFilters)
        {
            totalVertex += mf.mesh.vertexCount;
        }
        Debug.Log("number of vertices counted");
        return totalVertex;
    }

}
