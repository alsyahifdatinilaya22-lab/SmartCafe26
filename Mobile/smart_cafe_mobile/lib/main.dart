import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

void main() {
  runApp(const SmartCafeApp());
}

class SmartCafeApp extends StatelessWidget {
  const SmartCafeApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'SmartCafe26 Self-Order',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF4E342E),
          surface: const Color(0xFFF8F5F2),
        ),
        useMaterial3: true,
      ),
      home: const CustomerIdentityPage(),
    );
  }
}

// ================= MODEL DATA =================
class MenuItem {
  final String idMenu;
  final String namaKategori;
  final String namaMenu;
  final double harga;
  final int jumlahStok;
  final String statusStok;

  MenuItem({
    required this.idMenu,
    required this.namaKategori,
    required this.namaMenu,
    required this.harga,
    required this.jumlahStok,
    required this.statusStok,
  });

  factory MenuItem.fromJson(Map<String, dynamic> json) {
    return MenuItem(
      idMenu: json['idMenu'],
      namaKategori: json['namaKategori'],
      namaMenu: json['namaMenu'],
      harga: (json['harga'] as num).toDouble(),
      jumlahStok: json['jumlahStok'] ?? 0,
      statusStok: json['statusStok'],
    );
  }

  bool get isAvailable => statusStok == 'Tersedia' && jumlahStok > 0;
}

class CartItem {
  final MenuItem menu;
  int qty;
  String catatan;

  CartItem({required this.menu, this.qty = 1, this.catatan = ''});
}

String formatRupiah(double value) {
  final str = value.toStringAsFixed(0);
  final buffer = StringBuffer();
  int count = 0;
  for (int i = str.length - 1; i >= 0; i--) {
    buffer.write(str[i]);
    count++;
    if (count % 3 == 0 && i != 0) buffer.write('.');
  }
  return 'Rp ${buffer.toString().split('').reversed.join()}';
}

// ================= HALAMAN 1: IDENTITAS & NOMOR MEJA =================
class CustomerIdentityPage extends StatefulWidget {
  const CustomerIdentityPage({super.key});

  @override
  State<CustomerIdentityPage> createState() => _CustomerIdentityPageState();
}

class _CustomerIdentityPageState extends State<CustomerIdentityPage> {
  final _namaController = TextEditingController();
  String _selectedMeja = 'Meja 01';
  final List<String> _daftarMeja = List.generate(
    15,
    (index) => 'Meja ${(index + 1).toString().padLeft(2, '0')}',
  );

  @override
  void dispose() {
    _namaController.dispose();
    super.dispose();
  }

