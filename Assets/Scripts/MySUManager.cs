﻿using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.MixedReality.SceneUnderstanding;

// Unity
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Different rendering modes available for scene objects.
/// </summary>
public enum RenderMode
{
    Quad,
    QuadWithMask,
    Mesh,
    Wireframe
}

[StructLayout(LayoutKind.Sequential)]
public struct HolograhicFrameData
{
    public uint VersionNumber;
    public uint MaxNumberOfCameras;
    public IntPtr ISpatialCoordinateSystemPtr; // Windows::Perception::Spatial::ISpatialCoordinateSystem
    public IntPtr IHolographicFramePtr; // Windows::Graphics::Holographic::IHolographicFrame
    public IntPtr IHolographicCameraPtr; // // Windows::Graphics::Holographic::IHolographicCamera
}

public class MySUManager : MonoBehaviour
{
    #region Public Variables
    [Header("Root GameObject")]
    [Tooltip("GameObject that will be the parent of all Scene Understanding related game objects. If field is left empty an empty gameobject named 'Root' will be created.")]
    public GameObject SceneRoot = null;

    [Header("On Device Request Settings")]
    [Tooltip("Radius of the sphere around the camera, which is used to query the environment.")]
    [Range(5f, 100f)]
    public float BoundingSphereRadiusInMeters = 10.0f;

    [Tooltip("When enabled, the latest data from Scene Understanding data provider will be displayed periodically (controlled by the AutoRefreshIntervalInSeconds float).")]
    public bool AutoRefresh = true;

    [Tooltip("Interval to use for auto refresh, in seconds.")]
    [Range(1f, 60f)]
    public float AutoRefreshIntervalInSeconds = 10.0f;

    public float TimeElapsedSinceLastAutoRefresh = 0.0f;

    [Header("Events")]
    [Tooltip("User function that get called when a Scene Understanding event happens")]
    public UnityEvent OnLoadStarted;
    [Tooltip("User function that get called when a Scene Understanding event happens")]
    public UnityEvent OnLoadFinished;

    [Header("Render Filters")]
    [Tooltip("Toggles display of all scene objects, except for the world mesh.")]
    public bool RenderSceneObjects = true;
    [Tooltip("Toggles display of large, horizontal scene objects, aka 'Platform'.")]
    public bool RenderPlatformSceneObjects = true;
    [Tooltip("Toggles the display of background scene objects.")]
    public bool RenderBackgroundSceneObjects = true;
    [Tooltip("Toggles the display of unknown scene objects.")]
    public bool RenderUnknownSceneObjects = true;
    [Tooltip("Toggles the display of the world mesh.")]
    public bool RenderWorldMesh = false;
    [Tooltip("Toggles the display of completely inferred scene objects.")]
    public bool RenderCompletelyInferredSceneObjects = true;

    [Header("Render Colors")]
    [Tooltip("Colors for the Scene Understanding Background objects")]
    public Color ColorForBackgroundObjects = new Color(0.953f, 0.475f, 0.875f, 1.0f);
    [Tooltip("Colors for the Scene Understanding Wall objects")]
    public Color ColorForWallObjects = new Color(0.953f, 0.494f, 0.475f, 1.0f);
    [Tooltip("Colors for the Scene Understanding Floor objects")]
    public Color ColorForFloorObjects = new Color(0.733f, 0.953f, 0.475f, 1.0f);
    [Tooltip("Colors for the Scene Understanding Ceiling objects")]
    public Color ColorForCeilingObjects = new Color(0.475f, 0.596f, 0.953f, 1.0f);
    [Tooltip("Colors for the Scene Understanding Platform objects")]
    public Color ColorForPlatformsObjects = new Color(0.204f, 0.792f, 0.714f, 1.0f);
    [Tooltip("Colors for the Scene Understanding Unknown objects")]
    public Color ColorForUnknownObjects = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    [Tooltip("Colors for the Scene Understanding Inferred objects")]
    public Color ColorForInferredObjects = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    [Tooltip("Colors for the World mesh")]
    public Color ColorForWorldObjects = new Color(0.0f, 1.0f, 1.0f, 1.0f);


    [Header("Layers")]
    [Tooltip("Layer for Scene Understanding Background objects")]
    public int LayerForBackgroundObjects;
    [Tooltip("Layer for the Scene Understanding Wall objects")]
    public int LayerForWallObjects;
    [Tooltip("Layer for the Scene Understanding Floor objects")]
    public int LayerForFloorObjects;
    [Tooltip("Layer for the Scene Understanding Ceiling objects")]
    public int LayerForCeilingObjects;
    [Tooltip("Layer for the Scene Understanding Platform objects")]
    public int LayerForPlatformsObjects;
    [Tooltip("Layer for the Scene Understanding Unknown objects")]
    public int LayerForUnknownObjects;
    [Tooltip("Layer for the Scene Understanding Inferred objects")]
    public int LayerForInferredObjects;
    [Tooltip("Layer for the World mesh")]
    public int LayerForWorldObjects;

