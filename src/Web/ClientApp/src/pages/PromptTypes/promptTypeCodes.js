// Trùng PromptTypeCodes ở backend (Domain/Constants/PromptTypeCodes.cs): mã hệ thống dùng để chọn prompt theo tác vụ.
export const PROMPT_TYPE_CODES = [
  { code: 'edit-title', label: 'Biên tập tên truyện' },
  { code: 'edit-description', label: 'Biên tập giới thiệu truyện' },
  { code: 'edit-chapter-content', label: 'Biên tập nội dung chương (bản dịch word-by-word → tiếng Việt tự nhiên)' }
];
