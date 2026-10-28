import type { ReactNode } from "react";
import type { OrgUnitTreeNode } from "../../org-structure/orgStructureApi";
import { ApplicationView } from "../ApplicationView";
import { ApplyView } from "../ApplyView";
import { CommissionView, type EmployeeOption } from "../CommissionView";
import { FormalView } from "../FormalView";
import { MyApplicationsView } from "../MyApplicationsView";
import { OpenWavesView } from "../OpenWavesView";
import type {
  Commission,
  OpenWave,
  PromotionApplication,
  PromotionSettings,
  RankingItem,
  Rating,
  RatingTask,
  Review,
  TargetForm,
  Versioned,
  Wave,
  WaveApplication,
} from "../promotionApi";
import { RankingView } from "../RankingView";
import { RatersView } from "../RatersView";
import { RatingsView } from "../RatingsView";
import { RatingView } from "../RatingView";
import { ReviewView } from "../ReviewView";
import { SettingsView } from "../SettingsView";
import { WaveApplicationsView } from "../WaveApplicationsView";
import { WaveListView } from "../WaveListView";
import { WaveView } from "../WaveView";

/** The roles that act in the cycle of a wave, as the banner of a step names them. */
export type DemoRole =
  | "Администратор"
  | "HR"
  | "Кандидат"
  | "Руководитель"
  | "Оценивающий"
  | "Член комиссии"
  | "Председатель комиссии";

/** One step of the cycle: who acts, what they do, and the screen they act on. */
export interface DemoStep {
  role: DemoRole;
  title: string;
  description: string;
  /** Stage of the wave the step belongs to; 0 before the wave starts. */
  stage: number;
  render: () => ReactNode;
}

const noop = async () => undefined;
const nothing = () => {};

const SCALE = [
  { score: 1, label: "Знаком с темой" },
  { score: 2, label: "Применяет с поддержкой" },
  { score: 3, label: "Применяет самостоятельно" },
  { score: 4, label: "Развивает других" },
];

const WAVE_ID = "demo-wave";
interface CompetencyRow {
  id: string;
  name: string;
  type: Review["competencies"][number]["type"];
  required: number;
  selfScore: number;
  finalScore: number;
  circular: Review["competencies"][number]["circular"];
  artifact?: { text: string; links: string[] };
  isArtifactMandatory?: boolean;
}

const DESIGN: CompetencyRow = {
  id: "demo-design",
  name: "Проектирование систем",
  type: "Critical",
  required: 3,
  selfScore: 3,
  finalScore: 3,
  circular: {
    average: 3.2,
    distribution: [
      { score: 3, count: 4 },
      { score: 4, count: 1 },
    ],
    notObservedCount: 0,
  },
  artifact: {
    text: "Спроектировал разбиение платформы оплаты на сервисы и провёл разбор решения с командой.",
    links: ["https://example.org/design-review"],
  },
  isArtifactMandatory: true,
};
const QUALITY: CompetencyRow = {
  id: "demo-quality",
  name: "Качество кода",
  type: "Profile",
  required: 3,
  selfScore: 4,
  finalScore: 4,
  circular: {
    average: 3.6,
    distribution: [
      { score: 3, count: 2 },
      { score: 4, count: 3 },
    ],
    notObservedCount: 0,
  },
  artifact: {
    text: "Ввёл обязательную проверку кода и сократил число возвратов задач из тестирования.",
    links: [],
  },
};
const TEAMWORK: CompetencyRow = {
  id: "demo-teamwork",
  name: "Работа в команде",
  type: "Additional",
  required: 2,
  selfScore: 3,
  finalScore: 3,
  circular: {
    average: 3,
    distribution: [{ score: 3, count: 4 }],
    notObservedCount: 1,
  },
};
const COMPETENCIES = [DESIGN, QUALITY, TEAMWORK];

