using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Model;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using MyApp;
using System.IO;
using Avalonia.Controls;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;

namespace Server.Controllers
{
    public class ImageObject
    {
        public string label;
        public Bitmap img;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class DetectionController : ControllerBase
    {
        private ImageDbContext dB;

        private CancellationTokenSource cts = new CancellationTokenSource();
        
        private CancellationToken ct;
        

        public DetectionController(ImageDbContext db)
        {
            this.dB = db;
            ct = cts.Token;
        }

        [Route("start")]
        public async Task<ConcurrentBag<KeyValuePair<string, IReadOnlyList<YoloV4Result>>>> Get(string imageFolder) { 
            var bufferBlock = new BufferBlock<KeyValuePair<string, IReadOnlyList<YoloV4Result>>>();
            var mainYoloV4 = new MainYoloV4(imageFolder);
            var objects_list = new ConcurrentBag<KeyValuePair<string, IReadOnlyList<YoloV4Result>>>();
            var receive = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < mainYoloV4.Count; i++)
                {
                    var results = bufferBlock.Receive();
                    objects_list.Add(results);

                    foreach(var item in results.Value) {
                        var x1 = (int) item.BBox[0];
                        var y1 = (int) item.BBox[1];
                        var x2 = (int) item.BBox[2];
                        var y2 = (int) item.BBox[3];
                        var img = cropBitmap(results.Key, x1, y1, x2, y2);
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

            return objects_list;
        }

        [Route("get")]
        public List<DetectedObject> Get()
        {
            return dB.DetectedObjects.ToList();
        }


        [Route("clear")]
        public void Clear()
        {
            foreach (var item in dB.DetectedObjects)
                {
                    dB.DetectedObjects.Remove(item);
                }

                foreach (var item in dB.DetectedObjectDetails)
                {
                    dB.DetectedObjectDetails.Remove(item);
                }
                dB.SaveChanges();
        }

        [Route("cancel")]
        public void Cencel() {
            cts.Cancel();
            cts = new CancellationTokenSource();
        }
        
        private byte[] imageToByteArray(System.Drawing.Bitmap image)
        {
            MemoryStream ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            return  ms.ToArray();
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