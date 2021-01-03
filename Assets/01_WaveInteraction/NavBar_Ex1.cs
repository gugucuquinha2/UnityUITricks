﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// This class represents a dynamic and automated navigation system, where you can change menu options/screens dynamically.
/// </summary>
// This system makes use of the DOTween plugin for the animation. In replacement, Coroutines can be used to make the animations.
public class NavBar_Ex1 : MonoBehaviour
{
    [Header("-- CONTAINERS --")]
    [SerializeField]
    private GameObject canvas;
    private GameObject overlayCanvas;
    [SerializeField]
    private RectTransform groupContainer;
    [SerializeField]
    private RectTransform container;

    [Space]
    [Header("-- BUTTON ELEMENTS --")]
    [SerializeField]
    private RectTransform[] menuBtns;
    [SerializeField]
    private Image[] menuImgs;
    [SerializeField]
    private RectTransform selectionIcon;

    [Space]
    [Header("-- ANIMATION ELEMENTS --")]
    [SerializeField]
    private Color32 selectedColor;
    [SerializeField]
    private Color32 deselectedColor;
    [SerializeField]
    private AnimationCurve curve;
    private Sequence menuSeq;
    private Tween animIconTween;

    // positioning variables
    private float initialImgPosY, selectedImgPosY;
    private float selectedIconDefPosY;
    private int curMenuIndex = 0;

    private void Start()
    {
        // activate the canvas and update the layout group
        canvas.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(groupContainer);

        // default values
        SetDefaultVars();

        // display the main menu
        DisplayMenuForFirstTime();
    }

    /// <summary>
    /// Displays the Navigation bar for the first time.
    /// </summary>
    private void DisplayMenuForFirstTime()
    {
        // animation
        if (menuSeq != null)
            menuSeq.Kill();

        // the duration of the animation
        float dur = 0.5f;

        menuSeq = DOTween.Sequence();
        menuSeq.Append(menuImgs[curMenuIndex].rectTransform.DOAnchorPosY(selectedImgPosY, dur).SetEase(Ease.OutCubic));
    }

    /// <summary>
    /// Update the current selected option from the navigation bar.
    /// </summary>
    /// <param name="_targetMenuIndex">The target menu index (starts at 0).</param>
    public void ChangeMenu(int _targetMenuIndex)
    {
        // if we're already in this option, we ignore the animation logic
        if (_targetMenuIndex == curMenuIndex)
            return;

        // menu animation
        if (menuSeq != null)
            menuSeq.Kill();

        // the duration of the animation
        float dur = 0.5f;

        menuSeq = DOTween.Sequence();
        menuSeq.Append(selectionIcon.DOAnchorPosX(menuBtns[_targetMenuIndex].anchoredPosition.x, dur).SetEase(Ease.OutCubic));

        // ICONS WAVE ANIMATION

        // Nav bar options icons animation
        for (int i = 0; i < menuImgs.Length; i++)
        {
            if(i != _targetMenuIndex)
            {
                menuSeq.Join(menuImgs[i].rectTransform.DOAnchorPosY(initialImgPosY, dur).SetEase(Ease.OutCubic))
                       .Join(menuImgs[i].DOColor(deselectedColor, dur).SetEase(Ease.OutCubic));
            }
        }

        int optionsIndexDifference = _targetMenuIndex - curMenuIndex;
        int absoluteDifference = Mathf.Abs(optionsIndexDifference);

        // that means we're jumping more than one option (example: going from the menu index 0 to 1 or above)
        if (absoluteDifference > 1)
        {
            // get the duration of the "jump" animation for each option
            // this will give us the duration for the animation on each option before it reaches the target
            float splitDur = (dur / (absoluteDifference));

            // when going to the right
            if (optionsIndexDifference > 0)
            {
                for (int i = curMenuIndex + 1; i < _targetMenuIndex; i++)
                {
                    int durMultiplier = i - (curMenuIndex + 1); // starts at 0
                    // will only start animating for the current option after the animation of the previous one is halfway
                    menuSeq.Insert((splitDur * 0.5f) * durMultiplier, menuImgs[i].rectTransform.DOAnchorPosY(selectedImgPosY * 0.8f, dur).SetEase(curve));
                }
            }
            // when going to the left
            else if (optionsIndexDifference < 0)
            {
                for (int i = curMenuIndex - 1; i > _targetMenuIndex; i--)
                {
                    int durMultiplier = (curMenuIndex - 1) - i; // starts at 0
                    // will only start animating for the current option after the animation of the previous one is halfway
                    menuSeq.Insert((splitDur * 0.5f) * durMultiplier, menuImgs[i].rectTransform.DOAnchorPosY(selectedImgPosY * 0.8f, dur).SetEase(curve));
                }
            }

            // finally animate the target option
            menuSeq.Insert(dur * 0.25f, menuImgs[_targetMenuIndex].rectTransform.DOAnchorPosY(selectedImgPosY, dur).SetEase(Ease.OutCubic));
        }
        else
        {
            menuSeq.Insert(dur * 0.25f, menuImgs[_targetMenuIndex].rectTransform.DOAnchorPosY(selectedImgPosY, dur).SetEase(Ease.OutCubic));
        }

        // finally animate the target option
        menuSeq.Join(menuImgs[_targetMenuIndex].DOColor(selectedColor, dur).SetEase(Ease.OutCubic));

        // updates the current menu index we're at
        curMenuIndex = _targetMenuIndex;
    }

    /// <summary>
    /// Set and declare variables at the beginning of the game.
    /// </summary>
    private void SetDefaultVars()
    {
        // current menu option
        curMenuIndex = 0;

        // positioning variables
        initialImgPosY = menuImgs[0].rectTransform.anchoredPosition.y;
        selectedImgPosY = menuImgs[0].rectTransform.rect.height * 0.28f;
        selectedIconDefPosY = container.rect.height * 0.3f;

        // selected icon position
        selectionIcon.anchoredPosition = new Vector2(menuBtns[0].anchoredPosition.x, selectedIconDefPosY);

        // menu options setup
        for (int i = 0; i < menuImgs.Length; i++)
        {
            if (i != curMenuIndex)
                menuImgs[i].color = deselectedColor;

            int index = i;

            Button b = menuBtns[i].GetComponent<Button>();
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(delegate { ChangeMenu(index); });
        }
    }
}
