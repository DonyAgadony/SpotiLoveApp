namespace SpotiLove
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
        }

        protected override async void OnStart()
        {
            try
            {
                var userId = await SecureStorage.GetAsync("user_id");
                if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out Guid id))
                {
                    var name = await SecureStorage.GetAsync("user_name");
                    var email = await SecureStorage.GetAsync("user_email");

                    UserData.Current = new UserData
                    {
                        Id = id,
                        Name = name,
                        Email = email
                    };

                    System.Diagnostics.Debug.WriteLine($"✅ Restored user session: Id={id}, Name={name}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ No saved user session found");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ OnStart restore error: {ex.Message}");
            }
        }

        protected override async void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            System.Diagnostics.Debug.WriteLine($"🔗 Deep link received: {uri}");

            if (uri.Scheme == "spotilove" && uri.Host == "auth")
            {
                System.Diagnostics.Debug.WriteLine("✅ Valid Spotify callback detected");
                await SpotifyAuthHandler.HandleSpotifyCallback(uri.ToString());
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Unhandled deep link: {uri.Scheme}://{uri.Host}");
            }
        }
    }
}