using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace ARMarkersDataManager
{
    /// <summary>
    /// Class that defines the relationships between the map frame and ARmarkers (anchors, ARobjects and User position)
    /// </summary>
    [Serializable]
    public class ARmarkersContainer
    {
        public string name;
        // position of the anchor frame w.r.t. the map one 
        public float posX;
        public float posY;
        public float posZ;
        // orientation (Euler Angles) of the anchor frame w.r.t. the map one
        public float rotX;
        public float rotY;
        public float rotZ;
        // position of the user w.r.t. the map frame
        public float userX;
        public float userY;
        public float userZ;


        public void SetAnchorPosition(Vector3 position)
        {
            this.posX = position.x;
            this.posY = position.y;
            this.posZ = position.z;
        }

        public void SetAnchorOrientation(Quaternion orientation)
        {
            this.rotX = orientation.eulerAngles.x;
            this.rotY = orientation.eulerAngles.y;
            this.rotY = orientation.eulerAngles.z;
        }

        /*public override bool Equals(object obj)
        {
            if (!(obj is Vector3S))
            {
                return false;
            }

            var s = (Vector3S)obj;
            return x == s.x &&
                   y == s.y &&
                   z == s.z;
        }

        public override int GetHashCode()
        {
            var hashCode = 373119288;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            return hashCode;
        }*/

        public Vector3 GetAnchorPosition()
        {
            return new Vector3(posX, posY, posZ);
        }

        public Quaternion GetAchorOrientation()
        {
            Quaternion rot = Quaternion.identity;
            rot.eulerAngles = new Vector3(rotX, rotY, rotZ);
            return rot;
        }
        /*
        public static bool operator ==(Vector3S a, Vector3S b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z;
        }

        public static bool operator !=(Vector3S a, Vector3S b)
        {
            return a.x != b.x && a.y != b.y && a.z != b.z;
        }

        public static implicit operator Vector3(Vector3S x)
        {
            return new Vector3(x.x, x.y, x.z);
        }

        public static implicit operator Vector3S(Vector3 x)
        {
            return new Vector3S(x.x, x.y, x.z);
        }*/



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

    }
}
