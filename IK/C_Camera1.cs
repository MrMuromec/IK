using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//EMGU
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Cvb;
//
using System.Threading;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows;
//
using System.Threading.Tasks;

namespace IK
{
    public class C_Camera1
    {
        private Capture _capture; // Камера
        private AutoResetEvent _autoEvent; // Сигналка для таймера
        private System.Threading.Timer _timer; // Таймер
        private Queue<Image<Gray, Byte>> _BS02 = null; // Рисунок для отображения
        private Queue<IImage> _Images = null; // Очередь для кадров
        private Thread _ConversionThread = null; // Поток под преобразования

        public event MethodContainer event_BS;
        public delegate void MethodContainer();
        /// <summary>
        /// Запуск камеры
        /// </summary>
        public void _StartCamera(int _CameraSelectedIndex, int _fps)
        {
            _autoEvent = new AutoResetEvent(false);
            _capture = new Capture(_CameraSelectedIndex); // Выбор камеры
            //_capture = new Capture(@"J:\1.avi"); // файл 
            _capture.Start();

            _BS02 = new Queue<Image<Gray, byte>>(); // Создание очереди под кадры после обработки

            _Images = new System.Collections.Generic.Queue<IImage>(); // Создание очереди под кадры

            _ConversionThread = new Thread(this._Conversion);
            _ConversionThread.Start(_Images);

            _timer = new System.Threading.Timer(_TimerEvent, null, 0, (int)(1000 / _fps)); // Настройка таймера 
        }

        /// <summary>
        /// Стоп камеры
        /// </summary>
        public bool _StopCamera()
        {
            if (_timer != null)
            {
                _autoEvent.WaitOne();
                _timer.Dispose();
                _capture.Stop();
                _capture.Dispose();
                _ConversionThread.Abort();
                _timer = null;
                _Images.Clear();
                _BS02.Clear();
            }
            return true;
        }
        
        /// <summary>
        /// Переработка картинок
        /// </summary>
        void _Conversion(object _Images)
        {
            IImage _image;

            while (true)
            {
                Thread.Sleep(0);

                try
                {
                    _image = ((Queue<IImage>)_Images).Dequeue();

                }
                catch (System.InvalidOperationException)
                {
                    _image = null;
                    Thread.Sleep(10);
                    _autoEvent.Set();
                }
                if (_image != null)
                    _AdditionAsync(_image);
            }
        }

        /// <summary>
        /// Асинхронная магия ч.1
        /// "Запихивание"
        /// </summary>
        private async void _AdditionAsync( IImage _image)
        {
            // Queue<Image<Gray, Byte>> _BS02,
            Image<Gray, byte> result = await _RemakeAsync(_image);

            _BS02.Enqueue(result.Clone());

            //result.Dispose();
        }

        /// <summary>
        /// Асинхронная магия ч.2
        /// "Преобразование"
        /// </summary>
        private async Task<Image<Gray, Byte>> _RemakeAsync(IImage _image)
        {
            return await Task.Run(() =>
            {
                IImage[] _channel = _image.Split(); // Вроде на RGB

                Image<Gray, Byte> _Magic0 = new Image<Gray, byte>(_channel[0].Bitmap);
                Image<Gray, Byte> _Magic = _Magic0.Canny(40, 40);
                
                //_Magic0.Dispose();
                //_image.Dispose();

                return _Magic.Clone();
            });
        }

        /// <summary>
        /// Получение изображения с камеры по событию
        /// </summary>
        private void _TimerEvent(object obj)
        {
            IImage II = _capture.QueryFrame();
            _Images.Enqueue(II);
            event_BS(); // Событие
        }

        /// <summary>
        /// Обработанное изображение с камеры
        /// </summary>
        public BitmapSource BitmapCameraSource
        {
            get
            {
                BitmapSource _bs;
                try
                {
                    _bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        _BS02.Dequeue().Bitmap.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
                catch (System.InvalidOperationException)
                {
                    _bs = null;
                }
                return _bs;
            }
            set
            {
                ;
            }
        }
    }
}
