import { Typography } from "antd";
import { AppModal } from "../../app/AppModal";
import { useMailReport } from "../users/useSendUserMail";
import { CreateAccountForm } from "./CreateAccountForm";

interface AccountModalProps {
  /** The head the account is created for. */
  head: { id: string; fullName: string };
  onClose: () => void;
}

/** Dialog that creates the account of a unit's head with an invitation. Mount it only while it is shown. */
export function AccountModal({ head, onClose }: AccountModalProps) {
  const reportMail = useMailReport();
  return (
    <AppModal
      open
      title="Учётная запись руководителя"
      onCancel={onClose}
      footer={null}
    >
      <Typography.Paragraph>
        {`У ${head.fullName} нет учётной записи. Она будет создана с приглашением по почте и привязана к нему.`}
      </Typography.Paragraph>
      <CreateAccountForm
        employeeId={head.id}
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
