import { formatDocument, isValidDocument, normalizeDocument } from './partner-rules';

describe('partner rules', () => {
  it('accepts valid CPF and CNPJ, including the alphanumeric CNPJ of 2026', () => {
    expect(isValidDocument('529.982.247-25')).toBe(true);
    expect(isValidDocument('11.222.333/0001-81')).toBe(true);
    expect(isValidDocument('12.ABC.345/01DE-35')).toBe(true);
    expect(isValidDocument('12abc34501de35')).toBe(true);
  });

  it('rejects wrong check digits, repeated digits and other lengths', () => {
    expect(isValidDocument('529.982.247-24')).toBe(false);
    expect(isValidDocument('111.111.111-11')).toBe(false);
    expect(isValidDocument('11.222.333/0001-82')).toBe(false);
    expect(isValidDocument('12.ABC.345/01DE-3A')).toBe(false);
    expect(isValidDocument('123')).toBe(false);
  });

  it('normalizes and formats with the official masks', () => {
    expect(normalizeDocument(' 12.abc.345/01de-35 ')).toBe('12ABC34501DE35');
    expect(formatDocument('52998224725')).toBe('529.982.247-25');
    expect(formatDocument('12ABC34501DE35')).toBe('12.ABC.345/01DE-35');
    expect(formatDocument('123')).toBe('123');
  });
});
