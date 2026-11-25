using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();
app.UseCors();

string connString = @"Server=localhost\SQLEXPRESS01;Database=SmartCafe26;Trusted_Connection=True;TrustServerCertificate=True;";

// GET: Ambil Daftar Menu beserta JumlahStok
app.MapGet("/api/menu", async () =>
{
    var list = new List<object>();
    using var conn = new SqlConnection(connString);
    await conn.OpenAsync();

    string query = @"SELECT m.IdMenu, m.IdKategori, k.NamaKategori, m.NamaMenu, m.Harga, m.JumlahStok, m.StatusStok 
                     FROM Menu m 
                     INNER JOIN KategoriMenu k ON m.IdKategori = k.IdKategori";
    using var cmd = new SqlCommand(query, conn);
    using var reader = await cmd.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        list.Add(new
        {
            idMenu = reader.GetString(0),
            idKategori = reader.GetString(1),
            namaKategori = reader.GetString(2),
            namaMenu = reader.GetString(3),
            harga = reader.GetDouble(4),
            jumlahStok = reader.GetInt32(5),
            statusStok = reader.GetString(6)
        });
    }
    return Results.Ok(list);
});

// POST: Kirim Pesanan & Otomatis Kurangi Stok Menu
app.MapPost("/api/pesanan", async (PesananRequest req) =>
{
    using var conn = new SqlConnection(connString);
    await conn.OpenAsync();
    using var transaction = conn.BeginTransaction();

    try
    {
        string idPesanan = "ORD-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        double totalHarga = req.Items.Sum(i => i.Harga * i.Qty);

        string insertPesanan = @"INSERT INTO Pesanan (IdPesanan, NomorMeja, NamaPelanggan, WaktuPesan, TotalHarga, StatusPesanan)
                                 VALUES (@Id, @Meja, @Nama, GETDATE(), @Total, 'Menunggu')";
        using (var cmdP = new SqlCommand(insertPesanan, conn, transaction))
        {
            cmdP.Parameters.AddWithValue("@Id", idPesanan);
            cmdP.Parameters.AddWithValue("@Meja", req.NomorMeja);
            cmdP.Parameters.AddWithValue("@Nama", req.NamaPelanggan);
            cmdP.Parameters.AddWithValue("@Total", totalHarga);
            await cmdP.ExecuteNonQueryAsync();
        }

        int counter = 1;
        foreach (var item in req.Items)
        {
            string idDetail = $"{idPesanan}-{counter++:D2}";
            string insertDetail = @"INSERT INTO DetailPesanan (IdDetail, IdPesanan, IdMenu, Qty, Subtotal, Catatan)
                                    VALUES (@IdDetail, @IdPesanan, @IdMenu, @Qty, @Subtotal, @Catatan)";
            using (var cmdD = new SqlCommand(insertDetail, conn, transaction))
            {
                cmdD.Parameters.AddWithValue("@IdDetail", idDetail);
                cmdD.Parameters.AddWithValue("@IdPesanan", idPesanan);
                cmdD.Parameters.AddWithValue("@IdMenu", item.IdMenu);
                cmdD.Parameters.AddWithValue("@Qty", item.Qty);
                cmdD.Parameters.AddWithValue("@Subtotal", item.Harga * item.Qty);
                cmdD.Parameters.AddWithValue("@Catatan", string.IsNullOrWhiteSpace(item.Catatan) ? DBNull.Value : item.Catatan);
                await cmdD.ExecuteNonQueryAsync();
            }

            // Kurangi stok dan otomatis ubah status ke 'Habis' jika sisa stok <= 0
            string updateStok = @"UPDATE Menu 
                                  SET JumlahStok = CASE WHEN JumlahStok - @Qty < 0 THEN 0 ELSE JumlahStok - @Qty END,
                                      StatusStok = CASE WHEN JumlahStok - @Qty <= 0 THEN 'Habis' ELSE 'Tersedia' END
                                  WHERE IdMenu = @IdMenu";
            using (var cmdStok = new SqlCommand(updateStok, conn, transaction))
            {
                cmdStok.Parameters.AddWithValue("@Qty", item.Qty);
                cmdStok.Parameters.AddWithValue("@IdMenu", item.IdMenu);
                await cmdStok.ExecuteNonQueryAsync();
            }
        }

        await transaction.CommitAsync();
        return Results.Ok(new { success = true, idPesanan = idPesanan, totalHarga = totalHarga });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

app.Run("http://0.0.0.0:5000");

public record PesananItemDto(string IdMenu, int Qty, double Harga, string? Catatan);
public record PesananRequest(string NomorMeja, string NamaPelanggan, List<PesananItemDto> Items);