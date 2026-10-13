import { useQuery } from "@tanstack/react-query";
import { Alert, App, Flex, Skeleton, Steps, Tag, Typography } from "antd";
import { useEffect, useState } from "react";
import {
  useBlocker,
  useNavigate,
  useParams,
  useSearchParams,
} from "react-router";
import { MatrixEditor } from "../career/MatrixEditor";
import {
  type MatrixVersion,
  matrixCompletenessQuery,
  matrixQuery,
  matrixVersionQuery,
} from "../career/matrixApi";
import { VERSION_STATUS_LABELS } from "../career/matrixLabels";
import { BuilderCheckStep } from "./BuilderCheckStep";
import { BuilderCompetenciesStep } from "./BuilderCompetenciesStep";
import { BuilderSubmitStep } from "./BuilderSubmitStep";
import { BuilderTrackStep } from "./BuilderTrackStep";
import type { StepProps } from "./builderStep";
import { BUILDER_STEPS, deskPaths } from "./deskTexts";
import { LoadFailure, ScenarioPage } from "./ScenarioPage";
import { PROBLEM_STEP, remarksText } from "./scenarioTexts";

const FIRST_STEP = 1;
const COMPETENCIES_STEP = 2;
const LAST_STEP = BUILDER_STEPS.length;
const UNSAVED_MESSAGE =
  "Есть несохранённые правки. Сохраните их или отмените, чтобы перейти на другой экран.";

/** «Построить матрицу компетенций»: six steps from the track to the sending, each saved as the draft of a version. */
export function MatrixBuilderPage() {
  const { versionId } = useParams();
  const read = useQuery({
    ...matrixVersionQuery(versionId ?? ""),
    enabled: versionId !== undefined,
  });
  return (
    <ScenarioPage title="Построить матрицу компетенций">
      {read.error ? (
        <LoadFailure
          title="Не удалось загрузить версию"
          error={read.error}
          onRetry={() => void read.refetch()}
        />
      ) : versionId !== undefined && !read.data ? (
        <Skeleton active />
      ) : (
        <BuilderShell key={read.data?.id} version={read.data} />
      )}
    </ScenarioPage>
  );
}

/** The step a version opens on: the one asked for by the address, else where the author stopped. */
function openingStep(
  version: MatrixVersion | undefined,
  requested: string | null,
) {
  if (!version) return FIRST_STEP;
  const asked = Number(requested);
  if (Number.isInteger(asked) && asked > FIRST_STEP && asked <= LAST_STEP) {
    return asked;
  }
  return version.builderStep ?? COMPETENCIES_STEP;
}

