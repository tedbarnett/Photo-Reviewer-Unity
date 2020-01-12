using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanZoom : MonoBehaviour
{
    Vector3 touchStart;
    public float zoomOutMin = 1;
    public float zoomOutMax = 8;
    public GameObject objectToScale;
    public float currentScaleValue = 1.0f;
    public Text ScaleText;
    public float Multiplier = .01f;


    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = prevMagnitude - currentMagnitude;

            ScaleText.text = difference.ToString();

            zoom(difference * Multiplier);

        }
    }

    void zoom(float scaleValue)
    {
        //scaleValue = ScaleConvert(scaleValue);
        //double scaleRatio = System.Math.Round(100 / scaleValue, 1);

        //if (scaleRatio > 10) scaleRatio = System.Math.Round(scaleRatio, 0);
        //scaleText.text = "Scale = 1 : " + scaleRatio;


        //this.objectToScale.transform.localScale = new Vector3(scaleValue, scaleValue, scaleValue);




        Vector3 newScale = objectToScale.transform.localScale - new Vector3(scaleValue, scaleValue, scaleValue);
        objectToScale.transform.localScale = newScale;


    }
    // Convert Scale Slider to Scale Value
    float ScaleConvert(float value)
    {
        float ret;
        ret = Mathf.Pow(10, (value - 1));
        return ret;
    }
}