import { Button, type ButtonProps } from "antd";
import { useNavigate } from "react-router";

interface LinkButtonProps extends Pick<ButtonProps, "type" | "children"> {
  to: string;
  /** Navigation state handed to the opened screen, such as the return link of its breadcrumb. */
  state?: unknown;
}

/** A link that looks like a button: it is an anchor, so it opens in a new tab on request, and navigates inside the application otherwise. */
export function LinkButton({ to, type, state, children }: LinkButtonProps) {
  const navigate = useNavigate();
  return (
    <Button
      type={type}
      href={to}
      onClick={(event) => {
        event.preventDefault();
        void navigate(to, { state });
      }}
    >
      {children}
    </Button>
  );
}
