import { Typography } from "antd";
import { AppModal } from "../../app/AppModal";
import { useMailReport } from "../users/useSendUserMail";
import { CreateAccountForm } from "./CreateAccountForm";

/** Whose account the dialog creates: the unit's head, or its assigned HR. */
type AccountAddressee = "head" | "hr";

const TITLES: Record<AccountAddressee, string> = {
  head: "Учётная запись руководителя",
  hr: "Учётная запись HR",
};

interface AccountModalProps {
  /** The employee the account is created for and bound to. */
  employee: { id: string; fullName: string };
  /** An HR's account gets the role «HR», without which the assignment does not act. */
  addressee: AccountAddressee;
  onClose: () => void;
}

/** Dialog that creates the account of a unit's head or HR with an invitation. Mount it only while it is shown. */
export function AccountModal({
  employee,
  addressee,
  onClose,
}: AccountModalProps) {
  const reportMail = useMailReport();
  const isHr = addressee === "hr";
  return (
    <AppModal open title={TITLES[addressee]} onCancel={onClose} footer={null}>
      <Typography.Paragraph>
        {`У ${employee.fullName} нет учётной записи. Она будет создана ${isHr ? "с ролью «HR» и " : ""}с приглашением по почте и привязана к нему.`}
      </Typography.Paragraph>
      <CreateAccountForm
        employeeId={employee.id}
        roles={isHr ? ["Hr"] : undefined}
        submitLabel="Создать и отправить приглашение"
        onCreated={(account) => {
          onClose();
          reportMail(
            account.mailSent,
            "Учётная запись создана, приглашение отправлено",
          );
        }}
        onCancel={onClose}
      />
    </AppModal>
  );
}