const UNIT_DEV = "demo-unit-dev";
const UNIT_QA = "demo-unit-qa";
const TARGET = {
  unitId: UNIT_DEV,
  unitName: "Управление разработки",
  matrixId: "demo-matrix",
  trackId: "demo-track",
  trackName: "Разработка",
  levelId: "demo-level-senior",
  levelName: "Старший разработчик",
};

const CANDIDATE = { id: "demo-emp-candidate", name: "Иванова Анна Сергеевна" };
const PEOPLE = {
  hr: { id: "demo-emp-hr", name: "Белова Наталья Игоревна" },
  head: { id: "demo-emp-head", name: "Смирнов Алексей Викторович" },
  chair: { id: "demo-emp-chair", name: "Михайлов Сергей Петрович" },
  member: { id: "demo-emp-member", name: "Козлова Елена Андреевна" },
};
const RATERS = [
  { employeeId: "demo-emp-r1", fullName: "Петров Пётр Олегович" },
  { employeeId: "demo-emp-r2", fullName: "Сидорова Мария Павловна" },
  { employeeId: "demo-emp-r3", fullName: "Кузнецов Олег Денисович" },
  { employeeId: "demo-emp-r4", fullName: "Орлова Ирина Максимовна" },
  { employeeId: "demo-emp-r5", fullName: "Волков Дмитрий Романович" },
].map((rater) => ({ ...rater, source: "Candidate" as const }));
const SEARCH_RESULTS: EmployeeOption[] = [
  {
    id: "demo-emp-r6",
    name: "Громова Ольга Станиславовна",
    detail: "Отдел тестирования, инженер по тестированию",
  },
  {
    id: "demo-emp-r7",
    name: "Фёдоров Андрей Ильич",
    detail: "Управление разработки, разработчик",
  },
];

const SETTINGS: PromotionSettings = {
  minIntervalMonths: 12,
  stageDurationsDays: [21, 7, 14, 7, 14, 3, 14],
};

const UNITS: OrgUnitTreeNode[] = [
  {
    id: "demo-unit-root",
    name: "Технологический блок",
    parentId: null,
    headEmployeeId: null,
    isActive: true,
    version: 1,
    headName: null,
    employeeCount: 0,
  },
  {
    id: UNIT_DEV,
    name: TARGET.unitName,
    parentId: "demo-unit-root",
    headEmployeeId: PEOPLE.head.id,
    isActive: true,
    version: 1,
    headName: PEOPLE.head.name,
    employeeCount: 24,
  },
  {
    id: UNIT_QA,
    name: "Отдел тестирования",
    parentId: "demo-unit-root",
    headEmployeeId: null,
    isActive: true,
    version: 1,
    headName: null,
    employeeCount: 11,
  },
];

const COMMISSION: Versioned<Commission> = {
  unitId: UNIT_DEV,
  unitName: TARGET.unitName,
  chairEmployeeId: PEOPLE.chair.id,
  version: 1,
  members: [
    {
      employeeId: PEOPLE.hr.id,
      fullName: PEOPLE.hr.name,
      isRequired: true,
      isResponsibleHr: true,
      isHead: false,
      isActing: false,
    },
    {
      employeeId: PEOPLE.head.id,
      fullName: PEOPLE.head.name,
      isRequired: true,
      isResponsibleHr: false,
      isHead: true,
      isActing: false,
    },
    {
      employeeId: PEOPLE.chair.id,
      fullName: PEOPLE.chair.name,
      isRequired: false,
      isResponsibleHr: false,
      isHead: false,
      isActing: false,
    },
    {
      employeeId: PEOPLE.member.id,
      fullName: PEOPLE.member.name,
      isRequired: false,
      isResponsibleHr: false,
      isHead: false,
      isActing: false,
    },
  ],
};

const WAVE_DATES = {
  startsOn: "2027-03-01",
  endsOn: [
    "2027-03-14",
    "2027-03-21",
    "2027-04-04",
    "2027-04-11",
    "2027-04-25",
    "2027-04-28",
    "2027-05-12",
  ],
};

