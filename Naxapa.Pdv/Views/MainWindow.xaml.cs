using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Naxapa.Pdv.Models;
using Naxapa.Pdv.Services;

namespace Naxapa.Pdv.Views;

public partial class MainWindow : Window
{
    private readonly NaxapaApiClient _api;
    private readonly ObservableCollection<ProductDto> _products = [];
    private readonly ObservableCollection<CartLine> _cart = [];
    private CancellationTokenSource? _searchCts;

    public MainWindow(NaxapaApiClient api)
    {
        InitializeComponent();
        _api = api;
        ProductsGrid.ItemsSource = _products;
        CartGrid.ItemsSource = _cart;
        SessionText.Text = $"{api.Session?.UserName} · {api.Session?.Estabelecimento}";
        Loaded += async (_, _) => await LoadProductsAsync();
    }

    private async Task LoadProductsAsync(string? q = null)
    {
        try
        {
            GlobalStatus.Text = "Carregando produtos...";
            var data = await _api.ProductsAsync(q);
            _products.Clear(); foreach (var p in data.Items) _products.Add(p);
            GlobalStatus.Text = $"{data.Total} produto(s)";
        }
        catch (Exception ex) { GlobalStatus.Text = ex.Message; }
    }

    private async void RefreshProducts_Click(object sender, RoutedEventArgs e) => await LoadProductsAsync(ProductSearch.Text);
    private async void ProductSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchCts?.Cancel(); _searchCts = new CancellationTokenSource();
        try { await Task.Delay(350, _searchCts.Token); await LoadProductsAsync(ProductSearch.Text); } catch (OperationCanceledException) { }
    }

    private async void ProductsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not ProductDto p) return;
        try
        {
            var detail = await _api.ProductAsync(p.Id);
            var options = new List<CartOptionLine>();
            if (detail.OptionGroups.Count > 0)
            {
                var dialog = new OptionSelectionWindow(detail) { Owner = this };
                if (dialog.ShowDialog() != true) return;
                options.AddRange(dialog.SelectedOptions);
            }

            var signature = string.Join("|", options.OrderBy(x => x.OptionId).Select(x => $"{x.OptionId}:{x.Quantity}"));
            var existing = _cart.FirstOrDefault(x => x.Product.Id == p.Id &&
                string.Join("|", x.Options.OrderBy(o => o.OptionId).Select(o => $"{o.OptionId}:{o.Quantity}")) == signature);
            if (existing is null)
                _cart.Add(new CartLine { Product = p, Quantity = 1, Options = options });
            else
                existing.Quantity++;
            CartGrid.Items.Refresh();
            UpdateTotal();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Não foi possível carregar o produto"); }
    }

    private void UpdateTotal() => CartTotal.Text = $"Total: {_cart.Sum(x => x.Total):C}";

    private async void SubmitOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_cart.Count == 0) { MessageBox.Show("Adicione pelo menos um produto."); return; }
        try
        {
            var serviceType = (ServiceTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "BALCAO";
            long? reference = long.TryParse(ReferenceIdBox.Text, out var id) ? id : null;
            long? tableId = serviceType == "MESA" ? reference : null;
            long? tabId = serviceType == "COMANDA" ? reference : null;
            var order = await _api.CreateOrderAsync(serviceType, tableId: tableId, tabId: tabId);
            foreach (var line in _cart)
            {
                var item = await _api.AddItemAsync(order.Id, line.Product.Id, line.Quantity);
                foreach (var option in line.Options)
                    await _api.AddOptionAsync(item.PedidoItemId, option.OptionId, option.Quantity);
            }
            await _api.ChangeStatusAsync(order.Id, "PENDENTE", "Pedido enviado pelo NAXAPA PDV");
            _cart.Clear(); UpdateTotal();
            GlobalStatus.Text = $"Pedido #{order.Number} enviado.";
            await LoadOrdersAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Não foi possível enviar o pedido"); }
    }

    private async Task LoadOrdersAsync()
    {
        var status = (StatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
        var data = await _api.OrdersAsync(status);
        OrdersGrid.ItemsSource = data.Items;
    }

    private async void RefreshOrders_Click(object sender, RoutedEventArgs e) { try { await LoadOrdersAsync(); } catch (Exception ex) { GlobalStatus.Text = ex.Message; } }
    private async void AdvanceStatus_Click(object sender, RoutedEventArgs e)
    {
        if (OrdersGrid.SelectedItem is not OrderDto order) return;
        var next = order.Status switch { "NOVO" => "PENDENTE", "PENDENTE" => "EM_PREPARACAO", "EM_PREPARACAO" => "PRONTO", "PRONTO" => "FINALIZADO", _ => null };
        if (next is null) { MessageBox.Show("Este pedido não possui avanço de status disponível."); return; }
        try { await _api.ChangeStatusAsync(order.Id, next); await LoadOrdersAsync(); } catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void Payment_Click(object sender, RoutedEventArgs e)
    {
        if (OrdersGrid.SelectedItem is not OrderDto order) return;
        try
        {
            var methods = await _api.PaymentMethodsAsync();
            if (methods.Count == 0) { MessageBox.Show("Nenhuma forma de pagamento cadastrada."); return; }
            var dialog = new PaymentWindow(order, methods, _api) { Owner = this };
            if (dialog.ShowDialog() == true) await LoadOrdersAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || e.Source != Tabs) return;
        try
        {
            if (Tabs.SelectedIndex == 1) await LoadOrdersAsync();
            else if (Tabs.SelectedIndex == 2) await RefreshTablesTabsAsync();
            else if (Tabs.SelectedIndex == 3) await RefreshCashAsync();
        }
        catch (Exception ex) { GlobalStatus.Text = ex.Message; }
    }

    private async Task RefreshTablesTabsAsync()
    {
        TablesGrid.ItemsSource = await _api.TablesAsync();
        TabsGrid.ItemsSource = await _api.TabsAsync();
    }

    private async void RefreshTablesTabs_Click(object sender, RoutedEventArgs e)
    {
        try { await RefreshTablesTabsAsync(); } catch (Exception ex) { GlobalStatus.Text = ex.Message; }
    }

    private async void OpenTab_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TabCodeBox.Text)) { MessageBox.Show("Informe o código da comanda."); return; }
        try { await _api.OpenTabAsync(TabCodeBox.Text.Trim()); TabCodeBox.Clear(); await RefreshTablesTabsAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (TabsGrid.SelectedItem is not TabDto tab) return;
        try { await _api.CloseTabAsync(tab.Id); await RefreshTablesTabsAsync(); } catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async Task RefreshCashAsync()
    {
        CashRegisterBox.ItemsSource = await _api.CashRegistersAsync();
        CashGrid.ItemsSource = await _api.CashAsync();
    }

    private async void RefreshCash_Click(object sender, RoutedEventArgs e) { try { await RefreshCashAsync(); } catch (Exception ex) { GlobalStatus.Text = ex.Message; } }

    private bool TryCashValue(out decimal value)
    {
        if (!decimal.TryParse(CashValueBox.Text, out value) || value < 0) { MessageBox.Show("Informe um valor válido."); return false; }
        return true;
    }

    private async void OpenCash_Click(object sender, RoutedEventArgs e)
    {
        if (CashRegisterBox.SelectedItem is not CashRegisterDto cash) { MessageBox.Show("Selecione um caixa."); return; }
        if (!TryCashValue(out var value)) return;
        try { await _api.OpenCashAsync(cash.Id, value); CashValueBox.Clear(); await RefreshCashAsync(); } catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async Task MoveCashAsync(string type)
    {
        if (CashGrid.SelectedItem is not CashSummaryDto session || session.Status != "ABERTO") { MessageBox.Show("Selecione uma sessão aberta."); return; }
        if (!TryCashValue(out var value) || value <= 0) return;
        await _api.MoveCashAsync(session.CaixaSessaoId, type, value, $"{type} pelo NAXAPA PDV");
        CashValueBox.Clear(); await RefreshCashAsync();
    }
    private async void SupplyCash_Click(object sender, RoutedEventArgs e) { try { await MoveCashAsync("SUPRIMENTO"); } catch (Exception ex) { MessageBox.Show(ex.Message); } }
    private async void WithdrawCash_Click(object sender, RoutedEventArgs e) { try { await MoveCashAsync("SANGRIA"); } catch (Exception ex) { MessageBox.Show(ex.Message); } }
    private async void CloseCash_Click(object sender, RoutedEventArgs e)
    {
        if (CashGrid.SelectedItem is not CashSummaryDto session || session.Status != "ABERTO") { MessageBox.Show("Selecione uma sessão aberta."); return; }
        if (!TryCashValue(out var value)) return;
        try { await _api.CloseCashAsync(session.CaixaSessaoId, value, "Fechamento pelo NAXAPA PDV"); CashValueBox.Clear(); await RefreshCashAsync(); } catch (Exception ex) { MessageBox.Show(ex.Message); }
    }
}
