import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Form, Select } from "antd";
import { useNavigate } from "react-router";
import { useFormSubmit } from "../../app/useFormSubmit";
import { CatalogModal, FormActions } from "./CatalogModal";
import { CatalogSelect, IN_WORK } from "./CatalogSelect";
import { useProcessRoles } from "./careerApi";
import { createMatrix, invalidateMatrices, type NewMatrix } from "./matrixApi";

const FIELDS = ["orgUnitId", "directionId", "trackId", "scaleId"] as const;

interface MatrixCreateModalProps {
  onClose: () => void;
}

/** Dialog in which a unit head starts the matrix of a track for the unit; the new matrix opens at once. */
export function MatrixCreateModal({ onClose }: MatrixCreateModalProps) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [form] = Form.useForm<NewMatrix>();
  const directionId = Form.useWatch("directionId", form);
  const orgUnitId = Form.useWatch("orgUnitId", form);
  const roles = useProcessRoles();
  const create = useMutation({
    mutationFn: createMatrix,
    onSuccess: (created) => {
      onClose();
      void navigate(`/career/matrices/${created.id}`);
    },
    onSettled: () => invalidateMatrices(queryClient),
  });
  const { isSubmitting, submit } = useFormSubmit(form, create.mutateAsync);

  return (
    <CatalogModal
      title="Новая матрица"
      error={create.error}
      fields={FIELDS}
      onClose={onClose}
    >
      <Form
        form={form}
        name="matrix-create"
        layout="vertical"
        onFinish={submit}
      >
        <Form.Item
          name="orgUnitId"
          label="Подразделение"
          rules={[{ required: true }]}
        >
          <Select
            options={roles.data?.headedUnits.map((unit) => ({
              value: unit.id,
              label: unit.name,
            }))}
            onChange={() => form.resetFields(["trackId"])}
          />
        </Form.Item>
        <Form.Item
          name="directionId"
          label="Область"
          rules={[{ required: true }]}
        >
          <CatalogSelect
            segment="directions"
            filters={IN_WORK}
            onChange={() => form.resetFields(["trackId"])}
          />
        </Form.Item>
        <Form.Item
          name="trackId"
          label="Трек"
          rules={[{ required: true }]}
          extra={
            !directionId || !orgUnitId
              ? "Сначала выберите подразделение и область"
              : undefined
          }
        >
          <CatalogSelect
            segment="tracks"
            filters={{ ...IN_WORK, directionId, forOrgUnitId: orgUnitId }}
            disabled={!directionId || !orgUnitId}
          />
        </Form.Item>
        <Form.Item name="scaleId" label="Шкала" rules={[{ required: true }]}>
          <CatalogSelect segment="scales" filters={IN_WORK} />
        </Form.Item>
        <FormActions isSubmitting={isSubmitting} onCancel={onClose} />
      </Form>
    </CatalogModal>
  );
}
