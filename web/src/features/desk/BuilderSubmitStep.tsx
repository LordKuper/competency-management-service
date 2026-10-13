import { CheckCircleOutlined } from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import {
  Alert,
  App,
  Button,
  Descriptions,
  Flex,
  Skeleton,
  Typography,
} from "antd";
import { useNavigate } from "react-router";
import { trackQuery } from "../career/catalogApi";
import {
  type MatrixVersion,
  matrixCompletenessQuery,
  matrixPublicationQuery,
  matrixQuery,
} from "../career/matrixApi";
import { SKIPPED_NOTE } from "../career/matrixLabels";
import { draftRecords } from "../career/usePublishEntry";
import { CascadeTable } from "./CascadeTable";
import { CompletenessBlock } from "./CompletenessBlock";
import { DecisionError } from "./DecisionError";
import { deskPaths, shortName } from "./deskTexts";
import { outcomeText, remarksText } from "./scenarioTexts";
import { useVersionTransition } from "./useVersionTransition";

interface BuilderSubmitStepProps {
  version: MatrixVersion;
  onBack: () => void;
}

/** Step 6: the summary of the version and the route it takes, and the sending. */
export function BuilderSubmitStep({ version, onBack }: BuilderSubmitStepProps) {
  const { message } = App.useApp();
  const navigate = useNavigate();
  const matrix = useQuery(matrixQuery(version.matrixId));
  const track = useQuery(trackQuery(version.entries.track.id));
  const completeness = useQuery({
    ...matrixCompletenessQuery(version.id),
    refetchOnMount: "always",
  });
  const publication = useQuery({
    ...matrixPublicationQuery(version.id),
    refetchOnMount: "always",
  });
  const transition = useVersionTransition();
  const problems = completeness.data?.problems ?? null;
  const preview = publication.data;
  const isPublishing = preview?.publishingActions.includes("Submit") === true;
  const canSubmit = version.actions.includes("Submit");
  const { content, authorName, superiorApprover, hr } = version;
  const route = [
    `Автор: ${shortName(authorName ?? "")}`,
    superiorApprover
      ? `Вышестоящий руководитель: ${shortName(superiorApprover.name)}`
      : `Вышестоящий руководитель: ${SKIPPED_NOTE}`,
    hr ? `HR: ${shortName(hr.name)}` : "HR",
    "Публикация",
  ].join(" → ");
  const receiver = superiorApprover ?? hr;

  function submit() {
    void transition.decide({
      version,
      transition: "submit",
      preview,
      publishing: {
        isAskedFirst: false,
        title: "Отправить и опубликовать версию?",
        okText: "Отправить и опубликовать",
        lead: (count) =>
          count > 0
            ? `Вместе с версией станут действующими ${draftRecords(count)}:`
            : "Черновых записей нет: публикуется только версия.",
      },
      onDone: (saved, count) => {
        message.success(
          outcomeText(saved, count, "Версия отправлена на согласование"),
        );
        void navigate(deskPaths.desk);
      },
    });
  }

  if (problems === null || !preview) return <Skeleton active />;

  return (
    <Flex vertical gap="middle">
      <Typography.Title level={3} style={{ margin: 0 }}>
        Отправка
      </Typography.Title>
      {problems.length > 0 ? (
        <CompletenessBlock
          problems={problems}
          trackId={version.entries.track.id}
          warning={(count) =>
            `Версию пока нельзя отправить: ${remarksText(count)}`
          }
          returnLabel="К построению матрицы"
        />
      ) : (
        <Alert
          type="success"
          showIcon
          icon={<CheckCircleOutlined />}
          title="Версия полна"
          description="Замечаний нет: её можно отправить."
        />
      )}
      <Descriptions
        column={1}
        items={[
          {
            key: "unit",
            label: "Подразделение",
            children: matrix.data?.orgUnitName,
          },
          {
            key: "track",
            label: "Трек, шкала",
            children: `${version.entries.track.name}${track.data?.status === "Draft" ? " (черновой)" : ""} · ${version.entries.scale.name}`,
          },
          {
            key: "content",
            label: "Содержание",
            children: `Компетенций: ${content.competencies.length} · уровней: ${content.levels.length} · блоков: ${content.blocks.length}`,
          },
        ]}
      />
      {isPublishing ? (
        <Flex vertical gap="small">
          <Alert
            type="warning"
            showIcon
            title="Отправка опубликует версию"
            description={
              preview.entries.length > 0
                ? `Вы — ответственный HR этого подразделения, шаг 2 согласуется автоматически. Вместе с версией станут действующими ${draftRecords(preview.entries.length)}.`
                : "Вы — ответственный HR этого подразделения, шаг 2 согласуется автоматически. Черновых записей нет: публикуется только версия."
            }
          />
          {preview.entries.length > 0 && (
            <CascadeTable entries={preview.entries} />
          )}
        </Flex>
      ) : (
        <section>
          <Typography.Title level={4}>Маршрут согласования</Typography.Title>
          <Typography.Paragraph>{route}</Typography.Paragraph>
          {receiver && (
            <Typography.Paragraph type="secondary">
              {`Версия уйдёт ${shortName(receiver.name)}; ему и далее HR придёт письмо.`}
              {track.data?.status === "Draft" &&
                ` Трек «${version.entries.track.name}» — черновой: он опубликуется вместе с версией, если HR вправе его утвердить.`}
            </Typography.Paragraph>
          )}
        </section>
      )}
      {transition.error != null && (
        <DecisionError
          title="Версию нельзя отправить"
          error={transition.error}
        />
      )}
      <Flex gap="small" align="center" wrap>
        <Button disabled={transition.isPending} onClick={onBack}>
          Назад
        </Button>
        <Button
          type="primary"
          loading={transition.isPending}
          disabled={!canSubmit || problems.length > 0}
          onClick={submit}
        >
          {isPublishing
            ? "Отправить и опубликовать"
            : "Отправить на согласование"}
        </Button>
        {problems.length > 0 && (
          <Typography.Text type="secondary">
            {`Отправка недоступна: ${remarksText(problems.length)}`}
          </Typography.Text>
        )}
      </Flex>
    </Flex>
  );
}
