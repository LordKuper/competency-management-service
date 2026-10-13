import { theme as antdTheme, Card, Empty, Typography } from "antd";
import { Link } from "react-router";
import type { Desk, DeskGroup } from "./deskApi";
import { deskPaths, itemAction } from "./deskTexts";

/** The roles the scenario cards follow; process roles come from the units the user heads or answers for as HR. */
export interface SessionRoles {
  isAdmin: boolean;
  isHead: boolean;
  isHr: boolean;
  /** Bound to an employee card. */
  isBound: boolean;
}

interface Scenario {
  key: string;
  title: string;
  description: string;
  isFor: (roles: SessionRoles) => boolean;
  /** Fixed entry screen; scenarios opened on one version start from the first item waiting in their groups. */
  path?: string;
  /** Groups whose items the scenario works through. */
  groups?: DeskGroup[];
  /** Reference queues are counted as waiting in the queue, not waiting for the user. */
  isQueue?: boolean;
}

const SCENARIOS: Scenario[] = [
  {
    key: "build",
    title: "Построить матрицу компетенций",
    description:
      "Шаг за шагом: трек, компетенции, требования, формула и отправка на согласование.",
    isFor: (roles) => roles.isHead,
    path: deskPaths.matrixBuilder,
    groups: ["UnsubmittedVersions", "MatricesToBuild"],
  },
  {
    key: "rework",
    title: "Доработать возвращённую версию",
    description:
      "Посмотрите правки согласующего и решите, принять их, доработать или откатить.",
    isFor: (roles) => roles.isHead,
    groups: ["ReturnedVersions"],
  },
  {
    key: "review",
    title: "Согласовать версию",
    description:
      "Что изменилось, полна ли версия и что опубликуется вместе с ней.",
    isFor: (roles) => roles.isHead || roles.isHr,
    groups: ["VersionsToApprove"],
  },
  {
    key: "drafts",
    title: "Утвердить черновые записи",
    description:
      "Очередь черновых записей справочников: утвердить или поправить и утвердить.",
    isFor: (roles) => roles.isHr || roles.isAdmin,
    path: deskPaths.drafts,
    groups: ["DraftsToApprove"],
  },
  {
    key: "connect",
    title: "Подключить подразделение",
    description:
      "Руководитель, учётные записи, ответственный HR и должности подразделения — пробелы закрываются на месте.",
    isFor: (roles) => roles.isAdmin,
    path: deskPaths.connectUnit,
    groups: ["UnitsToConnect"],
  },
  {
    key: "onboard",
    title: "Принять сотрудника",
    description:
      "Карточка сотрудника и, по желанию, учётная запись с приглашением — одним сценарием.",
    isFor: (roles) => roles.isAdmin,
    path: deskPaths.onboardEmployee,
  },
  {
    key: "link",
    title: "Связать должности с треком",
    description: "Какие должности относятся к каким уровням трека.",
    isFor: (roles) => roles.isHead || roles.isHr || roles.isAdmin,
    path: deskPaths.trackPositions,
    groups: ["PositionsToLink"],
  },
  {
    key: "catalog",
    title: "Привести каталог в порядок",
    description:
      "Неиспользуемые записи справочников: удалить или деактивировать.",
    isFor: (roles) => roles.isHr || roles.isAdmin,
    path: deskPaths.catalogCleanup,
    groups: ["UnusedCatalogEntries"],
    isQueue: true,
  },
  {
    key: "org",
    title: "Навести порядок в оргструктуре",
    description:
      "Расхождения оргструктуры и учётных записей: должности вне перечня, учётные записи без сотрудника.",
    isFor: (roles) => roles.isAdmin,
    path: deskPaths.orgCleanup,
    groups: ["OrgCleanup"],
  },
  {
    key: "my-place",
    title: "Моё место в компании",
    description:
      "Ваше подразделение, должность, руководитель, ответственный HR, карьерные треки и требования.",
    isFor: (roles) => roles.isBound,
    path: deskPaths.myPlace,
  },
];

/**
 * "Что можно сделать": a card for every scenario the user's roles allow, never a disabled one. A card counts what
 * waits in its groups once the desk is read; the cards themselves do not wait for it.
 */
export function DeskScenarios({
  roles,
  desk,
}: {
  roles: SessionRoles;
  desk: Desk | undefined;
}) {
  const { token } = antdTheme.useToken();
  const scenarios = SCENARIOS.filter((scenario) => scenario.isFor(roles));
  if (scenarios.length === 0) {
    return (
      <Card>
        <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          description={
            <>
              <Typography.Title level={3}>
                Сценариев для вашей учётной записи пока нет
              </Typography.Title>
              <Typography.Paragraph>
                Учётная запись не привязана к сотруднику, и ролей процесса у неё
                нет. Обратитесь к администратору, чтобы вас привязали к карточке
                сотрудника.
              </Typography.Paragraph>
            </>
          }
        />
      </Card>
    );
  }
  return (
    <div
      style={{
        display: "grid",
        gridTemplateColumns: `repeat(auto-fill, minmax(${token.marginXXL * 5}px, 1fr))`,
        gap: token.margin,
      }}
    >
      {scenarios.map((scenario) => (
        <ScenarioCard key={scenario.key} scenario={scenario} desk={desk} />
      ))}
    </div>
  );
}

function ScenarioCard({
  scenario,
  desk,
}: {
  scenario: Scenario;
  desk: Desk | undefined;
}) {
  const { token } = antdTheme.useToken();
  const groups = desk?.groups.filter((group) =>
    scenario.groups?.includes(group.group),
  );
  const waiting = groups?.reduce((sum, group) => sum + group.total, 0);
  const firstItem = groups?.[0]?.items[0];
  const path =
    scenario.path ?? (firstItem && itemAction(firstItem)?.path) ?? null;
  return (
    <Card styles={{ body: { padding: token.padding } }}>
      <Typography.Title level={4} style={{ marginTop: 0 }}>
        {path ? <Link to={path}>{scenario.title}</Link> : scenario.title}
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        {scenario.description}
      </Typography.Paragraph>
      {scenario.groups && waiting !== undefined && (
        <Typography.Text
          style={{ color: token.colorTextTertiary, fontSize: token.fontSizeSM }}
        >
          {waitingCaption(waiting, scenario.isQueue)}
        </Typography.Text>
      )}
    </Card>
  );
}

function waitingCaption(waiting: number, isQueue: boolean | undefined): string {
  if (waiting === 0) return "Сейчас таких нет";
  return isQueue ? `В очереди: ${waiting}` : `Ждут вас: ${waiting}`;
}
