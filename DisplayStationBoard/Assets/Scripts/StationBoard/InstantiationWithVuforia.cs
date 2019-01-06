/* This is the version of Instantiation.cs used in the during the hands-on experience 
*  after the final presentation. It's used in the scene with the marker recognition 
*  (Display_billboard_withVuforia.unity), and creates an Interface at the marker position.
*  The differences between Instantiation.cs and InstantiationWithVufaria.cs are that in 
*  Instantiation.cs the buttons are always destroyed and newly created, while in 
*  InstantiationWith Vuforia.cs there are predefined buttons that are set active and inactive
*  according to the function of the button. This had to be done because when using Vuforia
*  the application creates the new buttons allways on a completely wrong position that is far 
*  away from the marker. With having the predefined buttons, the position only has to be set 
*  to the marker position once to get a correct display.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.EventSystems;

public class InstantiationWithVuforia : MonoBehaviour {

    // set up global variables
    private string departureStation;
    private string remoteUri;
    private string json = "";
    public static bool gettext = true;
    public static int size;
    public static int n = 10;
    public static Stationboard requested;
    public static Stationboard pinned;
    public static Stationboard[] destinations = new Stationboard[n];

    public GameObject buttonPrefab;
    public GameObject buttonPannel;
    public static GameObject[] buttonlist = new GameObject[n];
    public static GameObject[] stoplist = new GameObject[0];
    private string numstring = "";

    // start function, called first
    void Start () {
        
        // set departure station
        departureStation = this.transform.name;
        
        // request URL
        remoteUri = String.Format("http://transport.opendata.ch/v1/stationboard?station={0}&limit=10/stationboard.json", departureStation);
        
        // empty button list
        buttonlist = new GameObject[n];
        
        // reset all buttons in buttonPanel (inactive, interactable, remove all listeners)
        foreach(Transform child in buttonPannel.transform)
        {
            child.gameObject.SetActive(false);
            child.GetComponent<Button>().interactable = true;
            child.GetComponent<Button>().onClick.RemoveAllListeners();
        }
        
        // add n buttons of buttonPannel to the buttonlist, set them active and add OnClick listener
        for (int i = 0; i < n; i++) 
        {
            buttonlist[i] = buttonPannel.transform.GetChild(i).gameObject;
            buttonlist[i].SetActive(true);
            buttonlist[i].GetComponent<Button>().onClick.AddListener(OnClick);    
        }

        // element check
        if (destinations[0] != null)
        {
            // fill buttons with content
            for (int i = 0; i < n; i++)
            {
                // filter information from objects
                string departure = destinations[i].stop.departure.ToString("t").PadRight(15, ' ');
                string platform = destinations[i].stop.platform;
                string destination = destinations[i].to;
                string name = destinations[i].name;
                string[] arr = { departure, destination };
                string output = string.Join("                            ", arr);
                
                // adding text to buttons
                buttonlist[i].transform.GetChild(0).GetComponent<Text>().text = string.Format("  {0}", output);
                buttonlist[i].transform.GetChild(1).GetComponent<Text>().text = string.Format("{0}  ", platform);
                buttonlist[i].transform.GetChild(2).GetComponent<Text>().text = string.Format("                     {0}", name);

                // if a train is pinned...
                if (pinned != null)
                {
                    // identity check
                    if (pinned.name == name)
                    {
                        // change color of this button to green
                        var colors = buttonlist[i].GetComponent<Button>().colors;
                        colors.normalColor = new Color32(0x78, 0xCC, 0x78, 0xFF);
                        buttonlist[i].GetComponent<Button>().colors = colors;
                    } else
                    {
                        // otherwise set normal color
                        var colors = buttonlist[i].GetComponent<Button>().colors;
                        colors.normalColor = new Color32(0x78, 0xA7, 0xBA, 0xFF);
                        buttonlist[i].GetComponent<Button>().colors = colors;
                    }
                }
            }
        }
        // start coroutine every 10 seconds
        StartCoroutine(GetText());
    }

    // back button event
    void Back ()
    {
        // reset all buttons in stoplist (inactive, remove all listeners, empty text)
        foreach (var button in stoplist)
        {
            button.SetActive(false);
            button.GetComponent<Button>().onClick.RemoveAllListeners();
            button.transform.GetChild(0).GetComponent<Text>().text = "";
            button.transform.GetChild(1).GetComponent<Text>().text = "";
            button.transform.GetChild(2).GetComponent<Text>().text = "";
        }

        // empty list, stop coroutines and start over
        stoplist = new GameObject[0];
        StopAllCoroutines();
        gettext = true;
        size = 10;
        Start();
    }

    
    void Pin()
    {

        // set pinned to current requested train (clicked)
        pinned = requested;
        
        // set all information
        DepartureWarning.checkPinned = true;
        DepartureWarning.platform = requested.stop.platform;
        DepartureWarning.destination = requested.to;
        DepartureWarning.departureTime = requested.stop.departure;

        // reset all buttons in stoplist (inactive, remove all listeners, empty text)
        foreach (var button in stoplist)
        {
            button.SetActive(false);
            button.GetComponent<Button>().onClick.RemoveAllListeners();
            button.transform.GetChild(0).GetComponent<Text>().text = "";
            button.transform.GetChild(1).GetComponent<Text>().text = "";
            button.transform.GetChild(2).GetComponent<Text>().text = "";
        }

        // empty list, stop coroutines and start over
        stoplist = new GameObject[0];
        StopAllCoroutines();
        gettext = true;
        size = 10;
        Start();
    }

    // click function
    public void OnClick()
    {
        // set numstring as current selected item in scene
        numstring = EventSystem.current.currentSelectedGameObject.name;
        
        // pass unselected
        while (numstring == "") { }
        
        // call OnClickFunc
        OnClickFunc();
    }

    // on click function
    public void OnClickFunc ()
    {        
        // parse to int
        int num = int.Parse(numstring);

        // reset all buttons in buttonlist (inactive, remove all listeners)
        foreach (var button in buttonlist)
        {
            button.SetActive(false);
            button.GetComponent<Button>().onClick.RemoveAllListeners();
        }
        
        // set requested, size and stoplist + 2 because of back and pin button
        requested = destinations[num];
        size = requested.passList.Count;
        stoplist = new GameObject[size + 2];

        // loop through stations
        for (int k = 0; k < size; k++)
        {
            // set all information of station stop
            DateTime dateTime = requested.passList[k].arrival??DateTime.Now;
            string departure = dateTime.ToString("t").PadRight(15, ' ');
            string name = requested.passList[k].station.name;

            // first in the stop list
            if (name == null)
            {
                name = departureStation;
            }

            // set stopButton to equivalent button in buttonPannel and set active 
            GameObject stopButton = buttonPannel.transform.GetChild(k).gameObject;
            stopButton.SetActive(true);

            // put information in buttons and set to non interactable
            stopButton.transform.GetChild(0).GetComponent<Text>().text = String.Format("  ¬ {0}{1}", departure, name);
            stopButton.transform.GetChild(1).GetComponent<Text>().text = "";
            stopButton.transform.GetChild(2).GetComponent<Text>().text = "";
            stopButton.GetComponent<Button>().interactable = false;

            // append
            stoplist[k] = stopButton;
        }

        // same for back button
        GameObject backButton = buttonPannel.transform.GetChild(size).gameObject;
        backButton.SetActive(true);
        
        // add correct listener
        backButton.GetComponent<Button>().onClick.AddListener(Back);
        var backcolors = backButton.GetComponent<Button>().colors;
        backcolors.normalColor = new Color32(0x78, 0xA7, 0xBA, 0xFF);
        backButton.GetComponent<Button>().colors = backcolors;

        backButton.transform.GetChild(0).GetComponent<Text>().text = "  Back";
        backButton.transform.GetChild(1).GetComponent<Text>().text = "";
        backButton.transform.GetChild(2).GetComponent<Text>().text = "";

        stoplist[size] = backButton;
        size++;

        // same for pin button
        GameObject pinButton = buttonPannel.transform.GetChild(size).gameObject;
        pinButton.SetActive(true);
        
        // add correct listener
        pinButton.GetComponent<Button>().onClick.AddListener(Pin);
        
        var pincolors = pinButton.GetComponent<Button>().colors;
        pincolors.normalColor = new Color32(0x78, 0xA7, 0xBA, 0xFF);
        pinButton.GetComponent<Button>().colors = pincolors;

        pinButton.transform.GetChild(0).GetComponent<Text>().text = "  Pin this train!";
        pinButton.transform.GetChild(1).GetComponent<Text>().text = "";
        pinButton.transform.GetChild(2).GetComponent<Text>().text = "";

        stoplist[size] = pinButton;
        size++;

        gettext = false;
        numstring = "";
    }
    
    // this is the coroutine
    IEnumerator GetText()
    {
        // infinite loop
        while (true)
        {
            // get JSON response from URL request
            UnityWebRequest www = UnityWebRequest.Get(remoteUri);
            yield return www.SendWebRequest();
            json = www.downloadHandler.text;
            
            // create JSON object and parse it into it
            RootObject stationBoard = new RootObject();
            stationBoard = JsonConvert.DeserializeObject<RootObject>(json);

            // content check
            if (gettext)
            {
                // for all departures
                for (int i = 0; i < n; i++)
                {
                    // get all information from JSON object
                    string departure = stationBoard.stationboard[i].stop.departure.ToString("t").PadRight(15, ' ');
                    string platform = stationBoard.stationboard[i].stop.platform;
                    string destination = stationBoard.stationboard[i].to;
                    string name = stationBoard.stationboard[i].name;
                    string[] arr = { departure, destination };
                    string output = string.Join("                            ", arr);
                     
                    // put all this information in the buttons
                    buttonlist[i].transform.GetChild(0).GetComponent<Text>().text = string.Format("  {0}", output);
                    buttonlist[i].transform.GetChild(1).GetComponent<Text>().text = string.Format("{0}  ", platform);
                    buttonlist[i].transform.GetChild(2).GetComponent<Text>().text = string.Format("                     {0}", name);
                    destinations[i] = stationBoard.stationboard[i];

                    // if a train was pinned
                    if (pinned != null)
                    {
                        // and it resembles this train
                        if (pinned.name == name)
                        {
                            // set color to green
                            var colors = buttonlist[i].GetComponent<Button>().colors;
                            colors.normalColor = new Color32(0x78, 0xCC, 0x78, 0xFF);
                            buttonlist[i].GetComponent<Button>().colors = colors;
                        } else
                        {
                            // otherwise set to normal color
                            var colors = buttonlist[i].GetComponent<Button>().colors;
                            colors.normalColor = new Color32(0x78, 0xA7, 0xBA, 0xFF);
                            buttonlist[i].GetComponent<Button>().colors = colors;
                        }
                    }
                }
                // set coroutine to 10 seconds
                yield return new WaitForSeconds(10);
            }
        }
    }
}

