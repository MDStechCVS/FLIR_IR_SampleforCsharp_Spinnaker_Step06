using SpinnakerNET;
using SpinnakerNET.GenApi;
using SpinnakerNET.GUI;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SpinnakerNET.GUI.WPFControls;
using System.Threading;
using System.Drawing;
using Pen = System.Drawing.Pen;
using System.Collections.Generic;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using OpenCvSharp;


namespace SpinnakerTest
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        #region PRIVATE

        PropertyGridControl gridControl;

        Thread thMainView = null;

        CameraSelectionWindow camSelControl;

        // Min, Max icon
        MeasureSpotValue maxSpot;
        MeasureSpotValue minSpot;

        // ROI 
        MeasureBoxValue roiBox;

        // 카메라 해상도 
        private const int int640480 = 640 * 480;
        private const int int464348 = 464 * 348;
        private const int int320256 = 320 * 256;
        private const int int320240 = 320 * 240;

        private int stIntCamFrameArray = int320256;
        private int mCurWidth = 320;
        private int mCurHeight = 256;

        private bool bProcessing = false;

        // Offset Value  
        private const float mOffsetVal_001 = 0.01f;
        private const float mOffsetVal_01 = 0.1f;
        private const float mOffsetVal_004 = 0.04f;
        private const float mOffsetVal_04 = 0.4f;

        private float mConvertOffsetVal = mOffsetVal_001;

        private double minBox = 0;
        private double maxBox = 0;

        private Bitmap bmp = null;
        private Bitmap bmp2 = null;
        private int step = 64; // 총 4개의 간격으로 나눔

        private IManagedCamera connectcam = null; // 연결 된 카메라 객체 
        private string CamDevice = null;  //  연결 된 카메라 기종 

        // 카메라 온도 Range 설정 
        private int TempRangeVal = 0;
        private List<Int16> RangeIndexData = new List<Int16>();

        /// <summary>
        /// Data 추출 Thread 구동 여부
        /// </summary>
        private bool isRunning = false;

        // 현재 팔레트 
        private string Current_Palette = "Iron";

        // Scale 조정 
        private int _min;
        private int _diff;
        private int _max;

        //private int _scalemaxtemp;
        //private int _scalemaxraw;
        //private int _scalemintemp;
        //private int _scaleminraw;

        private int _mintext = 0;
        private int _maxtext = 0;

        private bool _usecheckbox = true;

        // 이미지 저장 경로 
        private string savepath = @"D:\MDS_Save";

        // NUC 
        private bool bAutoShutter = true;

        // 영상처리
        private ConcurrentQueue<Bitmap> frameQueue = new ConcurrentQueue<Bitmap>();
        private bool isProcessing = true;
        private ImageProcessing processing = new ImageProcessing();
        private string selectedItem;


        MDSColorPalette MDSPALETTE = new MDSColorPalette();

        // 16진수 값을 변환하여 ColorMap 생성한 값을 저장 
        private List<Color> PaletteColorMap = new List<Color>();

        private ImageProcessing imagetogray = new ImageProcessing();

        #endregion

        #region PUBLIC

        public class MeasureSpotValue
        {
            int mPointIdx;
            ushort mTempValue;

            Pen mPen = new Pen(System.Drawing.Color.AliceBlue);

            public MeasureSpotValue(System.Drawing.Color cl)
            {
                mPen.Color = cl;
            }

            public void SetXY(Graphics gr, int nX, int nY)
            {
                gr.DrawLine(mPen, nX - 10, nY, nX + 10, nY);  // 수평
                gr.DrawLine(mPen, nX, nY - 10, nX, nY + 10);  // 수직
            }

            public void SetPointIndex(int nIndex)
            {
                mPointIdx = nIndex;
            }

            public int GetPointIndex()
            {
                return mPointIdx;
            }

            public void SetTempVal(ushort usTempValue)
            {
                mTempValue = usTempValue;
            }
          
        }

        // 측정 영역 Box
        public class MeasureBoxValue
        {
            int mX;
            int mY;
            int mWidth;
            int mHeight;
            int mPointIdx;
            ushort mTempValue;
            bool mIsVisible = false;

            // Box 영역 내의 최대 최소 위치
            int mMax_X;
            int mMax_Y;
            int mMin_X;
            int mMin_Y;

            // Box 영역 내의 최대, 최소 온도값
            ushort mMax = 0;
            ushort mMin = 65535;

            Pen mPen = new Pen(System.Drawing.Color.AliceBlue);
            Pen mPenMax = new Pen(System.Drawing.Color.Red);
            Pen mPenMin = new Pen(System.Drawing.Color.Blue);

            public MeasureBoxValue(System.Drawing.Color cl, int nX, int nY, int nWidth, int nHeight)
            {
                mPen.Color = cl;

                mX = nX;
                mY = nY;
                mWidth = nWidth;
                mHeight = nHeight;
            }

            public void MeasureBoxValueChange(int nX, int nY, int nWidth, int nHeight)
            {
                try
                {
                    // ROI 영역의 위치 및 크기 값을 변경 
                    mX = nX;
                    mY = nY;
                    mWidth = nWidth;
                    mHeight = nHeight;
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            public void ResetMinMax()
            {
                mMax_X = 0;
                mMax_Y = 0;
                mMin_X = 0;
                mMin_Y = 0;

                mMax = 0;
                mMin = 65535;
            }

            public void SetXYWH(Graphics gr)
            {
                gr.DrawRectangle(mPen, mX, mY, mWidth, mHeight);  // Box
            }

            public void SetMax(Graphics gr)
            {
                gr.DrawRectangle(mPenMax, mMax_X - 3, mMax_Y - 3, 6, 6);  // Box Max
            }

            public void SetMin(Graphics gr)
            {
                gr.DrawRectangle(mPenMin, mMin_X - 3, mMin_Y - 3, 6, 6);  // Box Min
            }

            public void GetMinMax(out ushort usMin, out ushort usMax)
            {
                usMin = mMin;
                usMax = mMax;
            }

            public void SetPointIndex(int nIndex)
            {
                mPointIdx = nIndex;
            }

            public int GetPointIndex()
            {
                return mPointIdx;
            }

            public void SetTempVal(ushort usTempValue)
            {
                mTempValue = usTempValue;
            }

            public bool GetIsVisible()
            {
                return mIsVisible;
            }

            public void SetIsVisible(bool bVal)
            {
                mIsVisible = bVal;
            }

            public bool CheckXYinBox(int nX, int nY, ushort tempVal)
            {
                bool rValue = false;

                if ((mX <= nX) && ((mX + mWidth) >= nX))   // X 좌표가 범위 내에 있는지
                {
                    if ((mY <= nY) && ((mY + mHeight) >= nY))   // Y 좌표가 범위 내에 있는지
                    {
                        rValue = true;

                        // 최대 최소 온도 체크 후 백업
                        if (mMin >= tempVal)
                        {
                            mMin = tempVal;
                            mMin_X = nX;
                            mMin_Y = nY;
                        }
                        else if (mMax < tempVal)
                        {
                            mMax = tempVal;
                            mMax_X = nX;
                            mMax_Y = nY;
                        }
                    }
                }
                return rValue;
            }
        }





        #endregion

        #region STEP1 - 00. START / END 
        public MainWindow()
        {
            InitializeComponent();

            gridControl = new PropertyGridControl();
            Grid.SetRow(gridControl, 1);

            maxSpot = new MeasureSpotValue(System.Drawing.Color.Red);
            minSpot = new MeasureSpotValue(System.Drawing.Color.LightSkyBlue);

            Palette_ComboBox_Initialize();
            processsldInitialize();

            

            StartImageProcessingThread(); 
        }

        private void Window_Closed(object sender, EventArgs e)
        {

            if (connectcam != null)
            {
                connectcam.Dispose();
            }

            if (thMainView != null)
            {
                thMainView.Abort();
            }
        }
        #endregion

        #region STEP1 - 01. CAMERA SELECT / EVENT
        private void ConnectSpinnaker_Click(object sender, RoutedEventArgs e)
        {
            // Spinnaker 연결 창 전시 
            camSelControl = new CameraSelectionWindow();
            camSelControl.Width = 550;
            camSelControl.Height = 300;

            // Spinnaker 연결 창 클릭 이벤트 등록 
            camSelControl.OnDeviceClicked += ConnectControls;

            // Spinnaker 연결 창 전시 
            camSelControl.ShowModal(true);
        }

        void ConnectControls(object sender, CameraSelectionWindow.DeviceEventArgs args)
        {
            // Check whether an Interface is selected
            if (args.IsCamera == false && args.Interface != null)
            {
                ResetWindowControl(); // Disconnect previous device
            }
            // Check whether a System is selected
            else if (args.IsSystem == true && args.System != null)
            {
                ResetWindowControl(); // Disconnect previous device
            }
            else // Connect a camera (Previous device was interface || Camera is first device connected)
            {
                SetCamera(args.Camera, false, args.Interface);
                connectcam = args.Camera;
            }

            camSelControl.Close();
        }

        private void ResetWindowControl()
        {
            try
            {
                if (connectcam == null)
                {
                    return;
                }

                // Disconnect controls
                CameraDisconnect(connectcam);

            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
        }
        #endregion

        #region STEP1 - 02. CAMERA CONNECT
        /// <summary>
        /// Connect ImageDrawingControl and PropertyGridControl with IManagedCamera
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="startStreaming">Boolean indicating whether to start streaming</param>
        /// <param name="parentInterface">Parent interface </param>
        void SetCamera(IManagedCamera cam, bool startStreaming = false, IManagedInterface parentInterface = null)
        {
            try
            {
                cam.Init();

                // 카메라 연결 
                camPlayOne(cam);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("There was a problem connecting to IManagedCamera.\n{0}", ex.Message));
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        void camPlayOne(IManagedCamera cam)
        {
            try
            {
                // 카메라 파라미터 설정 
                ConfigurationCam(cam);

                // Begin acquiring images
                cam.BeginAcquisition();

                isRunning = true;

                Console.Write("\tDevice {0} ", 0);
               

                Thread.Sleep(1000);

                thMainView = new Thread(() => threadProc(cam));
                thMainView.Start();

            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }

        void ConfigurationCam(IManagedCamera cam)
        {
            try
            {
                INodeMap nodeMap = cam.GetNodeMap();

                StringReg iModelName = nodeMap.GetNode<StringReg>("DeviceModelName");
                string modelname = iModelName.ToString();

                StringReg iManufactureInfo = nodeMap.GetNode<StringReg>("DeviceManufacturerInfo");

                string manufacturerInfo = iManufactureInfo.ToString();
                // 연결된 카메라의 기종에 따라 Width, Height, parameter 값 설정 
                if (modelname.Contains("AX5")) // Ax5
                {
                    stIntCamFrameArray = int320256;
                    mCurWidth = 320;
                    mCurHeight = 256;

                    bmp = new Bitmap(mCurWidth, mCurHeight);
                    bmp2 = new Bitmap(mCurWidth, mCurHeight);

                    // pixelformat 설정 
                    IEnum iPixelFormat = nodeMap.GetNode<IEnum>("PixelFormat");
                    if (iPixelFormat != null && iPixelFormat.IsWritable)
                    {
                        IEnumEntry iPixelFormatMono14 = iPixelFormat.GetEntryByName("Mono14");
                        iPixelFormat.Value = iPixelFormatMono14.Value;
                        Console.WriteLine("iPixelFormatMono14 : " + nodeMap.GetNode<IEnum>("PixelFormat").ToString());
                    }

                    // digital output 설정 
                    IEnum iDigitalOutput = nodeMap.GetNode<IEnum>("DigitalOutput");
                    if (iDigitalOutput != null && iDigitalOutput.IsWritable)
                    {
                        IEnumEntry iDigitalOutput14bit = iDigitalOutput.GetEntryByName("bit14bit");
                        iDigitalOutput.Value = iDigitalOutput14bit.Value;
                        Console.WriteLine("iDigitalOutput14bit : " + nodeMap.GetNode<IEnum>("DigitalOutput").ToString());
                    }

                    // TemperatureLinearMode 설정 - Offset 값 설정 
                    IEnum iTemperatureLinearMode = nodeMap.GetNode<IEnum>("TemperatureLinearMode");
                    if (iTemperatureLinearMode != null && iTemperatureLinearMode.IsWritable)
                    {
                        IEnumEntry iTemperatureLinearModeOn = iTemperatureLinearMode.GetEntryByName("On");
                        iTemperatureLinearMode.Value = iTemperatureLinearModeOn.Value;

                        IEnum iTemperatureLinearResolution = nodeMap.GetNode<IEnum>("TemperatureLinearResolution");
                        if (iTemperatureLinearResolution != null && iTemperatureLinearResolution.IsWritable)
                        {
                            IEnumEntry iTemperatureLinearResolutionHigh = iTemperatureLinearResolution.GetEntryByName("High");
                            iTemperatureLinearResolution.Value = iTemperatureLinearResolutionHigh.Value;
                            mConvertOffsetVal = mOffsetVal_004;

                            Console.WriteLine("iTemperatureLinearModeOn : " + nodeMap.GetNode<IEnum>("TemperatureLinearMode").ToString());
                            Console.WriteLine("iTemperatureLinearResolution : " + nodeMap.GetNode<IEnum>("TemperatureLinearResolution").ToString());
                        }
                    }

                    CamDevice = "AX5";
                }
                else if (modelname.Contains("PT1000")) // FLIR Axx
                {
                    if(manufacturerInfo != null && manufacturerInfo.Contains("A320"))
                    {
                        stIntCamFrameArray = int320240;
                        mCurWidth = 320;
                        mCurHeight = 240;
                    }
                    else
                    {
                        stIntCamFrameArray = int640480;
                        mCurWidth = 640;
                        mCurHeight = 480;
                    }

                    bmp = new Bitmap(mCurWidth, mCurHeight);
                    bmp2 = new Bitmap(mCurWidth, mCurHeight);

                    // pixelformat 설정 
                    IEnum iPixelFormat = nodeMap.GetNode<IEnum>("PixelFormat");
                    if (iPixelFormat != null && iPixelFormat.IsWritable)
                    {
                        IEnumEntry iPixelFormatMono16 = iPixelFormat.GetEntryByName("Mono16");
                        iPixelFormat.Value = iPixelFormatMono16.Value;
                        Console.WriteLine("iPixelFormatMono16 : " + nodeMap.GetNode<IEnum>("PixelFormat").ToString());
                    }

                    // TemperatureLinearMode 설정 및 offset 값 설정 
                    IEnum iTemperatureLinearMode = nodeMap.GetNode<IEnum>("IRFormat");
                    if (iTemperatureLinearMode != null && iTemperatureLinearMode.IsWritable)
                    {
                        IEnumEntry iTemperatureLinearMode100mk = iTemperatureLinearMode.GetEntryByName("TemperatureLinear100mK");
                        iTemperatureLinearMode.Value = iTemperatureLinearMode100mk.Value;

                        mConvertOffsetVal = mOffsetVal_01;

                        Console.WriteLine("iTemperatureLinearMode 100mk : " + nodeMap.GetNode<IEnum>("IRFormat").ToString());
                    }

                    CamDevice = "PT1000";
                   

                }
                else if (modelname.Contains("A50")) // A50, A500
                {
                    stIntCamFrameArray = int464348;
                    mCurWidth = 464;
                    mCurHeight = 348;

                    bmp = new Bitmap(mCurWidth, mCurHeight);
                    bmp2 = new Bitmap(mCurWidth, mCurHeight);

                    // pixelformat 설정 
                    IEnum iPixelFormat = nodeMap.GetNode<IEnum>("PixelFormat");
                    if (iPixelFormat != null && iPixelFormat.IsWritable)
                    {
                        IEnumEntry iPixelFormatMono16 = iPixelFormat.GetEntryByName("Mono16");
                        iPixelFormat.Value = iPixelFormatMono16.Value;
                        Console.WriteLine("iPixelFormatMono16 : " + nodeMap.GetNode<IEnum>("PixelFormat").ToString());
                    }

                    // TemperatureLinearMode 설정 및 Offset 값 설정 
                    IEnum iTemperatureLinearMode = nodeMap.GetNode<IEnum>("IRFormat");
                    if (iTemperatureLinearMode != null && iTemperatureLinearMode.IsWritable)
                    {
                        IEnumEntry iTemperatureLinearMode100mk = iTemperatureLinearMode.GetEntryByName("TemperatureLinear100mK");
                        iTemperatureLinearMode.Value = iTemperatureLinearMode100mk.Value;

                        mConvertOffsetVal = mOffsetVal_01;

                        Console.WriteLine("iTemperatureLinearMode 100mk : " + nodeMap.GetNode<IEnum>("IRFormat").ToString());
                    }

                    CamDevice = "A50";

                }
                else if (modelname.Contains("A70")) // A70, A700
                {
                    stIntCamFrameArray = int640480;
                    mCurWidth = 640;
                    mCurHeight = 480;

                    bmp = new Bitmap(mCurWidth, mCurHeight);
                    bmp2 = new Bitmap(mCurWidth, mCurHeight);

                    // PixelFormat 설정 
                    IEnum iPixelFormat = nodeMap.GetNode<IEnum>("PixelFormat");
                    if (iPixelFormat != null && iPixelFormat.IsWritable)
                    {
                        IEnumEntry iPixelFormatMono16 = iPixelFormat.GetEntryByName("Mono16");
                        iPixelFormat.Value = iPixelFormatMono16.Value;
                        Console.WriteLine("iPixelFormatMono16 : " + nodeMap.GetNode<IEnum>("PixelFormat").ToString());
                    }

                    // TemperatureLinearMode 설정 및 Offset값 설정 
                    IEnum iTemperatureLinearMode = nodeMap.GetNode<IEnum>("IRFormat");
                    if (iTemperatureLinearMode != null && iTemperatureLinearMode.IsWritable)
                    {
                        IEnumEntry iTemperatureLinearMode100mk = iTemperatureLinearMode.GetEntryByName("TemperatureLinear100mK");
                        iTemperatureLinearMode.Value = iTemperatureLinearMode100mk.Value;

                        mConvertOffsetVal = mOffsetVal_01;

                        Console.WriteLine("iTemperatureLinearMode 100mk : " + nodeMap.GetNode<IEnum>("IRFormat").ToString());
                    }

                    CamDevice = "A70";
                   
                }
                else if (modelname.Contains("A400")) // A400
                {
                    stIntCamFrameArray = int320240;
                    mCurWidth = 320;
                    mCurHeight = 240;

                    bmp = new Bitmap(mCurWidth, mCurHeight);
                    bmp2 = new Bitmap(mCurWidth, mCurHeight);

                    // PixelFormat 설정 
                    IEnum iPixelFormat = nodeMap.GetNode<IEnum>("PixelFormat");
                    if (iPixelFormat != null && iPixelFormat.IsWritable)
                    {
                        IEnumEntry iPixelFormatMono16 = iPixelFormat.GetEntryByName("Mono16");
                        iPixelFormat.Value = iPixelFormatMono16.Value;
                        Console.WriteLine("iPixelFormatMono16 : " + nodeMap.GetNode<IEnum>("PixelFormat").ToString());
                    }

                    // TemperatureLinearMode 설정 및 Offset 값 설정 
                    IEnum iTemperatureLinearMode = nodeMap.GetNode<IEnum>("IRFormat");
                    if (iTemperatureLinearMode != null && iTemperatureLinearMode.IsWritable)
                    {
                        IEnumEntry iTemperatureLinearMode100mk = iTemperatureLinearMode.GetEntryByName("TemperatureLinear100mK");
                        iTemperatureLinearMode.Value = iTemperatureLinearMode100mk.Value;

                        mConvertOffsetVal = mOffsetVal_01;

                        Console.WriteLine("iTemperatureLinearMode 100mk : " + nodeMap.GetNode<IEnum>("IRFormat").ToString());
                    }

                    CamDevice = "A400";
                }

                // 카메라 별 측정 온도 값 구성 및 설정 
                TempRangeConf(nodeMap);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }

        private void CameraDisconnect(IManagedCamera cam)
        {
            try
            {
                isRunning = false;

                if (cam.IsStreaming())
                {
                    cam.EndAcquisition();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }

        private void DisConnectSpinnaker_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (connectcam == null)
                {
                    return;
                }
                CameraDisconnect(connectcam);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }
        #endregion

        #region STEP1 - 03. CAMERA DATA RECEIVE / SHOW RESULT
        /// <summary>
        /// MdsSdkControl 데이터 Receiver delegate function
        /// </summary>
        /// <param name="data">수신 데이터</param>
        /// <param name="w">width</param>
        /// <param name="h">height</param>
        /// <param name="minval">minimum value</param>
        /// <param name="maxval">maximum value</param>
        delegate void DelegateCtrlData_Receiver(UInt16[] data, int w, int h, ushort minval, ushort maxval);
        /// <summary>
        /// MdsSdkControl 데이터 Receiver
        /// 열화상 카메라로 부터 받은 데이터로 화면을 구성
        /// </summary>
        /// <param name="data">수신 데이터</param>
        /// <param name="w">width</param>
        /// <param name="h">height</param>
        /// <param name="minval">minimum value</param>
        /// <param name="maxval">maximum value</param>
        void CtrlData_Receiver(UInt16[] data, int w, int h, ushort minval, ushort maxval)
        {
            if (data == null)
                return;

            lock (this)
            {
                //SetImage를 수행중이면 리턴.(화면 갱신 skip)
                if (bProcessing)
                {
                    return;
                }
            }

            if (!this.CheckAccess())
            {
                this.Dispatcher.Invoke(new DelegateCtrlData_Receiver(CtrlData_Receiver), new object[] { data, w, h, minval, maxval });
                return;
            }

            try
            {
                lock (this)
                {
                    bProcessing = true;
                }

                System.Drawing.Color col;
                System.Drawing.Color processcol;
                IntPtr hBitmap = IntPtr.Zero;
                IntPtr hBitmaprocess = IntPtr.Zero;

                //x 는 image의 width
                //y 는 image의 height
                int x, y;

                // Box 내 영역의 최대 최소 온도값 초기화
                if (roiBox != null && roiBox.GetIsVisible())
                {
                    roiBox.ResetMinMax();
                }
                
                for (int a = 0; a < data.Length; a++)
                {
                    getXY(a, mCurWidth, out x, out y);

                    _min = minval;
                    _max = maxval;
                    _diff = _max - _min;

                    if (_diff == 0)
                    {
                        _diff = 1;
                    }

                    int rVal = (int)((data[a] - minval) * 255 / _diff);

                    // 인덱스가 음수로 나오는 경우 예외처리 
                    // Scale 설정이 수동인 경우 -> 설정한 min 값보다 측정된 온도 값이 더 낮은 경우 
                    if (rVal < 0) 
                    {
                        rVal = 0; 
                    }

                    if (selectedItem == "None") //영상처리가 none인 경우 
                    {
                        col = GenerateColorPalette(rVal);
                        //processcol = Color.FromArgb(0, 0, 0); 
                    }
                    else
                    {
                        col = GenerateColorPalette(rVal);
                        //processcol = GenerateColorGreyPalette(rVal);
                    }
                    
                    bmp.SetPixel(x, y, col);
                    //bmp2.SetPixel(x, y, processcol); 
              
                }

                //Graphics gr = Graphics.FromImage(bmp);
                ////Graphics gr2 = Graphics.FromImage(bmp2);


                //int maxX = 0;
                //int maxY = 0;
                //int minX = 0;
                //int minY = 0;

                //// max spot get x, y;
                //getXY(maxSpot.GetPointIndex(), mCurWidth, out maxX, out maxY);
                //getXY(minSpot.GetPointIndex(), mCurWidth, out minX, out minY);

                //maxSpot.SetXY(gr, maxX, maxY);
                //minSpot.SetXY(gr, minY, minY);

                // frameQueue에 제대로 삽입되는지 확인

                EnqueueFrame(bmp);
                //frameQueue.Enqueue(bmp);
                //Console.WriteLine("Frame enqueued successfully");

                // Bitmap is ready - update image control
                hBitmap = bmp.GetHbitmap();
                BitmapSource bmpSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                //hBitmaprocess = bmp2.GetHbitmap();
                //BitmapSource bmpSrc2 = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmaprocess, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                //if (bmpSrc.CanFreeze)
                //    bmpSrc.Freeze();

                //if (bmpSrc2.CanFreeze)
                //    bmpSrc2.Freeze();


                this.backgroundImageBrush.ImageSource = bmpSrc;
               // this.backgroundProcessImageBrush.ImageSource = bmpSrc2;

                DeleteObject(hBitmap);
                //DeleteObject(hBitmaprocess);

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("[CtrlEvent] " + e.ToString());
            }
            lock (this)
                bProcessing = false;

            return;
        }
        private void EnqueueFrame(Bitmap bmp)
        {
            lock (bitmapLock)
            {
                frameQueue.Enqueue(new Bitmap(bmp));
                Console.WriteLine("Frame enqueued successfully");
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);
        void threadProc(IManagedCamera cam)
        {
            while (true)
            {
                if (((Thread.CurrentThread.ThreadState & ThreadState.SuspendRequested) == ThreadState.SuspendRequested) ||
                ((Thread.CurrentThread.ThreadState & ThreadState.Suspended) == ThreadState.Suspended))
                {
                    break;
                }

                try
                {
                    if (!isRunning)
                    {
                        return;
                    }

                    if (cam.IsValid() != true && cam != null)
                    {
                        Console.WriteLine("cam is not valid");
                        break;
                    }

                    // Retrieve next received image and ensure image completion
                    using (IManagedImage rawImage = cam.GetNextImage())
                    {
                        if (rawImage.IsIncomplete)
                        {
                            Console.WriteLine("Image incomplete with status {0}...", rawImage.ImageStatus);
                            rawImage.Release();

                            Thread.Sleep(10);
                        }
                        else
                        {
                            // 최고, 최저 온도 값을 섭씨 온도로 계산하여 저장  
                            double minValue = 0;
                            double maxValue = 0;

                            int uint16Count = 0;
                            ushort max16 = 0;
                            ushort min16 = 65535;

                            UInt16[] imgArray = new UInt16[stIntCamFrameArray];

                            for (int a = 0; a < stIntCamFrameArray * 2; a += 2)
                            {
                                if (a >= rawImage.ManagedData.Length)
                                {
                                    // 온도 값이 들어오지 않는 경우 / 카메라 변경 시 예외처리 
                                    return;
                                }

                                ushort sample = BitConverter.ToUInt16(rawImage.ManagedData, a);


                                // 최고, 최저 온도 값 계산 및 좌표 계산 
                                if (min16 >= sample)
                                {
                                    minValue = ((float)(sample) * mConvertOffsetVal) - 273.15;
                                    min16 = sample;
                                    minSpot.SetPointIndex(a / 2);
                                    minSpot.SetTempVal(sample);

                                    _mintext = (int)minValue;
                                    
                                }
                                else if (max16 < sample)
                                {
                                    maxValue = ((float)(sample) * mConvertOffsetVal) - 273.15;
                                    max16 = sample;
                                    maxSpot.SetPointIndex(a / 2);
                                    maxSpot.SetTempVal(sample);

                                    _maxtext = (int)maxValue;
                                   
                                }

                                imgArray[a / 2] = sample;
                                uint16Count++;

                            }

                            CtrlData_Receiver(imgArray, mCurWidth, mCurHeight, min16, max16);

                            CompositionTarget_Rendering(minValue, maxValue, minBox, maxBox);
                            rawImage.Release();
                        }
                    }
                }
                catch (ThreadInterruptedException e)
                {
                    Console.WriteLine(e);
                    break;
                }
                catch (SpinnakerException ex)
                {
                    Console.WriteLine("Error: {0}", ex.Message);
                }

                Thread.Sleep(1);

            }

        }

        // Point index에서 X, Y, 좌표를 알아낸다.
        private void getXY(int sourceIndex, int sourceWidth, out int x, out int y)
        {
            y = sourceIndex / sourceWidth;
            x = sourceIndex % sourceWidth;
        }


        delegate void DelegateCompositionTarget_Rendering(double minval, double maxval, double measurePoint, double measurePoint2);
        void CompositionTarget_Rendering(double minval, double maxval, double roiminval, double roimaxval)
        {
            if (!this.CheckAccess())
            {
                this.Dispatcher.Invoke(new DelegateCompositionTarget_Rendering(CompositionTarget_Rendering), new object[] { minval, maxval, roiminval, roimaxval });
                return;
            }

            // 전체 화면 온도 값 표시 
            MinTemp.Content = string.Format("{0:F1}", minval);
            MaxTemp.Content = string.Format("{0:F1}", maxval);

       
        }
        #endregion


       

        #region STEP1 - 04. CAMERA TEMPERATURE RANGE CONFIGURATION 
        private void TempRangeConf(INodeMap nodeMap)
        {
            try
            {
                // 카메라가 연결되지 않은 경우 
                if (CamDevice == null)
                {
                    Console.WriteLine("No Connected Camera!");
                    return;
                }

                // 온도 range 항목 제거  
                comboRanges.Items.Clear();

                string[] retValue = null;

                // Ax5
                if (CamDevice.Contains("AX5"))
                {
                    IEnum SGM = nodeMap.GetNode<IEnum>("SensorGainMode");

                    if (SGM != null)
                    {
                        EnumEntry[] ee = SGM.Entries;

                        int countValue = ee.Length;

                        retValue = new string[countValue];

                        for (int a = 0; a < countValue; a++)
                        {
                            retValue[a] = ee[a].DisplayName;
                            RangeIndexData.Add((short)a);
                            Console.WriteLine("GainMode[" + a + "] : " + retValue[a]);
                        }
                    }
                }
                else
                {
                    int numCases = (int)nodeMap.GetNode<Integer>("NumCases");
                    IInteger QC = nodeMap.GetNode<IInteger>("QueryCase");
                    IInteger CC = nodeMap.GetNode<IInteger>("CurrentCase");
                    Float QCLL = nodeMap.GetNode<Float>("QueryCaseLowLimit");
                    Float QCHL = nodeMap.GetNode<Float>("QueryCaseHighLimit");
                    BoolNode QCE = nodeMap.GetNode<BoolNode>("QueryCaseEnabled");

                    double lo, hi;
                    long i;
                    bool enabled;
                    retValue = new string[numCases];
                    int index = 0;

                    for (i = 0; i < numCases; i++)
                    {
                        // Set case selector                        
                        QC.Value = i;

                        lo = QCLL.Value;
                        hi = QCHL.Value;
                        enabled = QCE.Value;

                        if (enabled)
                        {
                            string TempRange = string.Format(" {0}°C ~ {1}°C ", (lo - 273.15f).ToString("F0"), (hi - 273.15f).ToString("F0"));

                            //retValue는 온도 범위 저장 
                            retValue[index] = TempRange;
                            index++;

                            // RangeIndexData에는 CurrentCase 값이 저장 - ex) 1, 2, 6, 7 ..등과 같은 
                            RangeIndexData.Add((short)i);
                        }
                    }
                    // 기본 설정은 0번 인덱스에 저장된 온도 범위로 설정
                    CC.Value = RangeIndexData[0];
                }

                for (int j = 0; j < retValue.Length; j++)
                {
                    // 온도 범위를 ComboBoxitem으로 추가 
                    comboRanges.Items.Add(retValue[j]);
                }

                comboRanges.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[TempRangeConf]: {0}", ex.Message);
            }
        }


        /// <summary>
        /// 카메라 온도 범위 설정 변경 시 
        /// </summary>
        private void comboRanges_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ComboBox combobox = sender as ComboBox;
                if (connectcam == null)
                {
                    return;
                }


                if (!CamDevice.Contains("AX5"))
                {
                    INodeMap nodeMap = connectcam.GetNodeMap();

                    IInteger QueryCase = nodeMap.GetNode<IInteger>("QueryCase");
                    IInteger CurrentCase = nodeMap.GetNode<IInteger>("CurrentCase");
                    IBool bQueryCaseEnabled = nodeMap.GetNode<IBool>("QueryCaseEnabled");
                    IFloat dQueryCaseLowLimit = nodeMap.GetNode<IFloat>("QueryCaseLowLimit");
                    IFloat dQueryCaseHighLimit = nodeMap.GetNode<IFloat>("QueryCaseHighLimit");
                    double dLow = 0, dHigh = 0;

                    TempRangeVal = RangeIndexData[combobox.SelectedIndex];
                    QueryCase.Value = TempRangeVal;

                    if (bQueryCaseEnabled.Value == true)
                    {
                        if (QueryCase != null)
                        {
                            dLow = dQueryCaseLowLimit.Value;
                            dHigh = dQueryCaseHighLimit.Value;
                            CurrentCase.Value = RangeIndexData[combobox.SelectedIndex];
                        }
                    }
                }
                else
                {
                    INodeMap nodeMap = connectcam.GetNodeMap();
                    IEnum SGM = nodeMap.GetNode<IEnum>("SensorGainMode");

                    if (SGM != null)
                    {
                        SGM.Value = RangeIndexData[combobox.SelectedIndex];

                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("[TempRangeConf]: {0}", ex.Message);
            }
        }

        #endregion

        #region STEP2 - 01. PALETTE COMBOBOX CONFIGURATION / EVENT
        private void Palette_ComboBox_Initialize()
        {
            try
            {
                // 변경 가능한 Palette List를 ComboBox에 추가 
                
                Palette_ComboBox.Items.Add("Plasma");
                Palette_ComboBox.Items.Add("Iron");
                Palette_ComboBox.Items.Add("Arctic");
                Palette_ComboBox.Items.Add("Jet");
                Palette_ComboBox.Items.Add("Infer");
                Palette_ComboBox.Items.Add("Redgray");
                Palette_ComboBox.Items.Add("Viridis");
                Palette_ComboBox.Items.Add("Magma");
                Palette_ComboBox.Items.Add("Cividis");
                Palette_ComboBox.Items.Add("Coolwarm");
                Palette_ComboBox.Items.Add("Spring");
                Palette_ComboBox.Items.Add("Summer");

                // 기본 설정 팔레트는 Rainbow
                Palette_ComboBox.SelectedIndex =1;
                Current_Palette = "Iron";
                GetRGBfrom16bit(MDSPALETTE.Plasma_palette);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void Palette_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combobox = sender as ComboBox;
            string _palette = combobox.SelectedItem.ToString();

            if (combobox.SelectedItem.ToString() != Current_Palette)
            {
                // Palette 설정 변경  
                Current_Palette = combobox.SelectedItem.ToString();

                // ColorMap 재구성 
                GetColorMap();

            }
        }
        #endregion

        #region STEP2 - 02. MAKE COLORMAP
        //// ColorMap 생성 시 가질 수 있는 최저/최고 온도 값 
        private void GetColorMap()
        {

            switch (Current_Palette)
            {
                case "Rainbow":
                    break;
                case "Plasma":
                    GetRGBfrom16bit(MDSPALETTE.Plasma_palette);
                    break;
                case "Iron":
                    GetRGBfrom16bit(MDSPALETTE.iron_palette);
                    break;
                case "Arctic":
                    GetRGBfrom16bit(MDSPALETTE.Arctic_palette);
                    break;
                case "Jet":
                    GetRGBfrom16bit(MDSPALETTE.Jet_palette);
                    break;
                case "Infer":
                    GetRGBfrom16bit(MDSPALETTE.Infer_palette);
                    break;
                case "Redgray":
                    GetRGBfrom16bit(MDSPALETTE.Redgray_palette);
                    break;
                case "Viridis":
                    GetRGBfrom16bit(MDSPALETTE.Viridis_palette);
                    break;
                case "Magma":
                    GetRGBfrom16bit(MDSPALETTE.Magma_palette);
                    break;
                case "Cividis":
                    GetRGBfrom16bit(MDSPALETTE.Cividis_palette);
                    break;
                case "Coolwarm":
                    GetRGBfrom16bit(MDSPALETTE.Coolwarm_palette);
                    break;
                case "Spring":
                    GetRGBfrom16bit(MDSPALETTE.Spring_palette);
                    break;
                case "Summer":
                    GetRGBfrom16bit(MDSPALETTE.Summer_palette);
                    break;
                default:
                    Current_Palette = "Iron";
                    break;

            }
        }
        private void GetRGBfrom16bit(List<string> pal)
        {
            // 16진수 값을 R, G, B로 분리하여 PaletteColorMap에 저장 

            // 기존 Palette에 관한 ColorMap 값 초기화 
            PaletteColorMap.Clear();

            foreach (string val in pal)
            {
                // # 기호 제거 후 HEX 값을 파싱하여 RGB 컬러로 변환
                string hexWithoutHash = val.TrimStart('#');

                int r = int.Parse(hexWithoutHash.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                int g = int.Parse(hexWithoutHash.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                int b = int.Parse(hexWithoutHash.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

                // Color 객체로 변환하여 리스트에 추가
                PaletteColorMap.Add(Color.FromArgb(r, g, b));
            }

        }
        #endregion

        #region STEP2 - 03. APPLY COLORMAP

       
        private Color GenerateColorPalette(int rVal)
        {
            try
            {
                Color col = new Color();

                if (Current_Palette != "Iron")
                {
                    if (_usecheckbox == false) // 팔레트 구성이 자동이 아닌 경우 
                    {
                        if (PaletteColorMap.Count <= rVal)
                        {
                            col = PaletteColorMap[PaletteColorMap.Count - 1];

                        }
                        else
                        {
                            col = PaletteColorMap[rVal];
                        }

                    }
                    else
                    {
                        if (PaletteColorMap.Count > rVal)
                        {

                            col = PaletteColorMap[rVal];

                        }
                    }
                }
                else
                {
                    // Rainbow Palette
                    if (rVal < step) //Blue to Cyan
                    {
                        col = Color.FromArgb(0, rVal * 4, 255);
                    }
                    else if (rVal < step * 2) //Cyan to Green
                    {
                        col = Color.FromArgb(0, 255, 255 - (rVal - step) * 4);
                    }
                    else if (rVal < step * 3) //Green to Yellow
                    {
                        col = Color.FromArgb((rVal - step * 2) * 4, 255, 0);
                    }
                    else //Yellow to Red
                    {
                        col = Color.FromArgb(255, 255 - (rVal - step * 3) * 4, 0);
                    }
                }

                return col;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("rVal : "+ rVal);

                Color col = new Color();
                col = Color.FromArgb(255, 255, 255); 

                return col; 
                 
            }
            
        }

        private Color GenerateColorGreyPalette(int rVal)
        {
            try
            {
                Color col = new Color();
                col = Color.FromArgb(rVal, rVal, rVal); 
        
                return col;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("rVal : " + rVal);

                Color col = new Color();
                col = Color.FromArgb(255, 255, 255);

                return col;

            }

        }
       
        #endregion


        #region STEP3 - 02. TEMPERATURE CONVERTER
        // 섭씨 온도를 uint 값으로 변환하여 반환 
        private int TempChange(int temp)
        {
            try
            {
                double _temp = ((temp + 273.15) / mConvertOffsetVal);

                return (int)_temp;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 0; 
            }
        }


        #endregion







        #region STEP6 -01. IMAGE PROCESS

        #endregion
        //private void StartImageProcessingThread()
        //{
        //    Task.Run(() =>
        //    {
        //        while (isProcessing)
        //        {
        //            try
        //            {

        //                if (frameQueue.Count !=0 && frameQueue.TryDequeue(out var frame))
        //                {
        //                    Bitmap frameCopy;

        //                    try
        //                    {
        //                        frameCopy = new Bitmap(frame);
        //                    }
        //                    catch (InvalidOperationException ex)
        //                    {
        //                        // 카메라 연결 문제 또는 프레임 상태 문제 처리
        //                        // 카메라 NUC 조정 or 측정 온도 변경 등 잠시 프레임이 전달되지 않는 경우를 위한 예외처리 
        //                        Console.WriteLine($"Invalid operation while creating bitmap: {ex.Message}");
        //                        continue;
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        // 기타 예외 처리
        //                        Console.WriteLine($"Error creating bitmap: {ex.Message}");
        //                        continue;
        //                    }

        //                    var processedFrame = ProcessFrame(frameCopy);
        //                    Ther_Image.Dispatcher.Invoke(() => DisplayFrame(processedFrame));
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"An error occurred in the processing thread: {ex.Message}");
        //            }
        //        }
        //    });
        //}
        private object bitmapLock = new object();
        private void StartImageProcessingThread()
        {
            Task.Run(() =>
            {
                while (isProcessing)
                {
                    try
                    {
                        Bitmap frame = null;

                        lock (frameQueue)
                        {
                            if (frameQueue.Count != 0 && frameQueue.TryDequeue(out frame))
                            {
                                Console.WriteLine("Frame dequeued successfully");
                            }
                        }

                        if (frame != null)
                        {
                            Bitmap frameCopy = null;
                            try
                            {
                                // Bitmap 객체를 잠금
                                lock (bitmapLock)
                                {
                                    frameCopy = new Bitmap(frame);
                                }
                            }
                            catch (InvalidOperationException ex)
                            {
                                Console.WriteLine($"Invalid operation while creating bitmap: {ex.Message}");
                                continue;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error creating bitmap: {ex.Message}");
                                continue;
                            }

                            try
                            {
                                var processedFrame = ProcessFrame(frameCopy);
                                Ther_Image.Dispatcher.Invoke(() => DisplayFrame(processedFrame));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing frame: {ex.Message}");
                            }
                            finally
                            {
                                frameCopy?.Dispose();
                                frame?.Dispose();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred in the processing thread: {ex.Message}");
                    }
                }
            });
        }

        private Bitmap ProcessFrame(Bitmap frame)
        {
            // Bitmap의 복사본 생성
            Bitmap frameCopy = new Bitmap(frame);

            try
            {
                // 영상 처리 코드 수행
                switch (selectedItem)
                {
                    case "None": break;
                    case "Thresholding": // 이진화
                        frameCopy = processing.Thresholding(frameCopy);
                        break;
                    case "Grayscale": // 그레이스케일
                        frameCopy = processing.Grayscale(frameCopy);
                        break;
                    case "EdgeDetection": // 그레이스케일
                        frameCopy = processing.Edgedetection(frameCopy);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return frameCopy;
        }


        private void DisplayFrame(Bitmap frame)
        {
            try
            {
                IntPtr hBitmap = frame.GetHbitmap();
                BitmapSource bmpSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                if (bmpSrc.CanFreeze)
                    bmpSrc.Freeze();

                //this.backgroundImageBrush.ImageSource = bmpSrc;
               this.backgroundProcessImageBrush.ImageSource = bmpSrc;

                DeleteObject(hBitmap);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        private void processsldInitialize()
        {
            try
            {

                //threshold 값 지정 
                processingval.Maximum = 255;
                processingval.Minimum = 0;
                processingval.Value = 100;

                // 영상처리를 선택하지 않은 상태로 초기화 - threshold 값 조정 UI 숨기기
                selectedItem = "None"; 
                ProcessGrid.Visibility = System.Windows.Visibility.Collapsed;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex); 
            }
         
        }
        
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int sliderValue = (int)e.NewValue; // Slider 값은 double 타입이므로 int로 변환
            processing.thresholdvalue = sliderValue; 
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton btn = (RadioButton)sender;
            selectedItem = btn.Name;
        }
    }
}
