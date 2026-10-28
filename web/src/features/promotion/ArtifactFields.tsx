import { PlusOutlined } from "@ant-design/icons";
import { Button, Card, Col, Flex, Form, Input, Row, Typography } from "antd";
import type { ArtifactInput, ArtifactLink } from "./promotionApi";

/** Most artifacts one competency can carry. */
export const MAX_ARTIFACTS = 10;
const MAX_DESCRIPTION_LENGTH = 1000;
const WEB_LINK = /^https?:\/\/\S+$/i;

/** The artifacts a form holds by competency, under the `items` field of the form that shows `ArtifactEditor`. */
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

function filledLinks(items: ArtifactItems | undefined, competencyId: string) {
  return (items?.[competencyId]?.links ?? [])
    .map((link) => ({
      description: link.description?.trim() ?? "",
      url: link.url?.trim() ?? "",
    }))
    .filter((link) => link.description !== "" || link.url !== "");
}

/** The artifacts to save: those with something entered, in the order of the competencies. */
export function artifactsOf(
  items: ArtifactItems | undefined,
  competencyIds: readonly string[],
): ArtifactInput[] {
  return competencyIds.flatMap((competencyId) => {
    const links = filledLinks(items, competencyId);
    return links.length === 0 ? [] : [{ competencyId, links }];
  });
}

/** How many artifacts a competency holds in a form. */
export function artifactCountOf(
  items: ArtifactItems | undefined,
  competencyId: string,
): number {
  return filledLinks(items, competencyId).length;
}

interface ArtifactEditorProps {
  competencyId: string;
  competencyName: string;
}

/** The artifacts of one competency: up to ten cards, each with what was done and a link to a document elsewhere. Place it inside a form with an `items` field. */
export function ArtifactEditor({
  competencyId,
  competencyName,
}: ArtifactEditorProps) {
  return (
    <Form.List name={["items", competencyId, "links"]}>
      {(fields, { add, remove }) => (
        <Flex vertical gap="small">
          {fields.map((field, index) => (
            <Card
              key={field.key}
              size="small"
              title={`Артефакт ${index + 1}`}
              extra={
                <Button
                  type="link"
                  danger
                  aria-label={`Убрать артефакт ${index + 1}: ${competencyName}`}
                  onClick={() => remove(field.name)}
                >
                  Убрать
                </Button>
              }
            >
              <Row gutter={16}>
                <Col xs={24} md={14}>
                  <Form.Item
                    name={[field.name, "description"]}
                    label="Что сделано"
                    rules={[{ required: true, message: "Опишите артефакт" }]}
                  >
                    <Input.TextArea
                      rows={2}
                      maxLength={MAX_DESCRIPTION_LENGTH}
                      showCount
                      aria-label={`Что сделано ${index + 1}: ${competencyName}`}
                    />
                  </Form.Item>
                </Col>
                <Col xs={24} md={10}>
                  <Form.Item
                    name={[field.name, "url"]}
                    label="Ссылка"
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
                      aria-label={`Ссылка ${index + 1}: ${competencyName}`}
                      placeholder="https://"
                    />
                  </Form.Item>
                </Col>
              </Row>
            </Card>
          ))}
          <Flex align="center" gap="middle" wrap>
            <Button
              icon={<PlusOutlined />}
              disabled={fields.length >= MAX_ARTIFACTS}
              onClick={() => add({ description: "", url: "" })}
            >
              Добавить артефакт
            </Button>
            <Typography.Text type="secondary">
              До {MAX_ARTIFACTS} артефактов к компетенции, у каждого своя
              ссылка.
            </Typography.Text>
          </Flex>
        </Flex>
      )}
    </Form.List>
  );
}
