using System;
using System.Linq;
using System.Windows;

namespace DecorationWpf
{
    public partial class ProductManagementWindow : Window
    {
        private Товары editingProduct;

        public ProductManagementWindow(Товары product)
        {
            InitializeComponent();
            editingProduct = product;
            LoadComboBoxes();

            if (product != null)
            {
                FillForm();
            }
        }

        private void LoadComboBoxes()
        {
            using (DecorationEntities db = new DecorationEntities())
            {
                cbManufacturer.ItemsSource = db.Производители.ToList();
                cbSupplier.ItemsSource = db.Поставщики.ToList();
                cbCategory.ItemsSource = db.Категории.ToList();
                cbUnit.ItemsSource = db.Ед_измер.ToList();
            }
        }

        private void FillForm()
        {
            tbArticle.Text = editingProduct.Артикул;
            tbName.Text = editingProduct.Наименование;
            cbUnit.SelectedValue = editingProduct.id_ед_изм;
            tbPrice.Text = editingProduct.Стоимость.ToString();
            tbMaxDiscount.Text = editingProduct.Размер_максимально_возможной_скидки.ToString();
            cbManufacturer.SelectedValue = editingProduct.id_производителя;
            cbSupplier.SelectedValue = editingProduct.id_поставщика;
            cbCategory.SelectedValue = editingProduct.id_категории;
            tbDiscount.Text = editingProduct.Действующая_скидка.ToString();
            tbStock.Text = editingProduct.Кол_во_на_складе.ToString();
            tbDescription.Text = editingProduct.Описание;
            tbImagePath.Text = editingProduct.Изображение;
        }

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                string imagesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                System.IO.Directory.CreateDirectory(imagesDir);

                string fileName = System.IO.Path.GetFileName(dialog.FileName);
                string destPath = System.IO.Path.Combine(imagesDir, fileName);
                System.IO.File.Copy(dialog.FileName, destPath, true);

                tbImagePath.Text = fileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось скопировать изображение: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbArticle.Text) || string.IsNullOrWhiteSpace(tbName.Text))
            {
                MessageBox.Show("Заполните артикул и наименование.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(tbPrice.Text, out int price) || price < 0)
            {
                MessageBox.Show("Стоимость должна быть целым неотрицательным числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(tbMaxDiscount.Text, out int maxDiscount) || maxDiscount < 0 || maxDiscount > 100)
            {
                MessageBox.Show("Максимальная скидка должна быть от 0 до 100.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(tbDiscount.Text, out int discount) || discount < 0 || discount > maxDiscount)
            {
                MessageBox.Show("Действующая скидка должна быть от 0 до максимальной.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(tbStock.Text, out int stock) || stock < 0)
            {
                MessageBox.Show("Количество на складе должно быть целым неотрицательным числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (cbUnit.SelectedValue == null || cbCategory.SelectedValue == null ||
                cbManufacturer.SelectedValue == null || cbSupplier.SelectedValue == null)
            {
                MessageBox.Show("Выберите все значения из выпадающих списков.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (DecorationEntities db = new DecorationEntities())
            {
                if (editingProduct == null)
                {
                    int newId = (db.Товары.Max(t => (int?)t.id_товара) ?? 0) + 1;
                    db.Товары.Add(new Товары
                    {
                        id_товара = newId,
                        Артикул = tbArticle.Text.Trim(),
                        Наименование = tbName.Text.Trim(),
                        id_ед_изм = (int)cbUnit.SelectedValue,
                        Стоимость = price,
                        Размер_максимально_возможной_скидки = maxDiscount,
                        id_производителя = (int)cbManufacturer.SelectedValue,
                        id_поставщика = (int)cbSupplier.SelectedValue,
                        id_категории = (int)cbCategory.SelectedValue,
                        Действующая_скидка = discount,
                        Кол_во_на_складе = stock,
                        Описание = tbDescription.Text.Trim(),
                        Изображение = tbImagePath.Text.Trim()
                    });
                }
                else
                {
                    var existing = db.Товары.Find(editingProduct.id_товара);
                    if (existing == null)
                    {
                        MessageBox.Show("Товар не найден в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    existing.Артикул = tbArticle.Text.Trim();
                    existing.Наименование = tbName.Text.Trim();
                    existing.id_ед_изм = (int)cbUnit.SelectedValue;
                    existing.Стоимость = price;
                    existing.Размер_максимально_возможной_скидки = maxDiscount;
                    existing.id_производителя = (int)cbManufacturer.SelectedValue;
                    existing.id_поставщика = (int)cbSupplier.SelectedValue;
                    existing.id_категории = (int)cbCategory.SelectedValue;
                    existing.Действующая_скидка = discount;
                    existing.Кол_во_на_складе = stock;
                    existing.Описание = tbDescription.Text.Trim();
                    existing.Изображение = tbImagePath.Text.Trim();
                }

                db.SaveChanges();
            }

            MessageBox.Show("Товар сохранён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}