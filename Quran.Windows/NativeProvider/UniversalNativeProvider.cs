﻿using System;
using System.Collections.Generic;
using Windows.Storage;
using Quran.Core.Interfaces;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.ApplicationModel.Email;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Quran.Windows.Utils;
using Windows.UI.ViewManagement;
using System.IO.Compression;
using Windows.System.Display;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using System.IO;
using Quran.Core.Utils;
using Microsoft.ApplicationInsights;

namespace Quran.Windows.NativeProvider
{
    public class UniversalNativeProvider : INativeProvider
    {
        DisplayRequest _keepScreenOnRequest = null;
        private static TelemetryClient telemetry = new TelemetryClient();

        public double ActualWidth
        {
            get
            {
                return ApplicationView.GetForCurrentView().VisibleBounds.Width;
            }
        }

        public double ActualHeight
        {
            get
            {
                return ApplicationView.GetForCurrentView().VisibleBounds.Height;
            }
        }

        public double ScaleFactor
        {
            get
            {
                return DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            }
        }

        private ISettingsProvider settingsProvider;
        public ISettingsProvider SettingsProvider
        {
            get
            {
                if (settingsProvider == null)
                    settingsProvider = new UniversalSettingsProvider();
                return settingsProvider;
            }
        }

        private IAudioProvider audioProvider;
        public IAudioProvider AudioProvider
        {
            get
            {
                if (audioProvider == null)
                    audioProvider = new UniversalAudioProvider();
                return audioProvider;
            }
        }

		public async Task ExtractZip(StorageFile source, StorageFolder baseFolder)
        {
            using (var fileStream = await source.OpenStreamForReadAsync())
            {
                ZipArchive archive = new ZipArchive(fileStream);
                foreach (ZipArchiveEntry file in archive.Entries)
                {
                    string completeFileName = Path.Combine(baseFolder.Path, file.FullName);

                    if (file.Name == "")
                    {
                        // Assuming Empty for Directory
                        Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                        continue;
                    }

                    file.ExtractToFile(completeFileName, true);
                }
            }
        }

        public async Task ExtractZip2(StorageFile source, StorageFolder baseFolder)
        {
            try
            {
                await CompressionUtils.UnZipFileAync(source, baseFolder);
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex, new Dictionary<string, string> { { "Scenario", "ExtractingArchive" } });
            }
        }


        public void CopyToClipboard(string text)
        {
            DataPackage dp = new DataPackage();
            dp.SetText(text);
            Clipboard.SetContent(dp);
        }

        public async Task ComposeEmail(string to, string subject, string body = "")
        {
            EmailMessage email = new EmailMessage();
            email.To.Add(new EmailRecipient(to));
            email.Subject = subject;
            if (body != null)
            {
                email.Body = body;
            }
            await EmailManager.ShowComposeNewEmailAsync(email);
        }

        public async Task LaunchWebBrowser(string url)
        {
            await Launcher.LaunchUriAsync(new Uri(url));
        }

        public void ToggleDeviceSleep(bool enable)
        {
            if (!enable)
            {
                _keepScreenOnRequest = new DisplayRequest();
                _keepScreenOnRequest.RequestActive();
            }
            else
            {
                if (_keepScreenOnRequest != null)
                {
                    _keepScreenOnRequest.RequestRelease();
                    _keepScreenOnRequest = null;
                }
            }
        }

        public async Task ShowInfoMessageBox(string text)
        {
            var dialog = new MessageDialog(text);
            await dialog.ShowAsync();
        }

        public async Task ShowInfoMessageBox(string text, string title)
        {
            var dialog = new MessageDialog(text, title);
            dialog.Commands.Add(new UICommand { Label = "OK", Id = 0 });
            await dialog.ShowAsync();
        }

        public async Task<bool> ShowQuestionMessageBox(string text)
        {
            var dialog = new MessageDialog(text);
            dialog.Commands.Add(new UICommand { Label = "OK", Id = 0 });
            dialog.Commands.Add(new UICommand { Label = "Cancel", Id = 1 });
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;
            IUICommand result = await dialog.ShowAsync();
            return (int)result.Id == 0;
        }

        public async Task<bool> ShowQuestionMessageBox(string text, string title)
        {
            var dialog = new MessageDialog(text, title);
            dialog.Commands.Add(new UICommand { Label = "OK", Id = 0 });
            dialog.Commands.Add(new UICommand { Label = "Cancel", Id = 1 });
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;
            IUICommand result = await dialog.ShowAsync();
            return (int)result.Id == 0;
        }

        public Task ShowErrorMessageBox(string text)
        {
            return ShowInfoMessageBox(text);
        }

        public void Log(string text)
        {
            // TODO: Implement
        }    
    }
}
