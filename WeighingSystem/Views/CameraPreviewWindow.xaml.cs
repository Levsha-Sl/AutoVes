using System.ComponentModel;
using System.Windows;
using WeighingSystem.Models;

namespace WeighingSystem.Views
{
    public partial class CameraPreviewWindow : Window
    {
        private Size _initialSize;

        public CameraPreviewWindow(CameraModel camera)
        {
            InitializeComponent();
            _initialSize = camera.Permission;
            DataContext = camera;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateCameraResolution();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCameraResolution();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DataContext is CameraModel camera)
            {
                camera.Permission = _initialSize;
                camera.IsVideoInRootMenu = true;
            }
        }

        private void UpdateCameraResolution()
        {
            if (DataContext is CameraModel camera)
            {
                camera.Permission = new Size(ActualWidth, ActualHeight);
            }
        }
    }
}
