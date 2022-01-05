/*
 * Script that allows to serialize transformations of game objects (position and orientation)
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ARMarkersDataManager
{
    public enum ARtype
    {
        UserPose,
        Alert,
        SurvivorPose
    }

  /*  public class ARmarker
    {



        private string name;
        private ARtype type;
        private Vector3 position;
        private Quaternion orientation;

        public string Name { get { return name; } set { name = value; } }
        public ARtype Type { get { return type; } set { type = value; } }
        public Vector3 Position { get { return position; } set { position = value; } }
        public Quaternion Orientation { get { return orientation; } set { orientation = value; } }

        public ARmarker(string name, ARtype type)
        {
            this.name = name;
            this.type = type;
        }
        public ARmarker(string name, ARtype type, Vector3 position, Quaternion orientation)
        {
            this.name = name;
            this.type = type;
            this.position = position;
            this.orientation = orientation;
        }
    }*/

    /// <summary>
    /// Class that defines the relationships between the map frame and ARmarkers (anchors, ARobjects and User position)
    /// </summary>
    [Serializable]
    public class ARmarkersContainer
    {

        public string mainAnchorId;
        // position of the anchor frame w.r.t. the map one 
        public float MapPosX;
        public float MapPosY;
        public float MapPosZ;
        // orientation (Euler Angles) of the anchor frame w.r.t. the map one
        public float MapRotX;
        public float MapRotY;
        public float MapRotZ;
        // position of the user w.r.t. the map frame
        public float userX;
        public float userY;
        public float userZ;

        //public List<ARmarker> ARmarkerList = new List<ARmarker>();

        const int maxNumberOfARmarkers = 5;

        public string[] names = new string[maxNumberOfARmarkers];
        public int[] type = new int[maxNumberOfARmarkers];
        public float[] ARposX = new float[maxNumberOfARmarkers];
        public float[] ARposY = new float[maxNumberOfARmarkers];
        public float[] ARposZ = new float[maxNumberOfARmarkers];
        public float[] ARrotX = new float[maxNumberOfARmarkers];
        public float[] ARrotY = new float[maxNumberOfARmarkers];
        public float[] ARrotZ = new float[maxNumberOfARmarkers];

        int ARmarkerCount = 0;
        public int ARmarkerStored = 0;


        public void SetMapPosition(Vector3 position)
        {
            this.MapPosX = position.x;
            this.MapPosY = position.y;
            this.MapPosZ = position.z;
        }

        public void SetMapOrientation(Quaternion orientation)
        {
            this.MapRotX = orientation.eulerAngles.x;
            this.MapRotY = orientation.eulerAngles.y;
            this.MapRotZ = orientation.eulerAngles.z;
        }


        public Vector3 GetMapPosition()
        {
            return new Vector3(MapPosX, MapPosY, MapPosZ);
        }

        public Quaternion GetMapOrientation()
        {
            return Quaternion.Euler(MapRotX, MapRotY, MapRotZ);
        }


        /// <summary>
        /// Class to serialize and deserialize  ARMarkerType objects 
        /// </summary>


        public byte[] ARDataBinarySerialize(object data)
        {
            if (data == null)
            {
                Debug.LogError("The object to serialized is null");
                return null;
            }
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, data);
                return ms.ToArray();
            }
        }

        // Convert a byte array to an Object
        public ARmarkersContainer ARDataBinaryDeserialize(byte[] arrBytes)
        {
            if (arrBytes == null)
                Debug.LogError("bayte varible is null");
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            ARmarkersContainer obj = (ARmarkersContainer)binForm.Deserialize(memStream);

            return obj;
        }

        /// <summary>
        /// Store all AR markers data present in the scene. They are stroed in the ARmarkerList
        /// </summary>
        public void storeARmarkers(string mainAnchorName)
        {
            mainAnchorId = mainAnchorName;

            if (GameObject.FindGameObjectsWithTag("alert") == null)
            {
                Debug.Log("no alert AR markers found");
                return;
            }

            foreach (GameObject g in GameObject.FindGameObjectsWithTag("alert"))
            {
                Debug.Log("storing AR markers called:" + g.name);
                //ARmarker ARm = new ARmarker(g.name, ARtype.Alert , g.transform.localPosition, g.transform.localRotation);
                //ARmarkerList.Add(ARm);
                names[ARmarkerCount] = g.name;
                type[ARmarkerCount] = 1;
                g.transform.parent = GameObject.Find("CloudDataManager").transform;
                ARposX[ARmarkerCount] = g.transform.localPosition.x;
                ARposY[ARmarkerCount] = g.transform.localPosition.y;
                ARposZ[ARmarkerCount] = g.transform.localPosition.z;
                ARrotX[ARmarkerCount] = g.transform.localRotation.eulerAngles.x;
                ARrotY[ARmarkerCount] = g.transform.localRotation.eulerAngles.y;
                ARrotZ[ARmarkerCount] = g.transform.localRotation.eulerAngles.z;
                g.transform.parent = GameObject.Find("SceneRoot").transform;
                ARmarkerCount++;
                ARmarkerStored++;
            }
            Debug.Log(ARmarkerCount + " AR markers stored");
            ARmarkerCount = 0;
        }

        /// <summary>
        /// Locate all the ARmarkers present in the bunary file
        /// </summary>
        /// <param name="container"></param>
        /*public void placeDownladedMarkers(ARmarkersContainer container)
        {
            
            foreach (ARmarker m in container.ARmarkerList)
            {
                if (m.Type == ARtype.Alert)
                {
                    GameObject alert = Instantiate<GameObject>(alertPrefab);
                    alert.transform.parent = GameObject.Find("CloudDataManager").transform;
                    alert.transform.position = m.Position;
                    alert.transform.rotation = m.Orientation;
                    alert.name = "alert_" + alert.transform.position.x + "_" + alert.transform.position.y + "_" + alert.transform.position.z;
                    alertCount++;
                }
            }
            alertCount = 0; 
        }*/

    }
}
