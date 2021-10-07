using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks;


namespace Model
{
    public class MainYoloV4
    {
        public int Count;
        public BufferBlock<IReadOnlyList<YoloV4Result>> bufferBlock;
        string[] imageNames;

        static readonly string[] classesNames = new string[] { "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };

        const string modelPath = @"../../../Models/yolov4.onnx";

        public MainYoloV4(string imageFolder) {
            imageNames = Directory.GetFiles(imageFolder);
            bufferBlock = new BufferBlock<IReadOnlyList<YoloV4Result>>();
            Count = imageNames.Length;

        }

        public void GetResult() {
            var getResult = new ActionBlock<string>(imageName =>
            {
                MLContext mlContext = new MLContext();

                var pipeline = mlContext.Transforms.ResizeImages(inputColumnName: "bitmap", outputColumnName: "input_1:0", imageWidth: 416, imageHeight: 416, resizing: ResizingKind.IsoPad)
                    .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input_1:0", scaleImage: 1f / 255f, interleavePixelColors: true))
                    .Append(mlContext.Transforms.ApplyOnnxModel(
                        shapeDictionary: new Dictionary<string, int[]>()
                        {
                            { "input_1:0", new[] { 1, 416, 416, 3 } },
                            { "Identity:0", new[] { 1, 52, 52, 3, 85 } },
                            { "Identity_1:0", new[] { 1, 26, 26, 3, 85 } },
                            { "Identity_2:0", new[] { 1, 13, 13, 3, 85 } },
                        },
                        inputColumnNames: new[]
                        {
                            "input_1:0"
                        },
                        outputColumnNames: new[]
                        {
                            "Identity:0",
                            "Identity_1:0",
                            "Identity_2:0"
                        },
                        modelFile: modelPath, recursionLimit: 100));

                // Fit on empty list to obtain input data schema
                var model = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new List<YoloV4BitmapData>()));

                // Create prediction engine
                var predictionEngine = mlContext.Model.CreatePredictionEngine<YoloV4BitmapData, YoloV4Prediction>(model);

                using (var bitmap = new Bitmap(Image.FromFile(imageName)))
                {
                    // predict

                    var predict = predictionEngine.Predict(new YoloV4BitmapData() { Image = bitmap });
                    var results = predict.GetResults(classesNames, 0.3f, 0.7f);
                    bufferBlock.Post(results);
                    

                    /*using (var g = Graphics.FromImage(bitmap))
                    {
                        foreach (var res in results)
                        {
                            // draw predictions
                            var x1 = res.BBox[0];
                            var y1 = res.BBox[1];
                            var x2 = res.BBox[2];
                            var y2 = res.BBox[3];
                            g.DrawRectangle(Pens.Red, x1, y1, x2 - x1, y2 - y1);
                            using (var brushes = new SolidBrush(Color.FromArgb(50, Color.Red)))
                            {
                                g.FillRectangle(brushes, x1, y1, x2 - x1, y2 - y1);
                            }

                            g.DrawString(res.Label + " " + res.Confidence.ToString("0.00"),
                                         new Font("Arial", 12), Brushes.Blue, new PointF(x1, y1));
                        }
                        bitmap.Save(Path.Combine(imageOutputFolder, Path.ChangeExtension(Path.GetFileName(imageName), "_processed" + Path.GetExtension(Path.GetFileName(imageName)))));
                    }*/
                }
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 4
            });
            
            Parallel.ForEach (imageNames, imageName => 
            {
                getResult.Post(imageName);
            });


            getResult.Complete();

            getResult.Completion.Wait();

        }
    }
}
