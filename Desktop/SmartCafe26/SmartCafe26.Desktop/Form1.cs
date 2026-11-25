using System.Data;
using System.Text;
using SmartCafe26.Desktop.Logic;

namespace SmartCafe26.Desktop
{
    public partial class Form1 : Form
    {
        private readonly CafeRepository _repo = new();

        // Palet Warna Modern Cafe (Espresso & Warm Cream)
        private readonly Color _colEspresso = Color.FromArgb(44, 28, 22);
        private readonly Color _colMocha = Color.FromArgb(93, 64, 55);
        private readonly Color _colCaramel = Color.FromArgb(198, 124, 78);
        private readonly Color _colCreamBg = Color.FromArgb(248, 245, 242);
        private readonly Color _colGreen = Color.FromArgb(46, 125, 50);
        private readonly Color _colRed = Color.FromArgb(198, 40, 40);

        // Header, Auto-Refresh & Stat Cards
        private Label lblClock = new();
        private CheckBox chkAutoRefresh = new();
        private System.Windows.Forms.Timer _autoRefreshTimer = new();
        private Label lblStatMenu = new(), lblStatHabis = new(), lblStatAntrean = new(), lblStatOmzet = new();

        // Tab 1 Controls (Master Menu)
        private TextBox txtIdMenu = new(), txtNamaMenu = new(), txtHarga = new();
        private ComboBox cmbKategori = new();
        private NumericUpDown numStok = new();
        private DataGridView dgvMenu = new();

        // Tab 2 Controls (Antrean & Kasir POS)
        private DataGridView dgvPesanan = new(), dgvDetail = new();
        private Label lblIdPesanan = new(), lblMejaPelanggan = new(), lblTotalBayar = new(), lblKembalian = new();
        private TextBox txtUangBayar = new();
        private ComboBox cmbStatusOrder = new();
        private RichTextBox rtbStruk = new();
        private double _selectedTotal = 0;
        private string? _selectedOrderId = null;

        public Form1()
        {
            Text = "SmartCafe26 — Back-Office & Point of Sale";
            Size = new Size(1240, 780);
            MinimumSize = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = _colCreamBg;
            Font = new Font("Segoe UI", 10);

            InitializeCustomUI();
            LoadInitialData();
            SetupAutoRefresh();
        }

        private void InitializeCustomUI()
        {
            // ================= 1. TOP BANNER HEADER =================
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = _colEspresso,
                Padding = new Padding(20, 0, 20, 0)
            };

            var lblBrand = new Label
            {
                Text = "☕ SMARTCAFE 26  |  Back-Office & POS System",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 12)
            };

            var lblSub = new Label
            {
                Text = "SMKS Informatika Kota Serang • Integrated SQL Server & Mobile Order",
                ForeColor = Color.FromArgb(215, 204, 200),
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(23, 41)
            };

            var pnlRightHeader = new Panel
            {
                Dock = DockStyle.Right,
                Width = 360,
                BackColor = _colEspresso
            };

            lblClock = new Label
            {
                Text = DateTime.Now.ToString("dddd, dd MMM yyyy • HH:mm:ss"),
                ForeColor = Color.FromArgb(255, 224, 178),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(0, 12),
                Size = new Size(360, 22),
                TextAlign = ContentAlignment.MiddleRight
            };

            chkAutoRefresh = new CheckBox
            {
                Text = "Auto-Refresh Pesanan (5 detik)",
                Checked = true,
                ForeColor = Color.FromArgb(200, 230, 201),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(140, 38),
                Size = new Size(220, 24),
                TextAlign = ContentAlignment.MiddleRight,
                Cursor = Cursors.Hand
            };
            chkAutoRefresh.CheckedChanged += (s, e) =>
            {
                _autoRefreshTimer.Enabled = chkAutoRefresh.Checked;
            };

            pnlRightHeader.Controls.AddRange(new Control[] { lblClock, chkAutoRefresh });

