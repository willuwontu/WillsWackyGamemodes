using BepInEx.Configuration;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;
using WWGM.GameModeModifiers;
using WWGM.GameModes;

namespace WWGM
{
    public static class ConfigManager
    {
        private static Dictionary<string, ConfigBase> configs = new Dictionary<string, WWGM.ConfigBase>();

        public static ReadOnlyDictionary<string, object> ConfigValues => new ReadOnlyDictionary<string, object>(configs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.BoxedCurrentValue));

        public static ConfigFile ConfigFile { get; private set; }

        public static void Setup(ConfigFile configFile)
        {
            ConfigFile = configFile;

            Unbound.RegisterHandshake(WillsWackyGameModes.ModId, OnHandshakeCompleted);

            GM_StudDraw.Setup();
            GM_RollingCardBar.Setup();
            GM_Draft.Setup();

            SingletonModifier.Setup();
            ExtraStartingPicks.Setup();
            WinnersNeedHugsToo.Setup();

        }

        #region ConfigSync

        private static void OnHandshakeCompleted()
        {

            if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(ConfigManager), nameof(SyncSettings), new object[]
                {
                    ConfigValues.Keys.ToArray(),
                    ConfigValues.Values.ToArray()
                }); 
            }
        }

        [UnboundRPC]
        private static void SyncSettings(string[] keys, object[] values)
        {
            if (keys.Length != values.Length)
            {
                throw new ArgumentException("Keys and Values are not the same length.");
            }

            for (int i = 0; i < keys.Length; i++)
            {
                configs[keys[i]].BoxedCurrentValue = values[i];
            }
        }

        #endregion ConfigSync

        public static Config<T> Bind<T>(string section, string key, T defaultValue, string description)
        {
            Config<T> config = new Config<T>(ConfigFile, section, key, defaultValue, description);

            configs.Add($"{section}.{key}", config);

            return config;
        }

        public static Config<T> GetConfig<T>(string section, string key)
        {
            string entry = $"{section}.{key}";

            if (!configs.ContainsKey(entry))
            {
                throw new ArgumentException($"Section: '{section}' Key: '{key}', does not exist for Config File '{ConfigFile.ConfigFilePath}'");
            }

            if (configs[entry].ConfigType != typeof(T))
            {
                throw new ArgumentException($"{ConfigFile.ConfigFilePath}.{entry} is of type '{configs[entry].ConfigType}' which does not match type '{typeof(T)}'");
            }

            return (Config<T>)configs[entry];
        }

        public static Config<T> GetConfig<T>(string section, string key, T defaultValue, string description = "")
        {
            string entry = $"{section}.{key}";

            if (!configs.ContainsKey(entry))
            {
                Bind<T>(section, key, defaultValue, description);
            }

            if (configs[entry].ConfigType != typeof(T))
            {
                throw new ArgumentException($"{ConfigFile.ConfigFilePath}.{entry} is of type '{configs[entry].ConfigType}' which does not match type '{typeof(T)}'");
            }

            return (Config<T>)configs[entry];
        }

        #region Get

        public static T GetCurrentValue<T>(string section, string key)
        {
            return GetConfig<T>(section, key).CurrentValue;
        }

        public static T GetCurrentValue<T>(string section, string key, T defaultValue, string description = "")
        {
            return GetConfig<T>(section, key, defaultValue, description).CurrentValue;
        }

        public static T GetConfigValue<T>(string section, string key)
        {
            return GetConfig<T>(section, key).ConfigValue;
        }

        public static T GetConfigValue<T>(string section, string key, T defaultValue, string description = "")
        {
            return GetConfig<T>(section, key, defaultValue, description).ConfigValue;
        }

        #endregion Get

        #region Set

        public static void SetCurrentValue<T>(string section, string key, T value)
        {
            Config<T> config = GetConfig<T>(section, key);

            config.CurrentValue = value;
        }

        public static void SetConfigValue<T>(string section, string key, T value)
        {
            Config<T> config = GetConfig<T>(section, key);

            config.ConfigValue = value;
        }

        #endregion Set

    }

    public abstract class ConfigBase
    {
        internal ConfigBase(Type configType, string section, string key)
        {
            ConfigType = configType ?? throw new ArgumentNullException(nameof(configType));
            Section = section;
            Key = key;
        }

        public Type ConfigType { get; protected private set; }

        public string Section { get; protected private set; }

        public string Key { get; protected private set; }

        public abstract object BoxedCurrentValue { get; set; }

        public abstract object BoxedConfigValue { get; set; }
    }

    public class Config<DataType> : ConfigBase
    {
        DataType currentValue;

        public ConfigEntry<DataType> ConfigEntry { get; protected private set; }

        /// <summary>
        /// The current value of this setting.
        /// </summary>
        public DataType CurrentValue
        {
            get => currentValue;
            set
            {
                currentValue = ClampValue(value);
            }
        }

        /// <summary>
        /// <para>The saved config value of this setting.</para>
        /// <para>Setting this value will also override the current value of the setting.</para>
        /// </summary>
        public DataType ConfigValue
        {
            get => ConfigEntry.Value;
            set
            {
                ConfigEntry.Value = value;
                currentValue = ConfigEntry.Value;
            }
        }

        public override object BoxedCurrentValue
        {
            get => CurrentValue;
            set => CurrentValue = (DataType)value;
        }

        public override object BoxedConfigValue
        {
            get => ConfigValue;
            set => ConfigValue = (DataType)value;
        }

        public void ResetValue()
        {
            this.CurrentValue = this.ConfigValue;
        }

        public void SaveCurrentValue()
        {
            this.ConfigValue = this.CurrentValue;
        }

        protected DataType ClampValue(DataType value)
        {
            if (ConfigEntry.Description.AcceptableValues != null)
                return (DataType)ConfigEntry.Description.AcceptableValues.Clamp(value);
            return value;
        }

        internal Config(ConfigFile configFile, string section, string key, DataType defaultValue, string description) : base(typeof(DataType), section, key)
        {
            this.ConfigEntry = configFile.Bind<DataType>(section, key, defaultValue, description);

            this.currentValue = this.ConfigEntry.Value;
        }

        //public static Constructor CreateInstance<DataType>()
    }
}
