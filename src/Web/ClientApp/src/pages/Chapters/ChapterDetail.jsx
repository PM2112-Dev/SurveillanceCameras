import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, ExternalLink, Pencil, Sparkles } from 'lucide-react';
import { episodesClient, getErrorMessage } from '../../api/clients';
import { PageHeader } from '../../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';
import { useAsyncData } from '../../hooks/useAsyncData';
import { ChapterStatusBadge } from './chapterStatus';

const dateFormat = new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' });

function Paragraphs({ text, empty }) {
  if (!text) return <p className="hint">{empty}</p>;
  return text.split('\n').map((line, index) => <p key={index}>{line}</p>);
}

/** Chương: nội dung gốc và nội dung AI đã biên tập đặt cạnh nhau để so sánh. */
export function ChapterDetail() {
  const { storyId, id } = useParams();
  const [reloadKey, setReloadKey] = useState(0);
  const chapter = useAsyncData(() => episodesClient.getEpisodeById(Number(id)), [id, reloadKey]);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const info = chapter.data;

  const requestEdit = () => {
    setBusy(true);
    setMessage('');
    setError('');
    episodesClient.requestEpisodeEdit(Number(id))
      .then(() => setMessage('Đã gửi yêu cầu — AI biên tập ở nền, bấm “Tải lại” sau ít phút để xem kết quả.'))
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setBusy(false));
  };

  return (
    <>
      <PageHeader
        title={info ? `Chương ${info.episodeNumber}: ${info.nameEdit || info.nameRaw || info.title || ''}` : 'Chương'}
        actions={
          <>
            {info?.contentRaw && (
              <button type="button" className="secondary" onClick={requestEdit} disabled={busy} aria-busy={busy}>
                {!busy && <Sparkles size={16} aria-hidden="true" />} AI biên tập lại
              </button>
            )}
            {info?.linkRaw && (
              <a href={info.linkRaw} target="_blank" rel="noreferrer" role="button" className="secondary">
                <ExternalLink size={16} aria-hidden="true" /> Trang gốc
              </a>
            )}
            <Link to={`/stories/${storyId}/chapters/${id}/edit`} role="button" className="secondary">
              <Pencil size={16} aria-hidden="true" /> Sửa
            </Link>
            <Link to={`/stories/${storyId}`} role="button" className="secondary">
              <ArrowLeft size={16} aria-hidden="true" /> Truyện
            </Link>
          </>
        }
      />

      <ErrorAlert message={chapter.error || error} />
      {message && (
        <p className="success" role="status">
          {message}{' '}
          <button type="button" className="link-button" onClick={() => setReloadKey(key => key + 1)}>Tải lại</button>
        </p>
      )}

      {chapter.loading && !info ? (
        <LoadingBlock />
      ) : info && (
        <>
          <div className="badge-row chapter-meta">
            <ChapterStatusBadge status={info.status} error={info.error} />
            {info.fetchedAt && <small>Tải: {dateFormat.format(info.fetchedAt)}</small>}
            {info.editedAt && <small>Biên tập: {dateFormat.format(info.editedAt)}{info.promptTitle ? ` (prompt “${info.promptTitle}”)` : ''}</small>}
          </div>
          {info.error && <p className="error">{info.error}</p>}

          <div className="chapter-compare">
            <article>
              <h3>Bản gốc {info.nameRaw && <small>— {info.nameRaw}</small>}</h3>
              <Paragraphs text={info.contentRaw} empty="Chưa tải nội dung." />
            </article>
            <article>
              <h3>Đã biên tập {info.nameEdit && <small>— {info.nameEdit}</small>}</h3>
              <Paragraphs text={info.contentEdit} empty="Chưa biên tập." />
            </article>
          </div>
        </>
      )}
    </>
  );
}
