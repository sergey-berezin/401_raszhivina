using Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;

namespace ParallelYOLOv4MLNet
{
   
    class Program
    {
        static void Main()
        {
            const string imageFolder = "/Users/dasharazzhivina/Desktop/401_raszhivina/ParallelYOLOv4MLNet/ParallelYOLOv4MLNet/Images";
            //string imageFolder = Console.ReadLine();

            var mainYoloV4 = new MainYoloV4(imageFolder);

            CancellationTokenSource cts = new CancellationTokenSource();
        
            CancellationToken ct = cts.Token;

            var sw = new Stopwatch();
            sw.Start();

            int num = 0;
            var bufferBlock = new BufferBlock<KeyValuePair<string, IReadOnlyList<YoloV4Result>>>();
            var output = new ConcurrentBag<YoloV4Result>();
            
            var receive = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < mainYoloV4.Count; i++)
                {
                    var results = bufferBlock.Receive();
                    foreach(var item in results.Value) {
                        output.Add(item);
                    }
                    num++;
                    Console.WriteLine("Обработано {0:0.00}%", (double)num * 100 / mainYoloV4.Count); 
                    var query = output.GroupBy(x => x.Label);
                    foreach(var item in query) {
                        Console.WriteLine($"{item.Key} - {item.Count()}"); 
                    } 
                }
            }, TaskCreationOptions.LongRunning);

            mainYoloV4.GetResult(bufferBlock, ct);
            
            receive.Wait();

            sw.Stop();
            Console.WriteLine($"Done in {sw.ElapsedMilliseconds}ms.");
        }
    }
}
