using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AjaxLoader
{
    class AjaxImage : System.Windows.Controls.Image
    {
        // Private member properties.
        private Bitmap       _bitmap;
        private BitmapSource _bitmapSource;

        // Public member methods.
        public delegate void FrameUpdatedEventHandler();

        /// <summary>
        /// Delete local bitmap resource.
        /// </summary>
        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// Overrides the OnInitialized method.
        /// </summary>
        /// <param name="eventArguments"></param>
        protected override void OnInitialized(EventArgs eventArguments)
        {
            base.OnInitialized(eventArguments);
            this.Loaded   += new RoutedEventHandler(AnimatedGIFControl_Loaded);
            this.Unloaded += new RoutedEventHandler(AnimatedGIFControl_Unloaded);           
        }

        /// <summary>
        /// Loads the embedded image for the Image.Source.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArguments"></param>
        void AnimatedGIFControl_Loaded(object sender, RoutedEventArgs eventArguments)
        {
            // Get GIF image from Resources
            if (Properties.Resources.ajax_loader_white_on_black != null)
            {
                // Set Bitmap width and height.
                _bitmap     = Properties.Resources.ajax_loader_white_on_black;
                this.Width  = _bitmap.Width;
                this.Height = _bitmap.Height;

                // Set Bitmap source.
                _bitmapSource = GetBitmapSource();
                this.Source   = _bitmapSource;

                // Start animating the ajax image.
                ImageAnimator.Animate(_bitmap, OnFrameChanged);
            }
        }

        /// <summary>
        /// Closes the FileStream to unlock the GIF file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArguments"></param>
        private void AnimatedGIFControl_Unloaded(object sender, RoutedEventArgs eventArguments)
        {
            ImageAnimator.StopAnimate(_bitmap, OnFrameChanged);
        }

        private void FrameUpdatedCallback()
        {
            ImageAnimator.UpdateFrames();

            if (_bitmapSource != null)
            {
                _bitmapSource.Freeze();
            }

            // Convert the bitmap to BitmapSource that can be display in WPF Visual Tree.
            _bitmapSource = GetBitmapSource();
            this.Source   = _bitmapSource;
            
            InvalidateVisual();
        }

        private BitmapSource GetBitmapSource()
        {
            IntPtr handle = IntPtr.Zero;                

            try
            {
                handle        = _bitmap.GetHbitmap();
                _bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                if (handle != IntPtr.Zero)
                {
                    DeleteObject(handle);
                }
            }

            return _bitmapSource;
        }

        /// <summary>
        /// Event handler for the frame changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArguments"></param>
        private void OnFrameChanged(object sender, EventArgs eventArguments)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new FrameUpdatedEventHandler(FrameUpdatedCallback));
        }

    }
}
