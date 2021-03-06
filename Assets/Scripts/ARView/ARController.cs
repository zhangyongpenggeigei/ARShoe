﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using GoogleARCore;
using static AndroidController;
#if UNITY_EDITOR
// Set up touch input propagation while using Instant Preview in the editor.
using Input = GoogleARCore.InstantPreviewInput;
#endif

/// <summary>
/// Control ar view class.
/// </summary>
public class ARController : MonoBehaviour
{
    #region PUBLIC_MEMBERS
    /// <summary>
    /// The main camera being used to render the passthrough camera image (i.e. AR background).
    /// </summary>
    public Camera mainCamera;

    /// <summary>
    /// A prefab for tracking and visualizing detected planes.
    /// </summary>
    public GameObject detectedPlanePrefab;
    public GameObject indicators;
    public GameObject shadowPlaneIndicator;
    public GameObject rotationIndicator;
    public GameObject translationIndicator;
    public GameObject defaultIndicator;
    public GameObject planeGenerator;
    public GameObject pointCloud;
    #endregion

    #region PRIVATE_MEMBERS
    /// <summary>
    /// A list to hold all planes ARCore is tracking in the current frame. This object is used across
    /// the application to avoid per-frame allocations.
    /// </summary>
    List<DetectedPlane> m_AllPlanes = new List<DetectedPlane>();
    GroundPlaneUI m_GroundPlaneUI;
    ShoeController m_ShoeController;
    /// <summary>
    /// True if the app is in the process of quitting due to an ARCore connection error, otherwise false.
    /// </summary>
    bool m_IsQuitting = false;
    float indicatorHeight = 0.2f;
    float indicatorScale = 0.25f;
    float shadowMovedHeight = 0.15f;
    float shadowFixedHeight = 0.05f;
    float shadowMovedScale = 0.2f;
    float shadowFixedScaled = 0.18f;
    #endregion

    void Start()
    {
        m_GroundPlaneUI = FindObjectOfType<GroundPlaneUI>();
        m_ShoeController = FindObjectOfType<ShoeController>();
        InitializeIndicators();
    }

    /// <summary>
    /// The Unity Update() method.
    /// </summary>
    void Update()
    {
        _UpdateApplicationLifecycle();
        Session.GetTrackables<DetectedPlane>(m_AllPlanes);
        ChangePlanesVisualizer(); // Change visualizing of planes by status of shoe placed. 

        if (m_ShoeController.shoes != null)
        {
            SetIndicators(); // Set indicators children of shoe.

            // If the player has not touched the screen, we are done with this update.
            Touch[] touches = Input.touches;
            if (Input.touchCount < 1 || (touches[0] = Input.GetTouch(0)).phase == TouchPhase.Ended)
            {
                return;
            }

            if (m_ShoeController.IsPlaced)
            {
                m_ShoeController.IsPlaced = false;
                m_GroundPlaneUI.SetShoeMovable();
            }

            // Raycast against the location the player touched to search for planes.
            TrackableHit hit;
            TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon |
                TrackableHitFlags.FeaturePointWithSurfaceNormal;

            if (Input.touches[0].phase == TouchPhase.Ended)
            {
                TouchHandler.isFirstFrameWithTwoTouches = true;
            }

            if (Input.touchCount == 1
                && Frame.Raycast(touches[0].position.x, touches[0].position.y, raycastFilter, out hit))
            {
                TouchHandler.InteractSingleFinger(m_ShoeController.shoes, hit, touches);
            }
            else if (Input.touchCount == 2)
            {
                TouchHandler.InteractDoubleFinger(m_ShoeController.shoes, touches);
            }
        }
    }

    /// <summary>
    /// Initialize indicator's'
    /// </summary>
    void InitializeIndicators()
    {
        indicators.transform.SetParent(m_ShoeController.shoes.transform);
        indicators.transform.localPosition = new Vector3(0, 0, 0);
        indicators.transform.localRotation = Quaternion.Euler(0, 0, 0);
        indicators.transform.localScale = new Vector3(1, 1, 1);
        shadowPlaneIndicator.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        InitializeIndicator(rotationIndicator);
        InitializeIndicator(translationIndicator);
        InitializeIndicator(defaultIndicator);
    }

