using UnityEngine;
using TrackIRUnity;

namespace TIR
{
    [System.Serializable]
    public struct Limit
    {
        public float Lower;
        public float Upper;
    }

    public class TrackIRTransform : MonoBehaviour
    {
        [Tooltip("Transform for which to apply the TrackIR position and rotation. All tracking will be done " +
            "relative to the ")]
        public Transform trackedObject = null;

        [Space]
        [Tooltip("Multiplier to apply to the raw TrackIR position data. Default value has been calibrated to " +
            "approximate the numbers in the \"Game Column\" as seen in the TrackIR application.")]
        public float positionMultiplier = 0.000031f;

        [Tooltip("Multiplier to apply to the raw TrackIR rotation data. Default value has been calibrated to " +
            "approximate the numbers in the \"Game Column\" as seen in the TrackIR application.")]
        public float rotationMultiplier = 0.011f;

        [Header("Limits")]
        [Tooltip("Use the limits specified below put constraints on rotation and position.")]
        public bool useLimits = false;
        public Limit positionX = new Limit();
        public Limit positionY = new Limit();
        public Limit positionZ = new Limit();
        public Limit yawLimits = new Limit();
        public Limit pitchLimits = new Limit();
        public Limit rollLimits = new Limit();

        [Header("Debugging")]
        [Tooltip("Show the debug buttons for starting/stopping tracking and various debug texts.")]
        public bool isGUIVisible = true;
        [Tooltip("Rect to draw the debug text in.")]
        public Rect statusRect = new Rect(10, 90, 300, 200);
        [Tooltip("Rect to draw the debug text in.")]
        public Rect dataRect = new Rect(10, 295, 300, 200);

        private TrackIRClient trackIRclient = null;

        private string status = "";
        private string data = "";

        private bool isRunning = false;

        private Vector3 startPosition = Vector3.zero;
        private Quaternion startRotation = Quaternion.identity;

        private void Awake()
        {
            // Create an instance of the TrackerIR Client to get data from
            trackIRclient = new TrackIRClient();
            status = "";
            data = "";
        }

        private void OnEnable()
        {
            StartTracking();
        }

        private void OnDisable()
        {
            StopTracking();
        }

        // Update is called once per frame
        private void Update()
        {
            if (isRunning)
            {
                // Data for debugging output, can be removed if not debugging/testing
                data = trackIRclient.client_TestTrackIRData();

                // Data for head tracking
                TrackIRClient.LPTRACKIRDATA tid = trackIRclient.client_HandleTrackIRData();

                // Updates main camera, change to whatever
                Vector3 localPos = trackedObject.localPosition;
                Vector3 localEulers = trackedObject.localRotation.eulerAngles;

                if (!useLimits)
                {
                    localPos.x = -tid.fNPX * positionMultiplier;
                    localPos.y = tid.fNPY * positionMultiplier;
                    localPos.z = -tid.fNPZ * positionMultiplier;

                    localEulers.y = -tid.fNPYaw * rotationMultiplier;
                    localEulers.x = tid.fNPPitch * rotationMultiplier;
                    localEulers.z = tid.fNPRoll * rotationMultiplier;
                }
                else
                {
                    localPos.x = Mathf.Clamp(-tid.fNPX * positionMultiplier, positionX.Lower, positionX.Upper);
                    localPos.y = Mathf.Clamp(tid.fNPY * positionMultiplier, positionY.Lower, positionY.Upper);
                    localPos.z = Mathf.Clamp(-tid.fNPZ * positionMultiplier, positionZ.Lower, positionZ.Upper);

                    localEulers.y = Mathf.Clamp(-tid.fNPYaw * rotationMultiplier, yawLimits.Lower, yawLimits.Upper);
                    localEulers.x = Mathf.Clamp(tid.fNPPitch * rotationMultiplier, pitchLimits.Lower, pitchLimits.Upper);
                    localEulers.z = Mathf.Clamp(tid.fNPRoll * rotationMultiplier, rollLimits.Lower, rollLimits.Upper);
                }

                trackedObject.localRotation = startRotation * Quaternion.Euler(localEulers);
                trackedObject.localPosition = startPosition + localPos;
            }
        }

        private void OnGUI()
        {
            if (isGUIVisible)
            {
                // Testing GUI
                if (GUI.Button(new Rect(10, 10, 100, 25), "Start"))
                    StartTracking();
                if (GUI.Button(new Rect(10, 35, 100, 25), "Shutdown"))
                    StopTracking();
                if (GUI.Button(new Rect(10, 60, 100, 25), "Reset"))
                    ResetTracking();

                GUI.TextArea(statusRect, status);
                GUI.TextArea(dataRect, data);
            }
        }

        public void ResetTracking()
        {
            StopTracking();
            StartTracking();
        }

        public void StartTracking()
        {
            if (trackedObject == null)
            {
                Debug.LogError(name + ": Attempted to start TrackIR tracking without an assigned Tracked Object");
                return;
            }

            if (!isRunning)
            {
                // Start tracking
                status = trackIRclient.TrackIR_Enhanced_Init();
                isRunning = true;

                startPosition = trackedObject.localPosition;
                startRotation = trackedObject.localRotation;
            }
        }

        public void StopTracking()
        {
            if (isRunning)
            {
                // Stop tracking
                status = trackIRclient.TrackIR_Shutdown();
                isRunning = false;

                trackedObject.localPosition = startPosition;
                trackedObject.localRotation = startRotation;
            }
        }
    }
}
