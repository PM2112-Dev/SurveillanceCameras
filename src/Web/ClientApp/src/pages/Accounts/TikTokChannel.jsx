import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, BadgeCheck, Check, Copy, ExternalLink, UserSearch } from 'lucide-react';
import { accountsClient, getErrorMessage, userProfilesClient } from '../../api/clients';
import { PageHeader } from '../../components/common/PageHeader';
import { ErrorAlert } from '../../components/common/StateBlocks';
import { DramaList } from './DramaList';

// Nhập handle (@tên) hoặc secUid trực tiếp — secUid thì bỏ qua bước mở Chrome để tra handle.
const SEC_UID_REGEX = /^MS4w[A-Za-z0-9_-]{20,}$/;
const INPUT_PATTERN = '@?[A-Za-z0-9._]{1,24}|MS4w[A-Za-z0-9_\\-]{20,}';

// Mã nền tảng TikTok, trùng với UserProfileTypeCodes.TikTok ở backend.
const TIKTOK_TYPE_CODE = 'tiktok';
const SAVED_CHANNEL_LIMIT = 12;

const numberFormat = new Intl.NumberFormat('vi-VN');
const formatCount = value => numberFormat.format(value ?? 0);

function describeError(err, handle) {
  if (err?.status === 404) return `Không tìm thấy kênh @${handle} trên TikTok.`;

  return getErrorMessage(err);
}

/** Kênh đã lưu (UserProfileDto) có cùng dữ liệu với kết quả tra cứu, chỉ khác tên trường của tên kênh. */
const savedToProfile = saved => ({ ...saved, nickname: saved.title });

