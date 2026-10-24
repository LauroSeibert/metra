import { formatMoney, parseCents } from './money-input';

describe('parseCents', () => {
  it('reads the typed digits as cents', () => {
    expect(parseCents('1')).toBe(0.01);
    expect(parseCents('10')).toBe(0.1);
    expect(parseCents('100')).toBe(1);
    expect(parseCents('123456')).toBe(1234.56);
  });

  it('ignores the currency symbol, separators and leading zeros of the formatted text', () => {
    expect(parseCents('R$ 0,010')).toBe(0.1);
    expect(parseCents('R$ 1.234,567')).toBe(12345.67);
  });

  it('returns null when there are no digits and keeps zero as a value', () => {
    expect(parseCents('')).toBeNull();
    expect(parseCents('R$ ')).toBeNull();
    expect(parseCents('0')).toBe(0);
  });

  it('caps the number of digits', () => {
    expect(parseCents('9'.repeat(20))).toBe(9_999_999_999_999.99);
  });
});

describe('formatMoney', () => {
  it('formats in reais and leaves null empty', () => {
    expect(formatMoney(1234.5).replace(/\s/g, ' ')).toBe('R$ 1.234,50');
    expect(formatMoney(null)).toBe('');
  });
});
