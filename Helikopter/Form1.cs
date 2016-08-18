using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Media;

namespace Helikopter
{
    public partial class Form1 : Form
    {
        public enum Hareket { Saga, Sola, Sabit }
        private Bitmap buffer;
        private Bitmap zemin;
        private Point start_corner;
        private TextureBrush helikopter_texture, arazi_texture, ufo_texture, ufak_ufo_texture, 
            roket_texture, patlama_texture, buyuk_patlama_texture;
        private RectangleF helikopter_rect, arazi_rect;
        private List<RectangleF> ufolar_rect, roketler_rect, patlamalar_rect, buyuk_patlamalar_rect;
        private List<int> patlamalar_tile, buyuk_patlamalar_tile, ufo_life;
        private int helikopter_tile, ufo_tile, roket_tile;
        private Hareket helikopter_hareket;
        private float konum, ufo_hizi;
        private int puan, kacirilan_ufo, atis_izni;
        private Font yazi_tipi;
        private bool game_over, atis_serbest;
        private SoundPlayer roket_sesi, helikopter_sesi, patlama_sesi, infilak_sesi;

        public Form1()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.Black;

            yazi_tipi = new System.Drawing.Font("Arial Black", 24f);
            ufo_hizi = 1f; puan = 0; kacirilan_ufo = 0;
            AraziyiHazirla(); Sesler(); Rectangles(); Textures();
            game_over = false;
            ufolar_rect = new List<RectangleF>();
            ufo_life = new List<int>();
            roketler_rect = new List<RectangleF>();
            buyuk_patlamalar_rect = new List<RectangleF>();
            buyuk_patlamalar_tile = new List<int>();
            patlamalar_rect = new List<RectangleF>();
            patlamalar_tile = new List<int>();
            Timer gameTimer = new Timer() { Enabled = true, Interval = 15 };
            gameTimer.Tick += gameTimer_Tick;
            start_corner = new Point((Screen.PrimaryScreen.Bounds.Width - 720) / 2, (Screen.PrimaryScreen.Bounds.Height - 720) / 2);
        }

        void gameTimer_Tick(object sender, EventArgs e)
        {
            if (!game_over) Hareketler();
            Draw();
        }

        private void Sesler()
        {
            helikopter_sesi = new SoundPlayer(Properties.Resources.helikopter_ses);
            roket_sesi = new SoundPlayer(Properties.Resources.roket_ses);
            patlama_sesi = new SoundPlayer(Properties.Resources.boom_ses);
            infilak_sesi = new SoundPlayer(Properties.Resources.booom_ses);
            helikopter_sesi.PlayLooping();
        }

        private void AraziyiHazirla()
        {
            zemin = new Bitmap(Properties.Resources.arazi.Width, Properties.Resources.arazi.Height * 2);
            Graphics graphics_zemin = Graphics.FromImage(zemin);
            Bitmap ust_parca = Properties.Resources.arazi, alt_parca = Properties.Resources.arazi;
            ust_parca.RotateFlip(RotateFlipType.Rotate180FlipY);
            graphics_zemin.DrawImage(ust_parca, new Point(0, 0));
            graphics_zemin.DrawImage(alt_parca, new Point(0, ust_parca.Height));
        }

        private void Rectangles()
        {
            arazi_rect = new RectangleF(0f, 0f, 720f, 720f);
            helikopter_rect = new RectangleF(300f, 500f, 200f, 200f); helikopter_hareket = Hareket.Sabit;
        }

        private void Textures()
        {
            helikopter_texture = new TextureBrush(Properties.Resources.helikopter);
            ufo_texture = new TextureBrush(Properties.Resources.ufo);
            ufak_ufo_texture = new TextureBrush(Properties.Resources.ufak_ufo);
            roket_texture = new TextureBrush(Properties.Resources.roket);
            patlama_texture = new TextureBrush(Properties.Resources.patlama);
            buyuk_patlama_texture = new TextureBrush(Properties.Resources.buyuk_patlama);
            arazi_texture = new TextureBrush(zemin);
        }

        private void CreateBackupBuffer() { if (buffer != null) buffer.Dispose(); buffer = new Bitmap(720, 720); }

        private void Form1_Load(object sender, EventArgs e)
        {
            CreateBackupBuffer();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            CreateBackupBuffer();
        }

