﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quran.Core.Data;
using Quran.Core.Interfaces;
using Quran.Core.Utils;
using Quran.Core.ViewModels;

namespace Quran.Core
{
    public class QuranApp : IDisposable
    {
        private QuranApp()
        { }

        private static MainViewModel mainViewModel = null;
        private static SearchViewModel searchViewModel = null;
        private static DetailsViewModel detailsViewModel = null;
        private static TranslationsListViewModel translationsListViewModel = null;
        private static RecitersListViewModel recitersListViewModel = null;
        private static SettingsViewModel settingsViewModel = null;
        private static AyahInfoDatabaseHandler ayahInfoDatabaseHandler = null;

        #region View Models
        public static INativeProvider NativeProvider { get; set; }

        public static async Task Initialize()
        {
            var instance = new ScreenInfo(QuranApp.NativeProvider.ActualWidth,
                                          QuranApp.NativeProvider.ActualHeight, 
                                          QuranApp.NativeProvider.ScaleFactor);

            await FileUtils.Initialize(instance);
        }

        public static async Task<AyahInfoDatabaseHandler> GetAyahInfoDatabase()
        {
            if (ayahInfoDatabaseHandler != null)
            {
                return ayahInfoDatabaseHandler;
            }

            if (await FileUtils.FileExists(FileUtils.DatabaseFolder, 
                FileUtils.GetAyaPositionFileName()))
            {
                ayahInfoDatabaseHandler = new AyahInfoDatabaseHandler(FileUtils.GetAyaPositionFileName());
            }

            return ayahInfoDatabaseHandler;
        }

        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The MainViewModel object.</returns>
        public static async Task<MainViewModel> GetMainViewModel()
        {
            // Delay creation of the view model until necessary
            if (mainViewModel == null)
            {
                mainViewModel = new MainViewModel();
                await mainViewModel.Initialize();
            }
            return mainViewModel;
        }

        public static async Task SyncViewModelsWithSettings()
        {
            if (mainViewModel != null)
            {
                await mainViewModel.Refresh();
            }
            if (searchViewModel != null)
            {
                await searchViewModel.Refresh();
            }
            if (detailsViewModel != null)
            {
                await detailsViewModel.Refresh();
            }
        }

        public void Dispose()
        {
            if (ayahInfoDatabaseHandler != null)
            {
                ayahInfoDatabaseHandler.Dispose();
            }
        }

        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The MainViewModel object.</returns>
        public static MainViewModel MainViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (mainViewModel == null)
                    mainViewModel = new MainViewModel();

                return mainViewModel;
            }
            set { mainViewModel = value; }
        }

        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The SearchViewModel object.</returns>
        public static SearchViewModel SearchViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (searchViewModel == null)
                    searchViewModel = new SearchViewModel();

                return searchViewModel;
            }
            set { searchViewModel = value; }
        }

        /// <summary>
        /// A static DetailsViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The DetailsViewModel object.</returns>
        public static DetailsViewModel DetailsViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (detailsViewModel == null)
                    detailsViewModel = new DetailsViewModel();

                return detailsViewModel;
            }
            set { detailsViewModel = value; }
        }

        /// <summary>
        /// A static TranslationsListViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The TranslationsListViewModel object.</returns>
        public static TranslationsListViewModel TranslationsListViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (translationsListViewModel == null)
                    translationsListViewModel = new TranslationsListViewModel();

                return translationsListViewModel;
            }
            set { translationsListViewModel = value; }
        }

        /// <summary>
        /// A static RecitersListViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The RecitersListViewModel object.</returns>
        public static RecitersListViewModel RecitersListViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (recitersListViewModel == null)
                    recitersListViewModel = new RecitersListViewModel();

                return recitersListViewModel;
            }
            set { recitersListViewModel = value; }
        }

        /// <summary>
        /// A static SettingsViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The SettingsViewModel object.</returns>
        public static SettingsViewModel SettingsViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (settingsViewModel == null)
                    settingsViewModel = new SettingsViewModel();

                return settingsViewModel;
            }
            set { settingsViewModel = value; }
        }
        #endregion
    }
}
