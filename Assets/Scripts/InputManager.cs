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


#if WINDOWS_UWP
    using WindowsStorage = global::Windows.Storage;
#endif

public class InputManager : MonoBehaviour
{
    #region Public Variables
    [SerializeField]
    public TextMeshPro textObj = null;


    #endregion


    #region Member Variables
    // Istance of the map to resize it  
    private GameObject suMinimap = null;
    // Istance of te UI object to resize it as well as the map

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




    private bool toggleMiniMap = false;

    private bool toggleSceenRoot = false;

    #region Toggle Map Features
    public async void enableMeshWorld()
    {
        suManager.RenderWorldMesh = !suManager.RenderWorldMesh;
        await suManager.DisplayDataAsync();
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
    }

    public void increaseRadius()
    {
        suManager.BoundingSphereRadiusInMeters += 5;
        textObj.text = $"{suManager.BoundingSphereRadiusInMeters:F2}";
    }
    public void decreaseRadius()
    {
        suManager.BoundingSphereRadiusInMeters -= 5;
        textObj.text = $"{suManager.BoundingSphereRadiusInMeters:F2}";
    }
    #endregion

    #region UI object resize

    /// <summary>
    /// This function attach all UIObject to the MiniMap hologram in order to represent them in the resized map.
    /// </summary>
    private void addUIobjects()
    {
        textObj.text = "Inside addUIObjects";
        if (SceneObjPlacer.holoObjects.Count != 0)
        {
            textObj.text = "Dentro If";
            for (int i = 0; i < SceneObjPlacer.holoObjects.Count; i++)
            {
                //textObj.text = SceneObjPlacer.holoObjects[i].transform.parent.gameObject.name;
                //objOriginalTransform.Add(SceneObjPlacer.holoObjects[i].transform.parent);
                //textObj.text = "Salvato l'origin farme dell Ui Object";
                //SceneObjPlacer.holoObjects[i].transform.parent = suMinimap.transform;
                
                UIobjects.Add(Instantiate(SceneObjPlacer.holoObjects[i]));              
                UIobjects[i].transform.parent = suMinimap.transform;
                textObj.text = "After parenting";
                SceneObjPlacer.holoObjects[i].SetActive(false);
                textObj.text = "After Set active";
                textObj.text = $"{i:F2}";
            }
        }
    }
   // cheack if I destroy the suMinimap pbject I will destroy all the UIobjects copies as well. 

    private void removeUIObject()
    {
        if(UIobjects.Count > 0)
        {
            for (int i = 0; i < SceneObjPlacer.holoObjects.Count; i++)
            {
                suMinimap.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                textObj.text = "update scale ";
                var UIObjectWorldFrame = SceneRootTrasnMat * UIobjects[i].transform.localPosition;
                SceneObjPlacer.holoObjects[i].transform.position = new Vector3(UIObjectWorldFrame.x, UIObjectWorldFrame.y, UIObjectWorldFrame.z);
                //textObj.text = "update position ";
                //SceneObjPlacer.holoObjects[i].transform.localRotation = UIobjects[i].transform.localRotation;
                textObj.text = "update rotation ";

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
    #endregion

    #region MiniMap Resize

    /// <summary>
    /// Turns the mini map on.
    /// </summary>
    private void MiniMapOn()
    {
        if (suMinimap == null)
        {
            // Update the transform mutrix with the current position and rotation of the SceneRoot frame w.r.t. the global one
            SceneRootTrasnMat = Matrix4x4.TRS(suManager.SceneRoot.transform.position, suManager.SceneRoot.transform.rotation, Vector3.one);
            
            suMinimap = Instantiate(suManager.SceneRoot);
            suMinimap.name = "Minimap";

            //addUIobject();
            addUIobjects();
            suMinimap.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
            suMinimap.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            // add the collider component
            suMinimap.AddComponent<MeshCollider>();

            //Rigidbody rb = suMinimap.AddComponent<Rigidbody>();
            // add the component Object Manipulator Grapable
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
            suMinimap = null;
        }
        // destroy all objects
        suManager.SceneRoot.SetActive(true);
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
    #endregion

    #region Save And Load Scene
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

    #region Share Map
    private void SharedMiniMapOn()
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
    #region Azure Servicies
    /// <summary>
    /// Method to upload the current Mesh of the map on the Azure Cloud as binary file
    /// </summary>
    public async void uploadMeshOnBLOB()
    {
        byte[] mesh = sharedMeshManager.SaveMeshAsByte();
        textObj.text = "Saved mesh correctly";
        await dataManager.UploadBlob(mesh, "meshBLOB");
        textObj.text = "Mesh Saved on Blob";
    }

    /// <summary>
    /// Mthod to download the last mesh uploaded on the Azure BLOB Cloud
    /// </summary>
    public async void downloadMeshFromBLOB()
    {
        var returnedMesh  = dataManager.DownloadBlob("meshBLOB");
        byte[] mesh = await returnedMesh;
        textObj.text = "Download mesh correctly";
        sharedMeshManager.LoadMeshByte(mesh);
        textObj.text = "Loaded mesh correctly";
    }
    #endregion

}
