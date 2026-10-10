// Trùng giá trị với enum WebSourceType ở backend (Domain/Enums/WebSourceType.cs).
export const WEB_SOURCE_TYPES = [
  { value: 1, label: 'Wikidich (zzz)' },
  { value: 2, label: 'Wikidich' },
  { value: 3, label: 'Wikicv' }
];

export const webSourceTypeLabel = value =>
  WEB_SOURCE_TYPES.find(type => type.value === value)?.label ?? null;
