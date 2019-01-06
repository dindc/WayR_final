/* This is the version of Instatntiation.cs used in the final presentation. It's used
*  in the scene without the marker recognition (Display_billboard_noVuforia.unity),
*  and creates a floating Interface about 1.5 meters in front of the user with a fixed 
*  world position.
*  For the script using marker recognition, see InstantiationWithVuforia.cs.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.EventSystems;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.EventSystems;

public class Instantiation : MonoBehaviour
{

    // set up all global variables
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
    void Start()
    {

        // set departure station
        departureStation = this.transform.name;
        
        // request URL
        remoteUri = String.Format("http://transport.opendata.ch/v1/stationboard?station={0}&limit=10/stationboard.json", departureStation);
        
        // empty button list
        buttonlist = new GameObject[n];

        // create n buttons
        for (int i = 0; i < n; i++)
        {
            // inherit from prefab
            GameObject button = (GameObject)Instantiate(buttonPrefab);
            button.name = string.Format("{0}", i);
            button.transform.SetParent(buttonPannel.transform);
            button.GetComponent<Button>().onClick.AddListener(OnClick);
            button.AddComponent<ButtonClick>();

            // default text and text alignment
            button.transform.GetChild(0).GetComponent<Text>().text = "";
            button.transform.GetChild(0).GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            // create text and clone for right aligned text
            Text clone = Instantiate(button.transform.GetChild(0).GetComponent<Text>(), button.transform);
            Text clone2 = Instantiate(button.transform.GetChild(0).GetComponent<Text>(), button.transform);
            clone.alignment = TextAnchor.MiddleRight;

            // append button to list
            buttonlist[i] = button;
        }

        // element check
        if (destinations[0] != null)
        {
            // fill buttons with content
            for (int i = 0; i < n; i++)
            {
                // filter information from object
                string departure = destinations[i].stop.departure.ToString("t").PadRight(15, ' ');
                string platform = destinations[i].stop.platform;
                string destination = destinations[i].to;
                string name = destinations[i].name;
                string[] arr = { departure, destination };
                string output = string.Join("                            ", arr);

                // coloring and text of buttons
                buttonlist[i].GetComponentInChildren<Text>().color = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
                buttonlist[i].transform.GetChild(1).GetComponent<Text>().color = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
                buttonlist[i].transform.GetChild(2).GetComponent<Text>().color = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

                buttonlist[i].GetComponentInChildren<Text>().text = string.Format("  {0}", output);
                buttonlist[i].transform.GetChild(1).GetComponent<Text>().text = string.Format("{0}  ", platform);
                buttonlist[i].transform.GetChild(2).GetComponent<Text>().text = string.Format("                     {0}", name);

                // if a train is pinned ...
                if (pinned != null)
                {
                    // identity check
                    if (pinned.name == name)
                    {
                        // change the color of this button to green
                        var colors = buttonlist[i].GetComponent<Button>().colors;
                        colors.normalColor = Color.green;
                        buttonlist[i].GetComponent<Button>().colors = colors;
                    }
                    else
                    {
                        // otherwise set normal color
                        var colors = buttonlist[i].GetComponent<Button>().colors;
                        colors.normalColor = new Color32(0x78, 0xA7, 0xBA, 0xFF);
                        buttonlist[i].GetComponent<Button>().colors = colors;
                    }
                }
            }
        }
        // start the coroutine, every 10 seconds
        StartCoroutine(GetText());
    }

    // back button event
    void Back()
    {
        // destroy all existing buttons
        foreach (var button in stoplist)
        {
            Destroy(button);
        }

        // empty stoplist, stop coroutines and start over
        stoplist = new GameObject[0];
        StopAllCoroutines();
        gettext = true;
        size = 10;
        Start();
    }

    // pin train event
    void Pin()
    {
        // set pinned to current requested train (clicked)
        pinned = requested;
        
        // set all information
        DepartureWarning.checkPinned = true;
        DepartureWarning.platform = requested.stop.platform;
        DepartureWarning.destination = requested.to;
        DepartureWarning.departureTime = requested.stop.departure;

        // destroy all existing buttons
        foreach (var button in stoplist)
        {
            Destroy(button);
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
        
        // call onclick
        OnClickFunc();
    }

    // on click function
    public void OnClickFunc()
    {
        // parse to int
        int num = int.Parse(numstring);
   
        // destroy all buttons
        foreach (var button in buttonlist)
        {
            Destroy(button);
        }

        // set requested, size and stoplist + 2 because of back and pin button
        requested = destinations[num];
        size = requested.passList.Count;
        stoplist = new GameObject[size + 2];

        // loop through stations
        for (int k = 0; k < size; k++)
        {
            // set all information of station stop
            DateTime dateTime = requested.passList[k].arrival ?? DateTime.Now;
            string departure = dateTime.ToString("t").PadRight(15, ' ');
            string name = requested.passList[k].station.name;

            // first in the stop list
            if (name == null)
            {
                name = departureStation;
            }

            // inherit button prefab
            GameObject stopButton = (GameObject)Instantiate(buttonPrefab);
            stopButton.name = string.Format("{0}", k);
            stopButton.transform.SetParent(buttonPannel.transform);

            // put information in buttons
            stopButton.transform.GetChild(0).GetComponent<Text>().text = String.Format("  ¬ {0}{1}", departure, name);
            stopButton.transform.GetChild(0).GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            /*var colors = stopButton.GetComponent<Button>().colors;
            colors.normalColor = Color.grey;
            stopButton.GetComponent<Button>().colors = colors;
            */
            
            // set color and to non interactable
            stopButton.transform.GetChild(0).GetComponent<Text>().color = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
            stopButton.GetComponent<Button>().interactable = false;
            
            // append
            stoplist[k] = stopButton;
        }

        // same for back button
        GameObject backButton = (GameObject)Instantiate(buttonPrefab);
        backButton.name = string.Format("{0}", size);
        backButton.transform.SetParent(buttonPannel.transform);
        
        // add correct listener
        backButton.GetComponent<Button>().onClick.AddListener(Back);

        backButton.transform.GetChild(0).GetComponent<Text>().text = "  Back";
        backButton.transform.GetChild(0).GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

        stoplist[size] = backButton;
        size++;

        // same for pin button
        GameObject pinButton = (GameObject)Instantiate(buttonPrefab);
        pinButton.name = string.Format("{0}", size);
        pinButton.transform.SetParent(buttonPannel.transform);
        
        // add correct listener
        pinButton.GetComponent<Button>().onClick.AddListener(Pin);

        pinButton.transform.GetChild(0).GetComponent<Text>().text = "  Pin this train!";
        pinButton.transform.GetChild(0).GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

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
                    Instantiation.buttonlist[i].GetComponentInChildren<Text>().text = string.Format("  {0}", output);
                    Instantiation.buttonlist[i].transform.GetChild(1).GetComponent<Text>().text = string.Format("{0}  ", platform);
                    Instantiation.buttonlist[i].transform.GetChild(2).GetComponent<Text>().text = string.Format("                     {0}", name);
                    destinations[i] = stationBoard.stationboard[i];

                    buttonlist[i].transform.GetChild(1).GetComponent<Text>().color = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
                    buttonlist[i].transform.GetChild(2).GetComponent<Text>().color = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
                    buttonlist[i].GetComponentInChildren<Text>().color = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

                    // if a train was pinned
                    if (pinned != null)
                    {
                        // and it resembles this train
                        if (pinned.name == name)
                        {
                            // set color to green
                            var colors = buttonlist[i].GetComponent<Button>().colors;
                            colors.normalColor = Color.green;
                            buttonlist[i].GetComponent<Button>().colors = colors;
                        }
                        else
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
