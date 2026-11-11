export type Session = {
  token: string;
  expiresAtUtc: string;
  userId: number;
  userName: string;
  estabelecimentoId: number;
  estabelecimento: string;
};

export type Product = {
  id: number;
  code?: string | null;
  name: string;
  category: string;
  price: number;
  unit: string;
  available: boolean;
  imageUrl?: string | null;
};

export type ProductOption = {
  id: number;
  name: string;
  additionalValue: number;
};

export type OptionGroup = {
  id: number;
  name: string;
  selectionType: string;
  minimum: number;
  maximum: number;
  required: boolean;
  options: ProductOption[];
};

export type ProductDetail = Product & {
  description?: string | null;
  optionGroups: OptionGroup[];
};

export type CartItem = {
  key: string;
  product: Product;
  quantity: number;
  options: ProductOption[];
};

export type OrderSummary = {
  id: number;
  number: number;
  status: string;
  financialStatus: string;
  total: number;
  createdAtUtc: string;
};

export type PageResult<T> = {
  items: T[];
  page?: number;
  pageSize?: number;
  total?: number;
};
