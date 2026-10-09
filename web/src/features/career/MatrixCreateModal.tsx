import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Form, Select } from "antd";
import { useNavigate } from "react-router";
import { useFormSubmit } from "../../app/useFormSubmit";
import { CatalogModal, FormActions } from "./CatalogModal";
import { useProcessRoles } from "./careerApi";
import {
  directionsQuery,
  scalesQuery,
  tracksQuery,
  usableEntries,
} from "./catalogApi";
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
  const roles = useProcessRoles();
  const directions = useQuery(directionsQuery());
  const tracks = useQuery({
    ...tracksQuery(directionId ?? ""),
    enabled: !!directionId,
  });
  const scales = useQuery(scalesQuery());
  const create = useMutation({
    mutationFn: createMatrix,
    onSuccess: (created) => {
      onClose();
      void navigate(`/career/matrices/${created.id}`);
    },
    onSettled: () => invalidateMatrices(queryClient),
  });
  const { isSubmitting, submit } = useFormSubmit(form, create.mutateAsync);
  const options = (entries: readonly { id: string; name: string }[]) =>
    entries.map((entry) => ({ value: entry.id, label: entry.name }));

  return (
    <CatalogModal
      title="Создать матрицу"
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
          <Select options={options(roles.data?.headedUnits ?? [])} />
        </Form.Item>
        <Form.Item
          name="directionId"
          label="Направление"
          rules={[{ required: true }]}
        >
          <Select
            showSearch={{ optionFilterProp: "label" }}
            loading={directions.isFetching}
            options={options(usableEntries(directions.data?.items, null))}
            onChange={() => form.resetFields(["trackId"])}
          />
        </Form.Item>
        <Form.Item name="trackId" label="Трек" rules={[{ required: true }]}>
          <Select
            showSearch={{ optionFilterProp: "label" }}
            disabled={!directionId}
            loading={tracks.isFetching}
            options={options(usableEntries(tracks.data?.items, null))}
          />
        </Form.Item>
        <Form.Item name="scaleId" label="Шкала" rules={[{ required: true }]}>
          <Select
            showSearch={{ optionFilterProp: "label" }}
            loading={scales.isFetching}
            options={options(usableEntries(scales.data?.items, null))}
          />
        </Form.Item>
        <FormActions isSubmitting={isSubmitting} onCancel={onClose} />
      </Form>
    </CatalogModal>
  );
}
