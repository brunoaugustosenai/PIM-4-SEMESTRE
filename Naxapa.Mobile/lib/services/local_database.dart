import 'package:path/path.dart';
import 'package:sqflite/sqflite.dart';

import '../models/models.dart';

class LocalDatabase {
  LocalDatabase._();

  static final LocalDatabase instance = LocalDatabase._();

  Database? _db;

  Future<Database> get db async => _db ??= await _open();

  Future<Database> _open() async {
    final databasePath = join(
      await getDatabasesPath(),
      'naxapa_mobile.db',
    );

    return openDatabase(
      databasePath,
      version: 1,
      onCreate: (database, _) async {
        await database.execute('''
          CREATE TABLE products(
            id INTEGER PRIMARY KEY,
            code TEXT,
            name TEXT NOT NULL,
            category TEXT NOT NULL,
            price REAL NOT NULL,
            unit TEXT NOT NULL,
            available INTEGER NOT NULL,
            image_url TEXT,
            updated_at TEXT NOT NULL
          )
        ''');
        await database.execute(
          'CREATE INDEX ix_products_name ON products(name)',
        );
        await database.execute('''
          CREATE TABLE sync_meta(
            key TEXT PRIMARY KEY,
            value TEXT NOT NULL
          )
        ''');
      },
    );
  }

  Future<void> replaceProducts(List<Product> products) async {
    final database = await db;
    await database.transaction((transaction) async {
      await transaction.delete('products');
      final batch = transaction.batch();
      for (final product in products) {
        batch.insert(
          'products',
          product.toDb(),
          conflictAlgorithm: ConflictAlgorithm.replace,
        );
      }
      await batch.commit(noResult: true);
      await transaction.insert(
        'sync_meta',
        {
          'key': 'products_last_sync',
          'value': DateTime.now().toUtc().toIso8601String(),
        },
        conflictAlgorithm: ConflictAlgorithm.replace,
      );
    });
  }

  Future<List<Product>> products({String query = ''}) async {
    final database = await db;
    final term = query.trim();
    final rows = await database.query(
      'products',
      where: term.isEmpty ? null : 'name LIKE ? OR code LIKE ?',
      whereArgs: term.isEmpty ? null : ['%$term%', '%$term%'],
      orderBy: 'category, name',
    );
    return rows.map(Product.fromDb).toList(growable: false);
  }

  Future<DateTime?> lastProductSync() async {
    final database = await db;
    final rows = await database.query(
      'sync_meta',
      where: 'key = ?',
      whereArgs: const ['products_last_sync'],
      limit: 1,
    );
    if (rows.isEmpty) return null;
    return DateTime.tryParse(rows.first['value'] as String);
  }
}
