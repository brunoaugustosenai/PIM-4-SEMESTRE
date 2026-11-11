using System.Windows;
using Naxapa.Pdv.Services;

namespace Naxapa.Pdv.Views;

public partial class LoginWindow : Window
{
    private readonly AppSettings _settings = AppSettings.Load();
    public LoginWindow()
    {
        InitializeComponent();
        ApiUrlBox.Text = _settings.ApiBaseUrl;
        EstablishmentBox.Text = _settings.EstabelecimentoId?.ToString() ?? string.Empty;
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        LoginButton.IsEnabled = false;
        StatusText.Text = "Conectando...";
        try
        {
            long? establishmentId = long.TryParse(EstablishmentBox.Text, out var id) ? id : null;
            var api = new NaxapaApiClient(ApiUrlBox.Text);
            var session = await api.LoginAsync(LoginBox.Text, PasswordBox.Password, establishmentId);
            _settings.ApiBaseUrl = ApiUrlBox.Text.Trim();
            _settings.EstabelecimentoId = session.EstabelecimentoId;
            _settings.Save();
            var main = new MainWindow(api);
            main.Show();
            Close();
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }
}