        private void Hareketler()
        {
            konum += 5f; if (konum % 1200f == 0) { UfoGetir(); ufo_hizi += 0.5f; }
            if (atis_izni > 0) { atis_izni--; } else { atis_serbest = true; }
            int i = 0, c = ufolar_rect.Count;
            while (i < c) 
            {
                ufolar_rect[i] = new RectangleF(new PointF(ufolar_rect[i].X, ufolar_rect[i].Y + ufo_hizi), ufolar_rect[i].Size);
                if (ufolar_rect[i].Y > 960f) { ufolar_rect.RemoveAt(i); ufo_life.RemoveAt(i); c--; kacirilan_ufo++; game_over = kacirilan_ufo > 3; }
                i++;
            }
            i = 0; c = roketler_rect.Count;
            while (i < c)
            {
                roketler_rect[i] = new RectangleF(new PointF(roketler_rect[i].X, roketler_rect[i].Y - 8f), roketler_rect[i].Size);
                if (roketler_rect[i].Y < -120f) { roketler_rect.RemoveAt(i); c--; }
                i++;
            }
            i = 0; c = patlamalar_rect.Count;
            while (i < c)
            {
                patlamalar_rect[i] = new RectangleF(patlamalar_rect[i].X, patlamalar_rect[i].Y + 3f, 200f, 200f);
                if (patlamalar_rect[i].Y < -120f) { patlamalar_rect.RemoveAt(i); patlamalar_tile.RemoveAt(i); c--; }
                i++;
            }
            i = 0; c = buyuk_patlamalar_rect.Count;
            while (i < c)
            {
                buyuk_patlamalar_rect[i] = new RectangleF(buyuk_patlamalar_rect[i].X, buyuk_patlamalar_rect[i].Y + 3f, 400f, 400f);
                if (buyuk_patlamalar_rect[i].Y < -120f) { buyuk_patlamalar_rect.RemoveAt(i); buyuk_patlamalar_tile.RemoveAt(i); c--; }
                i++;
            }
            if (helikopter_hareket == Hareket.Saga) { HelikopterSaga(); }
            else if (helikopter_hareket == Hareket.Sola) { HelikopterSola(); }
            Carpisma();
        }

        private void HelikopterSola() { if (helikopter_rect.Left > 10f) helikopter_rect.X -= 8f; }
        private void HelikopterSaga() { if (helikopter_rect.Right < 710f) helikopter_rect.X += 8f; }

        private void UfoGetir()
        {
            Random rnd = new Random();
            ufolar_rect.Add(new RectangleF((float)rnd.Next(40, 530), -200f, 150f, 150f));
            ufo_life.Add(4);
        }

        private void Atesle()
        {
            if (atis_serbest)
            {
                roket_sesi.Play();
                roketler_rect.Add(new RectangleF(helikopter_rect.X + 84f, helikopter_rect.Y + 20f, 32f, 96f));
                roket_tile = 0; atis_izni = 10; atis_serbest = false;
            }
        }

        private void Carpisma()
        {
            int i = 0, c = roketler_rect.Count, j = 0, d = ufolar_rect.Count;
            while (i < c)
            {
                float roket_ucu_x = roketler_rect[i].X + 16f, roket_ucu_y = roketler_rect[i].Y;
                j = 0;
                while (j < d)
                {
                    float ufo_merkez_x = ufolar_rect[j].X + 75f, ufo_merkez_y = ufolar_rect[j].Y + 75f;
                    if ((roket_ucu_x - ufo_merkez_x) * (roket_ucu_x - ufo_merkez_x) +
                        (roket_ucu_y - ufo_merkez_y) * (roket_ucu_y - ufo_merkez_y) < 75f * 75f)
                    {
                        patlama_sesi.Play();
                        patlamalar_rect.Add(new RectangleF(roket_ucu_x - 100f, roket_ucu_y - 100f, 200f, 200f));
                        patlamalar_tile.Add(0);
                        roketler_rect[i] = new RectangleF(roket_ucu_x, -200f, 32f, 96f);
                        ufo_life[j]--;
                        if (ufo_life[j] < 1) 
                        {
                            infilak_sesi.Play();
                            buyuk_patlamalar_rect.Add(new RectangleF(ufo_merkez_x - 200f, ufo_merkez_y - 200f, 400f, 400f));
                            buyuk_patlamalar_tile.Add(0);
                            ufolar_rect.RemoveAt(j); ufo_life.RemoveAt(j); d--; puan += 50;
                        }
                    }
                    j++;
                }
                i++;
            }
        }

