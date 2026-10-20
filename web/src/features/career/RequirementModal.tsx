import {
  Button,
  Checkbox,
  Form,
  Input,
  InputNumber,
  Select,
  Space,
} from "antd";
import { CatalogModal, FormActions } from "./CatalogModal";
import type { ScalePoint } from "./catalogApi";
import type { MatrixRequirement } from "./matrixApi";

const DEFAULT_WEIGHT = 1;
interface RequirementValues {
  requiredScore?: number | null;
  weight: number;
  isMandatory?: boolean;
  expectedBehavior?: string | null;
  evidenceExamples?: string | null;
  validFrom?: string | null;
}

interface RequirementModalProps {
  title: string;
  levelId: string;
  competencyId: string;
  /** The requirement as it stands; omitted when the cell has none yet. */
  requirement?: MatrixRequirement;
  scalePoints: readonly ScalePoint[];
  /** Whether the version may be edited; otherwise the requirement is shown and cannot be changed. */
  isEditable: boolean;
  onSave: (requirement: MatrixRequirement) => void;
  onRemove: () => void;
  onClose: () => void;
}

/** Dialog with the requirement of one level for one competency; it changes the version being edited, not the server. */
export function RequirementModal({
  title,
  levelId,
  competencyId,
  requirement,
  scalePoints,
  isEditable,
  onSave,
  onRemove,
  onClose,
}: RequirementModalProps) {
  const [form] = Form.useForm<RequirementValues>();

  function save(values: RequirementValues) {
    onSave({
      levelId,
      competencyId,
      requiredScore: values.requiredScore ?? null,
      weight: values.weight,
      isMandatory: values.isMandatory ?? false,
      expectedBehavior: values.expectedBehavior?.trim() || null,
      evidenceExamples: values.evidenceExamples?.trim() || null,
      validFrom: values.validFrom || null,
    });
  }

  return (
    <CatalogModal title={title} error={null} fields={[]} onClose={onClose}>
      <Form
        form={form}
        name="matrix-requirement"
        layout="vertical"
        disabled={!isEditable}
        initialValues={{
          requiredScore: requirement?.requiredScore ?? undefined,
          weight: requirement?.weight ?? DEFAULT_WEIGHT,
          isMandatory: requirement?.isMandatory ?? false,
          expectedBehavior: requirement?.expectedBehavior ?? "",
          evidenceExamples: requirement?.evidenceExamples ?? "",
          validFrom: requirement?.validFrom ?? "",
        }}
        onFinish={save}
      >
        <Form.Item name="requiredScore" label="Требуемый уровень">
          <Select
            allowClear
            options={scalePoints.map((point) => ({
              value: point.score,
              label: `${point.score} — ${point.label}`,
            }))}
          />
        </Form.Item>
        <Form.Item name="weight" label="Вес" rules={[{ required: true }]}>
          <InputNumber min={0} style={{ width: "100%" }} />
        </Form.Item>
        <Form.Item name="isMandatory" valuePropName="checked">
          <Checkbox>Обязательная</Checkbox>
        </Form.Item>
        <Form.Item name="expectedBehavior" label="Ожидаемое поведение">
          <Input.TextArea autoSize={{ minRows: 3 }} />
        </Form.Item>
        <Form.Item name="evidenceExamples" label="Примеры подтверждений">
          <Input.TextArea autoSize={{ minRows: 3 }} />
        </Form.Item>
        <Form.Item name="validFrom" label="Действует с">
          <Input type="date" />
        </Form.Item>
        {isEditable && (
          <Space>
            <FormActions isSubmitting={false} onCancel={onClose} />
            {requirement && (
              <Button danger onClick={onRemove}>
                Удалить
              </Button>
            )}
          </Space>
        )}
      </Form>
    </CatalogModal>
  );
}
