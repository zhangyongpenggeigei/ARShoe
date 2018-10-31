﻿/*==============================================================================
Copyright (c) 2018 PTC Inc. All Rights Reserved.

Vuforia is a trademark of PTC Inc., registered in the United States and other
countries.
==============================================================================*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GroundPlaneUI : MonoBehaviour
{
    #region PUBLIC_MEMBERS
    [Header("UI Buttons")]
    public Button m_ResetButton;
    public Button m_ConfirmButton;
    public Button m_BackButton;
    public Button m_CaptureButton;
    public Button m_ListUpDown;
    public Button m_ShoeLeftRightTextButton;
    public Button m_SceneChangeButton;
    public Button m_HeartButton;
    public Button m_SocialShareButton;
    public Button m_BuyButton;

    [Header("UI Panels")]
    public RectTransform m_CustomListRectTransform;
    public RectTransform m_MidToolbarTectTrnasform;
    #endregion // PUBLIC_MEMBERS


    #region PRIVATE_MEMBERS
    ARController m_ARController;
    AudioSource shoePuttingSound;
    GraphicRaycaster[] m_GraphicRayCasters;
    PointerEventData m_PointerEventData;
    EventSystem m_EventSystem;
    #endregion // PRIVATE_MEMBERS


    #region MONOBEHAVIOUR_METHODS
    void Start()
    {
        m_ARController = FindObjectOfType<ARController>();
        m_GraphicRayCasters = FindObjectsOfType<GraphicRaycaster>();
        m_EventSystem = FindObjectOfType<EventSystem>();
        InitializeButtons();
        ChangeButtonStatus();
        // ToDo: If the shoe can custom, then show list up/down button, else hide list up/down button.
    }

    void Update()
    {
        ChangeButtonStatus();
        // If user click android back button, then call click back button method.
        if (Application.platform == RuntimePlatform.Android)
        {
            if (!ScreenshotPreview.previewGameObject.activeSelf && Input.GetKey(KeyCode.Escape))
            {
                ClickBackButton();
            }
            else if (ScreenshotPreview.previewGameObject.activeSelf && Input.GetKey(KeyCode.Escape))
            {
                ScreenshotPreview.previewGameObject.SetActive(false);
            }
        }
    }

    void InitializeButtons()
    {
        m_BackButton.onClick.AddListener(ClickBackButton);
        m_ConfirmButton.onClick.AddListener(ClickConfirmButton);
        m_ResetButton.onClick.AddListener(ClickResetButton);
        m_CaptureButton.onClick.AddListener(ClickCaptureButton);
        m_ListUpDown.onClick.AddListener(ClickListUpDownButton);
        m_ShoeLeftRightTextButton.onClick.AddListener(ClickShoeLeftRightTextButton);
        m_SceneChangeButton.onClick.AddListener(ClickSceneChangeButton);
        m_HeartButton.onClick.AddListener(ClickHeartButton);
        m_SocialShareButton.onClick.AddListener(ClickSocialShareButton);
        m_BuyButton.onClick.AddListener(ClickBuyButton);
        m_ResetButton.interactable = m_ConfirmButton.interactable = false;
        m_ResetButton.image.enabled = m_ConfirmButton.image.enabled = true;
    }

    void ClickBackButton()
    {
        CurrentCustomShoe.shoe.GetComponent<Swiper>().enabled = true;
        SceneChanger.ChangeToShoeListScene();
    }

    void ClickConfirmButton()
    {
        if (m_ConfirmButton.image.enabled)
        {
            SetShoeStopped();
        }
    }

    private void SetShoeStopped()
    {
        m_ARController.IsPlaced = true;
        m_ARController.FixShoe();
        m_ConfirmButton.image.enabled = false;
        ChangeButtonStatus();
    }

    void ClickResetButton()
    {
        m_ARController.ResetAR();
        ChangeButtonStatus();
    }

    void ClickCaptureButton()
    {
        StartCoroutine(ScreenshotPreview.CaptureAndShowPreviewImage()); // Start coroutine for screenshot function.
    }

    void ClickListUpDownButton()
    {
        Vector2 originalPanelVector = m_CustomListRectTransform.sizeDelta;
        Vector2 goalPanelVector;
        Vector2 originalToolbarVector = m_MidToolbarTectTrnasform.anchoredPosition;
        Vector2 goalToolbarVector;
        if (m_ListUpDown.image.sprite.name.Equals("up-arrow"))
        {
            m_ListUpDown.image.sprite = Resources.Load<Sprite>("Sprites/Icons/down-arrow");
            goalPanelVector = new Vector2(m_CustomListRectTransform.sizeDelta.x, 300f);
            goalToolbarVector = new Vector2(m_MidToolbarTectTrnasform.anchoredPosition.x, m_MidToolbarTectTrnasform.anchoredPosition.y + 300f);
            StartCoroutine(ExtendOrShrinkHeight(originalPanelVector, goalPanelVector, originalToolbarVector, goalToolbarVector));
        }
        else
        {
            m_ListUpDown.image.sprite = Resources.Load<Sprite>("Sprites/Icons/up-arrow");
            goalPanelVector = new Vector2(m_CustomListRectTransform.sizeDelta.x, 0f);
            goalToolbarVector = new Vector2(m_MidToolbarTectTrnasform.anchoredPosition.x, m_MidToolbarTectTrnasform.anchoredPosition.y - 300f);
            StartCoroutine(ExtendOrShrinkHeight(originalPanelVector, goalPanelVector, originalToolbarVector, goalToolbarVector));
        }
    }

    IEnumerator<RectTransform> ExtendOrShrinkHeight(Vector2 originalPanelVector, Vector2 goalVector, Vector2 originalToolbarVector, Vector2 goalToolbarVector)
    {
        float currentTime = 0f;
        float timeOver = 0.3f;

        while (currentTime < timeOver)
        {
            currentTime += Time.deltaTime;
            float normalizedValue = currentTime / timeOver; // we normalize our time 

            m_CustomListRectTransform.sizeDelta = Vector2.Lerp(originalPanelVector, goalVector, normalizedValue);
            m_MidToolbarTectTrnasform.anchoredPosition = Vector2.Lerp(originalToolbarVector, goalToolbarVector, normalizedValue);
            yield return null;
        }
    }

    void ClickShoeLeftRightTextButton()
    {
        if (m_ShoeLeftRightTextButton.GetComponent<Text>().text.Equals("R"))
        {
            m_ShoeLeftRightTextButton.GetComponent<Text>().text = "L";
            // ToDo: Change shoe right to left.
        }
        else
        {
            m_ShoeLeftRightTextButton.GetComponent<Text>().text = "R";
            // ToDo: Change shoe left to right.
        }
    }

    void ClickSceneChangeButton()
    {
        SceneChanger.ChangeToAttachShoes();
    }

    void ClickHeartButton()
    {
        if (m_HeartButton.image.sprite.name.Equals("UI_Icon_HeartEmpty"))
        {
            m_HeartButton.image.sprite = Resources.Load<Sprite>("Sprites/Icons/UI_Icon_Heart");
            ColorBlock colorBlock = m_HeartButton.colors;
            colorBlock.highlightedColor = new Color32(0, 164, 255, 255);
            m_HeartButton.colors = colorBlock;
            // ToDo: Save Changed info.
        }
        else
        {
            m_HeartButton.image.sprite = Resources.Load<Sprite>("Sprites/Icons/UI_Icon_HeartEmpty");
            ColorBlock colorBlock = m_HeartButton.colors;
            colorBlock.highlightedColor = new Color32(255, 255, 255, 255);
            m_HeartButton.colors = colorBlock;
            // ToDo: Save Changed info.
        }
    }

    void ClickSocialShareButton()
    {
        // ToDo: Get url of shop.
        #if UNITY_ANDROID
        new NativeShare().SetTitle("Title").SetText("text").Share();
        #elif UNITY_IOS
        new NativeShare().SetTitle("Title").SetText("text").Share();
        #endif
    }

    void ClickBuyButton() {
        // ToDo: Shoe webview.
    }

    public void SetShoeMovable()
    {
        m_ARController.IsPlaced = false;
        m_ARController.MoveShoe();
        m_ConfirmButton.image.enabled = true;
        ChangeButtonStatus();
    }

    // Change button's clickability and visualization.
    // Return true: If shoe object does not placed and vuforia detect floor, or shoe object placed.
    public void ChangeButtonStatus() {
        m_ResetButton.interactable = m_CaptureButton.interactable = m_ConfirmButton.interactable = m_ARController.DoesShoeActive;
        m_ConfirmButton.image.enabled = m_ARController.DoesShoeActive && !m_ARController.IsPlaced;
    }
#endregion // MONOBEHAVIOUR_METHODS
}
