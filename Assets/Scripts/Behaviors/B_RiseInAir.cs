using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class B_RiseInAir : Behavior
{
    public enum RiseInAirBehaviorSubtype
    {
        RiseInAir,
        RiseAndWiggle
    }
    [SerializeField]
    public RiseInAirBehaviorSubtype riseInAirBehaviorSubtype;

    private bool lastKnownActive;

    public GameObject raiseMe;  // the thing that gets raised
    private float riseHeight; // desired distance to rise above the position it was when the behavior initiated - this is determined by the Trait strength
    public float heightVariation; // it wobbles around a desired height a bit

    private Vector3 initialPosition;  // when the behavior started 
    private Vector3 initialRot;
    private float moveSpeed = 1f;
    private float currentHeightOffset;
    private int howOftenToUpdateHeightOffset = 120; // every 120 frames

    public float wiggleXspeed;
    public float wiggleXamplitude;
    public float wiggleYspeed;
    public float wiggleYamplitude;
    public float wiggleZspeed;
    public float wiggleZamplitude;

    protected override void StartBehavior()
    {
        if (DEBUG_Verbose)
            Debug.Log($"B_RiseInAir on Entity {name} starting with strength of {strength}.");

        initialPosition = raiseMe.transform.position;
        initialRot = raiseMe.transform.rotation.eulerAngles;

        riseHeight = strength;
        currentHeightOffset = riseHeight;

        Rigidbody rb = raiseMe.GetComponent<Rigidbody>();
        if (rb != null)
            rb.useGravity = false;
    }

    protected override void EndBehavior()
    {
        if (DEBUG_Verbose)
            Debug.Log("B_RiseInAir on Entity " + gameObject + " ending.");

        Rigidbody rb = raiseMe.GetComponent<Rigidbody>();
        if (rb != null)
            rb.useGravity = true;
        else
        {
            Collider myCollider = raiseMe.GetComponent<Collider>();
            if (myCollider != null)
            {
                Vector3 newPos = WorldRep.PlaceColliderOnTerrain(myCollider);
                myCollider.transform.position = newPos;
            }
            else
            {
                // if there's no collider nor a rigidbody, then just put it back where we found it
                raiseMe.transform.position = new Vector3(initialPosition.x, initialPosition.y, initialPosition.z);
                raiseMe.transform.rotation = Quaternion.Euler(initialRot);
            }
        }
    }

    // pause and unpause are intentionally not defined for this behavior - either stop it or end it


    private void Update()
    {
        if (behaviorEnabled)
        {
            if (Time.frameCount % howOftenToUpdateHeightOffset == 0)
                currentHeightOffset = riseHeight + Random.Range(-heightVariation, heightVariation);

            // update height towards desired height
            raiseMe.transform.position = Vector3.Lerp(raiseMe.transform.position, GetDesiredPosition(currentHeightOffset), moveSpeed * Time.deltaTime);

            if (riseInAirBehaviorSubtype == RiseInAirBehaviorSubtype.RiseAndWiggle)
                Wiggle();
        }
    }


    private Vector3 GetDesiredPosition(float heightOffset)
    {
        float desiredHeight = WorldRep.TerrainYHeightAtPosition(raiseMe.transform.position) + heightOffset;
        return new Vector3(raiseMe.transform.position.x, desiredHeight, raiseMe.transform.position.z);
    }


    private void Wiggle()
    {
        float rotX = Mathf.Sin(Time.time * wiggleXspeed) * wiggleXamplitude;
        float rotY = Mathf.Sin(Time.time * wiggleYspeed) * wiggleYamplitude;
        float rotZ = Mathf.Sin(Time.time * wiggleZspeed) * wiggleZamplitude;

        Vector3 newRot = new Vector3(initialRot.x + rotX, initialRot.y + rotY, initialRot.z + rotZ);

        transform.rotation = Quaternion.Euler(newRot);
    }



}
