using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//EMGU
//using Emgu.CV;
//using Emgu.CV.CvEnum;
//using Emgu.CV.Structure;
//using Emgu.CV.Cvb;
//
using System.Threading;
using System.Windows.Threading;
//using System.Drawing;
//DiresctShow
using DirectShowLib;

namespace IK
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        C_Camera1 _Camera = new C_Camera1(); // Класс для работы с камерой

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Событие загрузки
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            B_Stop.IsEnabled = false;
            B_Run.IsEnabled = true;
            // Формирования списка камер
            DsDevice[] _SystemCamereas = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            for (int i = 0; i < _SystemCamereas.Length; i++)
                CoB_Camera.Items.Add(_SystemCamereas[i].Name);
            if (CoB_Camera.Items.Count > 0)
                CoB_Camera.SelectedIndex = 0;
            else
            {
                CoB_Camera.IsEnabled = false;
                B_Run.IsEnabled = false;
            }
            // Список фпс
            for (int i = 40; i > 0; i-=2)
                CoB_fps.Items.Add(i);
            CoB_fps.SelectedIndex = 8;
        }
        /// <summary>
        /// Запуск камеры
        /// </summary>
        void _StartCamera ()
        {
            B_Stop.IsEnabled = true;
            B_Run.IsEnabled = false;
            CoB_Camera.IsEnabled = CoB_fps.IsEnabled = false;
            // Настройка самой камеры
            _Camera._StartCamera(CoB_Camera.SelectedIndex, int.Parse(CoB_fps.SelectedItem.ToString()));
            // Подписка на событие срабатывания таймера камеры
            _Camera.event_BS += this.update;
        }

        /// <summary>
        /// Стоп камеры
        /// </summary>
        void _StopCamera()
        {
            B_Stop.IsEnabled = false;
            B_Run.IsEnabled = true;
            CoB_Camera.IsEnabled = CoB_fps.IsEnabled = true;
            // Отписка на событие срабатывания таймера камеры
            if (_Camera._StopCamera())
                _Camera.event_BS -= this.update;
        }
        /// <summary>
        /// СТОП камеры (кнопка)
        /// </summary>
        private void B_Stop_Click(object sender, RoutedEventArgs e)
        {
            _StopCamera();
        }
        /// <summary>
        /// ПУСК камеры (кнопка)
        /// </summary>
        private void B_Run_Click(object sender, RoutedEventArgs e)
        {
            _StartCamera();
        }
        /// <summary>
        /// собите закрытие окна
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            _StopCamera();
        }

        /// <summary>
        /// Обработчик события создания нового кадра
        /// </summary>
        private void update()
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
            {
                BitmapSource _bs = _Camera.BitmapCameraSource;
                if (_bs != null)
                    I_Camera.Source = _bs;
            });
        }
    }
}
