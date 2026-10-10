import wikidichzz from './wikidichzz.json';
import wikicv from './wikicv.json';

// Cấu hình crawl mẫu đã chạy thử với trang thật (test LiveCrawl ở Infrastructure.IntegrationTests dùng chung các file này).
export const CRAWL_PRESETS = [
  { key: 'wikidichzz', label: 'Wikidichzz', config: wikidichzz },
  {
    key: 'wikicv',
    label: 'Wikicv',
    config: wikicv,
    note: 'Trang xếp hạng/tìm kiếm cần cookie đăng nhập; phân trang danh sách chưa kiểm chứng.'
  }
];
