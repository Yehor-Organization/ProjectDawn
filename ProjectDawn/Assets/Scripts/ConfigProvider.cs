using UnityEngine;

public static class ConfigProvider
{
    private static AppConfig _config;

    public static AppConfig Config
    {
        get
        {
            if (_config == null)
            {
#if UNITY_EDITOR
                _config = Resources.Load<AppConfig>("AppConfig_Dev");
#else
                _config = Resources.Load<AppConfig>("AppConfig_Prod");
#endif

                if (_config == null)
                {
                    Debug.LogError("[ConfigProvider] AppConfig not found in Resources");
                    return null;
                }

                Normalize();
            }

            return _config;
        }
    }

    private static void Normalize()
    {
        if (!string.IsNullOrWhiteSpace(_config.APIBaseUrl))
        {
            _config.APIBaseUrl = _config.APIBaseUrl.TrimEnd('/');
        }
        else
        {
            Debug.LogError("[ConfigProvider] APIBaseUrl is empty");
        }
    }
}