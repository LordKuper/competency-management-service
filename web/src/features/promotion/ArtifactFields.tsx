import { MinusCircleOutlined, PlusOutlined } from "@ant-design/icons";
import { Button, Card, Flex, Form, Input, Tag, Typography } from "antd";
import type { ArtifactInput, ArtifactLink } from "./promotionApi";
import { ARTIFACT_MANDATORY_EXPLANATION } from "./promotionTexts";

const MAX_LINKS = 10;
const MAX_DESCRIPTION_LENGTH = 1000;
const WEB_LINK = /^https?:\/\/\S+$/i;

/** The artifacts a form holds by competency, under the `items` field of the form that shows `ArtifactFields`. */
export type ArtifactItems = Record<
  string,
  { links?: { description?: string; url?: string }[] } | undefined
>;

/** A competency to give artifacts for, with the artifacts given so far. */
export interface ArtifactCompetency {
  competencyId: string;
  competencyName: string;
  links?: readonly ArtifactLink[];
}

/** The initial `items` of a form for the artifacts given so far. */
export function artifactItemsOf(
  competencies: readonly ArtifactCompetency[],
): ArtifactItems {
  return Object.fromEntries(
    competencies.map((competency) => [
      competency.competencyId,
      { links: (competency.links ?? []).map((link) => ({ ...link })) },
    ]),
  );
}

/** The artifacts to save: those with something entered, in the order of the competencies. */
export function artifactsOf(
  items: ArtifactItems | undefined,
  competencyIds: readonly string[],
): ArtifactInput[] {
  return competencyIds.flatMap((competencyId) => {
    const links = (items?.[competencyId]?.links ?? [])
      .map((link) => ({
        description: link.description?.trim() ?? "",
        url: link.url?.trim() ?? "",
      }))
      .filter((link) => link.description !== "" || link.url !== "");
    return links.length === 0 ? [] : [{ competencyId, links }];
  });
}

interface ArtifactFieldsProps {
  /** The competencies in the order they are shown; the ones that need an artifact come first. */
  competencies: readonly ArtifactCompetency[];
  /** The competencies an artifact is required for. */
  mandatoryIds: readonly string[];
}

/** The fields of the artifacts by competency: up to ten, each with what was done and a link to a document elsewhere. Place it inside a form with an `items` field. */
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
            <Form.List name={["items", id, "links"]}>
              {(fields, { add, remove }) => (
                <Flex vertical gap="small">
                  {fields.map((field, index) => (
                    <Flex key={field.key} gap="small" align="start">
                      <Flex vertical style={{ flex: 1 }}>
                        <Form.Item
                          name={[field.name, "description"]}
                          label="Что сделано"
                          rules={[
                            { required: true, message: "Опишите артефакт" },
                          ]}
                        >
                          <Input.TextArea
                            rows={2}
                            maxLength={MAX_DESCRIPTION_LENGTH}
                            showCount
                            aria-label={`Что сделано ${index + 1}: ${competency.competencyName}`}
                          />
                        </Form.Item>
                        <Form.Item
                          name={[field.name, "url"]}
                          label="Ссылка"
                          style={{ marginBottom: 0 }}
                          rules={[
                            { required: true, message: "Укажите ссылку" },
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
                      </Flex>
                      <Button
                        type="text"
                        icon={<MinusCircleOutlined />}
                        aria-label={`Убрать артефакт ${index + 1}: ${competency.competencyName}`}
                        onClick={() => remove(field.name)}
                      />
                    </Flex>
                  ))}
                  <Button
                    type="dashed"
                    icon={<PlusOutlined />}
                    disabled={fields.length >= MAX_LINKS}
                    onClick={() => add({ description: "", url: "" })}
                  >
                    Добавить артефакт
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
