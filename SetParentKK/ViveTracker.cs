using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;

namespace SetParentKK
{
    public class ViveTracker : MonoBehaviour
    {
        public void Init(SteamVR_ControllerManager _TrackersManager, SetParent _SetParentObj)
        {
            TrackersManager = _TrackersManager;
            SetParentObj = _SetParentObj;
        }

        public void Start()
        {
            //Create the cube
            TrackerCube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            Tracker.transform.parent = SetParentObj.cameraEye.transform.parent;
            SteamVR_TrackedObject MyTrackedObject = Tracker.AddComponent<SteamVR_TrackedObject>() as SteamVR_TrackedObject;
            int TrackerIndex = (int)FindTrackerIndex();
            MyTrackedObject.SetDeviceIndex(TrackerIndex);
            TrackersManager.objects.SetValue(Tracker, TrackerIndex);


            //Attach Cube to tracker
            TrackerCube.transform.position = Tracker.transform.position;
            TrackerCube.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
        }

        public void LateUpdate()
        {
            //Update Trackercube
            TrackerCube.transform.position = Tracker.transform.position;
            TrackerCube.transform.rotation = Tracker.transform.rotation;
            TrackerCube.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);

        }

        uint FindTrackerIndex()
        {
            uint index = 0;
            var error = ETrackedPropertyError.TrackedProp_Success;
            for (uint i = 0; i < 16; i++)
            {
                bool indexTaken = false;

                var result = new System.Text.StringBuilder((int)64);
                OpenVR.System.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_RenderModelName_String, result, 64, ref error);
                if (result.ToString().Contains("tracker"))
                {
                    foreach (uint j in SetParentObj.FoundTrackerIndices)
                    {
                        if (j == i)
                        {
                            indexTaken = true;
                        }
                    }
                    if (!indexTaken)
                    {
                        index = i;
                        SetParentObj.FoundTrackerIndices.Add(index);
                        return index;
                    }
                }
            }
            return 0;
        }

        SetParent SetParentObj;

        internal GameObject Tracker = new GameObject("MyTracker");
        internal GameObject TrackerCube;
        internal SteamVR_ControllerManager TrackersManager;
    }
}