function wave(stage: number): Versioned<Wave> {
  return {
    ...WAVE_DATES,
    id: WAVE_ID,
    stage,
    isFinished: false,
    authorId: "demo-user-hr",
    authorName: PEOPLE.hr.name,
    units: [{ id: UNIT_DEV, name: TARGET.unitName }],
    version: 1,
  };
}

const OPEN_WAVE: OpenWave = {
  ...WAVE_DATES,
  id: WAVE_ID,
  stage: 1,
  units: [
    {
      id: UNIT_DEV,
      name: TARGET.unitName,
      targets: [
        {
          matrixId: TARGET.matrixId,
          trackId: TARGET.trackId,
          trackName: TARGET.trackName,
          levels: [
            { id: "demo-level-middle", name: "Разработчик" },
            { id: TARGET.levelId, name: TARGET.levelName },
          ],
        },
      ],
    },
  ],
};

const TARGET_FORM: TargetForm = {
  matrixId: TARGET.matrixId,
  unitId: UNIT_DEV,
  unitName: TARGET.unitName,
  trackName: TARGET.trackName,
  levels: [
    { id: "demo-level-middle", name: "Разработчик", requiredEducation: null },
    {
      id: TARGET.levelId,
      name: TARGET.levelName,
      requiredEducation: "Bachelor",
    },
  ],
  competencies: COMPETENCIES.map(({ id, name }) => ({
    id,
    name,
    scalePoints: SCALE,
  })),
};

const SELF_SCORES = COMPETENCIES.map(({ id, name, selfScore }) => ({
  competencyId: id,
  competencyName: name,
  score: selfScore,
}));

const FORMAL_ENTERED = {
  hiredOrPromotedOn: "2024-06-03",
  ignoreInterval: false,
  ignoreIntervalByName: null,
  ignoreIntervalAt: null,
};

const ARTIFACTS = COMPETENCIES.flatMap(({ id, name, artifact }) =>
  artifact ? [{ competencyId: id, competencyName: name, ...artifact }] : [],
);

const RESULT_COMPETENCIES = COMPETENCIES.map(
  ({ id, name, required, finalScore }) => ({
    competencyId: id,
    competencyName: name,
    requiredScore: required,
    score: finalScore,
    isMet: finalScore >= required,
  }),
);

function application(
  overrides: Partial<PromotionApplication>,
): PromotionApplication {
  return {
    id: "demo-application",
    waveId: WAVE_ID,
    stage: 1,
    status: "Submitted",
    roles: ["Candidate"],
    candidateEmployeeId: CANDIDATE.id,
    candidateName: CANDIDATE.name,
    target: TARGET,
    education: "Bachelor",
    selfScores: SELF_SCORES,
    raters: RATERS,
    formal: null,
    formalFailures: [],
    warnings: [],
    artifacts: null,
    mandatoryArtifactCompetencyIds: null,
    feedbackReceivedAt: null,
    result: null,
    ...overrides,
  };
}

function summary(
  overrides: Partial<WaveApplication> &
    Pick<WaveApplication, "id" | "candidateName">,
): WaveApplication {
  return {
    waveId: WAVE_ID,
    waveStartsOn: WAVE_DATES.startsOn,
    stage: 1,
    status: "Submitted",
    candidateEmployeeId: `demo-emp-${overrides.id}`,
    target: TARGET,
    submittedAt: "2027-03-05T09:30:00Z",
    decision: null,
    ...overrides,
  };
}

const MY_APPLICATION = summary({
  id: "demo-application",
  candidateName: CANDIDATE.name,
  candidateEmployeeId: CANDIDATE.id,
});

function waveApplications(stage: number, status: WaveApplication["status"]) {
  return [
    summary({
      id: "demo-application",
      candidateName: CANDIDATE.name,
      stage,
      status,
    }),
    summary({
      id: "demo-application-2",
      candidateName: "Орехов Игорь Леонидович",
      stage,
      status,
    }),
    summary({
      id: "demo-application-3",
      candidateName: "Захарова Дарья Борисовна",
      stage,
      status,
    }),
  ];
}

