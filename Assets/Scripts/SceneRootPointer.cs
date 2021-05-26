using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneRootPointer : MonoBehaviour
{
    public GameObject sceneRootPointer = null;
    // Start is called before the first frame update
    void Start()
    {
        sceneRootPointer.transform.parent = GameObject.Find("SceneRoot").transform;
        sceneRootPointer.transform.localPosition = Vector3.zero;
        sceneRootPointer.transform.localRotation = Quaternion.identity;
    }


}
