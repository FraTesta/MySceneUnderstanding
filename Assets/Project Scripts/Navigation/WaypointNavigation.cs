using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using RestSharp;

public class WaypointNavigation : MonoBehaviour
{
    [Tooltip("Reference to the AzureModule to handle the Azure Spatial Anchors")]
    [SerializeField]
    private AnchorModuleScript AzureModule;
    // Start is called before the first frame update

    private SpatialAnchorManager cloudManager;
    private CloudSpatialAnchorWatcher currentWatcher;
    private CloudSpatialAnchor currentCloudAnchor;

    private AnchorLocateCriteria anchorLocateCriteria = new AnchorLocateCriteria();

    private void Start()
    {
        cloudManager = GetComponent<SpatialAnchorManager>();
        SetGraphEnabled(true);
    }

    void OnDestroy()
    {
        if (cloudManager != null && cloudManager.Session != null)
        {
            cloudManager.DestroySession();
        }

        if (currentWatcher != null)
        {
            currentWatcher.Stop();
            currentWatcher = null;
        }
    }

    protected void SetGraphEnabled(bool UseGraph, bool JustGraph = false)
    {
        anchorLocateCriteria.Strategy = UseGraph ?
                                        (JustGraph ? LocateStrategy.Relationship : LocateStrategy.AnyStrategy) :
                                        LocateStrategy.VisualInformation;
    }

    public async Task CreateAzureAnchor(GameObject theObject)
    {
        Debug.Log("\nAnchorModuleScript.CreateAzureAnchor()");

        // First we create a native XR anchor at the location of the object in question
        theObject.CreateNativeAnchor();

        // Then we create a new local cloud anchor
        CloudSpatialAnchor localCloudAnchor = new CloudSpatialAnchor();

        // Now we set the local cloud anchor's position to the native XR anchor's position
        localCloudAnchor.LocalAnchor = theObject.FindNativeAnchor().GetPointer();

        // Check to see if we got the local XR anchor pointer
        if (localCloudAnchor.LocalAnchor == IntPtr.Zero)
        {
            Debug.Log("Didn't get the local anchor...");
            return;
        }
        else
        {
            Debug.Log("Local anchor created");
        }

        // In this sample app we delete the cloud anchor explicitly, but here we show how to set an anchor to expire automatically
        localCloudAnchor.Expiration = DateTimeOffset.Now.AddDays(7);

        // Save anchor to cloud
        while (!cloudManager.IsReadyForCreate)
        {
            await Task.Delay(330);
            float createProgress = cloudManager.SessionStatus.RecommendedForCreateProgress;
        }

        bool success;

        try
        {
            Debug.Log("Creating Azure anchor... please wait...");

            // Actually save
            await cloudManager.CreateAnchorAsync(localCloudAnchor);

            // Store
            currentCloudAnchor = localCloudAnchor;
            localCloudAnchor = null;

            // Success?
            success = currentCloudAnchor != null;

            if (success)
            {
                Debug.Log($"Azure anchor with ID '{currentCloudAnchor.Identifier}' created successfully");

                // Notify AnchorFeedbackScript
                //OnCreateAnchorSucceeded?.Invoke();

                // Update the current Azure anchor ID
                Debug.Log($"Current Azure anchor ID updated to '{currentCloudAnchor.Identifier}'");
                //currentAzureAnchorID = currentCloudAnchor.Identifier;
            }
            else
            {
                Debug.Log($"Failed to save cloud anchor with ID to Azure");

                // Notify AnchorFeedbackScript
                //OnCreateAnchorFailed?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex.ToString());
        }
    }


    public void LocateNearByAnchors(CloudSpatialAnchor anchor)
    {
        
        

        NearAnchorCriteria nearAnchorCriteria = new NearAnchorCriteria();
        nearAnchorCriteria.SourceAnchor = anchor;
        nearAnchorCriteria.DistanceInMeters = 10;
        nearAnchorCriteria.MaxResultCount = 2; // max anchor to find
        anchorLocateCriteria.NearAnchor = nearAnchorCriteria;
        
        if ((cloudManager != null) && (cloudManager.Session != null))
        {
            currentWatcher = cloudManager.Session.CreateWatcher(anchorLocateCriteria);
            Debug.Log("Watcher created");
            Debug.Log("Looking for Azure anchor... please wait...");
        }
        else
        {
            Debug.Log("Attempt to create watcher failed, no session exists");
            currentWatcher = null;
        }
    }

}
