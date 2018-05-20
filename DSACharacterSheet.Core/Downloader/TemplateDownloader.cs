﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using DSACharacterSheet.Core.IO;
using DSACharacterSheet.Xml;
using Easy.Logger.Interfaces;
using GalaSoft.MvvmLight.Ioc;

namespace DSACharacterSheet.Core.Downloader
{
    public class TemplateDownloader : BindableBase
    {
        private static readonly string RawPath =
            "https://gist.githubusercontent.com/lightlike/2a26930578a805d1739fe598404e60cb/raw/";

        #region Properties

        private bool _isConnectionSuccessful = true;

        public bool IsConnectionSuccessful
        {
            get { return _isConnectionSuccessful; }
            private set
            {
                if (_isConnectionSuccessful == value)
                    return;
                _isConnectionSuccessful = value;
                OnPropertyChanged();
            }
        }


        private ObservableCollection<OnlineTemplate> _templates = new ObservableCollection<OnlineTemplate>();

        public ObservableCollection<OnlineTemplate> Templates
        {
            get
            {
                if (!_templates.Any())
                    Task.Run(() =>
                    {
                        var templates = LoadTemplatesAsync().Result;
                        IsConnectionSuccessful = templates != null;
                        Templates = templates;
                    });
                return _templates;
            }
            private set
            {
                if (_templates == value)
                    return;
                _templates = value;
                OnPropertyChanged();
            }
        }

        #endregion Properties

        #region Loading

        /// <summary>
        /// Loads the templates from an online Source
        /// </summary>
        /// <returns>true if connection is successful.</returns>
        private async Task<ObservableCollection<OnlineTemplate>> LoadTemplatesAsync()
        {
            return await Task.Run(() => {
                var textFromFile = "";

                var result = new ObservableCollection<OnlineTemplate>();

                try
                {
                    SimpleIoc.Default.GetInstance<ILogService>().GetLogger<TemplateDownloader>().Info("Downloading online Templates.");

                    textFromFile = (new WebClient()).DownloadString(RawPath);
                }
                catch (WebException e)
                {
                    SimpleIoc.Default.GetInstance<ILogService>().GetLogger<TemplateDownloader>().Warn("Could not connect to Template-Service", e);
                    return null;
                }

                var lines = textFromFile.Split(new[] { '\r', '\n' });

                foreach (var line in lines)
                {
                    result.Add(new OnlineTemplate(line));
                }

                return result;
            });
        }

        #endregion Loading


        #region Download

        private bool _isDownloadInitialized = false;

        public void Download(OnlineTemplate template)
        {
            template.IsDownloadStarted = true;

            StartDownloads();
        }

        private void StartDownloads()
        {
            if (_isDownloadInitialized)
                return;

            _isDownloadInitialized = true;

            Task.Run(async () =>
            {
                while (Templates.Any(x => x.IsDownloadStarted))
                {
                    var nextItem = Templates.First(x => x.IsDownloadStarted);

                    SimpleIoc.Default.GetInstance<ILogService>().GetLogger<TemplateDownloader>().Info("Downloading template: " + nextItem.ToString());

                    await nextItem.TryDownload();
                }

                _isDownloadInitialized = false;
            });
        }

        #endregion Download
    }
}
