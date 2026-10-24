import { previewInstallments } from './entry-form';

describe('previewInstallments', () => {
  it('splits like the server: remainder cents on the first installment', () => {
    const installments = previewInstallments(100, 3, '2026-01-31');

    expect(installments.map((i) => i.amount)).toEqual([33.34, 33.33, 33.33]);
    expect(installments.reduce((sum, i) => sum + Math.round(i.amount * 100), 0)).toBe(10_000);
  });

  it('keeps monthly due dates, clamping the day to the end of shorter months', () => {
    const installments = previewInstallments(90, 3, '2026-01-31');

    expect(installments.map((i) => i.dueDate)).toEqual(['2026-01-31', '2026-02-28', '2026-03-31']);
  });

  it('returns nothing for invalid input', () => {
    expect(previewInstallments(0, 3, '2026-01-10')).toEqual([]);
    expect(previewInstallments(100, 0, '2026-01-10')).toEqual([]);
    expect(previewInstallments(100, 61, '2026-01-10')).toEqual([]);
    expect(previewInstallments(100, 2, '')).toEqual([]);
  });
});
