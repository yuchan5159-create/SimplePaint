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

            cmbColor.SelectedIndexChanged += cmbColor_SelectedIndexChanged; 
            cmbColor.SelectedIndex = 0;  // 기본값: Black

            // 선두께트랙바이벤트연결
            trbLineWidth.Minimum= 1;    // 최소값
            trbLineWidth.Maximum= 10;   // 최대값
            trbLineWidth.Value= 2;trbLineWidth.ValueChanged+= trbLineWidth_ValueChanged;


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
            startPoint= e.Location;      // 시작점저장
                                         }
        private void PicCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDrawing) return;       // 그림그리기와상관없는마우스움직임은무시
            endPoint= e.Location;        // 현재위치갱신
            // picCanvas를다시그려라(Paint 이벤트를발생시킨다)
            picCanvas.Invalidate();       // 화면다시그리기(미리보기)
        }
        private void PicCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isDrawing) return;     // 그림그리기와상관없는마우스움직임은무시
            isDrawing = false;          // 드래그종료
            endPoint = e.Location;
             using (Pen pen= new Pen(currentColor, currentLineWidth))
            {
                DrawShape(canvasGraphics, pen, startPoint, endPoint);
            }
            picCanvas.Invalidate();     // 다시그려서결과반영, Paint 이벤트발생
                                    }
        private void PicCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (!isDrawing) return;
            // 점선펜(미리보기용)
            using (Pen previewPen = new Pen(currentColor, currentLineWidth))
            {
                previewPen.DashStyle = DashStyle.Dash;
                DrawShape(e.Graphics, previewPen, startPoint, endPoint);
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
