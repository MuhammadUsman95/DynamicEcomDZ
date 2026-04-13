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

        // ─── Colors (Dark Theme) ─────────────────────────────────
        private readonly Color BG_DARK = Color.FromArgb(18, 18, 24);
        private readonly Color BG_PANEL = Color.FromArgb(28, 28, 36);
        private readonly Color BG_CARD = Color.FromArgb(38, 38, 50);
        private readonly Color ACCENT = Color.FromArgb(99, 102, 241); // indigo
        private readonly Color ACCENT_HOV = Color.FromArgb(79, 82, 221);
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
        private Panel pnlQualityBar;

        public Form1()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);
            BuildUI();
        }

        // ═══════════════════════════════════════════════════════════
        //  UI BUILDER
        // ═══════════════════════════════════════════════════════════
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

        // ─── TOP BAR ─────────────────────────────────────────────
        private void BuildTopBar()
        {
            pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = BG_PANEL
            };
            pnlTop.Paint += (s, e) => {
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

            // Quality Label
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

            // TrackBar
            trackQuality = new TrackBar
            {
                Minimum = 1,
                Maximum = 100,
                Value = 40,
                TickFrequency = 10,
                Location = new Point(420, 12),
                Width = 340,
                Height = 36,
                BackColor = BG_PANEL
            };
            trackQuality.Scroll += TrackQuality_Scroll;

            // Quality hint labels
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

            pnlTop.Controls.AddRange(new Control[] {
                lblTitle, lblQualityHead, lblQualityVal, trackQuality, lMin, lMax
            });
            this.Controls.Add(pnlTop);
        }

        // ─── LEFT PANEL (file list) ───────────────────────────────
        private void BuildLeftPanel()
        {
            pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = BG_PANEL,
                Padding = new Padding(12)
            };
            pnlLeft.Paint += (s, e) => {
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

            // List
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

            // Buttons row 1
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

            pnlLeft.Controls.AddRange(new Control[] {
                lblFiles, lblImageCount, lstImages,
                btnAdd, btnRemove, btnClear
            });
            this.Controls.Add(pnlLeft);
        }

        // ─── MAIN PREVIEW AREA ───────────────────────────────────
        private void BuildMainArea()
        {
            pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BG_DARK,
                Padding = new Padding(16)
            };

            // ── Original card ─────────────────────────────────
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

            cardOrig.Controls.AddRange(new Control[] {
                lblOrigLabel, lblOrigSize, picOriginal
            });

            // ── Compressed card ───────────────────────────────
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
                Location = new Point(200, 29),
                AutoSize = true
            };

            picCompressed = new PictureBox
            {
                Location = new Point(8, 54),
                Size = new Size(446, 468),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = BG_DARK
            };

            cardComp.Controls.AddRange(new Control[] {
                lblCompLabel, lblCompSize, lblSaved, picCompressed
            });

            pnlMain.Controls.Add(cardOrig);
            pnlMain.Controls.Add(cardComp);

            // Resize handler to keep cards proportional
            pnlMain.Resize += (s, e) => {
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

        // ─── BOTTOM BAR ──────────────────────────────────────────
        private void BuildBottomBar()
        {
            pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                BackColor = BG_PANEL
            };
            pnlBottom.Paint += (s, e) => {
                e.Graphics.DrawLine(new Pen(BORDER, 1), 0, 0, pnlBottom.Width, 0);
            };

            // ── ROW 1 — Save Path ─────────────────────────────
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
            // Manual placeholder logic
            txtSavePath.GotFocus += (s, e) => {
                if (txtSavePath.ForeColor == TEXT_SEC)
                {
                    txtSavePath.Text = "";
                    txtSavePath.ForeColor = TEXT_PRI;
                }
            };
            txtSavePath.LostFocus += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtSavePath.Text))
                {
                    txtSavePath.Text = "Path paste karein   ya   Browse karein…";
                    txtSavePath.ForeColor = TEXT_SEC;
                    txtSavePath.BackColor = BG_CARD;
                }
            };
            // Ctrl+A support
            txtSavePath.KeyDown += (s, e) => {
                if (e.Control && e.KeyCode == Keys.A)
                {
                    txtSavePath.SelectAll();
                    e.SuppressKeyPress = true;
                }
            };
            // Validate on Leave — red tint if invalid
            txtSavePath.Leave += (s, e) => {
                string p = GetSavePath();
                if (p.Length == 0) { txtSavePath.BackColor = BG_CARD; return; }
                txtSavePath.BackColor = Directory.Exists(p)
                    ? Color.FromArgb(30, 52, 40)
                    : Color.FromArgb(52, 28, 28);
            };

            btnBrowsePath = MakeButton("📁  Browse", BG_CARD, new Point(564, 23));
            btnBrowsePath.Width = 110;
            btnBrowsePath.ForeColor = TEXT_PRI;
            btnBrowsePath.Click += (s, e) => {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Output folder select karein";
                    fbd.SelectedPath = txtSavePath.Text.Trim();
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        txtSavePath.Text = fbd.SelectedPath;
                        txtSavePath.BackColor = Color.FromArgb(30, 52, 40);
                    }
                }
            };

            // Quick "Same as Source" button
            var btnSameAsSource = MakeButton("↩  Same Folder", BG_CARD, new Point(682, 23));
            btnSameAsSource.Width = 130;
            btnSameAsSource.ForeColor = TEXT_SEC;
            btnSameAsSource.Font = new Font("Segoe UI", 8f);
            btnSameAsSource.Click += (s, e) => {
                if (imagePaths.Count == 0) return;
                string src = Path.GetDirectoryName(imagePaths[0]);
                txtSavePath.Text = src;
                txtSavePath.BackColor = Color.FromArgb(30, 52, 40);
            };

            // ── ROW 2 — Action buttons + Progress ─────────────
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

            pnlBottom.Controls.AddRange(new Control[] {
                lblPathHead, txtSavePath, btnBrowsePath, btnSameAsSource,
                btnSaveCurrent, btnSaveAll, pbBatch, lblBatchStatus
            });
            this.Controls.Add(pnlBottom);
        }

        // ═══════════════════════════════════════════════════════════
        //  EVENTS
        // ═══════════════════════════════════════════════════════════
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
                {
                    lstImages.SelectedIndex = 0;
                }
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
                sfd.FileName = Path.GetFileNameWithoutExtension(imagePaths[currentIndex]);
                if (sfd.ShowDialog() != DialogResult.OK) return;
                SaveJpeg(originalImage, sfd.FileName, trackQuality.Value);
                long sz = new FileInfo(sfd.FileName).Length;
                MessageBox.Show($"Saved!\nSize: {FormatBytes(sz)}", "Done",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            // ── Resolve output folder ─────────────────────────
            string outDir = GetSavePath();

            if (string.IsNullOrEmpty(outDir))
            {
                // Path box khali hai — FolderBrowserDialog fallback
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Output folder select karein";
                    if (fbd.ShowDialog() != DialogResult.OK) return;
                    outDir = fbd.SelectedPath;
                    txtSavePath.Text = outDir;
                    txtSavePath.BackColor = Color.FromArgb(30, 52, 40);
                }
            }
            else if (!Directory.Exists(outDir))
            {
                // Path exist nahi karta — create karne ka poochho
                var res = MessageBox.Show(
                    $"Ye folder exist nahi karta:\n{outDir}\n\nBanana chahte hain?",
                    "Folder nahi mila",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    try { Directory.CreateDirectory(outDir); }
                    catch
                    {
                        MessageBox.Show("Folder banana fail ho gaya.", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error); return;
                    }
                }
                else return;
            }

            // ── Batch save ────────────────────────────────────
            int quality = trackQuality.Value;
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
                            outPath = Path.Combine(outDir, $"{name}.jpg");

                        SaveJpeg(new Bitmap(img), outPath, quality);
                        done++;
                    }
                }
                catch { failed++; }

                pbBatch.Value = done + failed;
                if (statusLabel != null)
                    statusLabel.Text = $"{done + failed}/{imagePaths.Count}";
                Application.DoEvents();
            }

            pbBatch.Visible = false;
            if (statusLabel != null) statusLabel.Text = "";

            string msg = $"✔  {done} image(s) saved!\nFolder: {outDir}";
            if (failed > 0) msg += $"\n✖  {failed} file(s) failed.";
            MessageBox.Show(msg, "Batch Done", MessageBoxButtons.OK,
                failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        // ═══════════════════════════════════════════════════════════
        //  CORE LOGIC
        // ═══════════════════════════════════════════════════════════
        private void LoadImage(string path)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                using (var ms = new MemoryStream(bytes))
                {
                    using (var tmp = Image.FromStream(ms))
                    {
                        originalImage?.Dispose();
                        originalImage = new Bitmap(tmp);
                    }
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
            int quality = trackQuality.Value;

            ImageCodecInfo jpgCodec = GetEncoder(ImageFormat.Jpeg);
            EncoderParameters encParams = new EncoderParameters(1);
            encParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);

            using (var ms = new MemoryStream())
            {
                originalImage.Save(ms, jpgCodec, encParams);
                byte[] compressed = ms.ToArray();

                // Size label
                int pct = originalFileSize > 0
                    ? (int)(100.0 - (double)compressed.Length / originalFileSize * 100)
                    : 0;
                lblCompSize.Text = FormatBytes(compressed.Length);
                lblSaved.Text = pct > 0 ? $"▼ {pct}% saved" : (pct < 0 ? $"▲ {-pct}% larger" : "");
                lblSaved.ForeColor = pct >= 0 ? SUCCESS : Color.FromArgb(248, 113, 113);

                using (var ms2 = new MemoryStream(compressed))
                using (var tmp = Image.FromStream(ms2))
                {
                    var old = picCompressed.Image;
                    picCompressed.Image = null;
                    old?.Dispose();
                    picCompressed.Image = new Bitmap(tmp);
                }
            }
        }

        private void SaveJpeg(Image img, string path, int quality)
        {
            ImageCodecInfo codec = GetEncoder(ImageFormat.Jpeg);
            EncoderParameters encParams = new EncoderParameters(1);
            encParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);
            img.Save(path, codec, encParams);
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════
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
            // Subtle border painted on panel
            p.Paint += (s, e) => {
                var g = e.Graphics;
                var pen = new Pen(BORDER, 1);
                g.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                pen.Dispose();
            };
        }

        private void LstImages_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            Color bg = sel ? ACCENT : BG_CARD;
            string text = lstImages.Items[e.Index].ToString();

            e.Graphics.FillRectangle(new SolidBrush(bg), e.Bounds);
            // Icon
            e.Graphics.DrawString("🖼", new Font("Segoe UI", 9f),
                new SolidBrush(sel ? Color.White : TEXT_SEC),
                new Point(e.Bounds.X + 6, e.Bounds.Y + 4));
            // Text
            e.Graphics.DrawString(TruncateFilename(text, 28),
                new Font("Segoe UI", 9f),
                new SolidBrush(sel ? Color.White : TEXT_PRI),
                new Point(e.Bounds.X + 26, e.Bounds.Y + 5));

            // Bottom separator
            e.Graphics.DrawLine(new Pen(BORDER, 1),
                e.Bounds.X, e.Bounds.Bottom - 1,
                e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private string TruncateFilename(string name, int maxChars) =>
            name.Length > maxChars ? name.Substring(0, maxChars - 1) + "…" : name;

        private Color QualityColor(int q) =>
            q >= 75 ? SUCCESS :
            q >= 40 ? Color.FromArgb(251, 191, 36) :
                      Color.FromArgb(248, 113, 113);

        private Color Lighten(Color c, int amt) =>
            Color.FromArgb(Math.Min(255, c.R + amt),
                           Math.Min(255, c.G + amt),
                           Math.Min(255, c.B + amt));

        private Color Darken(Color c, int amt) =>
            Color.FromArgb(Math.Max(0, c.R - amt),
                           Math.Max(0, c.G - amt),
                           Math.Max(0, c.B - amt));

        // Returns actual typed path — placeholder text ko ignore karta hai
        private string GetSavePath()
        {
            string t = txtSavePath.Text.Trim();
            return (txtSavePath.ForeColor == TEXT_SEC) ? "" : t;
        }

        private string FormatBytes(long b) =>
            b >= 1048576 ? $"{b / 1048576.0:F2} MB" :
            b >= 1024 ? $"{b / 1024.0:F1} KB" :
                           $"{b} B";

        private ImageCodecInfo GetEncoder(ImageFormat fmt)
        {
            foreach (var c in ImageCodecInfo.GetImageDecoders())
                if (c.FormatID == fmt.Guid) return c;
            return null;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            originalImage?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
