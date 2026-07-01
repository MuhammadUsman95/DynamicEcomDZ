using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ImageShrinkUtility
{
    public partial class Form1 : Form
    {
        // ─── State ───────────────────────────────────────────────
        private List<string> imagePaths = new List<string>();
        private int currentIndex = -1;
        private Image originalImage;
        private long originalFileSize = 0;

        // ─── Target Compression ──────────────────────────────────
        private const int TARGET_KB = 10;      // Final image approx 10 KB
        private const int MIN_WIDTH = 120;     // Minimum width limit
        private const int MIN_HEIGHT = 120;    // Minimum height limit

        // ─── Colors (Dark Theme) ─────────────────────────────────
        private readonly Color BG_DARK = Color.FromArgb(18, 18, 24);
        private readonly Color BG_PANEL = Color.FromArgb(28, 28, 36);
        private readonly Color BG_CARD = Color.FromArgb(38, 38, 50);
        private readonly Color ACCENT = Color.FromArgb(99, 102, 241);
        private readonly Color TEXT_PRI = Color.FromArgb(240, 240, 255);
        private readonly Color TEXT_SEC = Color.FromArgb(140, 140, 165);
        private readonly Color SUCCESS = Color.FromArgb(52, 211, 153);
        private readonly Color BORDER = Color.FromArgb(55, 55, 72);

        // ─── Controls ────────────────────────────────────────────
        private Panel pnlTop, pnlLeft, pnlMain, pnlBottom;
        private ListBox lstImages;
        private Label lblTitle, lblImageCount, lblOrigSize, lblCompSize,
                      lblSaved, lblQualityVal, lblOrigLabel, lblCompLabel;
        private TrackBar trackQuality;
        private PictureBox picOriginal, picCompressed;
        private Button btnAdd, btnRemove, btnSaveAll, btnSaveCurrent, btnClear;
        private Button btnBrowsePath;
        private TextBox txtSavePath;
        private ProgressBar pbBatch;

        public Form1()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "Image Shrink Utility";
            this.Size = new Size(1220, 780);
            this.MinimumSize = new Size(1100, 700);
            this.BackColor = BG_DARK;
            this.ForeColor = TEXT_PRI;
            this.Font = new Font("Segoe UI", 9f);
            this.StartPosition = FormStartPosition.CenterScreen;

            BuildTopBar();
            BuildLeftPanel();
            BuildMainArea();
            BuildBottomBar();
        }

        private void BuildTopBar()
        {
            pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = BG_PANEL
            };

            pnlTop.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(BORDER, 1),
                    0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
            };

            lblTitle = new Label
            {
                Text = "✦  IMAGE SHRINK",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = TEXT_PRI,
                Location = new Point(22, 14),
                AutoSize = true
            };

            var lblQualityHead = new Label
            {
                Text = "QUALITY",
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = TEXT_SEC,
                Location = new Point(370, 12),
                AutoSize = true
            };

            lblQualityVal = new Label
            {
                Text = "40%",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = ACCENT,
                Location = new Point(368, 24),
                AutoSize = true
            };

            trackQuality = new TrackBar
            {
                Minimum = 5,
                Maximum = 100,
                Value = 40,
                TickFrequency = 5,
                Location = new Point(420, 12),
                Width = 340,
                Height = 36,
                BackColor = BG_PANEL
            };
            trackQuality.Scroll += TrackQuality_Scroll;

            var lMin = new Label
            {
                Text = "Low",
                ForeColor = TEXT_SEC,
                Font = new Font("Segoe UI", 7f),
                Location = new Point(420, 48),
                AutoSize = true
            };

            var lMax = new Label
            {
                Text = "High",
                ForeColor = TEXT_SEC,
                Font = new Font("Segoe UI", 7f),
                Location = new Point(736, 48),
                AutoSize = true
            };

            pnlTop.Controls.AddRange(new Control[]
            {
                lblTitle, lblQualityHead, lblQualityVal, trackQuality, lMin, lMax
            });

            this.Controls.Add(pnlTop);
        }

        private void BuildLeftPanel()
        {
            pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = BG_PANEL,
                Padding = new Padding(12)
            };

            pnlLeft.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(BORDER, 1),
                    pnlLeft.Width - 1, 0, pnlLeft.Width - 1, pnlLeft.Height);
            };

            var lblFiles = new Label
            {
                Text = "FILES",
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = TEXT_SEC,
                Location = new Point(12, 12),
                AutoSize = true
            };

            lblImageCount = new Label
            {
                Text = "0 images",
                Font = new Font("Segoe UI", 8f),
                ForeColor = ACCENT,
                Location = new Point(12, 28),
                AutoSize = true
            };

            lstImages = new ListBox
            {
                Location = new Point(12, 50),
                Size = new Size(236, 440),
                BackColor = BG_CARD,
                ForeColor = TEXT_PRI,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f),
                ItemHeight = 26,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            lstImages.DrawItem += LstImages_DrawItem;
            lstImages.SelectedIndexChanged += LstImages_SelectedIndexChanged;

            btnAdd = MakeButton("＋  Add Images", ACCENT, new Point(12, 500));
            btnAdd.Width = 236;
            btnAdd.Click += BtnAdd_Click;

            btnRemove = MakeButton("✕  Remove Selected", BG_CARD, new Point(12, 540));
            btnRemove.Width = 115;
            btnRemove.ForeColor = Color.FromArgb(248, 113, 113);
            btnRemove.Click += BtnRemove_Click;

            btnClear = MakeButton("Clear All", BG_CARD, new Point(133, 540));
            btnClear.Width = 115;
            btnClear.ForeColor = TEXT_SEC;
            btnClear.Click += BtnClear_Click;

            pnlLeft.Controls.AddRange(new Control[]
            {
                lblFiles, lblImageCount, lstImages, btnAdd, btnRemove, btnClear
            });

            this.Controls.Add(pnlLeft);
        }

        private void BuildMainArea()
        {
            pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BG_DARK,
                Padding = new Padding(16)
            };

            var cardOrig = new Panel
            {
                Location = new Point(16, 16),
                Size = new Size(462, 530),
                BackColor = BG_CARD
            };
            RoundCorners(cardOrig);

            lblOrigLabel = new Label
            {
                Text = "ORIGINAL",
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = TEXT_SEC,
                Location = new Point(12, 10),
                AutoSize = true
            };

            lblOrigSize = new Label
            {
                Text = "--",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = TEXT_PRI,
                Location = new Point(12, 26),
                AutoSize = true
            };

            picOriginal = new PictureBox
            {
                Location = new Point(8, 54),
                Size = new Size(446, 468),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = BG_DARK
            };

            cardOrig.Controls.AddRange(new Control[]
            {
                lblOrigLabel, lblOrigSize, picOriginal
            });

            var cardComp = new Panel
            {
                Location = new Point(494, 16),
                Size = new Size(462, 530),
                BackColor = BG_CARD
            };
            RoundCorners(cardComp);

            lblCompLabel = new Label
            {
                Text = "COMPRESSED",
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = TEXT_SEC,
                Location = new Point(12, 10),
                AutoSize = true
            };

            lblCompSize = new Label
            {
                Text = "--",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = SUCCESS,
                Location = new Point(12, 26),
                AutoSize = true
            };

            lblSaved = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = SUCCESS,
                Location = new Point(220, 29),
                AutoSize = true
            };

            picCompressed = new PictureBox
            {
                Location = new Point(8, 54),
                Size = new Size(446, 468),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = BG_DARK
            };

            cardComp.Controls.AddRange(new Control[]
            {
                lblCompLabel, lblCompSize, lblSaved, picCompressed
            });

            pnlMain.Controls.Add(cardOrig);
            pnlMain.Controls.Add(cardComp);

            pnlMain.Resize += (s, e) =>
            {
                int w = (pnlMain.ClientSize.Width - 48) / 2;
                int h = pnlMain.ClientSize.Height - 32;

                cardOrig.Size = new Size(w, h);
                cardComp.Location = new Point(w + 32, 16);
                cardComp.Size = new Size(w, h);

                picOriginal.Size = new Size(w - 16, h - 62);
                picCompressed.Size = new Size(w - 16, h - 62);
            };

            this.Controls.Add(pnlMain);
        }

        private void BuildBottomBar()
        {
            pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                BackColor = BG_PANEL
            };

            pnlBottom.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(BORDER, 1), 0, 0, pnlBottom.Width, 0);
            };

            var lblPathHead = new Label
            {
                Text = "SAVE TO",
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = TEXT_SEC,
                Location = new Point(16, 10),
                AutoSize = true
            };

            txtSavePath = new TextBox
            {
                Location = new Point(16, 26),
                Size = new Size(540, 28),
                BackColor = BG_CARD,
                ForeColor = TEXT_SEC,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f),
                Text = "Path paste karein   ya   Browse karein…"
            };

            txtSavePath.GotFocus += (s, e) =>
            {
                if (txtSavePath.ForeColor == TEXT_SEC)
                {
                    txtSavePath.Text = "";
                    txtSavePath.ForeColor = TEXT_PRI;
                }
            };

            txtSavePath.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSavePath.Text))
                {
                    txtSavePath.Text = "Path paste karein   ya   Browse karein…";
                    txtSavePath.ForeColor = TEXT_SEC;
                    txtSavePath.BackColor = BG_CARD;
                }
            };

            txtSavePath.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.A)
                {
                    txtSavePath.SelectAll();
                    e.SuppressKeyPress = true;
                }
            };

            txtSavePath.Leave += (s, e) =>
            {
                string p = GetSavePath();
                if (p.Length == 0)
                {
                    txtSavePath.BackColor = BG_CARD;
                    return;
                }

                txtSavePath.BackColor = Directory.Exists(p)
                    ? Color.FromArgb(30, 52, 40)
                    : Color.FromArgb(52, 28, 28);
            };

            btnBrowsePath = MakeButton("📁  Browse", BG_CARD, new Point(564, 23));
            btnBrowsePath.Width = 110;
            btnBrowsePath.ForeColor = TEXT_PRI;
            btnBrowsePath.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Output folder select karein";

                    string currentPath = GetSavePath();
                    if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
                        fbd.SelectedPath = currentPath;

                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        txtSavePath.Text = fbd.SelectedPath;
                        txtSavePath.ForeColor = TEXT_PRI;
                        txtSavePath.BackColor = Color.FromArgb(30, 52, 40);
                    }
                }
            };

            var btnSameAsSource = MakeButton("↩  Same Folder", BG_CARD, new Point(682, 23));
            btnSameAsSource.Width = 130;
            btnSameAsSource.ForeColor = TEXT_SEC;
            btnSameAsSource.Font = new Font("Segoe UI", 8f);
            btnSameAsSource.Click += (s, e) =>
            {
                if (imagePaths.Count == 0) return;

                string src = Path.GetDirectoryName(imagePaths[0]);
                txtSavePath.Text = src;
                txtSavePath.ForeColor = TEXT_PRI;
                txtSavePath.BackColor = Color.FromArgb(30, 52, 40);
            };

            btnSaveCurrent = MakeButton("💾  Save Current", BG_CARD, new Point(16, 60));
            btnSaveCurrent.Width = 160;
            btnSaveCurrent.ForeColor = TEXT_PRI;
            btnSaveCurrent.Click += BtnSaveCurrent_Click;

            btnSaveAll = MakeButton("⬇  Save All Images", ACCENT, new Point(188, 60));
            btnSaveAll.Width = 180;
            btnSaveAll.Click += BtnSaveAll_Click;

            pbBatch = new ProgressBar
            {
                Location = new Point(384, 66),
                Size = new Size(260, 22),
                Style = ProgressBarStyle.Continuous,
                BackColor = BG_CARD,
                ForeColor = SUCCESS,
                Visible = false
            };

            var lblBatchStatus = new Label
            {
                Name = "lblBatchStatus",
                Text = "",
                Font = new Font("Segoe UI", 8f),
                ForeColor = TEXT_SEC,
                Location = new Point(652, 68),
                AutoSize = true
            };

            pnlBottom.Controls.AddRange(new Control[]
            {
                lblPathHead, txtSavePath, btnBrowsePath, btnSameAsSource,
                btnSaveCurrent, btnSaveAll, pbBatch, lblBatchStatus
            });

            this.Controls.Add(pnlBottom);
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp";
                ofd.Multiselect = true;

                if (ofd.ShowDialog() != DialogResult.OK) return;

                foreach (string f in ofd.FileNames)
                {
                    if (!imagePaths.Contains(f))
                    {
                        imagePaths.Add(f);
                        lstImages.Items.Add(Path.GetFileName(f));
                    }
                }

                UpdateImageCount();

                if (lstImages.Items.Count > 0 && currentIndex < 0)
                    lstImages.SelectedIndex = 0;
            }
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            int idx = lstImages.SelectedIndex;
            if (idx < 0) return;

            imagePaths.RemoveAt(idx);
            lstImages.Items.RemoveAt(idx);
            UpdateImageCount();

            if (lstImages.Items.Count == 0)
            {
                currentIndex = -1;
                ClearPreviews();
            }
            else
            {
                lstImages.SelectedIndex = Math.Min(idx, lstImages.Items.Count - 1);
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            imagePaths.Clear();
            lstImages.Items.Clear();
            currentIndex = -1;
            ClearPreviews();
            UpdateImageCount();
        }

        private void LstImages_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = lstImages.SelectedIndex;
            if (idx < 0 || idx >= imagePaths.Count) return;

            currentIndex = idx;
            LoadImage(imagePaths[idx]);
        }

        private void TrackQuality_Scroll(object sender, EventArgs e)
        {
            lblQualityVal.Text = trackQuality.Value + "%";
            lblQualityVal.ForeColor = QualityColor(trackQuality.Value);
            CompressImage();
        }

        private void BtnSaveCurrent_Click(object sender, EventArgs e)
        {
            if (originalImage == null || currentIndex < 0)
            {
                MessageBox.Show("Pehle koi image select karein.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "JPEG Image|*.jpg";
                sfd.FileName = Path.GetFileNameWithoutExtension(imagePaths[currentIndex]) + ".jpg";

                if (sfd.ShowDialog() != DialogResult.OK) return;

                int fw, fh, fq;
                byte[] data = CompressToTargetKb(originalImage, TARGET_KB, out fw, out fh, out fq);
                File.WriteAllBytes(sfd.FileName, data);

                long sz = new FileInfo(sfd.FileName).Length;

                MessageBox.Show(
                    $"Saved!\nSize: {FormatBytes(sz)}\nDimension: {fw}×{fh}\nQuality: {fq}%",
                    "Done",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void BtnSaveAll_Click(object sender, EventArgs e)
        {
            if (imagePaths.Count == 0)
            {
                MessageBox.Show("Koi image list mein nahi hai.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string outDir = GetSavePath();

            if (string.IsNullOrEmpty(outDir))
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Output folder select karein";

                    if (fbd.ShowDialog() != DialogResult.OK) return;

                    outDir = fbd.SelectedPath;
                    txtSavePath.Text = outDir;
                    txtSavePath.ForeColor = TEXT_PRI;
                    txtSavePath.BackColor = Color.FromArgb(30, 52, 40);
                }
            }
            else if (!Directory.Exists(outDir))
            {
                var res = MessageBox.Show(
                    $"Ye folder exist nahi karta:\n{outDir}\n\nBanana chahte hain?",
                    "Folder nahi mila",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(outDir);
                    }
                    catch
                    {
                        MessageBox.Show("Folder banana fail ho gaya.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else return;
            }

            int done = 0, failed = 0;

            pbBatch.Visible = true;
            pbBatch.Maximum = imagePaths.Count;
            pbBatch.Value = 0;

            var statusLabel = pnlBottom.Controls["lblBatchStatus"] as Label;

            foreach (string path in imagePaths)
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(path);

                    using (var ms = new MemoryStream(bytes))
                    using (var img = Image.FromStream(ms))
                    {
                        string name = Path.GetFileNameWithoutExtension(path);
                        string outPath = Path.Combine(outDir, name + ".jpg");

                        int n = 1;
                        while (File.Exists(outPath))
                        {
                            outPath = Path.Combine(outDir, $"{name}.jpg");
                            n++;
                        }

                        int fw, fh, fq;
                        byte[] data = CompressToTargetKb(img, TARGET_KB, out fw, out fh, out fq);
                        File.WriteAllBytes(outPath, data);

                        done++;
                    }
                }
                catch
                {
                    failed++;
                }

                pbBatch.Value = done + failed;

                if (statusLabel != null)
                    statusLabel.Text = $"{done + failed}/{imagePaths.Count}";

                Application.DoEvents();
            }

            pbBatch.Visible = false;

            if (statusLabel != null)
                statusLabel.Text = "";

            string msg = $"✔  {done} image(s) saved under approx {TARGET_KB} KB!\nFolder: {outDir}";

            if (failed > 0)
                msg += $"\n✖  {failed} file(s) failed.";

            MessageBox.Show(msg, "Batch Done", MessageBoxButtons.OK,
                failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private void LoadImage(string path)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);

                using (var ms = new MemoryStream(bytes))
                using (var tmp = Image.FromStream(ms))
                {
                    originalImage?.Dispose();
                    originalImage = new Bitmap(tmp);
                }

                originalFileSize = new FileInfo(path).Length;

                lblOrigSize.Text = FormatBytes(originalFileSize)
                    + $"  {originalImage.Width}×{originalImage.Height}";

                picOriginal.Image = originalImage;

                CompressImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Image load nahi hui:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CompressImage()
        {
            if (originalImage == null) return;

            int finalW, finalH, finalQ;
            byte[] compressed = CompressToTargetKb(originalImage, TARGET_KB, out finalW, out finalH, out finalQ);

            int pct = originalFileSize > 0
                ? (int)(100.0 - (double)compressed.Length / originalFileSize * 100)
                : 0;

            lblCompSize.Text = $"{FormatBytes(compressed.Length)}  {finalW}×{finalH}  Q:{finalQ}%";
            lblSaved.Text = pct > 0 ? $"▼ {pct}% saved" : "";
            lblSaved.ForeColor = SUCCESS;

            using (var ms2 = new MemoryStream(compressed))
            using (var tmp = Image.FromStream(ms2))
            {
                var old = picCompressed.Image;
                picCompressed.Image = null;
                old?.Dispose();
                picCompressed.Image = new Bitmap(tmp);
            }
        }

        private byte[] CompressToTargetKb(Image sourceImage, int targetKb, out int finalWidth, out int finalHeight, out int finalQuality)
        {
            int targetBytes = targetKb * 1024;

            int width = sourceImage.Width;
            int height = sourceImage.Height;

            finalWidth = width;
            finalHeight = height;
            finalQuality = trackQuality.Value;

            while (width >= MIN_WIDTH && height >= MIN_HEIGHT)
            {
                using (Bitmap resized = ResizeImageKeepRatio(sourceImage, width, height))
                {
                    for (int q = trackQuality.Value; q >= 5; q -= 5)
                    {
                        byte[] data = ImageToJpegBytes(resized, q);

                        if (data.Length <= targetBytes)
                        {
                            finalWidth = resized.Width;
                            finalHeight = resized.Height;
                            finalQuality = q;
                            return data;
                        }
                    }
                }

                width = (int)(width * 0.90);
                height = (int)(height * 0.90);
            }

            using (Bitmap last = ResizeImageKeepRatio(sourceImage, MIN_WIDTH, MIN_HEIGHT))
            {
                finalWidth = last.Width;
                finalHeight = last.Height;
                finalQuality = 5;
                return ImageToJpegBytes(last, 5);
            }
        }

        private Bitmap ResizeImageKeepRatio(Image img, int maxWidth, int maxHeight)
        {
            double ratioX = (double)maxWidth / img.Width;
            double ratioY = (double)maxHeight / img.Height;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = Math.Max(1, (int)(img.Width * ratio));
            int newHeight = Math.Max(1, (int)(img.Height * ratio));

            Bitmap bmp = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                g.DrawImage(img, 0, 0, newWidth, newHeight);
            }

            return bmp;
        }

        private byte[] ImageToJpegBytes(Image img, int quality)
        {
            ImageCodecInfo codec = GetEncoder(ImageFormat.Jpeg);

            using (MemoryStream ms = new MemoryStream())
            {
                EncoderParameters encParams = new EncoderParameters(1);
                encParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);

                img.Save(ms, codec, encParams);
                return ms.ToArray();
            }
        }

        private void ClearPreviews()
        {
            originalImage?.Dispose();
            originalImage = null;

            picOriginal.Image = null;

            var old = picCompressed.Image;
            picCompressed.Image = null;
            old?.Dispose();

            lblOrigSize.Text = "--";
            lblCompSize.Text = "--";
            lblSaved.Text = "";
        }

        private void UpdateImageCount()
        {
            lblImageCount.Text = $"{imagePaths.Count} image{(imagePaths.Count != 1 ? "s" : "")}";
        }

        private Button MakeButton(string text, Color bg, Point loc)
        {
            var btn = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(140, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = TEXT_PRI,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Lighten(bg, 18);
            btn.FlatAppearance.MouseDownBackColor = Darken(bg, 10);

            return btn;
        }

        private void RoundCorners(Panel p)
        {
            p.Paint += (s, e) =>
            {
                using (var pen = new Pen(BORDER, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                }
            };
        }

        private void LstImages_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            bool sel = (e.State & DrawItemState.Selected) != 0;
            Color bg = sel ? ACCENT : BG_CARD;
            string text = lstImages.Items[e.Index].ToString();

            using (var bgBrush = new SolidBrush(bg))
            using (var iconBrush = new SolidBrush(sel ? Color.White : TEXT_SEC))
            using (var textBrush = new SolidBrush(sel ? Color.White : TEXT_PRI))
            using (var borderPen = new Pen(BORDER, 1))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);

                e.Graphics.DrawString("🖼", new Font("Segoe UI", 9f),
                    iconBrush, new Point(e.Bounds.X + 6, e.Bounds.Y + 4));

                e.Graphics.DrawString(TruncateFilename(text, 28),
                    new Font("Segoe UI", 9f),
                    textBrush, new Point(e.Bounds.X + 26, e.Bounds.Y + 5));

                e.Graphics.DrawLine(borderPen,
                    e.Bounds.X, e.Bounds.Bottom - 1,
                    e.Bounds.Right, e.Bounds.Bottom - 1);
            }
        }

        private string TruncateFilename(string name, int maxChars)
        {
            return name.Length > maxChars
                ? name.Substring(0, maxChars - 1) + "…"
                : name;
        }

        private Color QualityColor(int q)
        {
            return q >= 75 ? SUCCESS :
                   q >= 40 ? Color.FromArgb(251, 191, 36) :
                              Color.FromArgb(248, 113, 113);
        }

        private Color Lighten(Color c, int amt)
        {
            return Color.FromArgb(
                Math.Min(255, c.R + amt),
                Math.Min(255, c.G + amt),
                Math.Min(255, c.B + amt));
        }

        private Color Darken(Color c, int amt)
        {
            return Color.FromArgb(
                Math.Max(0, c.R - amt),
                Math.Max(0, c.G - amt),
                Math.Max(0, c.B - amt));
        }

        private string GetSavePath()
        {
            string t = txtSavePath.Text.Trim();
            return (txtSavePath.ForeColor == TEXT_SEC) ? "" : t;
        }

        private string FormatBytes(long b)
        {
            return b >= 1048576 ? $"{b / 1048576.0:F2} MB" :
                   b >= 1024 ? $"{b / 1024.0:F1} KB" :
                                $"{b} B";
        }

        private ImageCodecInfo GetEncoder(ImageFormat fmt)
        {
            foreach (var c in ImageCodecInfo.GetImageDecoders())
            {
                if (c.FormatID == fmt.Guid)
                    return c;
            }

            return null;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            originalImage?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
