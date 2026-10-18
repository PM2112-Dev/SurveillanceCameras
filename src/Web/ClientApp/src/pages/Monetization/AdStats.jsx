import { useMemo, useState } from 'react';
import { monetizationClient } from '../../api/clients';
import { useAsyncData } from '../../hooks/useAsyncData';
import { PageHeader } from '../../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';
import { formatLabel } from './AdPlacements';

const iso = date => date.toISOString().slice(0, 10);
const daysAgo = n => { const d = new Date(); d.setUTCDate(d.getUTCDate() - n); return iso(d); };
// Ngày UTC 00:00 để máy chủ đọc đúng ngày đã chọn.
const utc = value => new Date(`${value}T00:00:00Z`);
const num = n => (n ?? 0).toLocaleString('vi-VN');
const pct = (part, whole) => (whole > 0 ? `${((part / whole) * 100).toFixed(1)}%` : '—');
const money = (revenue, currency) => `${(revenue / 1e6).toLocaleString('vi-VN', { maximumFractionDigits: 2, minimumFractionDigits: 2 })} ${currency}`;
const revenueText = totals => {
  const entries = Object.entries(totals.revenue ?? {});
  return entries.length ? entries.map(([c, v]) => money(v, c)).join(' + ') : '—';
};
const ecpm = totals => {
  const entries = Object.entries(totals.revenue ?? {});
  return totals.impressions > 0 && entries.length
    ? entries.map(([c, v]) => money((v / totals.impressions) * 1000, c)).join(' + ')
    : '—';
};
const delta = (now, before) => {
  if (!before) return now ? 'mới' : '';
  const change = ((now - before) / before) * 100;
  return `${change >= 0 ? '▲' : '▼'} ${Math.abs(change).toFixed(0)}%`;
};

const columns = [
  ['Request', t => num(t.requests)],
  ['Lấp đầy', t => pct(t.filled, t.requests)],
  ['Lỗi', t => num(t.failed)],
  ['Hiển thị', t => num(t.impressions)],
  ['Tỷ lệ hiện', t => pct(t.impressions, t.requests)],
  ['Bấm', t => num(t.clicks)],
  ['CTR', t => pct(t.clicks, t.impressions)],
  ['Thưởng', t => num(t.rewarded)],
  ['Tắt', t => num(t.dismissed)],
  ['Doanh thu ước tính', revenueText],
  ['eCPM', ecpm]
];

