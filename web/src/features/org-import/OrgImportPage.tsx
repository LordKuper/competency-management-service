import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Flex } from "antd";
import { useState } from "react";
import { deskQueryKey } from "../../api/queryClient";
import { useLeaveGuard } from "../../app/leaveGuard";
import { careerQueryKey } from "../career/careerApi";
import { CompactAlert } from "../desk/ScenarioContext";
import { ScenarioPage } from "../desk/ScenarioPage";
import { StepRuler } from "../desk/StepRuler";
import {
  invalidateLegalEntities,
  invalidateOrgStructure,
} from "../org-structure/orgStructureApi";
import { invalidatePositions } from "../positions/positionsApi";
import { usersQueryKey } from "../users/usersApi";
import { HeadsStep } from "./HeadsStep";
import {
  choicesStillListed,
  type HeadChoices,
  headDecisionsOf,
} from "./importHeads";
import { decisionsOf, type KeptIds, keptStillMissing } from "./importPlan";
import { MissingStep } from "./MissingStep";
import {
  applyOrgImport,
  type ImportError,
  type OrgImportPlan,
  type OrgImportPreview,
  type OrgImportResult,
  previewOrgImport,
} from "./orgImportApi";
import { PreviewStep } from "./PreviewStep";
import { ResultStep } from "./ResultStep";
import { UploadStep } from "./UploadStep";

const STEPS = [
  "Загрузка",
  "Предпросмотр",
  "Отсутствующие",
  "Руководители",
  "Итог",
] as const;
const UPLOAD = 1;
const PREVIEW = 2;
const MISSING = 3;
const HEADS = 4;
const RESULT = 5;
const NOTHING_KEPT: KeptIds = new Set();
const NO_CHOICES: HeadChoices = new Map();

interface Loaded {
  file: File;
  plan: OrgImportPlan;
}

interface Applied {
  fileName: string;
  result: OrgImportResult;
}

/**
 * «Импорт оргструктуры»: the file, its plan and the decisions live only in this page's state; nothing is written to
 * the server before «Применить», and leaving the page cancels the import.
 */
export function OrgImportPage() {
  const queryClient = useQueryClient();
  const [step, setStep] = useState(UPLOAD);
  const [reached, setReached] = useState(UPLOAD);
  const [loaded, setLoaded] = useState<Loaded>();
  const [errors, setErrors] = useState<readonly ImportError[]>([]);
  const [kept, setKept] = useState<KeptIds>(NOTHING_KEPT);
  const [headChoices, setHeadChoices] = useState<HeadChoices>(NO_CHOICES);
  const [applied, setApplied] = useState<Applied>();
  const [isPlanStale, setIsPlanStale] = useState(false);
  const previewing = useMutation({ mutationFn: previewOrgImport });
  const applying = useMutation({ mutationFn: applyOrgImport });
  const isBusy = previewing.isPending || applying.isPending;
  const { confirmLeave } = useLeaveGuard({
    isUnsaved: loaded !== undefined,
    isSaving: applying.isPending,
  });

  function show(
    file: File,
    preview: OrgImportPreview,
    keptBefore: KeptIds,
    choicesBefore: HeadChoices,
  ) {
    const { plan } = preview;
    if (plan === null || preview.errors.length > 0) {
      setLoaded(undefined);
      setErrors(preview.errors);
      setKept(NOTHING_KEPT);
      setHeadChoices(NO_CHOICES);
      setStep(UPLOAD);
      setReached(UPLOAD);
      return;
    }
    setLoaded({ file, plan });
    setErrors([]);
    setKept(keptStillMissing(plan, keptBefore));
    setHeadChoices(choicesStillListed(plan, choicesBefore));
    setStep(PREVIEW);
    setReached(PREVIEW);
  }

  function selectFile(file: File) {
    const load = () => {
      setIsPlanStale(false);
      previewing.mutate(file, {
        onSuccess: (preview) => show(file, preview, NOTHING_KEPT, NO_CHOICES),
      });
    };
    if (kept.size > 0 || headChoices.size > 0) confirmLeave(load);
    else load();
  }

  function apply() {
    if (!loaded) return;
    const { file, plan } = loaded;
    setIsPlanStale(false);
    applying.mutate(
      {
        file,
        fingerprint: plan.fingerprint,
        decisions: decisionsOf(plan, kept),
        heads: headDecisionsOf(plan, kept, headChoices),
      },
      {
        onSuccess: (outcome) => {
          if ("recomputed" in outcome) {
            setIsPlanStale(true);
            show(file, outcome.recomputed, kept, headChoices);
            return;
          }
          void Promise.all([
            queryClient.invalidateQueries({ queryKey: deskQueryKey }),
            invalidateOrgStructure(queryClient),
            invalidateLegalEntities(queryClient),
            invalidatePositions(queryClient),
            queryClient.invalidateQueries({ queryKey: usersQueryKey }),
            queryClient.invalidateQueries({ queryKey: careerQueryKey }),
          ]);
          setApplied({ fileName: file.name, result: outcome.applied });
          setLoaded(undefined);
          setKept(NOTHING_KEPT);
          setHeadChoices(NO_CHOICES);
          setStep(RESULT);
          setReached(RESULT);
        },
      },
    );
  }

  function decide(ids: readonly string[], isKept: boolean) {
    setKept((previous) => {
      const next = new Set(previous);
      for (const id of ids) {
        if (isKept) next.add(id);
        else next.delete(id);
      }
      return next;
    });
  }

  function chooseHead(unitKey: string, employeeKey: string | null) {
    setHeadChoices((previous) => new Map(previous).set(unitKey, employeeKey));
  }

  return (
    <ScenarioPage title="Импорт оргструктуры">
      <Flex vertical gap="middle" data-tour="import-flow">
        <StepRuler
          steps={STEPS}
          current={step}
          reached={reached}
          isLocked={isBusy || applied !== undefined}
          onChange={setStep}
        />
        {isPlanStale && (
          <CompactAlert
            type="warning"
            lead="План изменился после предпросмотра."
            text="Оргструктура или файл стали другими, поэтому предпросмотр пересчитан: проверьте его. Решения, которые остались применимы, сохранены."
          />
        )}
        {step === UPLOAD && (
          <UploadStep
            loadedName={loaded?.file.name}
            errors={errors}
            failure={previewing.error}
            isLoading={previewing.isPending}
            onSelect={selectFile}
            onNext={() => setStep(PREVIEW)}
          />
        )}
        {step === PREVIEW && loaded && (
          <PreviewStep
            fileName={loaded.file.name}
            plan={loaded.plan}
            onBack={() => setStep(UPLOAD)}
            onNext={() => {
              setStep(MISSING);
              setReached((previous) => Math.max(previous, MISSING));
            }}
          />
        )}
        {step === MISSING && loaded && (
          <MissingStep
            plan={loaded.plan}
            kept={kept}
            onDecide={decide}
            onBack={() => setStep(PREVIEW)}
            onNext={() => {
              setStep(HEADS);
              setReached((previous) => Math.max(previous, HEADS));
            }}
          />
        )}
        {step === HEADS && loaded && (
          <HeadsStep
            plan={loaded.plan}
            kept={kept}
            choices={headChoices}
            onChoose={chooseHead}
            isApplying={applying.isPending}
            failure={applying.error}
            onBack={() => setStep(MISSING)}
            onApply={apply}
          />
        )}
        {step === RESULT && applied && (
          <ResultStep fileName={applied.fileName} result={applied.result} />
        )}
      </Flex>
    </ScenarioPage>
  );
}
