import { Button, Flex, Form, Input, Tag, Typography } from "antd";
import { useContext, useState } from "react";
import { ModalSavedContext } from "../../app/modalSaved";
import type { RaterDetails } from "./promotionApi";
import {
  describeEmailFailure,
  isSameEmail,
  normalizedEmail,
} from "./raterEmail";

interface RaterEmailsViewProps {
  raters: readonly RaterDetails[];
  /** Whether the viewer may enter and confirm the addresses now. */
  canEdit: boolean;
  /** Enters and confirms the address of a rater; a refusal is shown by the row. */
  onConfirm: (employeeId: string, email: string) => Promise<unknown>;
}

interface EmailsFormValues {
  emails: Record<string, string>;
}

/** The addresses of the raters who have no account, each entered or corrected and confirmed on its own. Show it only when some rater has no account. */
export function RaterEmailsView({
  raters,
  canEdit,
  onConfirm,
}: RaterEmailsViewProps) {
  const [form] = Form.useForm<EmailsFormValues>();
  const markSaved = useContext(ModalSavedContext);
  const [busyId, setBusyId] = useState<string>();
  const [failures, setFailures] = useState<Record<string, string>>({});
  const withoutAccount = raters.filter((rater) => !rater.hasAccount);
  const entered = Form.useWatch("emails", form);

  async function confirm(rater: RaterDetails) {
    setBusyId(rater.employeeId);
    setFailures(({ [rater.employeeId]: _dropped, ...rest }) => rest);
    try {
      await onConfirm(
        rater.employeeId,
        form.getFieldValue(["emails", rater.employeeId]) ?? "",
      );
      markSaved();
    } catch (error) {
      setFailures((current) => ({
        ...current,
        [rater.employeeId]: describeEmailFailure(error),
      }));
    } finally {
      setBusyId(undefined);
    }
  }

  return (
    <Flex vertical gap="small">
      <Typography.Title level={2} style={{ margin: 0 }}>
        Почта оценивающих
      </Typography.Title>
      <Form
        form={form}
        name="promotion-rater-emails"
        layout="vertical"
        disabled={busyId !== undefined}
        initialValues={{
          emails: Object.fromEntries(
            withoutAccount.map((rater) => [
              rater.employeeId,
              rater.email ?? "",
            ]),
          ),
        }}
      >
        {withoutAccount.map((rater) => {
          const email = entered?.[rater.employeeId] ?? rater.email ?? "";
          const isChanged = !isSameEmail(email, rater.email);
          const canConfirm =
            canEdit &&
            normalizedEmail(email) !== null &&
            (isChanged || !rater.isEmailConfirmed);
          return (
            <Flex key={rater.employeeId} vertical gap="small">
              <Typography.Text>{rater.fullName}</Typography.Text>
              <Flex gap="small" align="start">
                <Form.Item
                  name={["emails", rater.employeeId]}
                  validateStatus={
                    failures[rater.employeeId] ? "error" : undefined
                  }
                  help={failures[rater.employeeId]}
                  style={{ flex: 1, margin: 0 }}
                >
                  <Input
                    type="email"
                    autoComplete="off"
                    aria-label={`Почта: ${rater.fullName}`}
                    disabled={!canEdit}
                  />
                </Form.Item>
                <Button
                  disabled={!canConfirm}
                  loading={busyId === rater.employeeId}
                  aria-label={`Подтвердить почту: ${rater.fullName}`}
                  onClick={() => void confirm(rater)}
                >
                  Подтвердить
                </Button>
              </Flex>
              {!isChanged && rater.email && (
                <Flex>
                  <Tag color={rater.isEmailConfirmed ? "success" : "warning"}>
                    {rater.isEmailConfirmed
                      ? `Почта подтверждена${rater.emailConfirmedByName ? `: ${rater.emailConfirmedByName}` : ""}`
                      : "Почта не подтверждена"}
                  </Tag>
                </Flex>
              )}
            </Flex>
          );
        })}
      </Form>
    </Flex>
  );
}