            var clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) => lblClock.Text = DateTime.Now.ToString("dddd, dd MMM yyyy • HH:mm:ss");
            clockTimer.Start();

            pnlHeader.Controls.AddRange(new Control[] { lblBrand, lblSub, pnlRightHeader });

            // ================= 2. SUMMARY STAT CARDS =================
            var pnlStats = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 82,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(15, 10, 15, 5),
                BackColor = _colCreamBg
            };
            for (int i = 0; i < 4; i++) pnlStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            pnlStats.Controls.Add(CreateStatCard("TOTAL VARIAN MENU", lblStatMenu, _colMocha), 0, 0);
            pnlStats.Controls.Add(CreateStatCard("STOK HABIS (0 PORSI)", lblStatHabis, _colRed), 1, 0);
            pnlStats.Controls.Add(CreateStatCard("PESANAN AKTIF", lblStatAntrean, _colCaramel), 2, 0);
            pnlStats.Controls.Add(CreateStatCard("PENDAPATAN SELESAI", lblStatOmzet, _colGreen), 3, 0);

            // ================= 3. MAIN TAB CONTROL =================
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(16, 8),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            // ---------------- TAB 1: MASTER MENU & STOK ----------------
            var tabMenu = new TabPage("  📋 Manajemen Menu & Stok  ")
            {
                BackColor = _colCreamBg,
                Padding = new Padding(12)
            };
            var pnlInput = new Panel
            {
                Dock = DockStyle.Left,
                Width = 340,
                Padding = new Padding(18),
                BackColor = Color.White
            };

            var lblFormTitle = new Label
            {
                Text = "Form Data Menu & Stok",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = _colEspresso,
                Location = new Point(18, 15),
                AutoSize = true
            };
            pnlInput.Controls.Add(lblFormTitle);

            int y = 55;
            void AddField(string label, Control ctrl)
            {
                pnlInput.Controls.Add(new Label
                {
                    Text = label,
                    Location = new Point(18, y),
                    AutoSize = true,
                    ForeColor = Color.FromArgb(80, 80, 80),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                });
                ctrl.Location = new Point(18, y + 22);
                ctrl.Width = 300;
                ctrl.Font = new Font("Segoe UI", 10);
                pnlInput.Controls.Add(ctrl);
                y += 62;
            }

            AddField("Kode Menu (Contoh: MN006):", txtIdMenu);
            cmbKategori.DropDownStyle = ComboBoxStyle.DropDownList;
            AddField("Kategori:", cmbKategori);
            AddField("Nama Menu:", txtNamaMenu);
            AddField("Harga Jual (Rp):", txtHarga);

            numStok.Minimum = 0;
            numStok.Maximum = 9999;
            numStok.Value = 20;
            AddField("Jumlah Stok Tersedia (Porsi/Cup):", numStok);

            var btnTambah = CreateModernButton("➕ Simpan Baru", _colGreen, new Point(18, y + 5), 145);
            var btnUbah = CreateModernButton("✏️ Update Menu", _colMocha, new Point(173, y + 5), 145);
            var btnHapus = CreateModernButton("🗑️ Hapus", _colRed, new Point(18, y + 52), 145);
            var btnReset = CreateModernButton("🔄 Bersihkan", Color.DimGray, new Point(173, y + 52), 145);

            btnTambah.Click += BtnTambah_Click;
            btnUbah.Click += BtnUbah_Click;
            btnHapus.Click += BtnHapus_Click;
            btnReset.Click += (s, e) => ClearMenuForm();

            pnlInput.Controls.AddRange(new Control[] { btnTambah, btnUbah, btnHapus, btnReset });

            var pnlGridMenu = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 0, 0, 0)
            };
            StyleDataGridView(dgvMenu);
            dgvMenu.CellClick += DgvMenu_CellClick;
            dgvMenu.CellFormatting += DgvMenu_CellFormatting;
            pnlGridMenu.Controls.Add(dgvMenu);

            tabMenu.Controls.Add(pnlGridMenu);
            tabMenu.Controls.Add(pnlInput);

            // ---------------- TAB 2: ANTREAN & KASIR POS ----------------
            var tabPos = new TabPage("  🧾 Antrean Pesanan & Kasir POS  ")
            {
                BackColor = _colCreamBg,
                Padding = new Padding(12)
            };
            var pnlLeftPos = new Panel
            {
                Dock = DockStyle.Left,
                Width = 720,
                Padding = new Padding(0, 0, 12, 0)
            };

            var btnRefresh = CreateModernButton("🔄 Refresh Pesanan & Stok", _colEspresso, new Point(0, 0), 708);
            btnRefresh.Dock = DockStyle.Top;
            btnRefresh.Height = 40;
            btnRefresh.Click += BtnRefresh_Click;

            StyleDataGridView(dgvPesanan);
            dgvPesanan.Dock = DockStyle.Top;
            dgvPesanan.Height = 235;
            dgvPesanan.CellClick += DgvPesanan_CellClick;
            dgvPesanan.CellFormatting += DgvPesanan_CellFormatting;

            var lblDetailTitle = new Label
            {
                Text = "📌 Rincian Item Pesanan & Catatan Pelanggan:",
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.BottomLeft,
                ForeColor = _colEspresso,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            StyleDataGridView(dgvDetail);
            dgvDetail.Dock = DockStyle.Fill;

            pnlLeftPos.Controls.Add(dgvDetail);
            pnlLeftPos.Controls.Add(lblDetailTitle);
            pnlLeftPos.Controls.Add(dgvPesanan);
            pnlLeftPos.Controls.Add(btnRefresh);

            // Panel Kanan: Kasir & Struk
            var pnlRightPos = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18),
                BackColor = Color.White
            };

            lblIdPesanan = new Label
            {
                Text = "ID Pesanan: -",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = _colEspresso,
                Location = new Point(18, 14),
                AutoSize = true
            };
            lblMejaPelanggan = new Label
            {
                Text = "Meja / Pelanggan: -",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.DimGray,
                Location = new Point(18, 38),
                AutoSize = true
            };

            cmbStatusOrder.Location = new Point(18, 68);
            cmbStatusOrder.Width = 220;
            cmbStatusOrder.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatusOrder.Items.AddRange(new object[] { "Menunggu", "Diproses", "Selesai" });

            var btnUpdateStatus = CreateModernButton("Update Status", _colCaramel, new Point(248, 66), 155);
            btnUpdateStatus.Height = 31;
            btnUpdateStatus.Click += BtnUpdateStatus_Click;

            var pnlTotalBox = new Panel
            {
                Location = new Point(18, 108),
                Size = new Size(385, 48),
                BackColor = Color.FromArgb(255, 243, 224)
            };
            lblTotalBayar = new Label
            {
                Text = "Total Tagihan: Rp 0",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = _colEspresso,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlTotalBox.Controls.Add(lblTotalBayar);

            pnlRightPos.Controls.Add(new Label
            {
                Text = "Uang Tunai Diterima (Rp):",
                Location = new Point(18, 166),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            });
            txtUangBayar.Location = new Point(18, 188);
            txtUangBayar.Width = 385;
            txtUangBayar.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            txtUangBayar.TextChanged += TxtUangBayar_TextChanged;

            lblKembalian = new Label
            {
                Text = "Kembalian: Rp 0",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = _colGreen,
                Location = new Point(18, 224),
                AutoSize = true
            };

            var btnBayar = CreateModernButton("💳 PROSES BAYAR & CETAK NOTA", _colGreen, new Point(18, 256), 385);
            btnBayar.Height = 42;
            btnBayar.Click += BtnBayar_Click;

            rtbStruk.Location = new Point(18, 308);
            rtbStruk.Size = new Size(385, 230);
            rtbStruk.Font = new Font("Consolas", 9.5f);
            rtbStruk.ReadOnly = true;
            rtbStruk.BackColor = Color.FromArgb(252, 252, 250);
            rtbStruk.BorderStyle = BorderStyle.FixedSingle;

            pnlRightPos.Controls.AddRange(new Control[]
            {
                lblIdPesanan, lblMejaPelanggan, cmbStatusOrder, btnUpdateStatus,
                pnlTotalBox, txtUangBayar, lblKembalian, btnBayar, rtbStruk
            });

            tabPos.Controls.Add(pnlRightPos);
            tabPos.Controls.Add(pnlLeftPos);

            tabControl.TabPages.Add(tabMenu);
            tabControl.TabPages.Add(tabPos);

            Controls.Add(tabControl);
            Controls.Add(pnlStats);
            Controls.Add(pnlHeader);
        }

        // ================= AUTO-REFRESH SETUP =================
        private void SetupAutoRefresh()
        {
            _autoRefreshTimer.Interval = 5000; // 5 detik
            _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
            _autoRefreshTimer.Start();
        }

        private void AutoRefreshTimer_Tick(object? sender, EventArgs e)
        {
            if (Disposing || IsDisposed) return;

            // Jangan ganggu saat kasir sedang mengetik nominal uang bayar
            if (txtUangBayar.Focused && !string.IsNullOrWhiteSpace(txtUangBayar.Text)) return;

            try
            {
                LoadPesanan(preserveSelection: true);
                LoadMenu();
            }
            catch
            {
                // Abaikan error sesaat saat koneksi DB sibuk di background
            }
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            LoadPesanan(preserveSelection: true);
            LoadMenu();
            MessageBox.Show(
                "Antrean pesanan dan stok menu sudah diperbarui!",
                "Refresh Berhasil",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        // ================= HELPER UI STYLING =================
        private Panel CreateStatCard(string title, Label valueLabel, Color accentColor)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(5, 0, 5, 0)
            };
            var strip = new Panel
            {
                Dock = DockStyle.Left,
                Width = 6,
                BackColor = accentColor
            };
            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray,
                Location = new Point(14, 10),
                AutoSize = true
            };
            valueLabel.Text = "0";
            valueLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            valueLabel.ForeColor = accentColor;
            valueLabel.Location = new Point(14, 28);
            valueLabel.AutoSize = true;

            card.Controls.AddRange(new Control[] { valueLabel, lblTitle, strip });
            return card;
        }

        private Button CreateModernButton(string text, Color bgColor, Point loc, int width)
        {
            var btn = new Button
            {
                Text = text,
                Location = loc,
                Width = width,
                Height = 38,
                BackColor = bgColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void StyleDataGridView(DataGridView dgv)
        {
            dgv.Dock = DockStyle.Fill;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Color.FromArgb(235, 230, 225);
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = _colEspresso;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 38;
            dgv.RowTemplate.Height = 34;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 235, 233);
            dgv.DefaultCellStyle.SelectionForeColor = _colEspresso;
        }

        // ================= DATA LOADING & FORMATTING =================
        private void LoadInitialData()
        {
            cmbKategori.DataSource = _repo.GetKategori();
            cmbKategori.DisplayMember = "NamaKategori";
            cmbKategori.ValueMember = "IdKategori";

            LoadMenu();
            LoadPesanan();
        }

        private void LoadMenu()
        {
            var dt = _repo.GetAllMenu();
            dgvMenu.DataSource = dt;
            if (dgvMenu.Columns.Contains("IdKategori")) dgvMenu.Columns["IdKategori"].Visible = false;
            if (dgvMenu.Columns.Contains("Harga")) dgvMenu.Columns["Harga"].DefaultCellStyle.Format = "Rp #,##0";

            int habisCount = dt.AsEnumerable().Count(r => Convert.ToInt32(r["JumlahStok"]) <= 0);
            lblStatMenu.Text = $"{dt.Rows.Count} Item";
            lblStatHabis.Text = $"{habisCount} Item";
        }

        private void LoadPesanan(bool preserveSelection = false)
        {
            var dt = _repo.GetAntreanPesanan();
            dgvPesanan.DataSource = dt;
            if (dgvPesanan.Columns.Contains("TotalHarga")) dgvPesanan.Columns["TotalHarga"].DefaultCellStyle.Format = "Rp #,##0";

            int aktif = dt.AsEnumerable().Count(r => r["StatusPesanan"].ToString() != "Selesai");
            double omzet = dt.AsEnumerable()
                             .Where(r => r["StatusPesanan"].ToString() == "Selesai")
                             .Sum(r => Convert.ToDouble(r["TotalHarga"]));

            lblStatAntrean.Text = $"{aktif} Order";
            lblStatOmzet.Text = $"Rp {omzet:N0}";

            if (preserveSelection && !string.IsNullOrEmpty(_selectedOrderId))
            {
                foreach (DataGridViewRow row in dgvPesanan.Rows)
                {
                    if (row.Cells["IdPesanan"].Value?.ToString() == _selectedOrderId)
                    {
                        row.Selected = true;
                        dgvPesanan.CurrentCell = row.Cells[0];
                        cmbStatusOrder.SelectedItem = row.Cells["StatusPesanan"].Value?.ToString();
                        dgvDetail.DataSource = _repo.GetDetailPesanan(_selectedOrderId);
                        if (dgvDetail.Columns.Contains("Harga")) dgvDetail.Columns["Harga"].DefaultCellStyle.Format = "Rp #,##0";
                        if (dgvDetail.Columns.Contains("Subtotal")) dgvDetail.Columns["Subtotal"].DefaultCellStyle.Format = "Rp #,##0";
                        break;
                    }
                }
            }
        }

        private void DgvMenu_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvMenu.Columns[e.ColumnIndex].Name == "JumlahStok" && e.Value != null)
            {
                int stok = Convert.ToInt32(e.Value);
                if (stok <= 0)
                {
                    dgvMenu.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 238);
                    dgvMenu.Rows[e.RowIndex].DefaultCellStyle.ForeColor = _colRed;
                }
                else if (stok <= 5)
                {
                    dgvMenu.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 225);
                }
            }
        }

        private void DgvPesanan_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvPesanan.Columns[e.ColumnIndex].Name == "StatusPesanan" && e.Value != null)
            {
                string st = e.Value.ToString()!;
                if (st == "Menunggu") e.CellStyle!.ForeColor = _colRed;
                else if (st == "Diproses") e.CellStyle!.ForeColor = _colCaramel;
                else if (st == "Selesai") e.CellStyle!.ForeColor = _colGreen;
            }
        }

        // ================= CRUD MENU =================
        private void ClearMenuForm()
        {
            txtIdMenu.Clear();
            txtIdMenu.Enabled = true;
            txtNamaMenu.Clear();
            txtHarga.Clear();
            numStok.Value = 20;
        }

        private void DgvMenu_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvMenu.Rows[e.RowIndex];
            txtIdMenu.Text = row.Cells["IdMenu"].Value.ToString();
            txtIdMenu.Enabled = false;
            cmbKategori.SelectedValue = row.Cells["IdKategori"].Value.ToString();
            txtNamaMenu.Text = row.Cells["NamaMenu"].Value.ToString();
            txtHarga.Text = row.Cells["Harga"].Value.ToString();
            numStok.Value = Convert.ToDecimal(row.Cells["JumlahStok"].Value);
        }

        private void BtnTambah_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtIdMenu.Text) || string.IsNullOrWhiteSpace(txtNamaMenu.Text) || !double.TryParse(txtHarga.Text, out double harga) || harga <= 0)
            {
                MessageBox.Show("Pastikan Kode Menu, Nama Menu terisi dan Harga berupa angka positif!", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_repo.IsMenuExist(txtIdMenu.Text.Trim()))
            {
                MessageBox.Show("Kode Menu sudah terdaftar! Gunakan kode lain.", "Duplikat", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _repo.InsertMenu(txtIdMenu.Text.Trim(), cmbKategori.SelectedValue!.ToString()!, txtNamaMenu.Text.Trim(), harga, (int)numStok.Value);
            LoadMenu();
            ClearMenuForm();
            MessageBox.Show("Menu & jumlah stok berhasil ditambahkan!");
        }

        private void BtnUbah_Click(object? sender, EventArgs e)
        {
            if (txtIdMenu.Enabled || !double.TryParse(txtHarga.Text, out double harga) || harga <= 0)
            {
                MessageBox.Show("Pilih menu dari tabel terlebih dahulu dan pastikan harga valid!");
                return;
            }
            _repo.UpdateMenu(txtIdMenu.Text, cmbKategori.SelectedValue!.ToString()!, txtNamaMenu.Text.Trim(), harga, (int)numStok.Value);
            LoadMenu();
            ClearMenuForm();
            MessageBox.Show("Data menu & stok berhasil diperbarui!");
        }

        private void BtnHapus_Click(object? sender, EventArgs e)
        {
            if (txtIdMenu.Enabled) return;
            if (MessageBox.Show("Hapus menu ini?", "Konfirmasi", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    _repo.DeleteMenu(txtIdMenu.Text);
                    LoadMenu();
                    ClearMenuForm();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Menu tidak bisa dihapus karena sudah ada di riwayat pesanan.\n" + ex.Message);
                }
            }
        }

        // ================= KASIR POS & STRUK =================
        private void DgvPesanan_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvPesanan.Rows[e.RowIndex];
            string idPesanan = row.Cells["IdPesanan"].Value.ToString()!;
            string meja = row.Cells["NomorMeja"].Value.ToString()!;
            string pelanggan = row.Cells["NamaPelanggan"].Value.ToString()!;
            string status = row.Cells["StatusPesanan"].Value.ToString()!;
            _selectedOrderId = idPesanan;
            _selectedTotal = Convert.ToDouble(row.Cells["TotalHarga"].Value);

            lblIdPesanan.Text = $"ID Pesanan: {idPesanan}";
            lblMejaPelanggan.Text = $"Meja / Pelanggan: {meja} — {pelanggan}";
            cmbStatusOrder.SelectedItem = status;
            lblTotalBayar.Text = $"Total Tagihan: Rp {_selectedTotal:N0}";
            txtUangBayar.Clear();
            lblKembalian.Text = "Kembalian: Rp 0";

            dgvDetail.DataSource = _repo.GetDetailPesanan(idPesanan);
            if (dgvDetail.Columns.Contains("Harga")) dgvDetail.Columns["Harga"].DefaultCellStyle.Format = "Rp #,##0";
            if (dgvDetail.Columns.Contains("Subtotal")) dgvDetail.Columns["Subtotal"].DefaultCellStyle.Format = "Rp #,##0";
        }

        private void BtnUpdateStatus_Click(object? sender, EventArgs e)
        {
            if (dgvPesanan.CurrentRow == null) return;
            string idPesanan = dgvPesanan.CurrentRow.Cells["IdPesanan"].Value.ToString()!;
            _repo.UpdateStatusPesanan(idPesanan, cmbStatusOrder.Text);
            LoadPesanan(preserveSelection: true);
            MessageBox.Show("Status pesanan diperbarui!");
        }

        private void TxtUangBayar_TextChanged(object? sender, EventArgs e)
        {
            if (double.TryParse(txtUangBayar.Text, out double bayar))
            {
                double kembali = bayar - _selectedTotal;
                lblKembalian.Text = kembali >= 0 ? $"Kembalian: Rp {kembali:N0}" : "Kembalian: Uang Kurang";
                lblKembalian.ForeColor = kembali >= 0 ? _colGreen : _colRed;
            }
            else
            {
                lblKembalian.Text = "Kembalian: Rp 0";
            }
        }

        private void BtnBayar_Click(object? sender, EventArgs e)
        {
            if (dgvPesanan.CurrentRow == null) return;
            var row = dgvPesanan.CurrentRow;
            string idPesanan = row.Cells["IdPesanan"].Value.ToString()!;
            string status = row.Cells["StatusPesanan"].Value.ToString()!;

            if (status == "Selesai")
            {
                MessageBox.Show("Pesanan ini sudah lunas!");
                return;
            }
            if (!double.TryParse(txtUangBayar.Text, out double uangBayar) || uangBayar < _selectedTotal)
            {
                MessageBox.Show("Nominal uang diterima kurang dari total tagihan!", "Pembayaran Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double kembalian = uangBayar - _selectedTotal;
            string idTrx = _repo.ProsesPembayaran(idPesanan, _selectedTotal, uangBayar, kembalian);

            var sb = new StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine("             SMART CAFE 26              ");
            sb.AppendLine("     SMKS Informatika Kota Serang       ");
            sb.AppendLine("========================================");
            sb.AppendLine($"No. Trx : {idTrx}");
            sb.AppendLine($"Order   : {idPesanan}");
            sb.AppendLine($"Meja    : {row.Cells["NomorMeja"].Value} ({row.Cells["NamaPelanggan"].Value})");
            sb.AppendLine($"Waktu   : {DateTime.Now:dd/MM/yyyy HH:mm}");
            sb.AppendLine("----------------------------------------");
            foreach (DataGridViewRow item in dgvDetail.Rows)
            {
                sb.AppendLine($"{item.Cells["NamaMenu"].Value}");
                sb.AppendLine($"  {item.Cells["Qty"].Value} x {Convert.ToDouble(item.Cells["Harga"].Value):N0} = Rp {Convert.ToDouble(item.Cells["Subtotal"].Value):N0}");
            }
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"TOTAL BAYAR : Rp {_selectedTotal,17:N0}");
            sb.AppendLine($"TUNAI       : Rp {uangBayar,17:N0}");
            sb.AppendLine($"KEMBALIAN   : Rp {kembalian,17:N0}");
            sb.AppendLine("========================================");
            sb.AppendLine("     Terima Kasih Atas Kunjungan Anda   ");

            rtbStruk.Text = sb.ToString();
            LoadPesanan(preserveSelection: true);
            LoadMenu();
            MessageBox.Show("Pembayaran berhasil dicatat & struk dicetak!");
        }
    }
}