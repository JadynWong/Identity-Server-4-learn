using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using IdentityModel.Client;

namespace WpfClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private static string _accessToken;
        private static DiscoveryDocumentResponse _discovery;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void RequestAccessToken_ButtonClick(object sender, RoutedEventArgs e)
        {
            var userName = UserNameInput.Text;
            var password = PasswordInput.Password;

            var client = new HttpClient();
            var discovery = await client.GetDiscoveryDocumentAsync("https://localhost:5001");

            if (discovery.IsError)
            {
                MessageBox.Show(discovery.Error);
                return;
            }

            // request access token
            var tokenResponse = await client.RequestPasswordTokenAsync(new PasswordTokenRequest()
            {
                Address = discovery.TokenEndpoint,
                ClientId = "wpf client",
                ClientSecret = "wpf",
                Scope = "api1 openid profile address phone email",

                UserName = userName,
                Password = password
            });

            if (tokenResponse.IsError)
            {
                MessageBox.Show(tokenResponse.Error);
                return;
            }

            _discovery = discovery;
            _accessToken = tokenResponse.AccessToken;
            AccessTokenTextBlock.Text = tokenResponse.Json.ToString();
        }

        private async void RequestApi1Resource_ButtonClick(object sender, RoutedEventArgs e)
        {
            //call api
            var apiClient = new HttpClient();
            apiClient.SetBearerToken(_accessToken);

            var response = await apiClient.GetAsync("https://localhost:6001/identity");
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show(response.StatusCode.ToString());
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                ApiResponseTextBlock.Text = content;
            }
        }

        private async void RequestIdentityResource_ButtonClick(object sender, RoutedEventArgs e)
        {
            //call identity Resource form Identity Server
            var apiClient = new HttpClient();
            apiClient.SetBearerToken(_accessToken);

            var response = await apiClient.GetAsync(_discovery.UserInfoEndpoint);
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show(response.StatusCode.ToString());
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                IdentityResponseTextBlock.Text = content;
            }
        }
    }
}
