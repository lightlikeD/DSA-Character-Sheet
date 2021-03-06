﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Drachenhorn.Core.Lang;
using Drachenhorn.Core.Settings;
using Drachenhorn.Core.UI;
using Drachenhorn.Xml;
using Drachenhorn.Xml.Template;
using Easy.Logger.Interfaces;
using GalaSoft.MvvmLight.Ioc;
using MahApps.Metro;

namespace Drachenhorn.Desktop.UserSettings
{
    [Serializable]
    public class Settings : BindableBase, ISettings
    {
        #region c'tor

        public Settings()
        {
            PropertyChanged += (sender, args) => { Save(); };
            LastOpenFiles.CollectionChanged += (sender, args) => { Save(); };
        }

        #endregion

        [XmlIgnore]
        public bool IsNew
        {
            get => _isNew;
            private set
            {
                if (_isNew == value)
                    return;
                _isNew = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public CultureInfo CurrentCulture
        {
            get => SimpleIoc.Default.GetInstance<LanguageManager>().CurrentCulture;
            set
            {
                var temp = SimpleIoc.Default.GetInstance<LanguageManager>();
                if (Equals(temp.CurrentCulture, value))
                    return;
                temp.CurrentCulture = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore] public string Version => SquirrelManager.CurrentVersion.ToString();

        [XmlIgnore] public string NewVersion => SquirrelManager.NewVersion;

        [XmlElement("AccentColor")]
        public string AccentColor
        {
            get => _accentColor;
            set
            {
                if (_accentColor == value)
                    return;
                _accentColor = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("CurrentTemplatePath")]
        public string CurrentTemplatePath
        {
            get => CurrentTemplate?.Path;
            set => CurrentTemplate = TemplateManager.Manager.GetTemplate(value).EntireTemplate;
        }

        [XmlElement("VisualTheme")]
        public VisualThemeType VisualTheme
        {
            get => _visualTheme;
            set
            {
                if (_visualTheme == value)
                    return;
                _visualTheme = value;
                OnPropertyChanged();
            }
        }


        [XmlIgnore]
        public SheetTemplate CurrentTemplate
        {
            get => _currentTemplate;
            set
            {
                if (_currentTemplate == value)
                    return;
                _currentTemplate = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        [XmlElement("LastOpenFile")]
        public ObservableCollection<string> LastOpenFiles
        {
            get => _lastOpenFiles;
            set
            {
                if (_lastOpenFiles == value)
                    return;
                _lastOpenFiles = value;
                OnPropertyChanged();
            }
        }

        #region AccentColors

        public static IEnumerable<string> GetAccents()
        {
            return ThemeManager.Accents.Select(x => x.Name).OrderBy(x => x);
        }

        #endregion AccentColors

        #region Properties

        [XmlIgnore] private string _accentColor;

        [XmlIgnore] private SheetTemplate _currentTemplate;

        [XmlIgnore] private bool _isNew = true;

        [XmlIgnore] private VisualThemeType _visualTheme;

        [XmlIgnore] private ObservableCollection<string> _lastOpenFiles = new ObservableCollection<string>();

        [XmlElement("CurrentCulture")]
        public string CurrentCultureString
        {
            get => CurrentCulture.Name;
            set
            {
                try
                {
                    CurrentCulture = new CultureInfo(value);
                }
                catch (Exception)
                {
                    CurrentCulture = CultureInfo.CurrentUICulture;
                }
            }
        }

        #endregion

        #region Save/Load

        private static readonly string PropertiesDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Drachenhorn");

        private static string PropertiesPath => Path.Combine(PropertiesDirectory, "config.xml");

        /// <summary>
        ///     Loads the Properties from the "PROPERTIESDIRECTORY".
        /// </summary>
        /// <returns>The Loaded Properties.</returns>
        public static Settings Load()
        {
            if (!Directory.Exists(PropertiesDirectory))
                Directory.CreateDirectory(PropertiesDirectory);

            SimpleIoc.Default.GetInstance<ILogService>().GetLogger<Settings>().Info("Loading settings.");

            try
            {
                using (var stream = new FileStream(PropertiesPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var serializer = new XmlSerializer(typeof(Settings));
                    var temp = (Settings) serializer.Deserialize(stream);
                    temp.IsNew = false;

                    SimpleIoc.Default.GetInstance<ILogService>().GetLogger<Settings>()
                        .Debug("Finished loading Settings.");

                    return temp;
                }
            }
            catch (IOException)
            {
                SimpleIoc.Default.GetInstance<ILogService>().GetLogger<Settings>()
                    .Warn("Settings not found. Generating new.");
                var s = new Settings();
                s.Save();
                return s;
            }
            catch (InvalidOperationException)
            {
                MessageFactory.NewMessage()
                    .MessageTranslated("Notification.Settings.Corrupted")
                    .Title("Dialog.Error")
                    .ShowMessage();

                SimpleIoc.Default.GetInstance<ILogService>().GetLogger<Settings>()
                    .Warn("Settings corrupted. Generating new.");
                var s = new Settings();
                s.Save();
                return s;
            }
        }

        /// <summary>
        ///     Saves the Properties to the "PROPERTIESDIRECTORY"
        /// </summary>
        public void Save()
        {
            if (!Directory.Exists(PropertiesDirectory))
                Directory.CreateDirectory(PropertiesDirectory);

            SimpleIoc.Default.GetInstance<ILogService>().GetLogger<Settings>().Debug("Saving settings.");

            try
            {
                using (var stream = new StreamWriter(PropertiesPath))
                {
                    var serializer = new XmlSerializer(typeof(Settings));
                    serializer.Serialize(stream, this);
                }
            }
            catch (IOException)
            {
            }
        }

        #endregion Save/Load
    }
}