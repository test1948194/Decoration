using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DecorationWpf
{
    public partial class OrderManagementWindow : Window
    {
        private List<OrderView> allOrders;
        private string userFio;
        private int? userId;
        private int roleId;

        public OrderManagementWindow(string fio, int? id, int role)
        {
            InitializeComponent();

            userFio = fio;
            userId = id;
            roleId = role;

            InitFilters();
            LoadData();
        }

        private void InitFilters()
        {
            cbSort.Items.Add(new { Id = 0, SortName = "По умолчанию" });
            cbSort.Items.Add(new { Id = 1, SortName = "По возрастанию" });
            cbSort.Items.Add(new { Id = 2, SortName = "По убыванию" });
            cbSort.DisplayMemberPath = "SortName";
            cbSort.SelectedValuePath = "Id";
            cbSort.SelectedIndex = 0;

            cbDiscount.Items.Add(new { Id = -1, FilterName = "Все диапазоны" });
            cbDiscount.Items.Add(new { Id = 0, FilterName = "0–10%" });
            cbDiscount.Items.Add(new { Id = 11, FilterName = "11–14%" });
            cbDiscount.Items.Add(new { Id = 15, FilterName = "15% и более" });
            cbDiscount.DisplayMemberPath = "FilterName";
            cbDiscount.SelectedValuePath = "Id";
            cbDiscount.SelectedIndex = 0;

            colStatus.ItemsSource = new List<string> { "Новый", "Завершен" };
        }

        private void LoadData()
        {
            using (DecorationEntities db = new DecorationEntities())
            {
                var orders = db.Заказы
                    .Include("Пользователи")
                    .Include("Пункты_выдачи")
                    .Include("Состав_заказа.Товары")
                    .ToList();

                allOrders = orders.Select(o => new OrderView
                {
                    Order = o,
                    Дата_доставки = o.Дата_доставки,          
                    TotalSum = o.Состав_заказа.Sum(s => s.Товары.Стоимость * (s.Количество > 0 ? s.Количество : 1)),
                    TotalDiscount = CalculateOrderDiscount(o),
                    ClientFio = o.Пользователи != null
        ? $"{o.Пользователи.Фамилия} {o.Пользователи.Имя} {o.Пользователи.Отчество}".Trim()
        : "Гость",
                    PickupAddress = $"{o.Пункты_выдачи.Индекс}, {o.Пункты_выдачи.Город}, {o.Пункты_выдачи.Улица}, {o.Пункты_выдачи.Номер_улицы}",
                    RowBackground = GetRowBackground(o)
                }).ToList();
            }
            ApplyFilters();
        }

        private decimal CalculateOrderDiscount(Заказы order)
        {
            if (!order.Состав_заказа.Any()) return 0;
            decimal full = 0, discounted = 0;
            foreach (var s in order.Состав_заказа)
            {
                int qty = s.Количество > 0 ? s.Количество : 1;
                full += s.Товары.Стоимость * qty;
                discounted += s.Товары.Стоимость * (1m - s.Товары.Действующая_скидка / 100m) * qty;
            }
            if (full == 0) return 0;
            return Math.Round((full - discounted) / full * 100m, 2);
        }

        private string GetRowBackground(Заказы order)
        {
            bool allInStock = order.Состав_заказа.All(s => s.Товары.Кол_во_на_складе > 3);
            bool anyOutOfStock = order.Состав_заказа.Any(s => s.Товары.Кол_во_на_складе == 0);

            if (allInStock) return "#20b2aa";
            if (anyOutOfStock) return "#ff8c00";
            return "#ffffff";
        }

        private void ApplyFilters()
        {
            if (allOrders == null) return;

            IEnumerable<OrderView> query = allOrders;

            int discountFilter = (int)cbDiscount.SelectedValue;
            if (discountFilter == 0)
                query = query.Where(o => o.TotalDiscount >= 0 && o.TotalDiscount <= 10);
            else if (discountFilter == 11)
                query = query.Where(o => o.TotalDiscount >= 11 && o.TotalDiscount <= 14);
            else if (discountFilter == 15)
                query = query.Where(o => o.TotalDiscount >= 15);

            int sortId = (int)cbSort.SelectedValue;
            if (sortId == 1)
                query = query.OrderBy(o => o.TotalSum);
            else if (sortId == 2)
                query = query.OrderByDescending(o => o.TotalSum);

            dgOrders.ItemsSource = query.ToList();
        }

        private void CbSort_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void CbDiscount_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private void DeliveryDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dgOrders.SelectedItem is OrderView orderView)
            {
                using (DecorationEntities db = new DecorationEntities())
                {
                    var order = db.Заказы.FirstOrDefault(o => o.id_заказа == orderView.Order.id_заказа);
                    if (order != null && DateTime.TryParse(orderView.Order.Дата_доставки.ToString(), out DateTime newDate))
                    {
                        order.Дата_доставки = newDate;
                        db.SaveChanges();
                    }
                }
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            AuthWindow authWindow = new AuthWindow();
            authWindow.Show();
            this.Close();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            ProductWindow productWindow = new ProductWindow(userFio, userId, roleId);
            productWindow.Show();
            this.Close();
        }
    }

    public class OrderView
    {
        public Заказы Order { get; set; }
        public int id_заказа => Order.id_заказа;
        public DateTime Дата_заказа => Order.Дата_заказа;
        public DateTime Дата_доставки { get; set; }
        public string Код_для_получения => Order.Код_для_получения.ToString();
        public decimal TotalSum { get; set; }
        public decimal TotalDiscount { get; set; }
        public string ClientFio { get; set; }
        public string PickupAddress { get; set; }
        public string RowBackground { get; set; }
        public string Status
        {
            get => Order.Статус_заказа;
            set
            {
                if (Order.Статус_заказа != value)
                {
                    Order.Статус_заказа = value;

                    using (DecorationEntities db = new DecorationEntities())
                    {
                        var order = db.Заказы.Find(Order.id_заказа);
                        if (order != null)
                        {
                            order.Статус_заказа = value;
                            db.SaveChanges();
                        }
                    }
                }
            }
        }
    }
}