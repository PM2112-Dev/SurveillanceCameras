import { useState } from 'react';
import { SetMovieAccessRequest } from '../../web-api-client';
import { getErrorMessage, moviesClient } from '../../api/clients';

/** Khoảng tập quảng cáo / VIP đang áp dụng: cấu hình riêng của phim, không thì mặc định của hệ thống. */
export function effectiveAccessRule(movie, config) {
  if (movie?.hasCustomAccess) {
    return { adFrom: movie.adFromEpisode, adTo: movie.adToEpisode, vipFrom: movie.vipFromEpisode, vipTo: movie.vipToEpisode };
  }
  if (!config) return null;
  const free = Math.max(config.freeEpisodes ?? 0, 0);
  const ads = Math.max(config.adEpisodes ?? 0, 0);
  return { adFrom: ads > 0 ? free + 1 : null, adTo: ads > 0 ? free + ads : null, vipFrom: free + ads + 1, vipTo: null };
}

const inRange = (from, to, number) => from != null && number >= from && (to == null || number <= to);

/** 'vip' | 'ads' | 'free' — tập nằm trong cả hai khoảng là VIP (giống máy chủ). */
export function episodeAccess(rule, number) {
  if (!rule) return null;
  if (inRange(rule.vipFrom, rule.vipTo, number)) return 'vip';
  if (inRange(rule.adFrom, rule.adTo, number)) return 'ads';
  return 'free';
}

const rangeText = (from, to) => (from == null ? 'không có' : to == null ? `từ tập ${from} đến hết phim` : from === to ? `tập ${from}` : `tập ${from}–${to}`);

const toInput = value => (value == null ? '' : String(value));
const toNumber = value => (value === '' ? undefined : Number(value));

/** Đặt khoảng tập mở bằng quảng cáo và khoảng tập VIP của một phim trên app. */
export function MovieAccessForm({ movie, config, onSaved }) {
  const [custom, setCustom] = useState(Boolean(movie.hasCustomAccess));
  const [hasAds, setHasAds] = useState(movie.adFromEpisode != null);
  const [hasVip, setHasVip] = useState(movie.vipFromEpisode != null);
  const [form, setForm] = useState({
    adFrom: toInput(movie.adFromEpisode), adTo: toInput(movie.adToEpisode),
    vipFrom: toInput(movie.vipFromEpisode), vipTo: toInput(movie.vipToEpisode)
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [saved, setSaved] = useState(false);

  const update = patch => { setForm(current => ({ ...current, ...patch })); setSaved(false); };
  const rule = effectiveAccessRule(movie, config);

  const submit = event => {
    event.preventDefault();
    if (custom && ((hasAds && form.adFrom === '') || (hasVip && form.vipFrom === ''))) {
      setError('Nhập tập bắt đầu cho khoảng đã bật.');
      return;
    }
    setSaving(true);
    setError('');
    moviesClient.setMovieAccess(movie.id, new SetMovieAccessRequest({
      useDefault: !custom,
      adFromEpisode: custom && hasAds ? toNumber(form.adFrom) : undefined,
      adToEpisode: custom && hasAds ? toNumber(form.adTo) : undefined,
      vipFromEpisode: custom && hasVip ? toNumber(form.vipFrom) : undefined,
      vipToEpisode: custom && hasVip ? toNumber(form.vipTo) : undefined
    }))
      .then(() => { setSaved(true); onSaved(); })
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setSaving(false));
  };

  const range = (label, enabled, setEnabled, fromKey, toKey) => (
    <fieldset>
      <label>
        <input type="checkbox" checked={enabled} onChange={event => { setEnabled(event.target.checked); setSaved(false); }} />
        {label}
      </label>
      {enabled && (
        <div className="grid">
          <label>
            Từ tập
            <input type="number" min="1" step="1" required value={form[fromKey]}
              onChange={event => update({ [fromKey]: event.target.value })} />
          </label>
          <label>
            Đến tập
            <input type="number" min={form[fromKey] || 1} step="1" value={form[toKey]} placeholder="hết phim"
              onChange={event => update({ [toKey]: event.target.value })} />
          </label>
        </div>
      )}
    </fieldset>
  );

  return (
    <section className="profile-card">
      <h2>Quyền xem trên app</h2>
      {rule && (
        <p className="cell-sub">
          Đang áp dụng{movie.hasCustomAccess ? ' (cấu hình riêng)' : ' (mặc định hệ thống)'}: quảng cáo {rangeText(rule.adFrom, rule.adTo)};
          {' '}VIP {rangeText(rule.vipFrom, rule.vipTo)}; các tập còn lại miễn phí.
        </p>
      )}
      <form onSubmit={submit}>
        <fieldset>
          <label>
            <input type="radio" name="access-mode" checked={!custom} onChange={() => { setCustom(false); setSaved(false); }} />
            Dùng mặc định của hệ thống
          </label>
          <label>
            <input type="radio" name="access-mode" checked={custom} onChange={() => { setCustom(true); setSaved(false); }} />
            Đặt riêng cho phim này
          </label>
        </fieldset>
        {custom && (
          <>
            {range('Có tập mở bằng quảng cáo', hasAds, setHasAds, 'adFrom', 'adTo')}
            {range('Có tập chỉ dành cho VIP', hasVip, setHasVip, 'vipFrom', 'vipTo')}
            <small>
              Tập ngoài hai khoảng thì miễn phí. Tập nằm trong cả hai khoảng được tính là VIP. Để trống ô “Đến tập” nghĩa là
              tới hết phim.
            </small>
          </>
        )}
        {error && <p className="error">{error}</p>}
        <button type="submit" disabled={saving} aria-busy={saving}>Lưu quyền xem</button>
        {saved && <span className="cell-sub"> Đã lưu.</span>}
      </form>
    </section>
  );
}