const RATING_TASKS: RatingTask[] = [
  {
    applicationId: "demo-application",
    waveId: WAVE_ID,
    candidateName: CANDIDATE.name,
    stage: 3,
    canRate: true,
    isRated: false,
  },
  {
    applicationId: "demo-application-2",
    waveId: WAVE_ID,
    candidateName: "Орехов Игорь Леонидович",
    stage: 3,
    canRate: true,
    isRated: true,
  },
];

const RATING: Rating = {
  applicationId: "demo-application",
  candidateName: CANDIDATE.name,
  stage: 3,
  canEdit: true,
  competencies: COMPETENCIES.map(({ id, name }) => ({
    id,
    name,
    scalePoints: SCALE,
  })),
  answers: null,
};

const RANKING: RankingItem[] = [
  {
    rank: 1,
    applicationId: "demo-application",
    candidateEmployeeId: CANDIDATE.id,
    candidateName: CANDIDATE.name,
    target: TARGET,
    status: "UnderReview",
    rating: 84.5,
    threshold: 70,
    isReady: true,
    decision: null,
  },
  {
    rank: 2,
    applicationId: "demo-application-2",
    candidateEmployeeId: "demo-emp-demo-application-2",
    candidateName: "Орехов Игорь Леонидович",
    target: TARGET,
    status: "UnderReview",
    rating: 66,
    threshold: 70,
    isReady: false,
    decision: null,
  },
  {
    rank: null,
    applicationId: "demo-application-3",
    candidateEmployeeId: "demo-emp-demo-application-3",
    candidateName: "Захарова Дарья Борисовна",
    target: TARGET,
    status: "UnderReview",
    rating: null,
    threshold: 70,
    isReady: null,
    decision: null,
  },
];

function review(overrides: Partial<Review>): Review {
  return {
    id: "demo-application",
    waveId: WAVE_ID,
    stage: 5,
    status: "UnderReview",
    roles: ["Member"],
    candidateEmployeeId: CANDIDATE.id,
    candidateName: CANDIDATE.name,
    target: TARGET,
    education: "Bachelor",
    formal: FORMAL_ENTERED,
    formalFailures: [],
    meetsAfterRating: true,
    artifactsMissing: false,
    competencies: COMPETENCIES.map((row) => ({
      competencyId: row.id,
      name: row.name,
      type: row.type,
      requiredScore: row.required,
      scalePoints: SCALE,
      selfScore: row.selfScore,
      circular: row.circular,
      artifact: row.artifact
        ? { competencyId: row.id, competencyName: row.name, ...row.artifact }
        : null,
      isArtifactMandatory: row.isArtifactMandatory ?? false,
      finalScore: row.finalScore,
      isMet: row.finalScore >= row.required,
    })),
    rating: 84.5,
    threshold: 70,
    isReady: true,
    decision: null,
    decidedByName: null,
    decidedAt: null,
    history: [
      {
        waveId: "demo-wave-previous",
        waveStartsOn: "2025-09-01",
        target: {
          ...TARGET,
          levelId: "demo-level-middle",
          levelName: "Разработчик",
        },
        decision: "Meets",
        finalScores: [
          {
            competencyId: DESIGN.id,
            competencyName: DESIGN.name,
            score: 3,
          },
        ],
      },
    ],
    ...overrides,
  };
}

