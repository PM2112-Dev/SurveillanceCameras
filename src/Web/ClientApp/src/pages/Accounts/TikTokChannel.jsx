import { useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, BadgeCheck, Check, Copy, ExternalLink, UserSearch } from 'lucide-react';
import { accountsClient, getErrorMessage } from '../../api/clients';
import { PageHeader } from '../../components/common/PageHeader';
import { ErrorAlert } from '../../components/common/StateBlocks';

const HANDLE_PATTERN = '@?[A-Za-z0-9._]{1,24}';

const numberFormat = new Intl.NumberFormat('vi-VN');
const formatCount = value => numberFormat.format(value ?? 0);

function describeError(err, handle) {
  if (err?.status === 404) return `Không tìm thấy kênh @${handle} trên TikTok.`;

  return getErrorMessage(err);
}

export function TikTokChannel() {
  const { id } = useParams();
  const accountId = Number(id);

  const [accountTitle, setAccountTitle] = useState('');
  const [handleInput, setHandleInput] = useState('');
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [copied, setCopied] = useState(false);

  // Chỉ nhận kết quả của lần tra cứu mới nhất (lần đầu mất vài giây, người dùng có thể bấm lại).
  const latestRequest = useRef(0);

  useEffect(() => {
    accountsClient.getAccountById(accountId)
      .then(account => setAccountTitle(account.title ?? ''))
      .catch(err => setError(getErrorMessage(err)));
  }, [accountId]);

  const handleSubmit = event => {
    event.preventDefault();

    const handle = handleInput.trim().replace(/^@/, '');
    if (!handle) return;

    const requestId = ++latestRequest.current;
    setLoading(true);
    setError('');
    setProfile(null);
    setCopied(false);

    accountsClient.getTikTokUserProfile(accountId, handle)
      .then(result => {
        if (requestId === latestRequest.current) setProfile(result);
      })
      .catch(err => {
        if (requestId === latestRequest.current) setError(describeError(err, handle));
      })
      .finally(() => {
        if (requestId === latestRequest.current) setLoading(false);
      });
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
          pattern={HANDLE_PATTERN}
          maxLength={25}
          title="Chỉ gồm chữ, số, dấu chấm và gạch dưới (tối đa 24 ký tự)"
          placeholder="Handle TikTok, VD: @stardusttv_vietnam"
          value={handleInput}
          onChange={event => setHandleInput(event.target.value)}
          autoFocus
        />
        <button type="submit" disabled={loading} aria-busy={loading}>
          {!loading && <UserSearch size={16} aria-hidden="true" />} Xem thông tin
        </button>
      </form>

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
    </>
  );
}
