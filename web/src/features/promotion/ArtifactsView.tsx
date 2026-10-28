import { MinusCircleOutlined, PlusOutlined } from "@ant-design/icons";
import { Button, Card, Flex, Form, Input, Tag, Typography } from "antd";
import { useState } from "react";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import type { ArtifactInput } from "./promotionApi";

const MAX_LINKS = 10;
const MAX_TEXT_LENGTH = 4000;
const WEB_LINK = /^https?:\/\/\S+$/i;

interface ArtifactsFormValues {
  items: Record<string, { text?: string; links?: string[] }>;
}

interface ArtifactsViewProps {
  /** The competencies of the target in the order of the matrix, each with the artifact given so far. */
  competencies: ReadonlyArray<{
    competencyId: string;
    competencyName: string;
    text?: string;
    links?: readonly string[];
  }>;
  /** The competencies an artifact is required for. */
  mandatoryIds: readonly string[];
  /** Whether artifacts can be changed now; otherwise the ones given are only shown. */
  isEditable: boolean;
  /** Saves the artifacts; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (artifacts: ArtifactInput[]) => Promise<unknown>;
}

function artifactsOf(
  values: ArtifactsFormValues,
  competencyIds: readonly string[],
): ArtifactInput[] {
  return competencyIds.flatMap((competencyId) => {
    const item = values.items?.[competencyId];
    const text = item?.text?.trim();
    if (!text) return [];
    const links = (item?.links ?? [])
      .map((link) => link.trim())
      .filter((link) => link !== "");
    return [{ competencyId, text, links }];
  });
}

/** The artifacts of a candidate by competency: a description and links to documents elsewhere; the competencies that need one come first. */
export function ArtifactsView({
  competencies,
  mandatoryIds,
  isEditable,
  onSubmit,
}: ArtifactsViewProps) {
  const [form] = Form.useForm<ArtifactsFormValues>();
  const [isDirty, setIsDirty] = useState(false);
  const { isSubmitting, submit } = useFormSubmit(form, async (values) => {
    await onSubmit(
      artifactsOf(
        values,
        competencies.map((competency) => competency.competencyId),
      ),
    );
    setIsDirty(false);
  });
  useLeaveGuard({ isUnsaved: isDirty, isSaving: isSubmitting });
  const ordered = [
    ...competencies.filter((competency) =>
      mandatoryIds.includes(competency.competencyId),
    ),
    ...competencies.filter(
      (competency) => !mandatoryIds.includes(competency.competencyId),
    ),
  ];

  if (!isEditable) {
    const given = ordered.filter((competency) => competency.text);
    return given.length === 0 ? (
      <Typography.Text type="secondary">Артефактов нет.</Typography.Text>
    ) : (
      <Flex vertical gap="middle">
        {given.map((competency) => (
          <Card
            key={competency.competencyId}
            size="small"
            title={competency.competencyName}
          >
            <Typography.Paragraph>{competency.text}</Typography.Paragraph>
            {competency.links?.map((link) => (
              <div key={link}>
                <a href={link} target="_blank" rel="noreferrer noopener">
                  {link}
                </a>
              </div>
            ))}
          </Card>
        ))}
      </Flex>
    );
  }

  return (
    <Form
      form={form}
      name="promotion-artifacts"
      layout="vertical"
      disabled={isSubmitting}
      initialValues={{
        items: Object.fromEntries(
          competencies.map((competency) => [
            competency.competencyId,
            { text: competency.text, links: [...(competency.links ?? [])] },
          ]),
        ),
      }}
      onFinish={submit}
      onValuesChange={() => setIsDirty(form.isFieldsTouched())}
    >
      <Flex vertical gap="middle">
        {ordered.map((competency) => {
          const id = competency.competencyId;
          return (
            <Card
              key={id}
              size="small"
              title={
                <Flex align="center" gap="small" wrap>
                  <span>{competency.competencyName}</span>
                  {mandatoryIds.includes(id) && (
                    <Tag color="warning">Нужен артефакт</Tag>
                  )}
                </Flex>
              }
            >
              <Form.Item
                name={["items", id, "text"]}
                label="Описание"
                dependencies={[["items", id, "links"]]}
                rules={[
                  ({ getFieldValue }) => ({
                    validator: async (_, text?: string) => {
                      const links = getFieldValue(["items", id, "links"]) ?? [];
                      if (!text?.trim() && links.length > 0) {
                        throw new Error("Опишите артефакт");
                      }
                    },
                  }),
                ]}
              >
                <Input.TextArea
                  rows={3}
                  maxLength={MAX_TEXT_LENGTH}
                  showCount
                />
              </Form.Item>
              <Form.List name={["items", id, "links"]}>
                {(fields, { add, remove }) => (
                  <Flex vertical gap="small">
                    {fields.map((field, index) => (
                      <Flex key={field.key} gap="small" align="start">
                        <Form.Item
                          name={field.name}
                          style={{ flex: 1, marginBottom: 0 }}
                          rules={[
                            {
                              pattern: WEB_LINK,
                              message:
                                "Ссылка должна начинаться с http:// или https://",
                            },
                          ]}
                        >
                          <Input
                            aria-label={`Ссылка ${index + 1}: ${competency.competencyName}`}
                            placeholder="https://"
                          />
                        </Form.Item>
                        <Button
                          type="text"
                          icon={<MinusCircleOutlined />}
                          aria-label={`Убрать ссылку ${index + 1}: ${competency.competencyName}`}
                          onClick={() => remove(field.name)}
                        />
                      </Flex>
                    ))}
                    <Button
                      type="dashed"
                      icon={<PlusOutlined />}
                      disabled={fields.length >= MAX_LINKS}
                      onClick={() => add("")}
                    >
                      Добавить ссылку
                    </Button>
                  </Flex>
                )}
              </Form.List>
            </Card>
          );
        })}
        <Button type="primary" htmlType="submit" loading={isSubmitting}>
          Сохранить артефакты
        </Button>
      </Flex>
    </Form>
  );
}
