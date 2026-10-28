import { MinusCircleOutlined, PlusOutlined } from "@ant-design/icons";
import { Button, Card, Flex, Form, Input, Tag, Typography } from "antd";
import type { ArtifactInput } from "./promotionApi";
import { ARTIFACT_MANDATORY_EXPLANATION } from "./promotionTexts";

const MAX_LINKS = 10;
const MAX_TEXT_LENGTH = 4000;
const WEB_LINK = /^https?:\/\/\S+$/i;

/** The artifacts a form holds by competency, under the `items` field of the form that shows `ArtifactFields`. */
export type ArtifactItems = Record<
  string,
  { text?: string; links?: string[] } | undefined
>;

/** A competency to give an artifact for, with the artifact given so far. */
export interface ArtifactCompetency {
  competencyId: string;
  competencyName: string;
  text?: string;
  links?: readonly string[];
}

/** The initial `items` of a form for the artifacts given so far. */
export function artifactItemsOf(
  competencies: readonly ArtifactCompetency[],
): ArtifactItems {
  return Object.fromEntries(
    competencies.map((competency) => [
      competency.competencyId,
      { text: competency.text, links: [...(competency.links ?? [])] },
    ]),
  );
}

/** The artifacts to save: those with a description, in the order of the competencies. */
export function artifactsOf(
  items: ArtifactItems | undefined,
  competencyIds: readonly string[],
): ArtifactInput[] {
  return competencyIds.flatMap((competencyId) => {
    const item = items?.[competencyId];
    const text = item?.text?.trim();
    if (!text) return [];
    const links = (item?.links ?? [])
      .map((link) => link.trim())
      .filter((link) => link !== "");
    return [{ competencyId, text, links }];
  });
}

interface ArtifactFieldsProps {
  /** The competencies in the order they are shown; the ones that need an artifact come first. */
  competencies: readonly ArtifactCompetency[];
  /** The competencies an artifact is required for. */
  mandatoryIds: readonly string[];
}

/** The fields of the artifacts by competency: a description and links to documents elsewhere. Place it inside a form with an `items` field. */
export function ArtifactFields({
  competencies,
  mandatoryIds,
}: ArtifactFieldsProps) {
  const ordered = [
    ...competencies.filter((competency) =>
      mandatoryIds.includes(competency.competencyId),
    ),
    ...competencies.filter(
      (competency) => !mandatoryIds.includes(competency.competencyId),
    ),
  ];
  return (
    <Flex vertical gap="middle">
      {ordered.map((competency) => {
        const id = competency.competencyId;
        const isMandatory = mandatoryIds.includes(id);
        return (
          <Card
            key={id}
            size="small"
            title={
              <Flex align="center" gap="small" wrap>
                <span>{competency.competencyName}</span>
                {isMandatory && <Tag color="warning">Нужен артефакт</Tag>}
              </Flex>
            }
          >
            {isMandatory && (
              <Typography.Paragraph type="secondary">
                {ARTIFACT_MANDATORY_EXPLANATION}
              </Typography.Paragraph>
            )}
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
              <Input.TextArea rows={3} maxLength={MAX_TEXT_LENGTH} showCount />
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
    </Flex>
  );
}
