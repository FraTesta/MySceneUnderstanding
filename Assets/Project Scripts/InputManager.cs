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


#if WINDOWS_UWP
    using WindowsStorage = global::Windows.Storage;
#endif

public class InputManager : MonoBehaviour
{
    #region Public Variables



    #endregion


    #region Member Variables
    

    private GameObject suMinimap = null;

    private GameObject anchorMap = null;

    private GameObject ARmarker = null;

    //private GameObject UIobject = null;
    private List<GameObject> UIobjects = new List<GameObject>();

    //Transform Matrix from the SceneRoot frame w.r.t the WorldFrame
    private Matrix4x4 SceneRootTrasnMat;

    #endregion 

    [Tooltip("Reference to the main scene understanding manager for default commands.")]
    [SerializeField]
    private MySUManager suManager;

    [Tooltip("Reference to the scene object placer to handle the placed object")]
    [SerializeField]
    private SceneUnderstandingObjectPlacer SceneObjPlacer;

    [Tooltip("Reference to the DataManager to handle the Azure Servicies")]
    [SerializeField]
    private DataManager dataManager;

    [Tooltip("Reference to the DataManager to handle the Azure Servicies")]
    [SerializeField]
    private SharedMeshManager sharedMeshManager;

    [Tooltip("Reference to the AzureModule to handle the Azure Spatial Anchors")]
    [SerializeField]
    private AnchorModuleScript AzureModule;

    [Tooltip("Prefab of the AR marker to place")]
    [SerializeField]
    private GameObject objToPlaceRef;

    [Tooltip("Minimap material")]
    [SerializeField]
    private Material MinimapMaterial = null;

    [Tooltip("Joined map material")]
    [SerializeField]
    private Material joinedMapMaterial = null;

    [Tooltip("agent controller module")]
    [SerializeField]
    private myAgentController agentController = null;

    [Tooltip("User Prefab")]
    [SerializeField]
    private GameObject userPerfab = null;

    private bool toggleMiniMap = false;

    private bool toggleSceenRoot = false;

    private bool toggleRenderNav = false;

