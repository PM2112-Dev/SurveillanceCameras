import wikidichzz from './wikidichzz.json';
import wikicv from './wikicv.json';

// Cấu hình crawl mẫu đã chạy thử với trang thật (test LiveCrawl ở Infrastructure.IntegrationTests dùng chung các file này).
export const CRAWL_PRESETS = [
  { key: 'wikidichzz', label: 'Wikidichzz', config: wikidichzz },
  {
    key: 'wikicv',
    label: 'Wikicv',
    config: wikicv,
    note: 'Trang bảng xếp hạng (/bang-xep-hang) và tìm kiếm (/tim-kiem) cần cookie đăng nhập: bật “Cần cookie” và chọn tài khoản wikicv đã đăng nhập (thiếu thì báo lỗi rõ ràng). Gọi dồn dập sẽ bị cấm IP nên cấu hình giãn cách 3 giây giữa hai request (requestDelayMs). Chúng chia 20 truyện/trang, tối đa khoảng 1000; /truyen-nam không cần đăng nhập, 50 truyện và không phân trang.'
  }
];
