import { useQuery } from "@tanstack/react-query";
import { Alert, App, Flex, Skeleton, Tag, Typography } from "antd";
import { useEffect, useState } from "react";
import {
  useBlocker,
  useNavigate,
  useParams,
  useSearchParams,
} from "react-router";
import { trackQuery } from "../career/catalogApi";
import { MatrixEditor } from "../career/MatrixEditor";
import {
  type Matrix,
  type MatrixVersion,
  matrixCompletenessQuery,
  matrixQuery,
  matrixVersionQuery,
} from "../career/matrixApi";
import { VERSION_STATUS_LABELS } from "../career/matrixLabels";
import { BuilderCheckStep } from "./BuilderCheckStep";
import { BuilderCompetenciesStep } from "./BuilderCompetenciesStep";
import { BuilderSubmitStep } from "./BuilderSubmitStep";
import { type Basis, BuilderTrackStep } from "./BuilderTrackStep";
import type { StepProps } from "./builderStep";
import { BUILDER_STEPS, builderSteps, deskPaths } from "./deskTexts";
import { ContextLine } from "./ScenarioContext";
import { LoadFailure, ScenarioPage } from "./ScenarioPage";
import { StepRuler } from "./StepRuler";
import { PROBLEM_STEP } from "./scenarioTexts";

const FIRST_STEP = 1;
const COMPETENCIES_STEP = 2;
const LAST_STEP = BUILDER_STEPS.length;
const UNSAVED_MESSAGE =
  "Есть несохранённые правки. Сохраните их или отмените, чтобы перейти на другой экран.";

/** «Построить матрицу компетенций»: six steps from the track to the sending, each saved as the draft of a version. */
export function MatrixBuilderPage() {
  const { versionId } = useParams();
  const [params] = useSearchParams();
  const matrixId = params.get("matrixId") ?? undefined;
  const read = useQuery({
    ...matrixVersionQuery(versionId ?? ""),
    enabled: versionId !== undefined,
  });
  const startedFrom = useQuery({
    ...matrixQuery(matrixId ?? ""),
    enabled: versionId === undefined && matrixId !== undefined,
  });
  return (
    <ScenarioPage title="Построить матрицу компетенций">
      {startedFrom.error ? (
        <LoadFailure
          title="Не удалось загрузить матрицу"
          error={startedFrom.error}
          onRetry={() => void startedFrom.refetch()}
        />
      ) : matrixId !== undefined &&
        versionId === undefined &&
        !startedFrom.data ? (
        <Skeleton active />
      ) : read.error ? (
        <LoadFailure
          title="Не удалось загрузить версию"
          error={read.error}
          onRetry={() => void read.refetch()}
        />
      ) : versionId !== undefined && !read.data ? (
        <Skeleton active />
      ) : (
        <BuilderShell
          key={read.data?.id}
          version={read.data}
          startedFrom={startedFrom.data}
        />
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

interface BuilderShellProps {
  version: MatrixVersion | undefined;
  /** The matrix a new version is started for, when the desk sent the user here from it. */
  startedFrom?: Matrix;
}

function BuilderShell({ version, startedFrom }: BuilderShellProps) {
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
  const preselected = {
    unitId: params.get("unitId") ?? startedFrom?.orgUnitId ?? undefined,
    trackId: params.get("trackId") ?? startedFrom?.trackId ?? undefined,
    positionId:
      params.get("positionId") ?? startedFrom?.positionId ?? undefined,
    positionName: startedFrom?.positionName ?? undefined,
  };
  const [chosenBasis, setBasis] = useState<Basis>(
    preselected.positionId ? "position" : "track",
  );
  const basis: Basis = version
    ? version.entries.position
      ? "position"
      : "track"
    : chosenBasis;
  const matrix = useQuery({
    ...matrixQuery(version?.matrixId ?? ""),
    enabled: version !== undefined,
  });
  const track = useQuery({
    ...trackQuery(version?.entries.track?.id ?? ""),
    enabled: !!version?.entries.track,
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
    <Flex vertical gap="middle">
      {version && (
        <ContextLine>
          <Typography.Text>
            {[
              matrix.data?.orgUnitName,
              version.entries.position
                ? `должность «${version.entries.position.name}»`
                : `трек «${version.entries.track?.name}»`,
            ]
              .filter(Boolean)
              .join(" · ")}
          </Typography.Text>
          {track.data?.status === "Draft" && <Tag>Трек черновой</Tag>}
          <Tag>{`Версия ${version.number}`}</Tag>
          <Tag>{VERSION_STATUS_LABELS[version.status]}</Tag>
        </ContextLine>
      )}
      <StepRuler
        steps={builderSteps(basis === "position")}
        current={step}
        reached={version ? reached : FIRST_STEP}
        isLocked={isUnsaved}
        remarks={remarksByStep}
        onChange={goTo}
      />
      {version && !isEditable ? (
        <Alert
          type="info"
          showIcon
          title={
            version.status === "Draft"
              ? `Версия сейчас не ваша: править её может ${version.authorName}`
              : `Версия уже не черновик: сейчас она «${VERSION_STATUS_LABELS[version.status]}»`
          }
        />
      ) : (
        version &&
        version.builderStep !== null &&
        version.builderStep !== undefined &&
        version.builderStep > COMPETENCIES_STEP &&
        !requested &&
        step === version.builderStep && (
          <Alert
            type="info"
            showIcon
            title={`Продолжаем с шага ${step} «${builderSteps(basis === "position")[step - 1]}», где вы остановились. Шаги до него сохранены.`}
          />
        )
      )}
      <StepBody
        step={step}
        version={version}
        stepProps={stepProps}
        preselected={preselected}
        basis={basis}
        onBasisChange={setBasis}
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
  preselected: {
    unitId?: string;
    trackId?: string;
    positionId?: string;
    positionName?: string;
  };
  basis: Basis;
  onBasisChange: (basis: Basis) => void;
  onGoToStep: (step: number) => void;
  onUnsavedChange: (isUnsaved: boolean) => void;
}

function StepBody({
  step,
  version,
  stepProps,
  preselected,
  basis,
  onBasisChange,
  onGoToStep,
  onUnsavedChange,
}: StepBodyProps) {
  if (step === FIRST_STEP || !version || !stepProps) {
    return (
      <BuilderTrackStep
        version={version}
        preselected={preselected}
        basis={basis}
        onBasisChange={onBasisChange}
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
          key={step}
          version={version}
          onUnsavedChange={onUnsavedChange}
          builder={{
            part: step === 3 ? "requirements" : "formula",
            step,
            onBack: stepProps.onBack,
            onSaved: stepProps.onSaved,
            onSkip: stepProps.onSkip,
            onEditComposition: () => onGoToStep(COMPETENCIES_STEP),
          }}
        />
      );
    case 5:
      return <BuilderCheckStep {...stepProps} onGoToStep={onGoToStep} />;
    default:
      return <BuilderSubmitStep version={version} onBack={stepProps.onBack} />;
  }
}
