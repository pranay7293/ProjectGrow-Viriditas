using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerData;
using Tools;
using UnityEngine.UI;


public class BiofoundryUI : MonoBehaviour
{
    private PlayerInventoryCircuits _inventory;

    public SelectChassisUI selectChassisWindow;
    public GenomeMapUI editGenomeMapWindow;
    public SelectChassisUI selectEntityWithTraitWindow;
    public GenomeMapUI selectTraitWindow;
    public UI.CircuitButton circuitLabel;
    public Button manufactureCircuitButton;

    public bool anyWindowsOpen => selectChassisWindow.gameObject.activeInHierarchy || editGenomeMapWindow.gameObject.activeInHierarchy
    || circuitLabel.gameObject.activeInHierarchy || manufactureCircuitButton.gameObject.activeInHierarchy
        || selectEntityWithTraitWindow.gameObject.activeInHierarchy || selectTraitWindow.gameObject.activeInHierarchy;

    private OrganismDataSheet chassis;  // the organism we are building a circuit for
    private Circuit designedCircuit; // the circuit we are designing in this session of using the Biofoundry
    private Circuit.ModuleEdit[] moduleEdits;  // the list of Edits we are designing for the circuit
    private GenomeMap modifiedGenomeMap;  // the genome map we are modifying in this session

    private OrganismDataSheet sourceOrganism;  // when we take a Trait from an entity, which entity is it?

    public void CloseOpenWindows()
    {
        selectChassisWindow.gameObject.SetActive(false);
        editGenomeMapWindow.gameObject.SetActive(false);
        circuitLabel.gameObject.SetActive(false);
        manufactureCircuitButton.gameObject.SetActive(false);
        selectEntityWithTraitWindow.gameObject.SetActive(false);
        selectTraitWindow.gameObject.SetActive(false);
    }

    public void InitiateBiofoundryUI(PlayerInventoryCircuits inventory)
    {
        if (inventory == null)
        {
            Debug.LogError("InitiateBiofoundryUI called with null inventory.");
            return;
        }

        _inventory = inventory;

        designedCircuit = null;

        selectChassisWindow.gameObject.SetActive(true);
        selectChassisWindow.RefreshSelectChassisData(this);
    }

    public void GenomeMapSelected(OrganismDataSheet ods)
    {
        // where are we in the flow?
        if (selectChassisWindow.gameObject.activeInHierarchy)
            ChassisSelected(ods);
        else if (selectEntityWithTraitWindow.gameObject.activeInHierarchy)
            EntityWithTraitSelected(ods);
        else
            Debug.LogError("Bad news, I'm confused about where I am in the Biofoundry UI flow.");
    }

    private void ChassisSelected(OrganismDataSheet ods)
    {
        chassis = ods;
        moduleEdits = new Circuit.ModuleEdit[0];  // TODO - if this code is to be maintained, change 0 to the number of slots in this ods
        designedCircuit = new Circuit(chassis, moduleEdits);
        UpdateCircuitFeebdack();

        // TODO - copy the ods's genome map onto a new things, a modifiedGenomeMap instance, and use that instead?

        selectChassisWindow.gameObject.SetActive(false);
        editGenomeMapWindow.gameObject.SetActive(true);
        editGenomeMapWindow.DisplayGenomeMap(ods.genomeMap, TreeOfLifeUI.TreeOfLifeUIMode.SelectOutput); 
    }


    public void TraitSlotButtonClicked(GenomeMap.ModuleSlot moduleSlot)
    {
        // where are we in the flow?
        if (selectTraitWindow.gameObject.activeInHierarchy)
            TraitToAddSelected(moduleSlot);
        else if (editGenomeMapWindow.gameObject.activeInHierarchy)
            TraitSlotEdit(moduleSlot);
        else
            Debug.LogError("Bad news, I'm confused about where I am in the Biofoundry UI flow.");
    }

    public void TraitSlotEdit(GenomeMap.ModuleSlot moduleSlot)
    {
        // note that it's not possible to click on a slot with an unremoveable trait.

        if (moduleSlot.defaultModule.output != null)
        {
            // remove a Trait
            Circuit.ModuleEdit newEdit = new Circuit.ModuleEdit(null);  // TODO - to really support this code, pass in a parent folder transform
            newEdit.module.output = moduleSlot.defaultModule.output;
            newEdit.modifier = Circuit.ModuleEditModifier.Remove;
            newEdit.sourceOrganism = null;

            moduleEdits[0] = newEdit;  // TODO - if this code is to be maintained, swap out 0 with the appropriate slot
            designedCircuit = new Circuit(chassis, moduleEdits);
            UpdateCircuitFeebdack();
        }
        else
        {
            // add a Trait
            // start by launching some windows to find out which Trait to add
            selectEntityWithTraitWindow.gameObject.SetActive(true);
            selectEntityWithTraitWindow.RefreshSelectChassisData(this);
        }
    }

    public void EntityWithTraitSelected (OrganismDataSheet ods)
    {
        sourceOrganism = ods;

        selectEntityWithTraitWindow.gameObject.SetActive(false);
        selectTraitWindow.gameObject.SetActive(true);
        selectTraitWindow.DisplayGenomeMap(ods.genomeMap, TreeOfLifeUI.TreeOfLifeUIMode.SelectOutput); 
        // TODO - activate the GenomeMap in a particular way, only providing buttons for Traits compatible with this slot
    }

    private void TraitToAddSelected (GenomeMap.ModuleSlot moduleSlot)
    {
        selectTraitWindow.gameObject.SetActive(false);
        AddTraitToCircuit(moduleSlot.defaultModule.output);
    }

    private void AddTraitToCircuit(Trait toAdd)
    {
        Circuit.ModuleEdit newEdit = new Circuit.ModuleEdit(null); // TODO - to really support this code, pass in a parent folder transform
        newEdit.module.output = toAdd;
        newEdit.modifier = Circuit.ModuleEditModifier.Add;
        newEdit.sourceOrganism = sourceOrganism;

        moduleEdits[0] = newEdit;  // TODO - if this code is to be maintained, swap out 0 with the appropriate slot
        designedCircuit = new Circuit(chassis, moduleEdits);
        UpdateCircuitFeebdack();
    }


    private void ManufactureCircuit()
    {
        if (!designedCircuit.IsValid())
        {
            Debug.LogError("ManufactureCircuit called but designedCircuit is not valid.");
            return;
        }

        _inventory.Add(designedCircuit);
        CloseOpenWindows();
        gameObject.SetActive(false);
    }

    private void UpdateCircuitFeebdack()
    {
        circuitLabel.gameObject.SetActive(true);
        circuitLabel.SetCircuit(designedCircuit);

        manufactureCircuitButton.gameObject.SetActive(true);
        manufactureCircuitButton.interactable = designedCircuit.IsValid();
    }

}