    /// <summary>
    /// Initialize each indicator.
    /// </summary>
    void InitializeIndicator(GameObject indicator)
    {
        indicator.transform.localRotation = Quaternion.Euler(90f, -90f, 0f);
        indicator.transform.localScale = new Vector3(indicatorScale, indicatorScale, indicatorScale);
    }

    /// <summary>
    /// Set indicators hierachy under shoe object's transform.
    /// </summary>
    void SetIndicators()
    {
        shadowPlaneIndicator.SetActive(m_ShoeController.shoes.activeSelf); // Change shadow activity by shoe's activity.

        if(m_ShoeController.IsPlaced)
        {
            shadowPlaneIndicator.transform.localPosition = new Vector3(0, 0, 0);
#if (UNITY_IOS || !UNITY_ANDROID)
            shadowPlaneIndicator.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            shadowPlaneIndicator.transform.position -= Vector3.up * 0.1f;
#elif UNITY_ANDROID
            shadowPlaneIndicator.transform.localScale = new Vector3(shadowFixedScaled, shadowFixedScaled, shadowFixedScaled);
            shadowPlaneIndicator.transform.position -= Vector3.up * shadowFixedHeight;
#endif
        }
        else 
        {
            shadowPlaneIndicator.transform.localPosition = new Vector3(0, 0, 0);
#if (UNITY_IOS || !UNITY_ANDROID)
            shadowPlaneIndicator.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            shadowPlaneIndicator.transform.position -= Vector3.up * 0.2f;
#elif UNITY_ANDROID
            shadowPlaneIndicator.transform.localScale = new Vector3(shadowMovedScale, shadowMovedScale, shadowMovedScale);
            shadowPlaneIndicator.transform.position -= Vector3.up * shadowMovedHeight;
#endif
        }


        rotationIndicator.SetActive(Input.touchCount == 2 && !m_ShoeController.IsPlaced);
        if (rotationIndicator.activeSelf)
        {
            rotationIndicator.transform.position = m_ShoeController.shoes.transform.position;
            rotationIndicator.transform.position -= Vector3.up * indicatorHeight;
        }

        translationIndicator.SetActive(Input.touchCount == 1 && !m_ShoeController.IsPlaced && !defaultIndicator.activeSelf);
        if (translationIndicator.activeSelf)
        {
            translationIndicator.transform.position = m_ShoeController.shoes.transform.position;
            translationIndicator.transform.position -= Vector3.up * indicatorHeight;
        }

        defaultIndicator.SetActive(Input.touchCount == 0 && m_ShoeController.shoes.activeSelf && !m_ShoeController.IsPlaced);
        if (defaultIndicator.activeSelf)
        {
            defaultIndicator.transform.position = m_ShoeController.shoes.transform.position;
            defaultIndicator.transform.position -= Vector3.up * indicatorHeight;
        }
    }

    /// <summary>
    // If shoe object is placed, then hide plane visualizer. Else, show plane visualizer.
    /// </summary>
    private void ChangePlanesVisualizer()
    {
        planeGenerator.SetActive(!m_ShoeController.IsPlaced || !m_ShoeController.isActiveAndEnabled);
        pointCloud.SetActive(!m_ShoeController.IsPlaced || !m_ShoeController.isActiveAndEnabled);
    }

    public void DeleteIndicators()
    {
        Destroy(indicators);
    }

    /// <summary>
    /// Check and update the application lifecycle.
    /// </summary>
    void _UpdateApplicationLifecycle()
    {
        // Only allow the screen to sleep when not tracking.
        if (Session.Status != SessionStatus.Tracking)
        {
            const int lostTrackingSleepTimeout = 15;
            Screen.sleepTimeout = lostTrackingSleepTimeout;
        }
        else
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        if (m_IsQuitting)
        {
            return;
        }

        // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
        if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
        {
            _ShowAndroidToastMessage("Camera permission is needed to run this application.");
            m_IsQuitting = true;
            Invoke("_DoQuit", 0.5f);
        }
        else if (Session.Status.IsError())
        {
            _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
            m_IsQuitting = true;
            Invoke("_DoQuit", 0.5f);
        }
    }
}
