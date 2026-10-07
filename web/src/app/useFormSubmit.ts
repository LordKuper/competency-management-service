import type { FormInstance } from "antd";
import { useState } from "react";
import { showFieldErrors } from "./apiErrors";

/**
 * The submit handler of a form whose save is asynchronous: it marks the form as submitting while the save runs and
 * puts the server's field messages under the fields when the save is rejected. The caller shows any other failure.
 */
export function useFormSubmit<TValues>(
  form: FormInstance<TValues>,
  onSubmit: (values: TValues) => Promise<unknown>,
) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function submit(values: TValues) {
    setIsSubmitting(true);
    try {
      await onSubmit(values);
    } catch (error) {
      showFieldErrors(form, error);
    } finally {
      setIsSubmitting(false);
    }
  }

  return { isSubmitting, submit };
}
