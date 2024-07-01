using UnityEngine;

// Temp class to advance time to next stage over a fixed interval
// TODO: This is very messy and should be rewritten
[RequireComponent(typeof(TimeOfDay))]
public class TimeOfDayAdvanceTimer : MonoBehaviour
{
    [SerializeField] private float intervalSeconds = 120;

    private static readonly float[] TimeIntervals = new[]
    {
        TimeOfDay.TIME_NIGHT,
        TimeOfDay.TIME_SUNRISE,
        TimeOfDay.TIME_DAY,
        TimeOfDay.TIME_SUNSET
    };

    private TimeOfDay _timeOfDay;
    private float _lastNormalizedTime;
    private float _timer = 0;

    private void Awake()
    {
        _timeOfDay = GetComponent<TimeOfDay>();
        _lastNormalizedTime = _timeOfDay.CurrentTimeNormalized;
        _timer = 0;
    }

    private void Update()
    {
        // If the time was changed outside of this, reset our timer.
        if (!Mathf.Approximately(_lastNormalizedTime, _timeOfDay.CurrentTimeNormalized))
        {
            _lastNormalizedTime = _timeOfDay.CurrentTimeNormalized;
            _timer = 0;
        }

        _timer += Time.deltaTime;

        if (_timer > intervalSeconds)
        {
            _timer = 0;

            // Advance to next one
            var advanced = false;
            for (var i = 0; i < TimeIntervals.Length; ++i)
            {
                if (_timeOfDay.CurrentTimeNormalized < TimeIntervals[i] - .1f) // Adjusting for float error
                {
                    advanced = true;
                    _timeOfDay.SetTargetTime(TimeIntervals[i]);
                    break;
                }
            }

            // If didn't advance, go to next day
            if (!advanced)
                _timeOfDay.SetTargetTime(TimeIntervals[0]);
        }
    }
}
