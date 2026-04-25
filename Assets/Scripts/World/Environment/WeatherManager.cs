using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Core.Managers;
using SurvivalGame.Core.Events;

namespace SurvivalGame.World.Environment
{
    public class WeatherManager : ManagerBase
    {
        [Header("Weather Settings")]
        [SerializeField] private WeatherType _initialWeather = WeatherType.Clear;
        [SerializeField] private bool _enableWeatherChanges = true;
        [SerializeField] private float _minWeatherDuration = 120f;
        [SerializeField] private float _maxWeatherDuration = 600f;
        [SerializeField] private float _transitionDuration = 5f;

        [Header("Weather Probabilities")]
        [Range(0f, 1f)]
        [SerializeField] private float _clearProbability = 0.6f;
        [Range(0f, 1f)]
        [SerializeField] private float _cloudyProbability = 0.2f;
        [Range(0f, 1f)]
        [SerializeField] private float _rainyProbability = 0.15f;
        [Range(0f, 1f)]
        [SerializeField] private float _stormyProbability = 0.05f;

        [Header("References")]
        [SerializeField] private Light _sunLight;
        [SerializeField] private Material _skyboxMaterial;
        [SerializeField] private ParticleSystem _rainParticle;
        [SerializeField] private ParticleSystem _stormParticle;
        [SerializeField] private GameObject _clouds;
        [SerializeField] private AudioSource _ambientAudioSource;

        [Header("Audio")]
        [SerializeField] private AudioClip _rainSound;
        [SerializeField] private AudioClip _stormSound;
        [SerializeField] private AudioClip _windSound;

        private WeatherType _currentWeather;
        private WeatherType _targetWeather;
        private float _weatherTimer;
        private float _transitionTimer;
        private bool _isTransitioning;

        private float _currentSunIntensity;
        private Color _currentSkyTint;
        private float _currentRainIntensity;
        private float _currentCloudDensity;

        public static WeatherManager Instance => GetInstance<WeatherManager>();

        public WeatherType CurrentWeather => _currentWeather;
        public WeatherType TargetWeather => _targetWeather;
        public bool IsTransitioning => _isTransitioning;
        public float TransitionProgress => _isTransitioning ? _transitionTimer / _transitionDuration : 1f;

        public bool IsRaining => _currentWeather == WeatherType.Rainy || _currentWeather == WeatherType.Stormy;
        public bool IsStormy => _currentWeather == WeatherType.Stormy;
        public bool IsCloudy => _currentWeather == WeatherType.Cloudy || IsRaining;

        public override void Initialize()
        {
            base.Initialize();

            _currentWeather = _initialWeather;
            _targetWeather = _initialWeather;
            _weatherTimer = Random.Range(_minWeatherDuration, _maxWeatherDuration);

            ApplyWeather(_currentWeather, 1f);
        }

        private void Update()
        {
            if (!IsInitialized)
                return;

            if (_isTransitioning)
            {
                UpdateTransition();
            }
            else
            {
                if (_enableWeatherChanges)
                {
                    UpdateWeatherTimer();
                }
            }

            UpdateWeatherEffects();
        }

        private void UpdateWeatherTimer()
        {
            _weatherTimer -= Time.deltaTime;

            if (_weatherTimer <= 0f)
            {
                ChooseNewWeather();
            }
        }

        private void ChooseNewWeather()
        {
            float random = Random.value;
            float cumulative = 0f;

            cumulative += _clearProbability;
            if (random <= cumulative)
            {
                SetWeather(WeatherType.Clear);
                return;
            }

            cumulative += _cloudyProbability;
            if (random <= cumulative)
            {
                SetWeather(WeatherType.Cloudy);
                return;
            }

            cumulative += _rainyProbability;
            if (random <= cumulative)
            {
                SetWeather(WeatherType.Rainy);
                return;
            }

            SetWeather(WeatherType.Stormy);
        }

        private void UpdateTransition()
        {
            _transitionTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(_transitionTimer / _transitionDuration);

            ApplyWeather(_targetWeather, progress);

            if (progress >= 1f)
            {
                _isTransitioning = false;
                _currentWeather = _targetWeather;
                _weatherTimer = Random.Range(_minWeatherDuration, _maxWeatherDuration);

                EventManager.TriggerEvent(GameEvents.OnWeatherChanged, _currentWeather);
            }
        }

        private void ApplyWeather(WeatherType weather, float progress)
        {
            switch (weather)
            {
                case WeatherType.Clear:
                    _currentSunIntensity = Mathf.Lerp(_currentSunIntensity, 1.5f, progress);
                    _currentSkyTint = Color.Lerp(_currentSkyTint, Color.white, progress);
                    _currentRainIntensity = Mathf.Lerp(_currentRainIntensity, 0f, progress);
                    _currentCloudDensity = Mathf.Lerp(_currentCloudDensity, 0.1f, progress);
                    break;

                case WeatherType.Cloudy:
                    _currentSunIntensity = Mathf.Lerp(_currentSunIntensity, 0.8f, progress);
                    _currentSkyTint = Color.Lerp(_currentSkyTint, new Color(0.8f, 0.8f, 0.85f), progress);
                    _currentRainIntensity = Mathf.Lerp(_currentRainIntensity, 0f, progress);
                    _currentCloudDensity = Mathf.Lerp(_currentCloudDensity, 0.7f, progress);
                    break;

                case WeatherType.Rainy:
                    _currentSunIntensity = Mathf.Lerp(_currentSunIntensity, 0.5f, progress);
                    _currentSkyTint = Color.Lerp(_currentSkyTint, new Color(0.6f, 0.65f, 0.7f), progress);
                    _currentRainIntensity = Mathf.Lerp(_currentRainIntensity, 0.6f, progress);
                    _currentCloudDensity = Mathf.Lerp(_currentCloudDensity, 0.9f, progress);
                    break;

                case WeatherType.Stormy:
                    _currentSunIntensity = Mathf.Lerp(_currentSunIntensity, 0.3f, progress);
                    _currentSkyTint = Color.Lerp(_currentSkyTint, new Color(0.4f, 0.4f, 0.5f), progress);
                    _currentRainIntensity = Mathf.Lerp(_currentRainIntensity, 1f, progress);
                    _currentCloudDensity = Mathf.Lerp(_currentCloudDensity, 1f, progress);
                    break;
            }
        }

