// Trùng giá trị với enum AIProvider ở backend (Domain/Enums/AIProvider.cs).
// `models` chỉ là gợi ý nhanh và có thể đã cũ — danh sách chính xác lấy bằng nút "Lấy danh sách model".
export const AI_PROVIDERS = [
  {
    value: 1,
    label: 'Gemini',
    projectLabel: 'Google Cloud project',
    models: ['gemini-2.5-pro', 'gemini-2.5-flash', 'gemini-2.5-flash-lite']
  },
  {
    value: 2,
    label: 'Claude',
    projectLabel: 'Workspace (Anthropic Console)',
    models: ['claude-opus-5', 'claude-sonnet-5', 'claude-haiku-4-5', 'claude-fable-5-1']
  },
  {
    value: 3,
    label: 'ChatGPT',
    projectLabel: 'OpenAI project',
    models: ['gpt-5', 'gpt-5-mini', 'gpt-5-nano']
  },
  {
    value: 4,
    label: 'DeepSeek',
    projectLabel: 'Project',
    models: ['deepseek-chat', 'deepseek-reasoner']
  },
  {
    value: 5,
    label: 'Grok',
    projectLabel: 'xAI team',
    models: ['grok-4', 'grok-3-mini']
  }
];

export const providerOf = value => AI_PROVIDERS.find(provider => provider.value === Number(value));
