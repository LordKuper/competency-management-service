import { CheckCircleOutlined } from "@ant-design/icons";
import {
  theme as antdTheme,
  Empty,
  Flex,
  Skeleton,
  Tag,
  Typography,
} from "antd";
import { ErrorAlert } from "../../app/ErrorAlert";
import { contentMaxWidth } from "../../app/theme";
import { useCurrentUser, useIsAdmin, useIsHr } from "../auth/useCurrentUser";
import { useProcessRoles } from "../career/careerApi";
import { ActionGroup, DeskPanel, ReferenceGroup } from "./DeskGroups";
import { DeskScenarios, type SessionRoles } from "./DeskScenarios";
import { type Desk, useDesk } from "./deskApi";
import { PromotionWavesBlock } from "./PromotionWavesBlock";
import { useResendInvitation } from "./useResendInvitation";

/**
 * The home page: what the system expects of the user, grouped by kind, the promotion waves open to everyone, and the scenarios of the user's roles. The
 * scenario cards come from the session roles and show without waiting for the expected actions.
 */
export function DeskPage() {
  const { token } = antdTheme.useToken();
  const { data: user } = useCurrentUser();
  const isAdmin = useIsAdmin();
  const isHr = useIsHr();
  const processRoles = useProcessRoles();
  const desk = useDesk();
  const roles: SessionRoles = {
    isAdmin,
    isHead: (processRoles.data?.headedUnits.length ?? 0) > 0,
    isHr,
    isUnitHr: (processRoles.data?.hrUnits.length ?? 0) > 0,
    isBound: user?.employeeId != null,
  };
  return (
    <Flex
      vertical
      gap={token.marginLG}
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Typography.Title level={1} style={{ margin: 0 }}>
        Рабочий стол
      </Typography.Title>
      <Flex vertical gap={token.margin}>
        <Flex align="center" gap={token.marginXS}>
          <Typography.Title level={2} style={{ margin: 0 }}>
            Ожидают вашего действия
          </Typography.Title>
          {desk.data && desk.data.actionCount > 0 && (
            <Tag color="processing">{desk.data.actionCount}</Tag>
          )}
        </Flex>
        <ExpectedActions
          desk={desk.data}
          error={desk.error}
          onRetry={() => void desk.refetch()}
          roles={roles}
        />
      </Flex>
      <PromotionWavesBlock />
      <Flex vertical gap={token.margin}>
        <Typography.Title level={2} style={{ margin: 0 }}>
          Что можно сделать
        </Typography.Title>
        {processRoles.isPending ? (
          <Skeleton active />
        ) : (
          <DeskScenarios roles={roles} desk={desk.data} />
        )}
      </Flex>
    </Flex>
  );
}

/**
 * The expected actions block. Rows already shown stay while the desk is read again; a failed first read is reported
 * here only, the count in the header stays hidden.
 */
function ExpectedActions({
  desk,
  error,
  onRetry,
  roles,
}: {
  desk: Desk | undefined;
  error: Error | null;
  onRetry: () => void;
  roles: SessionRoles;
}) {
  const { token } = antdTheme.useToken();
  const resend = useResendInvitation();
  if (!desk) {
    return error ? (
      <ErrorAlert
        title="Не удалось загрузить ожидаемые действия"
        error={error}
        onRetry={onRetry}
      />
    ) : (
      <DeskPanel>
        <Skeleton active style={{ padding: token.padding }} />
      </DeskPanel>
    );
  }
  const actionGroups = desk.groups.filter((group) => !group.isReference);
  const referenceGroups = desk.groups.filter((group) => group.isReference);
  return (
    <>
      {actionGroups.length > 0 ? (
        actionGroups.map((group) => (
          <ActionGroup key={group.group} group={group} resend={resend} />
        ))
      ) : (
        <AllDone roles={roles} />
      )}
      {referenceGroups.length > 0 && (
        <ReferenceGroup groups={referenceGroups} />
      )}
    </>
  );
}

function AllDone({ roles }: { roles: SessionRoles }) {
  const { token } = antdTheme.useToken();
  const hasProcessRole =
    roles.isAdmin || roles.isHead || roles.isHr || roles.isUnitHr;
  return (
    <DeskPanel>
      <Empty
        image={
          <CheckCircleOutlined
            style={{
              color: token.colorSuccess,
              fontSize: token.fontSizeHeading2,
            }}
          />
        }
        styles={{
          root: { padding: token.paddingXL },
          image: { height: "auto" },
        }}
        description={
          <>
            <Typography.Title level={3}>Всё сделано</Typography.Title>
            <Typography.Paragraph>
              {allDoneText(roles, hasProcessRole)}
            </Typography.Paragraph>
          </>
        }
      />
    </DeskPanel>
  );
}

function allDoneText(roles: SessionRoles, hasProcessRole: boolean): string {
  if (hasProcessRole) {
    return "Действий, которые ждут вас, сейчас нет. Загляните в «Что можно сделать» ниже.";
  }
  if (roles.isBound) {
    return "От вас сейчас ничего не ждут. Свои данные в компании можно посмотреть в «Моё место в компании».";
  }
  return "От вас сейчас ничего не ждут.";
}
