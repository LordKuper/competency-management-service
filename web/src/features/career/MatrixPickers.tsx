import { Button, Form, Select, Space } from "antd";
import { CatalogModal } from "./CatalogModal";

interface Option {
  value: string;
  label: string;
}

interface PickerActionsProps {
  onCancel: () => void;
}

function PickerActions({ onCancel }: PickerActionsProps) {
  return (
    <Space>
      <Button type="primary" htmlType="submit">
        Добавить
      </Button>
      <Button onClick={onCancel}>Отмена</Button>
    </Space>
  );
}

interface AddLevelModalProps {
  options: readonly Option[];
  isLoading: boolean;
  onPick: (levelId: string) => void;
  onClose: () => void;
}

/** Dialog that picks one level of the track to add to the matrix. */
export function AddLevelModal({
  options,
  isLoading,
  onPick,
  onClose,
}: AddLevelModalProps) {
  return (
    <CatalogModal
      title="Добавить уровень"
      error={null}
      fields={[]}
      onClose={onClose}
    >
      <Form
        name="matrix-add-level"
        layout="vertical"
        onFinish={(values: { levelId: string }) => onPick(values.levelId)}
      >
        <Form.Item name="levelId" label="Уровни" rules={[{ required: true }]}>
          <Select
            showSearch={{ optionFilterProp: "label" }}
            loading={isLoading}
            options={[...options]}
          />
        </Form.Item>
        <PickerActions onCancel={onClose} />
      </Form>
    </CatalogModal>
  );
}

interface AddCompetencyModalProps {
  competencies: readonly Option[];
  blocks: readonly Option[];
  isLoading: boolean;
  onPick: (competencyId: string, blockId: string) => void;
  onClose: () => void;
}

/** Dialog that picks a catalogue competency and the block of the matrix it joins. */
export function AddCompetencyModal({
  competencies,
  blocks,
  isLoading,
  onPick,
  onClose,
}: AddCompetencyModalProps) {
  return (
    <CatalogModal
      title="Добавить компетенцию"
      error={null}
      fields={[]}
      onClose={onClose}
    >
      <Form
        name="matrix-add-competency"
        layout="vertical"
        initialValues={{ blockId: blocks[0]?.value }}
        onFinish={(values: { competencyId: string; blockId: string }) =>
          onPick(values.competencyId, values.blockId)
        }
      >
        <Form.Item
          name="competencyId"
          label="Компетенции"
          rules={[{ required: true }]}
        >
          <Select
            showSearch={{ optionFilterProp: "label" }}
            loading={isLoading}
            options={[...competencies]}
          />
        </Form.Item>
        <Form.Item
          name="blockId"
          label="Блоки компетенций"
          rules={[{ required: true }]}
        >
          <Select options={[...blocks]} />
        </Form.Item>
        <PickerActions onCancel={onClose} />
      </Form>
    </CatalogModal>
  );
}