function BuilderShell({ version }: { version: MatrixVersion | undefined }) {
  const { message } = App.useApp();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const requested = params.get("step");
  const opening = openingStep(version, requested);
  const [step, setStep] = useState(opening);
  const [reached, setReached] = useState(
    Math.max(opening, version?.builderStep ?? COMPETENCIES_STEP),
  );
  const [isUnsaved, setIsUnsaved] = useState(false);
  const matrix = useQuery({
    ...matrixQuery(version?.matrixId ?? ""),
    enabled: version !== undefined,
  });
  const completeness = useQuery({
    ...matrixCompletenessQuery(version?.id ?? ""),
    enabled: version !== undefined && reached >= LAST_STEP - 1,
  });
  const blocker = useBlocker(
    ({ nextLocation }) =>
      isUnsaved && !nextLocation.pathname.startsWith(deskPaths.matrixBuilder),
  );
  useEffect(() => {
    if (blocker.state === "blocked") {
      message.warning(UNSAVED_MESSAGE);
      blocker.reset();
    }
  }, [blocker, message]);

  const remarksByStep = new Map<number, number>();
  for (const problem of completeness.data?.problems ?? []) {
    const problemStep = PROBLEM_STEP[problem.code];
    if (problemStep !== null) {
      remarksByStep.set(problemStep, (remarksByStep.get(problemStep) ?? 0) + 1);
    }
  }

  function goTo(next: number) {
    setIsUnsaved(false);
    setStep(next);
    setReached((previous) => Math.max(previous, next));
  }

  const isEditable = version?.actions.includes("Edit") === true;
  const stepProps: StepProps | undefined = version && {
    version,
    step,
    onBack: () => goTo(step - 1),
    onSkip: () => goTo(step + 1),
    onSaved: (then) =>
      then === "next" ? goTo(step + 1) : void navigate(deskPaths.desk),
    onUnsavedChange: setIsUnsaved,
  };

  return (
    <Flex vertical gap="large">
      {version && (
        <Flex align="center" gap="small" wrap>
          <Typography.Text>
            {[matrix.data?.orgUnitName, `трек «${version.entries.track.name}»`]
              .filter(Boolean)
              .join(" · ")}
          </Typography.Text>
          <Tag>{`Версия ${version.number}`}</Tag>
          <Tag>{VERSION_STATUS_LABELS[version.status]}</Tag>
        </Flex>
      )}
      <Steps
        current={step - 1}
        onChange={(index) => goTo(index + 1)}
        items={BUILDER_STEPS.map((title, index) => {
          const number = index + 1;
          const remarks = remarksByStep.get(number);
          return {
            title,
            disabled: !version || number > reached || isUnsaved,
            status:
              number === step
                ? "process"
                : number < step || (version && number <= reached)
                  ? "finish"
                  : "wait",
            subTitle: number === step ? "Сейчас" : undefined,
            description: remarks ? (
              <Tag color="warning">{`Замечания: ${remarks}`}</Tag>
            ) : undefined,
          };
        })}
      />
      {version &&
        version.builderStep !== null &&
        version.builderStep !== undefined &&
        version.builderStep > COMPETENCIES_STEP &&
        !requested &&
        step === version.builderStep && (
          <Alert
            type="info"
            showIcon
            title={`Продолжаем с шага ${step} «${BUILDER_STEPS[step - 1]}», где вы остановились. Шаги до него сохранены.`}
          />
        )}
      {version && !isEditable && (
        <Alert
          type="info"
          showIcon
          title={
            version.status === "Draft"
              ? `Версия сейчас не ваша: править её может ${version.authorName}`
              : `Версия уже не черновик: сейчас она «${VERSION_STATUS_LABELS[version.status]}»`
          }
        />
      )}
      {remarksByStep.size > 0 && step < LAST_STEP - 1 && (
        <Alert
          type="warning"
          showIcon
          title={`В версии ${remarksText([...remarksByStep.values()].reduce((sum, count) => sum + count, 0))}: подробности на шаге «Проверка полноты».`}
        />
      )}
      <StepBody
        step={step}
        version={version}
        stepProps={stepProps}
        preselected={{
          unitId: params.get("unitId") ?? undefined,
          trackId: params.get("trackId") ?? undefined,
        }}
        onGoToStep={goTo}
        onUnsavedChange={setIsUnsaved}
      />
    </Flex>
  );
}

interface StepBodyProps {
  step: number;
  version: MatrixVersion | undefined;
  stepProps: StepProps | undefined;
  preselected: { unitId?: string; trackId?: string };
  onGoToStep: (step: number) => void;
  onUnsavedChange: (isUnsaved: boolean) => void;
}

function StepBody({
  step,
  version,
  stepProps,
  preselected,
  onGoToStep,
  onUnsavedChange,
}: StepBodyProps) {
  if (step === FIRST_STEP || !version || !stepProps) {
    return (
      <BuilderTrackStep
        version={version}
        preselected={preselected}
        onNext={() => onGoToStep(COMPETENCIES_STEP)}
        onUnsavedChange={onUnsavedChange}
      />
    );
  }
  switch (step) {
    case 2:
      return <BuilderCompetenciesStep {...stepProps} />;
    case 3:
    case 4:
      return (
        <MatrixEditor
          version={version}
          onUnsavedChange={onUnsavedChange}
          builder={{
            part: step === 3 ? "requirements" : "formula",
            step,
            onBack: stepProps.onBack,
            onSaved: stepProps.onSaved,
            onSkip: stepProps.onSkip,
          }}
        />
      );
    case 5:
      return <BuilderCheckStep {...stepProps} onGoToStep={onGoToStep} />;
    default:
      return <BuilderSubmitStep version={version} onBack={stepProps.onBack} />;
  }
}
