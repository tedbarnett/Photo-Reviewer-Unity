using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AutoCompleteInput : MonoBehaviour
{
	// References
	[SerializeField] InputField m_InputField;
	[SerializeField] ScrollRect m_ScrollRect;
	private RectTransform m_ScrollRectTransform;

	// Prefab for available options
	[SerializeField] OptionButton m_OptionPrefab;

	// The transform (parent) to spawn options into
	[SerializeField] Transform m_OptionsParent;

	[SerializeField] List<string> m_Options;

	private Dictionary<string, GameObject> m_OptionObjectSpawnedDict;       // Store a list of options in a dictionary, essentially object pooling the buttons

	private float m_OriginalOffsetMinY;
	private GameObject prefabObject;

	[SerializeField] int m_ComponentsHeight = 80;                           // The size of the option buttons
	[SerializeField] int m_OptionsOnDisplay = 3;                            // How many options the scrollview can display at one time

    private PhotoReview photoReview;
    private GoogleSheetsForUnity.GoogleSheetAccess googleSheetsForUnity; // Access variables and scripts in GoogleSheetAccess.cs


    private void Start()
	{
		m_OriginalOffsetMinY = m_ScrollRect.gameObject.GetComponent<RectTransform>().offsetMin.y;

		m_ScrollRectTransform = m_ScrollRect.gameObject.GetComponent<RectTransform>();

		m_ScrollRect.gameObject.SetActive(false);   // By default, we don't need to show the scroll view.

		SpawnClickableOptions(m_Options);

        photoReview = GetComponent<PhotoReview>();
        googleSheetsForUnity = GetComponent<GoogleSheetsForUnity.GoogleSheetAccess>();
    }

	public void SetAndCloseScrollView(string optionLabel)
	{
		m_InputField.text = optionLabel;
		m_ScrollRect.gameObject.SetActive(false);
        //Debug.Log("optionLabel = " + optionLabel);
        //photoReview.LocationInput.text = optionLabel; // Makes sure clicked autoComplete item is entered fully

		photoReview.EditedLocation(); // Making sure selected entry is saved

         // TODO: Add new unique item to the list of options
		    // m_OptionObjectSpawnedDict.Add("NEW" + m_InputField.text, prefabObject);

	}

	/// <summary>
	/// Spawns a list of all the available options into the scene, deactivates them, and adds them to the pool
	/// </summary>
	/// <param name="options"></param>
	public void SpawnClickableOptions(List<string> options)
	{
		ResetDictionaryAndCleanupSceneObjects();

		if (options == null || options.Count == 0)
		{
			Debug.LogError("Options lists is null or the list is == 0, please ensure it has something in it!");
			return;
		}

		for (int i = 0; i < options.Count; i++)
		{
			GameObject obj = Instantiate(m_OptionPrefab.gameObject, m_OptionsParent);
			obj.transform.localScale = Vector3.one;
			prefabObject = obj;

			m_OptionObjectSpawnedDict.Add(options[i], obj);

			string opt = options[i];
            //Debug.Log("In SpawnClickableOptions, opt = " + opt);

			obj.GetComponent<OptionButton>().Setup(options[i], m_ComponentsHeight, () =>
			{
                //Debug.Log("AUTOCOMPLETE button clicked!");
                //Debug.Log("CLICKED option " + opt); //TODO: Make sure locationText.text is set to this value "opt"
                photoReview.LocationInput.text = opt;
                photoReview.LocationText.text = opt;
				SetAndCloseScrollView(opt);
			});
		}
	}


	/// <summary>
	/// Cleans up the scrollview
	/// </summary>
	private void ResetDictionaryAndCleanupSceneObjects()
	{
		if (m_OptionObjectSpawnedDict == null)
		{
			m_OptionObjectSpawnedDict = new Dictionary<string, GameObject>();
			return;
		}

		if (m_OptionObjectSpawnedDict.Count == 0)
			return;

		foreach (KeyValuePair<string, GameObject> options in m_OptionObjectSpawnedDict)
			Destroy(options.Value);

		m_OptionObjectSpawnedDict.Clear();
	}

	/// <summary>
	/// Hooked up to the OnValueChanged() event of the inputfield specified, we listen out for changes within the input field.
	/// When the input.text has changed, we search the options dictionary and attempt to find matches, and display them if any.
	/// </summary>
	public void OnValueChanged()
	{
		string currentPhotoLocation = null;
		if (photoReview.settingUpTextFields) return; // skip this if still setting up text fields!

		//photoReview.StatusText.text = photoReview.StatusText.text + "photoReview.currentPhoto = " + photoReview.currentPhoto;
		//photoReview.StatusText.text = photoReview.StatusText.text + "googleSheetsForUnity.allPhotos[photoReview.currentPhoto].location = " + googleSheetsForUnity.allPhotos[photoReview.currentPhoto].location;

		if (googleSheetsForUnity.allPhotos[photoReview.currentPhoto].location != null)
		{
			currentPhotoLocation = googleSheetsForUnity.allPhotos[photoReview.currentPhoto].location;
		}

		if (m_InputField.text == "" || m_InputField.text == currentPhotoLocation)
		{
			m_ScrollRect.gameObject.SetActive(false); // Disable the scrollview if the inputfield is empty or newly-loaded
			return;
		}


		List<string> optionsThatMatched = m_OptionObjectSpawnedDict.Keys.
		   Where(optionKey => optionKey.ToLower().Contains(m_InputField.text.ToLower())).ToList();

		foreach (KeyValuePair<string, GameObject> keyValuePair in m_OptionObjectSpawnedDict)
		{
			if (optionsThatMatched.Contains(keyValuePair.Key))
				keyValuePair.Value.SetActive(true);
			else
				keyValuePair.Value.SetActive(false);
		}

		if (optionsThatMatched.Count == 0)
		{
			m_ScrollRect.gameObject.SetActive(false);        // Disable the scrollview if no options
			return;
		}


		// If options is > than the amount of options we can display
		if (optionsThatMatched.Count > m_OptionsOnDisplay)
		{
			// Then scale the height of the rect transform to only show the max amount of items we can show at one time
			m_ScrollRectTransform.offsetMin = new Vector2(
					   m_ScrollRect.GetComponent<RectTransform>().offsetMin.x,
						 m_OriginalOffsetMinY - (m_ComponentsHeight * m_OptionsOnDisplay));

		}
		else
		{
			// Else... just increase the height of the rect transform to display all of options that matched
			m_ScrollRectTransform.offsetMin = new Vector2(
					  m_ScrollRect.GetComponent<RectTransform>().offsetMin.x,
						m_OriginalOffsetMinY - (m_ComponentsHeight * optionsThatMatched.Count));
		}

		m_ScrollRect.gameObject.SetActive(true);            // If we get here, we can assume that we want to display the options.
	}
}
