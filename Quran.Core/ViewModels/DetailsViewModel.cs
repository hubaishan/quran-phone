// --------------------------------------------------------------------------------------------------------------------
// <summary>
//    Defines the DetailsViewModel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Quran.Core.Common;
using Quran.Core.Data;
using Quran.Core.Properties;
using Quran.Core.Utils;
using System.IO;
using Windows.UI.Xaml;
using Windows.Graphics.Display;
using Microsoft.ApplicationInsights;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Quran.Core.ViewModels
{
    /// <summary>
    /// Define the DetailsViewModel type.
    /// </summary>
    public partial class DetailsViewModel : ViewModelWithDownload
    {
        private TelemetryClient telemetry = new TelemetryClient();

        public DetailsViewModel()
        {
            Pages = new ObservableCollection<PageViewModel>();
            QuranApp.NativeProvider.AudioProvider.StateChanged += AudioProvider_StateChanged;
            QuranApp.NativeProvider.AudioProvider.TrackChanged += AudioProvider_TrackChanged;
        }

        public override async Task Initialize()
        {
            IsLoading = true;
            ClearPages();
            Orientation = DisplayInformation.GetForCurrentView().CurrentOrientation;
            if (SettingsUtils.Get<bool>(Constants.PREF_NIGHT_MODE))
            {
                Theme = ElementTheme.Dark;
            }
            else
            {
                Theme = ElementTheme.Light;
            }
            KeepInfoOverlay = SettingsUtils.Get<bool>(Constants.PREF_KEEP_INFO_OVERLAY);
            //Update translations
            var translation = SettingsUtils.Get<string>(Constants.PREF_ACTIVE_TRANSLATION);
            if (!string.IsNullOrEmpty(translation))
            {
                TranslationFile = translation.Split('|')[0];
                if (await HasTranslationFile())
                {
                    ShowTranslation = SettingsUtils.Get<bool>(Constants.PREF_SHOW_TRANSLATION);
                    ShowArabicInTranslation = SettingsUtils.Get<bool>(Constants.PREF_SHOW_ARABIC_IN_TRANSLATION);
                }
            }
            await CreatePages();
            await LoadCurrentPage();
            await base.Initialize();
            IsLoading = false;
        }

        #region Properties
        public ObservableCollection<PageViewModel> Pages { get; private set; }

        private ElementTheme theme;
        public ElementTheme Theme
        {
            get { return theme; }
            set
            {
                if (value == theme)
                    return;

                theme = value;

                base.OnPropertyChanged(() => Theme);
            }
        }

        private string translationFile;
        public string TranslationFile
        {
            get { return translationFile; }
            set
            {
                if (value == translationFile)
                    return;

                translationFile = value;

                base.OnPropertyChanged(() => TranslationFile);
            }
        }

        private bool showTranslation;
        public bool ShowTranslation
        {
            get { return showTranslation; }
            set
            {
                if (value == showTranslation)
                    return;

                showTranslation = value;

                base.OnPropertyChanged(() => ShowTranslation);
            }
        }

        private bool showArabicInTranslation;
        public bool ShowArabicInTranslation
        {
            get { return showArabicInTranslation; }
            set
            {
                if (value == showArabicInTranslation)
                    return;

                showArabicInTranslation = value;

                base.OnPropertyChanged(() => ShowArabicInTranslation);
            }
        }

        public int CurrentPageNumber
        {
            get
            {
                return GetPageNumberFromIndex(CurrentPageIndex);
            }
            set
            {
                CurrentPageIndex = GetIndexFromPageNumber(value);
            }
        }

        private string getJuzPart(int rub)
        {
            switch (rub % 8)
            {
                case 0:
                    return "";
                case 1:
                    return "⅛";
                case 2:
                    return "¼";
                case 3:
                    return "⅜";
                case 4:
                    return "½";
                case 5:
                    return "⅝";
                case 6:
                    return "¾";
                case 7:
                    return "⅞";
                default:
                    return "";
            }
        }

        private string currentSurahName;
        public string CurrentSurahName
        {
            get { return currentSurahName; }
            set
            {
                if (value == currentSurahName)
                    return;

                currentSurahName = value;

                base.OnPropertyChanged(() => CurrentSurahName);
            }
        }

        private int currentSurahNumber;
        public int CurrentSurahNumber
        {
            get { return currentSurahNumber; }
            set
            {
                if (value == currentSurahNumber)
                    return;

                currentSurahNumber = value;

                base.OnPropertyChanged(() => CurrentSurahNumber);
            }
        }

        private string currentJuzName;
        public string CurrentJuzName
        {
            get { return currentJuzName; }
            set
            {
                if (value == currentJuzName)
                    return;

                currentJuzName = value;

                base.OnPropertyChanged(() => CurrentJuzName);
            }
        }

        private bool currentPageBookmarked;
        public bool CurrentPageBookmarked
        {
            get { return currentPageBookmarked; }
            set
            {
                if (value == currentPageBookmarked)
                    return;

                currentPageBookmarked = value;

                base.OnPropertyChanged(() => CurrentPageBookmarked);
            }
        }

        private int currentPageIndex = -1;
        public int CurrentPageIndex
        {
            get { return currentPageIndex; }
            set
            {
                if (value == currentPageIndex)
                    return;

                currentPageIndex = value;
                
                if (value >= 0)
                {
                    LoadCurrentPage();
                }

                base.OnPropertyChanged(() => CurrentPageIndex);
                base.OnPropertyChanged(() => CurrentPageNumber);
                base.OnPropertyChanged(() => CurrentPage);
            }
        }

        public PageViewModel CurrentPage
        {
            get
            {
                if (CurrentPageIndex >= 0 && CurrentPageIndex < Pages.Count)
                    return Pages[CurrentPageIndex];
                else
                    return null;
            }
            set
            {
                if (value != null)
                {
                    CurrentPageIndex = GetIndexFromPageNumber(value.PageNumber);
                }
            }
        }

        private DisplayOrientations orientation;
        public DisplayOrientations Orientation
        {
            get { return orientation; }
            set
            {
                if (value == orientation)
                    return;

                orientation = value;

                base.OnPropertyChanged(() => Orientation);

                // directly affect KeepInfoOverlay and ManuButtonOpacity
                base.OnPropertyChanged(() => KeepInfoOverlay);
            }
        }
        
        private bool keepInfoOverlay;
        public bool KeepInfoOverlay
        {
            get
            {
                if (Orientation == DisplayOrientations.Landscape ||
                    Orientation == DisplayOrientations.LandscapeFlipped)
                {
                    return keepInfoOverlay;
                }
                return true;
            }
            set
            {
                if (value == keepInfoOverlay)
                    return;

                keepInfoOverlay = value;

                base.OnPropertyChanged(() => KeepInfoOverlay);
            }
        }

        private QuranAyah selectedAyah;
        public QuranAyah SelectedAyah
        {
            get { return selectedAyah; }
            set
            {
                if (value == selectedAyah)
                    return;

                selectedAyah = value;

                base.OnPropertyChanged(() => SelectedAyah);
            }
        }

        #endregion Properties

        #region Public methods

        public override async Task Refresh()
        {
            await LoadCurrentPage();
            await base.Refresh();
        }

        public void ClearPages()
        {
            foreach (var page in Pages)
            {
                ClearPage(page);
            }
        }

        public bool TogglePageBookmark()
        {
            try
            {
                using (var adapter = new BookmarksDatabaseHandler())
                {
                    adapter.TogglePageBookmark(CurrentPageNumber);
                }
                CurrentPageBookmarked = BookmarksDatabaseHandler.IsPageBookmarked(CurrentPageNumber);
                return true;
            }
            catch (Exception e)
            {
                QuranApp.NativeProvider.Log("error creating bookmark");
                telemetry.TrackException(e, new Dictionary<string, string> { { "Scenario", "TogglePageBookmark" } });
                return false;
            }
        }

        public bool AddAyahBookmark(QuranAyah ayah)
        {
            try
            {
                using (var adapter = new BookmarksDatabaseHandler())
                {
                    if (ayah == null)
                        adapter.AddBookmarkIfNotExists(null, null, CurrentPageNumber);
                    else
                        adapter.AddBookmarkIfNotExists(ayah.Surah, ayah.Ayah, CurrentPageNumber);
                }
                CurrentPageBookmarked = BookmarksDatabaseHandler.IsPageBookmarked(CurrentPageNumber);
                return true;
            }
            catch (Exception e)
            {
                QuranApp.NativeProvider.Log("error creating bookmark");
                telemetry.TrackException(e, new Dictionary<string, string> { { "Scenario", "AddBookmark" } });
                return false;
            }
        }
        
        public async void CopyAyahToClipboard(QuranAyah ayah)
        {
            if (ayah == null)
                return;

            if (ayah.Translation != null)
            {
                QuranApp.NativeProvider.CopyToClipboard(ayah.Translation);
            }
            else if (ayah.Text != null)
            {
                QuranApp.NativeProvider.CopyToClipboard(ayah.Text);
            }
            else
            {
                await DownloadArabicSearchFile();
                if (await FileUtils.HaveArabicSearchFile())
                {
                    try
                    {
                        using (var dbArabic = new QuranDatabaseHandler<ArabicAyah>(FileUtils.QURAN_ARABIC_DATABASE))
                        {
                            var ayahSurah =
                                await new TaskFactory().StartNew(() => dbArabic.GetVerse(ayah.Surah, ayah.Ayah));
                            QuranApp.NativeProvider.CopyToClipboard(ayahSurah.Text);
                        }
                    }
                    catch (Exception e)
                    {
                        telemetry.TrackException(e, new Dictionary<string, string> { { "Scenario", "OpenArabicDatabase" } });
                    }
                }
            }
        }

        public async Task<string> GetAyahString(QuranAyah ayah)
        {
            if (ayah == null)
            {
                return null;
            }

            else if (ayah.Text != null)
            {
                return ayah.Text;
            }
            else
            {
                await DownloadArabicSearchFile();
                if (await FileUtils.HaveArabicSearchFile())
                {
                    try
                    {
                        using (var dbArabic = new QuranDatabaseHandler<ArabicAyah>(FileUtils.QURAN_ARABIC_DATABASE))
                        {
                            var ayahSurah = dbArabic.GetVerse(ayah.Surah, ayah.Ayah);
                            string ayahText = ayahSurah.Text;
                            return ayahText;
                        }
                    }
                    catch (Exception e)
                    {
                        telemetry.TrackException(e, new Dictionary<string, string> { { "Scenario", "OpenArabicDatabase" } });
                    }
                }
            }
            return null;
        }

        public async Task<bool> DownloadAyahPositionFile()
        {
            if (!await FileUtils.HaveAyaPositionFile())
            {
                string url = FileUtils.GetAyaPositionFileUrl();
                string destination = FileUtils.GetQuranDatabaseDirectory();
                destination = Path.Combine(destination, Path.GetFileName(url));
                // start the download
                return await this.ActiveDownload.DownloadSingleFile(url, destination, Resources.loading_data);
            }
            else
            {
                return true;
            }
        }

        public async Task<bool> DownloadArabicSearchFile()
        {
            if (!await FileUtils.HaveArabicSearchFile())
            {
                string url = FileUtils.GetArabicSearchUrl();
                string destination = FileUtils.GetQuranDatabaseDirectory();
                destination = Path.Combine(destination, Path.GetFileName(url));
                // start the download
                return await this.ActiveDownload.DownloadSingleFile(url, destination, Resources.loading_data);
            }
            else
            {
                return true;
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            ClearPages();
        }
        #endregion

        #region Private helper methods
        /// <summary>
        ///     Creates and adds ItemViewModel objects into the Items collection.
        /// </summary>
        private async Task CreatePages()
        {
            if (Pages.Count == 0)
            {
                for (int page = Constants.PAGES_LAST; page >= Constants.PAGES_FIRST; page--)
                {
                    var pageModel = new PageViewModel(page, this) { ShowTranslation = this.ShowTranslation };
                    await pageModel.Initialize();
                    Pages.Add(pageModel);
                }
            }
        }

        private void ClearPage(PageViewModel pageModel)
        {
            pageModel.ImageSource = null;
            pageModel.Translations.Clear();
        }

        private async Task LoadCurrentPage()
        {
            await LoadPage(CurrentPage);
            if (CurrentPageIndex - 1 > 0)
            {
                LoadPage(Pages[CurrentPageIndex - 1]);
            }

            if (CurrentPageIndex + 1 < Pages.Count)
            {
                LoadPage(Pages[CurrentPageIndex + 1]);
            }

            var pageNumber = GetPageNumberFromIndex(CurrentPageIndex);
            CurrentSurahName = QuranUtils.GetSurahNameFromPage(pageNumber, false);
            CurrentSurahNumber = QuranUtils.GetSurahNumberFromPage(pageNumber);
            var rub = QuranUtils.GetRub3FromPage(pageNumber);
            CurrentJuzName = string.Format("{0} {1}{2} {3} {4}", QuranUtils.GetJuzTitle(),
                                           QuranUtils.GetJuzFromPage(pageNumber),
                                           getJuzPart(rub), Resources.quran_rub3, rub);
            CurrentPageBookmarked = BookmarksDatabaseHandler.IsPageBookmarked(CurrentPageNumber);
            
            if (!IsLoading)
            {
                SettingsUtils.Set<int>(Constants.PREF_LAST_PAGE, pageNumber);
            }
        }

        public async Task<bool> HasTranslationFile()
        {
            return !string.IsNullOrEmpty(this.TranslationFile) &&
                await FileUtils.FileExists(FileUtils.DatabaseFolder, this.TranslationFile);
        }

        private async Task LoadPage(PageViewModel pageModel)
        {
            if (pageModel == null)
            {
                return;
            }

            // Set image
            if (pageModel.ImageSource == null)
            {
                pageModel.ImageSource = FileUtils.GetImageOnlineUri(FileUtils.GetPageFileName(pageModel.PageNumber));
            }

            // Set translation
            if (pageModel.Translations.Count == 0)
            {
                try
                {
                    if (!await HasTranslationFile())
                    {
                        return;
                    }

                    List<QuranAyah> verses = null;
                    using (var db = new QuranDatabaseHandler<QuranAyah>(this.TranslationFile))
                    {
                        verses = await new TaskFactory().StartNew(() => db.GetVerses(pageModel.PageNumber));
                    }

                    FlowDirection flowDirection = FlowDirection.LeftToRight;
                    Regex translationFilePattern = new Regex(@"quran\.([\w-]+)\..*");
                    var fileMatch = translationFilePattern.Match(this.TranslationFile);
                    if (fileMatch.Success)
                    {
                        try
                        {
                            var cultureInfo = new CultureInfo(fileMatch.Groups[1].Value);
                            if (cultureInfo.TextInfo.IsRightToLeft)
                            {
                                flowDirection = FlowDirection.RightToLeft;
                            }
                        }
                        catch (Exception e)
                        {
                            telemetry.TrackException(e, new Dictionary<string, string> { { "Scenario", "ParseTranslationFileForFlowDirection" } });
                        }
                    }

                    List <ArabicAyah> versesArabic = null;
                    if (this.ShowArabicInTranslation && await FileUtils.HaveArabicSearchFile())
                    {
                        try
                        {
                            using (var dbArabic = new QuranDatabaseHandler<ArabicAyah>(FileUtils.QURAN_ARABIC_DATABASE))
                            {
                                versesArabic = await new TaskFactory().StartNew(() => dbArabic.GetVerses(pageModel.PageNumber));
                            }
                        }
                        catch (Exception e)
                        {
                            telemetry.TrackException(e, new Dictionary<string, string> { { "Scenario", "OpenArabicDatabase" } });
                        }
                    }

                    int tempSurah = -1;
                    for (int i = 0; i < verses.Count; i++)
                    {
                        var verse = verses[i];
                        if (verse.Surah != tempSurah)
                        {
                            pageModel.Translations.Add(new VerseViewModel(this)
                            {
                                IsHeader = true,
                                Text = QuranUtils.GetSurahName(verse.Surah, true)
                            });

                            tempSurah = verse.Surah;
                        }

                        var verseViewModel = new VerseViewModel(this)
                        {
                            Surah = verse.Surah,
                            Ayah = verse.Ayah,
                            Text = verse.Text,
                            FlowDirection = flowDirection
                        };
                        if (versesArabic != null)
                        {
                            verseViewModel.ArabicText = versesArabic[i].Text;
                        }

                        pageModel.Translations.Add(verseViewModel);
                    }
                }
                catch (Exception e)
                {
                    // Try delete bad translation file if error is "no such table: verses"
                    try
                    {
                        if (e.Message.StartsWith("no such table:", StringComparison.OrdinalIgnoreCase))
                        {
                            await FileUtils.SafeFileDelete(Path.Combine(FileUtils.GetQuranDatabaseDirectory(),
                                                                   this.TranslationFile));
                        }
                    }
                    catch
                    {
                        // Do nothing
                    }
                    pageModel.Translations.Add(new VerseViewModel(this) { Text = "Error loading translation..." });
                    telemetry.TrackException(e, new Dictionary<string, string> { { "Scenario", "LoadingTranslation" } });
                }
            }
            return;
        }

        private int GetIndexFromPageNumber(int number)
        {
            var index = Constants.PAGES_LAST - number;
            if (index < 0 || index > Constants.PAGES_LAST - 1)
                return Constants.PAGES_LAST - 1;
            else
                return index;
        }

        private int GetPageNumberFromIndex(int index)
        {
            var page = Constants.PAGES_LAST - index;
            if (page < Constants.PAGES_FIRST || page > Constants.PAGES_LAST)
            {
                return 0;
            }
            else
            {
                return page;
            }
        }
        #endregion Private helper methods
    }
}
