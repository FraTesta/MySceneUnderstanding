using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class myAgentController : MonoBehaviour
{
    [Tooltip("nav Mesh Surface")]
    [SerializeField]
    private NavMeshSurface navMeshSurf;

    [Tooltip("Scene Root gameObject ")]
    [SerializeField]
    private GameObject sceneRoot;


    // Start is called before the first frame update

    enum AreaType
    {
        Walkable,
        NotWalkable
    }

    [Tooltip("Player gameObject")]
    [SerializeField]
    private GameObject myPlayer;


    [Tooltip("Target gameObject")]
    [SerializeField]
    private GameObject target;

    // gameobject which represent the projection of the camera frame on the floor surface
    private GameObject cameraProjection;
    private NavMeshAgent myNavMeshAgent;
    // to draw the path 
    private LineRenderer myLineRender;
    private bool enableNagation = false;

    public GameObject Target {
        get { return this.target; }
    }

    // Start is called before the first frame update
    

    // Update is called once per frame
    void Update()
    {
        if (enableNagation)
        {
            if (myNavMeshAgent.hasPath)
            {
                drawPath();
            }
        }
    }

    #region initNavMesh
    public void initNavigation()
    {
        porjectCameraObject();
        // Init nav Mesh Player
        cameraProjection.AddComponent<NavMeshAgent>();
        Debug.Log("Nav Mesh Agent Component added");
        cameraProjection.AddComponent<LineRenderer>();
        Debug.Log("LineRender Comonent added");

        myNavMeshAgent = cameraProjection.GetComponent<NavMeshAgent>();
        myLineRender = cameraProjection.GetComponent<LineRenderer>();

        myNavMeshAgent.speed = 0;

        myLineRender.startWidth = 0.15f;
        myLineRender.endWidth = 0.15f;
        myLineRender.positionCount = 0; // how many points will be in the line, if 0 it will be just a line

        BakeMesh();
    }

    public void BakeMesh()
    {
        //sceneRoot childeren walkable classification
        UpdateNavMeshSettingsForObjsUnderRoot();
        Debug.Log(" Mesh Surfeces setted as walkable ");
        // Blake the navMesh
        navMeshSurf.BuildNavMesh();
        Debug.Log("Blake Nave Mesh");
        // create the navMesh Agent
        //CreateNavMeshAgent();
        enableNagation = true;
    }

    /// <summary>
    /// Analyze all childern of the sceneRoot, and classifies them as walkable or not walkable based on 
    /// </summary>
    void UpdateNavMeshSettingsForObjsUnderRoot()
    {
        // Iterate all the Scene Objects
        foreach (Transform sceneObjContainer in sceneRoot.transform)
        {
            foreach (Transform sceneObj in sceneObjContainer.transform)
            {
                NavMeshModifier nvm = sceneObj.gameObject.AddComponent<NavMeshModifier>();

                // Walkable = 0, Not Walkable = 1
                // This area types are unity predefined, in the unity inspector in the navigation tab go to areas
                // to see them
                nvm.overrideArea = true;
                nvm.area = sceneObj.parent.name == "Floor" ? (int)AreaType.Walkable : (int)AreaType.NotWalkable;
            }
        }
        
    }

    #endregion

    public void setDestination(Vector3 target)
    {
        myNavMeshAgent.SetDestination(target);
        Debug.Log("Desctination Sent");
    }

    private void drawPath()
    {
        myLineRender.positionCount = myNavMeshAgent.path.corners.Length; // we will use the corners as points 
        myLineRender.SetPosition(0, cameraProjection.transform.position);

        if (myNavMeshAgent.path.corners.Length < 2)
        {
            return;
        }

        for (int i = 1; i < myNavMeshAgent.path.corners.Length; i++)
        {
            Vector3 pointPosition = new Vector3(myNavMeshAgent.path.corners[i].x, myNavMeshAgent.path.corners[i].y, myNavMeshAgent.path.corners[i].z);
            myLineRender.SetPosition(i, pointPosition);
        }
    }

    private void porjectCameraObject()
    {
        cameraProjection = new GameObject("Projected Camera Object");
        //cameraProjection = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cameraProjection.transform.position = myPlayer.transform.position;
        cameraProjection.transform.rotation = myPlayer.transform.rotation;

        Vector3 cameraPosition = cameraProjection.transform.position;

        float altitude = findFloorDistance();
        if (altitude == -1000)
        {
            Debug.LogError("No floor quads found");
            return; 
        }
        else
        {
            cameraPosition.y = altitude; // project the cameraPosition on the floor
        }
        cameraProjection.transform.position = cameraPosition;

    }

    private float findFloorDistance()
    {
        //find the flor
        foreach (Transform sceneObjContainer in sceneRoot.transform)
        {
            foreach (Transform sceneObj in sceneObjContainer.transform)
            {
                if (sceneObj.parent.name == "Floor")
                    return sceneObj.transform.position.y;
            }
        }
        return -1000;
    }
}
