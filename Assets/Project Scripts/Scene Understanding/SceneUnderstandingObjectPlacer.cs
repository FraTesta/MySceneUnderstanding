// Copyright (c) Microsoft Corporation. All rights reserved.

    using System.Collections;
    using System.Collections.Generic;

    using UnityEngine;

/// <summary>
/// This component contains the logic for generating holograms that interact
/// with a Scene Understanding scene. it uses the built in physics system
/// in unity (RigidBody Component)
/// <summary>
public class SceneUnderstandingObjectPlacer : MonoBehaviour
{
    [Tooltip("Game Object to place as reference")]
    [SerializeField]
    private GameObject objToPlaceRef;
    [Tooltip("Material of the game object to place")]
    [SerializeField]
    private Material material;
    [Tooltip("Parent Frame of the objects")]
    [SerializeField]
    private GameObject parentFrame;

    private GameObject objToPlace = null;  // settato a public
    private bool isPlacing = false;

    public List<GameObject> objectPlaced = new List<GameObject>(); // personal variable

    // Container for all instantiated objects/holograms
    public List<GameObject> holoObjects = new List<GameObject>();

    [Tooltip("Anchor Module Script ")]
    [SerializeField]
    private AnchorModuleScript AnchorModue;


    private void StartPlacing()
    {
        objToPlace = Instantiate<GameObject>(objToPlaceRef, new Vector3(0,0,2), Quaternion.identity);
        objToPlace.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        BoxCollider bxObj = objToPlace.AddComponent<BoxCollider>();
        bxObj.center = new Vector3(0, 3, 0);
        bxObj.size = new Vector3(8, 9, 7);

        objToPlace.tag = "alert";
        objToPlace.transform.parent = parentFrame.transform;  
        // Add object to the list
        holoObjects.Add(objToPlace);

        // Disable collider for base object if it has any
        Collider parentCollider = objToPlace.GetComponent<Collider>();
        if (parentCollider != null)
        {
            parentCollider.enabled = false;
        }

        // Disable colliders for any child objects if any exists
        foreach (Transform child in objToPlace.transform)
        {
            Collider childCollider = child.GetComponent<Collider>();

            if (childCollider != null)
            {
                childCollider.enabled = false;
            }
        }
    }

    private async void FinishPlacing()
    {
        //await AnchorModue.StartAzureSession();   
        // Enable collider for base object if it has any
        Collider parentCollider = objToPlace.GetComponent<Collider>();
        if (parentCollider != null)
        {
            parentCollider.enabled = true;
        }

        // Enable colliders for any child objects if any exists
        foreach (Transform child in objToPlace.transform)
        {
            Collider childCollider = child.GetComponent<Collider>();

            if (childCollider != null)
            {
                childCollider.enabled = true;
            }
        }
        
        //foreach (GameObject obj in holoObjects)
        //{
          //  objectPlaced.Add(obj);
        //}

        objToPlace.name = "alert_" + objToPlace.transform.localPosition.x + "_" + objToPlace.transform.localPosition.y + "_" + objToPlace.transform.localPosition.z;
        // set as child of the current main cloud spatial anchor 
        //objToPlace.transform.parent = GameObject.Find("CloudDataManager").transform;
        //AnchorModue.shareNewAnchor(objToPlace);
        //objectPlaced.Add(objToPlace);
        //objectPlaced = objToPlace;
        objToPlace = null;
    }

    private void UpdateObjPos()
    {
        if (objToPlace == null)
        {
            return;
        }

        Vector3 newObjPosition = GetDesiredObjPos();
        objToPlace.transform.position = newObjPosition;
    }

    private Vector3 GetDesiredObjPos()
    {
        RaycastHit hit;
        bool hasTarget = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit);

        Vector3 newObjPos = Vector3.zero;
        if (hasTarget)
        {
            // Get a Position Slightly above of the object that the main camera is gazing at
            Vector3 selectedObjFacingTowards = -hit.transform.forward.normalized;
            newObjPos = Vector3.Dot(Camera.main.transform.TransformDirection(Vector3.forward), selectedObjFacingTowards) < 0 ? hit.point + (selectedObjFacingTowards * 0.3f) : hit.point - (selectedObjFacingTowards * 0.3f);
        }
        else
        {
            // If no object is being gazed at, then place the object infront of the camera.
            newObjPos = Camera.main.transform.position + (Camera.main.transform.forward * 2.0f);
        }

        return newObjPos;
    }

    IEnumerator SprayCoroutine()
    {
        yield return null;

        for (int i = 0; i < 10; i++)
        {
            PrimitiveType pt = i % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere;

            // Init
            GameObject tempgbj = GameObject.CreatePrimitive(pt);
            tempgbj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            tempgbj.GetComponent<MeshRenderer>().material = material;
            tempgbj.AddComponent<Rigidbody>();

            // Set Pos and add force
            tempgbj.transform.position = Camera.main.transform.position + (Camera.main.transform.forward * 1.0f);
            tempgbj.GetComponent<Rigidbody>().AddForce(Camera.main.transform.forward * 3.0f, ForceMode.Impulse);

            // Add primitive to list
            holoObjects.Add(tempgbj);

            yield return new WaitForSeconds(0.1f);
        }
    }

    public void FreezeHolograms()
    {
        // When the scene starts loading, freeze all holograms in place to avoid them falling on an empty scene
        foreach (GameObject obj in holoObjects)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();

            if (rb == null)
            {
                continue;
            }

            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    public void UnfreezeHolograms()
    {
        //When the scene finishes loading, unfreeze all holograms
        foreach (GameObject obj in holoObjects)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();

            if (rb == null)
            {
                continue;
            }

            rb.constraints = RigidbodyConstraints.None;
        }
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        UpdateObjPos();
    }

    //This function is defined as an Input Action, in the Scene Understanding Menu
    // Input Manager component
    public void Place()
    {
        if (!isPlacing)
        {
            StartPlacing();
        }
        else
        {
            FinishPlacing();
        }
        isPlacing = !isPlacing;
    }

    //This function is defined as an Input Action, in the Scene Understanding Menu
    // Input Manager component
    public void Spray()
    {
        StartCoroutine(SprayCoroutine());
    }
}