  void _mulaiPesan() {
    if (_namaController.text.trim().isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Silakan isi Nama Pelanggan terlebih dahulu')),
      );
      return;
    }

    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => MenuCatalogPage(
          namaPelanggan: _namaController.text.trim(),
          nomorMeja: _selectedMeja,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8F5F2),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Card(
            elevation: 3,
            color: Colors.white,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: const Color(0xFFEFEBE9),
                      borderRadius: BorderRadius.circular(50),
                    ),
                    child: const Icon(Icons.coffee, size: 52, color: Color(0xFF4E342E)),
                  ),
                  const SizedBox(height: 14),
                  const Text(
                    'SmartCafe 26',
                    style: TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFF2C1C16),
                    ),
                  ),
                  const Text(
                    'Customer Self-Order',
                    style: TextStyle(color: Colors.brown),
                  ),
                  const SizedBox(height: 24),
                  TextField(
                    controller: _namaController,
                    decoration: InputDecoration(
                      labelText: 'Nama Pelanggan',
                      prefixIcon: const Icon(Icons.person_outline),
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                    ),
                  ),
                  const SizedBox(height: 16),
                  DropdownButtonFormField<String>(
                    initialValue: _selectedMeja,
                    decoration: InputDecoration(
                      labelText: 'Nomor Meja',
                      prefixIcon: const Icon(Icons.table_restaurant_outlined),
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                    ),
                    items: _daftarMeja
                        .map((meja) => DropdownMenuItem(value: meja, child: Text(meja)))
                        .toList(),
                    onChanged: (val) {
                      if (val != null) setState(() => _selectedMeja = val);
                    },
                  ),
                  const SizedBox(height: 24),
                  SizedBox(
                    width: double.infinity,
                    height: 50,
                    child: FilledButton(
                      style: FilledButton.styleFrom(
                        backgroundColor: const Color(0xFF4E342E),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      ),
                      onPressed: _mulaiPesan,
                      child: const Text(
                        'Mulai Pesan Menu',
                        style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

// ================= HALAMAN 2: KATALOG MENU & KERANJANG =================
class MenuCatalogPage extends StatefulWidget {
  final String namaPelanggan;
  final String nomorMeja;

  const MenuCatalogPage({
    super.key,
    required this.namaPelanggan,
    required this.nomorMeja,
  });

  @override
  State<MenuCatalogPage> createState() => _MenuCatalogPageState();
}

class _MenuCatalogPageState extends State<MenuCatalogPage> {
  // Ganti ke IP Laptop jika pakai HP fisik (contoh: 'http://192.168.1.15:5000')
  final String baseUrl = 'http://localhost:5000';
  List<MenuItem> _menus = [];
  String _selectedCategory = 'Semua';
  final Map<String, CartItem> _cart = {};
  bool _isLoading = true;
  bool _isSending = false;

  @override
  void initState() {
    super.initState();
    _fetchMenus();
  }

  Future<void> _fetchMenus() async {
    setState(() => _isLoading = true);
    try {
      final response = await http.get(Uri.parse('$baseUrl/api/menu'));
      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        setState(() {
          _menus = data.map((item) => MenuItem.fromJson(item)).toList();
          if (!_categories.contains(_selectedCategory)) {
            _selectedCategory = 'Semua';
          }
        });
      } else {
        throw Exception('Status ${response.statusCode}');
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Gagal memuat menu dari API: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  List<String> get _categories {
    final cats = _menus.map((m) => m.namaKategori).toSet().toList();
    return ['Semua', ...cats];
  }

  List<MenuItem> get _filteredMenus {
    if (_selectedCategory == 'Semua') return _menus;
    return _menus.where((m) => m.namaKategori == _selectedCategory).toList();
  }

  double get _totalBayar =>
      _cart.values.fold(0, (sum, item) => sum + (item.menu.harga * item.qty));

  int get _totalQty => _cart.values.fold(0, (sum, item) => sum + item.qty);

  void _addToCart(MenuItem menu) {
    final currentQty = _cart[menu.idMenu]?.qty ?? 0;
    if (currentQty >= menu.jumlahStok) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Stok ${menu.namaMenu} tersisa ${menu.jumlahStok}')),
      );
      return;
    }

    setState(() {
      if (_cart.containsKey(menu.idMenu)) {
        _cart[menu.idMenu]!.qty++;
      } else {
        _cart[menu.idMenu] = CartItem(menu: menu);
      }
    });
  }

  void _openCartBottomSheet() {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        return StatefulBuilder(
          builder: (context, setSheetState) {
            return Padding(
              padding: EdgeInsets.only(
                bottom: MediaQuery.of(context).viewInsets.bottom,
                left: 16,
                right: 16,
                top: 16,
              ),
              child: SizedBox(
                height: MediaQuery.of(context).size.height * 0.78,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Keranjang Pesanan',
                          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                        ),
                        Text(
                          '${widget.nomorMeja} • ${widget.namaPelanggan}',
                          style: const TextStyle(color: Colors.brown, fontWeight: FontWeight.w600),
                        ),
                      ],
                    ),
                    const Divider(),
                    Expanded(
                      child: _cart.isEmpty
                          ? const Center(child: Text('Keranjang masih kosong'))
                          : ListView(
                              children: _cart.values.map((item) {
                                return Card(
                                  margin: const EdgeInsets.symmetric(vertical: 6),
                                  color: const Color(0xFFFDFBF9),
                                  child: Padding(
                                    padding: const EdgeInsets.all(12),
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        Row(
                                          children: [
                                            Expanded(
                                              child: Column(
                                                crossAxisAlignment: CrossAxisAlignment.start,
                                                children: [
                                                  Text(
                                                    item.menu.namaMenu,
                                                    style: const TextStyle(
                                                      fontWeight: FontWeight.bold,
                                                      fontSize: 15,
                                                    ),
                                                  ),
                                                  Text(
                                                    'Subtotal: ${formatRupiah(item.menu.harga * item.qty)}',
                                                    style: const TextStyle(color: Colors.brown),
                                                  ),
                                                ],
                                              ),
                                            ),
                                            IconButton(
                                              icon: const Icon(Icons.remove_circle_outline),
                                              onPressed: () {
                                                setState(() {
                                                  if (item.qty > 1) {
                                                    item.qty--;
                                                  } else {
                                                    _cart.remove(item.menu.idMenu);
                                                  }
                                                });
                                                setSheetState(() {});
                                              },
                                            ),
                                            Text(
                                              '${item.qty}',
                                              style: const TextStyle(
                                                fontWeight: FontWeight.bold,
                                                fontSize: 16,
                                              ),
                                            ),
                                            IconButton(
                                              icon: const Icon(Icons.add_circle_outline),
                                              onPressed: () {
                                                if (item.qty < item.menu.jumlahStok) {
                                                  setState(() => item.qty++);
                                                  setSheetState(() {});
                                                } else {
                                                  ScaffoldMessenger.of(context).showSnackBar(
                                                    SnackBar(
                                                      content: Text(
                                                        'Maksimal stok ${item.menu.jumlahStok}',
                                                      ),
                                                    ),
                                                  );
                                                }
                                              },
                                            ),
                                          ],
                                        ),
                                        const SizedBox(height: 8),
                                        TextFormField(
                                          initialValue: item.catatan,
                                          decoration: InputDecoration(
                                            hintText: 'Catatan (cth: Kurang manis, Es dipisah)',
                                            isDense: true,
                                            border: OutlineInputBorder(
                                              borderRadius: BorderRadius.circular(10),
                                            ),
                                          ),
                                          onChanged: (val) => item.catatan = val,
                                        ),
                                      ],
                                    ),
                                  ),
                                );
                              }).toList(),
                            ),
                    ),
                    const Divider(),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text('Total Pembayaran', style: TextStyle(fontSize: 16)),
                        Text(
                          formatRupiah(_totalBayar),
                          style: const TextStyle(
                            fontSize: 19,
                            fontWeight: FontWeight.bold,
                            color: Color(0xFF2E7D32),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    SizedBox(
                      width: double.infinity,
                      height: 48,
                      child: FilledButton.icon(
                        style: FilledButton.styleFrom(
                          backgroundColor: const Color(0xFF2E7D32),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        ),
                        icon: _isSending
                            ? const SizedBox(
                                width: 18,
                                height: 18,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                  color: Colors.white,
                                ),
                              )
                            : const Icon(Icons.send_rounded),
                        label: Text(_isSending ? 'Mengirim...' : 'Kirim Pesanan'),
                        onPressed: (_cart.isEmpty || _isSending)
                            ? null
                            : () {
                                Navigator.pop(ctx);
                                _kirimPesanan();
                              },
                      ),
                    ),
                    const SizedBox(height: 14),
                  ],
                ),
              ),
            );
          },
        );
      },
    );
  }

  Future<void> _kirimPesanan() async {
    setState(() => _isSending = true);

    final payload = {
      'nomorMeja': widget.nomorMeja,
      'namaPelanggan': widget.namaPelanggan,
      'items': _cart.values
          .map((item) => {
                'idMenu': item.menu.idMenu,
                'qty': item.qty,
                'harga': item.menu.harga,
                'catatan': item.catatan,
              })
          .toList(),
    };

    try {
      final response = await http.post(
        Uri.parse('$baseUrl/api/pesanan'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(payload),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final String idPesanan = data['idPesanan'];
        setState(() => _cart.clear());
        await _fetchMenus();

        if (!mounted) return;
        showDialog(
          context: context,
          builder: (_) => AlertDialog(
            title: const Text('Pesanan Terkirim!'),
            content: Text(
              'ID Pesanan: $idPesanan\n'
              'Meja: ${widget.nomorMeja}\n'
              'Pelanggan: ${widget.namaPelanggan}\n\n'
              'Pesanan sudah masuk ke antrean dapur & kasir.',
            ),
            actions: [
              FilledButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('OK'),
              ),
            ],
          ),
        );
      } else {
        throw Exception('Gagal mengirim pesanan (${response.statusCode})');
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error checkout: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isSending = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final visibleMenus = _filteredMenus;

    return Scaffold(
      backgroundColor: const Color(0xFFF8F5F2),
      appBar: AppBar(
        backgroundColor: const Color(0xFF2C1C16),
        foregroundColor: Colors.white,
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('${widget.nomorMeja} • ${widget.namaPelanggan}', style: const TextStyle(fontSize: 16)),
            const Text('Pilih Menu Favoritmu', style: TextStyle(fontSize: 12, color: Colors.white70)),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _fetchMenus,
            tooltip: 'Muat Ulang Menu & Stok',
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : Column(
              children: [
                SizedBox(
                  height: 52,
                  child: ListView.separated(
                    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                    scrollDirection: Axis.horizontal,
                    itemCount: _categories.length,
                    separatorBuilder: (_, _) => const SizedBox(width: 8),
                    itemBuilder: (context, index) {
                      final cat = _categories[index];
                      final isSelected = cat == _selectedCategory;
                      return ChoiceChip(
                        label: Text(cat),
                        selected: isSelected,
                        selectedColor: const Color(0xFF4E342E),
                        labelStyle: TextStyle(
                          color: isSelected ? Colors.white : Colors.black87,
                          fontWeight: FontWeight.w600,
                        ),
                        onSelected: (_) => setState(() => _selectedCategory = cat),
                      );
                    },
                  ),
                ),
                Expanded(
                  child: ListView.builder(
                    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
                    itemCount: visibleMenus.length,
                    itemBuilder: (context, index) {
                      final menu = visibleMenus[index];
                      final available = menu.isAvailable;
                      final inCartQty = _cart[menu.idMenu]?.qty ?? 0;

                      return Card(
                        color: available ? Colors.white : const Color(0xFFF3EFEA),
                        margin: const EdgeInsets.only(bottom: 10),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                        child: Padding(
                          padding: const EdgeInsets.all(14),
                          child: Row(
                            children: [
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      menu.namaMenu,
                                      style: TextStyle(
                                        fontSize: 16,
                                        fontWeight: FontWeight.bold,
                                        color: available ? const Color(0xFF2C1C16) : Colors.grey,
                                      ),
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      '${menu.namaKategori} • ${formatRupiah(menu.harga)}',
                                      style: TextStyle(
                                        color: available ? Colors.brown.shade700 : Colors.grey,
                                      ),
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      available ? 'Stok tersedia: ${menu.jumlahStok}' : 'Stok Habis',
                                      style: TextStyle(
                                        fontSize: 12,
                                        fontWeight: FontWeight.bold,
                                        color: available ? const Color(0xFF2E7D32) : const Color(0xFFC62828),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              !available
                                  ? Container(
                                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                                      decoration: BoxDecoration(
                                        color: const Color(0xFFFFEBEE),
                                        borderRadius: BorderRadius.circular(20),
                                      ),
                                      child: const Text(
                                        'Habis',
                                        style: TextStyle(
                                          color: Color(0xFFC62828),
                                          fontWeight: FontWeight.bold,
                                        ),
                                      ),
                                    )
                                  : FilledButton.icon(
                                      style: FilledButton.styleFrom(
                                        backgroundColor: const Color(0xFF4E342E),
                                        shape: RoundedRectangleBorder(
                                          borderRadius: BorderRadius.circular(10),
                                        ),
                                      ),
                                      icon: const Icon(Icons.add, size: 18),
                                      label: Text(inCartQty > 0 ? 'Tambah ($inCartQty)' : 'Tambah'),
                                      onPressed: () => _addToCart(menu),
                                    ),
                            ],
                          ),
                        ),
                      );
                    },
                  ),
                ),
              ],
            ),
      bottomNavigationBar: _cart.isEmpty
          ? null
          : Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: Colors.white,
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withValues(alpha: 0.08),
                    blurRadius: 8,
                    offset: const Offset(0, -2),
                  ),
                ],
              ),
              child: SafeArea(
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('$_totalQty Item dipilih', style: const TextStyle(fontSize: 12, color: Colors.grey)),
                        Text(
                          formatRupiah(_totalBayar),
                          style: const TextStyle(fontSize: 17, fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                    FilledButton.icon(
                      style: FilledButton.styleFrom(
                        backgroundColor: const Color(0xFF2E7D32),
                        padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      ),
                      icon: const Icon(Icons.shopping_cart_checkout),
                      label: const Text('Keranjang'),
                      onPressed: _openCartBottomSheet,
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}