export function TikTokChannel() {
  const { id } = useParams();
  const accountId = Number(id);

  const [accountTitle, setAccountTitle] = useState('');
  const [handleInput, setHandleInput] = useState('');
  const [profile, setProfile] = useState(null);
  const [secUid, setSecUid] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [copied, setCopied] = useState(false);
  const [savedChannels, setSavedChannels] = useState([]);

  // Chỉ nhận kết quả của lần tra cứu mới nhất (lần đầu mất vài giây, người dùng có thể bấm lại).
  const latestRequest = useRef(0);

  // Lỗi tải kênh đã lưu được bỏ qua: đây chỉ là lối tắt, không được chặn việc tra cứu.
  const loadSavedChannels = useCallback(() => {
    userProfilesClient.getUserProfiles(undefined, undefined, TIKTOK_TYPE_CODE, 1, SAVED_CHANNEL_LIMIT)
      .then(result => setSavedChannels(result.items ?? []))
      .catch(() => {});
  }, []);

  useEffect(loadSavedChannels, [loadSavedChannels]);

  useEffect(() => {
    accountsClient.getAccountById(accountId)
      .then(account => setAccountTitle(account.title ?? ''))
      .catch(err => setError(getErrorMessage(err)));
  }, [accountId]);

  const handleSubmit = event => {
    event.preventDefault();

    const value = handleInput.trim().replace(/^@/, '');
    if (!value) return;

    const requestId = ++latestRequest.current;
    setError('');
    setProfile(null);
    setSecUid('');
    setCopied(false);

    if (SEC_UID_REGEX.test(value)) {
      setLoading(false);
      setSecUid(value);
      return;
    }

    const handle = value;
    setLoading(true);

    accountsClient.searchTikTokUserProfile(accountId, handle)
      .then(result => {
        if (requestId !== latestRequest.current) return;
        setProfile(result);
        setSecUid(result.secUid ?? '');
        // Server vừa lưu/cập nhật kênh này.
        loadSavedChannels();
      })
      .catch(err => {
        if (requestId === latestRequest.current) setError(describeError(err, handle));
      })
      .finally(() => {
        if (requestId === latestRequest.current) setLoading(false);
      });
  };

  // Mở kênh đã lưu: dùng secUid có sẵn, không cần mở Chrome tra cứu lại.
  const openSavedChannel = saved => {
    latestRequest.current++;
    setLoading(false);
    setError('');
    setCopied(false);
    setHandleInput(saved.uniqueId ? `@${saved.uniqueId}` : '');
    setProfile(savedToProfile(saved));
    setSecUid(saved.secUid ?? '');
  };

  const copySecUid = () => {
    navigator.clipboard.writeText(profile.secUid ?? '')
      .then(() => {
        setCopied(true);
        setTimeout(() => setCopied(false), 1500);
      })
      .catch(() => setError('Không sao chép được, hãy chọn và sao chép thủ công.'));
  };

  return (
    <>
      <PageHeader
        title="Kênh TikTok"
        description={accountTitle ? `Tra cứu bằng cookie của tài khoản “${accountTitle}”.` : 'Tra cứu thông tin kênh theo handle.'}
        actions={
          <Link to="/accounts" role="button" className="secondary">
            <ArrowLeft size={16} aria-hidden="true" /> Tài khoản
          </Link>
        }
      />

      <form className="filter-bar" onSubmit={handleSubmit}>
        <input
          type="text"
          required
          pattern={INPUT_PATTERN}
          maxLength={120}
          title="Handle (chữ, số, dấu chấm, gạch dưới; tối đa 24 ký tự) hoặc secUid bắt đầu bằng MS4w"
          placeholder="Handle hoặc secUid, VD: @stardusttv_vietnam"
          value={handleInput}
          onChange={event => setHandleInput(event.target.value)}
          autoFocus
        />
        <button type="submit" disabled={loading} aria-busy={loading}>
          {!loading && <UserSearch size={16} aria-hidden="true" />} Tra cứu
        </button>
      </form>

      {savedChannels.length > 0 && (
        <div className="saved-channels">
          <small>Kênh đã tra cứu (bấm để mở lại, dữ liệu theo lần tra cứu gần nhất):</small>
          <div className="chip-row">
            {savedChannels.filter(saved => saved.secUid).map(saved => (
              <button
                key={saved.id}
                type="button"
                className="chip-button"
                aria-pressed={saved.secUid === secUid}
                onClick={() => openSavedChannel(saved)}
              >
                @{saved.uniqueId || saved.title}
              </button>
            ))}
          </div>
        </div>
      )}

      {loading && (
        <p className="hint">
          Server đang mở Chrome để tải trang kênh — lần đầu có thể mất vài giây…
        </p>
      )}

      <ErrorAlert message={error} />

      {profile && (
        <section className="profile-card" aria-label="Thông tin kênh">
          <div className="profile-head">
            {profile.avatarUrl && (
              <img
                className="profile-avatar"
                src={profile.avatarUrl}
                alt=""
                referrerPolicy="no-referrer"
                onError={event => { event.currentTarget.style.visibility = 'hidden'; }}
              />
            )}
            <div>
              <h2 className="profile-name">
                {profile.nickname || profile.uniqueId}
                {profile.verified && (
                  <BadgeCheck size={20} className="profile-verified" aria-label="Đã xác minh" />
                )}
              </h2>
              <a
                className="profile-handle"
                href={`https://www.tiktok.com/@${profile.uniqueId}`}
                target="_blank"
                rel="noreferrer"
              >
                @{profile.uniqueId} <ExternalLink size={14} aria-hidden="true" />
              </a>
              {profile.signature && <p className="profile-bio">{profile.signature}</p>}
            </div>
          </div>

          <div className="stat-grid">
            <div className="stat-card stat-card--static">
              <span className="stat-value">{formatCount(profile.followerCount)}</span>
              <span className="stat-label">Người theo dõi</span>
            </div>
            <div className="stat-card stat-card--static">
              <span className="stat-value">{formatCount(profile.followingCount)}</span>
              <span className="stat-label">Đang theo dõi</span>
            </div>
            <div className="stat-card stat-card--static">
              <span className="stat-value">{formatCount(profile.heartCount)}</span>
              <span className="stat-label">Lượt thích</span>
            </div>
            <div className="stat-card stat-card--static">
              <span className="stat-value">{formatCount(profile.videoCount)}</span>
              <span className="stat-label">Video</span>
            </div>
          </div>

          <dl className="profile-ids">
            <dt>secUid</dt>
            <dd>
              <code>{profile.secUid}</code>
              <button type="button" className="icon-btn" title="Sao chép secUid" onClick={copySecUid}>
                {copied ? <Check size={16} aria-hidden="true" /> : <Copy size={16} aria-hidden="true" />}
              </button>
            </dd>
            <dt>User ID</dt>
            <dd><code>{profile.userId}</code></dd>
          </dl>
        </section>
      )}

      {secUid && <DramaList accountId={accountId} secUid={secUid} />}
    </>
  );
}