function Breakdown({ title, rows, keyLabel, name }) {
  if (!rows?.length) return null;
  return (
    <section style={{ marginTop: 'var(--pico-spacing)' }}>
      <h3>{title}</h3>
      <div className="table-wrap">
        <table>
          <thead><tr><th>{keyLabel}</th>{columns.map(([label]) => <th key={label}>{label}</th>)}</tr></thead>
          <tbody>
            {rows.map(row => (
              <tr key={row.key}>
                <td>{name(row)}{row.lastError && <span className="cell-sub" title={row.lastError}>Lỗi gần nhất: {row.lastError}</span>}</td>
                {columns.map(([label, render]) => <td key={label}>{render(row.totals)}</td>)}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function toCsv(rows, name) {
  const header = ['Mục', ...columns.map(([label]) => label)];
  const lines = rows.map(row => [name(row), ...columns.map(([, render]) => render(row.totals))]);
  return [header, ...lines].map(line => line.map(cell => `"${String(cell).replaceAll('"', '""')}"`).join(',')).join('\n');
}

export function AdStats() {
  const [filters, setFilters] = useState({ from: daysAgo(29), to: daysAgo(0), platform: '', screen: '', placement: '', format: '' });
  const [applied, setApplied] = useState(filters);
  const placements = useAsyncData(() => monetizationClient.getAdPlacements(), []);
  const screens = useAsyncData(() => monetizationClient.getAdScreens(), []);
  const { data, loading, error } = useAsyncData(() => monetizationClient.getAdStats(
    utc(applied.from), utc(applied.to), applied.platform || undefined, applied.screen || undefined,
    applied.placement || undefined, applied.format || undefined), [applied]);

  const maxImpressions = useMemo(() => Math.max(1, ...(data?.byDay ?? []).map(d => d.totals.impressions)), [data]);
  const set = patch => setFilters(current => ({ ...current, ...patch }));
  const preset = days => { const next = { ...filters, from: daysAgo(days - 1), to: daysAgo(0) }; setFilters(next); setApplied(next); };
  const dayName = row => new Date(`${row.key}T00:00:00Z`).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', timeZone: 'UTC' });
  const placementName = row => <>{row.label ?? row.key} <code>{row.key}</code><span className="cell-sub">{formatLabel[row.format] ?? row.format}</span></>;
  const screenName = row => <>{row.label ?? row.key} <code>{row.key}</code></>;
  const download = () => {
    const blob = new Blob(['﻿' + toCsv(data.byDay, dayName)], { type: 'text/csv;charset=utf-8' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `ad-stats-${applied.from}_${applied.to}.csv`;
    link.click();
    URL.revokeObjectURL(link.href);
  };

  const totals = data?.totals;
  const previous = data?.previous;
  const cards = totals && [
    ['Request', num(totals.requests), delta(totals.requests, previous.requests)],
    ['Tỷ lệ lấp đầy', pct(totals.filled, totals.requests), `${num(totals.filled)} trả quảng cáo · ${num(totals.failed)} lỗi`],
    ['Lượt hiển thị', num(totals.impressions), delta(totals.impressions, previous.impressions)],
    ['CTR', pct(totals.clicks, totals.impressions), `${num(totals.clicks)} lượt bấm`],
    ['Doanh thu ước tính', revenueText(totals), delta(Object.values(totals.revenue ?? {}).reduce((a, b) => a + b, 0), Object.values(previous.revenue ?? {}).reduce((a, b) => a + b, 0))],
    ['eCPM', ecpm(totals), `${num(totals.rewarded)} lượt nhận thưởng`]
  ];

  return (
    <>
      <PageHeader title="Thống kê quảng cáo" description="Số request, lấp đầy, hiển thị, bấm và doanh thu theo ngày, vị trí, màn hình và nền tảng. Ngày tính theo giờ UTC." />
      <form className="filter-bar" onSubmit={e => { e.preventDefault(); setApplied(filters); }}>
        <input type="date" value={filters.from} max={filters.to} onChange={e => set({ from: e.target.value })} />
        <input type="date" value={filters.to} min={filters.from} onChange={e => set({ to: e.target.value })} />
        <select value={filters.platform} onChange={e => set({ platform: e.target.value })}>
          <option value="">Mọi nền tảng</option><option value="android">Android</option><option value="ios">iOS</option>
        </select>
        <select value={filters.format} onChange={e => set({ format: e.target.value })}>
          <option value="">Mọi loại</option>
          {Object.entries(formatLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select>
        <select value={filters.screen} onChange={e => set({ screen: e.target.value })}>
          <option value="">Mọi màn</option>
          {screens.data?.map(s => <option key={s.code} value={s.code}>{s.title}</option>)}
        </select>
        <select value={filters.placement} onChange={e => set({ placement: e.target.value })}>
          <option value="">Mọi vị trí</option>
          {placements.data?.map(p => <option key={p.code} value={p.code}>{p.title}</option>)}
        </select>
        <button type="submit">Xem</button>
        <button type="button" className="secondary" onClick={() => preset(7)}>7 ngày</button>
        <button type="button" className="secondary" onClick={() => preset(30)}>30 ngày</button>
        <button type="button" className="secondary" onClick={() => preset(90)}>90 ngày</button>
        <button type="button" className="secondary" onClick={download} disabled={!data}>Tải CSV</button>
      </form>

      {loading ? <LoadingBlock /> : <ErrorAlert message={error} />}
      {data && (
        <>
          <div className="stat-grid">
            {cards.map(([label, value, note]) => (
              <div className="stat-card stat-card--static" key={label}>
                <span className="stat-value">{value}</span><span className="stat-label">{label}</span>
                {note && <span className="cell-sub">{note}{note.startsWith('▲') || note.startsWith('▼') ? ' so với kỳ trước' : ''}</span>}
              </div>
            ))}
          </div>

          <section style={{ marginTop: 'var(--pico-spacing)' }}>
            <h3>Theo ngày</h3>
            <div className="table-wrap">
              <table>
                <thead><tr><th>Ngày</th>{columns.map(([label]) => <th key={label}>{label}</th>)}<th>Hiển thị</th></tr></thead>
                <tbody>
                  {[...data.byDay].reverse().map(row => (
                    <tr key={row.key}>
                      <td>{dayName(row)}</td>
                      {columns.map(([label, render]) => <td key={label}>{render(row.totals)}</td>)}
                      <td style={{ minWidth: 120 }}>
                        <div style={{ background: 'var(--pico-primary)', height: 8, borderRadius: 4, width: `${(row.totals.impressions / maxImpressions) * 100}%` }} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          <Breakdown title="Theo vị trí quảng cáo" rows={data.byPlacement} keyLabel="Vị trí" name={placementName} />
          <Breakdown title="Theo màn hình" rows={data.byScreen} keyLabel="Màn" name={screenName} />
          <Breakdown title="Theo loại quảng cáo" rows={data.byFormat} keyLabel="Loại" name={row => formatLabel[row.key] ?? row.key} />
          <Breakdown title="Theo nền tảng" rows={data.byPlatform} keyLabel="Nền tảng" name={row => (row.key === 'ios' ? 'iOS' : 'Android')} />
          {data.totals.requests === 0 && <p className="hint">Chưa có số liệu trong khoảng này. App gửi thống kê sau vài giây khi có quảng cáo và khi chuyển ứng dụng xuống nền.</p>}
        </>
      )}
    </>
  );
}
