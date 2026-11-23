using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DecorationWpf
{
    public partial class OrderWindow : Window
    {
        public bool IsOrderCompleted { get; private set; } = false;
        private List<OrderItem> orderItems;
        private int? userId;

        public OrderWindow(List<Товары> products, string fio, int? id)
        {
            InitializeComponent();
            txtClientFio.Text = fio;
            userId = id;

            orderItems = products.GroupBy(p => p.id_товара)
                                 .Select(g => new OrderItem { Product = g.First(), Quantity = g.Count() }).ToList();

            lvOrderItems.ItemsSource = orderItems;
            LoadPickupPoints();
            CalculateTotal();
        }

        private void LoadPickupPoints()
        {
            using (DecorationEntities db = new DecorationEntities())
            {
                var raw = db.Пункты_выдачи
                    .Select(p => new
                    {
                        Id = p.id_пункта_выдачи,
                        Индекс = p.Индекс,
                        Город = p.Город,
                        Улица = p.Улица,
                        Номер = p.Номер_улицы
                    })
                    .ToList();   

                var points = raw.Select(p => new
                {
                    Id = p.Id,
                    FullAddress = $"{p.Индекс}, г. {p.Город}, ул. {p.Улица}, д. {p.Номер}"
                }).ToList();    

                cbPickupPoints.ItemsSource = points;
                cbPickupPoints.DisplayMemberPath = "FullAddress";
                cbPickupPoints.SelectedValuePath = "Id";
                cbPickupPoints.SelectedIndex = 0;
            }
        }

        private void CalculateTotal()
        {
            decimal total = orderItems.Sum(item => item.Product.Стоимость * item.Quantity);
            txtTotalSum.Text = $"Итого: {total:N2} руб.";
        }

        private void Quantity_TextChanged(object sender, TextChangedEventArgs e) => CalculateTotal();

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is OrderItem item)
            {
                var result = MessageBox.Show($"Удалить {item.Product.Наименование} из заказа?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    orderItems.Remove(item);
                    lvOrderItems.ItemsSource = null;
                    lvOrderItems.ItemsSource = orderItems;
                    CalculateTotal();
                }
            }
        }

        private void CompleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (orderItems.Count == 0)
            {
                MessageBox.Show("Заказ пуст", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cbPickupPoints.SelectedValue == null)
            {
                MessageBox.Show("Выберите пункт выдачи.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (DecorationEntities db = new DecorationEntities())
                {
                    int newOrderId = (db.Заказы.Max(z => (int?)z.id_заказа) ?? 0) + 1;
                    int pickupId = (int)cbPickupPoints.SelectedValue;

                    Заказы newOrder = new Заказы
                    {
                        id_заказа = newOrderId,
                        Дата_заказа = DateTime.Now,
                        Дата_доставки = DateTime.Now.AddDays(3),
                        id_пункта_выдачи = pickupId,
                        id_пользователя = userId,
                        Код_для_получения = new Random().Next(1000, 9999),
                        Статус_заказа = "Новый"
                    };

                    db.Заказы.Add(newOrder);

                    int compositionId = (db.Состав_заказа.Max(c => (int?)c.id_состав_заказа) ?? 0) + 1;

                    foreach (var item in orderItems)
                    {
                        db.Состав_заказа.Add(new Состав_заказа
                        {
                            id_состав_заказа = compositionId++,
                            id_заказа = newOrderId,
                            id_товара = item.Product.id_товара,
                            Количество = item.Quantity   
                        });
                    }

                    db.SaveChanges();
                }

                IsOrderCompleted = true;
                MessageBox.Show("Заказ успешно оформлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                MessageBox.Show(
                    $"Не удалось сохранить заказ.\n{ex.InnerException?.InnerException?.Message ?? ex.Message}",
                    "Ошибка сохранения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }

    public class OrderItem
    {
        public Товары Product { get; set; }
        public int Quantity { get; set; }
        public string ProductName => Product.Наименование;
        public string ProductPrice => $"{Product.Стоимость:N2} руб.";
        public string TotalPrice => $"{Product.Стоимость * Quantity:N2} руб.";
    }
}