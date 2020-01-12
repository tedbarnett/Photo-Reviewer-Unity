using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class OptionButton : MonoBehaviour
{
    [SerializeField] Text m_ButtonTitle;
    [SerializeField] Button m_Button;
    [SerializeField] LayoutElement m_LayoutElement;
    [SerializeField] RectTransform m_RectTransform;

    public void Setup(string label, float height, UnityAction action)
    {
        m_ButtonTitle.text = label;
        m_Button.onClick.RemoveAllListeners();
        m_Button.onClick.AddListener(action);
        // We have to make the buttons in the scrollview slightly smaller,
        // this way if one button is in view, the scrollview won't show the
        // scrollbar.
        //height = height - 0.5f;

        m_LayoutElement.preferredHeight = height;

        // Just to make sure...
        m_RectTransform.sizeDelta = new Vector2(m_RectTransform.sizeDelta.x, height);

       
    }
}
