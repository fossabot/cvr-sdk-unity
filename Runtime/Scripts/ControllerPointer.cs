using UnityEngine;

//this is attached to a controller
//activates a IPointerFocus component. that component must be on the UI layer
//a line renderer is used to display the direction of the controller

//TODO use inputfeature to automatically configure this

namespace Cognitive3D
{
    /// <summary>
    /// 
    /// </summary>
    [AddComponentMenu("Cognitive3D/Internal/Controller Pointer")]
    public class ControllerPointer : MonoBehaviour, IControllerPointer
    {
        [HideInInspector]
        public bool isRightHand;

        private Material DefaultPointerMat;
        private bool focused;
        private bool isHand;
        private Vector3[] pointsArray;
        private LineRenderer lr;
        private Vector3 lrStartPos;
        private Vector3 lrEndPos;
        private const float LINE_RENDERER_DEFAULT_LENGTH = 20;

#if C3D_OCULUS
        private OVRHand hand;
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isHandPointer"></param>
        /// <param name="controllerTransform"></param>
        /// <returns></returns>
        public LineRenderer ConstructDefaultLineRenderer(bool isHandPointer, Transform controllerTransform)
        {
            lr = gameObject.AddComponent<LineRenderer>();
            lr.widthMultiplier = 0.03f;
            if (DefaultPointerMat == null) { DefaultPointerMat = Resources.Load<Material>("ExitPollPointerLine"); }
            lr.useWorldSpace = isHandPointer;
            pointsArray = new Vector3[2];

            // Hands: use OVRHands and update positions in update based on OVRHand.PointerPose()
            if (isHandPointer) 
            {
        #if C3D_OCULUS
                isHand = true;
                hand = controllerTransform.GetComponentInChildren<OVRHand>();
                return lr;
        #endif
            }
            ResetLineRenderer();
            return lr;
        }

        void Update()
        {
            Vector3 raycastStartPos;
            Vector3 raycastDirection;
            float pinchStrength = 0;
            float confidence = 0;
            if (isHand && hand != null)
            {
                pointsArray[0] = Cognitive3D_Manager.Instance.trackingSpace.TransformPoint(hand.PointerPose.position);
                pointsArray[1] = Cognitive3D_Manager.Instance.trackingSpace.TransformPoint(LINE_RENDERER_DEFAULT_LENGTH * hand.PointerPose.forward);
                raycastStartPos = pointsArray[0];
                raycastDirection = hand.PointerPose.forward;
                lr.SetPositions(pointsArray);
                pinchStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
                confidence = (hand.HandConfidence == OVRHand.TrackingConfidence.High) ? 1 : 0;
            }
            else
            {
                raycastStartPos = transform.position;
                raycastDirection = transform.forward;
            }

            IPointerFocus button = null;
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(raycastStartPos, raycastDirection, out hit, LINE_RENDERER_DEFAULT_LENGTH, LayerMask.GetMask("UI"))) // hit a UI element
            {
                button = hit.collider.GetComponent<IPointerFocus>();
                if (button != null)
                {
                    button.SetPointerFocus(isRightHand, pinchStrength, confidence);
                    pointsArray[1] = hit.point;
                    lr.SetPositions(pointsArray);
                    focused = true;
                    return;
                }
            }

            // If transition from "focused" state to
            // hitting nothing or non-button
            if (focused)
            {
                ResetLineRenderer();
                focused = false;
            }
        }

        private void ResetLineRenderer()
        {
            if (isHand)
            {
                lrStartPos = Cognitive3D_Manager.Instance.trackingSpace.TransformPoint(hand.PointerPose.position);
                lrEndPos = Cognitive3D_Manager.Instance.trackingSpace.TransformPoint(LINE_RENDERER_DEFAULT_LENGTH * hand.PointerPose.forward);
            }
            else
            {
                lrStartPos = transform.position;
                lrEndPos = lrStartPos + LINE_RENDERER_DEFAULT_LENGTH * transform.forward;
            }
            pointsArray[0] = lrStartPos;
            pointsArray[1] = lrEndPos;
            lr.material = DefaultPointerMat;
            lr.textureMode = LineTextureMode.Tile;
            lr.SetPositions(pointsArray);
        }
    }
}