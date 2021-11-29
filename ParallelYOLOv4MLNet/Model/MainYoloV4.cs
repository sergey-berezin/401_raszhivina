using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;
using System.Threading.Tasks.Dataflow;
using System.Threading;



namespace Model
{
    public class MainYoloV4
    {
        public int Count { get; set; }
        string[] imageNames;

        static readonly string[] classesNames = new string[] { "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };

        const string modelPath = "/Users/dasharazzhivina/Desktop/Models/yolov4.onnx";

        public MainYoloV4(string imageFolder) {
            imageNames = Directory.GetFiles(imageFolder, "*.jpg");
            Count = imageNames.Length;
        }

        public void GetResult(BufferBlock<KeyValuePair<string, IReadOnlyList<YoloV4Result>>> bufferBlock, CancellationToken ct) {
            var getResult = new ActionBlock<string>(imageName =>
            {
                if(ct.IsCancellationRequested) {
                    return;
                }

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
                    bufferBlock.Post(new KeyValuePair<string, IReadOnlyList<YoloV4Result>>(imageName, results));
                }
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct
            });
            
            foreach (var imageName in imageNames) {
                getResult.Post(imageName);
            }

            getResult.Complete();
            try {
                getResult.Completion.Wait();
            } catch {}

        }
    }
}
