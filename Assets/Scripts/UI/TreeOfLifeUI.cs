using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PlayerData;
using System.Linq;

// this version for the november '23 iGEM demo assumes the wheel of life is a static image that corresponds to the number
// of organism types that have been scanned previously.  see also DEBUG_PreScannedData

public class TreeOfLifeUI : MonoBehaviour
{
    public enum TreeOfLifeUIMode
    {
        ViewOnlyODS,  // open with the Organism Data Sheet
        ViewOnlyGenomeMap,  // open with the Genome Map
        SelectOrganism,
        SelectOutput
    }
    private TreeOfLifeUIMode currentMode;
    private GenomeMapUI.SelectOutputCallback selectOutputCallback;

    public TextMeshProUGUI noEntitiesYetLabel;

    private OrganismDataSheet[] listOfOrganisms;  // an array of all entity types the player has scanned or sampled at least once, represented by OrganismDataSheets
    private int currentIndex;  // which organism is currently selected with the wheel

    public OrganismDataSheet selectedOrganism => listOfOrganisms[currentIndex];  // TODO - bounds checking?

    private static float screenPosition_Center = 0f;
    private static float screenPosition_Right = 450f;
    [SerializeField] private GameObject treeOfLifeWheel;
    [SerializeField] private GameObject titleBarGO;
    [SerializeField] private TextMeshProUGUI titleBarText;

    [SerializeField] private float rotationSpeed = 90;  // degree of rotation per second
    private bool currentlyRotating;
    private float targetRotationAmount;
    private float rotationSoFar;
    private float rotationDirection;  // 1 is forward, -1 is backward
    private float queuedClickDirection; // 1, -1, or 0 for none

    // exactly one of these should be active at a time
    [SerializeField] private OrganismDataSheetUI organismDataSheetWindow;
    [SerializeField] private GenomeMapUI genomeMapWindow;


    // this is called (eg - by UIManager) after enabling this window
    // opens in the passed-in mode
    // defaults to the passed-in ODS/GenomeMap, or if null is passed in defaults to entry 0
    public void InitializeTreeofLifeUI(OrganismDataSheet startSelected, TreeOfLifeUIMode mode, GenomeMapUI.SelectOutputCallback callback)
    {
        currentMode = mode;
        selectOutputCallback = callback;

        // position the window depending on mode
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (currentMode == TreeOfLifeUIMode.SelectOutput) // SelectOutput
        {
            Vector2 newPosition = new Vector2(screenPosition_Right, rectTransform.anchoredPosition.y);
            rectTransform.anchoredPosition = newPosition;
        }
        else // ViewOnly or SelectOrganism
        {
            Vector2 newPosition = new Vector2(screenPosition_Center, rectTransform.anchoredPosition.y);
            rectTransform.anchoredPosition = newPosition;
        }

        RefreshTreeOfLifeData(startSelected);
        SnapWheelOfLifeToIndex(currentIndex);

        // open either the ODS window or the genome map window, depending on mode
        if (currentMode == TreeOfLifeUIMode.ViewOnlyODS) 
        {
            organismDataSheetWindow.gameObject.SetActive(true);
            genomeMapWindow.gameObject.SetActive(false);
            UpdateOrganismDataSheet(currentIndex);
        }
        else // SelectOutput or ViewOnlyGenomeMap  or SelectOrganism
        {
            genomeMapWindow.gameObject.SetActive(true);
            organismDataSheetWindow.gameObject.SetActive(false);
            UpdateGenomeMap(currentIndex);
        }

        // open the title bar with instructions, depending on mode
        if (currentMode == TreeOfLifeUIMode.SelectOrganism)
        {
            titleBarGO.SetActive(true);
            titleBarText.text = "Select A Chassis";
        }
        else if (currentMode == TreeOfLifeUIMode.SelectOutput)
        {
            titleBarGO.SetActive(true);
            titleBarText.text = "Select An Output";
        }
        else
        {
            titleBarGO.SetActive(false);
        }


    }

    private void RefreshTreeOfLifeData(OrganismDataSheet startSelected)
    {

        listOfOrganisms = PlayerScannedData.ScannedOrganismDataSheets.ToArray();

        // add to the list all organisms which have been sampled but not scanned
        OrganismDataSheet[] sampledOrganisms  = PlayerSampledData.SampledDataSheets.ToArray();
        foreach (OrganismDataSheet ods in sampledOrganisms)
            if (!listOfOrganisms.Contains(ods))
            {
                // this is basically listOfOrganisms.Add(ods);
                OrganismDataSheet[] newArray = new OrganismDataSheet[listOfOrganisms.Length + 1];
                for (int i = 0; i < listOfOrganisms.Length; i++)
                {
                    newArray[i] = listOfOrganisms[i];
                }
                newArray[newArray.Length - 1] = ods;
                listOfOrganisms = newArray;
            }

        if (listOfOrganisms.Length == 0)
            noEntitiesYetLabel.gameObject.SetActive(true);
        else
            noEntitiesYetLabel.gameObject.SetActive(false);

        if (startSelected == null)
        {
            currentIndex = 0;  // TODO - remember index between instances of opening the window
            return;
        }

        currentIndex = 0;

        while ((listOfOrganisms[currentIndex] != startSelected) && (currentIndex < listOfOrganisms.Length))
            currentIndex++;

        if (listOfOrganisms[currentIndex] != startSelected)
        {
            Debug.LogError($"Tree of Life could not find Organism Data Sheet {startSelected} to start on.");
            currentIndex = 0;
            return;
        }

        // otherwise currentIndex is set correctly, so return

    }

