using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace GreedyVisualizer
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<ImageSlice> UnoptimizedList { get; set; } = new ObservableCollection<ImageSlice>();
        private ObservableCollection<ImageSlice> OptimizedList { get; set; } = new ObservableCollection<ImageSlice>();
        
        // bar untuk sorting
        private const int SliceCount = 10; 
        
        // delay untuk visualisasi perbandingan
        private const int DelayMs = 50; 
        
        private CancellationTokenSource? _cancellationTokenSource;
        private int _unoptimizedOpsCount = 0;
        private int _optimizedOpsCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            UnoptimizedCanvas.ItemsSource = UnoptimizedList;
            OptimizedCanvas.ItemsSource = OptimizedList;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartSimulation();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            StartSimulation();
        }

        private async void StartSimulation()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            if (!PrepareImages()) return;

            // Reset Counter Text
            _unoptimizedOpsCount = 0;
            _optimizedOpsCount = 0;
            UnoptimizedCounterText.Text = $"Total Operasi: 0";
            OptimizedCounterText.Text = $"Total Operasi: 0";

            try
            {
                var task1 = RunUnoptimizedGreedy(UnoptimizedList, token);
                var task2 = RunOptimizedGreedy(OptimizedList, 0, OptimizedList.Count - 1, token);

                await Task.WhenAll(task1, task2);
            }
            catch (OperationCanceledException) { }
        }

        private bool PrepareImages()
        {
            string relativePath = @"picture\image.png";
            string fullPath = Path.GetFullPath(relativePath);

            if (!File.Exists(fullPath))
            {
                MessageBox.Show($"Gambar tidak ditemukan!\n{fullPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            BitmapImage src = new BitmapImage();
            src.BeginInit();
            src.UriSource = new Uri(fullPath, UriKind.Absolute);
            src.CacheOption = BitmapCacheOption.OnLoad; 
            src.EndInit();

            UnoptimizedList.Clear();
            OptimizedList.Clear();

            double sliceWidth = 400.0 / SliceCount;
            int pixelSliceWidth = src.PixelWidth / SliceCount;

            var random = new Random();
            var indices = Enumerable.Range(0, SliceCount).OrderBy(x => random.Next()).ToList();

            for (int i = 0; i < SliceCount; i++)
            {
                int shuffledIndex = indices[i];
                var rect = new Int32Rect(shuffledIndex * pixelSliceWidth, 0, pixelSliceWidth, src.PixelHeight);
                var cropped = new CroppedBitmap(src, rect);

                UnoptimizedList.Add(new ImageSlice { Image = cropped, OriginalIndex = shuffledIndex, Width = sliceWidth, XPosition = i * sliceWidth });
                OptimizedList.Add(new ImageSlice { Image = cropped, OriginalIndex = shuffledIndex, Width = sliceWidth, XPosition = i * sliceWidth });
            }

            return true;
        }

        // unoptimized greedy (Iterasi Linear berulang O(n^2))
        private async Task RunUnoptimizedGreedy(ObservableCollection<ImageSlice> list, CancellationToken token)
        {
            for (int i = 0; i < list.Count - 1; i++)
            {
                int minIndex = i;
                for (int j = i + 1; j < list.Count; j++)
                {
                    token.ThrowIfCancellationRequested();
                    
                    // Increment operasi dan Update UI
                    _unoptimizedOpsCount++;
                    UnoptimizedCounterText.Text = $"Total Operasi: {_unoptimizedOpsCount}";

                    await Task.Delay(DelayMs, token); 
                    
                    if (list[j].OriginalIndex < list[minIndex].OriginalIndex)
                    {
                        minIndex = j;
                    }
                }

                if (minIndex != i)
                {
                    Swap(list, i, minIndex);
                }
            }
        }

        // optimized reedy (pre sorting dengan QuickSort O(n log n))
        private async Task RunOptimizedGreedy(ObservableCollection<ImageSlice> list, int left, int right, CancellationToken token)
        {
            if (left < right)
            {
                token.ThrowIfCancellationRequested();
                int pivotIndex = await Partition(list, left, right, token);
                await RunOptimizedGreedy(list, left, pivotIndex - 1, token);
                await RunOptimizedGreedy(list, pivotIndex + 1, right, token);
            }
        }

        private async Task<int> Partition(ObservableCollection<ImageSlice> list, int left, int right, CancellationToken token)
        {
            int pivot = list[right].OriginalIndex;
            int i = left - 1;

            for (int j = left; j < right; j++)
            {
                token.ThrowIfCancellationRequested();
                
                // Increment operasi dan Update UI
                _optimizedOpsCount++;
                OptimizedCounterText.Text = $"Total Operasi: {_optimizedOpsCount}";

                await Task.Delay(DelayMs, token);

                if (list[j].OriginalIndex < pivot)
                {
                    i++;
                    Swap(list, i, j);
                }
            }
            Swap(list, i + 1, right);
            return i + 1;
        }

        private void Swap(ObservableCollection<ImageSlice> list, int indexA, int indexB)
        {
            var tempIndex = list[indexA].OriginalIndex;
            var tempImg = list[indexA].Image;

            list[indexA].OriginalIndex = list[indexB].OriginalIndex;
            list[indexA].Image = list[indexB].Image;

            list[indexB].OriginalIndex = tempIndex;
            list[indexB].Image = tempImg;
        }
    }

    public class ImageSlice : INotifyPropertyChanged
    {
        private BitmapSource? image;
        private double xPosition;

        public int OriginalIndex { get; set; }
        public double Width { get; set; }

        public BitmapSource? Image
        {
            get => image;
            set { image = value; OnPropertyChanged(nameof(Image)); }
        }

        public double XPosition
        {
            get => xPosition;
            set { xPosition = value; OnPropertyChanged(nameof(XPosition)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}