        private void Draw()
        {
            if (buffer != null)
            {
                Graphics graphics = Graphics.FromImage(buffer);
                graphics.Clear(Color.White);
                arazi_texture.ResetTransform();
                arazi_texture.TranslateTransform(0f, konum);
                graphics.FillRectangle(arazi_texture, arazi_rect);
                int i = 0, c = roketler_rect.Count, tile = 0;
                while (i < c)
                {
                    roket_texture.ResetTransform();
                    roket_texture.TranslateTransform(-(roket_tile % 9) * 32 + roketler_rect[i].X,
                        -(roket_tile - (roket_tile % 9)) * 32 / 3 + roketler_rect[i].Y);
                    graphics.FillRectangle(roket_texture, roketler_rect[i]);
                    i++;
                }
                roket_tile++; if (roket_tile > 44) roket_tile = 0;

                i = 0; c = ufolar_rect.Count;
                while (i < c)
                {
                    ufo_texture.ResetTransform();
                    ufo_texture.TranslateTransform(-(ufo_tile % 6) * 150 + ufolar_rect[i].X,
                        -(ufo_tile - (ufo_tile % 6)) * 25 + ufolar_rect[i].Y);
                    graphics.FillRectangle(ufo_texture, ufolar_rect[i]);
                    i++;
                }
                ufo_tile++; if (ufo_tile > 35) ufo_tile = 0;

                helikopter_texture.ResetTransform();
                helikopter_texture.TranslateTransform(-(helikopter_tile % 6) * 200 + helikopter_rect.X, 
                    -(helikopter_tile - (helikopter_tile % 6)) * 100 / 3 + helikopter_rect.Y);
                graphics.FillRectangle(helikopter_texture, helikopter_rect);
                helikopter_tile++; if (helikopter_tile > 35) helikopter_tile = 0;

                i = 0; c = patlamalar_rect.Count;
                while (i < c)
                {
                    tile = patlamalar_tile[i];
                    patlama_texture.ResetTransform();
                    patlama_texture.TranslateTransform(-(tile % 9) * 200 + patlamalar_rect[i].X,
                        -(tile - (tile % 9)) * 200 / 9 + patlamalar_rect[i].Y);
                    graphics.FillRectangle(patlama_texture, patlamalar_rect[i]);
                    patlamalar_tile[i]++; if (patlamalar_tile[i] > 80) { patlamalar_tile[i] = 80; } 
                    i++;
                }

                i = 0; c = buyuk_patlamalar_rect.Count;
                while (i < c)
                {
                    tile = buyuk_patlamalar_tile[i];
                    buyuk_patlama_texture.ResetTransform();
                    buyuk_patlama_texture.TranslateTransform(-(tile % 9) * 400 + buyuk_patlamalar_rect[i].X,
                        -(tile - (tile % 9)) * 400 / 9 + buyuk_patlamalar_rect[i].Y);
                    graphics.FillRectangle(buyuk_patlama_texture, buyuk_patlamalar_rect[i]);
                    buyuk_patlamalar_tile[i]++; if (buyuk_patlamalar_tile[i] > 80) { buyuk_patlamalar_tile[i] = 80; }
                    i++;
                }

                graphics.DrawString(puan.ToString(), yazi_tipi, Brushes.Red, 10f, 10f);

                i = 0;
                while (i < kacirilan_ufo)
                {
                    ufak_ufo_texture.ResetTransform();
                    ufak_ufo_texture.TranslateTransform(10f, 60f * (i + 1));
                    graphics.FillRectangle(ufak_ufo_texture, new RectangleF(10f, 60f * (i + 1), 50f, 50f));
                    i++;
                }

                if (game_over) graphics.DrawString("Kaybettiniz", yazi_tipi, Brushes.Red, 250f, 300f);

                Invalidate();
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (buffer != null) { e.Graphics.DrawImageUnscaled(buffer, start_corner); }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Environment.Exit(0);
            else if (e.KeyCode == Keys.Left) { helikopter_hareket = Hareket.Sola; }
            else if (e.KeyCode == Keys.Right) { helikopter_hareket = Hareket.Saga; }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            helikopter_hareket = Hareket.Sabit;
            if (e.KeyCode == Keys.Space) Atesle();
        }
    }
}
