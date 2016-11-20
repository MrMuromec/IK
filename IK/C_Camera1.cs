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
using System.Collections.Concurrent;
//
using System.Threading.Tasks;

namespace IK
{
    public class C_Camera1
    {
        private Capture _capture; // Камера
        private AutoResetEvent _autoEvent; // Сигналка 
        private System.Threading.Timer _timer; // Таймер

        private ConcurrentQueue<Image<Gray, Byte>> _BS02 = new ConcurrentQueue<Image<Gray, byte>>(); // Очередь для отображения
        private ConcurrentQueue<IImage> _Images = new ConcurrentQueue<IImage>(); // Очередь для кадров

        public event MethodContainer event_BS;
        public delegate void MethodContainer();
        /// <summary>
        /// Запуск камеры
        /// </summary>
        public void _StartCamera(int _CameraSelectedIndex, int _fps)
        {
            _autoEvent = new AutoResetEvent(false);
            _capture = new Capture(_CameraSelectedIndex); // Выбор камеры
            _capture.Start();

            this.event_BS += this._ConversionEvent;
            this.event_BS += this._GetImagesEvent;

            _timer = new System.Threading.Timer(_TimerEvent, null, 0, (int)(1000 / _fps)); // Настройка таймера 
        }
        /// <summary>
        /// Стоп камеры
        /// </summary>
        public bool _StopCamera()
        {
            {
                _timer.Change(0, System.Threading.Timeout.Infinite);
                this.event_BS -= this._ConversionEvent;
                this.event_BS -= this._GetImagesEvent;

                _autoEvent.WaitOne();
                Thread.Sleep(1000);

                _capture.Stop();
                _capture.Dispose();

                _timer.Dispose();
            }
            return true;
        }

        /// <summary>
        /// Переработка картинок
        /// </summary>
        private void _ConversionEvent()
        {
            IImage _image;
            if (((ConcurrentQueue<IImage>)_Images).TryDequeue(out _image))
            {
                _AdditionAsync((IImage)_image.Clone());
                _image.Dispose();
            }
            else
            {
                Thread.Sleep(10);
            } 
        }
        /// <summary>
        /// Получение изображения с камеры
        /// </summary>
        private void _GetImagesEvent()
        {
            _Images.Enqueue(_capture.QueryFrame().Clone());
            _autoEvent.Set();
        }

        /// <summary>
        /// Асинхронная магия ч.1
        /// "Запихивание"
        /// </summary>
        private async void _AdditionAsync( IImage _image)
        {
            Image<Gray, byte> result = await _RemakeAsync(_image);

            _BS02.Enqueue(result.Clone());
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

                return _Magic.Clone();
            });
        }

        /// <summary>
        /// Событие таймера
        /// </summary>
        private void _TimerEvent(object obj)
        {           
            event_BS(); // Событие
        }
        
        /// <summary>
        /// Обработанное изображение с камеры
        /// </summary>
        public BitmapSource BitmapCameraSource
        {
            get
            {
                Image<Gray, Byte> _im;
                BitmapSource _bs;
                if (_BS02.TryDequeue(out _im))
                {
                    _bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        _im.Clone().Bitmap.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                    _im.Dispose();
                    return _bs;
                }
                else
                    return null;
            }
            set
            {
                ;
            }
        }
    }
}
