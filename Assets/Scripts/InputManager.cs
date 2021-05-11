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
    private GameObject UIobject = null;

    private Transform mapFrame;

    private float scale = 0.1f;
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

    public void OnSliderUpdated(SliderEventData eventData)
    {
        scale = eventData.NewValue;
        textObj.text = $"{scale:F2}";

    }

    #region UI object resize
    private void resizeUIObject()
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
    }

    private void addUIobjects()
    {
        UIobject = Instantiate(SceneObjPlacer.objectPlaced);
        UIobject.transform.parent = suMinimap.transform;
        SceneObjPlacer.objectPlaced.SetActive(false);
    }


    private void unresizeUIObject()
    {
        if(UIobject != null)
        {
            DestroyImmediate(UIobject);
            UIobject = null;
        }
        SceneObjPlacer.objectPlaced.SetActive(true);
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
            
            addUIobjects();
            suMinimap.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
            suMinimap.transform.localScale = new Vector3(scale, scale, scale);
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
            DestroyImmediate(suMinimap);
            suMinimap = null;
        }
        // destroy all objects
        unresizeUIObject();
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
}