    [Header("Materials")]
    [Tooltip("Material for scene object meshes.")]
    public Material SceneObjectMeshMaterial = null;
    [Tooltip("Material for scene object quads.")]
    public Material SceneObjectQuadMaterial = null;
    [Tooltip("Material for scene object mesh wireframes.")]
    public Material SceneObjectWireframeMaterial = null;
    [Tooltip("Material for scene objects when in Ghost mode (invisible object with occlusion)")]
    public Material TransparentOcclussion = null;

    [Header("Physics")]
    [Tooltip("Toggles the creation of objects with collider components")]
    public bool AddColliders = false;

    [Header("Occlussion")]
    [Tooltip("Toggle Ghost Mode, (invisible objects that still occlude)")]
    public bool IsInGhostMode = false;

    [Header("Request Settings")]
    [Tooltip("Type of visualization to use for scene objects.")]
    public RenderMode SceneObjectRequestMode = RenderMode.Mesh;
    [Tooltip("Level Of Detail for the scene objects.")]
    public SceneMeshLevelOfDetail MeshQuality = SceneMeshLevelOfDetail.Medium;
    [Tooltip("When enabled, requests observed and inferred regions for scene objects. When disabled, requests only the observed regions for scene objects.")]
    public bool RequestInferredRegions = true;
    #endregion

    #region Private Variables
    private Dictionary<SceneObjectKind, Dictionary<RenderMode, Material>> materialCache;
    private readonly object SUDataLock = new object();
    private byte[] LatestSUSceneData = null;
    private readonly float MinBoundingSphereRadiusInMeters = 5f;
    private readonly float MaxBoundingSphereRadiusInMeters = 100f;
    private Guid LatestSceneGuid;
    private Guid LastDisplayedSceneGuid;
    private Task displayTask = null;
    private readonly int NumberOfSceneObjectsToLoadPerFrame = 5;
    #endregion