    // immediately changes the rotation of the wheel of life to match the passed-in index, visual change only
    private void SnapWheelOfLifeToIndex (int index)
    {
        // start by resetting the wheel to rotation 0
        treeOfLifeWheel.transform.rotation = Quaternion.identity;

        // index 0 = 0 rotation
        // each click = 360/number of entries

        float rot = index * (360f / (float)listOfOrganisms.Length);

        treeOfLifeWheel.transform.Rotate(Vector3.forward, rot);
    }

    // these are called when the UI to rotate the wheel of life is called (eg - the user clicks on the invisible buttons on the top or bottom of the wheel of life)
    // forward / counterclockwise
    public void ScrollUp()
    {
        if (currentlyRotating)
        {
            queuedClickDirection = 1f;
            return;
        }
        InitializeRotation(1f);
    }
    // backwards / clockwise
    public void ScrollDown()
    {
        if (currentlyRotating)
        {
            queuedClickDirection = -1f;
            return;
        }
        InitializeRotation(-1f);
    }

    private void InitializeRotation(float direction)
    {
        currentlyRotating = true;
        targetRotationAmount = 360f / (float)listOfOrganisms.Length;
        rotationSoFar = 0;
        rotationDirection = direction;
        queuedClickDirection = 0f;
    }

    // ends the current rotation and updates the wheel's index
    private void EndCurrentRotation()
    {
        if (rotationDirection == 1f) // forward
        {
            currentIndex++;
            if (currentIndex >= listOfOrganisms.Length)
                currentIndex -= listOfOrganisms.Length;
        }
        else // backwards
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex += listOfOrganisms.Length;
        }

        if (organismDataSheetWindow.gameObject.activeInHierarchy)
            UpdateOrganismDataSheet(currentIndex);
        if (genomeMapWindow.gameObject.activeInHierarchy)
            UpdateGenomeMap(currentIndex);

        if (queuedClickDirection == 0f)
            currentlyRotating = false;
        else
            InitializeRotation(queuedClickDirection);
    }

    private void Update()
    {
        if (currentlyRotating)
        {
            float new_rot_amount = Time.deltaTime * rotationSpeed;

            if ((rotationSoFar + new_rot_amount) > targetRotationAmount)
            {
                new_rot_amount = targetRotationAmount - rotationSoFar;
                EndCurrentRotation();
            }

            rotationSoFar += new_rot_amount;

            if (rotationDirection == -1f)  // backwards
                new_rot_amount *= -1f;

            treeOfLifeWheel.transform.Rotate(Vector3.forward, new_rot_amount);
        }
    }


    private void UpdateOrganismDataSheet(int index)
    {
        if (listOfOrganisms.Length == 0)
            return;

        organismDataSheetWindow.DisplayODS(listOfOrganisms[index], PlayerScannedData.GetScanLevel(listOfOrganisms[index]), currentMode);
    }

    private void UpdateGenomeMap(int index)
    {
        if (listOfOrganisms.Length == 0)
            return;

        GenomeMap genomeMap = listOfOrganisms[index].genomeMap;

        if (genomeMap == null)
        {
            Debug.LogError($"OrganismDataSheet {listOfOrganisms[index].name} does not have a genomeMap associated with it.");
            return;
        }

        if (currentMode == TreeOfLifeUIMode.SelectOutput)
            genomeMapWindow.DisplayGenomeMap(genomeMap, currentMode, selectOutputCallback);
        else
            genomeMapWindow.DisplayGenomeMap(genomeMap, currentMode);
    }


    public void ToggleFromODSToGenomeMap()
    {
        organismDataSheetWindow.gameObject.SetActive(false);
        genomeMapWindow.gameObject.SetActive(true);
        UpdateGenomeMap(currentIndex);
    }

    public void ToggleFromGenomeMapToODS()
    {
        genomeMapWindow.gameObject.SetActive(false);
        organismDataSheetWindow.gameObject.SetActive(true);
        UpdateOrganismDataSheet(currentIndex);
    }


}
