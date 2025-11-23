namespace SpotiLove
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            OnStart();
            MainPage = new AppShell();
        }
        protected override async void OnStart()
        {
            try
            {
                var userId = await SecureStorage.GetAsync("user_id");
                if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int id))
                {
                    var name = await SecureStorage.GetAsync("user_name");
                    var email = await SecureStorage.GetAsync("user_email");

                    UserData.Current = new UserData
                    {
                        Id = id,
                        Name = name,
                        Email = email
                    };

                    System.Diagnostics.Debug.WriteLine($"Restored user session: Id={id}, Name={name}");
                }
            }
            catch (Exception ex)
            {
                // SecureStorage access can fail in emulators — ignore or log
                System.Diagnostics.Debug.WriteLine($"OnStart restore error: {ex.Message}");
            }
        }

    protected override async void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            if (uri.Scheme == "spotilove" && uri.Host == "auth")
            {
                await SpotifyAuthHandler.HandleSpotifyCallback(uri.ToString());
            }
        }
    }
    }
   

