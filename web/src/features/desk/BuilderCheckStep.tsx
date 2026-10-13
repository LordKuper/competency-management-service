import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Button, Flex, Typography } from "antd";
import { MatrixErrorAlert } from "../career/MatrixErrorAlert";
import {
  invalidateMatrices,
  matrixCompletenessQuery,
  matrixVersionQuery,
  saveMatrixContent,
} from "../career/matrixApi";
import type { StepProps } from "./builderStep";
import { CompletenessBlock } from "./CompletenessBlock";
import { LoadFailure } from "./ScenarioPage";
import { remarksText } from "./scenarioTexts";

const SUBMIT_STEP = 6;

interface BuilderCheckStepProps extends StepProps {
  /** Takes the user to the step where a problem is put right. */
  onGoToStep: (step: number) => void;
}

/** Step 5: what keeps the saved draft from being sent, and where to put each thing right. */
export function BuilderCheckStep({
  version,
  onBack,
  onSaved,
  onSkip,
  onGoToStep,
}: BuilderCheckStepProps) {
  const queryClient = useQueryClient();
  const completeness = useQuery({
    ...matrixCompletenessQuery(version.id),
    refetchOnMount: "always",
  });
  const problems = completeness.data?.problems ?? null;
  const goOn = useMutation({
    mutationFn: () =>
      saveMatrixContent(
        version,
        version.content,
        Math.max(version.builderStep ?? 0, SUBMIT_STEP),
      ),
    onSuccess: (saved) => {
      queryClient.setQueryData(matrixVersionQuery(saved.id).queryKey, saved);
      onSaved("next");
    },
    onSettled: () => invalidateMatrices(queryClient),
  });
  const isEditable = version.actions.includes("Edit");
  const hasProblems = problems !== null && problems.length > 0;

  return (
    <Flex vertical gap="middle">
      <Typography.Title level={3} style={{ margin: 0 }}>
        Проверка полноты
      </Typography.Title>
      {completeness.error ? (
        <LoadFailure
          title="Не удалось выполнить проверку"
          error={completeness.error}
          onRetry={() => void completeness.refetch()}
        />
      ) : (
        <CompletenessBlock
          problems={problems}
          trackId={version.entries.track.id}
          warning={(count) =>
            `Версию пока нельзя отправить: ${remarksText(count)}`
          }
          returnLabel="К построению матрицы"
          onStep={onGoToStep}
        />
      )}
      {goOn.error != null && (
        <MatrixErrorAlert
          title="Не удалось перейти к отправке"
          error={goOn.error}
        />
      )}
      <Flex gap="small" align="center" wrap>
        <Button disabled={goOn.isPending} onClick={onBack}>
          Назад
        </Button>
        <Button
          type="primary"
          loading={goOn.isPending}
          disabled={problems === null || hasProblems}
          onClick={() => (isEditable ? goOn.mutate() : onSkip())}
        >
          Перейти к отправке
        </Button>
        {hasProblems && (
          <Typography.Text type="secondary">
            {`Отправка недоступна: ${remarksText(problems.length)}`}
          </Typography.Text>
        )}
      </Flex>
    </Flex>
  );
}
