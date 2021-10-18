using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class activateNavMesh : MonoBehaviour
{

    public NavMeshSurface navMeshSurf;
    public GameObject sceneRoot;
    public GameObject navMeshAgentRef;
    private GameObject navMeshAgentInstance;
    // Start is called before the first frame update

    enum AreaType
    {
        Walkable,
        NotWalkable
    }


    public void BakeMesh()
    {
        //sceneRoot childeren walkable classification
        UpdateNavMeshSettingsForObjsUnderRoot(); 
        // Blake the navMesh
        navMeshSurf.BuildNavMesh();
        // create the navMesh Agent
        //CreateNavMeshAgent();
    }
    void CreateNavMeshAgent()
    {
        if (navMeshAgentRef == null)
        {
            return;
        }

        if (navMeshAgentInstance == null)
        {
            navMeshAgentInstance = Instantiate<GameObject>(navMeshAgentRef, new Vector3(0.0f, -0.81f, -3.0f), Quaternion.identity);
        }
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
}


