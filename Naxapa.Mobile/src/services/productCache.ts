import * as SQLite from 'expo-sqlite';
import type { Product } from '../types/models';

let dbPromise: ReturnType<typeof SQLite.openDatabaseAsync> | null = null;

async function db() {
  if (!dbPromise) {
    dbPromise = SQLite.openDatabaseAsync('naxapa_mobile.db');
  }
  const database = await dbPromise;
  await database.execAsync(`
    PRAGMA journal_mode = WAL;
    CREATE TABLE IF NOT EXISTS products (
      id INTEGER PRIMARY KEY NOT NULL,
      code TEXT,
      name TEXT NOT NULL,
      category TEXT NOT NULL,
      price REAL NOT NULL,
      unit TEXT NOT NULL,
      available INTEGER NOT NULL,
      image_url TEXT,
      updated_at TEXT NOT NULL
    );
    CREATE INDEX IF NOT EXISTS ix_products_name ON products(name);
  `);
  return database;
}

export async function replaceProducts(products: Product[]): Promise<void> {
  const database = await db();
  await database.withTransactionAsync(async () => {
    await database.runAsync('DELETE FROM products');
    for (const product of products) {
      await database.runAsync(
        `INSERT INTO products
          (id, code, name, category, price, unit, available, image_url, updated_at)
         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`,
        product.id,
        product.code ?? null,
        product.name,
        product.category,
        product.price,
        product.unit,
        product.available ? 1 : 0,
        product.imageUrl ?? null,
        new Date().toISOString(),
      );
    }
  });
}

type ProductRow = {
  id: number;
  code: string | null;
  name: string;
  category: string;
  price: number;
  unit: string;
  available: number;
  image_url: string | null;
};

export async function cachedProducts(query = ''): Promise<Product[]> {
  const database = await db();
  const term = query.trim();
  const rows = term
    ? await database.getAllAsync<ProductRow>(
        `SELECT id, code, name, category, price, unit, available, image_url
           FROM products
          WHERE name LIKE ? OR code LIKE ?
          ORDER BY category, name`,
        `%${term}%`, `%${term}%`,
      )
    : await database.getAllAsync<ProductRow>(
        `SELECT id, code, name, category, price, unit, available, image_url
           FROM products
          ORDER BY category, name`,
      );

  return rows.map((row) => ({
    id: row.id,
    code: row.code,
    name: row.name,
    category: row.category,
    price: Number(row.price),
    unit: row.unit,
    available: row.available === 1,
    imageUrl: row.image_url,
  }));
}
