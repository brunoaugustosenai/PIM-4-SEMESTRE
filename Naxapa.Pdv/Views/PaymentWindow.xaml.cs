using System.Globalization;
using System.Windows;
using Naxapa.Pdv.Models;
using Naxapa.Pdv.Services;

namespace Naxapa.Pdv.Views;

public partial class PaymentWindow : Window
{
    private readonly OrderDto _order;
    private readonly NaxapaApiClient _api;
    public PaymentWindow(OrderDto order, IReadOnlyList<PaymentMethodDto> methods, NaxapaApiClient api)
    {
        InitializeComponent(); _order=order; _api=api;
        OrderText.Text=$"Pedido #{order.Number} — {order.Total:C}";
        MethodBox.ItemsSource=methods; MethodBox.SelectedIndex=0; ValueBox.Text=order.Total.ToString("0.00",CultureInfo.CurrentCulture);
    }
    private async void Confirm_Click(object sender,RoutedEventArgs e)
    {
        if(MethodBox.SelectedItem is not PaymentMethodDto method)return;
        if(!decimal.TryParse(ValueBox.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var value)||value<=0){StatusText.Text="Valor inválido.";return;}
        decimal? received=decimal.TryParse(ReceivedBox.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var r)?r:null;
        long? session=long.TryParse(CashSessionBox.Text,out var s)?s:null;
        try{await _api.PayAsync(_order.Id,method.Id,value,received,session);DialogResult=true;Close();}
        catch(Exception ex){StatusText.Text=ex.Message;}
    }
}
