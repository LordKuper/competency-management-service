import { Button, Tour, type TourProps } from "antd";
import { useRef, useState } from "react";
import { useLocation } from "react-router";
import type { TourStep } from "./featureContract";
import { tourFor } from "./features";

const GENERAL_STEP: TourStep = {
  title: "Об этом экране",
  description:
    "Здесь вы работаете с одним из разделов приложения. Остальные разделы открываются из меню вверху.",
};

/** antd spreads these onto the «Далее» button; `autoFocus` puts the keyboard there as the tour opens, and the button keeps focus from step to step. antd's own type for them does not list `autoFocus`, hence the class name that makes the object assignable. */
const NEXT_BUTTON_PROPS = { className: "tour-next", autoFocus: true };

/** Steps whose element is on the screen now, shaped for the tour; the element is looked up once, when the tour opens. */
function visibleSteps(pathname: string): NonNullable<TourProps["steps"]> {
  return (tourFor(pathname) ?? [GENERAL_STEP]).flatMap(
    ({ target, title, description }) => {
      const step = { title, description, nextButtonProps: NEXT_BUTTON_PROPS };
      if (!target) return [step];
      const element = document.querySelector<HTMLElement>(
        `[data-tour="${target}"]`,
      );
      return element ? [{ ...step, target: element }] : [];
    },
  );
}

/** Round «?» button of the navigation bar and the step-by-step tour of the current screen: the screen is dimmed and each step points at an element marked with `data-tour`. Closing returns focus to the button. */
export function ScreenTour() {
  const { pathname } = useLocation();
  return <PathTour key={pathname} pathname={pathname} />;
}

function PathTour({ pathname }: { pathname: string }) {
  const [steps, setSteps] = useState<TourProps["steps"]>();
  const buttonRef = useRef<HTMLButtonElement>(null);
  const close = () => {
    setSteps(undefined);
    buttonRef.current?.focus();
  };
  return (
    <>
      <Button
        ref={buttonRef}
        shape="circle"
        type="text"
        className="header-user-button header-help-button"
        aria-label="Справка по экрану"
        onClick={() => setSteps(visibleSteps(pathname))}
      >
        ?
      </Button>
      <Tour open={steps !== undefined} steps={steps} onClose={close} />
    </>
  );
}