    #region Toggle Map Features
    public async void updateMesh()
    {
        await suManager.DisplayDataAsync();
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

        // If the world mesh is turned of, it will be turned on to visualize the minimap as well
        if (!suManager.RenderWorldMesh)
            suManager.RenderWorldMesh = true;

        if (suMinimap == null)
        {

            // Update the transform mutrix with the current position and rotation of the SceneRoot frame w.r.t. the global one
            //SceneRootTrasnMat = Matrix4x4.TRS(suManager.SceneRoot.transform.position, suManager.SceneRoot.transform.rotation, Vector3.one);

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

    #region Share Map (forse inutile)
   /*private void SharedMiniMapOn()
    {
        if (suMinimap == null)
        {
            suMinimap = Instantiate(suManager.ParallelSceneRoot);
            suMinimap.name = "SharedMinimap";

            //addUIobject();
            //addUIobjects();
            suMinimap.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
            suMinimap.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            // add the collider component
            suMinimap.AddComponent<MeshCollider>();

            //Rigidbody rb = suMinimap.AddComponent<Rigidbody>();
            // add the component Object Manipulator Grapable
            suMinimap.AddComponent<ObjectManipulator>();
            suMinimap.AddComponent<NearInteractionGrabbable>();
            suManager.SceneRoot.SetActive(false);
            suManager.ParallelSceneRoot.SetActive(false);

        }
    }

    /// <summary>
    /// Turns the mini map off.
    /// </summary>
    private void SharedMiniMapOff()
    {
        if (suMinimap != null)
        {
            //removeUIObject();
            DestroyImmediate(suMinimap);
            suMinimap = null;
        }
        // destroy all objects
        //unresizeUIObject();
        suManager.SceneRoot.SetActive(true);
        suManager.ParallelSceneRoot.SetActive(true);
    }
    public void SharedMiniMapToggle()
    {
        toggleMiniMap = !toggleMiniMap;
        if (toggleMiniMap)
        {
            SharedMiniMapOn();
            //Resize UISceneObject 
            //resizeUIObject();
        }
        else
        {
            SharedMiniMapOff();
            //unresizeUIObject();
        }
    }*/
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
    public async void shareAnchor()
    {
        await AzureModule.StartAzureSession();
        await AzureModule.CreateAzureAnchor(GameObject.Find("CloudDataManager"));
        Debug.Log("Main Anchor created");
        AzureModule.ShareAzureAnchorIdToNetwork();
        
    }
    /// <summary>
    /// Share the second anchor called Anchor2, which should instanciated at the application start
    /// </summary>
    public async void shareAnchor2()
    {
        await AzureModule.CreateAzureAnchor(GameObject.Find("Anchor2"));
        Debug.Log("Second anchor shared");
        AzureModule.ShareAzureAnchorIdToNetwork();
    }

    /// <summary>
    /// Search the anchor with the ID saved on the cloud. It initialize a azure session as well.
    /// </summary>
    public async void findAnchor()
    {
        AzureModule.GetAzureAnchorIdFromNetwork();
        await AzureModule.StartAzureSession();
        AzureModule.FindAzureAnchor();
    }

    /// <summary>
    /// To find nearby anchors after locating the main one. 
    /// </summary>
    public void findSecondAnchor()
    {
        if (AzureModule.CurrentCloudAnchor != null)
        {
            AzureModule.LocateNearByAnchors(AzureModule.CurrentCloudAnchor);
        }
        else { Debug.LogError("Current Cloud Anchor is Null !!!"); }
    }


    #endregion


    #region join local maps
    /// <summary>
    /// make a copy of the current Scene (map), combine its mesh components into a single one and anchor it to the current spactial anchor 
    /// </summary>

    private void mapAnchorReference()
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

        suManager.SceneRoot.transform.parent = GameObject.Find("CloudDataManager").transform;
        Vector3 sceneRootPos = suManager.SceneRoot.transform.localPosition;
        Quaternion sceneRootRot = suManager.SceneRoot.transform.localRotation;
        suManager.SceneRoot.transform.parent = null;

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
        anchorMap.transform.localPosition = Vector3.zero;
        anchorMap.transform.localRotation = Quaternion.identity;
        anchorMap.transform.GetComponent<MeshFilter>().mesh = mesh;
        Debug.Log("Cobined mesh added");
        anchorMap.transform.localPosition = sceneRootPos;
        anchorMap.transform.localRotation = sceneRootRot;
        Debug.Log("Minimap located successfully");

        suManager.SceneRoot.SetActive(false);
    }

    /// <summary>
    /// To join the downloaded map (LoadedMap GameObject) with the minimap (suMiniMap )
    /// </summary>
    public void joinMap()
    {
        //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //cube.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        GameObject joinMaps = new GameObject("joinMaps");
        joinMaps.transform.position = GameObject.Find("CloudDataManager").transform.position;
        joinMaps.transform.rotation = GameObject.Find("CloudDataManager").transform.rotation;
        //cube.transform.localPosition = Vector3.zero;

        Debug.Log("Cube in achor position");

        GameObject.Find("LoadedMap").transform.parent = joinMaps.transform;
        Debug.Log("Loaded map anchired to the cube");

        if (anchorMap == null)
            mapAnchorReference(); 
        anchorMap.transform.parent = joinMaps.transform;

        joinMaps.transform.parent = null;
        joinMaps.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);        

    }

    /// <summary>
    /// Combine the previusly joined maps
    /// </summary>
    public void combineSubMaps()
    {
        GameObject combinedJoinMaps = new GameObject("combinedJoinMaps"); 
        GameObject joinMaps = GameObject.Find("joinMaps");

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
        Debug.Log("Mesh Render set");

        if (mesh == null)
            Debug.LogError("The combined mesh is null!!!");

        combinedJoinMaps.transform.GetComponent<MeshFilter>().mesh = mesh;
        combinedJoinMaps.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        combinedJoinMaps.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
        DestroyImmediate(joinMaps);
        combinedJoinMaps.AddComponent<MeshCollider>();
        combinedJoinMaps.AddComponent<ObjectManipulator>();
        combinedJoinMaps.AddComponent<NearInteractionGrabbable>();
    }
    #endregion

    #region Navigation

    public void enableNavigation()
    {
        agentController.initNavigation();
        Debug.Log("Initialization Finished");
        agentController.setDestination(agentController.Target.transform.position);
        Debug.Log("Navigation Started Properly");
    }

    public void enableMiniPath()
    {
        GameObject minimPath = Instantiate(GameObject.Find("InputManager"));
        Debug.Log("Mini path instatnciated");
        minimPath.transform.parent = suMinimap.transform;
        Debug.Log("path parent set");
        
    }
    #endregion
}
