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

        private ConcurrentDictionary<string, ConcurrentBag<Bitmap>> objectDict;
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
            objectDict = new ConcurrentDictionary<string, ConcurrentBag<Bitmap>>();
            ObjectDict_from_Db();
        }

        public void ObjectDict_from_Db() {

            DetectedClass.Clear();
            objectDict.Clear();
            Images.Clear();


            using (var db = new ImageDbContext())
            {
                foreach (var item in db.DetectedObjects)
                {
                    if(!objectDict.ContainsKey(item.Label)) {
                        DetectedClass.Add(item.Label);
                        objectDict[item.Label] = new ConcurrentBag<Bitmap>();
                    }
                    MemoryStream ms = new MemoryStream();
                    System.Drawing.Bitmap img = new System.Drawing.Bitmap(byteArrayToImage(item.Details.Image));
                    objectDict[item.Label].Add(toAvalonia(img));
                }
            }
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

        public void ShowImages() {
            //Console.WriteLine(SelectedClass);
            if(SelectedClass != null && objectDict.ContainsKey(SelectedClass)) {
                Images.Clear();
                foreach(var bitmap in objectDict[SelectedClass]) {
                    Images.Add(bitmap);
                }
            }
        }

        public void Cencel() {
            cts.Cancel();
            cts = new CancellationTokenSource();
            Enable = true;
        }

        public void Clear() {
            using (var db = new ImageDbContext())
            {
                foreach (var item in db.DetectedObjects)
                {
                    db.DetectedObjects.Remove(item);
                }

                foreach (var item in db.DetectedObjectDetails)
                {
                    db.DetectedObjectDetails.Remove(item);
                }
                db.SaveChanges();
            }
            ObjectDict_from_Db();
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
                            objectDict[item.Label] = new ConcurrentBag<Bitmap>();
                        }
                        var x1 = (int) item.BBox[0];
                        var y1 = (int) item.BBox[1];
                        var x2 = (int) item.BBox[2];
                        var y2 = (int) item.BBox[3];
                        var img = cropBitmap(results.Key, x1, y1, x2, y2);
                        objectDict[item.Label].Add(toAvalonia(img));
                        //Console.WriteLine($"label: {item.Label} bbox: {item.BBox[0]}, {item.Bbox[1]}");
                        using(var db = new ImageDbContext())  {
                            var obj = new DetectedObject { 
                                X1 = x1, 
                                Y1 = y1, 
                                X2 = x2, 
                                Y2 = y2,
                                Label = item.Label,
                                Details = new ObjectDetails { Image = imageToByteArray(img)}
                            };
                            if(!db.ImageInDb(obj)) {
                                db.Add(obj);
                                db.SaveChanges();
                            }
                        }
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

        public byte[] imageToByteArray(System.Drawing.Bitmap image)
        {
            MemoryStream ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            return  ms.ToArray();
        }
        public System.Drawing.Image byteArrayToImage(byte[] bytesArr)
        {
            using (MemoryStream memstr = new MemoryStream(bytesArr))
            {
                System.Drawing.Image img = System.Drawing.Image.FromStream(memstr);
                return img;
            }
        }
        private System.Drawing.Bitmap cropBitmap(string imageName, int x1, int y1, int x2, int y2)
        {
            using var fileStream = new FileStream(imageName, FileMode.Open, FileAccess.Read) {Position = 0};
            var b = new System.Drawing.Bitmap(fileStream);
            var r = new System.Drawing.Rectangle(x1, y1, x2 - x1, y2 - y1);
            System.Drawing.Bitmap nb = new System.Drawing.Bitmap(r.Width, r.Height);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(nb))
            {
                g.DrawImage(b, -r.X, -r.Y);
                return nb;
            }
        }

        private Bitmap toAvalonia(System.Drawing.Bitmap img) {
            using (MemoryStream memory = new MemoryStream())
            {
                img.Save(memory, System.Drawing.Imaging.ImageFormat.Jpeg);
                memory.Position = 0;

                return new Avalonia.Media.Imaging.Bitmap(memory);
            }
        }
    }
}
