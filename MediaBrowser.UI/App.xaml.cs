﻿using MediaBrowser.ApiInteraction;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.UI;
using MediaBrowser.IsoMounter;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Weather;
using MediaBrowser.UI.Controller;
using MediaBrowser.UI.Controls;
using MediaBrowser.UI.Pages;
using MediaBrowser.UI.Uninstall;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MediaBrowser.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : BaseApplication, IApplication
    {
        /// <summary>
        /// Gets or sets the clock timer.
        /// </summary>
        /// <value>The clock timer.</value>
        private Timer ClockTimer { get; set; }
        /// <summary>
        /// Gets or sets the server configuration timer.
        /// </summary>
        /// <value>The server configuration timer.</value>
        private Timer ServerConfigurationTimer { get; set; }

        /// <summary>
        /// Gets the name of the product.
        /// </summary>
        /// <value>The name of the product.</value>
        protected override string ProductName
        {
            get { return Globals.ProductName; }
        }

        /// <summary>
        /// Gets the name of the publisher.
        /// </summary>
        /// <value>The name of the publisher.</value>
        protected override string PublisherName
        {
            get { return Globals.PublisherName; }
        }

        /// <summary>
        /// Gets the name of the suite.
        /// </summary>
        /// <value>The name of the suite.</value>
        protected override string SuiteName
        {
            get { return Globals.SuiteName; }
        }

        /// <summary>
        /// Gets the name of the uninstaller file.
        /// </summary>
        /// <value>The name of the uninstaller file.</value>
        protected override string UninstallerFileName
        {
            get { return "MediaBrowser.UI.Uninstall.exe"; }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static App Instance
        {
            get
            {
                return Current as App;
            }
        }

        /// <summary>
        /// Gets the API client.
        /// </summary>
        /// <value>The API client.</value>
        public ApiClient ApiClient
        {
            get { return UIKernel.Instance.ApiClient; }
        }

        /// <summary>
        /// Gets the application window.
        /// </summary>
        /// <value>The application window.</value>
        public MainWindow ApplicationWindow { get; private set; }

        /// <summary>
        /// Gets the hidden window.
        /// </summary>
        /// <value>The hidden window.</value>
        public HiddenWindow HiddenWindow { get; private set; }

        /// <summary>
        /// The _current user
        /// </summary>
        private UserDto _currentUser;
        /// <summary>
        /// Gets or sets the current user.
        /// </summary>
        /// <value>The current user.</value>
        public UserDto CurrentUser
        {
            get
            {
                return _currentUser;
            }
            set
            {
                _currentUser = value;

                if (UIKernel.Instance.ApiClient != null)
                {
                    if (value == null)
                    {
                        UIKernel.Instance.ApiClient.CurrentUserId = null;
                    }
                    else
                    {
                        UIKernel.Instance.ApiClient.CurrentUserId = value.Id;
                    }
                }

                OnPropertyChanged("CurrentUser");
            }
        }

        /// <summary>
        /// The _server configuration
        /// </summary>
        private ServerConfiguration _serverConfiguration;
        /// <summary>
        /// Gets or sets the server configuration.
        /// </summary>
        /// <value>The server configuration.</value>
        public ServerConfiguration ServerConfiguration
        {
            get
            {
                return _serverConfiguration;
            }
            set
            {
                _serverConfiguration = value;
                OnPropertyChanged("ServerConfiguration");
            }
        }

        /// <summary>
        /// The _current time
        /// </summary>
        private DateTime _currentTime = DateTime.Now;
        /// <summary>
        /// Gets the current time.
        /// </summary>
        /// <value>The current time.</value>
        public DateTime CurrentTime
        {
            get
            {
                return _currentTime;
            }
            private set
            {
                _currentTime = value;
                OnPropertyChanged("CurrentTime");
            }
        }

        /// <summary>
        /// The _current weather
        /// </summary>
        private WeatherInfo _currentWeather;
        /// <summary>
        /// Gets the current weather.
        /// </summary>
        /// <value>The current weather.</value>
        public WeatherInfo CurrentWeather
        {
            get
            {
                return _currentWeather;
            }
            private set
            {
                _currentWeather = value;
                OnPropertyChanged("CurrentWeather");
            }
        }

        /// <summary>
        /// The _current theme
        /// </summary>
        private BaseTheme _currentTheme;
        /// <summary>
        /// Gets the current theme.
        /// </summary>
        /// <value>The current theme.</value>
        public BaseTheme CurrentTheme
        {
            get
            {
                return _currentTheme;
            }
            private set
            {
                _currentTheme = value;
                OnPropertyChanged("CurrentTheme");
            }
        }

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            RunApplication<App>("MediaBrowserUI");
        }

        /// <summary>
        /// Instantiates the kernel.
        /// </summary>
        /// <returns>IKernel.</returns>
        protected override IKernel InstantiateKernel()
        {
            return new UIKernel(new PismoIsoManager(LogManager.GetLogger("PismoIsoManager")));
        }

        /// <summary>
        /// Instantiates the main window.
        /// </summary>
        /// <returns>Window.</returns>
        protected override Window InstantiateMainWindow()
        {
            HiddenWindow = new HiddenWindow { };

            return HiddenWindow;
        }

        /// <summary>
        /// Shows the application window.
        /// </summary>
        private void ShowApplicationWindow()
        {
            var win = new MainWindow { };

            var config = UIKernel.Instance.Configuration;

            // Restore window position/size
            if (config.WindowState.HasValue)
            {
                // Set window state
                win.WindowState = config.WindowState.Value;

                // Set position if not maximized
                if (config.WindowState.Value != WindowState.Maximized)
                {
                    double left = 0;
                    double top = 0;

                    // Set left
                    if (config.WindowLeft.HasValue)
                    {
                        win.WindowStartupLocation = WindowStartupLocation.Manual;
                        win.Left = left = Math.Max(config.WindowLeft.Value, 0);
                    }

                    // Set top
                    if (config.WindowTop.HasValue)
                    {
                        win.WindowStartupLocation = WindowStartupLocation.Manual;
                        win.Top = top = Math.Max(config.WindowTop.Value, 0);
                    }

                    // Set width
                    if (config.WindowWidth.HasValue)
                    {
                        win.Width = Math.Min(config.WindowWidth.Value, SystemParameters.VirtualScreenWidth - left);
                    }

                    // Set height
                    if (config.WindowHeight.HasValue)
                    {
                        win.Height = Math.Min(config.WindowHeight.Value, SystemParameters.VirtualScreenHeight - top);
                    }
                }
            }

            win.LocationChanged += ApplicationWindow_LocationChanged;
            win.StateChanged += ApplicationWindow_LocationChanged;
            win.SizeChanged += ApplicationWindow_LocationChanged;

            ApplicationWindow = win;

            ApplicationWindow.Show();

            ApplicationWindow.Owner = HiddenWindow;

            SyncHiddenWindowLocation();
        }

        /// <summary>
        /// Handles the LocationChanged event of the ApplicationWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void ApplicationWindow_LocationChanged(object sender, EventArgs e)
        {
            SyncHiddenWindowLocation();
        }

        /// <summary>
        /// Syncs the hidden window location.
        /// </summary>
        public void SyncHiddenWindowLocation()
        {
            HiddenWindow.Width = ApplicationWindow.Width;
            HiddenWindow.Height = ApplicationWindow.Height;
            HiddenWindow.Top = ApplicationWindow.Top;
            HiddenWindow.Left = ApplicationWindow.Left;
            HiddenWindow.WindowState = ApplicationWindow.WindowState;

            ApplicationWindow.Activate();
        }

        /// <summary>
        /// Loads the kernel.
        /// </summary>
        protected override async void LoadKernel()
        {
            // Without this the app will shutdown after the splash screen closes
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Kernel = InstantiateKernel();

            try
            {
                var now = DateTime.UtcNow;

                await Kernel.Init();

                Logger.Info("Kernel.Init completed in {0} seconds.", (DateTime.UtcNow - now).TotalSeconds);

                ShutdownMode = System.Windows.ShutdownMode.OnLastWindowClose;

                await OnKernelLoaded();

                InstantiateMainWindow().Show();

                ShowApplicationWindow();

                await ApplicationWindow.LoadInitialUI().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error launching application", ex);

                MessageBox.Show("There was an error launching Media Browser: " + ex.Message);

                // Shutdown the app with an error code
                Shutdown(1);
            }
        }

        /// <summary>
        /// Called when [kernel loaded].
        /// </summary>
        /// <returns>Task.</returns>
        protected override async Task OnKernelLoaded()
        {
            await base.OnKernelLoaded().ConfigureAwait(false);

            PropertyChanged += AppPropertyChanged;

            // Update every 10 seconds
            ClockTimer = new Timer(ClockTimerCallback, null, 0, 10000);

            // Update every 30 minutes
            ServerConfigurationTimer = new Timer(ServerConfigurationTimerCallback, null, 0, 1800000);

            CurrentTheme = UIKernel.Instance.Themes.First();

            foreach (var resource in CurrentTheme.GetGlobalResources())
            {
                Resources.MergedDictionaries.Add(resource);
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Application.Exit" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.Windows.ExitEventArgs" /> that contains the event data.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            var win = ApplicationWindow;

            if (win != null)
            {
                // Save window position
                var config = UIKernel.Instance.Configuration;
                config.WindowState = win.WindowState;
                config.WindowTop = win.Top;
                config.WindowLeft = win.Left;
                config.WindowWidth = win.Width;
                config.WindowHeight = win.Height;
                UIKernel.Instance.SaveConfiguration();
            }

            base.OnExit(e);
        }

        /// <summary>
        /// Apps the property changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs" /> instance containing the event data.</param>
        async void AppPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("ServerConfiguration"))
            {
                if (string.IsNullOrEmpty(ServerConfiguration.WeatherLocation))
                {
                    CurrentWeather = null;
                }
                else
                {
                    try
                    {
                        CurrentWeather = await ApiClient.GetWeatherInfoAsync(ServerConfiguration.WeatherLocation);
                    }
                    catch (HttpException ex)
                    {
                        Logger.ErrorException("Error downloading weather information", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Clocks the timer callback.
        /// </summary>
        /// <param name="stateInfo">The state info.</param>
        private void ClockTimerCallback(object stateInfo)
        {
            CurrentTime = DateTime.Now;
        }

        /// <summary>
        /// Servers the configuration timer callback.
        /// </summary>
        /// <param name="stateInfo">The state info.</param>
        private async void ServerConfigurationTimerCallback(object stateInfo)
        {
            try
            {
                ServerConfiguration = await ApiClient.GetServerConfigurationAsync();
            }
            catch (HttpException ex)
            {
                Logger.ErrorException("Error refreshing server configuration", ex);
            }
        }

        /// <summary>
        /// Logouts the user.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task LogoutUser()
        {
            CurrentUser = null;

            await Dispatcher.InvokeAsync(() => Navigate(CurrentTheme.GetLoginPage()));
        }

        /// <summary>
        /// Navigates the specified page.
        /// </summary>
        /// <param name="page">The page.</param>
        public void Navigate(Page page)
        {
            _remoteImageCache = new FileSystemRepository(UIKernel.Instance.ApplicationPaths.RemoteImageCachePath);

            ApplicationWindow.Navigate(page);
        }

        /// <summary>
        /// Navigates to settings page.
        /// </summary>
        public void NavigateToSettingsPage()
        {
            Navigate(new SettingsPage());
        }

        /// <summary>
        /// Navigates to internal player page.
        /// </summary>
        public void NavigateToInternalPlayerPage()
        {
            Navigate(CurrentTheme.GetInternalPlayerPage());
        }

        /// <summary>
        /// Navigates to image viewer.
        /// </summary>
        /// <param name="imageUrl">The image URL.</param>
        /// <param name="caption">The caption.</param>
        public void OpenImageViewer(Uri imageUrl, string caption)
        {
            var tuple = new Tuple<Uri, string>(imageUrl, caption);

            OpenImageViewer(new[] { tuple });
        }

        /// <summary>
        /// Navigates to image viewer.
        /// </summary>
        /// <param name="images">The images.</param>
        public void OpenImageViewer(IEnumerable<Tuple<Uri, string>> images)
        {
            new ImageViewerWindow(images).ShowModal(ApplicationWindow);
        }

        /// <summary>
        /// Navigates to item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void NavigateToItem(BaseItemDto item)
        {
            if (item.IsRoot.HasValue && item.IsRoot.Value)
            {
                NavigateToHomePage();
            }
            else if (item.IsFolder)
            {
                Navigate(CurrentTheme.GetListPage(item));
            }
            else
            {
                Navigate(CurrentTheme.GetDetailPage(item));
            }
        }

        /// <summary>
        /// Displays the weather.
        /// </summary>
        public void DisplayWeather()
        {
            CurrentTheme.DisplayWeather();
        }

        /// <summary>
        /// Navigates to home page.
        /// </summary>
        public void NavigateToHomePage()
        {
            Navigate(CurrentTheme.GetHomePage());
        }

        /// <summary>
        /// Shows a notification message that will disappear on it's own
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="caption">The caption.</param>
        /// <param name="icon">The icon.</param>
        public void ShowNotificationMessage(string text, string caption = null, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            ApplicationWindow.ShowModalMessage(text, caption: caption, icon: icon);
        }

        /// <summary>
        /// Shows a notification message that will disappear on it's own
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="caption">The caption.</param>
        /// <param name="icon">The icon.</param>
        public void ShowNotificationMessage(UIElement text, string caption = null, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            ApplicationWindow.ShowModalMessage(text, caption: caption, icon: icon);
        }

        /// <summary>
        /// Shows a modal message box and asynchronously returns a MessageBoxResult
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="caption">The caption.</param>
        /// <param name="button">The button.</param>
        /// <param name="icon">The icon.</param>
        /// <returns>MessageBoxResult.</returns>
        public MessageBoxResult ShowModalMessage(string text, string caption = null, MessageBoxButton button = MessageBoxButton.OK, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            return ApplicationWindow.ShowModalMessage(text, caption: caption, button: button, icon: icon);
        }

        /// <summary>
        /// Shows a modal message box and asynchronously returns a MessageBoxResult
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="caption">The caption.</param>
        /// <param name="button">The button.</param>
        /// <param name="icon">The icon.</param>
        /// <returns>MessageBoxResult.</returns>
        public MessageBoxResult ShowModalMessage(UIElement text, string caption = null, MessageBoxButton button = MessageBoxButton.OK, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            return ApplicationWindow.ShowModalMessage(text, caption: caption, button: button, icon: icon);
        }

        /// <summary>
        /// Shows the error message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="caption">The caption.</param>
        public void ShowErrorMessage(string message, string caption = null)
        {
            caption = caption ?? "Error";
            ShowModalMessage(message, caption: caption, button: MessageBoxButton.OK, icon: MessageBoxIcon.Error);
        }

        /// <summary>
        /// Shows the default error message.
        /// </summary>
        public void ShowDefaultErrorMessage()
        {
            ShowErrorMessage("There was an error processing the request", "Error");
        }

        /// <summary>
        /// The _remote image cache
        /// </summary>
        private FileSystemRepository _remoteImageCache;

        /// <summary>
        /// Gets the remote image async.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>Task{Image}.</returns>
        public async Task<Image> GetRemoteImageAsync(string url)
        {
            var bitmap = await GetRemoteBitmapAsync(url);

            return new Image { Source = bitmap };
        }

        /// <summary>
        /// Gets the remote image async.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>Task{BitmapImage}.</returns>
        /// <exception cref="System.ArgumentNullException">url</exception>
        public Task<BitmapImage> GetRemoteBitmapAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }

            Logger.Info("Image url: " + url);

            return Task.Run(async () =>
            {
                var cachePath = _remoteImageCache.GetResourcePath(url.GetMD5().ToString());

                await _remoteImageCache.WaitForLockAsync(cachePath).ConfigureAwait(false);

                var releaseLock = true;
                try
                {
                    using (var stream = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, true))
                    {
                        return await GetRemoteBitmapAsync(stream).ConfigureAwait(false);
                    }
                }
                catch (FileNotFoundException)
                {
                    // Doesn't exist. No biggie
                    releaseLock = false;
                }
                finally
                {
                    if (releaseLock)
                    {
                        _remoteImageCache.ReleaseLock(cachePath);
                    }
                }

                try
                {
                    using (var httpStream = await UIKernel.Instance.ApiClient.GetImageStreamAsync(url + "&x=1"))
                    {
                        return await GetRemoteBitmapAsync(httpStream, cachePath);
                    }
                }
                finally
                {
                    _remoteImageCache.ReleaseLock(cachePath);
                }
            });
        }

        /// <summary>
        /// Gets the image async.
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="cachePath">The cache path.</param>
        /// <returns>Task{BitmapImage}.</returns>
        private async Task<BitmapImage> GetRemoteBitmapAsync(Stream sourceStream, string cachePath = null)
        {
            byte[] bytes;

            using (var ms = new MemoryStream())
            {
                await sourceStream.CopyToAsync(ms).ConfigureAwait(false);

                bytes = ms.ToArray();
            }

            if (!string.IsNullOrEmpty(cachePath))
            {
                using (var fileStream = new FileStream(cachePath, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, true))
                {
                    await fileStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }
            }

            using (Stream stream = new MemoryStream(bytes))
            {
                var bitmapImage = new BitmapImage
                {
                    CreateOptions = BitmapCreateOptions.DelayCreation
                };

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }
    }
}
