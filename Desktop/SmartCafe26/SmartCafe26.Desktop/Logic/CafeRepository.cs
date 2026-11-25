using System.Data;
using Microsoft.Data.SqlClient;
using SmartCafe26.Desktop.Data;

namespace SmartCafe26.Desktop.Logic
{
    public class CafeRepository
    {
        // --- 1. MANAJEMEN MENU (CRUD + JUMLAH STOK) ---
        public DataTable GetAllMenu()
        {
            using var conn = DbConnectionFactory.GetConnection();
            string sql = @"SELECT m.IdMenu, m.IdKategori, k.NamaKategori, m.NamaMenu, m.Harga, m.JumlahStok, m.StatusStok 
                           FROM Menu m INNER JOIN KategoriMenu k ON m.IdKategori = k.IdKategori";
            using var da = new SqlDataAdapter(sql, conn);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public DataTable GetKategori()
        {
            using var conn = DbConnectionFactory.GetConnection();
            using var da = new SqlDataAdapter("SELECT IdKategori, NamaKategori FROM KategoriMenu", conn);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public bool IsMenuExist(string idMenu)
        {
            using var conn = DbConnectionFactory.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("SELECT COUNT(1) FROM Menu WHERE IdMenu = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", idMenu);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public void InsertMenu(string idMenu, string idKategori, string nama, double harga, int jumlahStok)
        {
            using var conn = DbConnectionFactory.GetConnection();
            conn.Open();
            string statusStok = jumlahStok > 0 ? "Tersedia" : "Habis";
            string sql = "INSERT INTO Menu (IdMenu, IdKategori, NamaMenu, Harga, JumlahStok, StatusStok) VALUES (@Id, @Kat, @Nama, @Harga, @Jml, @Stok)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", idMenu);
            cmd.Parameters.AddWithValue("@Kat", idKategori);
            cmd.Parameters.AddWithValue("@Nama", nama);
            cmd.Parameters.AddWithValue("@Harga", harga);
            cmd.Parameters.AddWithValue("@Jml", jumlahStok);
            cmd.Parameters.AddWithValue("@Stok", statusStok);
            cmd.ExecuteNonQuery();
        }

        public void UpdateMenu(string idMenu, string idKategori, string nama, double harga, int jumlahStok)
        {
            using var conn = DbConnectionFactory.GetConnection();
            conn.Open();
            string statusStok = jumlahStok > 0 ? "Tersedia" : "Habis";
            string sql = "UPDATE Menu SET IdKategori=@Kat, NamaMenu=@Nama, Harga=@Harga, JumlahStok=@Jml, StatusStok=@Stok WHERE IdMenu=@Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", idMenu);
            cmd.Parameters.AddWithValue("@Kat", idKategori);
            cmd.Parameters.AddWithValue("@Nama", nama);
            cmd.Parameters.AddWithValue("@Harga", harga);
            cmd.Parameters.AddWithValue("@Jml", jumlahStok);
            cmd.Parameters.AddWithValue("@Stok", statusStok);
            cmd.ExecuteNonQuery();
        }

        public void DeleteMenu(string idMenu)
        {
            using var conn = DbConnectionFactory.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("DELETE FROM Menu WHERE IdMenu=@Id", conn);
            cmd.Parameters.AddWithValue("@Id", idMenu);
            cmd.ExecuteNonQuery();
        }

        // --- 2. ANTREAN PESANAN & KASIR ---
        public DataTable GetAntreanPesanan()
        {
            using var conn = DbConnectionFactory.GetConnection();
            string sql = @"SELECT IdPesanan, NomorMeja, NamaPelanggan, WaktuPesan, TotalHarga, StatusPesanan 
                           FROM Pesanan ORDER BY WaktuPesan DESC";
            using var da = new SqlDataAdapter(sql, conn);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public DataTable GetDetailPesanan(string idPesanan)
        {
            using var conn = DbConnectionFactory.GetConnection();
            string sql = @"SELECT d.IdMenu, m.NamaMenu, d.Qty, m.Harga, d.Subtotal, ISNULL(d.Catatan, '-') AS Catatan
                           FROM DetailPesanan d INNER JOIN Menu m ON d.IdMenu = m.IdMenu
                           WHERE d.IdPesanan = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", idPesanan);
            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public void UpdateStatusPesanan(string idPesanan, string statusBaru)
        {
            using var conn = DbConnectionFactory.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("UPDATE Pesanan SET StatusPesanan=@Status WHERE IdPesanan=@Id", conn);
            cmd.Parameters.AddWithValue("@Status", statusBaru);
            cmd.Parameters.AddWithValue("@Id", idPesanan);
            cmd.ExecuteNonQuery();
        }

        public string ProsesPembayaran(string idPesanan, double totalBayar, double uangBayar, double kembalian)
        {
            using var conn = DbConnectionFactory.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                string idTrans = "TRX-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                string sqlTrx = @"INSERT INTO TransaksiKasir (IdTransaksi, IdPesanan, TotalBayar, UangBayar, UangKembali, WaktuBayar)
                                  VALUES (@IdTrx, @IdPes, @Total, @Bayar, @Kembali, GETDATE())";
                using (var cmd = new SqlCommand(sqlTrx, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@IdTrx", idTrans);
                    cmd.Parameters.AddWithValue("@IdPes", idPesanan);
                    cmd.Parameters.AddWithValue("@Total", totalBayar);
                    cmd.Parameters.AddWithValue("@Bayar", uangBayar);
                    cmd.Parameters.AddWithValue("@Kembali", kembalian);
                    cmd.ExecuteNonQuery();
                }

                using (var cmdUpd = new SqlCommand("UPDATE Pesanan SET StatusPesanan='Selesai' WHERE IdPesanan=@Id", conn, trans))
                {
                    cmdUpd.Parameters.AddWithValue("@Id", idPesanan);
                    cmdUpd.ExecuteNonQuery();
                }

                trans.Commit();
                return idTrans;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
    }
}