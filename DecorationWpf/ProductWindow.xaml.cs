using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DecorationWpf
{
    public partial class ProductWindow : Window
    {
        private List<Товары> allProducts;
        private List<Товары> currentOrder = new List<Товары>();
        private int? currentUserId;
        private int roleId;

        private Товары selectedProduct = null;
        private Border lastSelectedBorder = null;

        public ProductWindow(string userFio, int? userId, int role)
        {
            InitializeComponent();
            txtUserFio.Text = userFio;
            currentUserId = userId;
            roleId = role;

            ApplyRolePermissions();
            InitFilters();
            LoadData();
        }

        private void ApplyRolePermissions()
        {
            if (roleId == 2)
            {
                btnAddProduct.Visibility = Visibility.Visible;
                btnEditProduct.Visibility = Visibility.Visible;
                btnDeleteProduct.Visibility = Visibility.Visible;
                btnOrders.Visibility = Visibility.Visible;
            }
            else if (roleId == 3)
            {
                btnOrders.Visibility = Visibility.Visible;
            }
        }

        private void InitFilters()
        {
            cbPrice.Items.Add(new { Id = 0, SortName = "По умолчанию" });
            cbPrice.Items.Add(new { Id = 1, SortName = "По возрастанию цены" });
            cbPrice.Items.Add(new { Id = 2, SortName = "По убыванию цены" });
            cbPrice.DisplayMemberPath = "SortName";
            cbPrice.SelectedValuePath = "Id";
            cbPrice.SelectedIndex = 0;

            cbDiscount.Items.Add(new { Id = -1, FilterName = "Все диапазоны" });
            cbDiscount.Items.Add(new { Id = 0, FilterName = "0–9,99%" });
            cbDiscount.Items.Add(new { Id = 10, FilterName = "10–14,99%" });
            cbDiscount.Items.Add(new { Id = 15, FilterName = "15% и более" });
            cbDiscount.DisplayMemberPath = "FilterName";
            cbDiscount.SelectedValuePath = "Id";
            cbDiscount.SelectedIndex = 0;
        }

        private void LoadData()
        {
            using (DecorationEntities db = new DecorationEntities())
            {
                allProducts = db.Товары
                    .Include("Производители")
                    .Include("Категории")
                    .Include("Ед_измер")
                    .ToList();
            }
            icProducts.ItemsSource = null;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (allProducts == null) return;

            IEnumerable<Товары> query = allProducts;

            string searchText = tbSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(p => p.Наименование.ToLower().Contains(searchText));
            }

            int discountFilter = (int)cbDiscount.SelectedValue;
            if (discountFilter == 0)
                query = query.Where(p => p.Действующая_скидка >= 0 && p.Действующая_скидка <= 9);
            else if (discountFilter == 10)
                query = query.Where(p => p.Действующая_скидка >= 10 && p.Действующая_скидка <= 14);
            else if (discountFilter == 15)
                query = query.Where(p => p.Действующая_скидка >= 15);

            int sortId = (int)cbPrice.SelectedValue;
            if (sortId == 1)
                query = query.OrderBy(p => p.Стоимость * (1 - (decimal)p.Действующая_скидка / 100m));
            else if (sortId == 2)
                query = query.OrderByDescending(p => p.Стоимость * (1 - (decimal)p.Действующая_скидка / 100m));

            var filteredList = query.ToList();
            icProducts.ItemsSource = filteredList;

            txtQuantity.Text = $"Показано {filteredList.Count} из {allProducts.Count}";
            txtNotFound.Visibility = filteredList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TbSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void CbPrice_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void CbDiscount_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private void AddToOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is Товары product)
            {
                currentOrder.Add(product);
                btnViewOrder.Visibility = Visibility.Visible;
                MessageBox.Show($"Товар '{product.Наименование}' добавлен в заказ!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnViewOrder_Click(object sender, RoutedEventArgs e)
        {
            OrderWindow orderWindow = new OrderWindow(currentOrder, txtUserFio.Text, currentUserId);
            orderWindow.ShowDialog();

            if (orderWindow.IsOrderCompleted)
            {
                currentOrder.Clear();
                btnViewOrder.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnOrders_Click(object sender, RoutedEventArgs e)
        {
            OrderManagementWindow ordersWindow = new OrderManagementWindow(txtUserFio.Text, currentUserId, roleId);
            ordersWindow.Show();
            this.Close();
        }

        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            ProductManagementWindow productWindow = new ProductManagementWindow(null);
            productWindow.Owner = this;
            productWindow.ShowDialog();
            LoadData();
        }

        private void BtnEditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProduct == null)
            {
                MessageBox.Show("Сначала выберите товар, кликнув по нему в списке!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ProductManagementWindow editForm = new ProductManagementWindow(selectedProduct);
            editForm.Owner = this;

            if (editForm.ShowDialog() == true)
            {
                LoadData();
                selectedProduct = null;
                lastSelectedBorder = null;
            }
        }

        private void BtnDeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProduct == null)
            {
                MessageBox.Show("Сначала выберите товар, кликнув по нему в списке!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Вы точно хотите удалить товар '{selectedProduct.Наименование}'?",
                "Удаление товара",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                using (DecorationEntities db = new DecorationEntities())
                {
                    var product = db.Товары.Find(selectedProduct.id_товара);
                    if (product != null)
                    {
                        if (db.Состав_заказа.Any(oi => oi.id_товара == product.id_товара))
                        {
                            MessageBox.Show("Нельзя удалить товар: он присутствует в заказе.", "Удаление запрещено", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        db.Товары.Remove(product);
                        db.SaveChanges();
                    }
                }

                selectedProduct = null;
                lastSelectedBorder = null;
                LoadData();
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            AuthWindow authWindow = new AuthWindow();
            authWindow.Show();
            this.Close();
        }

        private void Product_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                Border border = sender as Border;
                if (border != null && border.DataContext is Товары product)
                {
                    if (lastSelectedBorder != null)
                    {
                        lastSelectedBorder.BorderBrush = Brushes.Black;
                        lastSelectedBorder.BorderThickness = new Thickness(1);
                    }

                    border.BorderBrush = (Brush)FindResource("Accent");
                    border.BorderThickness = new Thickness(3);

                    lastSelectedBorder = border;
                    selectedProduct = product;
                }
            }
        }
    }
}