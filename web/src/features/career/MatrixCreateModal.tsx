import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Alert, Form, Radio, Select } from "antd";
import { useState } from "react";
import { useNavigate } from "react-router";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import { CatalogModal, FormActions } from "./CatalogModal";
import { CatalogSelect, IN_WORK } from "./CatalogSelect";
import { useProcessRoles } from "./careerApi";
import { MatrixPositionSelect } from "./MatrixPositionSelect";
import { createMatrix, invalidateMatrices, type NewMatrix } from "./matrixApi";

const FIELDS = ["orgUnitId", "directionId", "trackId", "positionId"] as const;
const POSITION_HINT =
  "Матрица должности обычно нужна для управленческих должностей, которые не связаны напрямую с карьерными треками специалистов.";

type Basis = "track" | "position";

interface MatrixCreateModalProps {
  onClose: () => void;
}

/** Dialog in which a unit head starts the matrix of a track or of a position for the unit; the new matrix opens at once. */
export function MatrixCreateModal({ onClose }: MatrixCreateModalProps) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { leaveAfterSave } = useLeaveGuard();
  const [form] = Form.useForm<NewMatrix>();
  const [basis, setBasis] = useState<Basis>("track");
  const directionId = Form.useWatch("directionId", form);
  const orgUnitId = Form.useWatch("orgUnitId", form);
  const roles = useProcessRoles();
  const create = useMutation({
    mutationFn: createMatrix,
    onSuccess: (created) => {
      onClose();
      leaveAfterSave(() => void navigate(`/career/matrices/${created.id}`));
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
        <Form.Item label="Основа матрицы">
          <Radio.Group
            value={basis}
            options={[
              { value: "track", label: "По треку" },
              { value: "position", label: "По должности" },
            ]}
            onChange={(event) => {
              setBasis(event.target.value as Basis);
              form.resetFields(["directionId", "trackId", "positionId"]);
            }}
          />
        </Form.Item>
        {basis === "position" && (
          <Alert
            type="info"
            showIcon
            title={POSITION_HINT}
            style={{ marginBottom: "var(--ant-margin)" }}
          />
        )}
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
            onChange={() => form.resetFields(["trackId", "positionId"])}
          />
        </Form.Item>
        {basis === "position" ? (
          <Form.Item
            name="positionId"
            label="Должность"
            rules={[{ required: true, message: "Выберите должность" }]}
          >
            <MatrixPositionSelect orgUnitId={orgUnitId} />
          </Form.Item>
        ) : (
          <>
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
                filters={{
                  ...IN_WORK,
                  directionId: directionId ?? undefined,
                  forOrgUnitId: orgUnitId,
                }}
                disabled={!directionId || !orgUnitId}
              />
            </Form.Item>
          </>
        )}
        <FormActions isSubmitting={isSubmitting} onCancel={onClose} />
      </Form>
    </CatalogModal>
  );
}
