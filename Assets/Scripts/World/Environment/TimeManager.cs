using UnityEngine;
using SurvivalGame.Core.Managers;
using SurvivalGame.Core.Events;

namespace SurvivalGame.World.Environment
{
    public class TimeManager : ManagerBase
    {
        [Header("Time Settings")]
        [SerializeField] private int _startingHour = 6;
        [SerializeField] private int _startingMinute = 0;
        [SerializeField] private float _timeScale = 1f;
        [SerializeField] private int _minutesPerRealSecond = 1;

        [Header("Day/Night Settings")]
        [SerializeField] private int _sunriseHour = 6;
        [SerializeField] private int _sunsetHour = 18;
        [SerializeField] private float _transitionDuration = 1f;

        [Header("References")]
        [SerializeField] private Light _sunLight;
        [SerializeField] private Light _moonLight;
        [SerializeField] private Material _skyboxMaterial;

        private float _currentTime;
        private int _dayCount = 1;
        private bool _isPaused = false;

        public static TimeManager Instance => GetInstance<TimeManager>();

        public int CurrentHour => Mathf.FloorToInt(_currentTime) % 24;
        public int CurrentMinute => Mathf.FloorToInt((_currentTime % 1f) * 60f);
        public float CurrentTimeDecimal => _currentTime % 24f;
        public int DayCount => _dayCount;
        public bool IsDay => CurrentHour >= _sunriseHour && CurrentHour < _sunsetHour;
        public bool IsNight => !IsDay;

        public float TimeOfDayPercent
        {
            get
            {
                float time = CurrentTimeDecimal;
                return time / 24f;
            }
        }

        public float SunHeightPercent
        {
            get
            {
                float dayLength = _sunsetHour - _sunriseHour;
                float currentHour = CurrentHour + CurrentMinute / 60f;

                if (currentHour < _sunriseHour || currentHour > _sunsetHour)
                    return 0f;

                float midday = _sunriseHour + dayLength / 2f;
                float distanceFromMidday = Mathf.Abs(currentHour - midday);
                return 1f - (distanceFromMidday / (dayLength / 2f));
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            _currentTime = _startingHour + (_startingMinute / 60f);
            UpdateLighting();
        }

        private void Update()
        {
            if (!IsInitialized || _isPaused)
                return;

            float timeDelta = Time.deltaTime * _timeScale * (_minutesPerRealSecond / 60f);
            _currentTime += timeDelta;

            if (_currentTime >= 24f)
            {
                _currentTime -= 24f;
                _dayCount++;
                EventManager.TriggerEvent("OnNewDay", _dayCount);
            }

            UpdateLighting();
            EventManager.TriggerEvent(GameEvents.OnTimeOfDayChanged, CurrentTimeDecimal);
        }

        private void UpdateLighting()
        {
            float timePercent = TimeOfDayPercent;

            float sunRotation = Mathf.Lerp(-90f, 270f, timePercent);
            if (_sunLight != null)
            {
                _sunLight.transform.rotation = Quaternion.Euler(sunRotation, 170f, 0f);
            }

            float sunHeight = SunHeightPercent;
            if (_sunLight != null)
            {
                float sunIntensity = Mathf.Lerp(0f, 1.5f, sunHeight);
                _sunLight.intensity = sunIntensity;

                Color sunColor = Color.Lerp(Color.white, new Color(1f, 0.8f, 0.6f), 1f - sunHeight);
                _sunLight.color = sunColor;
            }

            if (_moonLight != null)
            {
                float moonIntensity = IsNight ? Mathf.Lerp(0.3f, 0.8f, 1f - sunHeight) : 0f;
                _moonLight.intensity = moonIntensity;
            }

            if (_skyboxMaterial != null)
            {
                float exposure = Mathf.Lerp(0.5f, 1.5f, sunHeight);
                _skyboxMaterial.SetFloat("_Exposure", exposure);
            }

            RenderSettings.ambientIntensity = Mathf.Lerp(0.2f, 1f, sunHeight);
        }

        public void SetTime(int hour, int minute = 0)
        {
            _currentTime = hour + (minute / 60f);
            UpdateLighting();
            EventManager.TriggerEvent(GameEvents.OnTimeOfDayChanged, CurrentTimeDecimal);
        }

        public void SetDay(int day)
        {
            _dayCount = day;
        }

        public void PauseTime()
        {
            _isPaused = true;
        }

        public void ResumeTime()
        {
            _isPaused = false;
        }

        public void SetTimeScale(float scale)
        {
            _timeScale = Mathf.Max(0.1f, scale);
        }

        public string GetTimeString()
        {
            return $"{CurrentHour:D2}:{CurrentMinute:D2}";
        }

        public string GetDateString()
        {
            return $"Day {_dayCount}";
        }

        public string GetFullTimeString()
        {
            return $"{GetDateString()} - {GetTimeString()}";
        }

        public TimeSaveData GetSaveData()
        {
            return new TimeSaveData
            {
                CurrentTime = _currentTime,
                DayCount = _dayCount,
                TimeScale = _timeScale
            };
        }

        public void LoadFromSaveData(TimeSaveData saveData)
        {
            if (saveData == null) return;

            _currentTime = saveData.CurrentTime;
            _dayCount = saveData.DayCount;
            _timeScale = saveData.TimeScale;

            UpdateLighting();
        }
    }

    [System.Serializable]
    public class TimeSaveData
    {
        public float CurrentTime;
        public int DayCount;
        public float TimeScale;
    }
}
