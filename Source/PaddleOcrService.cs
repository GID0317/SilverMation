using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace SilverMation
{
    public class PaddleOcrService
    {
        private static readonly object locker = new();
        private readonly PaddleOcrAll _paddleOcrAll;

        public PaddleOcrService()
        {
            var path = "PaddleOCRModels";
            var localDetModel = DetectionModel.FromDirectory(Path.Combine(path, "en_PP-OCRv3_det"), ModelVersion.V3);
            var localClsModel = ClassificationModel.FromDirectory(Path.Combine(path, "en_number_mobile_v2.0_rec")); // Use the English classification model
            var localRecModel = RecognizationModel.FromDirectory(Path.Combine(path, "en_PP-OCRv3_rec"), Path.Combine(path, "en_dict.txt"), ModelVersion.V4);
            var model = new FullOcrModel(localDetModel, localClsModel, localRecModel);

            _paddleOcrAll = new PaddleOcrAll(model, PaddleDevice.Onnx())
            {
                AllowRotateDetection = false,
                Enable180Classification = false
            };
        }

        public string Ocr(Mat mat)
        {
            return OcrResult(mat).Text;
        }

        public PaddleOcrResult OcrResult(Mat mat)
        {
            lock (locker)
            {
                long startTime = Stopwatch.GetTimestamp();
                var result = _paddleOcrAll.Run(mat);
                TimeSpan time = Stopwatch.GetElapsedTime(startTime);
                Debug.WriteLine($"PaddleOcr took {time.TotalMilliseconds}ms, result: {result.Text}");
                return result;
            }
        }

        public string OcrWithoutDetector(Mat mat)
        {
            lock (locker)
            {
                var str = _paddleOcrAll.Recognizer.Run(mat).Text;
                Debug.WriteLine($"PaddleOcrWithoutDetector result: {str}");
                return str;
            }
        }
    }
}
