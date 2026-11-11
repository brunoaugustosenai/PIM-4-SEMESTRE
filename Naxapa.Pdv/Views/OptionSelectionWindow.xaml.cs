using System.Windows;
using System.Windows.Controls;
using Naxapa.Pdv.Models;

namespace Naxapa.Pdv.Views;

public partial class OptionSelectionWindow : Window
{
    private readonly ProductDetailDto _product;
    private readonly Dictionary<long, List<(ProductOptionDto option, CheckBox check, TextBox quantity)>> _controls = [];
    public IReadOnlyList<CartOptionLine> SelectedOptions { get; private set; } = [];

    public OptionSelectionWindow(ProductDetailDto product)
    {
        InitializeComponent();
        _product = product;
        ProductName.Text = $"{product.Name} · {product.Price:C}";
        ProductDescription.Text = product.Description ?? product.Category;
        BuildGroups();
    }

    private void BuildGroups()
    {
        if (_product.OptionGroups.Count == 0)
        {
            GroupsPanel.Children.Add(new TextBlock { Text = "Este produto não possui adicionais ou variações.", Foreground = System.Windows.Media.Brushes.Gray });
            return;
        }

        foreach (var group in _product.OptionGroups)
        {
            var box = new GroupBox { Header = $"{group.Name} · mínimo {group.Minimum}, máximo {group.Maximum}", Margin = new Thickness(0, 0, 0, 12), Padding = new Thickness(10) };
            var stack = new StackPanel();
            var list = new List<(ProductOptionDto, CheckBox, TextBox)>();
            foreach (var option in group.Options.OrderBy(x => x.Order))
            {
                var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
                var qty = new TextBox { Text = "1", Width = 48, Margin = new Thickness(8, 0, 0, 0), ToolTip = "Quantidade" };
                DockPanel.SetDock(qty, Dock.Right);
                row.Children.Add(qty);
                var check = new CheckBox { Content = option.AdditionalValue > 0 ? $"{option.Name} (+{option.AdditionalValue:C})" : option.Name, VerticalAlignment = VerticalAlignment.Center };
                row.Children.Add(check);
                stack.Children.Add(row);
                list.Add((option, check, qty));
            }
            _controls[group.Id] = list;
            box.Content = stack;
            GroupsPanel.Children.Add(box);
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;
        var selected = new List<CartOptionLine>();
        foreach (var group in _product.OptionGroups)
        {
            var groupSelected = new List<CartOptionLine>();
            foreach (var (option, check, quantityBox) in _controls[group.Id])
            {
                if (check.IsChecked != true) continue;
                if (!int.TryParse(quantityBox.Text, out var quantity) || quantity < 1)
                {
                    ValidationText.Text = $"Informe uma quantidade válida para {option.Name}.";
                    return;
                }
                groupSelected.Add(new CartOptionLine { OptionId = option.Id, Name = option.Name, AdditionalValue = option.AdditionalValue, Quantity = quantity });
            }

            var totalQuantity = groupSelected.Sum(x => x.Quantity);
            if (totalQuantity < group.Minimum || totalQuantity > group.Maximum || (group.Required && totalQuantity == 0))
            {
                ValidationText.Text = $"O grupo '{group.Name}' exige de {group.Minimum} a {group.Maximum} seleção(ões).";
                return;
            }
            selected.AddRange(groupSelected);
        }

        SelectedOptions = selected;
        DialogResult = true;
    }
}