/** The full cycle of a wave, from its announcement to the end of the feedback stage, step by step by the role that acts. */
export const DEMO_STEPS: readonly DemoStep[] = [
  {
    role: "Администратор",
    title: "Настройки повышения",
    description:
      "Администратор задаёт минимальный интервал между повышениями и длительность каждого этапа по умолчанию.",
    stage: 0,
    render: () => <SettingsView settings={SETTINGS} onSubmit={noop} />,
  },
  {
    role: "HR",
    title: "Состав комиссии подразделения",
    description:
      "Ответственный HR добавляет в комиссию подразделения участников и выбирает председателя. Тот, кто входит по должности, остаётся в составе.",
    stage: 0,
    render: () => (
      <CommissionView
        commission={COMMISSION}
        candidates={SEARCH_RESULTS}
        isSearching={false}
        onSearch={nothing}
        onSubmit={noop}
      />
    ),
  },
  {
    role: "HR",
    title: "Объявление волны",
    description:
      "HR выбирает дату начала приёма заявок, даты окончания этапов и подразделения волны. Даты окончания подставляются по настройкам.",
    stage: 0,
    render: () => (
      <WaveView
        units={UNITS}
        durations={SETTINGS.stageDurationsDays}
        canEdit
        onSubmit={noop}
      />
    ),
  },
  {
    role: "HR",
    title: "Список волн",
    description:
      "Объявленная волна появляется в списке. Из него HR открывает волну, чтобы следить за этапами и заявками.",
    stage: 0,
    render: () => (
      <WaveListView
        waves={[wave(0)]}
        total={1}
        page={1}
        pageSize={20}
        isLoading={false}
        onPageChange={nothing}
      />
    ),
  },
  {
    role: "Кандидат",
    title: "Открытые волны",
    description:
      "Сотрудник подразделений волны видит её сроки, подразделения, треки и уровни, на которые можно подать заявку.",
    stage: 1,
    render: () => (
      <OpenWavesView
        waves={[OPEN_WAVE]}
        applicationIdByWave={new Map()}
        canApply={false}
      />
    ),
  },
  {
    role: "Кандидат",
    title: "Заявка на повышение",
    description:
      "Кандидат выбирает подразделение, трек и уровень, указывает образование, оценивает себя по компетенциям и называет оценивающих.",
    stage: 1,
    render: () => (
      <ApplyView
        wave={OPEN_WAVE}
        application={application({})}
        targetForm={TARGET_FORM}
        isTargetLoading={false}
        candidates={SEARCH_RESULTS}
        candidateEmployeeId={CANDIDATE.id}
        isSearching={false}
        onSearch={nothing}
        onTargetChange={nothing}
        onSubmit={noop}
      />
    ),
  },
  {
    role: "Кандидат",
    title: "Мои заявки",
    description:
      "Поданная заявка появляется в списке заявок кандидата со статусом. Из карточки её можно открыть.",
    stage: 1,
    render: () => <MyApplicationsView applications={[MY_APPLICATION]} />,
  },
  {
    role: "HR",
    title: "Заявки волны",
    description:
      "На этапе формальной проверки ответственный HR видит заявки волны и открывает проверку каждой.",
    stage: 2,
    render: () => (
      <WaveApplicationsView
        applications={waveApplications(2, "Submitted")}
        total={3}
        page={1}
        pageSize={20}
        isLoading={false}
        onPageChange={nothing}
        onOpenFormal={nothing}
      />
    ),
  },
  {
    role: "HR",
    title: "Формальная проверка",
    description:
      "HR вносит дату приёма на работу или последнего повышения кандидата. Если срок с этой даты короткий, экран предупреждает об этом.",
    stage: 2,
    render: () => (
      <FormalView
        application={application({ stage: 2 })}
        canEnter
        minIntervalMonths={SETTINGS.minIntervalMonths}
        onSubmit={noop}
      />
    ),
  },
  {
    role: "Руководитель",
    title: "Список оценивающих",
    description:
      "Руководитель подразделения цели проверяет список оценивающих кандидата и при необходимости дополняет его.",
    stage: 3,
    render: () => (
      <RatersView
        application={application({
          stage: 3,
          status: "Admitted",
          roles: ["Head"],
          formal: FORMAL_ENTERED,
        })}
        candidates={SEARCH_RESULTS}
        isSearching={false}
        onSearch={nothing}
        onSubmit={noop}
      />
    ),
  },
  {
    role: "Оценивающий",
    title: "Кого нужно оценить",
    description:
      "Оценивающий видит список кандидатов, которых его просили оценить, и отметки о том, кого он уже оценил.",
    stage: 3,
    render: () => <RatingsView tasks={RATING_TASKS} />,
  },
  {
    role: "Оценивающий",
    title: "Оценка кандидата",
    description:
      "Оценивающий выбирает балл по каждой компетенции или отмечает, что не наблюдал её проявлений. Видны только имя кандидата и компетенции.",
    stage: 3,
    render: () => <RatingView rating={RATING} onSubmit={noop} />,
  },
  {
    role: "Кандидат",
    title: "Артефакты",
    description:
      "Кандидат добавляет артефакты к компетенциям: описание и ссылки. К отмеченным компетенциям артефакт обязателен.",
    stage: 4,
    render: () => (
      <ApplicationView
        application={application({
          stage: 4,
          status: "Admitted",
          formal: FORMAL_ENTERED,
          artifacts: ARTIFACTS,
          mandatoryArtifactCompetencyIds: [DESIGN.id],
        })}
        isBusy={false}
        onWithdraw={nothing}
        onMarkFeedback={nothing}
        onSaveArtifacts={noop}
      />
    ),
  },
  {
    role: "Член комиссии",
    title: "Рейтинг кандидатов",
    description:
      "Член комиссии видит кандидатов подразделения по рейтингу и открывает карточку каждого.",
    stage: 5,
    render: () => <RankingView items={RANKING} isLoading={false} />,
  },
  {
    role: "Член комиссии",
    title: "Карточка кандидата",
    description:
      "Член комиссии изучает самооценку, сводку круговой оценки без имён, артефакты, формальную проверку и прошлые заявки. Итоговые оценки вносит председатель.",
    stage: 5,
    render: () => (
      <ReviewView review={review({ roles: ["Member"] })} onSubmit={noop} />
    ),
  },
  {
    role: "Председатель комиссии",
    title: "Итоговые оценки и решение",
    description:
      "Председатель вносит итоговый балл по каждой компетенции, видит рассчитанные рейтинг и готовность и фиксирует решение комиссии.",
    stage: 5,
    render: () => (
      <ReviewView review={review({ roles: ["Chair"] })} onSubmit={noop} />
    ),
  },
  {
    role: "Кандидат",
    title: "Результат",
    description:
      "После публикации результатов кандидат видит решение комиссии и итоговые баллы против требований.",
    stage: 6,
    render: () => (
      <ApplicationView
        application={application({
          stage: 6,
          status: "Decided",
          formal: FORMAL_ENTERED,
          artifacts: ARTIFACTS,
          result: { decision: "Meets", competencies: RESULT_COMPETENCIES },
        })}
        isBusy={false}
        onWithdraw={nothing}
        onMarkFeedback={nothing}
        onSaveArtifacts={noop}
      />
    ),
  },
  {
    role: "Кандидат",
    title: "Обратная связь",
    description:
      "Обратную связь кандидату дают вне системы. Получив её, кандидат отмечает это в заявке.",
    stage: 7,
    render: () => (
      <ApplicationView
        application={application({
          stage: 7,
          status: "Decided",
          formal: FORMAL_ENTERED,
          artifacts: ARTIFACTS,
          result: { decision: "Meets", competencies: RESULT_COMPETENCIES },
        })}
        isBusy={false}
        onWithdraw={nothing}
        onMarkFeedback={nothing}
        onSaveArtifacts={noop}
      />
    ),
  },
  {
    role: "HR",
    title: "Проверка отметок обратной связи",
    description:
      "На последнем этапе волны HR следит за её ходом. После его окончания HR получает список кандидатов, которые не отметили получение обратной связи.",
    stage: 7,
    render: () => (
      <WaveView
        wave={wave(7)}
        units={UNITS}
        durations={SETTINGS.stageDurationsDays}
        canEdit
        onSubmit={noop}
      />
    ),
  },
];
