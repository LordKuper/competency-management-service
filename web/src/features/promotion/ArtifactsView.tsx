import { Button, Card, Flex, Form, Typography } from "antd";
import { useState } from "react";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import {
  type ArtifactCompetency,
  ArtifactFields,
  type ArtifactItems,
  artifactItemsOf,
  artifactsOf,
} from "./ArtifactFields";
import type { ArtifactInput } from "./promotionApi";

interface ArtifactsFormValues {
  items: ArtifactItems;
}

interface ArtifactsViewProps {
  /** The competencies of the target in the order of the matrix, each with the artifact given so far. */
  competencies: readonly ArtifactCompetency[];
  /** The competencies an artifact is required for. */
  mandatoryIds: readonly string[];
  /** Whether artifacts can be changed now; otherwise the ones given are only shown. */
  isEditable: boolean;
  /** Saves the artifacts; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (artifacts: ArtifactInput[]) => Promise<unknown>;
}

/** The artifacts of a candidate by competency, saved on their own once the application is filed; the competencies that need one come first. */
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
        values.items,
        competencies.map((competency) => competency.competencyId),
      ),
    );
    setIsDirty(false);
  });
  useLeaveGuard({ isUnsaved: isDirty, isSaving: isSubmitting });

  if (!isEditable) {
    const given = competencies.filter((competency) => competency.text);
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
      initialValues={{ items: artifactItemsOf(competencies) }}
      onFinish={submit}
      onValuesChange={() => setIsDirty(form.isFieldsTouched())}
    >
      <Flex vertical gap="middle">
        <ArtifactFields
          competencies={competencies}
          mandatoryIds={mandatoryIds}
        />
        <Button type="primary" htmlType="submit" loading={isSubmitting}>
          Сохранить артефакты
        </Button>
      </Flex>
    </Form>
  );
}
