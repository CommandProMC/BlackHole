using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace BlackHole
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        public MainWindow()
        {
            InitializeComponent();

            this.SourceInitialized += MainWindow_SourceInitialized;

            Uri resourceUri = new Uri("pack://application:,,,/Assets/BlackHole.gif");

            try
            {
                var streamInfo = Application.GetResourceStream(resourceUri);
                if (streamInfo != null)
                {
                    var path = Path.Combine(Path.GetTempPath(), "BlackHole.gif");

                    using (var fileStream = File.Create(path))
                    {
                        streamInfo.Stream.CopyTo(fileStream);
                    }

                    BackdropImage.Source = new Uri(path);
                    BackdropImage.Play();
                }
            }
            catch (Exception ex)
            {
                RootGrid.Background = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri("pack:///application:,,,/Assets/BlackHole.png")),
                    Stretch = Stretch.Uniform
                };
                LoadingIndicator.Visibility = Visibility.Hidden;
            }

            (this.Resources["FallInAnimation"] as Storyboard).Completed += (s, e) =>
            {
                FileIconContainer.Children.Remove(FileIcon);
                FileIconContainer.Children.Clear();
                FileIconContainer.Children.Add(FileIcon);
                FileIcon.Source = null;
            };

        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).EnsureHandle();

            int backdrop = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));

            int color = 0x000000;
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref color, sizeof(int));

            int darkmode = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkmode, sizeof(int));
        }

        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Handled = true;

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files != null && files.Length == 1)
                {
                    var cd = new ConfirmationWindow(Path.GetFileName(files[0]));
                    cd.Owner = this;
                    cd.ShowDialog();
                    if (cd.DialogResult != true) return;

                    FileIcon.Source = FileIconHelper.GetFileIcon(files[0]);
                    File.Delete(files[0]);
                }
                else if (files != null && files.Length > 1)
                {
                    var cd = new ConfirmationWindow($"these {files.Length} files");
                    cd.Owner = this;
                    cd.ShowDialog();
                    if (cd.DialogResult != true) return;

                    var parentGrid = new Grid();
                    foreach (var file in files)
                    {
                        var icon = new Image
                        {
                            Source = FileIconHelper.GetFileIcon(file),
                            Stretch = Stretch.Uniform,
                            RenderTransformOrigin = new Point(0.5, 0.5),
                            RenderTransform = new RotateTransform(Random.Shared.Next(0, 359))
                        };
                        parentGrid.Children.Add(icon);
                        File.Delete(file);
                    }
                    FileIconContainer.Children.Add(parentGrid);
                }

                (this.Resources["FallInAnimation"] as Storyboard).Begin();
            }
            else
            {
                e.Handled = false;
            }
        }

        private void BackdropImage_MediaEnded(object sender, RoutedEventArgs e)
        {
            (sender as MediaElement).Position = TimeSpan.FromMilliseconds(1);
            (sender as MediaElement).Play();
        }

        private void BackdropImage_BufferingEnded(object sender, RoutedEventArgs e)
        {
            LoadingIndicator.Visibility = Visibility.Hidden;
        }
    }
}