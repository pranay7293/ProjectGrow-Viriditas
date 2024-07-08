using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Temp class for time of day
// TODO: This is very messy and should be rewritten
[ExecuteInEditMode]
public class TimeOfDay : MonoBehaviour
{
    private static TimeOfDay _instance;

    public const float TIME_NIGHT = .1f; // 2:30 am - not 0 to make a little more interesting
    public const float TIME_SUNRISE = .25f;
    public const float TIME_DAY = .4f; // 9:30am - just to not be directly above and make shadows mildly interesting
    public const float TIME_SUNSET = .75f;

    [SerializeField] private Light sun;
    [SerializeField] private Light moon;
    [SerializeField] private VolumeProfile volumeProfile;
    [SerializeField] private Vector2 cloudOpacityRange = Vector2.one;

    private string LabelTimeOfDay => $"Time: {TimeOfDayText}";

    [SerializeField, Range(0, 1), LabelText("Temp Target Time")] private float tempTargetTimeOfDay = TIME_DAY;
    [SerializeField] private float tempTargetMoveSpeed = .2f;

    [SerializeField, DisableIf("@true"), LabelText("$TimeOfDayText")] private float globalTime = TIME_DAY;

    [ButtonGroup] private void Night() => SetTargetTime(TIME_NIGHT);
    [ButtonGroup] private void Sunrise() => SetTargetTime(TIME_SUNRISE);
    [ButtonGroup] private void Day() => SetTargetTime(TIME_DAY);
    [ButtonGroup] private void Sunset() => SetTargetTime(TIME_SUNSET);

    public static TimeOfDay Instance => _instance;

    public float CurrentTimeNormalized => globalTime % 1;
    public string TimeOfDayText => new DateTime().Add(TimeSpan.FromHours(CurrentTimeNormalized * 24)).ToString("hh:mm tt");
    public bool IsDay => CurrentTimeNormalized >= .25f && CurrentTimeNormalized <= .75f;
    public float LightLevel => 1 - 2 * Mathf.Abs(CurrentTimeNormalized - .5f); // 0 at 0, 1 and 1 at .5

    private IndirectLightingController indirectLightingController;
    private CloudLayer cloudLayer;
    private PhysicallyBasedSky physicallyBasedSky;

    void Awake()
    {
        if (_instance != null)
        {
            Debug.LogError("Already had a TimeOfDay defined, there should only be one!");
            if (Application.isPlaying) Destroy(this);
            return;
        }

        _instance = this;
        tempTargetTimeOfDay = globalTime;

        TryGetVolumeComponent(out indirectLightingController);
        TryGetVolumeComponent(out cloudLayer);
    }

    public void SetTargetTime(float time)
    {
        if (!Application.isPlaying)
        {
            // If not playing, just set the time and continue
            tempTargetTimeOfDay = time;
            return;
        }

        // Otherwise try to go to that time and loop around if necessary
        var day = (int)globalTime;
        var targetDayTime = day + time;

        // If we've already passed that time for today, go to next day
        if (globalTime > targetDayTime)
            targetDayTime += 1;

        tempTargetTimeOfDay = targetDayTime;
    }

    private void Update()
    {
        if (_instance == null) _instance = this;

        if (Karyo_GameCore.Instance != null)
        {
            InputManager inputManager = Karyo_GameCore.Instance.inputManager;

            if (inputManager.TimeOfDayActivate_Day) Day();
            if (inputManager.TimeOfDayActivate_Sunset) Sunset();
            if (inputManager.TimeOfDayActivate_Night) Night();
            if (inputManager.TimeOfDayActivate_Sunrise) Sunrise();
        }

        UpdateTempTargetTimeOfDay(Time.deltaTime);
        UpdateRotation();
    }

    // TODO: Replace this with a real time of day solution instead of a target we move towards
    private void UpdateTempTargetTimeOfDay(float deltaTime)
    {
        // Regular move towards, consider doing some wrapping or something fancier if we wanted...
        globalTime = Mathf.MoveTowards(globalTime, tempTargetTimeOfDay, deltaTime * tempTargetMoveSpeed);
    }

    private void UpdateRotation()
    {
        var degrees = globalTime * 360 - 180;

        // TODO: This is also moving the moon as a child of sun -- cleanup...
        sun.transform.localRotation = Quaternion.Euler(degrees, 0, 0);

        var daylight = IsDay;
        sun.shadows = daylight ? LightShadows.Soft : LightShadows.None;
        moon.shadows = daylight ? LightShadows.None : LightShadows.Soft;

        if (indirectLightingController != null)
        {
            indirectLightingController.indirectDiffuseLightingMultiplier.value = Mathf.Lerp(
                Karyo_GameCore.Instance.sceneConfiguration.NightAmbientMultiplier,
                Karyo_GameCore.Instance.sceneConfiguration.DaylightAmbientMultiplier,
                LightLevel);
        }

        if (cloudLayer != null)
        {
            cloudLayer.opacity.value = Mathf.Lerp(
                cloudOpacityRange.x, cloudOpacityRange.y, LightLevel
            );
        }
    }

    private bool TryGetVolumeComponent<T>(out T component) where T : VolumeComponent
    {
        component = default;
        return volumeProfile && Karyo_GameCore.Instance != null && volumeProfile.TryGet<T>(out component);
    }
}
