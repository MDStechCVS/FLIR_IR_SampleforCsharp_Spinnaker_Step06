using OpenCvSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace SpinnakerTest
{
    class ImageProcessing
    {
        #region Private Variable 

        public int thresholdvalue = 80;

        #endregion

        #region ImageProcessing
        /// <summary>
        /// 이진화
        /// </summary>
        /// <param name="_bitmap"></param>
        /// <returns></returns>
        public Bitmap Thresholding(Bitmap _bitmap) 
        {
            Mat src = BitmapToMat(_bitmap);
            Mat dst = new Mat();
            Cv2.CvtColor(src, dst, ColorConversionCodes.RGBA2GRAY);
            Cv2.Threshold(dst, dst, thresholdvalue, 255, ThresholdTypes.Binary);
            Bitmap processBitmap = MatToBitmap(dst);
            return processBitmap;
        }

        /// <summary>
        /// 그레이스케일 
        /// </summary>
        /// <param name="_bitmap"></param>
        /// <returns></returns>
        public Bitmap Grayscale(Bitmap _bitmap)
        {
            Mat src = BitmapToMat(_bitmap);
            Mat dst = new Mat();
            Cv2.CvtColor(src, dst, ColorConversionCodes.RGBA2GRAY);
            Bitmap processBitmap = MatToBitmap(dst);
            return processBitmap;
        }

        /// <summary>
        /// 이진화
        /// </summary>
        /// <param name="_bitmap"></param>
        /// <returns></returns>
        public Bitmap Edgedetection(Bitmap _bitmap)
        {
            Mat src = BitmapToMat(_bitmap);
            Mat dst = new Mat();
            Cv2.CvtColor(src, dst, ColorConversionCodes.RGBA2GRAY);
            Cv2.Threshold(dst, dst, thresholdvalue, 255, ThresholdTypes.Binary);
            Cv2.Canny(dst, dst, 100, 500,3, false); 
            Bitmap processBitmap = MatToBitmap(dst);
            return processBitmap;
        }

        /// <summary>
        /// 침식
        /// </summary>
        /// <param name="_bitmap"></param>
        /// <returns></returns>
        public Bitmap Erosion(Bitmap _bitmap)
        {
            Mat src = BitmapToMat(_bitmap);
            Mat dst = new Mat();

            // 커널 정의
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5));
            // 커널 사이즈 변경 
            //Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(thresholdvalue, thresholdvalue));

            Cv2.CvtColor(src, dst, ColorConversionCodes.RGBA2GRAY);
            Cv2.Threshold(dst, dst, thresholdvalue, 255, ThresholdTypes.Binary);
            Cv2.Erode(dst, dst, kernel);

            Bitmap processBitmap = MatToBitmap(dst);
            return processBitmap;
        }

      
        /// <summary>
        /// 팽창
        /// </summary>
        /// <param name="_bitmap"></param>
        /// <returns></returns>
        public Bitmap Dilatation(Bitmap _bitmap)
        {
            Mat src = BitmapToMat(_bitmap);
            Mat dst = new Mat();

            // 커널 정의
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5));
            // 커널 사이즈 변경 
            //Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(thresholdvalue, thresholdvalue));

            Cv2.CvtColor(src, dst, ColorConversionCodes.RGBA2GRAY);
            Cv2.Dilate(dst, dst, kernel);

            Bitmap processBitmap = MatToBitmap(dst);
            return processBitmap;
        }

        /// <summary>
        /// 침식 연산 후 팽창 
        /// </summary>
        /// <param name="_bitmap"></param>
        /// <returns></returns>
        public Bitmap Opening(Bitmap _bitmap)
        {
            Mat src = BitmapToMat(_bitmap);
            Mat dst = new Mat();

            // 커널 정의
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5));
            // 커널 사이즈 변경 
            //Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(thresholdvalue, thresholdvalue));

            Cv2.CvtColor(src, dst, ColorConversionCodes.RGBA2GRAY);
            Cv2.Threshold(dst, dst, thresholdvalue, 255, ThresholdTypes.Binary);
            Cv2.MorphologyEx(dst, dst, MorphTypes.Open, kernel);

            Bitmap processBitmap = MatToBitmap(dst);
            return processBitmap;
        }

        /// <summary>
        /// 침식 연산 후 팽창 
        /// </summary>
        /// <param name="_bitmap"></param>
        /// <returns></returns>
        public Bitmap Gradient(Bitmap _bitmap)
        {
            Mat src = BitmapToMat(_bitmap);
            Mat dst = new Mat();

            // 커널 정의
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5));
            // 커널 사이즈 변경 
            //Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(thresholdvalue, thresholdvalue));

            Cv2.CvtColor(src, dst, ColorConversionCodes.RGBA2GRAY);
            //Cv2.Threshold(dst, dst, thresholdvalue, 255, ThresholdTypes.Binary);
            Cv2.MorphologyEx(dst, dst, MorphTypes.Gradient, kernel);

            Bitmap processBitmap = MatToBitmap(dst);
            return processBitmap;
        }
        #endregion

        #region Converter
        public Mat BitmapToMat(Bitmap originalBitmap)
        {
            BitmapData bitmapData = null;
            Mat mat = null;

            try
            {
                // Bitmap 정보를 가져옵니다.
                bitmapData = originalBitmap.LockBits(new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height),
                                                     ImageLockMode.ReadOnly, originalBitmap.PixelFormat);

                // 픽셀 포맷에 따라 채널 수를 결정합니다.
                int channels = (originalBitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

                // Mat 객체 생성
                mat = new Mat(originalBitmap.Height, originalBitmap.Width, channels == 3 ? MatType.CV_8UC3 : MatType.CV_8UC4);

                // Bitmap 데이터를 Mat로 복사
                unsafe
                {
                    byte* src = (byte*)bitmapData.Scan0;
                    byte* dst = (byte*)mat.DataPointer;

                    int stride = bitmapData.Stride;
                    long step = mat.Step();

                    for (int y = 0; y < originalBitmap.Height; y++)
                    {
                        Buffer.MemoryCopy(src + y * stride, dst + y * step, step, stride);
                    }
                }
            }
            finally
            {
                // 반드시 UnlockBits 호출하여 리소스 해제
                if (bitmapData != null)
                    originalBitmap.UnlockBits(bitmapData);
            }

            return mat;
        }
        public Bitmap MatToBitmap(Mat mat)
        {
            // PixelFormat을 결정합니다.
            PixelFormat pixelFormat;
            if (mat.Channels() == 1)
            {
                pixelFormat = PixelFormat.Format8bppIndexed;
            }
            else if (mat.Channels() == 3)
            {
                pixelFormat = PixelFormat.Format24bppRgb;
            }
            else if (mat.Channels() == 4)
            {
                pixelFormat = PixelFormat.Format32bppArgb;
            }
            else
            {
                throw new NotSupportedException($"Unsupported number of channels: {mat.Channels()}");
            }

            Bitmap bitmap = new Bitmap(mat.Width, mat.Height, pixelFormat);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                                    ImageLockMode.WriteOnly, pixelFormat);

            try
            {
                unsafe
                {
                    byte* src = (byte*)mat.DataPointer;
                    byte* dst = (byte*)bitmapData.Scan0.ToPointer();

                    int stride = bitmapData.Stride;
                    int step = (int)mat.Step();

                    for (int y = 0; y < mat.Height; y++)
                    {
                        Buffer.MemoryCopy(src + y * step, dst + y * stride, bitmapData.Height * stride, step);
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            // 그레이스케일 팔레트 설정 (8bppIndexed 이미지인 경우)
            if (mat.Channels() == 1)
            {
                ColorPalette palette = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                {
                    palette.Entries[i] = Color.FromArgb(i, i, i);
                }
                bitmap.Palette = palette;
            }

            return bitmap;
        }

        #endregion


    }
}
