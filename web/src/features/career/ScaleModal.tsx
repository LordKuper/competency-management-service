import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Button, Flex, Form, Input, InputNumber } from "antd";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { useEditBase } from "../../app/useEditBase";
import { useFormSubmit } from "../../app/useFormSubmit";
import { CatalogModal, FormActions } from "./CatalogModal";
import { invalidateCatalogs, type Scale, type ScalePoint } from "./catalogApi";

const NAME_MAX_LENGTH = 200;
const LABEL_MAX_LENGTH = 500;
const POINT_SCORE_WIDTH = 96;
const FIELDS = ["name", "points"] as const;
const DUPLICATE_SCORES = "Баллы не должны повторяться";
const USED_NOTICE = "Используется в опубликованной матрице — изменить нельзя";

interface ScaleValues {
  name: string;
  points?: Partial<ScalePoint>[];
}

interface ScaleModalProps {
  /** Scale being edited as the cache holds it now; omitted when one is created. */
  scale?: Scale;
  onClose: () => void;
}

/** Dialog that creates a scale with its points or edits one. */
export function ScaleModal({ scale: latest, onClose }: ScaleModalProps) {
  const queryClient = useQueryClient();
  const { base: scale, noteSaveFailure } = useEditBase(latest);
  const save = useMutation({
    mutationFn: async (values: ScaleValues) => {
      const body = {
        name: values.name,
        points: (values.points ?? []).map((point) => ({
          score: point.score ?? 0,
          label: point.label ?? "",
        })),
      };
      return unwrap(
        scale
          ? await api.PUT("/api/v1/career/scales/{id}", {
              params: {
                path: { id: scale.id },
                header: { "If-Match": ifMatchOf(scale.version) },
              },
              body,
            })
          : await api.POST("/api/v1/career/scales", {
              body,
            }),
      );
    },
    onSuccess: onClose,
    onError: noteSaveFailure,
    onSettled: () => invalidateCatalogs(queryClient),
  });

  return (
    <CatalogModal
      title={scale ? "Изменение шкалы" : "Новая шкала"}
      error={save.error}
      fields={FIELDS}
      conflictNotice={USED_NOTICE}
      onClose={onClose}
    >
      <ScaleForm
        key={scale?.version}
        scale={scale}
        onSubmit={(values) => save.mutateAsync(values)}
        onCancel={onClose}
      />
    </CatalogModal>
  );
}

interface ScaleFormProps {
  scale?: Scale;
  onSubmit: (values: ScaleValues) => Promise<unknown>;
  onCancel: () => void;
}

function ScaleForm({ scale, onSubmit, onCancel }: ScaleFormProps) {
  const [form] = Form.useForm<ScaleValues>();
  const { isSubmitting, submit } = useFormSubmit(form, onSubmit);

  return (
    <Form
      form={form}
      name="scale"
      layout="vertical"
      initialValues={{
        name: scale?.name ?? "",
        points: scale?.points ?? [{}],
      }}
      onFinish={submit}
    >
      <Form.Item
        name="name"
        label="Название"
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
      <Form.List
        name="points"
        rules={[
          {
            validator: async (_, points?: Partial<ScalePoint>[]) => {
              const scores = (points ?? [])
                .map((point) => point?.score)
                .filter((score) => score != null);
              if (new Set(scores).size < scores.length) {
                throw new Error(DUPLICATE_SCORES);
              }
            },
          },
        ]}
      >
        {(fields, { add, remove }, { errors }) => (
          <>
            {fields.map((field) => (
              <Flex key={field.key} gap="middle" align="start">
                <Form.Item
                  name={[field.name, "score"]}
                  label="Балл"
                  rules={[{ required: true }]}
                  style={{ width: POINT_SCORE_WIDTH }}
                >
                  <InputNumber style={{ width: "100%" }} />
                </Form.Item>
                <Form.Item
                  name={[field.name, "label"]}
                  label="Значение"
                  rules={[
                    { required: true, whitespace: true },
                    {
                      max: LABEL_MAX_LENGTH,
                      message: `Не длиннее ${LABEL_MAX_LENGTH} символов`,
                    },
                  ]}
                  style={{ flex: 1 }}
                >
                  <Input.TextArea autoSize={{ minRows: 1 }} />
                </Form.Item>
                <Form.Item label=" ">
                  <Button onClick={() => remove(field.name)}>Удалить</Button>
                </Form.Item>
              </Flex>
            ))}
            <Form.Item>
              <Button onClick={() => add({})}>Добавить балл</Button>
              <Form.ErrorList errors={errors} />
            </Form.Item>
          </>
        )}
      </Form.List>
      <FormActions isSubmitting={isSubmitting} onCancel={onCancel} />
    </Form>
  );
}
