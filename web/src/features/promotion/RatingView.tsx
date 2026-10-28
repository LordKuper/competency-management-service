import { Alert, Button, Flex, Form, Radio } from "antd";
import { useState } from "react";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import type { Rating, RatingAnswer } from "./promotionApi";
import { scalePointLabel } from "./promotionTexts";

const NOT_OBSERVED = "notObserved";

interface RatingFormValues {
  answers: Record<string, number | typeof NOT_OBSERVED>;
}

interface RatingViewProps {
  rating: Rating;
  /** Saves the answers; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (answers: RatingAnswer[]) => Promise<unknown>;
}

/** The competencies of a candidate to score on the scale of each, or to mark as not observed. */
export function RatingView({ rating, onSubmit }: RatingViewProps) {
  const [form] = Form.useForm<RatingFormValues>();
  const [isDirty, setIsDirty] = useState(false);
  const competencies = rating.competencies.filter(
    (competency) => competency.scalePoints.length > 0,
  );
  const { isSubmitting, submit } = useFormSubmit(form, async (values) => {
    await onSubmit(
      competencies.flatMap((competency) => {
        const answer = values.answers?.[competency.id];
        return answer === undefined
          ? []
          : [
              {
                competencyId: competency.id,
                score: answer === NOT_OBSERVED ? null : answer,
              },
            ];
      }),
    );
    setIsDirty(false);
  });
  useLeaveGuard({ isUnsaved: isDirty, isSaving: isSubmitting });

  return (
    <Form
      form={form}
      name="promotion-rating"
      layout="vertical"
      disabled={isSubmitting || !rating.canEdit}
      initialValues={{
        answers: Object.fromEntries(
          (rating.answers ?? []).map((answer) => [
            answer.competencyId,
            answer.score ?? NOT_OBSERVED,
          ]),
        ),
      }}
      onFinish={submit}
      onValuesChange={() => setIsDirty(form.isFieldsTouched())}
    >
      {!rating.canEdit && (
        <Form.Item>
          <Alert type="info" showIcon title="Оценку сейчас нельзя изменить" />
        </Form.Item>
      )}
      {competencies.map((competency) => (
        <Form.Item
          key={competency.id}
          name={["answers", competency.id]}
          label={competency.name}
          rules={[
            { required: true, message: "Выберите балл или «Не наблюдал»" },
          ]}
        >
          <Radio.Group
            orientation="vertical"
            options={[
              ...competency.scalePoints.map((point) => ({
                value: point.score,
                label: scalePointLabel(point),
              })),
              { value: NOT_OBSERVED, label: "Не наблюдал" },
            ]}
          />
        </Form.Item>
      ))}
      {rating.canEdit && (
        <Flex>
          <Button type="primary" htmlType="submit" loading={isSubmitting}>
            Сохранить оценку
          </Button>
        </Flex>
      )}
    </Form>
  );
}
