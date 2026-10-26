import { CheckCircleOutlined } from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import { App, Button, Flex, Skeleton, Typography } from "antd";
import { useEffect } from "react";
import { useNavigate } from "react-router";
import type { LeaveEdits } from "../../app/leaveGuard";
import { trackQuery } from "../career/catalogApi";
import {
  type MatrixVersion,
  matrixCompletenessQuery,
  matrixPublicationQuery,
  matrixQuery,
} from "../career/matrixApi";
import { sharedBlocksOf } from "../career/matrixContent";
import { versionCompetencyIds } from "../career/matrixGroups";
import { SKIPPED_NOTE } from "../career/matrixLabels";
import { draftRecords, publishedCount } from "../career/usePublishEntry";
import { CascadeTable } from "./CascadeTable";
import { CompletenessBlock } from "./CompletenessBlock";
import { DecisionError } from "./DecisionError";
import { deskPaths, shortName } from "./deskTexts";
import { ActionBar, CompactAlert } from "./ScenarioContext";
import { LoadFailure } from "./ScenarioPage";
import { outcomeText, remarksText } from "./scenarioTexts";
import { useVersionTransition } from "./useVersionTransition";

interface BuilderSubmitStepProps {
  version: MatrixVersion;
  onBack: () => void;
  /** Told whether the sending is in flight, so the wizard holds its steps until it ends. */
  onEditsChange?: (edits: LeaveEdits) => void;
}

/** Step 6: the summary of the version and the route it takes, and the sending. */
export function BuilderSubmitStep({
  version,
  onBack,
  onEditsChange,
}: BuilderSubmitStepProps) {
  const { message } = App.useApp();
  const navigate = useNavigate();
  const track = useQuery({
    ...trackQuery(version.entries.track?.id ?? ""),
    enabled: version.entries.track !== null,
  });
  const matrix = useQuery(matrixQuery(version.matrixId));
  const completeness = useQuery({
    ...matrixCompletenessQuery(version.id),
    refetchOnMount: "always",
  });
  const publication = useQuery({
    ...matrixPublicationQuery(version.id),
    refetchOnMount: "always",
  });
  const transition = useVersionTransition();
  const isSaving = transition.isPending;
  useEffect(
    () => onEditsChange?.({ isUnsaved: false, isSaving }),
    [isSaving, onEditsChange],
  );
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

  const failure = completeness.error ?? publication.error;
  if (failure) {
    return (
      <Flex vertical gap="middle">
        <LoadFailure
          title="Не удалось подготовить отправку"
          error={failure}
          onRetry={() => {
            void completeness.refetch();
            void publication.refetch();
          }}
        />
        <ActionBar start={<Button onClick={onBack}>Назад</Button>}>
          <Typography.Text type="secondary">
            Отправка недоступна, пока не загружены проверка полноты и перечень
            публикации.
          </Typography.Text>
          <Button type="primary" disabled>
            Отправить на согласование
          </Button>
        </ActionBar>
      </Flex>
    );
  }
  if (problems === null || !preview) return <Skeleton active />;
  const cascadeCount = publishedCount(preview);

  return (
    <Flex vertical gap="middle">
      {problems.length > 0 ? (
        <CompletenessBlock
          problems={problems}
          trackId={version.entries.track?.id ?? ""}
          warning={(count) =>
            `Версию пока нельзя отправить: ${remarksText(count)}`
          }
          originName="Построить матрицу компетенций"
        />
      ) : (
        <CompactAlert
          type="success"
          icon={<CheckCircleOutlined />}
          lead="Версия полна:"
          text="замечаний нет, можно отправлять на согласование."
        />
      )}
      {version.entries.position && (
        <Typography.Text strong>
          {`Матрица должности «${version.entries.position.name}» — ${matrix.data?.orgUnitName ?? ""}`}
        </Typography.Text>
      )}
      <Typography.Text>
        {[
          `Содержание: компетенций ${versionCompetencyIds(version).length}`,
          version.entries.position ? null : `уровней ${content.levels.length}`,
          `общих блоков ${sharedBlocksOf(content).length}`,
        ]
          .filter(Boolean)
          .join(" · ")}
      </Typography.Text>
      {isPublishing ? (
        <Flex vertical gap="small">
          <CompactAlert
            type="warning"
            lead="Отправка опубликует версию"
            text={
              cascadeCount > 0
                ? `и вместе с ней ${draftRecords(cascadeCount)}: вы — ответственный HR этого подразделения, шаг 2 согласуется автоматически.`
                : "без черновых записей: вы — ответственный HR этого подразделения, шаг 2 согласуется автоматически."
            }
          />
          {cascadeCount > 0 && <CascadeTable drafts={preview} />}
        </Flex>
      ) : (
        <section>
          <Typography.Title level={4}>Маршрут согласования</Typography.Title>
          <Typography.Paragraph>{route}</Typography.Paragraph>
          {receiver && (
            <Typography.Paragraph type="secondary">
              {`Версия уйдёт ${shortName(receiver.name)}; ему и далее HR придёт письмо.`}
              {track.data?.status === "Draft" &&
                ` Трек «${version.entries.track?.name}» — черновой: он опубликуется вместе с версией, если HR вправе его утвердить.`}
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
      <ActionBar
        start={
          <Button disabled={transition.isPending} onClick={onBack}>
            Назад
          </Button>
        }
      >
        {problems.length > 0 && (
          <Typography.Text type="secondary">
            {`Отправка недоступна: ${remarksText(problems.length)}`}
          </Typography.Text>
        )}
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
      </ActionBar>
    </Flex>
  );
}
