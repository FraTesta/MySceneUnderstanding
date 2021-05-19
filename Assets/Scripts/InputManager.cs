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
    #endregion 

    [Tooltip("Reference to the main scene understanding manager for default commands.")]
    [SerializeField]
    private MySUManager suManager;

    [Tooltip("Reference to the scene object placer to handle the placed object")]
    [SerializeField]
    private SceneUnderstandingObjectPlacer SceneObjPlacer;

    [Tooltip("Reference to the Labeler Component for SU Scene")]
    [SerializeField]
    private SceneUnderstandingLabeler labeler = null;

    private bool toggleMiniMap = false;

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
    #endregion


    #region UI object resize
    /*private void resizeUIObject()
    {
        if (UIobject == null)
        {
            UIobject = Instantiate(SceneObjPlacer.objectPlaced);
            // riportare la posizione dell'oggetto rispetto a questo frame.

            // setto il teso
            //UIobject.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
            UIobject.transform.position = new Vector3(0, 0, 0);
            UIobject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            SceneObjPlacer.objectPlaced.SetActive(false);
        }
    }*/

    /*private void addUIobject()
    {
        if (SceneObjPlacer.objectPlaced != null)
        {
            UIobject = Instantiate(SceneObjPlacer.objectPlaced);
            UIobject.transform.parent = suMinimap.transform;
            SceneObjPlacer.objectPlaced.SetActive(false);
        }
    }*/

    // Actually this function make a copy of every UIobject placed and set the SceenRoot frame as their own reference frame.
    // So basically it attaches the UIObject copies to a scene map hologram in order to get just one hologram for all.
    private void addUIobjects()
    {
        textObj.text = "Before If";
        if (SceneObjPlacer.holoObjects.Count != 0)
        {          
            textObj.text = "After If ";
            for (int i = 0; i < SceneObjPlacer.holoObjects.Count; i++)
            {               
                textObj.text = $"{i:F2}";
                UIobjects.Add(Instantiate(SceneObjPlacer.holoObjects[i]));              
                textObj.text = "After Instanciate";
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
                SceneObjPlacer.holoObjects[i].SetActive(true);
                DestroyImmediate(UIobjects[i]);
            }
            UIobjects.Clear();

        }

    }
    #endregion

    #region Map Resize

    /// <summary>
    /// Turns the mini map on.
    /// </summary>
    private void MiniMapOn()
    {
        if (suMinimap == null)
        {
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
        //unresizeUIObject();
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

    public void increaseRadius()
    {
        suManager.BoundingSphereRadiusInMeters += suManager.BoundingSphereRadiusInMeters;
        textObj.text = $"{suManager.BoundingSphereRadiusInMeters:F2}";
    }
    public void decreaseRadius()
    {
        suManager.BoundingSphereRadiusInMeters -= suManager.BoundingSphereRadiusInMeters;
        textObj.text = $"{suManager.BoundingSphereRadiusInMeters:F2}";
    }
}
