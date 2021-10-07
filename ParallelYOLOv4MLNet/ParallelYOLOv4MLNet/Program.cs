using Model;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks;

namespace ParallelYOLOv4MLNet
{
   
    class Program
    {
        const string imageFolder = @"Assets/Images";

        const string imageOutputFolder = @"Assets/Output";

        static void Main()
        {
            Directory.CreateDirectory(imageOutputFolder);

            var mainYoloV4 = new MainYoloV4(imageFolder);

            var sw = new Stopwatch();
            sw.Start();

            int num = 0;
            
            var receive = Task.Run(() =>
            {
                for (int i = 0; i < mainYoloV4.Count; i++)
                {
                    var r = mainYoloV4.bufferBlock.Receive();
                    num++;
                    Console.WriteLine("{0:0.00}%", (double)num * 100 / mainYoloV4.Count); 
                    foreach(var item in r) {
                        Console.WriteLine(item.Label); 
                    } 
                }
            });


            mainYoloV4.GetResult();
            
            receive.Wait();

            sw.Stop();
            Console.WriteLine($"Done in {sw.ElapsedMilliseconds}ms.");

            
        }
    }
}
