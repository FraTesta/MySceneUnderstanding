using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;


#if WINDOWS_UWP
using Windows.Storage;
#endif



public class performanceTest : MonoBehaviour
{
    const int maxBufferSize = 500;

    int frameCount = 0;
    float dt = 0.0f;
    float fps = 0.0f;
    float updateRate = 4;  // 4 updates per sec.
    float[] fpsArray = new float[maxBufferSize];
    int arrayCounter = 0;

    // vertices count variables
    [SerializeField]
    public GameObject root = null;
    const int computingTime = 15; // update ogni 15 secondi
    int timeInterval = 0;
    int totalVertex = 0;
    int[] totalVertexList = new int[maxBufferSize];


    void Update()
    {
        frameCount++;
        dt += Time.deltaTime;
        if (dt > 1.0f / updateRate)
        {
            fps = frameCount / dt;
            frameCount = 0;
            dt -= 1.0f / updateRate;
            if (fps < 10)
                fps = 10;

            fpsArray[arrayCounter] = fps;
            if (arrayCounter == maxBufferSize -1 )
            {
                arrayCounter = 0;
                Debug.Log("saving fps ...");
                savePerformanceOnDisk();
                Debug.Log("save vertieces ...");
                saveVertices();
            }
            arrayCounter++;

            totalVertexList[arrayCounter] = totalVertex;
            timeInterval++;
        }
        if (timeInterval == (updateRate*computingTime))
        {
            timeInterval = 0;
            updateVertices();
            if (totalVertex == 0)
                totalVertex = totalVertexList[arrayCounter - 1]; // saturate error
        }
    }

    private void savePerformanceOnDisk()
    {
        Debug.Log("\nAnchorModuleScript.SaveAzureAnchorIDToDisk()");

        string filename = "performance.txt";
        string path = Application.persistentDataPath;

#if WINDOWS_UWP
                    StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                    path = storageFolder.Path.Replace('\\', '/') + "/";
#endif

        string filePath = Path.Combine(path, filename);
        File.AppendAllLines(filePath, fpsArray.Select(i => i.ToString()).ToArray());

        Debug.Log($"Current fps number '{fps}' successfully saved to path '{filePath}'");
    }

    public void updateVertices()
    {
        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter mf in meshFilters)
        {
            totalVertex += mf.mesh.vertexCount;
        }
        Debug.Log("number of vertices counted");
    }

    private void saveVertices()
    {
        string filename = "vertices.txt";
        string path = Application.persistentDataPath;

#if WINDOWS_UWP
                    StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                    path = storageFolder.Path.Replace('\\', '/') + "/";
#endif

        string filePath = Path.Combine(path, filename);
        File.AppendAllLines(filePath, totalVertexList.Select(i => i.ToString()).ToArray());

        Debug.Log($"Current verteces number '{totalVertex}' successfully saved to path '{filePath}'");
    }

    public void savePerformance()
    {
        savePerformanceOnDisk();
        saveVertices();
        Debug.Log("performance data salved");
    }

}