using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// THIS SCRIPT'S PROPERTIES ARE OPTIMIZED FOR MOBILE USE AND AS SUCH, IT MAY FEEL SLOW OR UNRESPONSIVE WHEN USED ON PC OR WITH A MOUSE.

/// <summary>
/// This script allows to add swipe/drag interaction to UI panels. This specific version is extended to replicate the behaviour of a notifications bar.
/// The speed of the swipe, directly influences the speed and direction of the panel animation.
/// </summary>
public class SwipeAnimation : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // UI VARIABLES
    private GameObject canvas;
    private RectTransform thisRect, canvasRect;
    [SerializeField]
    private TextMeshProUGUI directionText, speedText;

    // CHANGEABLE PROPERTIES
    [Tooltip("The animation curve serving as the 'ease' for the animation.")]
    [SerializeField]
    private AnimationCurve animCurve;
    [Tooltip("The percentage size of the panel's RectTransform, based on the Canvas/Screen size, all state sizes and positions regarding panel sizes will be proportional and derived from this variable.")]
    [SerializeField]
    private float rectPercentageSize = 0.75f;

    [Tooltip("The longest the panel can take to animate, based on the calculated swipe speed.")]
    [SerializeField]
    private float maxAnimDuration = 0.7f;
    [Tooltip("The fastest the panel can take to animate, based on the calculated swipe speed.")]
    [SerializeField]
    private float minAnimDuration = 0.15f;
    [Tooltip("The threshold animation duration, that determines the swipe is fast enough to override states (go from 'closed' to 'expand' or the opposite, overriding the 'open' state.)")]
    [SerializeField]
    private float overrideAnimDuration = 0.2f;
    
    private Vector2 lastPos;
    private float expandedBottomLimitPosition, openBottomLimitPosition;
    private float startTime = 0;
    private float difference;

    // ENUMERATORS
    public enum SwipeDir { none = 0, up = 1, down = 2, left = 3, right = 4 }
    public enum PanelState { none = 0, open = 1, expanded = 2, closed = 3 }

    private PanelState curState = PanelState.none;

    private void Start()
    {
        curState = PanelState.closed;

        canvas = transform.GetComponentInParent<Canvas>().gameObject;
        canvasRect = canvas.GetComponent<RectTransform>();
        thisRect = GetComponent<RectTransform>();

        thisRect.sizeDelta = new Vector2(thisRect.sizeDelta.x, canvasRect.rect.height * rectPercentageSize);
        thisRect.anchoredPosition = new Vector2(thisRect.anchoredPosition.x, thisRect.rect.height * 0.05f);

        // get the center position that separates the "expanded" state from the "open" state
        expandedBottomLimitPosition = (canvasRect.rect.height * rectPercentageSize) - GetPercentageSizeOfRect(4);
        // get the center position that separates the "open" state from the "closed" state
        openBottomLimitPosition = GetPercentageSizeOfRect(4);
    }

    #region EVENTS
    public void OnBeginDrag(PointerEventData eventData)
    {
        // store the difference between the position of the RectTransform and the mouse position
        // this will be as an offset to adjust the Rect position while being dragged
        difference = thisRect.position.y - Input.mousePosition.y;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // update the panel position when its being dragged or swipped
        SetPanelPositionOnSwipe();

        // set the state of the panel based on its positon
        UpdatePanelStateOnSwipe();

        // records latest position before releasing finger/mouse
        lastPos = eventData.position;

        // keeps track of time since the lastPos of the drag
        startTime = Time.time;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // get the Vector regarding the direction of the swipe
        Vector3 swipeDirection = (eventData.position - lastPos).normalized;

        // store the distance of the swipe, its duration so we can calculate its speed
        float dis = Vector2.Distance(lastPos, eventData.position);
        float timeDiff = Time.time - startTime;

        // calculate the duration of the swipe animation applied to the RectTransform based on the speed of the swipe
        float duration = SwipeSpeedToDuration(timeDiff, dis);

        speedText.text = "ANIMATION DURATION: " + duration;

        startTime = 0;

        // get the swipe direction
        switch (GetSwipeDirection(swipeDirection))
        {
            case SwipeDir.down:
                switch (curState)
                {
                    case PanelState.closed:
                        // close the panel
                        ClosePanel(duration);
                        break;
                    default:
                        if(duration < overrideAnimDuration)
                        {
                            // close the panel
                            ClosePanel(duration);
                        }
                        else
                        {
                            // set the panel to open
                            OpenPanel(duration);
                        }
                        break;
                }

                break;
            case SwipeDir.up:
                switch (curState)
                {
                    case PanelState.closed:
                        if (duration < overrideAnimDuration)
                        {
                            // set it to expand
                            ExpandPanel(duration);
                        }
                        else
                        {
                            // set the panel to open
                            OpenPanel(duration);
                        } 
                        break;
                    default:
                        // set it to expand
                        ExpandPanel(duration);
                        break;
                }
                break;
        }
    }
    #endregion

    #region SWIPE MANAGEMENT

    /// <summary>
    /// Define a swipe direction, based on the stored direction Vector from the input event.
    /// </summary>
    /// <returns></returns>
    private SwipeDir GetSwipeDirection(Vector3 _swipeDirection)
    {
        SwipeDir swipeDir;

        // vertical swipe
        if (Mathf.Abs(_swipeDirection.x) <= Mathf.Abs(_swipeDirection.y))
        {
            if (_swipeDirection.y > 0)
            {
                swipeDir = SwipeDir.up;
            }
            else if (_swipeDirection.y < 0)
            {
                swipeDir = SwipeDir.down;
            }
            else
            {
                swipeDir = GetSwipeDirBasedOnFingerPos();
            }           
        }
        else swipeDir = GetSwipeDirBasedOnFingerPos();

        directionText.text = "SWIPE DIRECTION: " + swipeDir.ToString().ToUpper();

        return swipeDir;
    }

    /// <summary>
    /// Whenever there is no swipe, check what the RectTransform position is, so that we can decide where to animate it  
    /// </summary>
    private SwipeDir GetSwipeDirBasedOnFingerPos()
    {
        switch (curState)
        {
            case PanelState.closed:
                return (thisRect.anchoredPosition.y <= openBottomLimitPosition) ? SwipeDir.down : SwipeDir.up;
            default:
                return (thisRect.anchoredPosition.y <= expandedBottomLimitPosition) ? SwipeDir.down : SwipeDir.up;
        }
    }

    /// <summary>
    /// Determine the current state of the panel's RectTransform, solely based on its Y position
    /// </summary>
    private void UpdatePanelStateOnSwipe()
    {
        if (thisRect.anchoredPosition.y > GetPercentageSizeOfRect(2) &&
            thisRect.anchoredPosition.y <= expandedBottomLimitPosition)
        {
            curState = PanelState.open;
        }
        else if (thisRect.anchoredPosition.y > expandedBottomLimitPosition)
        {
            curState = PanelState.expanded;
        }
        else if (thisRect.anchoredPosition.y < GetPercentageSizeOfRect(2))
        {
            curState = PanelState.closed;
        }
    }

    /// <summary>
    /// Setup the position of the panel based on the input position.
    /// </summary>
    private void SetPanelPositionOnSwipe()
    {
        thisRect.position = new Vector2(thisRect.position.x, Input.mousePosition.y + difference);

        float clampedYPos = Mathf.Clamp(thisRect.anchoredPosition.y, 0, thisRect.rect.height);

        thisRect.anchoredPosition = new Vector2(thisRect.anchoredPosition.x, clampedYPos);
    }

    /// <summary>
    /// Converts the swipe speed into animation duration in seconds
    /// </summary>
    private float SwipeSpeedToDuration(float _timeDiff, float _dis)
    {
        float speed = 0;
        if (_timeDiff != 0)
        {
            // calculate the speed (distance divided by the duration of the swipe)
            speed = _dis / _timeDiff;

            // remap the speed values to usable duration values for the animation
            speed = Map(speed, 0, 9000, maxAnimDuration, minAnimDuration);
        }
        // when there's no swipe, we define a default speed for the animation
        else speed = 0.5f;

        return speed;
    }

    #endregion

    #region ANIMATION

    private Coroutine panelAnimCo;

    private void OpenPanel(float _duration)
    {
        if (panelAnimCo != null)
            StopCoroutine(panelAnimCo);
        panelAnimCo = StartCoroutine(OpenPanelAnimation(_duration));
    }
    private IEnumerator OpenPanelAnimation(float _duration)
    {
        curState = PanelState.open;

        float yInit = thisRect.anchoredPosition.y;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / _duration;
            float ease = animCurve.Evaluate(t);

            Vector2 anchorPos = thisRect.anchoredPosition;
            anchorPos.y = Mathf.Lerp(yInit, GetPercentageSizeOfRect(2), ease);
            thisRect.anchoredPosition = anchorPos;

            yield return null;
        }
        panelAnimCo = null;
    }

    private void ExpandPanel(float _duration)
    {
        if (panelAnimCo != null)
            StopCoroutine(panelAnimCo);
        panelAnimCo = StartCoroutine(ExpandPanelAnimation(_duration));
    }
    private IEnumerator ExpandPanelAnimation(float _duration)
    {
        curState = PanelState.expanded;

        float yInit = thisRect.anchoredPosition.y;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / _duration;
            float ease = animCurve.Evaluate(t);

            Vector2 anchorPos = thisRect.anchoredPosition;
            anchorPos.y = Mathf.Lerp(yInit, thisRect.rect.height, ease);
            thisRect.anchoredPosition = anchorPos;

            yield return null;
        }
        panelAnimCo = null;
    }

    private void ClosePanel(float _duration)
    {
        if (panelAnimCo != null)
            StopCoroutine(panelAnimCo);
        panelAnimCo = StartCoroutine(ClosePanelAnimation(_duration));
    }
    private IEnumerator ClosePanelAnimation(float _duration)
    {
        curState = PanelState.closed;

        float yInit = thisRect.anchoredPosition.y;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / _duration;
            float ease = animCurve.Evaluate(t);

            Vector2 anchorPos = thisRect.anchoredPosition;
            anchorPos.y = Mathf.Lerp(yInit, canvasRect.rect.height * 0.05f, ease);
            thisRect.anchoredPosition = anchorPos;

            yield return null;
        }

        panelAnimCo = null;
    }
    #endregion

    #region MISC
    /// <summary>
    /// Map a certain value to a different range
    /// </summary>
    public float Map(float _value, float _from1, float _to1, float _from2, float _to2)
    {
        if (_value <= _from1)
            return _from2;
        else if (_value >= _to1)
            return _to2;
        else return (_value - _from1) / (_to1 - _from1) * (_to2 - _from2) + _from2;
    }

    /// <summary>
    /// Gets the percentage size of the interactable RectTransform, based on a percentage of the size of the screen (represented by the canvas in this case)
    /// </summary>
    /// <param name="_value">The division value</param>
    private float GetPercentageSizeOfRect(int _value)
    {
        return canvasRect.rect.height * (rectPercentageSize / _value);
    }
    #endregion
}
