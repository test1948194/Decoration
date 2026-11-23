using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DecorationWpf
{
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string login = tbLogin.Text;
            string password = tbPassword.Password;

            using (DecorationEntities db = new DecorationEntities())
            {
                var user = db.Пользователи
                    .Include("Роли")
                    .FirstOrDefault(x => x.Login == login && x.Password == password);

                if (user != null)
                {
                    string fio = $"{user.Фамилия} {user.Имя} {user.Отчество}".Trim();
                    ProductWindow pw = new ProductWindow(fio, user.id_пользователя, user.id_роли);
                    pw.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoginForGuest_Click(object sender, MouseButtonEventArgs e)
        {
            ProductWindow pw = new ProductWindow("Гость", null, 0);
            pw.Show();
            this.Close();
        }
    }
}