import { Button, Form, Input, Select, Space } from "antd";
import { CatalogModal } from "./CatalogModal";
import { CatalogSelect, type PickerFilters } from "./CatalogSelect";

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

const NAME_MAX_LENGTH = 200;

interface AddOwnBlockModalProps {
  onPick: (name: string) => void;
  onClose: () => void;
}

/** Dialog that names a new block of the version's own, which competencies are then put in. */
export function AddOwnBlockModal({ onPick, onClose }: AddOwnBlockModalProps) {
  return (
    <CatalogModal
      title="Добавить блок"
      error={null}
      fields={[]}
      onClose={onClose}
    >
      <Form
        name="matrix-add-own-block"
        layout="vertical"
        onFinish={(values: { name: string }) => onPick(values.name.trim())}
      >
        <Form.Item
          name="name"
          label="Название блока"
          rules={[
            { required: true, whitespace: true, message: "Укажите название" },
            {
              max: NAME_MAX_LENGTH,
              message: `Не длиннее ${NAME_MAX_LENGTH} символов`,
            },
          ]}
        >
          <Input autoComplete="off" />
        </Form.Item>
        <PickerActions onCancel={onClose} />
      </Form>
    </CatalogModal>
  );
}

interface AddCompetencyModalProps {
  /** The competencies a matrix of this version may use: its scale, its direction or general, in work. */
  filters: PickerFilters;
  /** Competencies already in the matrix, which are not offered again. */
  usedIds: readonly string[];
  blocks: readonly Option[];
  /** The block chosen when the dialog opens; the first one when omitted. */
  defaultBlock?: string;
  onPick: (competencyId: string, blockId: string) => void;
  onClose: () => void;
}

/** Dialog that picks a catalogue competency, searched on the server, and the block of the matrix it joins. */
export function AddCompetencyModal({
  filters,
  usedIds,
  blocks,
  defaultBlock,
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
        initialValues={{ blockId: defaultBlock ?? blocks[0]?.value }}
        onFinish={(values: { competencyId: string; blockId: string }) =>
          onPick(values.competencyId, values.blockId)
        }
      >
        <Form.Item
          name="competencyId"
          label="Компетенции"
          rules={[{ required: true }]}
        >
          <CatalogSelect
            segment="competencies"
            filters={filters}
            exclude={usedIds}
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
