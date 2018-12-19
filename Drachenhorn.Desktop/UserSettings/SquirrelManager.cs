﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Easy.Logger.Interfaces;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Win32;
using NuGet;
using Squirrel;

namespace Drachenhorn.Desktop.UserSettings
{
    public static class SquirrelManager
    {
        #region Squirrel

        private static readonly string GithubUpdatePath = "https://github.com/Drachenhorn-Team/Drachenhorn";

        private static IUpdateManager GetUpdateManager()
        {
            return UpdateManager.GitHubUpdateManager(GithubUpdatePath).Result;
        }

        public static void Startup()
        {
            SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Info("Startup");

            SquirrelAwareApp.HandleEvents(
                OnInitialInstall,
                OnAppUpdate,
                OnAppObsoleted,
                OnAppUninstall,
                OnFirstRun);
        }

        public static async Task<bool> IsUpdateAvailable(Action<int> progress = null)
        {
            try
            {
                SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Info("Checking for Update");
                using (var mgr = GetUpdateManager())
                {
                    var update = await mgr.CheckForUpdate(progress: progress);
                    if (update.ReleasesToApply.Any())
                    {
                        SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Info("Update available");
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                if (!e.Message.StartsWith("Update.exe not found"))
                    SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Warn("Error with Squirrel.", e);
            }

            return false;
        }

        public static async void UpdateSquirrel(Action<int> progress = null,
            Action<bool, SemanticVersion> finished = null)
        {
            try
            {
                SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Info("Doing Update");
                using (var mgr = GetUpdateManager())
                {
                    var release = await mgr.UpdateApp(progress);
                    SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Info("Update finished");
                    finished?.Invoke(true, release.Version);
                }
            }
            catch (Exception e)
            {
                if (!e.Message.StartsWith("Update.exe not found"))
                    SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Warn("Error with Squirrel.", e);
                finished?.Invoke(false, null);
            }
        }

        #endregion Squirrel

        #region Events

        private static void OnInitialInstall(Version version)
        {
            try
            {
                using (var mgr = new UpdateManager("C:"))
                {
                    SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Info("OnInitialInstall");
                    mgr.CreateShortcutForThisExe();

                    ExtractFileIcons(Path.Combine(mgr.RootAppDirectory, "icons"));

                    RegisterFileTypes(mgr.RootAppDirectory);

                    //UpdateManager.RestartAppWhenExited();
                    //Application.Current.Shutdown();
                }
            }
            catch (Exception e)
            {
                if (!e.Message.StartsWith("Update.exe not found"))
                    SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Warn("Error with Squirrel.", e);
            }
        }

        private static void OnAppUpdate(Version version)
        {
            SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Info("OnAppUpdate");

            using (var mgr = new UpdateManager("C:"))
            {
                ExtractFileIcons(Path.Combine(mgr.RootAppDirectory, "icons"));
            }

            //mgr.CreateShortcutForThisExe();
        }

        private static void OnAppObsoleted(Version version)
        {
            SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Info("OnAppObsolete");
        }

        private static void OnAppUninstall(Version version)
        {
            try
            {
                using (var mgr = new UpdateManager("C:"))
                {
                    SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Info("OnAppUninstall");
                    mgr.RemoveShortcutForThisExe();

                    // ReSharper disable once AssignNullToNotNullAttribute
                    var reg = new StreamReader(Assembly.GetExecutingAssembly()
                            .GetManifestResourceStream("Drachenhorn.Desktop.Resources.DrachenhornDelete.reg"))
                        .ReadToEnd();

                    var tempFile = Path.GetTempPath() + Guid.NewGuid() + ".reg";

                    File.WriteAllText(tempFile, reg);

                    // ReSharper disable once PossibleNullReferenceException
                    Process.Start("regedit.exe", "/s " + tempFile).WaitForExit();

                    Application.Current.Shutdown();
                }
            }
            catch (Exception e)
            {
                if (!e.Message.StartsWith("Update.exe not found"))
                    SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Warn("Error with Squirrel.", e);
            }
        }

        private static void OnFirstRun()
        {
            SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Info("OnAppUpdate");
        }

        #endregion Events

        #region Helper

        private static void ExtractFileIcons(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var fileNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            foreach (var fileName in fileNames)
            {
                if (!fileName.EndsWith(".ico")) continue;

                var split = fileName.Split('.');

                var filePath = Path.Combine(dir, split[split.Length - 2] + "." + split[split.Length - 1]);

                if (File.Exists(filePath))
                    File.Delete(filePath);

                using (var fileStream = File.Create(filePath))
                {
                    // ReSharper disable once PossibleNullReferenceException
                    Assembly.GetExecutingAssembly().GetManifestResourceStream(fileName).CopyTo(fileStream);
                }
            }
        }

        private static void RegisterFileTypes(string basePath)
        {
            if (Registry.ClassesRoot.GetSubKeyNames().All(x => x != "Drachenhorn"))
            {
                SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater")
                    .Info("Register File Extensions: " + basePath);

                // ReSharper disable once AssignNullToNotNullAttribute
                var reg = new StreamReader(Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("Drachenhorn.Desktop.Resources.Drachenhorn.reg")).ReadToEnd();

                reg = reg.Replace("%dir%", basePath.Replace("\\", "\\\\"));

                var tempFile = Path.GetTempPath() + Guid.NewGuid() + ".reg";

                File.WriteAllText(tempFile, reg);

                // ReSharper disable once PossibleNullReferenceException
                Process.Start("regedit.exe", "/s " + tempFile).WaitForExit();
            }
        }

        #endregion Helper
    }
}