        private void UpdateWeatherEffects()
        {
            if (_sunLight != null)
            {
                _sunLight.intensity = _currentSunIntensity;
            }

            if (_skyboxMaterial != null)
            {
                _skyboxMaterial.SetColor("_Tint", _currentSkyTint);
            }

            if (_clouds != null)
            {
                Renderer[] cloudRenderers = _clouds.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in cloudRenderers)
                {
                    foreach (Material mat in renderer.materials)
                    {
                        mat.SetFloat("_Density", _currentCloudDensity);
                    }
                }
            }

            UpdateRainEffects();
            UpdateAmbientAudio();
        }

        private void UpdateRainEffects()
        {
            if (_rainParticle != null)
            {
                if (_currentRainIntensity > 0.01f && _currentWeather != WeatherType.Stormy)
                {
                    if (!_rainParticle.isPlaying)
                    {
                        _rainParticle.Play();
                    }

                    var emission = _rainParticle.emission;
                    emission.rateOverTime = 500f * _currentRainIntensity;
                }
                else
                {
                    if (_rainParticle.isPlaying)
                    {
                        _rainParticle.Stop();
                    }
                }
            }

            if (_stormParticle != null)
            {
                if (_currentRainIntensity > 0.5f && _currentWeather == WeatherType.Stormy)
                {
                    if (!_stormParticle.isPlaying)
                    {
                        _stormParticle.Play();
                    }

                    var emission = _stormParticle.emission;
                    emission.rateOverTime = 200f * _currentRainIntensity;
                }
                else
                {
                    if (_stormParticle.isPlaying)
                    {
                        _stormParticle.Stop();
                    }
                }
            }
        }

        private void UpdateAmbientAudio()
        {
            if (_ambientAudioSource == null) return;

            if (IsStormy)
            {
                if (_ambientAudioSource.clip != _stormSound || !_ambientAudioSource.isPlaying)
                {
                    _ambientAudioSource.clip = _stormSound;
                    _ambientAudioSource.loop = true;
                    _ambientAudioSource.Play();
                }
                _ambientAudioSource.volume = 0.8f * _currentRainIntensity;
            }
            else if (IsRaining)
            {
                if (_ambientAudioSource.clip != _rainSound || !_ambientAudioSource.isPlaying)
                {
                    _ambientAudioSource.clip = _rainSound;
                    _ambientAudioSource.loop = true;
                    _ambientAudioSource.Play();
                }
                _ambientAudioSource.volume = 0.5f * _currentRainIntensity;
            }
            else
            {
                if (_ambientAudioSource.isPlaying)
                {
                    _ambientAudioSource.Stop();
                }
            }
        }

        public void SetWeather(WeatherType weather)
        {
            if (weather == _currentWeather && !_isTransitioning)
                return;

            _targetWeather = weather;
            _transitionTimer = 0f;
            _isTransitioning = true;
        }

        public void ForceWeather(WeatherType weather)
        {
            _targetWeather = weather;
            _currentWeather = weather;
            _isTransitioning = false;

            ApplyWeather(weather, 1f);
            _weatherTimer = Random.Range(_minWeatherDuration, _maxWeatherDuration);

            EventManager.TriggerEvent(GameEvents.OnWeatherChanged, _currentWeather);
        }

        public void EnableWeatherChanges(bool enable)
        {
            _enableWeatherChanges = enable;
        }

        public WeatherSaveData GetSaveData()
        {
            return new WeatherSaveData
            {
                CurrentWeather = (int)_currentWeather,
                TargetWeather = (int)_targetWeather,
                WeatherTimer = _weatherTimer,
                IsTransitioning = _isTransitioning,
                TransitionTimer = _transitionTimer
            };
        }

        public void LoadFromSaveData(WeatherSaveData saveData)
        {
            if (saveData == null) return;

            _currentWeather = (WeatherType)saveData.CurrentWeather;
            _targetWeather = (WeatherType)saveData.TargetWeather;
            _weatherTimer = saveData.WeatherTimer;
            _isTransitioning = saveData.IsTransitioning;
            _transitionTimer = saveData.TransitionTimer;

            if (!_isTransitioning)
            {
                ApplyWeather(_currentWeather, 1f);
            }
        }
    }

    public enum WeatherType
    {
        Clear,
        Cloudy,
        Rainy,
        Stormy
    }

    [System.Serializable]
    public class WeatherSaveData
    {
        public int CurrentWeather;
        public int TargetWeather;
        public float WeatherTimer;
        public bool IsTransitioning;
        public float TransitionTimer;
    }
}
