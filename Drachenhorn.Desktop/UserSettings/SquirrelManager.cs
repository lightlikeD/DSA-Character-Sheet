﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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

        public static void Startup()
        {
            try
            {
                using (var mgr = new UpdateManager("C:"))
                {
                    SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Info("Starting");

                    SquirrelAwareApp.HandleEvents(
                        OnInitialInstall,
                        OnAppUpdate,
                        OnAppObsoleted,
                        OnAppUninstall,
                        OnFirstRun);
                }
            }
            catch (Exception e)
            {
                // ignored
#if !DEBUG
                SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Warn("Error with Squirrel.", e);
#endif
                return;
            }
        }

        public static async Task<bool> IsUpdateAvailable(Action<int> progress = null, Action<SemanticVersion> onIsUpdateAvailable = null)
        {
            try
            {
                using (var mgr = UpdateManager.GitHubUpdateManager("https://github.com/Drachenhorn-Team/Drachenhorn").Result)
                {
                    var update = await mgr.CheckForUpdate(progress: progress);
                    if (update.ReleasesToApply.Any())
                    {
                        onIsUpdateAvailable?.Invoke(update.FutureReleaseEntry.Version);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                // ignored
#if RELEASE
                SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Warn("Error with Squirrel.", e);
#endif
            }
            return false;
        }

        public static async void UpdateSquirrel(Action<int> progress = null, Action<bool, SemanticVersion> finished = null)
        {
            try
            {
                using (var mgr = UpdateManager.GitHubUpdateManager("https://github.com/Drachenhorn-Team/Drachenhorn").Result)
                {
                    var release = await mgr.UpdateApp(progress);
                    finished?.Invoke(true, release.Version);
                }
            }
            catch (Exception e)
            {
                // ignored
#if RELEASE
                SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Warn("Error with Squirrel.", e);
#endif
            }

            finished?.Invoke(true, null);
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

                    UpdateManager.RestartApp();
                }
            }
            catch (Exception e)
            {
                // ignored
#if RELEASE
                SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Warn("Error with Squirrel.", e);
#endif
            }
        }

        private static void OnAppUpdate(Version version)
        {
            SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Info("OnAppUpdate");
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
                // ignored
#if RELEASE
                SimpleIoc.Default.GetInstance<ILogService>().GetLogger("Updater").Warn("Error with Squirrel.", e);
#endif
            }
        }

        private static void OnFirstRun()
        {
            using (var mgr = new UpdateManager("C:"))
                ExtractFileIcons(Path.Combine(mgr.RootAppDirectory, "icons"));

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

                using (FileStream fileStream = File.Create(
                    Path.Combine(dir, split[split.Length - 2] + "." + split[split.Length - 1])))
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