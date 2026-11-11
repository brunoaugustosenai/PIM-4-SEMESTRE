export const money = (value: number) =>
  new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  }).format(Number.isFinite(value) ? value : 0);

export const cartUnitTotal = (price: number, additions: number[]) =>
  additions.reduce((sum, value) => sum + value, price);
