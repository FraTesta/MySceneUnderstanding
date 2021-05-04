using System;
using System.Collections;
using System.Linq;
using UnityEngine.XR.WSA.Input;
using UnityEngine.Windows.Speech;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InputManager : MonoBehaviour
{
    #region Member Variables
    private GameObject suMinimap = null;
    #endregion // Member Variables

    [Tooltip("Reference to the main scene understanding manager for default commands.")]
    [SerializeField]
    private MySUManager suManager;

    [Tooltip("Reference to the Labeler Component for SU Scene")]
    [SerializeField]
    private SceneUnderstandingLabeler labeler = null;

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

    public async void toggleText()
    {
        labeler.DisplayTextLabels = !labeler.DisplayTextLabels;
        await suManager.DisplayDataAsync();
    }

    public void toggleMiniMap()
    {
        GameObject suMinimap;
        suMinimap = Instantiate(suManager.SceneRoot);
        suMinimap.name = "Minimap";
        suMinimap.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
        suMinimap.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        suManager.SceneRoot.SetActive(false);

    }
}
