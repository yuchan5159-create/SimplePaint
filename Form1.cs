namespace SimplePaint
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Drawing.Printing;
    using System.Windows.Forms;

    public partial class Form1 : Form
    {

        enum  ToolType{ Line, Rectangle, Circle }  // 사용할도형타입
        private Bitmap canvasBitmap;          // 실제그림이저장되는비트맵
        private Graphics canvasGraphics;      // 비트맵위에그리기위한객체
        private bool isDrawing= false;       // 현재드래그중인지여부
        private Point startPoint;             // 드래그시작점
        private Point endPoint;               // 드래그끝점
        private ToolType currentTool= ToolType.Line;  // 현재선택된도형
        private Color currentColor= Color.Black;      // 현재색상
        private int currentLineWidth= 2;              // 현재선두께
        private int zoomPercent = 100;


        public Form1()
        {
            InitializeComponent();
            // 캔버스초기화
            canvasBitmap= new Bitmap(picCanvas.Width, picCanvas.Height);
            canvasGraphics= Graphics.FromImage(canvasBitmap);
            canvasGraphics.Clear(Color.White);   // 캔버스를흰색으로초기화
            
            picCanvas.Image = canvasBitmap;   // 그린그림을화면(PictureBox)에표시

            // 마우스이벤트연결
            picCanvas.MouseDown+= PicCanvas_MouseDown;
            picCanvas.MouseMove+= PicCanvas_MouseMove;
            picCanvas.MouseUp+= PicCanvas_MouseUp;

            // picCanvas가다시그려질때PicCanvas_Paint함수를실행하도록연결
            picCanvas.Paint+= PicCanvas_Paint;

            // 도형선택버튼이벤트연결
            btnLine.Click+= btnLine_Click;
            btnRectangle.Click+= btnRectangle_Click;
            btnCircle.Click+= btnCircle_Click;
            btnSaveFile.Click += btnSaveFile_Click;
            btnOpenFile.Click += btnOpenFile_Click;
            btnZoomIn.Click += btnZoomIn_Click;
            btnZoomOut.Click += btnZoomOut_Click;
            panelCanvas.MouseWheel += PanelCanvas_MouseWheel;
            picCanvas.MouseWheel += PanelCanvas_MouseWheel;
            picCanvas.MouseEnter += (s, e) => picCanvas.Focus();

            cmbColor.SelectedIndexChanged += cmbColor_SelectedIndexChanged; 
            cmbColor.SelectedIndex = 0;  // 기본값: Black

            // 선두께트랙바이벤트연결
            trbLineWidth.Minimum= 1;    // 최소값
            trbLineWidth.Maximum= 10;   // 최대값
            trbLineWidth.Value= 2;trbLineWidth.ValueChanged+= trbLineWidth_ValueChanged;


      }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*";
                if (ofd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    Image img = Image.FromFile(ofd.FileName);
                    // replace canvas bitmap with loaded image
                    canvasBitmap?.Dispose();
                    canvasBitmap = new Bitmap(img.Width, img.Height);
                    canvasGraphics = Graphics.FromImage(canvasBitmap);
                    canvasGraphics.Clear(Color.White);
                    canvasGraphics.DrawImage(img, 0, 0, img.Width, img.Height);
                    img.Dispose();

                    // set picture box size to image size * scale
                    ApplyZoom();
                    // reset scroll to top-left
                    panelCanvas.AutoScrollPosition = new Point(0, 0);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"이미지 열기 중 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnZoomIn_Click(object sender, EventArgs e)
        {
            zoomPercent = Math.Min(400, zoomPercent + 10);
            ApplyZoom();
        }

        private void btnZoomOut_Click(object sender, EventArgs e)
        {
            zoomPercent = Math.Max(10, zoomPercent - 10);
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (canvasBitmap == null) return;
            int w = canvasBitmap.Width * zoomPercent / 100;
            int h = canvasBitmap.Height * zoomPercent / 100;
            picCanvas.Size = new Size(w, h);
            // replace displayed image with scaled copy
            var old = picCanvas.Image;
            picCanvas.Image = new Bitmap(canvasBitmap, w, h);
            if (old != null && old != canvasBitmap) old.Dispose();
            // ensure panel scroll position remains valid
            panelCanvas.AutoScrollMinSize = picCanvas.Size;
        }

        private void PanelCanvas_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (canvasBitmap == null) return;
            // zoom in/out by wheel delta (120 per notch)
            int delta = e.Delta;
            int oldZoom = zoomPercent;
            if (delta > 0) zoomPercent = Math.Min(400, zoomPercent + 10);
            else zoomPercent = Math.Max(10, zoomPercent - 10);

            // calculate mouse position relative to image before and after zoom
            // point in image coordinates
            Point mouseOnControl = picCanvas.PointToClient(Cursor.Position);
            Point imagePointBefore = ControlToImagePoint(mouseOnControl);

            ApplyZoom();

            // new control point for same image point
            Point newControl = ImageToControlPoint(imagePointBefore);

            // adjust scroll so that the imagePointBefore is at same control location
            // set AutoScrollPosition to negative of desired scroll offset
            int scrollX = Math.Max(0, newControl.X - mouseOnControl.X);
            int scrollY = Math.Max(0, newControl.Y - mouseOnControl.Y);
            panelCanvas.AutoScrollPosition = new Point(scrollX, scrollY);
        }

        private void btnSaveFile_Click(object sender, EventArgs e)
        {
            if (canvasBitmap == null)
            {
                MessageBox.Show("저장할 그림이 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG (*.png)|*.png|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|Bitmap (*.bmp)|*.bmp";
                sfd.DefaultExt = "png";
                sfd.AddExtension = true;

                if (sfd.ShowDialog() != DialogResult.OK) return;

                ImageFormat fmt = ImageFormat.Png;
                string ext = Path.GetExtension(sfd.FileName).ToLowerInvariant();
                switch (ext)
                {
                    case ".jpg":
                    case ".jpeg":
                        fmt = ImageFormat.Jpeg; break;
                    case ".bmp":
                        fmt = ImageFormat.Bmp; break;
                    default:
                        if (sfd.FilterIndex == 2) fmt = ImageFormat.Jpeg;
                        else if (sfd.FilterIndex == 3) fmt = ImageFormat.Bmp;
                        else fmt = ImageFormat.Png;
                        break;
                }

                try
                {
                    canvasBitmap.Save(sfd.FileName, fmt);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"파일 저장 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void PicCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            isDrawing = true;             // 드래그시작
            startPoint = ControlToImagePoint(e.Location); // 시작점저장 (이미지 좌표)
                                         }
        private void PicCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDrawing) return;       // 그림그리기와상관없는마우스움직임은무시
            endPoint = ControlToImagePoint(e.Location);        // 현재위치갱신 (이미지 좌표)
            // picCanvas를다시그려라(Paint 이벤트를발생시킨다)
            picCanvas.Invalidate();       // 화면다시그리기(미리보기)
        }
        private void PicCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isDrawing) return;     // 그림그리기와상관없는마우스움직임은무시
            isDrawing = false;          // 드래그종료
            endPoint = ControlToImagePoint(e.Location);
             using (Pen pen= new Pen(currentColor, currentLineWidth))
            {
                DrawShape(canvasGraphics, pen, startPoint, endPoint);
            }
            // 갱신된 비트맵을 화면에 반영
            ApplyZoom();
            picCanvas.Invalidate();     // 다시그려서결과반영, Paint 이벤트발생
                                    }
        private void PicCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (!isDrawing) return;
            // 점선펜(미리보기용) - 이미지 좌표를 컨트롤(디스플레이) 좌표로 변환
            Point p1 = ImageToControlPoint(startPoint);
            Point p2 = ImageToControlPoint(endPoint);
            using (Pen previewPen = new Pen(currentColor, Math.Max(1, currentLineWidth * zoomPercent / 100)))
            {
                previewPen.DashStyle = DashStyle.Dash;
                DrawShape(e.Graphics, previewPen, p1, p2);
            }
        }
        private void DrawShape(Graphics g, Pen pen, Point p1, Point p2)
        {
         Rectangle rect = GetRectangle(p1, p2);
         switch (currentTool) 
            {
                case ToolType.Line: 
                    g.DrawLine(pen, p1, p2);
                    break; 
                case ToolType.Rectangle: 
                    g.DrawRectangle(pen, rect); break; 
                case ToolType.Circle: 
                    g.DrawEllipse(pen, rect); break;
            }
        }

        private Rectangle GetRectangle(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int width = Math.Abs(p1.X - p2.X);
            int height = Math.Abs(p1.Y - p2.Y);
            return new Rectangle(x, y, width, height);
        }

        private Point ControlToImagePoint(Point controlPoint)
        {
            // e.Location on picCanvas is already in control (picturebox) coordinates.
            // Convert by reversing the zoom.
            int x = controlPoint.X * 100 / Math.Max(1, zoomPercent);
            int y = controlPoint.Y * 100 / Math.Max(1, zoomPercent);
            return new Point(x, y);
        }

        private Point ImageToControlPoint(Point imagePoint)
        {
            int x = imagePoint.X * zoomPercent / 100;
            int y = imagePoint.Y * zoomPercent / 100;
            return new Point(x, y);
        }


        private void trbLineWidth_ValueChanged(object sender, EventArgs e) 
        {
            currentLineWidth = trbLineWidth.Value;
        }



        private void btnLine_Click(object sender, EventArgs e) { currentTool = ToolType.Line; }
        private void btnRectangle_Click(object sender, EventArgs e) { currentTool = ToolType.Rectangle; }
        private void btnCircle_Click(object sender, EventArgs e) { currentTool = ToolType.Circle; 
        }

        private void cmbColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cmbColor.SelectedIndex)
            {
                case 0: // Black 검정
                    currentColor = Color.Black;
                    break;
                case 1: // Red 빨강
                    currentColor = Color.Red;
                    break;
                case 2: // Blue 파랑
                    currentColor = Color.Blue;
                    break;
                case 3: // Green 녹색
                    currentColor = Color.Green;
                    break;
                default:
                    currentColor = Color.Black;
                    break;
            }
        }


        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }
    }
}