    #region Unity Start
    // Start is called before the first frame update
    private async void Start()
    {
        SceneRoot = SceneRoot == null ? new GameObject("Scene Root") : SceneRoot;

        if (!SceneObserver.IsSupported())
        {
            Debug.LogError("SceneUnderstandingManager.Start: Scene Understanding not supported.");
            return;
        }

        SceneObserverAccessStatus access = await SceneObserver.RequestAccessAsync();
        if (access != SceneObserverAccessStatus.Allowed)
        {
            Debug.LogError("SceneUnderstandingManager.Start: Access to Scene Understanding has been denied.\n" +
                           "Reason: " + access);
            return;
        }
        try
        {
#pragma warning disable CS4014
            Task.Run(() => RetrieveDataContinuously());
#pragma warning restore CS4014
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    #endregion

    #region Unity Update
    private async void Update()
    {
        if (AutoRefresh)
        {
            TimeElapsedSinceLastAutoRefresh += Time.deltaTime;
            if (TimeElapsedSinceLastAutoRefresh >= AutoRefreshIntervalInSeconds)
            {
                try
                {
                    await DisplayDataAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in Update");
                }
                TimeElapsedSinceLastAutoRefresh = 0.0f;
            }
        }
    }
    #endregion

    #region Data Querying and Consumption
    // It is recommended to deserialize a scene from scene fragments
    // consider all scenes as made up of scene fragments, even if only one.
    private SceneFragment GetLatestSceneSerialization()
    {
        SceneFragment fragmentToReturn = null;

        lock (SUDataLock)
        {
            if (LatestSUSceneData != null)
            {
                byte[] sceneBytes = null;
                int sceneLength = LatestSUSceneData.Length;
                sceneBytes = new byte[sceneLength];

                Array.Copy(LatestSUSceneData, sceneBytes, sceneLength);

                // Deserialize the scene into a Scene Fragment
                fragmentToReturn = SceneFragment.Deserialize(sceneBytes);
            }
        }

        return fragmentToReturn;
    }

    private Guid GetLatestSUSceneId()
    {
        Guid suSceneIdToReturn;

        lock (SUDataLock)
        {
            // Return the GUID for the latest scene
            suSceneIdToReturn = LatestSceneGuid;
        }

        return suSceneIdToReturn;
    }

    /// <summary>
    /// Retrieves Scene Understanding data continuously from the runtime.
    /// </summary>
    private void RetrieveDataContinuously()
    {
        // At the beginning, retrieve only the observed scene object meshes.
        RetrieveData(BoundingSphereRadiusInMeters, false, true, false, false, SceneMeshLevelOfDetail.Coarse);

        while (true)
        {
            // Always request quads, meshes and the world mesh. SceneUnderstandingManager will take care of rendering only what the user has asked for.
            RetrieveData(BoundingSphereRadiusInMeters, true, true, RequestInferredRegions, true, MeshQuality);
        }
    }


    /// <summary>
    /// Calls into the Scene Understanding APIs, to retrieve the latest scene as a byte array.
    /// </summary>
    /// <param name="enableQuads">When enabled, quad representation of scene objects is retrieved.</param>
    /// <param name="enableMeshes">When enabled, mesh representation of scene objects is retrieved.</param>
    /// <param name="enableInference">When enabled, both observed and inferred scene objects are retrieved. Otherwise, only observed scene objects are retrieved.</param>
    /// <param name="enableWorldMesh">When enabled, retrieves the world mesh.</param>
    /// <param name="lod">If world mesh is enabled, lod controls the resolution of the mesh returned.</param>
    private void RetrieveData(float boundingSphereRadiusInMeters, bool enableQuads, bool enableMeshes, bool enableInference, bool enableWorldMesh, SceneMeshLevelOfDetail lod)
    {
        Debug.Log("SceneUnderstandingManager.RetrieveData: Started.");

        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        try
        {
            SceneQuerySettings querySettings;
            querySettings.EnableSceneObjectQuads = enableQuads;
            querySettings.EnableSceneObjectMeshes = enableMeshes;
            querySettings.EnableOnlyObservedSceneObjects = !enableInference;
            querySettings.EnableWorldMesh = enableWorldMesh;
            querySettings.RequestedMeshLevelOfDetail = lod;

            // Ensure that the bounding radius is within the min/max range.
            boundingSphereRadiusInMeters = Mathf.Clamp(boundingSphereRadiusInMeters, MinBoundingSphereRadiusInMeters, MaxBoundingSphereRadiusInMeters);

            // Make sure the scene query has completed swap with latestSUSceneData under lock to ensure the application is always pointing to a valid scene.
            SceneBuffer serializedScene = SceneObserver.ComputeSerializedAsync(querySettings, boundingSphereRadiusInMeters).GetAwaiter().GetResult();
            lock (SUDataLock)
            {
                // The latest data queried from the device is stored in these variables
                LatestSUSceneData = new byte[serializedScene.Size];
                serializedScene.GetData(LatestSUSceneData);
                LatestSceneGuid = Guid.NewGuid();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        stopwatch.Stop();
        Debug.Log(string.Format("SceneUnderstandingManager.RetrieveData: Completed. Radius: {0}; Quads: {1}; Meshes: {2}; Inference: {3}; WorldMesh: {4}; LOD: {5}; Bytes: {6}; Time (secs): {7};",
                                boundingSphereRadiusInMeters,
                                enableQuads,
                                enableMeshes,
                                enableInference,
                                enableWorldMesh,
                                lod,
                                (LatestSUSceneData == null ? 0 : LatestSUSceneData.Length),
                                stopwatch.Elapsed.TotalSeconds));
    }


    #endregion

    #region Display Data into Unity
    /// <summary>
    /// Displays the most recently updated SU data as Unity game objects.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> that represents the operation.
    /// </returns>
    public Task DisplayDataAsync()
    {
        // See if we already have a running task
        if ((displayTask != null) && (!displayTask.IsCompleted))
        {
            // Yes we do. Return the already running task.
            Debug.Log($"error in DisplayDataAsync already in progress.");
            return displayTask;
        }
        // We have real work to do. Time to start the coroutine and track it.
        else
        {
            // Create a completion source
            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();

            // Store the task
            displayTask = completionSource.Task;

            // Run Callbacks for On Load Started
            OnLoadStarted.Invoke();

            // Start the coroutine and pass in the completion source
            StartCoroutine(DisplayDataRoutine(completionSource));

            // Return the newly running task
            return displayTask;
        }
    }

    /// <summary>
    /// This coroutine will deserialize the latest SU data, either queried from the device
    /// or from disk and use it to create Unity Objects that represent that geometry
    /// </summary>
    /// <param name="completionSource">
    /// The <see cref="TaskCompletionSource{TResult}"/> that can be used to signal the coroutine is complete.
    /// </param>
    private IEnumerator DisplayDataRoutine(TaskCompletionSource<bool> completionSource)
    {
        Debug.Log("SceneUnderstandingManager.DisplayData: About to display the latest set of Scene Objects");
        Scene suScene = null;

        // Get Latest Scene and Deserialize it
        // Scenes Queried from a device are Scenes composed of one Scene Fragment
        SceneFragment sceneFragment = GetLatestSceneSerialization();
        SceneFragment[] sceneFragmentsArray = new SceneFragment[1] { sceneFragment };
        suScene = Scene.FromFragments(sceneFragmentsArray);

        // Get Latest Scene GUID
        Guid latestGuidSnapShot = GetLatestSUSceneId();
        LastDisplayedSceneGuid = latestGuidSnapShot;
        


        if (suScene != null)
        {
            // Retrieve a transformation matrix that will allow us orient the Scene Understanding Objects into
            // their correct corresponding position in the unity world
            System.Numerics.Matrix4x4? sceneToUnityTransformAsMatrix4x4 = GetSceneToUnityTransformAsMatrix4x4(suScene);

            if (sceneToUnityTransformAsMatrix4x4 != null)
            {
                // If there was previously a scene displayed in the game world, destroy it
                // to avoid overlap with the new scene about to be displayed
                DestroyAllGameObjectsUnderParent(SceneRoot.transform);

                // Allow from one frame to yield the coroutine back to the main thread
                yield return null;

                // Using the transformation matrix generated above, port its values into the tranform of the scene root (Numerics.matrix -> GameObject.Transform)
                SetUnityTransformFromMatrix4x4(SceneRoot.transform, sceneToUnityTransformAsMatrix4x4.Value, true);

                // After the scene has been oriented, loop through all the scene objects and
                // generate their corresponding Unity Object
                IEnumerable<SceneObject> sceneObjects = suScene.SceneObjects;

                int i = 0;
                foreach (SceneObject sceneObject in sceneObjects)
                {
                    if (DisplaySceneObject(sceneObject))
                    {
                        if (++i % NumberOfSceneObjectsToLoadPerFrame == 0)
                        {
                            // Allow a certain number of objects to load before yielding back to main thread
                            yield return null;
                        }
                    }
                }
            }

            // When all objects have been loaded, finish.
            Debug.Log("SceneUnderStandingManager.DisplayData: Display Completed");

            // Run CallBacks for Onload Finished
            OnLoadFinished.Invoke();

            // Let the task complete
            completionSource.SetResult(true);
        }
    }

    /// <summary>
    /// Create a Unity Game Object for an individual Scene Understanding Object
    /// </summary>
    /// <param name="suObject">The Scene Understanding Object to generate in Unity</param>
    private bool DisplaySceneObject(SceneObject suObject)
    {
        if (suObject == null)
        {
            Debug.LogWarning("SceneUnderstandingManager.DisplaySceneObj: Object is null");
            return false;
        }

        // If requested, scene objects can be excluded from the generation, the World Mesh is considered
        // a separate object hence is not affected by this filter
        if (RenderSceneObjects == false && suObject.Kind != SceneObjectKind.World)
        {
            return false;
        }

        // If an individual type of object is requested to not be rendered, avoid generation of unity object
        SceneObjectKind kind = suObject.Kind;
        switch (kind)
        {
            case SceneObjectKind.World:
                if (!RenderWorldMesh)
                    return false;
                break;
            case SceneObjectKind.Platform:
                if (!RenderPlatformSceneObjects)
                    return false;
                break;
            case SceneObjectKind.Background:
                if (!RenderBackgroundSceneObjects)
                    return false;
                break;
            case SceneObjectKind.Unknown:
                if (!RenderUnknownSceneObjects)
                    return false;
                break;
            case SceneObjectKind.CompletelyInferred:
                if (!RenderCompletelyInferredSceneObjects)
                    return false;
                break;
        }

        // This gameobject will hold all the geometry that represents the Scene Understanding Object
        GameObject unityParentHolderObject = new GameObject(suObject.Kind.ToString());
        unityParentHolderObject.transform.parent = SceneRoot.transform;

        // Scene Understanding uses a Right Handed Coordinate System and Unity uses a left handed one, convert.
        System.Numerics.Matrix4x4 converted4x4LocationMatrix = ConvertRightHandedMatrix4x4ToLeftHanded(suObject.GetLocationAsMatrix());
        // From the converted Matrix pass its values into the unity transform (Numerics -> Unity.Transform)
        SetUnityTransformFromMatrix4x4(unityParentHolderObject.transform, converted4x4LocationMatrix, true);

        // This list will keep track of all the individual objects that represent the geometry of
        // the Scene Understanding Object
        List<GameObject> unityGeometryObjects = null;
        switch (kind)
        {
            // Create all the geometry and store it in the list
            case SceneObjectKind.World:
                unityGeometryObjects = CreateWorldMeshInUnity(suObject);
                break;
            default:
                unityGeometryObjects = CreateSUObjectInUnity(suObject);
                break;
        }

        // For all the Unity Game Objects that represent The Scene Understanding Object
        // Of this iteration, make sure they are all children of the UnityParent object
        // And that their local postion and rotation is relative to their parent
        foreach (GameObject geometryObject in unityGeometryObjects)
        {
            geometryObject.transform.parent = unityParentHolderObject.transform;
            geometryObject.transform.localPosition = Vector3.zero;
            geometryObject.transform.localRotation = Quaternion.identity;
        }


        // If the Scene is running on a device, add a World Anchor to align the Unity object
        // to the XR scene
        unityParentHolderObject.AddComponent<UnityEngine.XR.WSA.WorldAnchor>();
        

        //Return that the Scene Object was indeed represented as a unity object and wasn't skipped
        return true;
    }

    /// <summary>
    /// Create a world Mesh Unity Object that represents the World Mesh Scene Understanding Object
    /// </summary>
    /// <param name="suObject">The Scene Understanding Object to generate in Unity</param>
    private List<GameObject> CreateWorldMeshInUnity(SceneObject suObject)
    {
        // The World Mesh Object is different from the rest of the Scene Understanding Objects
        // in the Sense that its unity representation is not affected by the filters or Request Modes
        // in this component, the World Mesh Renders even of the Scene Objects are disabled and
        // the World Mesh is always represented with a WireFrame Material, different to the Scene
        // Understanding Objects whose materials vary depending on the Settings in the component

        IEnumerable<SceneMesh> suMeshes = suObject.Meshes;
        Mesh unityMesh = GenerateUnityMeshFromSceneObjectMeshes(suMeshes);

        GameObject gameObjToReturn = new GameObject(suObject.Kind.ToString());
        gameObjToReturn.layer = LayerForWorldObjects;
        Material tempMaterial = GetMaterial(SceneObjectKind.World, RenderMode.Wireframe);
        AddMeshToUnityObject(gameObjToReturn, unityMesh, ColorForWorldObjects, tempMaterial);

        if (AddColliders)
        {
            // Generate a unity mesh for physics
            Mesh unityColliderMesh = GenerateUnityMeshFromSceneObjectMeshes(suObject.ColliderMeshes);

            MeshCollider col = gameObjToReturn.AddComponent<MeshCollider>();
            col.sharedMesh = unityColliderMesh;
        }

        // Also the World Mesh is represented as one big Mesh in Unity, different to the rest of SceneObjects
        // Where their multiple meshes are represented in separate game objects
        return new List<GameObject> { gameObjToReturn };
    }

    /// <summary>
    /// Create a list of Unity GameObjects that represent all the Meshes/Geometry in a Scene
    /// Understanding Object
    /// </summary>
    /// <param name="suObject">The Scene Understanding Object to generate in Unity</param>
    private List<GameObject> CreateSUObjectInUnity(SceneObject suObject)
    {
        // Each SU object has a specific type, query for its correspoding color
        // according to its type
        Color? color = GetColor(suObject.Kind);
        int layer = GetLayer(suObject.Kind);

        // QUADS
        List<GameObject> listOfGeometryGameObjToReturn = new List<GameObject>();
        if (SceneObjectRequestMode == RenderMode.Quad || SceneObjectRequestMode == RenderMode.QuadWithMask)
        {
            // If the Request Settings are requesting quads, create a gameobject in unity for
            // each quad in the Scene Object
            foreach (SceneQuad quad in suObject.Quads)
            {
                Mesh unityMesh = GenerateUnityMeshFromSceneObjectQuad(quad);

                Material tempMaterial = GetMaterial(suObject.Kind, SceneObjectRequestMode);

                GameObject gameObjectToReturn = new GameObject(suObject.Kind.ToString());
                gameObjectToReturn.layer = layer;
                AddMeshToUnityObject(gameObjectToReturn, unityMesh, color, tempMaterial);

                if (SceneObjectRequestMode == RenderMode.QuadWithMask)
                {
                    ApplyQuadRegionMask(quad, gameObjectToReturn, color.Value);
                }

                if (AddColliders)
                {
                    gameObjectToReturn.AddComponent<BoxCollider>();
                }

                // Add to list
                listOfGeometryGameObjToReturn.Add(gameObjectToReturn);
            }
        }
        else // MESH OR WUREFRAME
        {
            // If the Request Settings are requesting Meshes or WireFrame, create a gameobject in unity for
            // each Mesh, and apply either the default material or the wireframe material
            for (int i = 0; i < suObject.Meshes.Count; i++)
            {
                SceneMesh suGeometryMesh = suObject.Meshes[i];
                SceneMesh suColliderMesh = suObject.ColliderMeshes[i];

                // Generate the unity mesh for the Scene Understanding mesh.
                Mesh unityMesh = GenerateUnityMeshFromSceneObjectMeshes(new List<SceneMesh> { suGeometryMesh });
                GameObject gameObjectToReturn = new GameObject(suObject.Kind.ToString() + "Mesh");
                gameObjectToReturn.layer = layer;

                Material tempMaterial = GetMaterial(suObject.Kind, SceneObjectRequestMode);

                // Add the created Mesh into the Unity Object
                AddMeshToUnityObject(gameObjectToReturn, unityMesh, color, tempMaterial);

                if (AddColliders)
                {
                    // Generate a unity mesh for physics
                    Mesh unityColliderMesh = GenerateUnityMeshFromSceneObjectMeshes(new List<SceneMesh> { suColliderMesh });

                    MeshCollider col = gameObjectToReturn.AddComponent<MeshCollider>();
                    col.sharedMesh = unityColliderMesh;
                }

                listOfGeometryGameObjToReturn.Add(gameObjectToReturn);
            }
        }

        // Return all the Geometry GameObjects that represent a Scene
        // Understanding Object
        return listOfGeometryGameObjToReturn;
    }

    #region Generation of Mesh and Quads from SceneObjectMeshes to Unity Meshes
    /// <summary>
    /// Create a unity Mesh from a set of Scene Understanding Meshes
    /// </summary>
    /// <param name="suMeshes">The Scene Understanding mesh to generate in Unity</param>
    private Mesh GenerateUnityMeshFromSceneObjectMeshes(IEnumerable<SceneMesh> suMeshes)
    {
        if (suMeshes == null)
        {
            Debug.LogWarning("SceneUnderstandingManager.GenerateUnityMeshFromSceneObjectMeshes: Meshes is null.");
            return null;
        }

        // Retrieve the data and store it as Indices and Vertices
        List<int> combinedMeshIndices = new List<int>();
        List<Vector3> combinedMeshVertices = new List<Vector3>();

        foreach (SceneMesh suMesh in suMeshes)
        {
            if (suMesh == null)
            {
                Debug.LogWarning("SceneUnderstandingManager.GenerateUnityMeshFromSceneObjectMeshes: Mesh is null.");
                continue;
            }

            uint[] meshIndices = new uint[suMesh.TriangleIndexCount];
            suMesh.GetTriangleIndices(meshIndices);

            System.Numerics.Vector3[] meshVertices = new System.Numerics.Vector3[suMesh.VertexCount];
            suMesh.GetVertexPositions(meshVertices);

            uint indexOffset = (uint)combinedMeshVertices.Count;

            // Store the Indices and Vertices
            for (int i = 0; i < meshVertices.Length; i++)
            {
                // Here Z is negated because Unity Uses Left handed Coordinate system and Scene Understanding uses Right Handed
                combinedMeshVertices.Add(new Vector3(meshVertices[i].X, meshVertices[i].Y, -meshVertices[i].Z));
            }

            for (int i = 0; i < meshIndices.Length; i++)
            {
                combinedMeshIndices.Add((int)(meshIndices[i] + indexOffset));
            }
        }

        Mesh unityMesh = new Mesh();

        // Unity has a limit of 65,535 vertices in a mesh.
        // This limit exists because by default Unity uses 16-bit index buffers.
        // Starting with 2018.1, Unity allows one to use 32-bit index buffers.
        if (combinedMeshVertices.Count > 65535)
        {
            Debug.Log("SceneUnderstandingManager.GenerateUnityMeshForSceneObjectMeshes: CombinedMeshVertices count is " + combinedMeshVertices.Count + ". Will be using a 32-bit index buffer.");
            unityMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        // Apply the Indices and Vertices
        unityMesh.SetVertices(combinedMeshVertices);
        unityMesh.SetIndices(combinedMeshIndices.ToArray(), MeshTopology.Triangles, 0);
        unityMesh.RecalculateNormals();

        return unityMesh;
    }

    /// <summary>
    /// Create a Unity Mesh from a Scene Understanding Quad
    /// </summary>
    /// <param name="suQuad">The Scene Understanding quad to generate in Unity</param>
    private Mesh GenerateUnityMeshFromSceneObjectQuad(SceneQuad suQuad)
    {
        if (suQuad == null)
        {
            Debug.LogWarning("SceneUnderstandingManager.GenerateUnityMeshForSceneObjectQuad: Quad is null.");
            return null;
        }

        float widthInMeters = suQuad.Extents.X;
        float heightInMeters = suQuad.Extents.Y;

        // Bounds of the quad.
        List<Vector3> vertices = new List<Vector3>()
            {
                new Vector3(-widthInMeters / 2, -heightInMeters / 2, 0),
                    new Vector3( widthInMeters / 2, -heightInMeters / 2, 0),
                    new Vector3(-widthInMeters / 2,  heightInMeters / 2, 0),
                    new Vector3( widthInMeters / 2,  heightInMeters / 2, 0)
            };

        List<int> triangles = new List<int>()
            {
                1, 3, 0,
                3, 2, 0
            };

        List<Vector2> uvs = new List<Vector2>()
            {
                new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1)
            };

        Mesh unityMesh = new Mesh();
        unityMesh.SetVertices(vertices);
        unityMesh.SetIndices(triangles.ToArray(), MeshTopology.Triangles, 0);
        unityMesh.SetUVs(0, uvs);

        return unityMesh;
    }
    #endregion

    #region Utiliy Functions (GetColor, GetLayer, GetMaterial, AddMeshToUnityObject)

    /// <summary>
    /// Get the corresponding color for each SceneObject Kind
    /// </summary>
    /// <param name="kind">The Scene Understanding kind from which to query the color</param>
    private Color? GetColor(SceneObjectKind kind)
    {
        switch (kind)
        {
            case SceneObjectKind.Background:
                return ColorForBackgroundObjects;
            case SceneObjectKind.Wall:
                return ColorForWallObjects;
            case SceneObjectKind.Floor:
                return ColorForFloorObjects;
            case SceneObjectKind.Ceiling:
                return ColorForCeilingObjects;
            case SceneObjectKind.Platform:
                return ColorForPlatformsObjects;
            case SceneObjectKind.Unknown:
                return ColorForUnknownObjects;
            case SceneObjectKind.CompletelyInferred:
                return ColorForInferredObjects;
            case SceneObjectKind.World:
                return ColorForWorldObjects;
            default:
                return null;
        }
    }
    /// <summary>
    /// Get the corresponding layer for each SceneObject Kind
    /// </summary>
    /// <param name="kind">The Scene Understanding kind from which to query the layer</param>
    private int GetLayer(SceneObjectKind kind)
    {
        switch (kind)
        {
            case SceneObjectKind.Background:
                return LayerForBackgroundObjects;
            case SceneObjectKind.Wall:
                return LayerForWallObjects;
            case SceneObjectKind.Floor:
                return LayerForFloorObjects;
            case SceneObjectKind.Ceiling:
                return LayerForCeilingObjects;
            case SceneObjectKind.Platform:
                return LayerForPlatformsObjects;
            case SceneObjectKind.Unknown:
                return LayerForUnknownObjects;
            case SceneObjectKind.CompletelyInferred:
                return LayerForInferredObjects;
            case SceneObjectKind.World:
                return LayerForWorldObjects;
            default:
                return 0;
        }
    }

    /// <summary>
    /// Get the cached material for each SceneObject Kind
    /// </summary>
    /// <param name="kind">
    /// The <see cref="SceneObjectKind"/> to obtain the material for.
    /// </param>
    /// <param name="mode">
    /// The <see cref="RenderMode"/> to obtain the material for.
    /// </param>
    /// <remarks>
    /// If <see cref="IsInGhostMode"/> is true, the ghost material will be returned.
    /// </remarks>
    private Material GetMaterial(SceneObjectKind kind, RenderMode mode)
    {
        // If in ghost mode, just return transparent
        if (IsInGhostMode) { return TransparentOcclussion; }

        // Make sure we have a cache
        if (materialCache == null) { materialCache = new Dictionary<SceneObjectKind, Dictionary<RenderMode, Material>>(); }

        // Find or create cache specific to this Kind
        Dictionary<RenderMode, Material> kindModeCache;
        if (!materialCache.TryGetValue(kind, out kindModeCache))
        {
            kindModeCache = new Dictionary<RenderMode, Material>();
            materialCache[kind] = kindModeCache;
        }

        // Find or create material specific to this Mode
        Material mat;
        if (!kindModeCache.TryGetValue(mode, out mat))
        {
            // Determine the source material by kind
            Material sourceMat;
            switch (mode)
            {
                case RenderMode.Quad:
                case RenderMode.QuadWithMask:
                    sourceMat = SceneObjectQuadMaterial;
                    break;
                case RenderMode.Wireframe:
                    sourceMat = SceneObjectWireframeMaterial;
                    break;
                default:
                    sourceMat = SceneObjectMeshMaterial;
                    break;
            }

            // Create an instance
            mat = Instantiate(sourceMat);

            // Set color to match the kind
            Color? color = GetColor(kind);
            if (color != null)
            {
                mat.color = color.Value;
                mat.SetColor("_WireColor", color.Value);
            }

            // Store
            kindModeCache[mode] = mat;
        }

        // Return the found or created material
        return mat;
    }


    /// <summary>
    /// Function to add a Mesh to a Unity Object
    /// </summary>
    /// <param name="unityObject">The unity object to where the mesh will be applied </param>
    /// <param name="mesh"> Mesh to be applied                                       </param>
    /// <param name="color"> Color to apply to the Mesh                              </param>
    /// <param name="material"> Material to apply to the unity Mesh Renderer         </param>
    private void AddMeshToUnityObject(GameObject unityObject, Mesh mesh, Color? color, Material material)
    {
        if (unityObject == null || mesh == null || material == null)
        {
            Debug.Log("SceneUnderstandingManager.AddMeshToUnityObject: One or more arguments are null");
        }

        MeshFilter mf = unityObject.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = unityObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
    }

    /// <summary>
    /// Apply Region mask to a Scene Object
    /// </summary>
    private void ApplyQuadRegionMask(SceneQuad quad, GameObject gameobject, Color color)
    {
        if (quad == null || gameobject == null)
        {
            Debug.LogWarning("SceneUnderstandingManager.ApplyQuadRegionMask: One or more arguments are null.");
            return;
        }

        // Resolution of the mask.
        ushort width = 256;
        ushort height = 256;

        byte[] mask = new byte[width * height];
        quad.GetSurfaceMask(width, height, mask);

        MeshRenderer meshRenderer = gameobject.GetComponent<MeshRenderer>();
        if (meshRenderer == null || meshRenderer.sharedMaterial == null || meshRenderer.sharedMaterial.HasProperty("_MainTex") == false)
        {
            Debug.LogWarning("SceneUnderstandingManager.ApplyQuadRegionMask: Mesh renderer component is null or does not have a valid material.");
            return;
        }

        // Create a new texture.
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        // Transfer the invalidation mask onto the texture.
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; ++i)
        {
            byte value = mask[i];

            if (value == (byte)SceneRegionSurfaceKind.NotSurface)
            {
                pixels[i] = Color.clear;
            }
            else
            {
                pixels[i] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(true);

        // Set the texture on the material.
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
    #endregion

    #endregion

    #region Utility Functions

    /// <summary>
    /// Function to destroy all children under a Unity Transform
    /// </summary>
    /// <param name="parentTransform"> Parent Transform to remove children from </param>
    private void DestroyAllGameObjectsUnderParent(Transform parentTransform)
    {
        if (parentTransform == null)
        {
            Debug.LogWarning("SceneUnderstandingManager.DestroyAllGameObjectsUnderParent: Parent is null.");
            return;
        }

        foreach (Transform child in parentTransform)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Function to return the correspoding transformation matrix to pass geometry
    /// from the Scene Understanding Coordinate System to the Unity one
    /// </summary>
    /// <param name="scene"> Scene from which to get the Scene Understanding Coordinate System </param>
    private System.Numerics.Matrix4x4? GetSceneToUnityTransformAsMatrix4x4(Scene scene)
    {
        System.Numerics.Matrix4x4? sceneToUnityTransform = System.Numerics.Matrix4x4.Identity;


        Microsoft.Windows.Perception.Spatial.SpatialCoordinateSystem sceneCoordinateSystem = Microsoft.Windows.Perception.Spatial.Preview.SpatialGraphInteropPreview.CreateCoordinateSystemForNode(scene.OriginSpatialGraphNodeId);
        HolograhicFrameData holoFrameData = Marshal.PtrToStructure<HolograhicFrameData>(UnityEngine.XR.XRDevice.GetNativePtr());
        Microsoft.Windows.Perception.Spatial.SpatialCoordinateSystem unityCoordinateSystem = Microsoft.Windows.Perception.Spatial.SpatialCoordinateSystem.FromNativePtr(holoFrameData.ISpatialCoordinateSystemPtr);

        sceneToUnityTransform = sceneCoordinateSystem.TryGetTransformTo(unityCoordinateSystem);

        if (sceneToUnityTransform != null)
        {
            sceneToUnityTransform = ConvertRightHandedMatrix4x4ToLeftHanded(sceneToUnityTransform.Value);
        }
        else
        {
            Debug.LogWarning("SceneUnderstandingManager.GetSceneToUnityTransform: Scene to Unity transform is null.");
        }
        

        return sceneToUnityTransform;
    }

    /// <summary>
    /// Converts a right handed tranformation matrix into a left handed one
    /// </summary>
    /// <param name="matrix"> Matrix to convert </param>
    private System.Numerics.Matrix4x4 ConvertRightHandedMatrix4x4ToLeftHanded(System.Numerics.Matrix4x4 matrix)
    {
        matrix.M13 = -matrix.M13;
        matrix.M23 = -matrix.M23;
        matrix.M43 = -matrix.M43;

        matrix.M31 = -matrix.M31;
        matrix.M32 = -matrix.M32;
        matrix.M34 = -matrix.M34;

        return matrix;
    }

    /// <summary>
    /// Passes all the values from a 4x4 tranformation matrix into a Unity Tranform
    /// </summary>
    /// <param name="targetTransform"> Transform to pass the values into                                    </param>
    /// <param name="matrix"> Matrix from which the values to pass are gathered                             </param>
    /// <param name="updateLocalTransformOnly"> Flag to update local transform or global transform in unity </param>
    private void SetUnityTransformFromMatrix4x4(Transform targetTransform, System.Numerics.Matrix4x4 matrix, bool updateLocalTransformOnly = false)
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("SceneUnderstandingManager.SetUnityTransformFromMatrix4x4: Unity transform is null.");
            return;
        }

        Vector3 unityTranslation;
        Quaternion unityQuat;
        Vector3 unityScale;

        System.Numerics.Vector3 vector3;
        System.Numerics.Quaternion quaternion;
        System.Numerics.Vector3 scale;

        System.Numerics.Matrix4x4.Decompose(matrix, out scale, out quaternion, out vector3);

        unityTranslation = new Vector3(vector3.X, vector3.Y, vector3.Z);
        unityQuat = new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        unityScale = new Vector3(scale.X, scale.Y, scale.Z);

        if (updateLocalTransformOnly)
        {
            targetTransform.localPosition = unityTranslation;
            targetTransform.localRotation = unityQuat;
        }
        else
        {
            targetTransform.SetPositionAndRotation(unityTranslation, unityQuat);
        }
    }

    #endregion
}



   
