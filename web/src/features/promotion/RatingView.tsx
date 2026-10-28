import { Alert, Button, Col, Flex, Form, Row, Tag, Typography } from "antd";
import { useState } from "react";
import { Link } from "react-router";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import { DeadlineNote } from "./DeadlineNote";
import { Panel } from "./Panel";
import type { Rating, RatingAnswer, RatingTask } from "./promotionApi";
import { competenciesText, STAGE } from "./promotionTexts";
import { promotionPaths } from "./routes";
import { NOT_OBSERVED, type ScaleAnswer, ScaleChoice } from "./ScaleChoice";

interface RatingFormValues {
  answers: Record<string, ScaleAnswer>;
}

interface RatingViewProps {
  rating: Rating;
  /** The candidates the viewer is asked to rate, so they can move between them. */
  tasks: readonly RatingTask[];
  /** Saves the answers; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (answers: RatingAnswer[]) => Promise<unknown>;
  /** Opens the next candidate in the queue; omitted when there is none. */
  onOpenNext?: () => void;
}

function QueueTag({ task }: { task: RatingTask }) {
  if (task.isRated) return <Tag color="success">Оценено</Tag>;
  return task.canRate ? (
    <Tag color="processing">Ждёт оценки</Tag>
  ) : (
    <Tag>Не оценено</Tag>
  );
}

function Queue({
  tasks,
  currentId,
}: {
  tasks: readonly RatingTask[];
  currentId: string;
}) {
  return (
    <Panel title="Кого оценить" tour="rating-queue" level={3}>
      <Flex
        vertical
        gap="small"
        component="ul"
        style={{ margin: 0, padding: 0 }}
      >
        {tasks.map((task) => (
          <Flex
            key={task.applicationId}
            component="li"
            justify="space-between"
            align="center"
            gap="small"
            style={{ listStyle: "none" }}
          >
            <Link
              to={promotionPaths.rating(task.applicationId)}
              aria-current={task.applicationId === currentId || undefined}
            >
              {task.applicationId === currentId ? (
                <b>{task.candidateName}</b>
              ) : (
                task.candidateName
              )}
            </Link>
            <QueueTag task={task} />
          </Flex>
        ))}
      </Flex>
    </Panel>
  );
}

/** The competencies of a candidate to score, each on its own scale or as not observed, beside the queue of the candidates the viewer is asked to rate. */
export function RatingView({
  rating,
  tasks,
  onSubmit,
  onOpenNext,
}: RatingViewProps) {
  const [form] = Form.useForm<RatingFormValues>();
  const [isDirty, setIsDirty] = useState(false);
  const answers = Form.useWatch("answers", form);
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
  const isLocked = isSubmitting || !rating.canEdit;
  const leftCount = competencies.filter(
    (competency) => answers?.[competency.id] === undefined,
  ).length;

  return (
    <Flex vertical gap="middle">
      <DeadlineNote
        session={rating.session}
        label="Оценки можно менять до"
        stage={STAGE.rating}
      />
      <Row gutter={[24, 24]}>
        <Col xs={24} lg={7}>
          <Queue tasks={tasks} currentId={rating.applicationId} />
        </Col>
        <Col xs={24} lg={17}>
          <Panel title={rating.candidateName} tour="rating-form">
            <Form
              form={form}
              name="promotion-rating"
              layout="vertical"
              disabled={isLocked}
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
              <Flex vertical gap="middle">
                <Typography.Text type="secondary">
                  Оцените, как сотрудник проявлял компетенцию за последние
                  полгода. Если не видели работу в этой области — «Не наблюдал».
                  Оценка анонимная, кандидат и комиссия видят только средние
                  баллы.
                </Typography.Text>
                {!rating.canEdit && (
                  <Alert
                    type="info"
                    showIcon
                    title="Оценку сейчас нельзя изменить"
                  />
                )}
                {competencies.map((competency) => (
                  <Form.Item
                    key={competency.id}
                    name={["answers", competency.id]}
                    label={
                      <Flex vertical>
                        <span>{competency.name}</span>
                        {competency.description && (
                          <Typography.Text type="secondary">
                            {competency.description}
                          </Typography.Text>
                        )}
                      </Flex>
                    }
                    style={{ margin: 0 }}
                    rules={[
                      {
                        required: true,
                        message: "Выберите балл или «Не наблюдал»",
                      },
                    ]}
                  >
                    <ScaleChoice
                      name={competency.name}
                      points={competency.scalePoints}
                      hasNotObserved
                      disabled={isLocked}
                    />
                  </Form.Item>
                ))}
                {rating.canEdit && (
                  <Flex align="center" gap="small" wrap>
                    <Button
                      type="primary"
                      htmlType="submit"
                      loading={isSubmitting}
                    >
                      Сохранить оценку
                    </Button>
                    {onOpenNext && (
                      <Button disabled={isSubmitting} onClick={onOpenNext}>
                        Следующий кандидат
                      </Button>
                    )}
                    {leftCount > 0 && (
                      <Typography.Text type="secondary">
                        Осталось оценить {competenciesText(leftCount)}
                      </Typography.Text>
                    )}
                  </Flex>
                )}
              </Flex>
            </Form>
          </Panel>
        </Col>
      </Row>
    </Flex>
  );
}
