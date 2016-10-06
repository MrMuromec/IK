﻿using System;
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
        private AutoResetEvent _autoEvent; // Сигналка для таймера
        private System.Threading.Timer _timer; // Таймер
        private ConcurrentQueue<Image<Gray, Byte>> _BS02 = null; // Рисунок для отображения
        private ConcurrentQueue<IImage> _Images = null; // Очередь для кадров
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
            _capture.Start();

            _BS02 = new ConcurrentQueue<Image<Gray, byte>>(); // Создание очереди под кадры после обработки

            _Images = new ConcurrentQueue<IImage>(); // Создание очереди под кадры

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
                //_Images;
                //_BS02;
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
                if (((ConcurrentQueue<IImage>)_Images).TryDequeue(out _image))
                {
                    _AdditionAsync((IImage)_image.Clone());
                    _image.Dispose();
                }
                else
                {
                    Thread.Sleep(10);
                    _autoEvent.Set();
                }                  
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
            _Images.Enqueue(_capture.QueryFrame());
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
                if (_BS02.TryDequeue(out _im))
                {
                    return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        _im.Bitmap.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
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
