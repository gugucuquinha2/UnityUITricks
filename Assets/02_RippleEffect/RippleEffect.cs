using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// This class allows the interactive element to have a Ripple Effect, similar to Google Material design. This script should be added as a component on the element we want to be interactive.
/// </summary>
public class RippleEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    // Changeable variables
    [Tooltip("The Ripple image prefab.")]
    [SerializeField]
    private RectTransform ripplePrefab;
    [Tooltip("The parent RectTransform that will contain the Ripple image (The RecTransform containing the 'Mask' component).")]
    [SerializeField]
    private RectTransform parent;
    [SerializeField]
    private bool useRandomColor = false;
    [SerializeField]
    private Color rippleColor;
    [SerializeField]
    private float opacity;

    [SerializeField]
    private float rippleDur = 0.5f, fadeDur = 0.5f;

    // private variables
    private RectTransform rippleRectTransform;
    private RectTransform thisRect;
    private Image rippleImage;
    private Vector2 targetSize;
    private Dictionary<Image, Sequence> dic = new Dictionary<Image, Sequence>();
    private bool bIsPointerDown, bIsAlreadyFading;

    // Start is called before the first frame update
    void Start()
    {
        thisRect = GetComponent<RectTransform>();
        bIsAlreadyFading = false;
        bIsPointerDown = false;
    }

    #region EVENTS
    public void OnPointerDown(PointerEventData eventData)
    {
        // store the position of the interaction
        Vector3 prePos = eventData.pressPosition;
        prePos.z = thisRect.position.z - Camera.main.transform.position.z;
        Vector3 pos = Camera.main.ScreenToWorldPoint(prePos);

        bIsPointerDown = true;

        // instantiate a ripple in the clicked/touched position
        Ripple(pos);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // fade/destroy the ripple
        Fade();
        bIsPointerDown = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Vector3 prePos = eventData.position;
        prePos.z = thisRect.position.z - Camera.main.transform.position.z;
        Vector3 pos = Camera.main.ScreenToWorldPoint(prePos);

        // instantiate a ripple in the position we entered the interactive element (only if we are still interacting with that element)
        Ripple(pos);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // fade/destroy the ripple
        Fade();
    }
    #endregion

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            useRandomColor = !useRandomColor;
    }
#endif

    #region ANIMATION

    private void Ripple(Vector3 _posToTriggerThis)
    {
        // if we're interacting with the element
        if (!bIsPointerDown)
            return;
 
        // call for a new Ripple to be instanced
        SetupRippleProperties(_posToTriggerThis).Append(rippleRectTransform.DOSizeDelta(targetSize, rippleDur).SetEase(Ease.InOutQuad));
    }

    private Sequence SetupRippleProperties(Vector3 _posToTriggerThis)
    {
        bIsAlreadyFading = false;

        // instantiate and setup initial properties of the ripple
        rippleRectTransform = Instantiate(ripplePrefab, parent);
        rippleRectTransform.position = _posToTriggerThis;
        rippleRectTransform.SetSiblingIndex(0);
        rippleRectTransform.sizeDelta = Vector2.zero;
        rippleImage = rippleRectTransform.GetComponent<Image>();

        // are we using a random color or the desired one selected on the inspector?
        if (useRandomColor)
            rippleImage.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
        else rippleImage.color = rippleColor;

        // add this newly created ripple to a dictionary (several ripples can be stacked)
        dic.Add(rippleImage, DOTween.Sequence());

        // calculate the correct target size of the ripple, so it always covers the entire interactible element
        float circleRadius = Mathf.Sqrt((thisRect.rect.width * thisRect.rect.width) + (thisRect.rect.height * thisRect.rect.height));
        float circleDiameter = circleRadius * 2;
        targetSize = new Vector2(circleDiameter, circleDiameter);

        // setup the opacity of the ripple
        Color c = rippleImage.color;
        c.a = opacity;
        rippleImage.color = c;

        return dic[rippleImage];
    }

    private void Fade()
    {
        if (rippleImage == null)
            return; 

        if (rippleImage.color.a == 0 || bIsAlreadyFading)
            return;

        bIsAlreadyFading = true;

        // fade out all existant/visible ripples
        foreach (KeyValuePair<Image, Sequence> d in dic)
        {
            Color c = d.Key.color;

            c.a = opacity;
            d.Key.color = c;

            d.Value.Join(d.Key.DOFade(0, fadeDur).SetEase(Ease.InCubic)
                                 .OnComplete(delegate { FinishAnim(d.Key); }));
        }
        dic.Clear();
    }

    private void FinishAnim(Image _toKill)
    {
        // disable (can also be destroy) the ripples after they dissapear
        _toKill.gameObject.SetActive(false);
    }

#endregion
}
