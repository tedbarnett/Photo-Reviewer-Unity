using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FitMyChild : MonoBehaviour
{
	public GameObject childTextField;
    public GameObject childInputFieldText;
    public GameObject parentToStretch;
    public float originalChildInputHeight; // set manually in Inspector
    public float originalChildTextHeight; // set manually in Inspector
    public bool isInputCurrently;
    public GameObject heightText; // Debug only
    private Text heightInfo;

    private RectTransform parentRectTransform;
	private RectTransform childInputRectTransform;
    private RectTransform childTextRectTransform;
    private float originalParentHeight;
    private float originalParentWidth;
	private float newParentHeight;
    [HideInInspector]
    public float lastNewParentHeight;
    //private Text infoTextString;

    private void Start()
	{

		//infoTextString = infoText.GetComponent<Text>();
		parentRectTransform = parentToStretch.GetComponent<RectTransform>();
		childInputRectTransform = childInputFieldText.GetComponent<RectTransform>();
        childTextRectTransform = childTextField.GetComponent<RectTransform>();
        originalParentHeight = parentRectTransform.rect.height;
        originalParentWidth = parentRectTransform.rect.width;
        lastNewParentHeight = 0.0f;

        heightInfo = heightText.GetComponent<Text>();
        //Debug.Log("originalParentHeight = " + originalParentHeight);
        //Debug.Log("originalChildHeight = " + originalChildHeight);

    }
    private void LateUpdate()
    {
        if (isInputCurrently)
        {
            newParentHeight = Mathf.Max(originalParentHeight, originalParentHeight + (childInputRectTransform.rect.height - originalChildInputHeight));
        }
        else
        {
            newParentHeight = Mathf.Max(originalParentHeight, originalParentHeight + (childTextRectTransform.rect.height - originalChildTextHeight));
        }
        //heightInfo.text = "newParentHeight = " + newParentHeight;
        //heightInfo.text += "\n" + "originalParentHeight = " + originalParentHeight;
        //heightInfo.text += "\n" + "childTextRectTransform.rect.height = " + childTextRectTransform.rect.height;
        //heightInfo.text += "\n" + "lastNewParentHeight = " + lastNewParentHeight;

        if (Mathf.Abs(newParentHeight - lastNewParentHeight) < 0.001f) return; // no real change to height
        ResetParentHeight(newParentHeight);
    }

    void ResetParentHeight(float targetParentHeight)
	{
		parentRectTransform.sizeDelta = new Vector2(originalParentWidth, targetParentHeight);
        lastNewParentHeight = targetParentHeight;
    }

    public void ResetToOriginalParentHeight()
    {
        parentRectTransform.sizeDelta = new Vector2(originalParentWidth, originalParentHeight);
        Debug.Log("In ResetToOriginalParentHeight, originalParentHeight = " + originalParentHeight);
        lastNewParentHeight = originalParentHeight;
        //LayoutRebuilder.ForceRebuildLayoutImmediate(parentRectTransform);
    }
}
