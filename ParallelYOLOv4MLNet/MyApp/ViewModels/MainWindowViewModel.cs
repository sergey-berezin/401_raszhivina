using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Model;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using System.Threading;


namespace MyApp.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {

        private ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> objectDict = new ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>();
        private string _imageFolder = "/Users/dasharazzhivina/Desktop/401_raszhivina/ParallelYOLOv4MLNet/ParallelYOLOv4MLNet/Images";
        private CancellationTokenSource cts = new CancellationTokenSource();
        
        private CancellationToken ct;

        private bool enable = true;
        public bool Enable
        {
            get
            {
                return enable;
            }
            set
            {
                enable = value;
                OnPropertyChanged("Enable");
            }
        }

        public string ImageFolder
        {
            get
            {
                return _imageFolder;
            }
            set
            {
                _imageFolder = value;
                OnPropertyChanged("ImageFolder");
            }
        }

        private string _selectedClass = "";
        public string SelectedClass
        {
            get
            {
                return _selectedClass;
            }
            set
            {
                _selectedClass = value;
                OnPropertyChanged("SelectedClass");
                ShowImages();
            }
        }

        private ObservableCollection<string> _detectedClass = new ObservableCollection<string>();
        public ObservableCollection<string> DetectedClass
        {
            get { return _detectedClass; }
            set
            {
                _detectedClass = value;
                OnPropertyChanged("DetectedClass");
            }
        }

        private ObservableCollection<Bitmap> _images = new ObservableCollection<Bitmap>();
        public ObservableCollection<Bitmap> Images
        {
            get { return _images; }
            set
            {
                _images = value;
                OnPropertyChanged("Images");
            }
        }

        public MainWindowViewModel() {
            ct = cts.Token;
        }

        public async void Open()
        {
            var dialog = new OpenFolderDialog();
            string result = await dialog.ShowAsync(new Window());
            if (result != null)
            {
                ImageFolder = result;
            }
        }

        private Task<Bitmap> GetBitmap(string imageName)
        {
            return Task.Run(() =>
            {
                using var fileStream = new FileStream(imageName, FileMode.Open, FileAccess.Read) {Position = 0};
                var bitmap = new Bitmap(fileStream);
                return bitmap;
            });
        }

        public async void ShowImages() {
            //Console.WriteLine(SelectedClass);
            if(SelectedClass != null && objectDict.ContainsKey(SelectedClass)) {
                Images.Clear();
                foreach(var imageName in objectDict[SelectedClass].Keys) {
                    var bitmap = await GetBitmap(imageName);
                    Images.Add(bitmap);
                }
            }
        }

        public void Cencel() {
            cts.Cancel();
        }

        public async void Detection()
        {
            Enable = false;
            string imageFolder = ImageFolder;
            var mainYoloV4 = new MainYoloV4(imageFolder);
            DetectedClass.Clear();
            objectDict.Clear();
            Images.Clear();

            var bufferBlock = new BufferBlock<KeyValuePair<string, IReadOnlyList<YoloV4Result>>>();
            
            
            var receive = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < mainYoloV4.Count; i++)
                {
                    var results = bufferBlock.Receive();
                    
                    foreach(var item in results.Value) {
                        if(!objectDict.ContainsKey(item.Label)) {
                            DetectedClass.Add(item.Label);
                            objectDict[item.Label] = new ConcurrentDictionary<string, bool>();
                        }
                        objectDict[item.Label].TryAdd(results.Key, true);
                        //Console.WriteLine($"label: {item.Label} path: {results.Key}");
                    }
                }
            }, TaskCreationOptions.LongRunning);

            var f = Task.Factory.StartNew(() =>
            {
                mainYoloV4.GetResult(bufferBlock, ct);
            }, TaskCreationOptions.LongRunning);

            await f;
            await receive; 
            
            Enable = true;
        }
    }
}
