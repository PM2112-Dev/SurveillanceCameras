import { CreateStoryVideoTypeCommand, UpdateStoryVideoTypeCommand } from '../../web-api-client';
import { storyVideoTypesClient } from '../../api/clients';
import { EntityForm, useEntityForm } from '../../components/common/EntityForm';

export function StoryVideoTypeForm() {
  const state = useEntityForm({
    initial: { title: '', code: '' },
    loadEntity: id => storyVideoTypesClient.getStoryVideoTypeById(id),
    toForm: entity => ({ title: entity.title ?? '', code: entity.code ?? '' }),
    save: ({ isEdit, id, form }) =>
      isEdit
        ? storyVideoTypesClient.updateStoryVideoType(id, new UpdateStoryVideoTypeCommand({ id, ...form }))
        : storyVideoTypesClient.createStoryVideoType(new CreateStoryVideoTypeCommand(form)),
    backTo: '/story-video-types'
  });

  return (
    <EntityForm title={state.isEdit ? 'Sửa loại video truyện' : 'Thêm loại video truyện'} state={state}>
      <label>
        Tên loại video <span className="required">*</span>
        <input
          type="text"
          required
          maxLength={500}
          value={state.form.title}
          onChange={event => state.update({ title: event.target.value })}
          placeholder="VD: Audio TTS dài"
        />
      </label>

      <label>
        Mã
        <input
          type="text"
          maxLength={100}
          value={state.form.code}
          onChange={event => state.update({ code: event.target.value })}
          placeholder="VD: audio-long"
        />
      </label>
    </EntityForm>
  );
}
